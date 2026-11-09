import type {
  CurrentUserPermissionsResponse,
  VehiclePermissionsResponse,
} from "@shared/vehicle-contracts"
import type { Account, Garage } from "./garage.ts"

export type VehicleRole = "Owner" | "Manager" | "Contributor" | "Viewer"
export type ResourceAuthorship = {
  createdByUserId?: string
  assignedToUserId?: string
}
export type AuthUser = {
  id: string
  email: string
  fullName: string
  accountStatus: string
  emailVerified: boolean
}
export type VehiclePermissionContext = {
  vehicleId: string
  role: VehicleRole
  permissions: string[]
}
const roles: readonly string[] = ["Owner", "Manager", "Contributor", "Viewer"]

export function vehicleAccess(
  snapshot: CurrentUserPermissionsResponse | undefined,
  userId: string | undefined,
  vehicleId: string,
): VehiclePermissionsResponse | undefined {
  if (!userId || snapshot?.userId !== userId) return undefined
  return snapshot.vehicles.find(
    (vehicle) =>
      vehicle.vehicleId === vehicleId && roles.includes(vehicle.role),
  )
}

export function canPermission(
  snapshot: CurrentUserPermissionsResponse | undefined,
  userId: string | undefined,
  permission: string,
  vehicleId: string,
  resource?: ResourceAuthorship,
): boolean {
  const access = vehicleAccess(snapshot, userId, vehicleId)
  if (!access) return false
  const grant = access.permissions.find((item) => item.code === permission)
  if (!grant) return false
  switch (grant.scope) {
    case "all":
      return true
    case "own":
      return !!userId && resource?.createdByUserId === userId
    case "ownOrAssigned":
      return (
        !!userId &&
        (resource?.createdByUserId === userId ||
          resource?.assignedToUserId === userId)
      )
    default:
      return false
  }
}

export function accessibleGarage(
  data: Garage | undefined,
  snapshot: CurrentUserPermissionsResponse | undefined,
  userId: string | undefined,
): Garage | undefined {
  if (!data || !userId || snapshot?.userId !== userId) return undefined
  const readable = new Set(
    snapshot.vehicles
      .filter((item) =>
        canPermission(snapshot, userId, "vehicle.read", item.vehicleId),
      )
      .map((item) => item.vehicleId),
  )
  return {
    ...data,
    vehicles: data.vehicles.filter((item) => readable.has(item.id)),
    entries: data.entries.filter((item) => readable.has(item.vehicleId) && canPermission(snapshot, userId,
      item.kind === "fuel" ? "fuel.read" : item.kind === "service" ? "maintenance.read" : "vehicle.read", item.vehicleId)),
    plans: data.plans.filter((item) => readable.has(item.vehicleId) && canPermission(snapshot, userId, "maintenance_plan.read", item.vehicleId)),
  }
}

export function withoutVehicle(
  snapshot: CurrentUserPermissionsResponse | undefined,
  vehicleId: string,
): CurrentUserPermissionsResponse | undefined {
  return (
    snapshot && {
      ...snapshot,
      vehicles: snapshot.vehicles.filter(
        (item) => item.vehicleId !== vehicleId,
      ),
    }
  )
}

export function toAuthUser(account: Account | null): AuthUser | null {
  if (!account) return null
  return {
    id: account.id,
    email: account.email,
    fullName: account.name,
    accountStatus: account.accountStatus,
    emailVerified: account.emailVerified,
  }
}
