import LoginForm from "@/components/AuthForm"
export { default as LoginForm } from "@/components/AuthForm"
import { useEffect, useState } from "react"
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  InputAdornment,
  MenuItem,
  TextField,
  Typography,
} from "@mui/material"
import CloseRounded from "@mui/icons-material/CloseRounded"
import ShieldOutlined from "@mui/icons-material/ShieldOutlined"
import CheckCircleOutline from "@mui/icons-material/CheckCircleOutline"
import DirectionsCarOutlined from "@mui/icons-material/DirectionsCarOutlined"
import SpeedOutlined from "@mui/icons-material/SpeedOutlined"
import EventNoteOutlined from "@mui/icons-material/EventNoteOutlined"
import HelpOutlineRounded from "@mui/icons-material/HelpOutlineRounded"
import DeleteOutline from "@mui/icons-material/DeleteOutline"
import { useForm, Controller } from "react-hook-form"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import {
  dateLabel,
  defaultImage,
  number,
  odoOf,
  today,
  validateEntry,
  type Entry,
  type Plan,
  type Vehicle,
} from "@/lib/models/garage"
import { api } from "@/lib/apis/iCareXApi"
import { useGarage, type ModalState } from "@/lib/state/store"
const entrySchema = z.object({
  vehicleId: z.string().min(1, "Vui lòng chọn xe."),
  title: z
    .string()
    .trim()
    .min(2, "Nhập ít nhất 2 ký tự.")
    .max(100, "Tối đa 100 ký tự."),
  date: z
    .string()
    .regex(/^\d{2}\/\d{2}\/\d{4}$/, "Nhập ngày theo dd/MM/yyyy.")
    .refine((value) => {
      const iso = value.split("/").reverse().join("-")
      const date = new Date(`${iso}T12:00:00Z`)
      return (
        !Number.isNaN(date.getTime()) && date.toISOString().slice(0, 10) === iso
      )
    }, "Ngày không hợp lệ."),
  odo: z
    .number({ error: "Nhập số ODO." })
    .min(0, "ODO không được âm.")
    .max(9999999, "ODO quá lớn."),
  cost: z
    .number({ error: "Nhập chi phí." })
    .min(0, "Chi phí không được âm.")
    .max(10000000000, "Chi phí quá lớn."),
  liters: z
    .number({ error: "Nhập số lít." })
    .min(0, "Số lít không được âm.")
    .max(500, "Tối đa 500 lít."),
  note: z.string().max(500, "Tối đa 500 ký tự."),
})
type EntryFields = z.infer<typeof entrySchema>
function FormSection({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <Box className="full dialog-form-section">
      <Typography variant="subtitle2">{title}</Typography>
      <Typography variant="body2" color="text.secondary">
        {description}
      </Typography>
    </Box>
  )
}
function EntryForm({
  modal,
}: {
  modal: Extract<ModalState, { type: "entry" }>
}) {
  const { data, save, pending, setModal, refetch, notify, canManageOdo } =
    useGarage()
  const [error, setError] = useState("")
  const initialVehicle =
    modal.entry?.vehicleId ??
    modal.vehicleId ??
    data?.vehicles.find((vehicle) => canManageOdo(vehicle.id))?.id ??
    ""
  const vehicle = data?.vehicles.find((v) => v.id === initialVehicle)
  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
    setValue,
  } = useForm<EntryFields>({
    resolver: zodResolver(entrySchema),
    defaultValues: {
      vehicleId: initialVehicle,
      title:
        modal.entry?.title ??
        modal.plan?.title ??
        (modal.kind === "fuel"
          ? "Đổ xăng RON 95"
          : modal.kind === "odo"
            ? "Cập nhật ODO"
            : ""),
      date: dateLabel(modal.entry?.date ?? today()),
      odo:
        modal.entry?.odo ??
        (vehicle && data ? odoOf(vehicle, data.entries) : 0),
      cost: modal.entry?.cost ?? 0,
      liters: modal.entry?.liters ?? 0,
      note: modal.entry?.note ?? "",
    },
  })
  if (!data) return null
  const submit = handleSubmit(async (values) => {
    setError("")
    if (modal.kind !== "odo" || !canManageOdo(values.vehicleId)) {
      setError("Bạn không có quyền ghi ODO cho phương tiện này.")
      return
    }
    const entry: Entry = {
      ...values,
      date: values.date.split("/").reverse().join("-"),
      kind: modal.kind,
      id: modal.entry?.id ?? crypto.randomUUID(),
      planId: modal.entry?.planId ?? modal.plan?.id,
      cost: 0,
      liters: 0,
    }
    try {
      validateEntry(data, entry)
    } catch (e) {
      setError((e as Error).message)
      return
    }
    if (modal.kind === "odo") {
      try {
        if (modal.entry) {
          await api.updateOdometerReading(entry.vehicleId, entry.id, {
            date: entry.date,
            odo: entry.odo,
            note: entry.note,
            version: modal.entry.version ?? 0,
          })
        } else {
          await api.createOdometerReading(entry.vehicleId, {
            date: entry.date,
            odo: entry.odo,
            note: entry.note,
          })
        }
        await refetch()
        notify(modal.entry ? "Đã cập nhật ODO." : "Đã thêm bản ghi ODO.")
        setModal(null)
      } catch (e) {
        setError(e instanceof Error ? e.message : "Không thể lưu ODO.")
        notify(e instanceof Error ? e.message : "Không thể lưu ODO.", "error")
      }
      return
    }
    const ok = await save(
      (d) => {
        validateEntry(d, entry)
        const index = d.entries.findIndex((e) => e.id === entry.id)
        if (index >= 0) d.entries[index] = entry
        else d.entries.push(entry)
      },
      modal.plan
        ? "Đã ghi nhận dịch vụ và tính lại kỳ bảo dưỡng tiếp theo."
        : modal.entry
          ? "Đã cập nhật lịch sử và tính lại dữ liệu liên quan."
          : "Đã thêm bản ghi thành công.",
    )
    if (ok) setModal(null)
  })
  return (
    <Box component="form" id="entry-form" onSubmit={submit} noValidate>
      <DialogContent>
        {modal.plan && (
          <Alert severity="info" sx={{ mb: 2 }}>
            Hoàn tất qua bản ghi dịch vụ có chi phí. Chi phí chỉ được tính một
            lần trong lịch sử.
          </Alert>
        )}
        {!data.vehicles.length && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            Bạn cần thêm phương tiện trước khi ghi nhận.
          </Alert>
        )}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Box className="form-grid">
          <FormSection
            title="Thông tin ghi nhận"
            description="Chọn phương tiện và ghi lại số km tại thời điểm thực hiện."
          />
          <Controller
            name="vehicleId"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="Phương tiện *"
                disabled={!!modal.entry || !!modal.plan}
                error={!!errors.vehicleId}
                helperText={errors.vehicleId?.message}
                className="full"
                onChange={(e) => {
                  field.onChange(e)
                  const v = data.vehicles.find((v) => v.id === e.target.value)
                  if (v) setValue("odo", odoOf(v, data.entries))
                }}
              >
                {data.vehicles
                  .filter((v) => canManageOdo(v.id))
                  .map((v) => (
                    <MenuItem value={v.id} key={v.id}>
                      {v.name} · {v.plate}
                    </MenuItem>
                  ))}
              </TextField>
            )}
          />
          <TextField
            className="full"
            label="Nội dung *"
            {...register("title")}
            error={!!errors.title}
            helperText={errors.title?.message}
          />
          <TextField
            label="Ngày ghi nhận *"
            placeholder="dd/MM/yyyy"
            {...register("date")}
            error={!!errors.date}
            helperText={
              errors.date?.message ?? "Định dạng dd/MM/yyyy · Giờ Việt Nam"
            }
          />
          <TextField
            label="Số ODO *"
            type="number"
            {...register("odo", { valueAsNumber: true })}
            error={!!errors.odo}
            helperText={
              errors.odo?.message ?? "Phải nhất quán với lịch sử trước và sau"
            }
            slotProps={{
              input: {
                endAdornment: (
                  <InputAdornment position="end">km</InputAdornment>
                ),
              },
            }}
          />
          {modal.kind !== "odo" && (
            <TextField
              label="Tổng chi phí *"
              type="number"
              {...register("cost", { valueAsNumber: true })}
              error={!!errors.cost}
              helperText={errors.cost?.message}
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">VNĐ</InputAdornment>
                  ),
                },
              }}
            />
          )}
          {modal.kind === "fuel" && (
            <TextField
              label="Lượng nhiên liệu *"
              type="number"
              {...register("liters", { valueAsNumber: true })}
              error={!!errors.liters}
              helperText={errors.liters?.message}
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">lít</InputAdornment>
                  ),
                },
                htmlInput: { step: ".01" },
              }}
            />
          )}
          <TextField
            className="full"
            label={
              modal.kind === "service"
                ? "Garage / ghi chú"
                : "Địa điểm / ghi chú"
            }
            multiline
            minRows={2}
            {...register("note")}
            error={!!errors.note}
            helperText={errors.note?.message}
          />
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button
          color="inherit"
          onClick={() => setModal(null)}
          disabled={pending || isSubmitting}
        >
          Hủy
        </Button>
        <Button
          type="submit"
          variant="contained"
          disabled={pending || isSubmitting || !data.vehicles.length}
          startIcon={
            pending || isSubmitting ? (
              <CircularProgress size={16} color="inherit" />
            ) : undefined
          }
        >
          {pending || isSubmitting
            ? "Đang lưu..."
            : modal.plan
              ? "Lưu dịch vụ & hoàn tất"
              : "Lưu bản ghi"}
        </Button>
      </DialogActions>
    </Box>
  )
}
const vehicleSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Nhập tên xe (ít nhất 2 ký tự).")
    .max(60, "Tối đa 60 ký tự."),
  plate: z
    .string()
    .trim()
    .min(5, "Nhập biển số hợp lệ.")
    .max(20, "Tối đa 20 ký tự.")
    .regex(
      /^[0-9A-Za-z.\-\s]+$/,
      "Biển số chỉ gồm chữ, số, dấu chấm và gạch ngang.",
    ),
  vehicleTypeId: z.string().min(1, "Chọn loại phương tiện."),
  vehicleBrandId: z.string().min(1, "Chọn hãng xe."),
  vehicleModelId: z.string().min(1, "Chọn dòng xe."),
  year: z
    .number({ error: "Nhập năm sản xuất." })
    .int("Năm sản xuất phải là số nguyên.")
    .min(1980, "Năm từ 1980.")
    .max(new Date().getFullYear() + 1, "Năm không hợp lệ."),
  baseOdo: z
    .number({ error: "Nhập số ODO." })
    .min(0, "ODO không được âm.")
    .max(9999999, "ODO quá lớn."),
})
type VehicleFields = z.infer<typeof vehicleSchema>
function VehicleForm({ vehicle }: { vehicle?: Vehicle }) {
  const { data, notify, refetch, setModal } = useGarage()
  const [error, setError] = useState("")
  const [pending, setPending] = useState(false)
  const [types, setTypes] = useState<{ id: string; name: string }[]>([])
  const [brands, setBrands] = useState<{
    id: string
    vehicleTypeId: string
    name: string
  }[]>([])
  const [models, setModels] = useState<{
    id: string
    vehicleBrandId: string
    name: string
  }[]>([])
  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<VehicleFields>({
    resolver: zodResolver(vehicleSchema),
    defaultValues: vehicle ?? {
      name: "",
      plate: "",
      vehicleTypeId: "",
      vehicleBrandId: "",
      vehicleModelId: "",
      year: new Date().getFullYear(),
      baseOdo: 0,
    },
  })
  const selectedTypeId = watch("vehicleTypeId")
  const selectedBrandId = watch("vehicleBrandId")
  useEffect(() => {
    api
      .vehicleTypes()
      .then(setTypes)
      .catch((e) =>
        notify(
          e instanceof Error ? e.message : "Không thể tải loại xe.",
          "error",
        ),
      )
  }, [notify])
  useEffect(() => {
    if (!selectedTypeId) return
    api
      .vehicleBrands(selectedTypeId)
      .then((items) => {
        setBrands(items)
        if (!items.some((item) => item.id === selectedBrandId)) {
          setValue("vehicleBrandId", "")
          setValue("vehicleModelId", "")
        }
      })
      .catch((e) =>
        notify(
          e instanceof Error ? e.message : "Không thể tải hãng xe.",
          "error",
        ),
      )
  }, [selectedTypeId, notify])
  useEffect(() => {
    if (!selectedBrandId) return
    api
      .vehicleModels(selectedBrandId)
      .then((items) => {
        setModels(items)
        if (!items.some((item) => item.id === watch("vehicleModelId")))
          setValue("vehicleModelId", "")
      })
      .catch((e) =>
        notify(
          e instanceof Error ? e.message : "Không thể tải dòng xe.",
          "error",
        ),
      )
  }, [selectedBrandId, notify])
  const submit = handleSubmit(async (values) => {
    setError("")
    if (
      data?.vehicles.some(
        (v) =>
          v.id !== vehicle?.id &&
          v.plate.replace(/\W/g, "").toUpperCase() ===
            values.plate.replace(/\W/g, "").toUpperCase(),
      )
    ) {
      setError("Biển số này đã có trong danh sách xe.")
      return
    }
    if (
      vehicle &&
      data?.entries.some(
        (e) => e.vehicleId === vehicle.id && e.odo < values.baseOdo,
      )
    ) {
      setError("ODO ban đầu không thể lớn hơn ODO đã có trong lịch sử.")
      return
    }
    setPending(true)
    try {
      if (vehicle) {
        await api.updateVehicle(vehicle.id, {
          ...values,
          version: vehicle.version,
          image: defaultImage(vehicle.type),
        })
      } else {
        await api.createVehicle({
          ...values,
          image: defaultImage(
            types.find((item) => item.id === values.vehicleTypeId)?.name ??
              "Ô tô",
          ),
        })
      }
      await refetch()
      notify(vehicle ? "Đã cập nhật thông tin xe." : "Đã thêm phương tiện mới.")
      setModal(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể lưu phương tiện.")
      notify(
        e instanceof Error ? e.message : "Không thể lưu phương tiện.",
        "error",
      )
    } finally {
      setPending(false)
    }
  })
  return (
    <Box component="form" onSubmit={submit} noValidate>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Box className="form-grid">
          <FormSection
            title="Nhận diện phương tiện"
            description="Đặt tên dễ nhớ và chọn đúng loại, hãng, dòng xe của bạn."
          />
          <TextField
            label="Tên phương tiện *"
            placeholder="VD: Toyota Vios"
            className="full"
            {...register("name")}
            error={!!errors.name}
            helperText={errors.name?.message}
          />
          <Controller
            name="vehicleTypeId"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="Loại phương tiện *"
                error={!!errors.vehicleTypeId}
                helperText={errors.vehicleTypeId?.message}
              >
                {types.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {item.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
          <Controller
            name="vehicleBrandId"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="Hãng xe *"
                error={!!errors.vehicleBrandId}
                helperText={errors.vehicleBrandId?.message}
                disabled={!selectedTypeId}
              >
                {brands.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {item.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
          <Controller
            name="vehicleModelId"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="Dòng xe *"
                error={!!errors.vehicleModelId}
                helperText={errors.vehicleModelId?.message}
                disabled={!selectedBrandId}
              >
                {models.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {item.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
          <TextField
            label="Biển số *"
            placeholder="51G-123.45"
            {...register("plate")}
            error={!!errors.plate}
            helperText={errors.plate?.message}
          />
          <TextField
            label="Năm sản xuất *"
            type="number"
            {...register("year", { valueAsNumber: true })}
            error={!!errors.year}
            helperText={errors.year?.message}
          />
          <TextField
            label="ODO ban đầu (km) *"
            type="number"
            {...register("baseOdo", { valueAsNumber: true })}
            error={!!errors.baseOdo}
            helperText={
              errors.baseOdo?.message ?? "ODO lúc bắt đầu theo dõi xe"
            }
          />
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button color="inherit" onClick={() => setModal(null)}>
          Hủy
        </Button>
        <Button type="submit" variant="contained" disabled={pending}>
          {pending ? "Đang lưu..." : "Lưu phương tiện"}
        </Button>
      </DialogActions>
    </Box>
  )
}
const planSchema = z.object({
  vehicleId: z.string().min(1, "Chọn phương tiện."),
  title: z
    .string()
    .trim()
    .min(2, "Nhập tên hạng mục.")
    .max(100, "Tối đa 100 ký tự."),
  dueDate: entrySchema.shape.date,
  dueOdo: z
    .number({ error: "Nhập ODO đến hạn." })
    .min(1, "ODO phải lớn hơn 0.")
    .max(9999999, "ODO tối đa 9.999.999 km."),
  intervalKm: z
    .number({ error: "Nhập chu kỳ km." })
    .min(1, "Chu kỳ phải lớn hơn 0.")
    .max(1000000, "Chu kỳ tối đa 1.000.000 km."),
  intervalMonths: z
    .number({ error: "Nhập chu kỳ tháng." })
    .int("Số tháng phải là số nguyên.")
    .min(1, "Tối thiểu 1 tháng.")
    .max(120, "Tối đa 120 tháng."),
})
type PlanFields = z.infer<typeof planSchema>
function PlanForm({ modal }: { modal: Extract<ModalState, { type: "plan" }> }) {
  const { data, save, pending, setModal } = useGarage()
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<PlanFields>({
    resolver: zodResolver(planSchema),
    defaultValues: modal.plan
      ? { ...modal.plan, dueDate: dateLabel(modal.plan.dueDate) }
      : {
          vehicleId: modal.vehicleId ?? data?.vehicles[0]?.id ?? "",
          title: "",
          dueDate: dateLabel(today()),
          dueOdo: 50000,
          intervalKm: 5000,
          intervalMonths: 6,
        },
  })
  return (
    <Box
      component="form"
      noValidate
      onSubmit={handleSubmit(async (values) => {
        const plan: Plan = {
          ...values,
          dueDate: values.dueDate.split("/").reverse().join("-"),
          id: modal.plan?.id ?? crypto.randomUUID(),
        }
        if (
          await save((d) => {
            const i = d.plans.findIndex((p) => p.id === plan.id)
            if (i >= 0) d.plans[i] = plan
            else d.plans.push(plan)
          }, "Đã lưu kế hoạch bảo dưỡng.")
        )
          setModal(null)
      })}
    >
      <DialogContent>
        <Alert severity="info" sx={{ mb: 2 }}>
          Đến hạn theo ngày hoặc số km, tùy điều kiện nào đến trước. Kỳ tiếp
          theo tính từ dịch vụ gần nhất.
        </Alert>
        <Box className="form-grid">
          <FormSection
            title="Thiết lập lịch chăm xe"
            description="Chọn hạng mục và mốc đầu tiên, sau đó đặt chu kỳ lặp lại."
          />
          <Controller
            name="vehicleId"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                label="Phương tiện *"
                className="full"
                disabled={!!modal.plan}
                error={!!errors.vehicleId}
                helperText={errors.vehicleId?.message}
              >
                {data?.vehicles.map((v) => (
                  <MenuItem key={v.id} value={v.id}>
                    {v.name} · {v.plate}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
          <TextField
            className="full"
            label="Hạng mục bảo dưỡng *"
            {...register("title")}
            error={!!errors.title}
            helperText={errors.title?.message}
          />
          <TextField
            label="Ngày đến hạn đầu tiên *"
            {...register("dueDate")}
            error={!!errors.dueDate}
            helperText={errors.dueDate?.message ?? "dd/MM/yyyy"}
          />
          <TextField
            label="ODO đến hạn đầu tiên (km) *"
            type="number"
            {...register("dueOdo", { valueAsNumber: true })}
            error={!!errors.dueOdo}
            helperText={errors.dueOdo?.message}
          />
          <TextField
            label="Lặp lại mỗi (km) *"
            type="number"
            {...register("intervalKm", { valueAsNumber: true })}
            error={!!errors.intervalKm}
            helperText={errors.intervalKm?.message}
          />
          <TextField
            label="Lặp lại mỗi (tháng) *"
            type="number"
            {...register("intervalMonths", { valueAsNumber: true })}
            error={!!errors.intervalMonths}
            helperText={errors.intervalMonths?.message}
          />
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button color="inherit" onClick={() => setModal(null)}>
          Hủy
        </Button>
        <Button
          type="submit"
          variant="contained"
          disabled={pending || !data?.vehicles.length}
        >
          {pending ? "Đang lưu..." : "Lưu kế hoạch"}
        </Button>
      </DialogActions>
    </Box>
  )
}

export default function Dialogs() {
  const { modal, setModal, data, save, pending, notify, refetch } = useGarage()
  if (!modal) return null
  const title =
    modal.type === "login"
      ? "Chào mừng đến iCareX"
      : modal.type === "entry"
        ? `${modal.entry ? "Chỉnh sửa" : "Thêm"} ${
            modal.kind === "fuel"
              ? "lần đổ nhiên liệu"
              : modal.kind === "service"
                ? "dịch vụ bảo dưỡng"
                : "bản ghi ODO"
          }`
        : modal.type === "vehicle"
          ? `${modal.vehicle ? "Chỉnh sửa" : "Thêm"} phương tiện`
          : modal.type === "plan"
            ? `${modal.plan ? "Chỉnh sửa" : "Tạo"} kế hoạch bảo dưỡng`
            : modal.type === "help"
              ? "Đồng hành cùng chiếc xe của bạn"
              : modal.entity === "vehicle"
                ? "Lưu trữ phương tiện"
                : "Xóa bản ghi"
  const description =
    modal.type === "login"
      ? "CHĂM XE · AN TÂM MỖI NGÀY"
      : modal.type === "vehicle"
        ? "Mỗi chiếc xe, một hành trình được chăm sóc."
        : modal.type === "entry"
          ? "Lưu lại hôm nay, dễ dàng theo dõi ngày mai."
          : modal.type === "plan"
            ? "Chủ động lên lịch cho lần chăm xe tiếp theo."
            : modal.type === "help"
              ? "Một nơi để quản lý hành trình chăm xe."
              : "Kiểm tra thông tin trước khi xác nhận."
  const icon =
    modal.type === "login" ? (
      <ShieldOutlined />
    ) : modal.type === "vehicle" ? (
      <DirectionsCarOutlined />
    ) : modal.type === "entry" ? (
      <SpeedOutlined />
    ) : modal.type === "plan" ? (
      <EventNoteOutlined />
    ) : modal.type === "help" ? (
      <HelpOutlineRounded />
    ) : (
      <DeleteOutline />
    )
  return (
    <Dialog
      className={`icarex-dialog dialog-${modal.type}`}
      open
      onClose={() => !pending && setModal(null)}
      fullWidth
      maxWidth={modal.type === "delete" ? "xs" : "sm"}
      aria-labelledby="dialog-title"
      aria-describedby="dialog-description"
    >
      <DialogTitle className="dialog-header" component="div">
        <Box
          className={`icon-box dialog-heading-icon ${
            modal.type === "delete" ? "red" : ""
          }`}
        >
          {icon}
        </Box>
        <Box>
          <Typography id="dialog-title" component="h2" variant="h6">
            {title}
          </Typography>
          <Typography
            id="dialog-description"
            variant="body2"
            color="text.secondary"
          >
            {description}
          </Typography>
        </Box>
        <IconButton
          aria-label="Đóng hộp thoại"
          disabled={pending}
          onClick={() => setModal(null)}
          className="dialog-close"
          sx={{
            position: "absolute",
            right: "var(--space-lg)",
            top: "var(--space-lg)",
          }}
        >
          <CloseRounded fontSize="small" />
        </IconButton>
      </DialogTitle>
      {modal.type === "login" && <LoginForm />}
      {modal.type === "entry" && <EntryForm modal={modal} />}
      {modal.type === "vehicle" && <VehicleForm vehicle={modal.vehicle} />}
      {modal.type === "plan" && <PlanForm modal={modal} />}
      {modal.type === "delete" && (
        <>
          <DialogContent>
            <Typography variant="body2">
              {modal.entity === "vehicle"
                ? "Bạn muốn lưu trữ"
                : "Bạn có chắc muốn xóa"}{" "}
              “{modal.title}”?
            </Typography>
            <Alert severity="warning" sx={{ mt: 2 }}>
              {modal.entity === "vehicle"
                ? "Phương tiện sẽ được lưu trữ. Lịch sử đã ghi nhận được giữ lại."
                : modal.entity === "entry"
                  ? "ODO, chi phí và hạn bảo dưỡng liên quan sẽ được tính lại. Xóa dịch vụ hoàn tất có thể làm kế hoạch đến hạn trở lại."
                  : "Kế hoạch sẽ bị xóa, các dịch vụ đã ghi nhận vẫn được giữ trong lịch sử."}
            </Alert>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 3 }}>
            <Button color="inherit" onClick={() => setModal(null)}>
              Giữ lại
            </Button>
            <Button
              variant="contained"
              color="error"
              disabled={pending}
              onClick={async () => {
                if (modal.entity === "vehicle") {
                  try {
                    await api.archiveVehicle(
                      modal.id,
                      data?.vehicles.find((v) => v.id === modal.id)?.version ??
                        0,
                    )
                    await refetch()
                    notify("Đã lưu trữ phương tiện.", "success")
                    setModal(null)
                  } catch (e) {
                    notify(
                      e instanceof Error
                        ? e.message
                        : "Không thể lưu trữ phương tiện.",
                      "error",
                    )
                  }
                  return
                }
                const entry =
                  modal.entity === "entry"
                    ? data?.entries.find((item) => item.id === modal.id)
                    : undefined
                if (entry?.kind === "odo") {
                  try {
                    await api.deleteOdometerReading(
                      entry.vehicleId,
                      entry.id,
                      entry.version ?? 0,
                    )
                    await refetch()
                    notify("Đã xóa bản ghi ODO.")
                    setModal(null)
                  } catch (e) {
                    notify(
                      e instanceof Error
                        ? e.message
                        : "Không thể xóa bản ghi ODO.",
                      "error",
                    )
                  }
                  return
                }
                if (
                  await save((d) => {
                    if (modal.entity === "entry")
                      d.entries = d.entries.filter((e) => e.id !== modal.id)
                    if (modal.entity === "plan") {
                      d.plans = d.plans.filter((p) => p.id !== modal.id)
                      d.entries.forEach((e) => {
                        if (e.planId === modal.id) delete e.planId
                      })
                    }
                  }, "Đã xóa và cập nhật dữ liệu liên quan.")
                )
                  setModal(null)
              }}
            >
              {pending
                ? "Đang xử lý..."
                : modal.entity === "vehicle"
                  ? "Lưu trữ xe"
                  : "Xác nhận xóa"}
            </Button>
          </DialogActions>
        </>
      )}
      {modal.type === "help" && (
        <>
          <DialogContent>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              iCareX giúp bạn theo dõi nhiều phương tiện trong một không gian.
              Bắt đầu chỉ với vài bước:
            </Typography>
            {[
              [
                "01",
                "Thêm phương tiện",
                "Khai báo biển số và ODO ban đầu của xe.",
              ],
              [
                "02",
                "Ghi lại mỗi hành trình",
                "Cập nhật ODO, các lần đổ nhiên liệu và dịch vụ.",
              ],
              [
                "03",
                "Chăm xe đúng lúc",
                "Tạo kế hoạch theo ngày và km; ghi dịch vụ để hoàn tất.",
              ],
            ].map(([n, t, s]) => (
              <Box
                key={n}
                className="help-step"
                sx={{
                  display: "flex",
                  gap: "var(--space-xl)",
                  mb: "var(--space-2xl)",
                  p: "var(--space-xl)",
                  bgcolor: "var(--bg-faint)",
                  borderRadius: "var(--corner-md)",
                }}
              >
                <Box
                  className="icon-box"
                  sx={{ bgcolor: "var(--surface-bg)", fontWeight: 600 }}
                >
                  {n}
                </Box>
                <Box>
                  <Typography variant="subtitle2">{t}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {s}
                  </Typography>
                </Box>
              </Box>
            ))}
            <Alert severity="info">
              Nhắc bảo dưỡng chỉ hiển thị trong ứng dụng khi đang mở web. Không
              có thông báo nền, email hay SMS. Múi giờ: Asia/Ho_Chi_Minh.
            </Alert>
            {data && (
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ display: "block", mt: 2 }}
              >
                Bạn đang theo dõi {number(data.vehicles.length)} phương tiện.
                Ngày Việt Nam: {dateLabel(today())}.
              </Typography>
            )}
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 3 }}>
            <Button
              variant="contained"
              startIcon={<CheckCircleOutline />}
              onClick={() => setModal(null)}
            >
              Đã hiểu
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  )
}
