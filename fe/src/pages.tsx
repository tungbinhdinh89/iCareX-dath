import { useEffect, useState, type ReactNode } from "react"
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  Divider,
  FormControlLabel,
  IconButton,
  Menu,
  MenuItem,
  Switch,
  Tab,
  Tabs,
  TablePagination,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material"
import AddRounded from "@mui/icons-material/AddRounded"
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded"
import DirectionsCarOutlined from "@mui/icons-material/DirectionsCarOutlined"
import LocalGasStationOutlined from "@mui/icons-material/LocalGasStationOutlined"
import BuildOutlined from "@mui/icons-material/BuildOutlined"
import SpeedOutlined from "@mui/icons-material/SpeedOutlined"
import AccountBalanceWalletOutlined from "@mui/icons-material/AccountBalanceWalletOutlined"
import NotificationsNoneOutlined from "@mui/icons-material/NotificationsNoneOutlined"
import CalendarMonthOutlined from "@mui/icons-material/CalendarMonthOutlined"
import EastRounded from "@mui/icons-material/EastRounded"
import InfoOutlined from "@mui/icons-material/InfoOutlined"
import EditOutlined from "@mui/icons-material/EditOutlined"
import DeleteOutline from "@mui/icons-material/DeleteOutline"
import FileDownloadOutlined from "@mui/icons-material/FileDownloadOutlined"
import SearchRounded from "@mui/icons-material/SearchRounded"
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded"
import ShieldOutlined from "@mui/icons-material/ShieldOutlined"
import CheckCircleOutline from "@mui/icons-material/CheckCircleOutline"
import { useNavigate, useParams, useSearchParams } from "react-router"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import {
  dateLabel,
  money,
  monthLabel,
  number,
  odoOf,
  planState,
  today,
  type Entry,
  type Garage,
} from "./data"
import {
  Empty,
  ExpenseChart,
  HistoryTable,
  MonthFilter,
  Reminder,
  VehicleCard,
  VehicleFilter,
} from "./components"
import { useGarage } from "./store"

function Heading({
  title,
  subtitle,
  children,
}: {
  title: string
  subtitle: string
  children?: ReactNode
}) {
  return (
    <Box className="page-heading">
      <Box>
        <Typography variant="h4" component="h1">
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.7 }}>
          {subtitle}
        </Typography>
      </Box>
      {children}
    </Box>
  )
}
function Stat({
  title,
  value,
  icon,
  foot,
  tone = "",
  children,
}: {
  title: string
  value: string
  icon: ReactNode
  foot: ReactNode
  tone?: string
  children?: ReactNode
}) {
  return (
    <Box className="stat-card">
      <Box className="stat-head">
        <Typography variant="body2" color="text.secondary">
          {title}
        </Typography>
        <Box className={`icon-box ${tone}`}>{icon}</Box>
      </Box>
      <Box className="stat-value">
        {value}
        {children}
      </Box>
      <Box className="stat-foot">{foot}</Box>
    </Box>
  )
}
function AddRecordButton() {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const { open } = useGarage()
  return (
    <>
      <Button
        variant="contained"
        startIcon={<AddRounded />}
        onClick={(e) => setAnchor(e.currentTarget)}
      >
        Thêm ghi nhận
      </Button>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        {[
          [
            "fuel",
            "Đổ nhiên liệu",
            <LocalGasStationOutlined key="fuel" fontSize="small" />,
          ],
          [
            "service",
            "Bảo dưỡng / sửa chữa",
            <BuildOutlined key="service" fontSize="small" />,
          ],
          ["odo", "Cập nhật ODO", <SpeedOutlined key="odo" fontSize="small" />],
        ].map(([kind, title, icon]) => (
          <MenuItem
            key={String(kind)}
            onClick={() => {
              setAnchor(null)
              open({ type: "entry", kind: kind as Entry["kind"] })
            }}
            sx={{ gap: 1.5, fontSize: "var(--text-small-size)" }}
          >
            {icon}
            {title}
          </MenuItem>
        ))}
      </Menu>
    </>
  )
}
function currentEntries(data: Garage, vehicle: string, month: string) {
  return data.entries.filter(
    (e) =>
      (vehicle === "all" || e.vehicleId === vehicle) &&
      (month === "all" || e.date.startsWith(month)),
  )
}
function driven(data: Garage, vehicle: string, month: string) {
  return data.vehicles
    .filter((v) => vehicle === "all" || v.id === vehicle)
    .reduce((sum, v) => {
      const entries = data.entries.filter(
        (e) =>
          e.vehicleId === v.id && (month === "all" || e.date.startsWith(month)),
      )
      if (!entries.length) return sum
      const before =
        month === "all"
          ? v.baseOdo
          : Math.max(
              v.baseOdo,
              ...data.entries
                .filter((e) => e.vehicleId === v.id && e.date < `${month}-01`)
                .map((e) => e.odo),
            )
      return sum + Math.max(0, Math.max(...entries.map((e) => e.odo)) - before)
    }, 0)
}
export default function Dashboard() {
  const { data, session, open, month, selectedVehicle } = useGarage()
  const navigate = useNavigate()
  if (!data) return null
  const activeMonth = month === "all" ? today().slice(0, 7) : month
  const allEntries = currentEntries(data, selectedVehicle, "all")
  const entries = currentEntries(data, selectedVehicle, activeMonth)
  const vehicles = data.vehicles.filter(
    (v) => selectedVehicle === "all" || v.id === selectedVehicle,
  )
  const plans = data.plans
    .filter((p) => selectedVehicle === "all" || p.vehicleId === selectedVehicle)
    .sort((a, b) => planState(a, data).days - planState(b, data).days)
  const due = plans.filter((p) => planState(p, data).status !== "good")
  const fuel = entries
    .filter((e) => e.kind === "fuel")
    .reduce((s, e) => s + e.cost, 0)
  const service = entries
    .filter((e) => e.kind === "service")
    .reduce((s, e) => s + e.cost, 0)
  const latest = [...entries]
    .sort((a, b) => b.date.localeCompare(a.date) || b.odo - a.odo)
    .slice(0, 4)
  return (
    <Box className="route-fade">
      <Box className="page-heading">
        <Box>
          <Box className="eyebrow">
            {session
              ? `Rất vui được gặp lại, ${data.profile.name.split(" ").slice(-2).join(" ")}!`
              : "Mọi chiếc xe. Một nơi chăm sóc."}
          </Box>
          <Typography variant="h4" component="h1">
            Tổng quan phương tiện
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.7 }}>
            Chăm xe chủ động, an tâm trên mọi hành trình.
          </Typography>
        </Box>
        <AddRecordButton />
      </Box>
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: 1.5,
          mb: 2.5,
          flexWrap: "wrap",
        }}
      >
        <Box
          sx={{
            display: "flex",
            gap: 1.5,
            "& .MuiTextField-root": { width: { xs: 157, sm: 190 } },
          }}
        >
          <VehicleFilter />
          <MonthFilter all={false} />
        </Box>
        <Box
          className="desktop-date"
          sx={{
            display: { xs: "none", md: "flex" },
            alignItems: "center",
            gap: 0.8,
            color: "text.secondary",
          }}
        >
          <CalendarMonthOutlined sx={{ fontSize: 16 }} />
          <Typography variant="caption">{dateLabel(today())}</Typography>
        </Box>
      </Box>
      <Box className="stats-grid">
        <Stat
          title="Phương tiện của bạn"
          value={number(vehicles.length)}
          icon={<DirectionsCarOutlined fontSize="small" />}
          foot={
            <>
              {vehicles.filter((v) => v.type === "Ô tô").length} ô tô{" "}
              <Box component="span" sx={{ mx: 0.6 }}>
                ·
              </Box>{" "}
              {vehicles.filter((v) => v.type === "Xe máy").length} xe máy
            </>
          }
        />
        <Stat
          title="Tổng chi phí"
          value={money(fuel + service)}
          icon={<AccountBalanceWalletOutlined fontSize="small" />}
          tone="green"
          foot={
            <>
              <Box
                component="span"
                sx={{ color: "var(--success)", display: "inline-flex" }}
              >
                <CalendarMonthOutlined sx={{ fontSize: 12 }} />
              </Box>{" "}
              {monthLabel(activeMonth)}
            </>
          }
        />
        <Stat
          title="Quãng đường đã đi"
          value={number(driven(data, selectedVehicle, activeMonth))}
          icon={<SpeedOutlined fontSize="small" />}
          foot={<>Tính từ các bản ghi ODO</>}
        >
          <Box
            component="span"
            sx={{
              ml: 0.8,
              fontSize: "var(--text-small-size)",
              fontWeight: 400,
              color: "text.secondary",
            }}
          >
            km
          </Box>
        </Stat>
        <Stat
          title="Nhắc bảo dưỡng"
          value={number(due.length)}
          icon={<NotificationsNoneOutlined fontSize="small" />}
          tone="amber"
          foot={
            <>
              <Box component="span" sx={{ color: "var(--danger)" }}>
                {
                  due.filter((p) => planState(p, data).status === "overdue")
                    .length
                }{" "}
                quá hạn
              </Box>
              <Box component="span" sx={{ mx: 0.4 }}>
                ·
              </Box>
              {due.filter((p) => planState(p, data).status === "soon").length}{" "}
              sắp đến hạn
            </>
          }
        />
      </Box>
      <Box className="dashboard-grid">
        <Box sx={{ minWidth: 0 }}>
          <Box className="panel">
            <Box className="panel-header">
              <Box>
                <Typography variant="h6">Xu hướng chi phí</Typography>
                <Typography
                  sx={{
                    fontSize: "var(--text-meta-size)",
                    color: "text.secondary",
                    mt: 0.5,
                  }}
                >
                  Hiểu rõ chi tiêu, chăm xe hiệu quả hơn
                </Typography>
              </Box>
              <Button
                size="small"
                endIcon={<ArrowForwardRounded sx={{ fontSize: 15 }} />}
                onClick={() => navigate("/bao-cao")}
                sx={{ fontSize: "var(--text-meta-size)" }}
              >
                Xem báo cáo
              </Button>
            </Box>
            <Box className="chart-summary">
              <Box className="chart-legend">
                <Box className="legend-dot" />
                Nhiên liệu
              </Box>
              <Box className="chart-legend">
                <Box className="legend-dot green" />
                Bảo dưỡng & sửa chữa
              </Box>
              <Typography
                sx={{
                  ml: "auto",
                  fontSize: "var(--text-meta-size)",
                  color: "text.secondary",
                }}
              >
                Đơn vị: triệu VNĐ
              </Typography>
            </Box>
            <Box
              className="chart-wrap"
              role="img"
              aria-label="Biểu đồ chi phí nhiên liệu và bảo dưỡng sáu tháng gần nhất"
            >
              <ExpenseChart entries={allEntries} endMonth={activeMonth} />
            </Box>
          </Box>
          <Box className="panel section-space">
            <Box className="panel-header">
              <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                <Typography variant="h6">Garage của bạn</Typography>
                <Chip
                  size="small"
                  label={vehicles.length}
                  sx={{ background: "var(--bg-subtle)", height: 21 }}
                />
              </Box>
              <Button
                size="small"
                endIcon={<ArrowForwardRounded sx={{ fontSize: 15 }} />}
                onClick={() => navigate("/xe")}
                sx={{ fontSize: "var(--text-meta-size)" }}
              >
                Tất cả phương tiện
              </Button>
            </Box>
            {vehicles.length ? (
              <Box className="vehicles-grid">
                {vehicles.slice(0, 2).map((v) => (
                  <VehicleCard key={v.id} vehicle={v} />
                ))}
              </Box>
            ) : (
              <Empty
                action={
                  <Button onClick={() => open({ type: "vehicle" })}>
                    Thêm phương tiện
                  </Button>
                }
              />
            )}
          </Box>
        </Box>
        <Box className="dashboard-right">
          <Box className="panel">
            <Typography variant="h6" sx={{ mb: 2 }}>
              Ghi nhận nhanh
            </Typography>
            <Box className="quick-grid">
              <Button
                className="quick-action"
                onClick={() => open({ type: "entry", kind: "fuel" })}
              >
                <Box className="icon-box">
                  <LocalGasStationOutlined />
                </Box>
                Đổ nhiên liệu
              </Button>
              <Button
                className="quick-action"
                onClick={() => open({ type: "entry", kind: "service" })}
              >
                <Box className="icon-box amber">
                  <BuildOutlined />
                </Box>
                Bảo dưỡng / sửa chữa
              </Button>
              <Button
                className="quick-action"
                onClick={() => open({ type: "entry", kind: "odo" })}
              >
                <Box className="icon-box green">
                  <SpeedOutlined />
                </Box>
                Cập nhật ODO
              </Button>
              <Button
                className="quick-action"
                onClick={() => open({ type: "vehicle" })}
              >
                <Box className="icon-box">
                  <AddRounded />
                </Box>
                Thêm phương tiện
              </Button>
            </Box>
            <Box className="tip">
              <InfoOutlined
                sx={{
                  fontSize: 16,
                  color: "primary.main",
                  mt: 0.2,
                  flexShrink: 0,
                }}
              />
              <Typography
                sx={{
                  fontSize: "var(--text-meta-size)",
                  color: "primary.main",
                  lineHeight: 1.8,
                }}
              >
                Cập nhật ODO thường xuyên để nhận nhắc bảo dưỡng chính xác hơn.
              </Typography>
            </Box>
          </Box>
          <Box className="panel section-space">
            <Box className="panel-header">
              <Typography variant="h6">Cần bạn chú ý</Typography>
              <Box className="icon-box amber" sx={{ width: 28, height: 28 }}>
                <NotificationsNoneOutlined sx={{ fontSize: 18 }} />
              </Box>
            </Box>
            {due.length ? (
              due
                .slice(0, 3)
                .map((plan) => (
                  <Reminder key={plan.id} plan={plan} data={data} compact />
                ))
            ) : (
              <Empty
                title="Mọi thứ đều ổn"
                description="Chưa có hạng mục nào đến hạn."
              />
            )}
            <Button
              fullWidth
              variant="outlined"
              size="small"
              endIcon={<EastRounded sx={{ fontSize: 15 }} />}
              sx={{ mt: 2.5, fontSize: "var(--text-meta-size)" }}
              onClick={() => navigate("/nhac-bao-duong")}
            >
              Xem tất cả nhắc nhở
            </Button>
          </Box>
        </Box>
      </Box>
      <Box className="panel activity-panel section-space">
        <Box className="panel-header">
          <Box>
            <Typography variant="h6">Hoạt động gần đây</Typography>
            <Typography
              sx={{
                fontSize: "var(--text-meta-size)",
                color: "text.secondary",
                mt: 0.5,
              }}
            >
              Nhật ký chăm sóc cho những người bạn đồng hành
            </Typography>
          </Box>
          <Button
            size="small"
            endIcon={<ArrowForwardRounded sx={{ fontSize: 15 }} />}
            onClick={() => navigate("/lich-su")}
            sx={{ fontSize: "var(--text-meta-size)" }}
          >
            Xem lịch sử
          </Button>
        </Box>
        <HistoryTable entries={latest} editable={false} />
      </Box>
    </Box>
  )
}

export function Vehicles() {
  const { data, open } = useGarage()
  const [search, setSearch] = useState("")
  const [type, setType] = useState("all")
  if (!data) return null
  const vehicles = data.vehicles.filter(
    (v) =>
      (type === "all" || v.type === type) &&
      `${v.name} ${v.plate}`.toLowerCase().includes(search.toLowerCase()),
  )
  return (
    <Box className="route-fade">
      <Heading
        title="Phương tiện của tôi"
        subtitle="Mỗi chiếc xe, một người bạn đồng hành."
      >
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          onClick={() => open({ type: "vehicle" })}
        >
          Thêm phương tiện
        </Button>
      </Heading>
      <Box className="filters">
        <TextField
          label="Tìm tên xe, biển số"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          slotProps={{
            input: {
              startAdornment: (
                <SearchRounded
                  sx={{ mr: 1, fontSize: 18, color: "text.secondary" }}
                />
              ),
            },
          }}
        />
        <TextField
          select
          label="Loại phương tiện"
          value={type}
          onChange={(e) => setType(e.target.value)}
        >
          <MenuItem value="all">Tất cả loại xe</MenuItem>
          <MenuItem value="Ô tô">Ô tô</MenuItem>
          <MenuItem value="Xe máy">Xe máy</MenuItem>
        </TextField>
        <Typography variant="caption" color="text.secondary">
          {vehicles.length} phương tiện
        </Typography>
      </Box>
      {vehicles.length ? (
        <Box
          className="vehicles-grid"
          sx={{
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, minmax(0, 1fr))",
              xl: "repeat(3, minmax(0, 1fr))",
            },
          }}
        >
          {vehicles.map((v) => (
            <VehicleCard key={v.id} vehicle={v} />
          ))}
        </Box>
      ) : (
        <Box className="panel">
          <Empty
            title="Không tìm thấy phương tiện"
            description={
              data.vehicles.length
                ? "Thử từ khóa hoặc loại phương tiện khác."
                : "Thêm xe đầu tiên và bắt đầu hành trình chăm sóc."
            }
            action={
              <Button
                variant="contained"
                onClick={() => open({ type: "vehicle" })}
              >
                Thêm phương tiện
              </Button>
            }
          />
        </Box>
      )}
    </Box>
  )
}
export function VehicleDetail() {
  const { id } = useParams()
  const { data, open } = useGarage()
  const navigate = useNavigate()
  const [tab, setTab] = useState(0)
  if (!data) return null
  const vehicle = data.vehicles.find((v) => v.id === id)
  if (!vehicle)
    return (
      <Empty
        title="Không tìm thấy xe"
        description="Xe đã bị xóa hoặc không thuộc tài khoản này."
        action={
          <Button onClick={() => navigate("/xe")}>Về danh sách xe</Button>
        }
      />
    )
  const entries = data.entries
    .filter((e) => e.vehicleId === id)
    .sort((a, b) => b.date.localeCompare(a.date) || b.odo - a.odo)
  const plans = data.plans.filter((p) => p.vehicleId === id)
  return (
    <Box className="route-fade">
      <Button
        startIcon={<ArrowBackRounded />}
        size="small"
        onClick={() => navigate("/xe")}
        sx={{ mb: 2 }}
      >
        Phương tiện của tôi
      </Button>
      <Heading
        title={vehicle.name}
        subtitle={`${vehicle.plate} · ${vehicle.type} · Năm ${vehicle.year}`}
      >
        <Box sx={{ display: "flex", gap: 1 }}>
          <Tooltip title="Chỉnh sửa xe">
            <IconButton
              aria-label="Chỉnh sửa xe"
              onClick={() => open({ type: "vehicle", vehicle })}
            >
              <EditOutlined />
            </IconButton>
          </Tooltip>
          <Tooltip title="Xóa phương tiện">
            <IconButton
              aria-label="Xóa phương tiện"
              color="error"
              onClick={() =>
                open({
                  type: "delete",
                  entity: "vehicle",
                  id: vehicle.id,
                  title: vehicle.name,
                })
              }
            >
              <DeleteOutline />
            </IconButton>
          </Tooltip>
        </Box>
      </Heading>
      <Box className="detail-metrics" sx={{ mb: 3 }}>
        <Stat
          title="ODO hiện tại"
          value={`${number(odoOf(vehicle, data.entries))} km`}
          icon={<SpeedOutlined />}
          foot={`ODO ban đầu: ${number(vehicle.baseOdo)} km`}
        />
        <Stat
          title="Chi phí toàn thời gian"
          value={money(entries.reduce((s, e) => s + e.cost, 0))}
          icon={<AccountBalanceWalletOutlined />}
          tone="green"
          foot={`${entries.filter((e) => e.kind !== "odo").length} giao dịch đã ghi nhận`}
        />
        <Stat
          title="Kế hoạch bảo dưỡng"
          value={number(plans.length)}
          icon={<BuildOutlined />}
          tone="amber"
          foot={`${plans.filter((p) => planState(p, data).status !== "good").length} hạng mục cần chú ý`}
        />
      </Box>
      <Box className="panel">
        <Box className="filters">
          {(["fuel", "service", "odo"] as const).map((kind) => (
            <Button
              key={kind}
              variant={kind === "fuel" ? "contained" : "outlined"}
              onClick={() =>
                open({ type: "entry", kind, vehicleId: vehicle.id })
              }
              startIcon={
                kind === "fuel" ? (
                  <LocalGasStationOutlined />
                ) : kind === "service" ? (
                  <BuildOutlined />
                ) : (
                  <SpeedOutlined />
                )
              }
            >
              {kind === "fuel"
                ? "Đổ nhiên liệu"
                : kind === "service"
                  ? "Thêm dịch vụ"
                  : "Cập nhật ODO"}
            </Button>
          ))}
          <Button
            variant="outlined"
            startIcon={<AddRounded />}
            onClick={() => open({ type: "plan", vehicleId: vehicle.id })}
          >
            Tạo kế hoạch
          </Button>
        </Box>
        <Tabs value={tab} onChange={(_, t: number) => setTab(t)} sx={{ mb: 2 }}>
          <Tab label="Lịch sử chăm sóc" />
          <Tab label="Kế hoạch bảo dưỡng" />
          <Tab label="Thông tin xe" />
        </Tabs>
        {tab === 0 && <HistoryTable entries={entries} />}
        {tab === 1 &&
          (plans.length ? (
            plans.map((p) => <Reminder key={p.id} plan={p} data={data} />)
          ) : (
            <Empty description="Chưa có kế hoạch cho phương tiện này." />
          ))}
        {tab === 2 && (
          <Box sx={{ py: 2 }}>
            <Typography variant="body2">Tên xe: {vehicle.name}</Typography>
            <Typography variant="body2" sx={{ mt: 1 }}>
              Biển số: {vehicle.plate}
            </Typography>
            <Typography variant="body2" sx={{ mt: 1 }}>
              Loại xe: {vehicle.type} · Năm sản xuất: {vehicle.year}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              ODO được nhập thủ công và tính từ lịch sử. Ảnh phương tiện là ảnh
              minh họa từ Unsplash.
            </Typography>
          </Box>
        )}
      </Box>
    </Box>
  )
}
export function Records({ kind }: { kind?: Entry["kind"] }) {
  const { data, month, selectedVehicle, open, setMonth } = useGarage()
  const [params] = useSearchParams()
  const [search, setSearch] = useState(params.get("q") ?? "")
  const [kindFilter, setKindFilter] = useState("all")
  const [page, setPage] = useState(0)
  const [rows, setRows] = useState(10)
  const [order, setOrder] = useState("desc")
  useEffect(() => {
    setSearch(params.get("q") ?? "")
  }, [params])
  useEffect(() => {
    setPage(0)
  }, [search, month, selectedVehicle, kind, kindFilter, rows, order])
  if (!data) return null
  const base = currentEntries(data, selectedVehicle, month).filter(
    (e) => !kind || e.kind === kind,
  )
  const entries = base
    .filter(
      (e) =>
        (kindFilter === "all" || e.kind === kindFilter) &&
        `${e.title} ${e.note} ${data.vehicles.find((v) => v.id === e.vehicleId)?.name} ${data.vehicles.find((v) => v.id === e.vehicleId)?.plate}`
          .toLowerCase()
          .includes(search.toLowerCase()),
    )
    .sort(
      (a, b) =>
        (order === "desc" ? -1 : 1) *
        (a.date.localeCompare(b.date) || a.odo - b.odo),
    )
  const liters = base.reduce((s, e) => s + e.liters, 0),
    cost = base.reduce((s, e) => s + e.cost, 0)
  return (
    <Box className="route-fade">
      <Heading
        title={
          kind === "fuel"
            ? "Nhật ký nhiên liệu"
            : kind === "service"
              ? "Bảo dưỡng & sửa chữa"
              : "Lịch sử hoạt động"
        }
        subtitle={
          kind === "fuel"
            ? "Theo dõi từng lần đổ nhiên liệu, nắm rõ chi phí sử dụng."
            : kind === "service"
              ? "Lưu giữ mọi dịch vụ, chăm sóc xe đúng cách."
              : "Toàn bộ hành trình chăm sóc xe, được sắp xếp rõ ràng."
        }
      >
        {kind ? (
          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={() => open({ type: "entry", kind })}
          >
            {kind === "fuel" ? "Thêm lần đổ xăng" : "Thêm dịch vụ"}
          </Button>
        ) : (
          <AddRecordButton />
        )}
      </Heading>
      {kind && (
        <Box className="detail-metrics" sx={{ mb: 3 }}>
          <Stat
            title={
              kind === "fuel" ? "Tổng chi nhiên liệu" : "Tổng chi phí dịch vụ"
            }
            value={money(cost)}
            icon={
              kind === "fuel" ? <LocalGasStationOutlined /> : <BuildOutlined />
            }
            foot={month === "all" ? "Trong toàn bộ lịch sử" : monthLabel(month)}
          />
          <Stat
            title={
              kind === "fuel" ? "Lượng nhiên liệu" : "Dịch vụ đã thực hiện"
            }
            value={
              kind === "fuel" ? `${number(liters)} lít` : number(base.length)
            }
            icon={kind === "fuel" ? <SpeedOutlined /> : <CheckCircleOutline />}
            tone="green"
            foot={
              kind === "fuel"
                ? `${base.length} lần đổ nhiên liệu`
                : "Chỉ tính các dịch vụ hợp lệ"
            }
          />
          <Stat
            title={
              kind === "fuel" ? "Đơn giá trung bình" : "Chi phí trung bình"
            }
            value={money(
              kind === "fuel"
                ? liters
                  ? cost / liters
                  : 0
                : base.length
                  ? cost / base.length
                  : 0,
            )}
            icon={<AccountBalanceWalletOutlined />}
            tone="amber"
            foot={
              kind === "fuel" ? "VNĐ / lít · Theo các bản ghi" : "VNĐ / dịch vụ"
            }
          />
        </Box>
      )}
      <Box className="panel">
        <Box className="filters">
          <VehicleFilter />
          <MonthFilter />
          <TextField
            label="Tìm trong lịch sử"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            slotProps={{
              input: {
                startAdornment: (
                  <SearchRounded
                    sx={{ mr: 1, fontSize: 18, color: "text.secondary" }}
                  />
                ),
              },
            }}
          />
          {!kind && (
            <TextField
              select
              label="Loại hoạt động"
              value={kindFilter}
              onChange={(e) => setKindFilter(e.target.value)}
            >
              <MenuItem value="all">Tất cả hoạt động</MenuItem>
              <MenuItem value="fuel">Nhiên liệu</MenuItem>
              <MenuItem value="service">Bảo dưỡng</MenuItem>
              <MenuItem value="odo">ODO</MenuItem>
            </TextField>
          )}
          <TextField
            select
            label="Sắp xếp"
            value={order}
            onChange={(e) => setOrder(e.target.value)}
            sx={{ maxWidth: 155 }}
          >
            <MenuItem value="desc">Mới nhất trước</MenuItem>
            <MenuItem value="asc">Cũ nhất trước</MenuItem>
          </TextField>
        </Box>
        <HistoryTable
          entries={entries.slice(page * rows, page * rows + rows)}
        />
        <TablePagination
          component="div"
          count={entries.length}
          page={Math.min(
            page,
            Math.max(0, Math.ceil(entries.length / rows) - 1),
          )}
          onPageChange={(_, p) => setPage(p)}
          rowsPerPage={rows}
          rowsPerPageOptions={[5, 10, 25]}
          onRowsPerPageChange={(e) => setRows(Number(e.target.value))}
          labelRowsPerPage="Số dòng:"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}–${to} / ${count}`
          }
          getItemAriaLabel={(type) =>
            type === "next" ? "Trang sau" : "Trang trước"
          }
        />
      </Box>
      {kind === "service" && (
        <Alert severity="info" sx={{ mt: 3 }}>
          Muốn hoàn tất một kế hoạch? Chọn “Thực hiện” tại màn hình Kế hoạch bảo
          dưỡng. Bản ghi dịch vụ được liên kết và chi phí không bị tính trùng.
        </Alert>
      )}
      {month !== "all" && !entries.length && (
        <Button onClick={() => setMonth("all")} sx={{ mt: 2 }}>
          Xem tất cả thời gian
        </Button>
      )}
    </Box>
  )
}
export function Plans({ reminders = false }: { reminders?: boolean }) {
  const { data, open, selectedVehicle, save, pending } = useGarage()
  const [status, setStatus] = useState(reminders ? "attention" : "all")
  const [search, setSearch] = useState("")
  useEffect(() => setStatus(reminders ? "attention" : "all"), [reminders])
  if (!data) return null
  const plans = data.plans
    .filter(
      (p) =>
        (selectedVehicle === "all" || p.vehicleId === selectedVehicle) &&
        p.title.toLowerCase().includes(search.toLowerCase()) &&
        (status === "all" ||
          (status === "attention"
            ? planState(p, data).status !== "good"
            : planState(p, data).status === status)),
    )
    .sort((a, b) => planState(a, data).days - planState(b, data).days)
  return (
    <Box className="route-fade">
      <Heading
        title={reminders ? "Nhắc bảo dưỡng" : "Kế hoạch bảo dưỡng"}
        subtitle={
          reminders
            ? "Một chút quan tâm hôm nay, an tâm cho chặng đường dài."
            : "Lên lịch theo thời gian và số km. Không bỏ lỡ kỳ chăm sóc nào."
        }
      >
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          onClick={() => open({ type: "plan" })}
        >
          Tạo kế hoạch
        </Button>
      </Heading>
      {reminders && (
        <Alert
          severity={data.settings.reminders ? "info" : "warning"}
          sx={{ mb: 3 }}
        >
          {data.settings.reminders
            ? `Nhắc trước ${data.settings.thresholdDays} ngày hoặc ${number(data.settings.thresholdKm)} km. Chỉ hiển thị khi ứng dụng đang mở.`
            : "Thông báo trong ứng dụng đang tắt. Bạn vẫn có thể theo dõi các hạng mục tại đây."}
        </Alert>
      )}
      <Box className="filters">
        <VehicleFilter />
        <TextField
          select
          label="Trạng thái"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <MenuItem value="all">Tất cả trạng thái</MenuItem>
          <MenuItem value="attention">Cần chú ý</MenuItem>
          <MenuItem value="overdue">Quá hạn</MenuItem>
          <MenuItem value="soon">Sắp đến hạn</MenuItem>
          <MenuItem value="good">Đúng lịch</MenuItem>
        </TextField>
        <TextField
          label="Tìm hạng mục"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Typography variant="caption" color="text.secondary">
          {plans.length} kế hoạch
        </Typography>
      </Box>
      {plans.length ? (
        <Box className="vehicles-grid">
          {plans.map((p) => {
            const state = planState(p, data)
            return (
              <Box className="panel" key={p.id}>
                <Box
                  sx={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                  }}
                >
                  <Box className="icon-box amber">
                    <BuildOutlined fontSize="small" />
                  </Box>
                  <Box>
                    <Tooltip title="Sửa kế hoạch">
                      <IconButton
                        size="small"
                        aria-label={`Sửa kế hoạch ${p.title}`}
                        onClick={() => open({ type: "plan", plan: p })}
                      >
                        <EditOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Xóa kế hoạch">
                      <IconButton
                        size="small"
                        aria-label={`Xóa kế hoạch ${p.title}`}
                        onClick={() =>
                          open({
                            type: "delete",
                            entity: "plan",
                            id: p.id,
                            title: p.title,
                          })
                        }
                      >
                        <DeleteOutline fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </Box>
                <Reminder plan={p} data={data} />
                <Divider sx={{ my: 2 }} />
                <Typography variant="caption" color="text.secondary">
                  Chu kỳ: {number(p.intervalKm)} km hoặc {p.intervalMonths}{" "}
                  tháng
                </Typography>
                {state.service && (
                  <Box
                    sx={{
                      bgcolor: "var(--success-soft)",
                      p: 1.5,
                      borderRadius: "var(--corner-md)",
                      mt: 1.5,
                    }}
                  >
                    <Typography
                      variant="caption"
                      sx={{ color: "var(--success)" }}
                    >
                      Đã thực hiện: {dateLabel(state.service.date)} ·{" "}
                      {money(state.service.cost)}
                    </Typography>
                    <Typography
                      variant="caption"
                      color="text.secondary"
                      sx={{ display: "block" }}
                    >
                      Kỳ tiếp theo đã được tính lại từ dịch vụ gần nhất.
                    </Typography>
                  </Box>
                )}
              </Box>
            )
          })}
        </Box>
      ) : (
        <Box className="panel">
          <Empty
            title={
              status === "attention"
                ? "Không có nhắc nhở cần xử lý"
                : "Chưa có kế hoạch phù hợp"
            }
            description="Tạo kế hoạch hoặc thay đổi bộ lọc để xem hạng mục khác."
            action={
              <Button onClick={() => open({ type: "plan" })}>
                Tạo kế hoạch mới
              </Button>
            }
          />
        </Box>
      )}
      {reminders && (
        <Box className="panel section-space">
          <Typography variant="subtitle1">Tùy chọn nhắc nhở</Typography>
          <FormControlLabel
            control={
              <Switch
                checked={data.settings.reminders}
                disabled={pending}
                onChange={(_, value) => {
                  void save(
                    (d) => {
                      d.settings.reminders = value
                    },
                    value
                      ? "Đã bật nhắc bảo dưỡng trong ứng dụng."
                      : "Đã tắt nhắc bảo dưỡng trong ứng dụng.",
                  )
                }}
              />
            }
            label={
              <Typography variant="body2">
                Hiện số nhắc nhở trên thanh điều hướng
              </Typography>
            }
          />
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ display: "block" }}
          >
            Các bản ghi và kế hoạch vẫn được giữ nguyên khi tắt nhắc nhở.
          </Typography>
        </Box>
      )}
    </Box>
  )
}
export function Reports() {
  const { data, selectedVehicle, month, notify } = useGarage()
  if (!data) return null
  const entries = currentEntries(data, selectedVehicle, month)
  const fuel = entries
      .filter((e) => e.kind === "fuel")
      .reduce((s, e) => s + e.cost, 0),
    service = entries
      .filter((e) => e.kind === "service")
      .reduce((s, e) => s + e.cost, 0)
  const km = driven(data, selectedVehicle, month)
  const exportCsv = () => {
    const escape = (s: string) => `"${s.replace(/"/g, '""')}"`
    const lines = [
      [
        "Ngày",
        "Phương tiện",
        "Biển số",
        "Loại",
        "Nội dung",
        "ODO (km)",
        "Chi phí (VNĐ)",
        "Số lít",
        "Ghi chú",
      ],
      ...[...entries]
        .sort((a, b) => b.date.localeCompare(a.date))
        .map((e) => {
          const v = data.vehicles.find((v) => v.id === e.vehicleId)
          return [
            dateLabel(e.date),
            v?.name ?? "",
            v?.plate ?? "",
            e.kind === "fuel"
              ? "Nhiên liệu"
              : e.kind === "service"
                ? "Dịch vụ"
                : "ODO",
            e.title,
            String(e.odo),
            String(e.cost),
            String(e.liters),
            e.note,
          ]
        }),
    ]
    const content =
      "\uFEFF" + lines.map((line) => line.map(escape).join(",")).join("\r\n")
    const url = URL.createObjectURL(
      new Blob([content], { type: "text/csv;charset=utf-8;" }),
    )
    const link = document.createElement("a")
    link.href = url
    link.download = `VehicleCare-${month === "all" ? "toan-bo" : month}.csv`
    link.click()
    URL.revokeObjectURL(url)
    notify(`Đã xuất ${entries.length} bản ghi theo bộ lọc hiện tại.`)
  }
  return (
    <Box className="route-fade">
      <Heading
        title="Báo cáo & thống kê"
        subtitle="Bức tranh chi phí rõ ràng cho những quyết định thông minh hơn."
      >
        <Button
          variant="outlined"
          startIcon={<FileDownloadOutlined />}
          onClick={exportCsv}
          disabled={!entries.length}
        >
          Xuất CSV
        </Button>
      </Heading>
      <Box className="filters">
        <VehicleFilter />
        <MonthFilter />
        <Typography variant="caption" color="text.secondary">
          Đơn vị: VNĐ · km · lít
        </Typography>
      </Box>
      <Box className="stats-grid">
        <Stat
          title="Tổng chi phí"
          value={money(fuel + service)}
          icon={<AccountBalanceWalletOutlined />}
          foot={`${entries.filter((e) => e.kind !== "odo").length} giao dịch`}
        />
        <Stat
          title="Nhiên liệu"
          value={money(fuel)}
          icon={<LocalGasStationOutlined />}
          tone="green"
          foot={`${number(entries.reduce((s, e) => s + e.liters, 0))} lít nhiên liệu`}
        />
        <Stat
          title="Bảo dưỡng & sửa chữa"
          value={money(service)}
          icon={<BuildOutlined />}
          tone="amber"
          foot={`${entries.filter((e) => e.kind === "service").length} dịch vụ`}
        />
        <Stat
          title="Chi phí / quãng đường"
          value={km ? `${number(Math.round((fuel + service) / km))} ₫` : "—"}
          icon={<SpeedOutlined />}
          foot={km ? `${number(km)} km đã ghi nhận` : "Cần dữ liệu ODO để tính"}
        />
      </Box>
      <Box className="dashboard-grid">
        <Box className="panel">
          <Box className="panel-header">
            <Typography variant="h6">Biến động chi phí 6 tháng</Typography>
          </Box>
          <Box className="chart-summary">
            <Box className="chart-legend">
              <Box className="legend-dot" />
              Nhiên liệu
            </Box>
            <Box className="chart-legend">
              <Box className="legend-dot green" />
              Bảo dưỡng
            </Box>
          </Box>
          <Box className="chart-wrap" sx={{ height: 300 }}>
            <ExpenseChart
              entries={currentEntries(data, selectedVehicle, "all")}
              endMonth={month}
            />
          </Box>
          <Typography variant="caption" color="text.secondary">
            Biểu đồ hiển thị 6 tháng kết thúc tại tháng đang chọn.
          </Typography>
        </Box>
        <Box className="panel">
          <Typography variant="h6">Cơ cấu chi phí</Typography>
          <Box sx={{ height: 300, pt: 2 }}>
            {fuel + service ? (
              <ExpenseChart entries={entries} doughnut />
            ) : (
              <Empty description="Chưa có chi phí trong khoảng thời gian này." />
            )}
          </Box>
        </Box>
      </Box>
      <Box className="panel section-space">
        <Typography variant="h6" sx={{ mb: 2 }}>
          Chi phí theo phương tiện
        </Typography>
        {data.vehicles
          .filter((v) => selectedVehicle === "all" || v.id === selectedVehicle)
          .map((v) => {
            const cost = entries
              .filter((e) => e.vehicleId === v.id)
              .reduce((s, e) => s + e.cost, 0)
            return (
              <Box
                key={v.id}
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                  py: 2,
                  borderBottom: "1px solid var(--border-secondary)",
                  gap: 2,
                }}
              >
                <Box>
                  <Typography variant="subtitle2">{v.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {v.plate}
                  </Typography>
                </Box>
                <Box sx={{ textAlign: "right" }}>
                  <Typography variant="subtitle2">{money(cost)}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {fuel + service
                      ? number((cost / (fuel + service)) * 100)
                      : 0}
                    % tổng chi phí
                  </Typography>
                </Box>
              </Box>
            )
          })}
      </Box>
      <Alert severity="info" sx={{ mt: 3 }}>
        Số liệu được tính từ các bản ghi thủ công. Chi phí/km bao gồm nhiên liệu
        và dịch vụ; không phải chỉ số tiêu hao nhiên liệu thực tế. ODO và chi
        phí sẽ tính lại khi lịch sử thay đổi.
      </Alert>
    </Box>
  )
}
const profileSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Tên phải có ít nhất 2 ký tự.")
    .max(60, "Tối đa 60 ký tự."),
  phone: z
    .string()
    .trim()
    .refine(
      (v) => !v || /^0\d{9}$/.test(v.replace(/\s/g, "")),
      "Nhập số điện thoại Việt Nam gồm 10 chữ số.",
    ),
})
export function Profile() {
  const { data, session, save, pending, open } = useGarage()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<z.infer<typeof profileSchema>>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      name: data?.profile.name ?? "",
      phone: data?.profile.phone ?? "",
    },
  })
  useEffect(() => {
    if (data) reset({ name: data.profile.name, phone: data.profile.phone })
  }, [data, reset])
  if (!session)
    return (
      <Box className="panel">
        <Empty
          title="Hồ sơ của riêng bạn"
          description="Đăng nhập để quản lý hồ sơ và dữ liệu cá nhân."
          action={
            <Button variant="contained" onClick={() => open({ type: "login" })}>
              Đăng nhập
            </Button>
          }
        />
      </Box>
    )
  return (
    <Box className="route-fade">
      <Heading
        title="Hồ sơ cá nhân"
        subtitle="Thông tin tài khoản và không gian chăm sóc xe của bạn."
      />
      <Box className="settings-stack">
        <Box
          className="panel"
          sx={{ display: "flex", alignItems: "center", gap: 2.5 }}
        >
          <Avatar
            sx={{
              width: 64,
              height: 64,
              bgcolor: "var(--brand-secondary)",
              color: "primary.main",
            }}
          >
            {data?.profile.name
              .split(" ")
              .slice(-2)
              .map((n) => n[0])
              .join("")}
          </Avatar>
          <Box>
            <Typography variant="h6">{data?.profile.name}</Typography>
            <Chip
              label="Tài khoản trải nghiệm"
              size="small"
              color="primary"
              variant="outlined"
              sx={{ mt: 1 }}
            />
          </Box>
        </Box>
        <Box
          component="form"
          className="panel"
          noValidate
          onSubmit={handleSubmit(async (values) => {
            await save((d) => {
              d.profile.name = values.name
              d.profile.phone = values.phone
            }, "Đã cập nhật hồ sơ cá nhân.")
          })}
        >
          <Typography variant="h6" sx={{ mb: 3 }}>
            Thông tin cá nhân
          </Typography>
          <Box className="form-grid">
            <TextField
              label="Họ và tên *"
              {...register("name")}
              error={!!errors.name}
              helperText={errors.name?.message}
            />
            <TextField
              label="Số điện thoại"
              {...register("phone")}
              error={!!errors.phone}
              helperText={errors.phone?.message}
            />
            <TextField
              className="full"
              label="Email đăng nhập"
              value={session.email}
              disabled
              helperText="Email không thể thay đổi sau khi đăng ký."
            />
          </Box>
          <Button
            variant="contained"
            type="submit"
            disabled={pending}
            sx={{ mt: 3 }}
          >
            {pending ? "Đang lưu..." : "Lưu thông tin"}
          </Button>
        </Box>
        <Alert severity="info">
          Xác thực tài khoản và phiên đăng nhập được lưu ở backend. Dữ liệu garage
          hiện vẫn là dữ liệu mẫu trong bộ nhớ cho tới khi business API hoàn tất.
        </Alert>
      </Box>
    </Box>
  )
}
export function Settings() {
  const { data, session, save, pending, open } = useGarage()
  const [days, setDays] = useState(data?.settings.thresholdDays ?? 30)
  const [km, setKm] = useState(data?.settings.thresholdKm ?? 500)
  const [reminders, setReminders] = useState(data?.settings.reminders ?? true)
  const [error, setError] = useState("")
  useEffect(() => {
    if (data) {
      setDays(data.settings.thresholdDays)
      setKm(data.settings.thresholdKm)
      setReminders(data.settings.reminders)
    }
  }, [data])
  return (
    <Box className="route-fade">
      <Heading
        title="Cài đặt"
        subtitle="Điều chỉnh VehicleCare theo cách bạn chăm sóc xe."
      />
      <Box className="settings-stack">
        <Box
          className="panel"
          component="form"
          noValidate
          onSubmit={async (e) => {
            e.preventDefault()
            setError("")
            if (
              !Number.isInteger(days) ||
              days < 1 ||
              days > 365 ||
              !Number.isFinite(km) ||
              km < 1 ||
              km > 100000
            ) {
              setError("Số ngày từ 1–365 và số km từ 1–100.000.")
              return
            }
            await save((d) => {
              d.settings = { reminders, thresholdDays: days, thresholdKm: km }
            }, "Đã lưu cài đặt nhắc bảo dưỡng.")
          }}
        >
          <Typography variant="h6">Nhắc bảo dưỡng</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Chủ động nhắc trước khi đến hạn, theo điều kiện nào đến trước.
          </Typography>
          <FormControlLabel
            sx={{ my: 2 }}
            control={
              <Switch
                checked={reminders}
                onChange={(_, value) => setReminders(value)}
              />
            }
            label={
              <Typography variant="body2">
                Bật nhắc nhở trong ứng dụng
              </Typography>
            }
          />
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          <Box className="form-grid">
            <TextField
              type="number"
              label="Nhắc trước (ngày)"
              value={days}
              onChange={(e) => setDays(Number(e.target.value))}
            />
            <TextField
              type="number"
              label="Nhắc trước (km)"
              value={km}
              onChange={(e) => setKm(Number(e.target.value))}
            />
          </Box>
          <Button
            type="submit"
            variant="contained"
            disabled={pending}
            sx={{ mt: 3 }}
          >
            {pending ? "Đang lưu..." : "Lưu cài đặt"}
          </Button>
        </Box>
        <Box className="panel">
          <Typography variant="h6" sx={{ mb: 2 }}>
            Ngôn ngữ & định dạng
          </Typography>
          {[
            ["Ngôn ngữ", "Tiếng Việt"],
            ["Đơn vị tiền tệ", "Việt Nam đồng (VNĐ)"],
            ["Quãng đường / nhiên liệu", "Kilômét (km) / Lít"],
            ["Ngày tháng", "dd/MM/yyyy"],
            ["Múi giờ", "Asia/Ho_Chi_Minh (UTC+7)"],
          ].map(([label, value]) => (
            <Box
              key={label}
              sx={{
                display: "flex",
                justifyContent: "space-between",
                gap: 2,
                py: 1.5,
              }}
            >
              <Typography variant="body2" color="text.secondary">
                {label}
              </Typography>
              <Typography variant="body2">{value}</Typography>
            </Box>
          ))}
        </Box>
        <Box className="panel">
          <Box sx={{ display: "flex", gap: 1, alignItems: "center", mb: 1.5 }}>
            <ShieldOutlined color="primary" />
            <Typography variant="h6">Quyền riêng tư & dữ liệu</Typography>
          </Box>
          <Typography variant="body2" color="text.secondary">
            Dữ liệu garage hiện chưa gửi đến máy chủ. Phiên đăng nhập dùng cookie
            HttpOnly và session backend; garage sẽ trở về bộ mẫu khi tải lại trang.
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
            Không gửi nhắc nhở khi web đã đóng. Ảnh minh họa từ Unsplash, không
            đại diện chính xác mẫu xe.
          </Typography>
          <Button sx={{ mt: 2 }} onClick={() => open({ type: "help" })}>
            Xem hướng dẫn sử dụng{" "}
            <ArrowForwardRounded sx={{ fontSize: 16, ml: 1 }} />
          </Button>
        </Box>
        {!session && (
          <Alert severity="info">
            Bạn đang xem bản trải nghiệm. Cần đăng nhập để lưu thay đổi cài đặt
            trong phiên hiện tại.
          </Alert>
        )}
      </Box>
    </Box>
  )
}
export function NotFound() {
  const navigate = useNavigate()
  return (
    <Box className="panel">
      <Empty
        title="Không tìm thấy trang"
        description="Đường dẫn này không tồn tại trong VehicleCare."
        action={
          <Button variant="contained" onClick={() => navigate("/")}>
            Về tổng quan
          </Button>
        }
      />
    </Box>
  )
}
