import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Alert, Snackbar } from "@mui/material"
import {
  api,
  type Account,
  type Entry,
  type Garage,
  type Plan,
  type Vehicle,
} from "@/lib/apis/iCareXApi"

export type ModalState = { type: "login" } | {
  type: "entry"
  kind: Entry["kind"]
  entry?: Entry
  vehicleId?: string
  plan?: Plan
} | {
  type: "vehicle"
  vehicle?: Vehicle
} | {
  type: "plan"
  plan?: Plan
  vehicleId?: string
} | {
  type: "delete"
  entity: "entry" | "vehicle" | "plan"
  id: string
  title: string
} | { type: "help" } | null
type ContextValue = {
  session: Account | null
  data: Garage | undefined
  loading: boolean
  error: Error | null
  modal: ModalState
  setModal: (m: ModalState) => void
  open: (m: ModalState) => void
  login: (account: Account) => void
  logout: () => Promise<void>
  save: (action: (d: Garage) => void, message?: string) => Promise<boolean>
  pending: boolean
  notify: (text: string, severity?: "success" | "error" | "info") => void
  refetch: () => void
  month: string
  setMonth: (m: string) => void
  selectedVehicle: string
  setSelectedVehicle: (id: string) => void
}
const Context = createContext<ContextValue | null>(null)
type UpdateRequest = {
  id: string
  action: (d: Garage) => void
}
export const useGarage = () => {
  const value = useContext(Context)
  if (!value) throw new Error("iCareX cần GarageProvider.")
  return value
}
export default function GarageProvider({ children }: { children: ReactNode }) {
  const client = useQueryClient()
  const [session, setSession] = useState<Account | null>(null)
  const [modal, setModal] = useState<ModalState>(null)
  const [month, setMonth] = useState("all")
  const [selectedVehicle, setSelectedVehicle] = useState("all")
  const [toast, setToast] = useState<{
    id: string
    text: string
    severity: "success" | "error" | "info"
  } | null>(null)
  useEffect(() => {
    void api.me().then(setSession).catch(() => setSession(null))
  }, [])
  const query = useQuery({
    queryKey: ["garage", session?.id ?? "preview"],
    queryFn: () => api.get(session),
    retry: 1,
    staleTime: Infinity,
  })
  useEffect(() => {
    if (
      query.data &&
      selectedVehicle !== "all" &&
      !query.data.vehicles.some((v) => v.id === selectedVehicle)
    )
      setSelectedVehicle("all")
  }, [query.data, selectedVehicle])
  const mutation = useMutation({
    mutationFn: ({ id, action }: UpdateRequest) => api.update(id, action),
    onSuccess: (data, { id }) => client.setQueryData(["garage", id], data),
  })
  const notify: ContextValue["notify"] = (text, severity = "success") =>
    setToast({ id: crypto.randomUUID(), text, severity })
  const open = (m: ModalState) => {
    if (m && m.type !== "login" && m.type !== "help" && !session) {
      setModal({ type: "login" })
      return
    }
    setModal(m)
  }
  const save: ContextValue["save"] = async (
    action,
    message = "Đã lưu thay đổi.",
  ) => {
    if (!session) {
      setModal({ type: "login" })
      return false
    }
    try {
      await mutation.mutateAsync({ id: session.id, action })
      notify(message)
      return true
    } catch (error) {
      notify(
        error instanceof Error
          ? error.message
          : "Không thể lưu. Vui lòng thử lại.",
        "error",
      )
      return false
    }
  }
  return (
    <Context.Provider
      value={{
        session,
        data: query.data,
        loading: query.isPending,
        error: query.error,
        modal,
        setModal,
        open,
        pending: mutation.isPending,
        save,
        notify,
        login: (account) => {
          setSession(account)
          setSelectedVehicle("all")
          setMonth("all")
          setModal(null)
          notify(`Xin chào ${account.name}!`)
        },
        logout: async () => {
          try {
            await api.logout()
          } catch (error) {
            notify(
              error instanceof Error ? error.message : "Không thể đăng xuất.",
              "error",
            )
            return
          }
          setSession(null)
          setModal(null)
          setSelectedVehicle("all")
          setMonth("all")
          notify(
            "Đã đăng xuất. Phiên đăng nhập không được lưu trên thiết bị.",
            "info",
          )
        },
        refetch: () => {
          void query.refetch()
        },
        month,
        setMonth,
        selectedVehicle,
        setSelectedVehicle,
      }}
    >
      {children}
      <Snackbar
        key={toast?.id}
        open={!!toast}
        autoHideDuration={5000}
        onClose={() => setToast(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          severity={toast?.severity ?? "success"}
          variant="filled"
          onClose={() => setToast(null)}
        >
          {toast?.text}
        </Alert>
      </Snackbar>
    </Context.Provider>
  )
}
