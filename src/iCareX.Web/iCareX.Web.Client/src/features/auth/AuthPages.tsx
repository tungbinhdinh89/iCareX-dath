import { useEffect, useRef, useState } from "react"
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from "@mui/material"
import { Link, useNavigate } from "react-router"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import { AuthShell } from "@/components/AuthenticationPage"
import { api } from "@/lib/apis/iCareXApi"
import {
  consumeSecretFragment,
  emailSchema,
  newPasswordSchema,
} from "@/lib/models/auth"
import { useGarage } from "@/lib/state/store"

export function useSecretFragment() {
  const captured = useRef(false)
  const [secret, setSecret] = useState<{ email: string; token: string } | null>(
    null,
  )
  useEffect(() => {
    if (captured.current) return
    captured.current = true
    setSecret(consumeSecretFragment())
  }, [])
  return secret
}

function message(error: unknown) {
  return error instanceof Error
    ? error.message
    : "Không thể thực hiện yêu cầu. Vui lòng thử lại."
}

export function VerifyEmailPage() {
  const secret = useSecretFragment()
  const { session, sessionStatus, login, logout } = useGarage()
  const [pending, setPending] = useState(false)
  const [error, setError] = useState("")
  const [notice, setNotice] = useState("")
  const [done, setDone] = useState(false)
  const {
    register,
    setValue,
    handleSubmit,
    formState: { errors },
  } = useForm<{ email: string }>({
    resolver: zodResolver(z.object({ email: emailSchema })),
    defaultValues: { email: "" },
  })
  useEffect(() => {
    setValue("email", secret?.email || session?.email || "")
  }, [secret?.email, session?.email, setValue])
  if (!secret || sessionStatus === "checking")
    return (
      <AuthShell>
        <Box className="auth-content">
          <CircularProgress size={28} aria-label="Đang kiểm tra xác thực" />
        </Box>
      </AuthShell>
    )
  return (
    <AuthShell>
      <Box className="auth-content">
        <Stack spacing={3}>
          <Box>
            <Typography variant="h5" component="h1">
              Xác thực email
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Xác nhận email để bảo vệ tài khoản và mở không gian chăm xe.
            </Typography>
          </Box>
          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}
          {notice && <Alert severity="success">{notice}</Alert>}
          {session?.emailVerified && session.accountStatus === "Active" ? (
            <>
              <Alert severity="success">Email của bạn đã được xác thực.</Alert>
              <Button variant="contained" component={Link} to="/">
                Mở không gian của tôi
              </Button>
            </>
          ) : done ? (
            <Button component={Link} to="/dang-nhap" variant="contained">
              Đăng nhập để tiếp tục
            </Button>
          ) : (
            <>
              {secret.token ? (
                <>
                  <Typography variant="body2">
                    Xác nhận địa chỉ {secret.email}.
                  </Typography>
                  <Button
                    variant="contained"
                    disabled={pending}
                    onClick={async () => {
                      setPending(true)
                      setError("")
                      setNotice("")
                      try {
                        await api.verifyEmail(secret)
                        setDone(true)
                        setNotice("Email đã được xác thực thành công.")
                        if (session) login(await api.me())
                      } catch (error) {
                        setError(message(error))
                      } finally {
                        setPending(false)
                      }
                    }}
                  >
                    {pending ? "Đang xác thực…" : "Xác thực email của tôi"}
                  </Button>
                </>
              ) : (
                <Alert severity="info">
                  Mở liên kết trong email xác thực để hoàn tất. Nếu chưa nhận
                  được, bạn có thể yêu cầu gửi lại bên dưới.
                </Alert>
              )}
              <Box
                component="form"
                noValidate
                onSubmit={handleSubmit(async ({ email }) => {
                  setPending(true)
                  setError("")
                  setNotice("")
                  try {
                    await api.requestVerification(email)
                    setNotice(
                      "Nếu địa chỉ cần xác thực, hướng dẫn đã được gửi. Hãy kiểm tra hộp thư và thư rác.",
                    )
                  } catch (error) {
                    setError(message(error))
                  } finally {
                    setPending(false)
                  }
                })}
              >
                <Stack spacing={2}>
                  <TextField
                    label="Email"
                    required
                    type="email"
                    {...register("email")}
                    error={!!errors.email}
                    helperText={errors.email?.message}
                    disabled={pending}
                  />
                  <Button variant="outlined" type="submit" disabled={pending}>
                    Gửi lại email xác thực
                  </Button>
                </Stack>
              </Box>
              {session && (
                <Button
                  disabled={pending}
                  onClick={async () => {
                    setPending(true)
                    setError("")
                    try {
                      const account = await api.me()
                      if (account.emailVerified) login(account)
                      else
                        setError(
                          "Email chưa được xác thực. Vui lòng mở liên kết trong hộp thư.",
                        )
                    } catch (error) {
                      setError(message(error))
                    } finally {
                      setPending(false)
                    }
                  }}
                >
                  Tôi đã xác thực, kiểm tra lại
                </Button>
              )}
            </>
          )}
          {session ? (
            <Button disabled={pending} onClick={() => void logout()}>
              Đăng xuất
            </Button>
          ) : (
            <Button component={Link} to="/dang-nhap">
              Về trang đăng nhập
            </Button>
          )}
        </Stack>
      </Box>
    </AuthShell>
  )
}

export function ForgotPasswordPage() {
  const [notice, setNotice] = useState("")
  const [error, setError] = useState("")
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<{ email: string }>({
    resolver: zodResolver(z.object({ email: emailSchema })),
    defaultValues: { email: "" },
  })
  return (
    <AuthShell>
      <Box
        className="auth-content"
        component="form"
        noValidate
        onSubmit={handleSubmit(async ({ email }) => {
          setError("")
          setNotice("")
          try {
            await api.forgotPassword(email)
            setNotice(
              "Nếu địa chỉ phù hợp với một tài khoản, hướng dẫn đặt lại mật khẩu đã được gửi. Hãy kiểm tra hộp thư và thư rác.",
            )
          } catch (error) {
            setError(message(error))
          }
        })}
      >
        <Stack spacing={3}>
          <Box>
            <Typography variant="h5" component="h1">
              Quên mật khẩu?
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Nhập email để nhận hướng dẫn đặt lại mật khẩu.
            </Typography>
          </Box>
          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}
          {notice && <Alert severity="success">{notice}</Alert>}
          <TextField
            label="Email"
            type="email"
            autoComplete="email"
            required
            {...register("email")}
            error={!!errors.email}
            helperText={errors.email?.message}
            disabled={isSubmitting}
          />
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {isSubmitting ? "Đang gửi…" : "Gửi hướng dẫn"}
          </Button>
          <Button component={Link} to="/dang-nhap">
            Về trang đăng nhập
          </Button>
        </Stack>
      </Box>
    </AuthShell>
  )
}

const resetSchema = z
  .object({ newPassword: newPasswordSchema, confirmPassword: z.string() })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "Mật khẩu xác nhận chưa khớp.",
  })

export function ResetPasswordPage() {
  const secret = useSecretFragment()
  const { session, logout } = useGarage()
  const [error, setError] = useState("")
  const [done, setDone] = useState(false)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof resetSchema>>({
    resolver: zodResolver(resetSchema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  })
  return (
    <AuthShell>
      <Box className="auth-content">
        <Stack spacing={3}>
          <Typography variant="h5" component="h1">
            Đặt lại mật khẩu
          </Typography>
          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}
          {!secret ? (
            <CircularProgress size={24} />
          ) : done ? (
            <>
              <Alert severity="success">
                Mật khẩu đã được cập nhật. Hãy đăng nhập lại trên các thiết bị
                của bạn.
              </Alert>
              <Button component={Link} to="/dang-nhap" variant="contained">
                Đăng nhập
              </Button>
            </>
          ) : !secret.email || !secret.token ? (
            <>
              <Alert severity="warning">
                Liên kết đặt lại mật khẩu chưa đầy đủ. Hãy yêu cầu một liên kết
                mới.
              </Alert>
              <Button component={Link} to="/quen-mat-khau">
                Gửi lại hướng dẫn
              </Button>
            </>
          ) : (
            <Box
              component="form"
              noValidate
              onSubmit={handleSubmit(async ({ newPassword }) => {
                setError("")
                try {
                  await api.resetPassword({ ...secret, newPassword })
                  reset()
                  setDone(true)
                  if (session) await logout()
                } catch (error) {
                  setError(message(error))
                }
              })}
            >
              <Stack spacing={3}>
                <Typography variant="body2" color="text.secondary">
                  Tạo mật khẩu mới cho {secret.email}.
                </Typography>
                <TextField
                  label="Mật khẩu mới"
                  type="password"
                  autoComplete="new-password"
                  required
                  {...register("newPassword")}
                  error={!!errors.newPassword}
                  helperText={
                    errors.newPassword?.message ??
                    "Ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt."
                  }
                  disabled={isSubmitting}
                />
                <TextField
                  label="Nhập lại mật khẩu mới"
                  type="password"
                  autoComplete="new-password"
                  required
                  {...register("confirmPassword")}
                  error={!!errors.confirmPassword}
                  helperText={errors.confirmPassword?.message}
                  disabled={isSubmitting}
                />
                <Button
                  variant="contained"
                  type="submit"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? "Đang cập nhật…" : "Lưu mật khẩu mới"}
                </Button>
              </Stack>
            </Box>
          )}
        </Stack>
      </Box>
    </AuthShell>
  )
}

const changeSchema = z
  .object({
    currentPassword: z.string().min(1, "Nhập mật khẩu hiện tại."),
    newPassword: newPasswordSchema,
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "Mật khẩu xác nhận chưa khớp.",
  })

export default function ChangePasswordPage() {
  const { logout, notify } = useGarage()
  const navigate = useNavigate()
  const [error, setError] = useState("")
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<z.infer<typeof changeSchema>>({
    resolver: zodResolver(changeSchema),
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      confirmPassword: "",
    },
  })
  return (
    <Box className="route-fade">
      <Box className="page-heading">
        <Box>
          <Typography variant="h4" component="h1">
            Đổi mật khẩu
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            Giữ tài khoản an toàn trên mọi hành trình.
          </Typography>
        </Box>
      </Box>
      <Box
        className="panel"
        sx={{ maxWidth: 600 }}
        component="form"
        noValidate
        onSubmit={handleSubmit(async ({ currentPassword, newPassword }) => {
          setError("")
          try {
            await api.changePassword({ currentPassword, newPassword })
            reset()
            await logout()
            notify("Đã đổi mật khẩu. Vui lòng đăng nhập lại.", "info")
            navigate("/dang-nhap", { replace: true })
          } catch (error) {
            setError(message(error))
          }
        })}
      >
        <Stack spacing={3}>
          <Alert severity="info">
            Sau khi đổi mật khẩu, các phiên đăng nhập sẽ kết thúc. Bạn cần đăng
            nhập lại.
          </Alert>
          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}
          <TextField
            label="Mật khẩu hiện tại"
            type="password"
            autoComplete="current-password"
            required
            {...register("currentPassword")}
            error={!!errors.currentPassword}
            helperText={errors.currentPassword?.message}
            disabled={isSubmitting}
          />
          <TextField
            label="Mật khẩu mới"
            type="password"
            autoComplete="new-password"
            required
            {...register("newPassword")}
            error={!!errors.newPassword}
            helperText={
              errors.newPassword?.message ??
              "Ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt."
            }
            disabled={isSubmitting}
          />
          <TextField
            label="Nhập lại mật khẩu mới"
            type="password"
            autoComplete="new-password"
            required
            {...register("confirmPassword")}
            error={!!errors.confirmPassword}
            helperText={errors.confirmPassword?.message}
            disabled={isSubmitting}
          />
          <Box sx={{ display: "flex", gap: 2 }}>
            <Button component={Link} to="/ho-so" disabled={isSubmitting}>
              Hủy
            </Button>
            <Button variant="contained" type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Đang cập nhật…" : "Đổi mật khẩu"}
            </Button>
          </Box>
        </Stack>
      </Box>
    </Box>
  )
}
