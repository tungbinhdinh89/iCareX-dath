import type { CsrfResponse, LoginRequest, RegisterRequest } from "@shared/auth-contracts"
import type { VehicleResponse, OdometerReadingResponse as ReadingResponse } from "@shared/vehicle-contracts"
import {
  defaultImage,
  type Account,
  type Entry,
  type Garage,
  type Vehicle,
} from "../models/garage.ts"
export type { Account, Entry, Garage, Plan, Vehicle } from "../models/garage.ts"

export type VehicleTypeOption = { id: string; code: string; name: string }
export type VehicleBrandOption = { id: string; vehicleTypeId: string; name: string }
export type VehicleModelOption = { id: string; vehicleBrandId: string; name: string }

let csrfToken = ""
let sessionGeneration = 0
export function resetSessionRequests() {
  sessionGeneration += 1
  csrfToken = ""
}
export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.name = "ApiError"
    this.status = status
  }
}
const unauthorizedListeners = new Set<() => void>()
export function onUnauthorized(listener: () => void) {
  unauthorizedListeners.add(listener)
  return () => { unauthorizedListeners.delete(listener) }
}
async function authRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/v1/auth${path}`, {
    ...init,
    credentials: "include",
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const fields = body?.errors as Record<string, string[]> | undefined
    throw new ApiError(
      fields
        ? Object.values(fields).flat().join(" ")
        : body?.detail ?? body?.title ?? "Yêu cầu xác thực thất bại.",
      response.status,
    )
  }
  return response.status === 204 ? (undefined as T) : response.json()
}
async function csrf() {
  const response = await authRequest<CsrfResponse>("/csrf")
  csrfToken = response.token
  return csrfToken
}
async function authMutation<T>(path: string, body?: unknown) {
  if (!csrfToken) await csrf()
  return authRequest<T>(path, {
    method: "POST",
    headers: { "X-CSRF-TOKEN": csrfToken },
    body: JSON.stringify(body ?? {}),
  })
}
async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const generation = sessionGeneration
  const response = await fetch(`/api/v1${path}`, {
    ...init,
    credentials: "include",
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  })
  if (!response.ok) {
    if (response.status === 401 && generation === sessionGeneration) unauthorizedListeners.forEach((listener) => listener())
    const body = await response.json().catch(() => null)
    const fields = body?.errors as Record<string, string[]> | undefined
    throw new ApiError(
      fields
        ? Object.values(fields).flat().join(" ")
        : body?.detail ?? body?.title ?? "Yêu cầu API thất bại.",
      response.status,
    )
  }
  return response.status === 204 ? (undefined as T) : response.json()
}
const mapVehicle = (vehicle: VehicleResponse): Vehicle => ({
  id: vehicle.id,
  name: vehicle.name,
  plate: vehicle.plate,
  type: vehicle.vehicleTypeName === "Ô tô" ? "Ô tô" : "Xe máy",
  vehicleTypeId: vehicle.vehicleTypeId,
  vehicleBrandId: vehicle.vehicleBrandId,
  vehicleBrandName: vehicle.vehicleBrandName,
  vehicleModelId: vehicle.vehicleModelId,
  vehicleModelName: vehicle.vehicleModelName,
  year: vehicle.year,
  baseOdo: vehicle.baseOdo,
  currentOdo: vehicle.currentOdo,
  image: vehicle.imageUrl || defaultImage(vehicle.vehicleTypeName),
  isArchived: vehicle.isArchived,
  version: vehicle.version,
})
const mapReading = (reading: ReadingResponse): Entry => ({
  id: reading.id,
  vehicleId: reading.vehicleId,
  kind: "odo",
  title: reading.title,
  date: reading.date,
  odo: reading.odo,
  cost: 0,
  liters: 0,
  note: reading.note,
  version: reading.version,
})
export const api = {
  async vehicleTypes() {
    return apiRequest<VehicleTypeOption[]>("/vehicle-catalog/types")
  },
  async vehicleBrands(vehicleTypeId: string) {
    return apiRequest<VehicleBrandOption[]>(`/vehicle-catalog/types/${vehicleTypeId}/brands`)
  },
  async vehicleModels(vehicleBrandId: string) {
    return apiRequest<VehicleModelOption[]>(`/vehicle-catalog/brands/${vehicleBrandId}/models`)
  },
  async login(email: string, password: string) {
    return authMutation<Account>("/login", { email, password } satisfies LoginRequest)
  },
  async register(request: RegisterRequest) {
    return authMutation<Account>("/register", request)
  },
  async me() {
    return authRequest<Account>("/me")
  },
  async logout() {
    await authMutation<void>("/logout")
  },
  async get(account: Account | null): Promise<Garage> {
    if (!account)
      return {
        vehicles: [],
        entries: [],
        plans: [],
        profile: { name: "", email: "", phone: "" },
        settings: { reminders: true, thresholdKm: 500, thresholdDays: 30 },
      }
    const response = await apiRequest<{ vehicles: VehicleResponse[] }>(
      "/vehicles?includeArchived=true",
    )
    const vehicles = response.vehicles.map(mapVehicle)
    const readings = (
      await Promise.all(
        vehicles.map((vehicle) =>
          apiRequest<ReadingResponse[]>(`/vehicles/${vehicle.id}/odometer-readings`),
        ),
      )
    ).flat().map(mapReading)
    return {
      vehicles,
      entries: readings,
      plans: [],
      profile: { name: account.name, email: account.email, phone: account.phone },
      settings: { reminders: true, thresholdKm: 500, thresholdDays: 30 },
    }
  },
  async createVehicle(request: {
    name: string
    plate: string
    vehicleModelId: string
    year: number
    baseOdo: number
    image?: string
  }) {
    const vehicle = await apiRequest<VehicleResponse>("/vehicles", {
      method: "POST",
      body: JSON.stringify({
        ...request,
        imageUrl: request.image ?? "",
      }),
    })
    return mapVehicle(vehicle)
  },
  async updateVehicle(id: string, request: {
    name: string
    plate: string
    vehicleModelId: string
    year: number
    baseOdo: number
    image?: string
    version: number
  }) {
    const vehicle = await apiRequest<VehicleResponse>(`/vehicles/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        ...request,
        imageUrl: request.image ?? "",
      }),
    })
    return mapVehicle(vehicle)
  },
  async archiveVehicle(id: string, version: number) {
    return mapVehicle(await apiRequest<VehicleResponse>(`/vehicles/${id}/archive`, {
      method: "POST", body: JSON.stringify({ version }),
    }))
  },
  async restoreVehicle(id: string, version: number) {
    return mapVehicle(await apiRequest<VehicleResponse>(`/vehicles/${id}/restore`, {
      method: "POST", body: JSON.stringify({ version }),
    }))
  },
  async createOdometerReading(vehicleId: string, request: {
    date: string
    odo: number
    note: string
  }) {
    return mapReading(await apiRequest<ReadingResponse>(`/vehicles/${vehicleId}/odometer-readings`, {
      method: "POST",
      body: JSON.stringify(request),
    }))
  },
  async updateOdometerReading(vehicleId: string, readingId: string, request: {
    date: string
    odo: number
    note: string
    version: number
  }) {
    return mapReading(await apiRequest<ReadingResponse>(
      `/vehicles/${vehicleId}/odometer-readings/${readingId}`,
      { method: "PUT", body: JSON.stringify(request) },
    ))
  },
  async deleteOdometerReading(vehicleId: string, readingId: string, version: number) {
    await apiRequest<void>(
      `/vehicles/${vehicleId}/odometer-readings/${readingId}?version=${version}`,
      { method: "DELETE" },
    )
  },
  async update(_id: string, _action: (data: Garage) => void): Promise<Garage> {
    throw new Error("Tính năng này đang chờ API của ticket tiếp theo.")
  },
}
