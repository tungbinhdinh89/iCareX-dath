import { useEffect, useState, type ReactNode } from "react"
import {
  Alert,
  Avatar,
  Badge,
  Box,
  Button,
  CircularProgress,
  Drawer,
  IconButton,
  InputAdornment,
  List,
  ListItemButton,
  ListItemText,
  Menu,
  MenuItem,
  Skeleton,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material"
import GridViewOutlined from "@mui/icons-material/GridViewOutlined"
import DirectionsCarOutlined from "@mui/icons-material/DirectionsCarOutlined"
import LocalGasStationOutlined from "@mui/icons-material/LocalGasStationOutlined"
import BuildOutlined from "@mui/icons-material/BuildOutlined"
import EventNoteOutlined from "@mui/icons-material/EventNoteOutlined"
import NotificationsNoneOutlined from "@mui/icons-material/NotificationsNoneOutlined"
import HistoryOutlined from "@mui/icons-material/HistoryOutlined"
import InsertChartOutlined from "@mui/icons-material/InsertChartOutlined"
import PersonOutlineRounded from "@mui/icons-material/PersonOutlineRounded"
import SettingsOutlined from "@mui/icons-material/SettingsOutlined"
import HelpOutlineRounded from "@mui/icons-material/HelpOutlineRounded"
import SearchRounded from "@mui/icons-material/SearchRounded"
import KeyboardArrowDownRounded from "@mui/icons-material/KeyboardArrowDownRounded"
import ChevronRightRounded from "@mui/icons-material/ChevronRightRounded"
import MenuRounded from "@mui/icons-material/MenuRounded"
import VerifiedUserOutlined from "@mui/icons-material/VerifiedUserOutlined"
import LogoutRounded from "@mui/icons-material/LogoutRounded"
import LoginRounded from "@mui/icons-material/LoginRounded"
import EastRounded from "@mui/icons-material/EastRounded"
import { NavLink, Outlet, useLocation, useNavigate } from "react-router"
import { planState } from "@/lib/models/garage"
import { useGarage } from "@/lib/state/store"
import Dialogs from "@/components/Dialogs"

const items: {
  to: string
  label: string
  icon: ReactNode
  count?: boolean
}[] = [
  { to: "/", label: "Tổng quan", icon: <GridViewOutlined fontSize="small" /> },
  {
    to: "/xe",
    label: "Phương tiện của tôi",
    icon: <DirectionsCarOutlined fontSize="small" />,
  },
  {
    to: "/nhien-lieu",
    label: "Nhiên liệu",
    icon: <LocalGasStationOutlined fontSize="small" />,
  },
  {
    to: "/bao-duong",
    label: "Bảo dưỡng & sửa chữa",
    icon: <BuildOutlined fontSize="small" />,
  },
  {
    to: "/ke-hoach",
    label: "Kế hoạch bảo dưỡng",
    icon: <EventNoteOutlined fontSize="small" />,
  },
  {
    to: "/nhac-bao-duong",
    label: "Nhắc bảo dưỡng",
    icon: <NotificationsNoneOutlined fontSize="small" />,
    count: true,
  },
  {
    to: "/lich-su",
    label: "Lịch sử hoạt động",
    icon: <HistoryOutlined fontSize="small" />,
  },
  {
    to: "/bao-cao",
    label: "Báo cáo & thống kê",
    icon: <InsertChartOutlined fontSize="small" />,
  },
]
function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { data, open, session } = useGarage()
  const location = useLocation()
  const count = data?.settings.reminders
    ? data.plans.filter((p) => planState(p, data).status !== "good").length
    : 0
  const nav = (to: string, label: string, icon: ReactNode, badge = false) => (
    <ListItemButton
      component={NavLink}
      to={to}
      end={to === "/"}
      key={to}
      selected={
        to === "/"
          ? location.pathname === "/"
          : location.pathname.startsWith(to)
      }
      className="nav-item"
      onClick={onNavigate}
    >
      {icon}
      <ListItemText primary={label} />
      {badge && !!count && <Box className="nav-count">{count}</Box>}
    </ListItemButton>
  )
  return (
    <Box className="sidebar" component="aside">
      <Box className="brand">
        <Box className="brand-mark">
          <DirectionsCarOutlined />
        </Box>
        <Box>
          <Box className="brand-title">
            iCareX
            <Box component="span" sx={{ color: "var(--accent)" }}>
              .
            </Box>
          </Box>
          <Box className="brand-subtitle">CHĂM XE · AN TÂM MỖI NGÀY</Box>
        </Box>
      </Box>
      <Box className="nav-caption">KHÔNG GIAN CỦA BẠN</Box>
      <List component="nav" aria-label="Điều hướng chính" disablePadding>
        {items.map((i) => nav(i.to, i.label, i.icon, i.count))}
      </List>
      <Box className="nav-caption" sx={{ mt: 2 }}>
        CÁ NHÂN
      </Box>
      <List component="nav" aria-label="Tài khoản" disablePadding>
        {nav(
          "/ho-so",
          "Hồ sơ cá nhân",
          <PersonOutlineRounded fontSize="small" />,
        )}
        {nav("/cai-dat", "Cài đặt", <SettingsOutlined fontSize="small" />)}
      </List>
      <Box className="sidebar-bottom">
        <Box className="demo-note">
          <Box sx={{ display: "flex", alignItems: "center", gap: 0.8, mb: 1 }}>
            <VerifiedUserOutlined
              sx={{ color: "var(--accent)", fontSize: 16 }}
            />
            <Typography
              sx={{
                fontSize: "var(--text-meta-size)",
                color: "var(--on-brand)",
                fontWeight: 600,
              }}
            >
              {session ? "Không gian cá nhân" : "Khám phá iCareX"}
            </Typography>
          </Box>
          <Typography
            sx={{ fontSize: 10, lineHeight: 1.9, color: "var(--nav-muted)" }}
          >
            {session
              ? "Dữ liệu chỉ lưu trong bộ nhớ. Tải lại trang sẽ bắt đầu phiên mới."
              : "Chăm sóc xe dễ dàng hơn với hai tài khoản trải nghiệm."}
          </Typography>
          {!session && (
            <Button
              size="small"
              onClick={() => open({ type: "login" })}
              endIcon={<EastRounded sx={{ fontSize: 14 }} />}
              sx={{
                color: "var(--accent)",
                fontSize: 10,
                p: 0,
                mt: 0.8,
                minHeight: 24,
              }}
            >
              Trải nghiệm ngay
            </Button>
          )}
        </Box>
        <ListItemButton
          className="nav-item"
          onClick={() => {
            open({ type: "help" })
            onNavigate?.()
          }}
        >
          <HelpOutlineRounded fontSize="small" />
          <ListItemText primary="Hướng dẫn & trợ giúp" />
        </ListItemButton>
        <Typography
          sx={{ fontSize: 9, color: "var(--nav-muted)", px: 2, mt: 1 }}
        >
          ICAREX · PHIÊN BẢN DEMO 1.0
        </Typography>
      </Box>
    </Box>
  )
}
export default function Layout() {
  const { data, session, loading, error, refetch, open, logout } = useGarage()
  const location = useLocation()
  const navigate = useNavigate()
  const [mobile, setMobile] = useState(false)
  const [search, setSearch] = useState("")
  const [accountAnchor, setAccountAnchor] = useState<HTMLElement | null>(null)
  const current =
    items.find((i) =>
      i.to === "/"
        ? location.pathname === "/"
        : location.pathname.startsWith(i.to),
    )?.label ??
    (location.pathname === "/ho-so"
      ? "Hồ sơ cá nhân"
      : location.pathname === "/cai-dat"
        ? "Cài đặt"
        : "iCareX")
  const count = data?.settings.reminders
    ? data.plans.filter((p) => planState(p, data).status !== "good").length
    : 0
  useEffect(() => {
    document.title = `${current} | iCareX`
    document.documentElement.lang = "vi"
    window.scrollTo(0, 0)
  }, [current, location.pathname])
  return (
    <Box className="app-shell">
      <Sidebar />
      <Drawer
        open={mobile}
        onClose={() => setMobile(false)}
        className="mobile-drawer"
      >
        <Sidebar onNavigate={() => setMobile(false)} />
      </Drawer>
      <Box className="main-shell">
        <Box component="header" className="topbar">
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.5,
              minWidth: 0,
            }}
          >
            <IconButton
              className="mobile-menu"
              aria-label="Mở điều hướng"
              onClick={() => setMobile(true)}
            >
              <MenuRounded />
            </IconButton>
            <Typography
              className="topbar-breadcrumb"
              variant="caption"
              color="text.secondary"
              sx={{
                display: { xs: "none", sm: "block" },
                whiteSpace: "nowrap",
              }}
            >
              Không gian cá nhân
            </Typography>
            <ChevronRightRounded
              sx={{
                fontSize: 14,
                color: "text.disabled",
                display: { xs: "none", sm: "block" },
              }}
            />
            <Typography
              variant="caption"
              sx={{ fontWeight: 500, whiteSpace: "nowrap" }}
            >
              {current}
            </Typography>
          </Box>
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: { xs: 1, sm: 2 },
            }}
          >
            <Box
              component="form"
              onSubmit={(e) => {
                e.preventDefault()
                navigate(`/lich-su?q=${encodeURIComponent(search.trim())}`)
                setSearch("")
              }}
            >
              <TextField
                className="topbar-search"
                placeholder="Tìm kiếm hoạt động..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                slotProps={{
                  htmlInput: { "aria-label": "Tìm kiếm hoạt động" },
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <SearchRounded sx={{ fontSize: 18 }} />
                      </InputAdornment>
                    ),
                  },
                }}
              />
            </Box>
            <Tooltip title="Nhắc bảo dưỡng">
              <IconButton
                aria-label={`Nhắc bảo dưỡng, ${count} hạng mục cần chú ý`}
                onClick={() => navigate("/nhac-bao-duong")}
              >
                <Badge badgeContent={count} color="error" variant="dot">
                  <NotificationsNoneOutlined sx={{ fontSize: 22 }} />
                </Badge>
              </IconButton>
            </Tooltip>
            {!session && (
              <Button
                variant="outlined"
                size="small"
                onClick={() => open({ type: "login" })}
              >
                Đăng nhập
              </Button>
            )}
            <Box
              sx={{
                height: 28,
                borderLeft: "1px solid var(--border-secondary)",
                display: { xs: "none", sm: "block" },
              }}
            />
            <Button
              color="inherit"
              onClick={(e) => setAccountAnchor(e.currentTarget)}
              sx={{ p: "0 !important", minWidth: 0, gap: 1 }}
            >
              <Avatar
                sx={{
                  width: 35,
                  height: 35,
                  bgcolor: "var(--brand-secondary)",
                  color: "primary.main",
                  fontSize: 12,
                  fontWeight: 600,
                }}
              >
                {session
                  ? data?.profile.name
                      .split(" ")
                      .slice(-2)
                      .map((n) => n[0])
                      .join("")
                  : "VC"}
              </Avatar>
              {session && (
                <Box className="topbar-user-name" sx={{ textAlign: "left" }}>
                  <Typography
                    sx={{ fontSize: "var(--text-meta-size)", fontWeight: 600 }}
                  >
                    {data?.profile.name}
                  </Typography>
                  <Typography sx={{ fontSize: 9, color: "text.secondary" }}>
                    Tài khoản cá nhân
                  </Typography>
                </Box>
              )}
              <KeyboardArrowDownRounded
                sx={{ fontSize: 16, color: "text.secondary" }}
              />
            </Button>
            <Menu
              anchorEl={accountAnchor}
              open={!!accountAnchor}
              onClose={() => setAccountAnchor(null)}
            >
              {session ? (
                [
                  <MenuItem
                    key="profile"
                    onClick={() => {
                      setAccountAnchor(null)
                      navigate("/ho-so")
                    }}
                    sx={{ gap: 1, fontSize: "var(--text-small-size)" }}
                  >
                    <PersonOutlineRounded fontSize="small" />
                    Hồ sơ cá nhân
                  </MenuItem>,
                  <MenuItem
                    key="logout"
                    onClick={() => {
                      setAccountAnchor(null)
                      logout()
                      navigate("/")
                    }}
                    sx={{ gap: 1, fontSize: "var(--text-small-size)" }}
                  >
                    <LogoutRounded fontSize="small" />
                    Đăng xuất
                  </MenuItem>,
                ]
              ) : (
                <MenuItem
                  onClick={() => {
                    setAccountAnchor(null)
                    open({ type: "login" })
                  }}
                  sx={{ gap: 1, fontSize: "var(--text-small-size)" }}
                >
                  <LoginRounded fontSize="small" />
                  Đăng nhập
                </MenuItem>
              )}
            </Menu>
          </Box>
        </Box>
        <Box component="main" className="page-content" id="main-content">
          {loading ? (
            <Box
              aria-label="Đang tải dữ liệu"
              sx={{ display: "flex", flexDirection: "column", gap: 3 }}
            >
              <Skeleton width="45%" height={45} />
              <Box className="stats-grid">
                {[0, 1, 2, 3].map((i) => (
                  <Skeleton key={i} variant="rounded" height={150} />
                ))}
              </Box>
              <Skeleton variant="rounded" height={330} />
              <Box sx={{ textAlign: "center" }}>
                <CircularProgress size={22} />
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ ml: 1.5 }}
                >
                  Đang chuẩn bị garage của bạn...
                </Typography>
              </Box>
            </Box>
          ) : error ? (
            <Alert
              severity="error"
              action={<Button onClick={refetch}>Thử lại</Button>}
            >
              Không thể tải dữ liệu: {error.message}
            </Alert>
          ) : (
            <Outlet />
          )}
          <Box component="footer" className="footer">
            <Box>
              © {new Date().getFullYear()} iCareX. Chăm xe hôm nay, an tâm
              ngày mai.
            </Box>
            <Box sx={{ display: "flex", alignItems: "center", gap: 0.6 }}>
              <Box
                sx={{
                  width: 5,
                  height: 5,
                  borderRadius: "50%",
                  bgcolor: "var(--success)",
                }}
              />
              {session
                ? "Dữ liệu trong bộ nhớ"
                : "Dữ liệu minh họa · Đăng nhập để ghi nhận"}
              <Box sx={{ mx: 0.5 }}>·</Box>Asia/Ho_Chi_Minh
            </Box>
          </Box>
        </Box>
      </Box>
      <Dialogs />
    </Box>
  )
}
