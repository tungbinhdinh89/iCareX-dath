import { useEffect, useState, type ReactNode } from "react"
import {
  Alert,
  Avatar,
  Badge,
  Box,
  Breadcrumbs,
  Button,
  Drawer,
  IconButton,
  Link,
  List,
  ListItemButton,
  ListItemText,
  Menu,
  MenuItem,
  Skeleton,
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
import KeyboardArrowDownRounded from "@mui/icons-material/KeyboardArrowDownRounded"
import MenuRounded from "@mui/icons-material/MenuRounded"
import CloseRounded from "@mui/icons-material/CloseRounded"
import ShieldOutlined from "@mui/icons-material/ShieldOutlined"
import LogoutRounded from "@mui/icons-material/LogoutRounded"
import {
  Link as RouterLink,
  Outlet,
  useLocation,
  useNavigate,
} from "react-router"
import { planState } from "@/lib/models/garage"
import { useGarage } from "@/lib/state/store"
import Dialogs from "@/components/Dialogs"
const items: {
  to: string
  label: string
  icon: ReactNode
  count?: boolean
  permission?: string
}[] = [
  { to: "/", label: "Tổng quan", icon: <GridViewOutlined /> },
  { to: "/xe", label: "Xe của tôi", icon: <DirectionsCarOutlined /> },
  {
    to: "/nhien-lieu",
    permission: "fuel.read",
    label: "Nhiên liệu",
    icon: <LocalGasStationOutlined />,
  },
  {
    to: "/bao-duong",
    permission: "maintenance.read",
    label: "Dịch vụ",
    icon: <BuildOutlined />,
  },
  {
    to: "/ke-hoach",
    permission: "maintenance_plan.read",
    label: "Kế hoạch bảo dưỡng",
    icon: <EventNoteOutlined />,
  },
  {
    to: "/nhac-bao-duong",
    permission: "reminder.read",
    label: "Hộp thư",
    icon: <NotificationsNoneOutlined />,
    count: true,
  },
  {
    to: "/lich-su",
    permission: "vehicle.read",
    label: "Lịch sử hoạt động",
    icon: <HistoryOutlined />,
  },
  {
    to: "/bao-cao",
    permission: "vehicle.read",
    label: "Báo cáo",
    icon: <InsertChartOutlined />,
  },
]
const assetBase = import.meta.env.BASE_URL
function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { data, open, can } = useGarage()
  const location = useLocation()
  const navigate = useNavigate()
  const count = data?.settings.reminders
    ? data.plans.filter((p) => planState(p, data).status !== "good").length
    : 0
  const active = (to: string) =>
    to === "/" ? location.pathname === "/" : location.pathname.startsWith(to)
  const go = (to: string) => {
    navigate(to)
    onNavigate?.()
  }
  const link = (item: typeof items[number]) => (
    <Tooltip title={item.label} placement="right" key={item.to}>
      <ListItemButton
        className="nav-item"
        selected={active(item.to)}
        aria-label={
          item.count && count ? `${item.label}, ${count} nhắc nhở` : item.label
        }
        aria-current={active(item.to) ? "page" : undefined}
        onClick={() => go(item.to)}
      >
        <Box className="nav-item-icon">{item.icon}</Box>
        <ListItemText className="nav-item-label" primary={item.label} />
        {item.count && count > 0 && <Box className="nav-count">{count}</Box>}
      </ListItemButton>
    </Tooltip>
  )
  return (
    <Box className="sidebar" component="aside" aria-label="Điều hướng ứng dụng">
      <Box className="sidebar-brand-row">
        <Button
          className="sidebar-brand"
          aria-label="iCareX — Tổng quan"
          onClick={() => go("/")}
        >
          <Box
            component="img"
            className="sidebar-logo"
            src={`${assetBase}brand/brand-logo-dark.svg`}
            alt="iCareX"
          />
          <Box
            component="img"
            className="sidebar-symbol"
            src={`${assetBase}brand/brand-symbol-dark.svg`}
            alt="iCareX"
          />
        </Button>
        {onNavigate && (
          <IconButton
            className="sidebar-close"
            aria-label="Đóng điều hướng"
            onClick={onNavigate}
            color="inherit"
          >
            <CloseRounded />
          </IconButton>
        )}
      </Box>
      <List
        className="sidebar-navigation"
        component="nav"
        aria-label="Quản lý xe"
        disablePadding
      >
        {items
          .filter(
            (item) =>
              !item.permission ||
              can(
                item.permission,
                /^\/xe\/([^/]+)/.exec(location.pathname)?.[1],
              ),
          )
          .map(link)}
        {link({
          to: "/loi-moi",
          label: "Lời mời chia sẻ",
          icon: <NotificationsNoneOutlined />,
        })}
      </List>
      <Box className="sidebar-bottom">
        <List component="nav" aria-label="Tài khoản" disablePadding>
          {link({
            to: "/ho-so",
            label: "Hồ sơ cá nhân",
            icon: <PersonOutlineRounded />,
          })}
          {link({
            to: "/cai-dat",
            label: "Cài đặt",
            icon: <SettingsOutlined />,
          })}
        </List>
        <Tooltip title="Hướng dẫn sử dụng" placement="right">
          <ListItemButton
            className="nav-item"
            aria-label="Hướng dẫn sử dụng"
            onClick={() => {
              open({ type: "help" })
              onNavigate?.()
            }}
          >
            <Box className="nav-item-icon">
              <HelpOutlineRounded />
            </Box>
            <ListItemText
              className="nav-item-label"
              primary="Hướng dẫn sử dụng"
            />
          </ListItemButton>
        </Tooltip>
      </Box>
    </Box>
  )
}

export default function Layout() {
  const { data, session, loading, error, refetch, logout, can } = useGarage()
  const location = useLocation()
  const navigate = useNavigate()
  const [mobile, setMobile] = useState(false)
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
    setMobile(false)
  }, [current, location.pathname])
  if (!session) return null
  const initials = session.name
    .trim()
    .split(/\s+/)
    .slice(-2)
    .map((n) => n[0])
    .join("")
  return (
    <Box className="app-shell">
      <a className="skip-link" href="#main-content">
        Đến nội dung chính
      </a>
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
          <Box className="topbar-context">
            <IconButton
              className="mobile-menu"
              aria-label="Mở điều hướng"
              aria-expanded={mobile}
              onClick={() => setMobile(true)}
            >
              <MenuRounded />
            </IconButton>
            <Box
              component="img"
              className="topbar-mobile-logo"
              src={`${assetBase}brand/brand-logo-light.svg`}
              alt="iCareX"
            />
            <Breadcrumbs
              className="topbar-breadcrumb"
              aria-label="Điều hướng vị trí"
              separator="/"
            >
              <Link
                component={RouterLink}
                to="/"
                underline="hover"
                color="text.secondary"
                variant="body2"
              >
                Không gian cá nhân
              </Link>
              <Typography variant="body2" color="text.primary" fontWeight={600}>
                {current}
              </Typography>
            </Breadcrumbs>
          </Box>
          <Box className="topbar-actions">
            {can("reminder.read") && (
              <Tooltip title="Hộp thư">
                <IconButton
                  aria-label={`Hộp thư, ${count} nhắc nhở`}
                  onClick={() => navigate("/nhac-bao-duong")}
                >
                  <Badge badgeContent={count} color="error">
                    <NotificationsNoneOutlined />
                  </Badge>
                </IconButton>
              </Tooltip>
            )}
            <Button
              className="topbar-account"
              color="inherit"
              aria-label={`Tài khoản ${session.name}`}
              aria-haspopup="menu"
              aria-expanded={!!accountAnchor}
              onClick={(e) => setAccountAnchor(e.currentTarget)}
            >
              <Avatar className="topbar-avatar">{initials}</Avatar>
              <Typography className="topbar-user-name" variant="body2">
                {session.name}
              </Typography>
              <KeyboardArrowDownRounded fontSize="small" />
            </Button>
            <Menu
              anchorEl={accountAnchor}
              open={!!accountAnchor}
              onClose={() => setAccountAnchor(null)}
            >
              <MenuItem
                onClick={() => {
                  setAccountAnchor(null)
                  navigate("/ho-so")
                }}
                sx={{ gap: 1 }}
              >
                <PersonOutlineRounded fontSize="small" />
                Hồ sơ cá nhân
              </MenuItem>
              <MenuItem
                onClick={() => {
                  setAccountAnchor(null)
                  navigate("/ho-so/doi-mat-khau")
                }}
                sx={{ gap: 1 }}
              >
                <ShieldOutlined fontSize="small" />
                Đổi mật khẩu
              </MenuItem>
              <MenuItem
                onClick={async () => {
                  setAccountAnchor(null)
                  await logout()
                }}
                sx={{ gap: 1 }}
              >
                <LogoutRounded fontSize="small" />
                Đăng xuất
              </MenuItem>
            </Menu>
          </Box>
        </Box>
        <Box
          component="main"
          className="page-content"
          id="main-content"
          tabIndex={-1}
        >
          {loading ? (
            <Box
              role="status"
              aria-label="Đang tải dữ liệu"
              className="loading-content"
            >
              <Typography variant="body2" color="text.secondary">
                Đang tải dữ liệu xe của bạn…
              </Typography>
              <Skeleton width="45%" height={44} />
              <Box className="detail-metrics">
                {[0, 1, 2].map((i) => (
                  <Skeleton key={i} variant="rounded" height={140} />
                ))}
              </Box>
              <Skeleton variant="rounded" height={320} />
            </Box>
          ) : error ? (
            <Box className="panel">
              <Alert
                severity="error"
                action={<Button onClick={refetch}>Thử lại</Button>}
              >
                Không tải được dữ liệu. Vui lòng kiểm tra kết nối và thử lại.
              </Alert>
            </Box>
          ) : (
            <Outlet />
          )}
          <Box component="footer" className="footer">
            <Box>© {new Date().getFullYear()} iCareX</Box>
            <Box>Chủ động chăm xe, an tâm mỗi hành trình.</Box>
          </Box>
        </Box>
      </Box>
      <Dialogs />
    </Box>
  )
}
