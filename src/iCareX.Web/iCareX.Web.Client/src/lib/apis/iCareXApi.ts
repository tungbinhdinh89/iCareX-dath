import type {
  CsrfResponse,
  LoginRequest,
  RegisterRequest,
  EmailActionRequest,
  VerifyEmailRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
} from "@shared/auth-contracts"
import type {
  VehicleInvitationResponse,
  CreateVehicleInvitationRequest,
  InvitationTokenRequest,
  CurrentUserPermissionsResponse,
  VehiclePermissionsResponse,
  VehicleMemberResponse,
  UpdateVehicleMemberRequest,
  VehicleListResponse,
  VehicleResponse,
  OdometerReadingResponse as ReadingResponse,
} from "@shared/vehicle-contracts"
import {
  defaultImage,
  type Account,
  type Entry,
  type Garage,
  type Vehicle,
} from "../models/garage.ts"
export type { Account, Entry, Garage, Plan, Vehicle } from "../models/garage.ts"
export type VehicleTypeOption = { id: string; code: string; name: string }
export type VehicleBrandOption = {
  id: string
  vehicleTypeId: string
  name: string
}
export type VehicleModelOption = {
  id: string
  vehicleBrandId: string
  name: string
}
let csrfToken = ""
let csrfRequest: Promise<string> | null = null
let sessionGeneration = 0

export function resetSessionRequests() {
  sessionGeneration += 1
  csrfToken = ""
  csrfRequest = null
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
const accessListeners = new Set<(vehicleId: string) => void>()

export function onVehicleAccessChanged(listener: (vehicleId: string) => void) {
  accessListeners.add(listener)
  return () => {
    accessListeners.delete(listener)
  }
}

export function onUnauthorized(listener: () => void) {
  unauthorizedListeners.add(listener)
  return () => {
    unauthorizedListeners.delete(listener)
  }
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
        : (body?.detail ?? body?.title ?? "Yêu cầu xác thực thất bại."),
      response.status,
    )
  }
  return response.status === 204 ? undefined as T : response.json()
}
async function csrf() {
  if (!csrfRequest) {
    const generation = sessionGeneration
    csrfRequest = authRequest<CsrfResponse>("/csrf")
      .then((response) => {
        if (generation !== sessionGeneration)
          throw new ApiError("Session changed.", 401)
        csrfToken = response.token
        return csrfToken
      })
      .finally(() => {
        if (generation === sessionGeneration) csrfRequest = null
      })
  }
  return csrfRequest
}
async function authMutation<T>(path: string, body?: unknown) {
  const generation = sessionGeneration
  if (!csrfToken) await csrf()
  if (generation !== sessionGeneration)
    throw new ApiError("Session changed.", 401)
  try {
    const result = await authRequest<T>(path, {
      method: "POST",
      headers: { "X-CSRF-TOKEN": csrfToken },
      body: JSON.stringify(body ?? {}),
    })
    if (generation !== sessionGeneration)
      throw new ApiError("Session changed.", 401)
    return result
  } catch (error) {
    if (
      generation === sessionGeneration &&
      ["/logout", "/change-password"].includes(path) &&
      error instanceof ApiError &&
      error.status === 401
    )
      unauthorizedListeners.forEach((listener) => listener())
    throw error
  }
}
async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const generation = sessionGeneration
  const mutation = !["GET", "HEAD"].includes(
    (init?.method ?? "GET").toUpperCase(),
  )
  if (mutation && !csrfToken) await csrf()
  if (generation !== sessionGeneration)
    throw new ApiError("Session changed.", 401)
  const response = await fetch(`/api/v1${path}`, {
    ...init,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(mutation ? { "X-CSRF-TOKEN": csrfToken } : {}),
      ...(init?.headers ?? {}),
    },
  })
  if (!response.ok) {
    if (response.status === 401 && generation === sessionGeneration)
      unauthorizedListeners.forEach((listener) => listener())
    if (
      generation === sessionGeneration &&
      [403, 404].includes(response.status)
    ) {
      const vehicle = /^\/vehicles\/([^/?]+)/.exec(path)?.[1]
      if (vehicle) accessListeners.forEach((listener) => listener(vehicle))
    }
    const body = await response.json().catch(() => null)
    const fields = body?.errors as Record<string, string[]> | undefined
    throw new ApiError(
      fields
        ? Object.values(fields).flat().join(" ")
        : (body?.detail ?? body?.title ?? "Yêu cầu API thất bại."),
      response.status,
    )
  }
  const result =
    response.status === 204 ? undefined as T : (await response.json()) as T
  if (generation !== sessionGeneration)
    throw new ApiError("Session changed.", 401)
  return result
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
  requestVerification(email: string) {
    return authMutation<unknown>("/verification-email", {
      email,
    } satisfies EmailActionRequest)
  },
  verifyEmail(request: VerifyEmailRequest) {
    return authMutation<void>("/verify-email", request)
  },
  forgotPassword(email: string) {
    return authMutation<unknown>("/forgot-password", {
      email,
    } satisfies EmailActionRequest)
  },
  resetPassword(request: ResetPasswordRequest) {
    return authMutation<void>("/reset-password", request)
  },
  changePassword(request: ChangePasswordRequest) {
    return authMutation<void>("/change-password", request)
  },
  vehicleInvitations(vehicleId: string, signal?: AbortSignal) {
    return apiRequest<VehicleInvitationResponse[]>(
      `/vehicles/${encodeURIComponent(vehicleId)}/invitations`,
      { signal },
    )
  },
  createVehicleInvitation(
    vehicleId: string,
    request: CreateVehicleInvitationRequest,
  ) {
    return apiRequest<VehicleInvitationResponse>(
      `/vehicles/${encodeURIComponent(vehicleId)}/invitations`,
      { method: "POST", body: JSON.stringify(request) },
    )
  },
  revokeVehicleInvitation(
    vehicleId: string,
    invitationId: string,
    version: number,
  ) {
    return apiRequest<void>(
      `/vehicles/${encodeURIComponent(vehicleId)}/invitations/${encodeURIComponent(invitationId)}?version=${version}`,
      { method: "DELETE" },
    )
  },
  invitationInbox(signal?: AbortSignal) {
    return apiRequest<VehicleInvitationResponse[]>("/invitations/inbox", {
      signal,
    })
  },
  previewInvitation(invitationId: string, token: string) {
    return apiRequest<VehicleInvitationResponse>(
      `/invitations/${encodeURIComponent(invitationId)}/preview`,
      {
        method: "POST",
        body: JSON.stringify({ token } satisfies InvitationTokenRequest),
      },
    )
  },
  acceptInvitation(invitationId: string, token: string) {
    return apiRequest<void>(
      `/invitations/${encodeURIComponent(invitationId)}/accept`,
      {
        method: "POST",
        body: JSON.stringify({ token } satisfies InvitationTokenRequest),
      },
    )
  },
  rejectInvitation(invitationId: string, token: string) {
    return apiRequest<void>(
      `/invitations/${encodeURIComponent(invitationId)}/reject`,
      {
        method: "POST",
        body: JSON.stringify({ token } satisfies InvitationTokenRequest),
      },
    )
  },
  currentPermissions(signal?: AbortSignal) {
    return apiRequest<CurrentUserPermissionsResponse>("/me/permissions", {
      signal,
    })
  },
  vehiclePermissions(vehicleId: string, signal?: AbortSignal) {
    return apiRequest<VehiclePermissionsResponse>(
      `/vehicles/${encodeURIComponent(vehicleId)}/permissions`,
      { signal },
    )
  },
  vehicleMembers(vehicleId: string, signal?: AbortSignal) {
    return apiRequest<VehicleMemberResponse[]>(
      `/vehicles/${encodeURIComponent(vehicleId)}/members`,
      { signal },
    )
  },
  updateVehicleMember(
    vehicleId: string,
    userId: string,
    request: UpdateVehicleMemberRequest,
  ) {
    return apiRequest<VehicleMemberResponse>(
      `/vehicles/${encodeURIComponent(vehicleId)}/members/${encodeURIComponent(userId)}`,
      { method: "PATCH", body: JSON.stringify(request) },
    )
  },
  revokeVehicleMember(vehicleId: string, userId: string, version: number) {
    return apiRequest<void>(
      `/vehicles/${encodeURIComponent(vehicleId)}/members/${encodeURIComponent(userId)}?version=${version}`,
      { method: "DELETE" },
    )
  },
  async vehicleTypes() {
    return apiRequest<VehicleTypeOption[]>("/vehicle-catalog/types")
  },
  async vehicleBrands(vehicleTypeId: string) {
    return apiRequest<VehicleBrandOption[]>(
      `/vehicle-catalog/types/${vehicleTypeId}/brands`,
    )
  },
  async vehicleModels(vehicleBrandId: string) {
    return apiRequest<VehicleModelOption[]>(
      `/vehicle-catalog/brands/${vehicleBrandId}/models`,
    )
  },
  async login(email: string, password: string) {
    return authMutation<Account>("/login", {
      email,
      password,
    } satisfies LoginRequest)
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
  async get(account: Account | null, signal?: AbortSignal): Promise<Garage> {
    if (!account)
      return {
        vehicles: [],
        entries: [],
        plans: [],
        profile: { name: "", email: "", phone: "" },
        settings: { reminders: true, thresholdKm: 500, thresholdDays: 30 },
      }
    const response = await apiRequest<VehicleListResponse>(
      "/vehicles?includeArchived=true",
      { signal },
    )
    const vehicles = response.vehicles.map(mapVehicle)
    const readings = (
      await Promise.all(
        vehicles.map((vehicle) =>
          apiRequest<ReadingResponse[]>(
            `/vehicles/${vehicle.id}/odometer-readings`,
            { signal },
          ),
        ),
      )
    )
      .flat()
      .map(mapReading)
    return {
      vehicles,
      entries: readings,
      plans: [],
      profile: {
        name: account.name,
        email: account.email,
        phone: account.phone,
      },
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
  async updateVehicle(
    id: string,
    request: {
      name: string
      plate: string
      vehicleModelId: string
      year: number
      baseOdo: number
      image?: string
      version: number
    },
  ) {
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
    return mapVehicle(
      await apiRequest<VehicleResponse>(`/vehicles/${id}/archive`, {
        method: "POST",
        body: JSON.stringify({ version }),
      }),
    )
  },
  async restoreVehicle(id: string, version: number) {
    return mapVehicle(
      await apiRequest<VehicleResponse>(`/vehicles/${id}/restore`, {
        method: "POST",
        body: JSON.stringify({ version }),
      }),
    )
  },
  async createOdometerReading(
    vehicleId: string,
    request: {
      date: string
      odo: number
      note: string
    },
  ) {
    return mapReading(
      await apiRequest<ReadingResponse>(
        `/vehicles/${vehicleId}/odometer-readings`,
        {
          method: "POST",
          body: JSON.stringify(request),
        },
      ),
    )
  },
  async updateOdometerReading(
    vehicleId: string,
    readingId: string,
    request: {
      date: string
      odo: number
      note: string
      version: number
    },
  ) {
    return mapReading(
      await apiRequest<ReadingResponse>(
        `/vehicles/${vehicleId}/odometer-readings/${readingId}`,
        { method: "PUT", body: JSON.stringify(request) },
      ),
    )
  },
  async deleteOdometerReading(
    vehicleId: string,
    readingId: string,
    version: number,
  ) {
    await apiRequest<void>(
      `/vehicles/${vehicleId}/odometer-readings/${readingId}?version=${version}`,
      { method: "DELETE" },
    )
  },
  async update(_id: string, _action: (data: Garage) => void): Promise<Garage> {
    throw new Error("Tính năng này đang chờ API của ticket tiếp theo.")
  },
}
