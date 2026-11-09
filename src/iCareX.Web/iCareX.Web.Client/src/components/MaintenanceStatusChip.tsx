import { Box, Chip } from "@mui/material"

export type MaintenanceStatus = "notDue" | "soon" | "due" | "overdue" | "unknown"

const styles = {
  notDue: { label: "Chưa đến hạn", tone: "success", symbol: "✓" },
  soon: { label: "Sắp đến hạn", tone: "warning", symbol: "△" },
  due: { label: "Đến hạn", tone: "due", symbol: "!" },
  overdue: { label: "Quá hạn", tone: "danger", symbol: "!" },
  unknown: { label: "Không đủ dữ liệu", tone: "unknown", symbol: "?" },
} satisfies Record<MaintenanceStatus, { label: string; tone: string; symbol: string }>

/** Presentation only: callers must supply a status from an actual evaluation. */
export default function MaintenanceStatusChip({ status }: { status: MaintenanceStatus }) {
  const value = styles[status]
  return (
    <Chip
      className="maintenance-status-chip"
      size="small"
      icon={<Box component="span" aria-hidden="true">{value.symbol}</Box>}
      label={value.label}
      sx={{
        color: `var(--${value.tone})`,
        bgcolor: `var(--${value.tone}-soft)`,
        flexShrink: 0,
        "& .MuiChip-icon": { color: "inherit", fontSize: "var(--text-meta-size)" },
      }}
    />
  )
}
