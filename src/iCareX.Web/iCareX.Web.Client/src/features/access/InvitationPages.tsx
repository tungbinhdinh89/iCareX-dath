import { useEffect, useRef, useState } from "react"
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from "@mui/material"
import { useQuery } from "@tanstack/react-query"
import { Link, useParams } from "react-router"
import type { VehicleInvitationResponse } from "@shared/vehicle-contracts"
import { AuthShell } from "@/components/AuthenticationPage"
import AuthForm from "@/components/AuthForm"
import { api, ApiError } from "@/lib/apis/iCareXApi"
import { useGarage } from "@/lib/state/store"
import { useSecretFragment } from "@/features/auth/AuthPages"
import { roleLabels } from "@/features/access/MembersPage"

const invitationError = (error: unknown) =>
  error instanceof ApiError && error.status === 410
    ? "Lời mời đã hết hạn. Hãy đề nghị chủ xe gửi một lời mời mới."
    : error instanceof ApiError && error.status === 403
      ? "Email tài khoản chưa khớp với email được mời. Hãy dùng đúng tài khoản nhận lời mời."
      : error instanceof ApiError && error.status === 409
        ? "Lời mời đã được xử lý hoặc phương tiện không còn nhận thành viên."
        : error instanceof ApiError && error.status === 404
          ? "Không tìm thấy lời mời hợp lệ. Hãy kiểm tra lại liên kết trong email."
          : error instanceof Error
            ? error.message
            : "Không thể mở lời mời. Vui lòng thử lại."

export function InvitationInboxPage() {
  const { session } = useGarage()
  const query = useQuery({
    queryKey: ["invitations", session?.id],
    queryFn: ({ signal }) => api.invitationInbox(signal),
    enabled: !!session?.emailVerified,
    retry: false,
    refetchOnWindowFocus: true,
  })
  return (
    <Box className="route-fade">
      <Stack spacing={3}>
        <Box className="page-heading">
          <Box>
            <Typography variant="h4" component="h1">
              Lời mời chia sẻ xe
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Những hành trình bạn được mời đồng hành.
            </Typography>
          </Box>
        </Box>
        <Box className="panel">
          <Stack spacing={3}>
            <Alert severity="info">
              Mở liên kết trong email để xem và phản hồi lời mời an toàn.
            </Alert>
            {query.isPending ? (
              <CircularProgress size={24} />
            ) : query.error ? (
              <Alert
                severity="error"
                action={
                  <Button onClick={() => void query.refetch()}>Thử lại</Button>
                }
              >
                Không thể tải lời mời.
              </Alert>
            ) : !query.data?.length ? (
              <Typography color="text.secondary">
                Bạn chưa có lời mời đang chờ.
              </Typography>
            ) : (
              query.data.map((invite) => (
                <Box className="member-row" key={invite.id}>
                  <Box>
                    <Typography variant="subtitle1">
                      {invite.vehicleName}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {roleLabels[invite.role] ?? invite.role} · Hết hạn{" "}
                      {new Date(invite.expiresAtUtc).toLocaleDateString(
                        "vi-VN",
                      )}
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Kiểm tra email nhận lời mời
                  </Typography>
                </Box>
              ))
            )}
          </Stack>
        </Box>
      </Stack>
    </Box>
  )
}

export default function InvitationPage() {
  const { id = "" } = useParams()
  const secret = useSecretFragment()
  const { session, sessionStatus, login, logout, refreshPermissions } =
    useGarage()
  const [invite, setInvite] = useState<VehicleInvitationResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")
  const [notice, setNotice] = useState("")
  const [pending, setPending] = useState(false)
  const [processed, setProcessed] = useState<"accepted" | "rejected" | null>(
    null,
  )
  const request = useRef(0)
  const preview = async () => {
    if (!secret?.token) return
    const sequence = ++request.current
    setLoading(true)
    setError("")
    try {
      const result = await api.previewInvitation(id, secret.token)
      if (sequence === request.current) setInvite(result)
    } catch (error) {
      if (sequence === request.current) setError(invitationError(error))
    } finally {
      if (sequence === request.current) setLoading(false)
    }
  }
  useEffect(() => {
    void preview()
    return () => {
      request.current += 1
    }
  }, [id, secret?.token])
  const respond = async (accept: boolean) => {
    if (!secret?.token || pending) return
    setPending(true)
    setError("")
    try {
      if (accept) {
        await api.acceptInvitation(id, secret.token)
        await refreshPermissions()
      } else await api.rejectInvitation(id, secret.token)
      setProcessed(accept ? "accepted" : "rejected")
    } catch (error) {
      setError(invitationError(error))
    } finally {
      setPending(false)
    }
  }
  return (
    <AuthShell>
      <Box className="auth-content">
        <Stack spacing={3}>
          <Box>
            <Typography variant="h5" component="h1">
              Cùng chăm sóc một hành trình
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Kiểm tra phương tiện và vai trò trước khi phản hồi lời mời.
            </Typography>
          </Box>
          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}
          {notice && <Alert severity="info">{notice}</Alert>}
          {!secret || loading || sessionStatus === "checking" ? (
            <CircularProgress size={28} aria-label="Đang tải lời mời" />
          ) : !secret.token ? (
            <Alert severity="warning">
              Liên kết lời mời chưa đầy đủ. Hãy mở lại email chứa lời mời.
            </Alert>
          ) : processed ? (
            <>
              <Alert severity="success">
                {processed === "accepted"
                  ? "Bạn đã chấp nhận lời mời chia sẻ xe."
                  : "Bạn đã từ chối lời mời."}
              </Alert>
              <Button
                variant="contained"
                component={Link}
                to={
                  processed === "accepted" && invite
                    ? `/xe/${invite.vehicleId}`
                    : "/"
                }
              >
                {processed === "accepted"
                  ? "Mở phương tiện"
                  : "Về trang tổng quan"}
              </Button>
            </>
          ) : !invite ? (
            <Button onClick={() => void preview()} disabled={loading}>
              Thử mở lại lời mời
            </Button>
          ) : (
            <>
              <Box className="panel" sx={{ bgcolor: "background.default" }}>
                <Typography variant="h6">{invite.vehicleName}</Typography>
                <Typography sx={{ mt: 1 }}>
                  Vai trò: {roleLabels[invite.role] ?? invite.role}
                </Typography>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ mt: 1 }}
                >
                  Hết hạn:{" "}
                  {new Date(invite.expiresAtUtc).toLocaleDateString("vi-VN")}
                </Typography>
              </Box>
              {!session ? (
                <>
                  <Alert severity="info">
                    Đăng nhập bằng email nhận lời mời để tiếp tục. Liên kết được
                    giữ riêng trong trang này.
                  </Alert>
                  <AuthForm />
                </>
              ) : !session.emailVerified ? (
                <>
                  <Alert severity="warning">
                    Xác thực email tài khoản trước khi phản hồi lời mời.
                  </Alert>
                  <Button
                    variant="contained"
                    disabled={pending}
                    onClick={async () => {
                      setPending(true)
                      setError("")
                      try {
                        await api.requestVerification(session.email)
                        setNotice(
                          "Hãy kiểm tra email xác thực, sau đó quay lại trang này.",
                        )
                      } catch (error) {
                        setError(invitationError(error))
                      } finally {
                        setPending(false)
                      }
                    }}
                  >
                    Gửi email xác thực
                  </Button>
                  <Button
                    disabled={pending}
                    onClick={async () => {
                      setPending(true)
                      setError("")
                      try {
                        const account = await api.me()
                        if (account.emailVerified) login(account)
                        else setError("Email chưa được xác thực.")
                      } catch (error) {
                        setError(invitationError(error))
                      } finally {
                        setPending(false)
                      }
                    }}
                  >
                    Tôi đã xác thực, kiểm tra lại
                  </Button>
                </>
              ) : (
                <>
                  <Typography variant="body2" color="text.secondary">
                    Bạn đang dùng tài khoản {session.email}.
                  </Typography>
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                    <Button
                      variant="outlined"
                      disabled={pending}
                      onClick={() => void respond(false)}
                    >
                      Từ chối lời mời
                    </Button>
                    <Button
                      variant="contained"
                      disabled={pending}
                      onClick={() => void respond(true)}
                    >
                      {pending ? "Đang xử lý…" : "Chấp nhận lời mời"}
                    </Button>
                  </Stack>
                </>
              )}
              {session && (
                <Button disabled={pending} onClick={() => void logout()}>
                  Dùng tài khoản khác
                </Button>
              )}
            </>
          )}
        </Stack>
      </Box>
    </AuthShell>
  )
}
