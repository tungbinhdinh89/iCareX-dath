import { useEffect } from "react"
import { Alert, Box, Button, CircularProgress, Typography } from "@mui/material"
import DirectionsCarOutlined from "@mui/icons-material/DirectionsCarOutlined"
import CheckCircleOutline from "@mui/icons-material/CheckCircleOutline"
import ShieldOutlined from "@mui/icons-material/ShieldOutlined"
import { Navigate, Outlet, useLocation } from "react-router"
import { LoginForm } from "@/components/Dialogs"
import { sessionReturnPath } from "@/lib/models/session"
import { useGarage } from "@/lib/state/store"

function SessionStatus() {
  const { sessionStatus, retrySession } = useGarage()
  return (
    <Box component="main" className="session-screen">
      <Box className="session-status" role="status" aria-live="polite">
        <Box className="icon-box"><DirectionsCarOutlined /></Box>
        <Typography variant="h6" component="h1">iCareX</Typography>
        {sessionStatus === "error" ? (
          <>
            <Alert severity="error">Chưa thể kết nối để kiểm tra phiên đăng nhập. Vui lòng thử lại.</Alert>
            <Button variant="contained" onClick={retrySession}>Thử kết nối lại</Button>
          </>
        ) : (
          <><CircularProgress size={28} /><Typography color="text.secondary">Đang mở không gian của bạn…</Typography></>
        )}
      </Box>
    </Box>
  )
}

export function RequireSession() {
  const { sessionStatus } = useGarage()
  const location = useLocation()
  if (sessionStatus === "checking" || sessionStatus === "error") return <SessionStatus />
  if (sessionStatus !== "authenticated") {
    return <Navigate to="/dang-nhap" replace state={{ from: sessionReturnPath(`${location.pathname}${location.search}${location.hash}`) }} />
  }
  return <Outlet />
}

export default function AuthenticationPage() {
  const { sessionStatus } = useGarage()
  const location = useLocation()
  useEffect(() => {
    document.title = "Đăng nhập | iCareX"
    document.documentElement.lang = "vi"
  }, [])
  if (sessionStatus === "checking" || sessionStatus === "error") return <SessionStatus />
  if (sessionStatus === "authenticated") {
    const state = location.state as { from?: unknown } | null
    return <Navigate to={sessionReturnPath(state?.from)} replace />
  }
  return (
    <Box component="main" className="authentication-page">
      <Box component="aside" className="auth-story">
        <Box className="auth-brand"><Box component="img" src={`${import.meta.env.BASE_URL}brand/brand-logo-dark.svg`} alt="iCareX" /></Box>
        <Box className="auth-story-copy">
          <Typography className="auth-eyebrow">CHĂM XE · AN TÂM MỖI NGÀY</Typography>
          <Typography component="h1" className="auth-story-title">Mỗi hành trình,<br />thêm một an tâm.</Typography>
          <Typography className="auth-story-description">Một không gian riêng để theo dõi phương tiện, số km và những lần chăm sóc xe của bạn.</Typography>
          <Box className="auth-benefits">
            {["Phương tiện luôn trong tầm tay", "Theo dõi số km theo thời gian", "Lưu giữ hành trình chăm xe"].map((label) => <Box key={label}><CheckCircleOutline fontSize="small" /><Typography variant="body2">{label}</Typography></Box>)}
          </Box>
        </Box>
        <Box className="auth-story-footer"><ShieldOutlined fontSize="small" /><Typography variant="caption">Chăm xe hôm nay. An tâm ngày mai.</Typography></Box>
      </Box>
      <Box className="auth-card-wrap">
        <Box className="auth-card"><LoginForm /></Box>
        <Typography variant="caption" color="text.secondary" className="auth-page-footer">© {new Date().getFullYear()} iCareX · Không gian chăm xe cá nhân</Typography>
      </Box>
    </Box>
  )
}
