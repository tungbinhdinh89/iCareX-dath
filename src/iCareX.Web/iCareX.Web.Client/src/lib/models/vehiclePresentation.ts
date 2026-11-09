import { odoOf, today, type Garage, type Vehicle } from './garage.ts'

export type VehicleTab = 'all' | 'owned' | 'shared' | 'archived'
export const vehicleRoleLabels: Record<string, string> = {
  Owner: 'Chủ sở hữu', Manager: 'Quản lý', Contributor: 'Người đóng góp', Viewer: 'Người xem',
}
export const fuelTypeLabels: Record<string, string> = {
  Gasoline: 'Xăng', Diesel: 'Dầu diesel', Electric: 'Điện', Hybrid: 'Hybrid', PlugInHybrid: 'Plug-in hybrid',
  Ethanol: 'Ethanol', Biodiesel: 'Biodiesel', LPG: 'LPG', CNG: 'CNG', Hydrogen: 'Hydrogen', Other: 'Khác',
}
export function matchesVehicleTab(vehicle: Pick<Vehicle, 'isArchived'>, role: string | undefined, tab: VehicleTab) {
  if (!role || !Object.prototype.hasOwnProperty.call(vehicleRoleLabels, role)) return false
  if (tab === 'archived') return vehicle.isArchived
  if (vehicle.isArchived) return false
  return tab === 'all' || (tab === 'owned' ? role === 'Owner' : role !== 'Owner')
}

export type MaintenanceSummary = {
  status: 'AllUpToDate' | 'Upcoming' | 'Due' | 'Overdue' | 'NoSchedule'
  label: string
  title?: string
  remaining?: number
}

// Availability must come from an actual integrated data source, never from a Vehicle flag/status.
export function vehicleMaintenanceSummary(data: Garage, vehicle: Vehicle): MaintenanceSummary | null {
  if (!data.maintenanceAvailable || vehicle.isArchived) return null
  const plans = data.plans.filter((plan) => plan.vehicleId === vehicle.id)
  if (!plans.length) return { status: 'NoSchedule', label: 'Chưa thiết lập lịch bảo dưỡng' }
  const current = odoOf(vehicle, data.entries)
  const schedules = plans.map((plan) => {
    const remaining = plan.dueOdo - current
    const days = Math.round((Date.parse(`${plan.dueDate}T00:00:00+07:00`) - Date.parse(`${today()}T00:00:00+07:00`)) / 86400000)
    const threshold = vehicle.odometerUnit === 'Miles' ? data.settings.thresholdKm / 1.609344 : vehicle.odometerUnit === 'Hours' ? 0 : data.settings.thresholdKm
    const status = remaining < 0 || days < 0 ? 'Overdue' : remaining === 0 || days === 0 ? 'Due'
      : remaining <= threshold || days <= data.settings.thresholdDays ? 'Upcoming' : 'AllUpToDate'
    return { status, title: plan.title, remaining, days } as const
  })
  const rank = { Overdue: 0, Due: 1, Upcoming: 2, AllUpToDate: 3 }
  const next = schedules.sort((a, b) => rank[a.status] - rank[b.status] || a.remaining - b.remaining)[0]
  const labels = { AllUpToDate: 'Chưa đến hạn', Upcoming: 'Sắp đến lịch', Due: 'Đến hạn', Overdue: 'Quá hạn' }
  return { status: next.status, label: labels[next.status], title: next.title, remaining: next.remaining }
}

export function vehicleFormDefaults(vehicle?: Vehicle) {
  return {
      name: vehicle?.registrationName ?? "", plate: vehicle?.plate ?? "",
      vehicleTypeId: vehicle?.vehicleTypeId ?? "", vehicleBrandId: vehicle?.vehicleBrandId ?? "",
      vehicleModelId: vehicle?.vehicleModelId ?? "", year: vehicle?.year ?? null, baseOdo: vehicle?.baseOdo ?? 0,
      fuelType: vehicle?.fuelType ?? "", odometerUnit: (vehicle?.odometerUnit as "Kilometres" | "Miles" | "Hours") ?? "Kilometres",
      fuelCapacity: vehicle?.fuelCapacity ?? null,
      purchasePrice: vehicle?.purchasePrice ?? null, salePrice: vehicle?.salePrice ?? null,
       vin: vehicle?.vin ?? "", registrationId: vehicle?.registrationId ?? "",
      notes: vehicle?.notes ?? "", imageUrl: vehicle?.image ?? "",
    }
}
