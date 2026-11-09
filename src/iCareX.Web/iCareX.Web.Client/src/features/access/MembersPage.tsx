import { useState } from "react"
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material"
import { Link, useParams } from "react-router"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import type { VehicleMemberResponse } from "@shared/vehicle-contracts"
import { api } from "@/lib/apis/iCareXApi"
import { emailSchema } from "@/lib/models/auth"
import { useGarage } from "@/lib/state/store"

export const roleLabels: Record<string, string> = {
  Owner: "Chủ sở hữu",
  Manager: "Quản lý",
  Contributor: "Người đóng góp",
  Viewer: "Người xem",
}
const editableRoles = ["Manager", "Contributor", "Viewer"] as const
const roleSchema = z.object({ role: z.enum(editableRoles) })
const invitationSchema = z.object({
  email: emailSchema,
  role: z.enum(editableRoles),
})
const statusLabels: Record<string, string> = {
  Pending: "Đang chờ phản hồi",
  Accepted: "Đã chấp nhận",
  Rejected: "Đã từ chối",
  Revoked: "Đã thu hồi",
  Expired: "Đã hết hạn",
}
const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Không thể lưu thay đổi."

function EditMember({
  vehicleId,
  member,
  onClose,
  onSaved,
}: {
  vehicleId: string
  member: VehicleMemberResponse
  onClose: () => void
  onSaved: () => Promise<void>
}) {
  const [error, setError] = useState("")
  const { notify, can } = useGarage()
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof roleSchema>>({
    resolver: zodResolver(roleSchema),
    defaultValues: {
      role: editableRoles.includes(member.role as typeof editableRoles[number])
        ? member.role as typeof editableRoles[number]
        : "Viewer",
    },
  })
  return (
    <Dialog
      open
      onClose={() => !isSubmitting && onClose()}
      fullWidth
      maxWidth="xs"
      aria-labelledby="member-role-title"
    >
      <Box
        component="form"
        noValidate
        onSubmit={handleSubmit(async ({ role }) => {
          if (!can("vehicle.member.update", vehicleId)) return
          setError("")
          try {
            await api.updateVehicleMember(vehicleId, member.userId, {
              role,
              version: member.version,
            })
            await onSaved()
            notify("Đã cập nhật vai trò thành viên.")
            onClose()
          } catch (error) {
            setError(errorMessage(error))
          }
        })}
      >
        <DialogTitle id="member-role-title">
          Đổi vai trò của {member.name}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={3} sx={{ pt: 1 }}>
            {error && (
              <Alert severity="error" role="alert">
                {error}
              </Alert>
            )}
            <TextField
              select
              label="Vai trò"
              defaultValue={member.role}
              {...register("role")}
              error={!!errors.role}
              helperText={errors.role?.message}
              disabled={isSubmitting}
            >
              {editableRoles.map((role) => (
                <MenuItem key={role} value={role}>
                  {roleLabels[role]}
                </MenuItem>
              ))}
            </TextField>
            <Typography variant="body2" color="text.secondary">
              Vai trò được áp dụng riêng cho phương tiện này.
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button disabled={isSubmitting} onClick={onClose}>
            Hủy
          </Button>
          <Button variant="contained" type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang lưu…" : "Lưu vai trò"}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

function InvitationsSection({ vehicleId }: { vehicleId: string }) {
  const { session, notify, can } = useGarage()
  const client = useQueryClient()
  const [error, setError] = useState("")
  const query = useQuery({
    queryKey: ["vehicle-invitations", session?.id, vehicleId],
    queryFn: ({ signal }) => api.vehicleInvitations(vehicleId, signal),
    enabled: can("vehicle.member.invite", vehicleId),
    retry: false,
  })
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof invitationSchema>>({
    resolver: zodResolver(invitationSchema),
    defaultValues: { email: "", role: "Viewer" },
  })
  const revoke = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) =>
      api.revokeVehicleInvitation(vehicleId, id, version),
    onSuccess: async () => {
      await client.invalidateQueries({
        queryKey: ["vehicle-invitations", session?.id, vehicleId],
      })
      notify("Đã thu hồi lời mời.")
    },
    onError: (error) => setError(errorMessage(error)),
  })
  if (!can("vehicle.member.invite", vehicleId)) return null
  return (
    <Box className="panel">
      <Stack spacing={3}>
        <Box>
          <Typography variant="h6" component="h2">
            Mời người đồng hành
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Lời mời được gửi qua email. Người nhận cần đăng nhập bằng đúng email
            đã được mời.
          </Typography>
        </Box>
        {error && (
          <Alert severity="error" role="alert">
            {error}
          </Alert>
        )}
        <Box
          component="form"
          noValidate
          onSubmit={handleSubmit(async (values) => {
            if (!can("vehicle.member.invite", vehicleId)) return
            setError("")
            try {
              await api.createVehicleInvitation(vehicleId, values)
              reset()
              await query.refetch()
              notify("Đã gửi lời mời qua email.")
            } catch (error) {
              setError(errorMessage(error))
            }
          })}
        >
          <Stack
            spacing={3}
            direction={{ xs: "column", sm: "row" }}
            alignItems={{ sm: "flex-start" }}
          >
            <TextField
              label="Email người nhận"
              type="email"
              fullWidth
              required
              {...register("email")}
              error={!!errors.email}
              helperText={errors.email?.message}
              disabled={isSubmitting}
            />
            <TextField
              select
              label="Vai trò"
              defaultValue="Viewer"
              {...register("role")}
              error={!!errors.role}
              helperText={errors.role?.message}
              disabled={isSubmitting}
              sx={{ minWidth: 180 }}
            >
              {editableRoles.map((role) => (
                <MenuItem key={role} value={role}>
                  {roleLabels[role]}
                </MenuItem>
              ))}
            </TextField>
            <Button
              variant="contained"
              type="submit"
              disabled={isSubmitting}
              sx={{ flexShrink: 0 }}
            >
              {isSubmitting ? "Đang gửi…" : "Gửi lời mời"}
            </Button>
          </Stack>
        </Box>
        {query.isPending ? (
          <CircularProgress size={24} aria-label="Đang tải lời mời" />
        ) : query.error ? (
          <Alert
            severity="error"
            action={
              <Button onClick={() => void query.refetch()}>Thử lại</Button>
            }
          >
            Không thể tải danh sách lời mời.
          </Alert>
        ) : !query.data?.length ? (
          <Typography color="text.secondary">Chưa có lời mời nào.</Typography>
        ) : (
          <Stack spacing={2}>
            {query.data.map((invite) => (
              <Box key={invite.id} className="member-row">
                <Box>
                  <Typography variant="subtitle2">
                    {roleLabels[invite.role] ?? invite.role}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {statusLabels[invite.status] ?? invite.status} · Hết hạn{" "}
                    {new Date(invite.expiresAtUtc).toLocaleDateString("vi-VN")}
                  </Typography>
                </Box>
                {invite.status === "Pending" && (
                  <Button
                    color="error"
                    disabled={revoke.isPending}
                    onClick={() =>
                      revoke.mutate({ id: invite.id, version: invite.version })
                    }
                  >
                    Thu hồi lời mời
                  </Button>
                )}
              </Box>
            ))}
          </Stack>
        )}
      </Stack>
    </Box>
  )
}

export default function MembersPage() {
  const { id = "" } = useParams()
  const { session, data, can, notify, refreshPermissions } = useGarage()
  const [editing, setEditing] = useState<VehicleMemberResponse | null>(null)
  const [removing, setRemoving] = useState<VehicleMemberResponse | null>(null)
  const [error, setError] = useState("")
  const query = useQuery({
    queryKey: ["vehicle-members", session?.id, id],
    queryFn: ({ signal }) => api.vehicleMembers(id, signal),
    enabled: can("vehicle.member.read", id),
    retry: false,
    refetchOnWindowFocus: true,
  })
  const saved = async () => {
    await query.refetch()
    await refreshPermissions()
  }
  const revoke = useMutation({
    mutationFn: (member: VehicleMemberResponse) =>
      api.revokeVehicleMember(id, member.userId, member.version),
    onSuccess: async () => {
      setRemoving(null)
      await saved()
      notify("Đã thu hồi quyền truy cập. Lịch sử của thành viên được giữ lại.")
    },
    onError: (error) => setError(errorMessage(error)),
  })
  const vehicle = data?.vehicles.find((item) => item.id === id)
  return (
    <Box className="route-fade">
      <Stack spacing={3}>
        <Box className="page-heading">
          <Box>
            <Button component={Link} to={`/xe/${id}`} sx={{ mb: 1 }}>
              Về hồ sơ xe
            </Button>
            <Typography variant="h4" component="h1">
              Thành viên phương tiện
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              {vehicle?.name} · {vehicle?.plate}
            </Typography>
          </Box>
        </Box>
        <Box className="panel">
          <Stack spacing={3}>
            <Box>
              <Typography variant="h6" component="h2">
                Cùng chăm sóc, đúng vai trò
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Chỉ chủ sở hữu được thay đổi vai trò và thu hồi quyền truy cập.
              </Typography>
            </Box>
            {error && (
              <Alert severity="error" role="alert">
                {error}
              </Alert>
            )}
            {query.isPending ? (
              <CircularProgress size={24} aria-label="Đang tải thành viên" />
            ) : query.error ? (
              <Alert
                severity="error"
                action={
                  <Button onClick={() => void query.refetch()}>Thử lại</Button>
                }
              >
                Không thể tải thành viên.
              </Alert>
            ) : (
              <Stack spacing={2}>
                {query.data?.map((member) => (
                  <Box className="member-row" key={member.userId}>
                    <Box>
                      <Typography variant="subtitle1">
                        {member.name}
                        {member.userId === session?.id ? " (Bạn)" : ""}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Tham gia{" "}
                        {new Date(member.joinedAtUtc).toLocaleDateString(
                          "vi-VN",
                        )}
                      </Typography>
                    </Box>
                    <Box
                      sx={{
                        display: "flex",
                        alignItems: "center",
                        flexWrap: "wrap",
                        gap: 1,
                      }}
                    >
                      <Chip
                        label={roleLabels[member.role] ?? member.role}
                        variant="outlined"
                      />
                      {member.role !== "Owner" &&
                        can("vehicle.member.update", id) && (
                          <Button
                            size="small"
                            onClick={() => setEditing(member)}
                          >
                            Đổi vai trò
                          </Button>
                        )}
                      {member.role !== "Owner" &&
                        can("vehicle.member.remove", id) && (
                          <Button
                            size="small"
                            color="error"
                            onClick={() => {
                              setError("")
                              setRemoving(member)
                            }}
                          >
                            Thu hồi
                          </Button>
                        )}
                    </Box>
                  </Box>
                ))}
              </Stack>
            )}
          </Stack>
        </Box>
        <InvitationsSection vehicleId={id} />
        {editing && can("vehicle.member.update", id) && (
          <EditMember
            vehicleId={id}
            member={editing}
            onClose={() => setEditing(null)}
            onSaved={saved}
          />
        )}
        <Dialog
          open={!!removing && can("vehicle.member.remove", id)}
          onClose={() => !revoke.isPending && setRemoving(null)}
          fullWidth
          maxWidth="xs"
          aria-labelledby="revoke-member-title"
        >
          <DialogTitle id="revoke-member-title">
            Thu hồi quyền truy cập?
          </DialogTitle>
          <DialogContent>
            <Stack spacing={2}>
              {error && <Alert severity="error">{error}</Alert>}
              <Typography>
                {removing?.name} sẽ không còn được mở phương tiện này.
              </Typography>
              <Alert severity="info">
                Các bản ghi và tác giả trong lịch sử vẫn được giữ lại.
              </Alert>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button
              disabled={revoke.isPending}
              onClick={() => setRemoving(null)}
            >
              Hủy
            </Button>
            <Button
              variant="contained"
              color="error"
              disabled={revoke.isPending}
              onClick={() => removing && revoke.mutate(removing)}
            >
              {revoke.isPending ? "Đang thu hồi…" : "Thu hồi quyền"}
            </Button>
          </DialogActions>
        </Dialog>
      </Stack>
    </Box>
  )
}
