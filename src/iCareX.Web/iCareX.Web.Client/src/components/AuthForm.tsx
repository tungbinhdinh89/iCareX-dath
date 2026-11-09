import { useState } from "react"
import {
  Alert,
  Box,
  Button,
  DialogActions,
  DialogContent,
  IconButton,
  InputAdornment,
  TextField,
  Typography,
} from "@mui/material"
import VisibilityOutlined from "@mui/icons-material/VisibilityOutlined"
import VisibilityOffOutlined from "@mui/icons-material/VisibilityOffOutlined"
import { Link } from "react-router"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { credentialsSchema } from "@/lib/models/auth"
import { api } from "@/lib/apis/iCareXApi"
import { useGarage } from "@/lib/state/store"

type Fields = { name: string; phone: string; email: string; password: string }

export default function AuthForm({
  initialMode = "login",
}: {
  initialMode?: "login" | "register"
}) {
  const { login, notify } = useGarage()
  const [mode, setMode] = useState(initialMode)
  const [show, setShow] = useState(false)
  const [error, setError] = useState("")
  const [success, setSuccess] = useState("")
  const registering = mode === "register"
  const {
    register,
    handleSubmit,
    resetField,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<Fields>({
    resolver: zodResolver(credentialsSchema(registering)),
    defaultValues: { name: "", phone: "", email: "", password: "" },
  })
  return (
    <Box
      component="form"
      noValidate
      onSubmit={handleSubmit(async (values) => {
        setError("")
        setSuccess("")
        try {
          if (registering) {
            await api.register(values)
            setSuccess(
              "Tài khoản đã được tạo. Kiểm tra email để xác thực, sau đó đăng nhập.",
            )
            resetField("password")
            setMode("login")
          } else login(await api.login(values.email, values.password))
        } catch (error) {
          const message =
            error instanceof Error
              ? error.message
              : "Không thể thực hiện yêu cầu."
          setError(message)
          notify(message, "error")
        }
      })}
    >
      <DialogContent className="auth-content">
        <Box className="auth-intro">
          <Typography variant="h5" component="h2">
            {registering
              ? "Bắt đầu hành trình chăm xe"
              : "Rất vui được gặp lại bạn"}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {registering
              ? "Một tài khoản cho những chiếc xe và người bạn đồng hành của bạn."
              : "Đăng nhập để mở không gian chăm xe của bạn."}
          </Typography>
        </Box>
        <Box
          className="auth-fields"
          sx={{ display: "flex", flexDirection: "column", gap: 3 }}
        >
          {error && (
            <Alert severity="error" role="alert">
              {error}
            </Alert>
          )}
          {success && <Alert severity="success">{success}</Alert>}
          {registering && (
            <>
              <TextField
                label="Họ và tên"
                autoComplete="name"
                required
                {...register("name")}
                error={!!errors.name}
                helperText={errors.name?.message}
                disabled={isSubmitting}
              />
              <TextField
                label="Số điện thoại"
                type="tel"
                autoComplete="tel"
                {...register("phone")}
                error={!!errors.phone}
                helperText={errors.phone?.message ?? "Không bắt buộc"}
                disabled={isSubmitting}
              />
            </>
          )}
          <TextField
            label="Email"
            type="email"
            autoComplete="username"
            required
            {...register("email")}
            error={!!errors.email}
            helperText={errors.email?.message}
            disabled={isSubmitting}
          />
          <TextField
            label="Mật khẩu"
            type={show ? "text" : "password"}
            autoComplete={registering ? "new-password" : "current-password"}
            required
            {...register("password")}
            error={!!errors.password}
            helperText={
              errors.password?.message ??
              (registering
                ? "Ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt."
                : undefined)
            }
            disabled={isSubmitting}
            slotProps={{
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      type="button"
                      aria-label={show ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                      onClick={() => setShow(!show)}
                    >
                      {show ? (
                        <VisibilityOffOutlined />
                      ) : (
                        <VisibilityOutlined />
                      )}
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }}
          />
          {!registering && (
            <Button
              component={Link}
              to="/quen-mat-khau"
              size="small"
              sx={{ alignSelf: "flex-end" }}
            >
              Quên mật khẩu?
            </Button>
          )}
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3, flexWrap: "wrap", gap: 1 }}>
        <Button
          disabled={isSubmitting}
          onClick={() => {
            setMode(registering ? "login" : "register")
            setError("")
            setSuccess("")
            clearErrors()
          }}
        >
          {registering ? "Đã có tài khoản" : "Tạo tài khoản"}
        </Button>
        <Button variant="contained" type="submit" disabled={isSubmitting}>
          {isSubmitting
            ? "Đang xử lý…"
            : registering
              ? "Tạo tài khoản"
              : "Đăng nhập"}
        </Button>
      </DialogActions>
    </Box>
  )
}
