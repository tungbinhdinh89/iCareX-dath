import MoreVertRounded from "@mui/icons-material/MoreVertRounded"
import DirectionsCarOutlined from "@mui/icons-material/DirectionsCarOutlined"
import { fuelTypeLabels, vehicleRoleLabels, vehicleMaintenanceSummary } from "@/lib/models/vehiclePresentation"
import { useMemo, useState } from "react"
import {
  Box,
  Button,
  CardActionArea,
  IconButton,
  MenuItem,
  Menu,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material"
import SpeedOutlined from "@mui/icons-material/SpeedOutlined"
import LocalGasStationOutlined from "@mui/icons-material/LocalGasStationOutlined"
import BuildOutlined from "@mui/icons-material/BuildOutlined"
import EditOutlined from "@mui/icons-material/EditOutlined"
import DeleteOutline from "@mui/icons-material/DeleteOutline"
import ChevronRight from "@mui/icons-material/ChevronRight"
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded"
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Filler,
  Tooltip as ChartTooltip,
  Legend,
} from "chart.js"
import { Line, Doughnut } from "react-chartjs-2"
import { useNavigate } from "react-router"
import {
  dateLabel,
  monthLabel,
  money,
  number,
  odoOf,
  odometerUnitLabel,
  planState,
  today,
  type Entry,
  type Garage,
  type Plan,
  type Vehicle,
} from "@/lib/models/garage"
import { useGarage } from "@/lib/state/store"
import MaintenanceStatusChip from "@/components/MaintenanceStatusChip"
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Filler,
  ChartTooltip,
  Legend,
)
export const kindLabel = (kind: Entry["kind"]) =>
  kind === "fuel"
    ? "Nhiên liệu"
    : kind === "service"
      ? "Bảo dưỡng"
      : "Cập nhật ODO"

export function KindIcon({ kind }: { kind: Entry["kind"] }) {
  return (
    <Box
      className={`icon-box ${
        kind === "service" ? "amber" : kind === "odo" ? "green" : ""
      }`}
    >
      {kind === "fuel" ? (
        <LocalGasStationOutlined fontSize="small" />
      ) : kind === "service" ? (
        <BuildOutlined fontSize="small" />
      ) : (
        <SpeedOutlined fontSize="small" />
      )}
    </Box>
  )
}

export function Empty({
  title = "Chưa có dữ liệu",
  description = "Thêm bản ghi đầu tiên để bắt đầu theo dõi.",
  action,
}: {
  title?: string
  description?: string
  action?: React.ReactNode
}) {
  return (
    <Box className="empty">
      <Box className="empty-illustration">
        <DirectionsCarOutlined sx={{ fontSize: 32 }} />
      </Box>
      <Typography variant="subtitle1" color="text.primary">
        {title}
      </Typography>
      <Typography variant="body2" sx={{ mt: 0.5, mb: 2 }}>
        {description}
      </Typography>
      {action}
    </Box>
  )
}

export function VehicleFilter() {
  const { data, selectedVehicle, setSelectedVehicle } = useGarage()
  return (
    <TextField
      select
      label="Phương tiện"
      value={selectedVehicle}
      onChange={(e) => setSelectedVehicle(e.target.value)}
    >
      <MenuItem value="all">Tất cả phương tiện</MenuItem>
      {data?.vehicles.map((v) => (
        <MenuItem key={v.id} value={v.id}>
          {v.name} · {v.plate}
        </MenuItem>
      ))}
    </TextField>
  )
}

export function MonthFilter({ all = true }: { all?: boolean }) {
  const { month, setMonth, data } = useGarage()
  const months = useMemo(
    () =>
      Array.from(
        new Set([
          today().slice(0, 7),
          ...(data?.entries.map((e) => e.date.slice(0, 7)) ?? []),
        ]),
      )
        .sort()
        .reverse(),
    [data],
  )
  return (
    <TextField
      select
      label="Thời gian"
      value={month === "all" && !all ? today().slice(0, 7) : month}
      onChange={(e) => setMonth(e.target.value)}
    >
      {all && <MenuItem value="all">Tất cả thời gian</MenuItem>}
      {months.map((m) => (
        <MenuItem key={m} value={m}>
          {monthLabel(m)}
        </MenuItem>
      ))}
    </TextField>
  )
}

export function VehicleCard({ vehicle }: { vehicle: Vehicle }) {
  const { data, open, can, canManageOdo, permissions } = useGarage()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const navigate = useNavigate()
  if (!data || !can('vehicle.read', vehicle.id)) return null
  const membership = permissions?.vehicles.find((item) => item.vehicleId === vehicle.id)
  const maintenance = vehicleMaintenanceSummary(data, vehicle)
  const canEdit = can('vehicle.update', vehicle.id)
  const canMembers = can('vehicle.member.invite', vehicle.id)
  const canArchive = can('vehicle.archive', vehicle.id)
  const odo = canManageOdo(vehicle.id)
  const hasMenu = canEdit || canMembers || canArchive || odo
  const action = (run: () => void) => { setAnchor(null); run() }
  return (
    <Box className="vehicle-card vehicle-card-v2">
      <CardActionArea onClick={() => navigate(`/xe/${vehicle.id}`)} aria-label={`Xem chi tiết ${vehicle.name}`}>
        <Box component="img" src={vehicle.image} alt={`Ảnh ${vehicle.name}`} className="vehicle-card-image" />
        <Box className="vehicle-body">
          <Box>
            <Typography component="h3" variant="subtitle1">{vehicle.name}</Typography>
            <Typography variant="body2" color="text.secondary">{vehicle.vehicleBrandName} {vehicle.vehicleModelName}</Typography>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
              {[vehicle.plate, vehicle.year, vehicle.type].filter(Boolean).join(' · ')}
            </Typography>
          </Box>
          <Box sx={{ mt: 2 }}>
            <Typography variant="caption" color="text.secondary">ODO HIỆN TẠI</Typography>
            <Typography variant="h5" sx={{ mt: 0.5 }}>{number(odoOf(vehicle, data.entries))} <Box component="span" sx={{ fontSize: 14, fontWeight: 400 }}>{odometerUnitLabel(vehicle)}</Box></Typography>
          </Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1, flexWrap: 'wrap', mt: 2 }}>
            <Box><Typography variant="caption" color="text.secondary">Nhiên liệu</Typography><Typography variant="body2">{fuelTypeLabels[vehicle.fuelType] ?? vehicle.fuelType}</Typography></Box>
            {membership && <Chip variant="outlined" size="small" label={vehicleRoleLabels[membership.role] ?? membership.role} />}
          </Box>
          {vehicle.isArchived && <Chip size="small" label="Đã lưu trữ" sx={{ mt: 2 }} />}
          {maintenance && <Box sx={{ mt: 2 }}><Chip size="small" color={maintenance.status === 'Overdue' ? 'error' : maintenance.status === 'Due' || maintenance.status === 'Upcoming' ? 'warning' : 'default'} label={maintenance.label} />
            {maintenance.title && <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>{maintenance.title}{maintenance.remaining != null && maintenance.remaining > 0 ? ` · Còn ${number(maintenance.remaining)} ${odometerUnitLabel(vehicle)}` : ''}</Typography>}
          </Box>}
        </Box>
      </CardActionArea>
      <Box className="vehicle-card-actions">
        <Button onClick={() => navigate(`/xe/${vehicle.id}`)} endIcon={<ChevronRight />}>Xem chi tiết</Button>
        {hasMenu && <IconButton aria-label={`Thao tác với ${vehicle.name}`} aria-haspopup="menu" aria-expanded={!!anchor} onClick={(event) => setAnchor(event.currentTarget)} sx={{ width: 44, height: 44 }}><MoreVertRounded /></IconButton>}
      </Box>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        {canEdit && <MenuItem onClick={() => action(() => open({ type: 'vehicle', vehicle }))}>Sửa thông tin xe</MenuItem>}
        {odo && <MenuItem onClick={() => action(() => open({ type: 'entry', kind: 'odo', vehicleId: vehicle.id }))}>Cập nhật ODO</MenuItem>}
        {canMembers && <MenuItem onClick={() => action(() => navigate(`/xe/${vehicle.id}/thanh-vien`))}>Quản lý thành viên</MenuItem>}
        {canArchive && <MenuItem onClick={() => action(() => open({ type: 'delete', entity: 'vehicle', id: vehicle.id, title: vehicle.name }))}>Lưu trữ xe</MenuItem>}
      </Menu>
    </Box>
  )
}

export function Reminder({
  plan,
  data,
  compact = false,
}: {
  plan: Plan
  data: Garage
  compact?: boolean
}) {
  const state = planState(plan, data)
  const vehicle = data.vehicles.find((v) => v.id === plan.vehicleId)
  return (
    <Box className="reminder">
      <Box className="reminder-title">
        <Box>
          <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
            {plan.title}
          </Typography>
          <Typography
            sx={{ fontSize: "var(--text-meta-size)", color: "text.secondary" }}
          >
            {vehicle?.name} · {vehicle?.plate}
          </Typography>
        </Box>
        <MaintenanceStatusChip
          status={
            state.status === "overdue"
              ? "overdue"
              : state.status === "soon"
                ? "soon"
                : "notDue"
          }
        />
      </Box>
      <Box
        sx={{ display: "flex", justifyContent: "space-between", mt: 1, gap: 1 }}
      >
        <Typography
          sx={{ fontSize: "var(--text-meta-size)", color: "text.secondary" }}
        >
          {state.days < 0
            ? `Quá ${Math.abs(state.days)} ngày`
            : `Còn ${state.days} ngày`}{" "}
          ·{" "}
          {state.km < 0
            ? `Vượt ${number(-state.km)} km`
            : `Còn ${number(state.km)} km`}
        </Typography>
      </Box>
      {!compact && (
        <Typography variant="caption" color="text.secondary">
          Hạn: {dateLabel(state.dueDate)} hoặc {number(state.dueOdo)} km
        </Typography>
      )}
    </Box>
  )
}

export function HistoryTable({
  entries,
  editable = true,
}: {
  entries: Entry[]
  editable?: boolean
}) {
  const { data, open, canManageOdo } = useGarage()
  if (!data) return null
  if (!entries.length) return <Empty />
  const hasActions =
    editable &&
    entries.some(
      (entry) => entry.kind === "odo" && canManageOdo(entry.vehicleId),
    )
  return (
    <Box className="activity-views">
      <TableContainer className="activity-table">
        <Table size="small" aria-label="Lịch sử chăm sóc xe">
          <TableHead>
            <TableRow>
              <TableCell>Hoạt động</TableCell>
              <TableCell>Phương tiện</TableCell>
              <TableCell>Ngày ghi nhận</TableCell>
              <TableCell align="right">ODO</TableCell>
              <TableCell align="right">Chi phí</TableCell>
              {hasActions && <TableCell align="right">Thao tác</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {entries.map((entry) => {
              const vehicle = data.vehicles.find(
                (v) => v.id === entry.vehicleId,
              )
              return (
                <TableRow key={entry.id} hover>
                  <TableCell>
                    <Box className="activity-label">
                      <KindIcon kind={entry.kind} />
                      <Box>
                        <Typography
                          variant="body2"
                          sx={{ fontWeight: 500, whiteSpace: "nowrap" }}
                        >
                          {entry.title}
                        </Typography>
                        <Typography
                          sx={{
                            fontSize: "var(--text-meta-size)",
                            color: "text.secondary",
                          }}
                        >
                          {entry.kind === "fuel"
                            ? `${number(entry.liters)} lít · `
                            : ""}
                          {entry.note || kindLabel(entry.kind)}
                          {entry.planId ? " · Theo kế hoạch" : ""}
                        </Typography>
                      </Box>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography
                      sx={{
                        fontSize: "var(--text-small-size)",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {vehicle?.name}
                    </Typography>
                    <Typography
                      sx={{
                        fontSize: "var(--text-meta-size)",
                        color: "text.secondary",
                      }}
                    >
                      {vehicle?.plate}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>
                    {dateLabel(entry.date)}
                  </TableCell>
                  <TableCell align="right">{number(entry.odo)} {odometerUnitLabel(data.vehicles.find((v) => v.id === entry.vehicleId))}</TableCell>
                  <TableCell
                    align="right"
                    sx={{ fontWeight: 600, whiteSpace: "nowrap" }}
                  >
                    {entry.kind === "odo" ? "—" : money(entry.cost)}
                  </TableCell>
                  {hasActions && (
                    <TableCell align="right" sx={{ whiteSpace: "nowrap" }}>
                      {entry.kind === "odo" &&
                        canManageOdo(entry.vehicleId) && (
                          <>
                            <Tooltip title="Chỉnh sửa">
                              <IconButton
                                size="small"
                                aria-label={`Sửa ${entry.title}`}
                                onClick={() =>
                                  open({
                                    type: "entry",
                                    kind: entry.kind,
                                    entry,
                                  })
                                }
                              >
                                <EditOutlined sx={{ fontSize: 17 }} />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Xóa bản ghi">
                              <IconButton
                                size="small"
                                aria-label={`Xóa ${entry.title}`}
                                onClick={() =>
                                  open({
                                    type: "delete",
                                    entity: "entry",
                                    id: entry.id,
                                    title: entry.title,
                                  })
                                }
                              >
                                <DeleteOutline sx={{ fontSize: 17 }} />
                              </IconButton>
                            </Tooltip>
                          </>
                        )}
                    </TableCell>
                  )}
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </TableContainer>
      <Box
        className="activity-card-list"
        role="list"
        aria-label="Lịch sử chăm sóc xe"
      >
        {entries.map((entry) => {
          const vehicle = data.vehicles.find((v) => v.id === entry.vehicleId)
          return (
            <Box className="activity-card" role="listitem" key={entry.id}>
              <Box className="activity-card-heading">
                <Typography variant="subtitle2">
                  {dateLabel(entry.date)} · {kindLabel(entry.kind)}
                </Typography>
                <KindIcon kind={entry.kind} />
              </Box>
              <Typography variant="body2">{entry.title}</Typography>
              <Typography variant="caption" color="text.secondary">
                {vehicle?.name} · {vehicle?.plate}
              </Typography>
              {entry.note && (
                <Typography variant="body2" color="text.secondary">
                  {entry.note}
                </Typography>
              )}
              <Box className="activity-card-values">
                <Typography variant="body2" color="text.secondary">
                  {number(entry.odo)} {odometerUnitLabel(data.vehicles.find((v) => v.id === entry.vehicleId))}
                </Typography>
                <Typography variant="subtitle2">
                  {entry.kind === "odo" ? "—" : money(entry.cost)}
                </Typography>
              </Box>
              {editable &&
                entry.kind === "odo" &&
                canManageOdo(entry.vehicleId) && (
                  <Box className="activity-card-actions">
                    <Button
                      size="small"
                      startIcon={<EditOutlined />}
                      onClick={() =>
                        open({ type: "entry", kind: entry.kind, entry })
                      }
                      aria-label={`Sửa ${entry.title}`}
                    >
                      Sửa
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      startIcon={<DeleteOutline />}
                      onClick={() =>
                        open({
                          type: "delete",
                          entity: "entry",
                          id: entry.id,
                          title: entry.title,
                        })
                      }
                      aria-label={`Xóa ${entry.title}`}
                    >
                      Xóa
                    </Button>
                  </Box>
                )}
            </Box>
          )
        })}
      </Box>
    </Box>
  )
}
function chartColors() {
  const styles = getComputedStyle(document.documentElement)
  const get = (key: string) => styles.getPropertyValue(key).trim()
  return {
    blue: get("--brand-primary"),
    green: get("--accent"),
    muted: get("--text-secondary"),
    grid: get("--border-secondary"),
    font: get("--font-sans"),
    soft: get("--surface-hover"),
  }
}

export function ExpenseChart({
  entries,
  endMonth,
  doughnut = false,
}: {
  entries: Entry[]
  endMonth?: string
  doughnut?: boolean
}) {
  const c = chartColors()
  const end = endMonth && endMonth !== "all" ? endMonth : today().slice(0, 7)
  const keys = Array.from({ length: 6 }, (_, i) => {
    const d = new Date(`${end}-15T00:00:00Z`)
    d.setUTCMonth(d.getUTCMonth() - 5 + i)
    return d.toISOString().slice(0, 7)
  })
  const total = (kind: string) =>
    entries.filter((e) => e.kind === kind).reduce((sum, e) => sum + e.cost, 0)
  if (doughnut)
    return (
      <Doughnut
        data={{
          labels: ["Nhiên liệu", "Bảo dưỡng & sửa chữa"],
          datasets: [
            {
              data: [total("fuel"), total("service")],
              backgroundColor: [c.blue, c.green],
              borderWidth: 0,
              spacing: 4,
              borderRadius: 4,
            },
          ],
        }}
        options={{
          maintainAspectRatio: false,
          cutout: "75%",
          plugins: {
            legend: {
              position: "bottom",
              labels: {
                usePointStyle: true,
                pointStyle: "circle",
                padding: 24,
                font: { family: c.font, size: 11 },
              },
            },
            tooltip: {
              titleFont: { family: c.font },
              bodyFont: { family: c.font },
              callbacks: {
                label: (item) => `${item.label}: ${money(Number(item.raw))}`,
              },
            },
          },
        }}
      />
    )
  return (
    <Line
      data={{
        labels: keys.map((key) => `Tháng ${Number(key.slice(5))}`),
        datasets: [
          {
            label: "Nhiên liệu",
            data: keys.map((key) =>
              entries
                .filter((e) => e.kind === "fuel" && e.date.startsWith(key))
                .reduce((s, e) => s + e.cost, 0),
            ),
            borderColor: c.blue,
            backgroundColor: (context) => {
              const chart = context.chart
              const area = chart.chartArea
              if (!area) return c.soft
              const gradient = chart.ctx.createLinearGradient(
                0,
                area.top,
                0,
                area.bottom,
              )
              gradient.addColorStop(0, `${c.blue}20`)
              gradient.addColorStop(1, `${c.blue}00`)
              return gradient
            },
            fill: true,
            tension: 0.4,
            pointRadius: 3,
            pointHoverRadius: 6,
            pointBackgroundColor: c.blue,
            pointBorderColor: "white",
            pointBorderWidth: 2,
            borderWidth: 2.5,
          },
          {
            label: "Bảo dưỡng",
            data: keys.map((key) =>
              entries
                .filter((e) => e.kind === "service" && e.date.startsWith(key))
                .reduce((s, e) => s + e.cost, 0),
            ),
            borderColor: c.green,
            backgroundColor: "transparent",
            tension: 0.4,
            pointRadius: 0,
            pointHoverRadius: 5,
            borderWidth: 2,
            borderDash: [5, 4],
          },
        ],
      }}
      options={{
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: "index" },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: getComputedStyle(document.documentElement)
              .getPropertyValue("--brand-dark")
              .trim(),
            padding: 12,
            titleFont: { family: c.font },
            bodyFont: { family: c.font },
            callbacks: {
              label: (item) =>
                `${item.dataset.label}: ${money(item.parsed.y ?? 0)}`,
            },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            border: { display: false },
            ticks: {
              color: c.muted,
              font: { family: c.font, size: 10 },
              maxRotation: 0,
            },
          },
          y: {
            beginAtZero: true,
            border: { display: false, dash: [3, 4] },
            grid: { color: c.grid, drawTicks: false },
            ticks: {
              color: c.muted,
              count: 5,
              padding: 10,
              font: { family: c.font, size: 10 },
              callback: (val) =>
                Number(val) === 0 ? "0" : `${number(Number(val) / 1000000)} tr`,
            },
          },
        },
      }}
    />
  )
}
