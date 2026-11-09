import type { ReactNode } from "react"
import { Alert, Box, Button, CircularProgress } from "@mui/material"
import { Link, Outlet, useParams } from "react-router"
import { useGarage } from "@/lib/state/store"

export function RequireVehicleAccess({
  permission = "vehicle.read",
}: {
  permission?: string
}) {
  const { id } = useParams()
  const {
    permissionsLoading,
    permissionsError,
    refreshPermissions,
    canAccessVehicle,
    can,
  } = useGarage()
  if (permissionsLoading)
    return (
      <Box role="status" aria-label="Đang kiểm tra quyền truy cập">
        <CircularProgress size={24} />
      </Box>
    )
  if (permissionsError)
    return (
      <Alert
        severity="error"
        action={
          <Button onClick={() => void refreshPermissions()}>Thử lại</Button>
        }
      >
        Không thể kiểm tra quyền truy cập.
      </Alert>
    )
  if (!id || !canAccessVehicle(id))
    return (
      <Box className="panel">
        <Alert severity="warning">
          Phương tiện không còn khả dụng hoặc bạn không có quyền truy cập.
        </Alert>
        <Button component={Link} to="/xe" sx={{ mt: 2 }}>
          Về danh sách xe
        </Button>
      </Box>
    )
  if (!can(permission, id))
    return (
      <Alert severity="info">
        Bạn không có quyền mở mục này trên phương tiện.
      </Alert>
    )
  return <Outlet />
}

export function RequirePermission({
  permission,
  children,
}: {
  permission: string
  children: ReactNode
}) {
  const { can } = useGarage()
  return can(permission) ? (
    children
  ) : (
    <Alert severity="info">
      Mục này chưa khả dụng với quyền truy cập hiện tại.
    </Alert>
  )
}

export default function Permission({
  permission,
  vehicleId,
  children,
}: {
  permission: string
  vehicleId?: string
  children: ReactNode
}) {
  const { can } = useGarage()
  return can(permission, vehicleId) ? children : null
}
