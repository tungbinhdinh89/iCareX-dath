export type Vehicle = {
  id: string
  name: string
  plate: string
  type: "Ô tô" | "Xe máy"
  year: number
  baseOdo: number
  image: string
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
  profile: {
    name: string
    email: string
    phone: string
  }
  settings: {
    reminders: boolean
    thresholdKm: number
    thresholdDays: number
  }
}
export type Account = AccountResponse

export const accounts: Account[] = [
  { id: "minhanh", name: "Nguyễn Minh Anh", email: "minhanh@vehiclecare.vn", phone: "", roles: ["User"] },
  { id: "hoangnam", name: "Trần Hoàng Nam", email: "hoangnam@vehiclecare.vn", phone: "", roles: ["User"] },
]
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
export function dateAt(days: number, months = 0) {
  const d = new Date(`${today()}T12:00:00+07:00`)
  d.setUTCMonth(d.getUTCMonth() + months)
  d.setUTCDate(d.getUTCDate() + days)
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Ho_Chi_Minh",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(d)
}
const assetBase = import.meta.env?.BASE_URL ?? "/"
const carImage = `${assetBase}images/car.jpg`
const bikeImage = `${assetBase}images/scooter.jpg`
export const defaultImage = (type: string) =>
  type === "Ô tô" ? carImage : bikeImage
function seed(account: Account): Garage {
  const second = account.id === "hoangnam"
  const vehicles: Vehicle[] = second
    ? [
        {
          id: "v3",
          name: "Mazda 3",
          plate: "51H-678.90",
          type: "Ô tô",
          year: 2022,
          baseOdo: 15000,
          image: carImage,
        },
      ]
    : [
        {
          id: "v1",
          name: "Toyota Vios",
          plate: "51G-123.45",
          type: "Ô tô",
          year: 2021,
          baseOdo: 30000,
          image: carImage,
        },
        {
          id: "v2",
          name: "Honda Air Blade",
          plate: "59P1-456.78",
          type: "Xe máy",
          year: 2023,
          baseOdo: 12000,
          image: bikeImage,
        },
      ]
  const entries: Entry[] = []
  vehicles.forEach((v, vi) => {
    for (let i = 0; i < 24; i++) {
      const kind = i % 4 === 2 ? "service" : "fuel"
      const isCar = v.type === "Ô tô"
      entries.push({
        id: `${v.id}-e${i}`,
        vehicleId: v.id,
        kind,
        title:
          kind === "service"
            ? [
                "Thay dầu động cơ",
                "Bảo dưỡng định kỳ",
                "Kiểm tra hệ thống phanh",
              ][Math.floor(i / 4) % 3]
            : "Đổ xăng RON 95",
        date: dateAt(-161 + i * 7),
        odo: v.baseOdo + (i + 1) * (isCar ? 510 : 160),
        cost:
          kind === "service"
            ? isCar
              ? 650000 + (i % 3) * 180000
              : 150000 + (i % 3) * 50000
            : isCar
              ? 650000 + (i % 4) * 65000
              : 80000 + (i % 3) * 20000,
        liters:
          kind === "fuel"
            ? isCar
              ? 28 + (i % 4) * 3
              : 3.5 + (i % 3) * 0.8
            : 0,
        note:
          kind === "service"
            ? isCar
              ? "Toyota An Thành"
              : "HEAD Honda Phát Tiến"
            : "Petrolimex · RON 95",
      })
    }
  })
  const plans: Plan[] = [
    {
      id: "p1",
      vehicleId: vehicles[0].id,
      title: "Thay dầu động cơ",
      dueDate: dateAt(-3),
      dueOdo: vehicles[0].baseOdo + 12000,
      intervalKm: 5000,
      intervalMonths: 6,
    },
    {
      id: "p2",
      vehicleId: vehicles[0].id,
      title: "Kiểm tra lốp & đảo lốp",
      dueDate: dateAt(6),
      dueOdo: vehicles[0].baseOdo + 12500,
      intervalKm: 10000,
      intervalMonths: 6,
    },
    {
      id: "p3",
      vehicleId: vehicles[1]?.id ?? vehicles[0].id,
      title: "Bảo dưỡng định kỳ",
      dueDate: dateAt(15),
      dueOdo:
        (vehicles[1]?.baseOdo ?? vehicles[0].baseOdo) +
        (vehicles[1] ? 4100 : 15000),
      intervalKm: 3000,
      intervalMonths: 3,
    },
  ]
  return {
    vehicles,
    entries,
    plans,
    profile: {
      name: account.name,
      email: account.email,
      phone: second ? "090 123 4567" : "093 456 7890",
    },
    settings: { reminders: true, thresholdKm: 500, thresholdDays: 30 },
  }
}
const stores: Record<string, Garage> = Object.fromEntries(
  accounts.map((a) => [a.id, seed(a)]),
)
const copy = <T>(value: T): T => structuredClone(value)
const delay = () => new Promise((resolve) => setTimeout(resolve, 350))
let csrfToken = ""
async function authRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/v1/auth${path}`, {
    ...init,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const fields = body?.errors as Record<string, string[]> | undefined
    const message = fields
      ? Object.values(fields).flat().join(" ")
      : body?.detail ?? body?.title
    throw new Error(message || "Yêu cầu xác thực thất bại.")
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
  async get(id: string | null) {
    await delay()
    const account = id
      ? { id, name: "Tài khoản VehicleCare", email: "", phone: "", roles: ["User"] }
      : accounts[0]
    if (!stores[account.id]) stores[account.id] = seed(account)
    return copy(stores[account.id])
  },
  async update(id: string, action: (data: Garage) => void) {
    await delay()
    if (!stores[id])
      throw new Error("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.")
    const next = copy(stores[id])
    action(next)
    stores[id] = next
    return copy(next)
  },
}
export const odoOf = (v: Vehicle, entries: Entry[]) =>
  Math.max(
    v.baseOdo,
    ...entries.filter((e) => e.vehicleId === v.id).map((e) => e.odo),
  )
export function validateEntry(data: Garage, entry: Entry) {
  const vehicle = data.vehicles.find((v) => v.id === entry.vehicleId)
  if (!vehicle) throw new Error("Không tìm thấy xe đã chọn.")
  if (!Number.isFinite(entry.odo) || entry.odo < 0)
    throw new Error("ODO phải là số không âm.")
  if (!Number.isFinite(entry.cost) || !Number.isFinite(entry.liters))
    throw new Error("Chi phí và số lít phải là số hợp lệ.")
  if (entry.date > today())
    throw new Error("Ngày ghi nhận không được ở tương lai.")
  const history = data.entries.filter(
    (e) => e.vehicleId === entry.vehicleId && e.id !== entry.id,
  )
  // Entries are day-granular; several trips on the same day may have different ODOs.
  const before = Math.max(
    vehicle.baseOdo,
    ...history.filter((e) => e.date < entry.date).map((e) => e.odo),
  )
  const after = Math.min(
    ...history.filter((e) => e.date > entry.date).map((e) => e.odo),
  )
  if (entry.odo < before || entry.odo > after)
    throw new Error(
      `ODO phải từ ${number(before)}${
        Number.isFinite(after) ? ` đến ${number(after)}` : " trở lên"
      } km để nhất quán với lịch sử.`,
    )
  if (entry.kind !== "odo" && entry.cost <= 0)
    throw new Error("Chi phí phải lớn hơn 0 VNĐ.")
  if (entry.kind === "fuel" && entry.liters <= 0)
    throw new Error("Số lít phải lớn hơn 0.")
}
export function planState(plan: Plan, data: Garage) {
  const service = data.entries
    .filter((e) => e.planId === plan.id)
    .sort((a, b) => b.date.localeCompare(a.date) || b.odo - a.odo)[0]
  let dueDate = plan.dueDate,
    dueOdo = plan.dueOdo
  if (service) {
    const d = new Date(`${service.date}T12:00:00+07:00`)
    const day = d.getUTCDate()
    d.setUTCDate(1)
    d.setUTCMonth(d.getUTCMonth() + plan.intervalMonths)
    const lastDay = new Date(
      Date.UTC(d.getUTCFullYear(), d.getUTCMonth() + 1, 0),
    ).getUTCDate()
    d.setUTCDate(Math.min(day, lastDay))
    dueDate = d.toISOString().slice(0, 10)
    dueOdo = service.odo + plan.intervalKm
  }
  const vehicle = data.vehicles.find((v) => v.id === plan.vehicleId)
  const km = dueOdo - (vehicle ? odoOf(vehicle, data.entries) : 0)
  const days = Math.round(
    (new Date(`${dueDate}T00:00:00+07:00`).getTime() -
      new Date(`${today()}T00:00:00+07:00`).getTime()) /
      86400000,
  )
  const status =
    days < 0 || km < 0
      ? "overdue"
      : days <= data.settings.thresholdDays || km <= data.settings.thresholdKm
        ? "soon"
        : "good"
  return { dueDate, dueOdo, km, days, status, service }
}
import type {
  AccountResponse,
  CsrfResponse,
  LoginRequest,
  RegisterRequest,
} from "../../shared/auth-contracts"
