import type {
  AccountResponse,
  CsrfResponse,
  LoginRequest,
  RegisterRequest,
} from "../../shared/auth-contracts"

export type Vehicle = {
  id: string
  name: string
  plate: string
  type: "Ô tô" | "Xe máy"
  year: number
  baseOdo: number
  currentOdo: number
  image: string
  isArchived: boolean
  version: number
}
export type Entry = {
  id: string
  vehicleId: string
  kind: "fuel" | "service" | "odo"
  title: string
  date: string
  odo: number
  cost: number
  liters: number
  note: string
  planId?: string
  version?: number
}
export type Plan = {
  id: string
  vehicleId: string
  title: string
  dueDate: string
  dueOdo: number
  intervalKm: number
  intervalMonths: number
}
export type Garage = {
  vehicles: Vehicle[]
  entries: Entry[]
  plans: Plan[]
  profile: { name: string; email: string; phone: string }
  settings: { reminders: boolean; thresholdKm: number; thresholdDays: number }
}
export type Account = AccountResponse

export const money = (n: number) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(n)
export const number = (n: number) =>
  new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 2 }).format(n)
export const today = () =>
  new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Ho_Chi_Minh",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date())
export const dateLabel = (d: string) =>
  d ? d.split("-").reverse().join("/") : "—"
export const monthKey = (d: string) => d.slice(0, 7)
export const monthLabel = (d: string) =>
  `Tháng ${Number(d.slice(5, 7))}, ${d.slice(0, 4)}`
const assetBase = import.meta.env?.BASE_URL ?? "/"
const carImage = `${assetBase}images/car.jpg`
const bikeImage = `${assetBase}images/scooter.jpg`
export const defaultImage = (type: string) =>
  type === "Ô tô" ? carImage : bikeImage

type VehicleResponse = {
  id: string
  name: string
  plate: string
  type: "Car" | "Motorcycle"
  year: number
  baseOdo: number
  currentOdo: number
  imageUrl: string
  isArchived: boolean
  version: number
}
type ReadingResponse = {
  id: string
  vehicleId: string
  date: string
  odo: number
  title: string
  note: string
  version: number
}

let csrfToken = ""
async function authRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/v1/auth${path}`, {
    ...init,
    credentials: "include",
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const fields = body?.errors as Record<string, string[]> | undefined
    throw new Error(
      fields
        ? Object.values(fields).flat().join(" ")
        : body?.detail ?? body?.title ?? "Yêu cầu xác thực thất bại.",
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
  const response = await fetch(`/api/v1${path}`, {
    ...init,
    credentials: "include",
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const fields = body?.errors as Record<string, string[]> | undefined
    throw new Error(
      fields
        ? Object.values(fields).flat().join(" ")
        : body?.detail ?? body?.title ?? "Yêu cầu API thất bại.",
    )
  }
  return response.status === 204 ? (undefined as T) : response.json()
}
const mapVehicle = (vehicle: VehicleResponse): Vehicle => ({
  id: vehicle.id,
  name: vehicle.name,
  plate: vehicle.plate,
  type: vehicle.type === "Car" ? "Ô tô" : "Xe máy",
  year: vehicle.year,
  baseOdo: vehicle.baseOdo,
  currentOdo: vehicle.currentOdo,
  image: vehicle.imageUrl || defaultImage(vehicle.type === "Car" ? "Ô tô" : "Xe máy"),
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
    type: "Ô tô" | "Xe máy"
    year: number
    baseOdo: number
    image?: string
  }) {
    const vehicle = await apiRequest<VehicleResponse>("/vehicles", {
      method: "POST",
      body: JSON.stringify({
        ...request,
        type: request.type === "Ô tô" ? "Car" : "Motorcycle",
        imageUrl: request.image ?? "",
      }),
    })
    return mapVehicle(vehicle)
  },
  async updateVehicle(id: string, request: {
    name: string
    plate: string
    type: "Ô tô" | "Xe máy"
    year: number
    baseOdo: number
    image?: string
    version: number
  }) {
    const vehicle = await apiRequest<VehicleResponse>(`/vehicles/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        ...request,
        type: request.type === "Ô tô" ? "Car" : "Motorcycle",
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
export const odoOf = (v: Vehicle, entries: Entry[]) =>
  Math.max(v.baseOdo, v.currentOdo, ...entries.filter((e) => e.vehicleId === v.id).map((e) => e.odo))
export function validateEntry(data: Garage, entry: Entry) {
  const vehicle = data.vehicles.find((v) => v.id === entry.vehicleId)
  if (!vehicle) throw new Error("Không tìm thấy xe đã chọn.")
  if (!Number.isFinite(entry.odo) || entry.odo < 0) throw new Error("ODO phải là số không âm.")
  if (!Number.isFinite(entry.cost) || !Number.isFinite(entry.liters)) throw new Error("Chi phí và số lít phải là số hợp lệ.")
  if (entry.date > today()) throw new Error("Ngày ghi nhận không được ở tương lai.")
}
export function planState(plan: Plan, data: Garage) {
  const vehicle = data.vehicles.find((v) => v.id === plan.vehicleId)
  const km = plan.dueOdo - (vehicle ? odoOf(vehicle, data.entries) : 0)
  const days = new Date(`${plan.dueDate}T00:00:00+07:00`).getTime() -
    new Date(`${today()}T00:00:00+07:00`).getTime()
  const dayCount = Math.round(days / 86400000)
  const status = dayCount < 0 || km < 0 ? "overdue" :
    dayCount <= data.settings.thresholdDays || km <= data.settings.thresholdKm ? "soon" : "good"
  return {
    dueDate: plan.dueDate,
    dueOdo: plan.dueOdo,
    km,
    days: dayCount,
    status,
    service: undefined as Entry | undefined,
  }
}
