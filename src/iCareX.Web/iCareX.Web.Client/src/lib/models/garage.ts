import type { AccountResponse } from "@shared/auth-contracts"

export type Vehicle = {
  id: string
  name: string
  plate: string
  type: string
  vehicleTypeId: string
  vehicleBrandId: string
  vehicleBrandName: string
  vehicleModelId: string
  vehicleModelName: string
  year: number | null
  baseOdo: number
  registrationName?: string | null
  fuelType: string
  fuelCapacity?: number | null
  odometerUnit: string
  vin?: string | null
  registrationId?: string | null
  purchasePrice?: number | null
  salePrice?: number | null
  createdAtUtc?: string
  updatedAtUtc?: string
  notes?: string | null
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
  maintenanceAvailable?: boolean
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
  ["Ô tô", "Xe tải", "Xe buýt", "Xe van"].includes(type) ? carImage
    : type === "Xe máy" ? bikeImage : `${assetBase}brand/brand-symbol-light.svg`

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
  return { dueDate: plan.dueDate, dueOdo: plan.dueOdo, km, days: dayCount, status, service: undefined as Entry | undefined }
}

export const odometerUnitLabel = (v?: Pick<Vehicle, "odometerUnit">) =>
  v?.odometerUnit === "Miles" ? "mi" : v?.odometerUnit === "Hours" ? "giờ" : "km"

export const distanceInKilometres = (value: number, unit: string) =>
  unit === "Hours" ? 0 : unit === "Miles" ? value * 1.609344 : value
