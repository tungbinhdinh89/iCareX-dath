import {
  createContext,
  useContext,
  useEffect,
  useCallback,
  useRef,
  useMemo,
  useState,
  type ReactNode,
} from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Alert, Snackbar } from "@mui/material"
import {
  api,
  ApiError,
  onUnauthorized,
  onVehicleAccessChanged,
  resetSessionRequests,
  type Account,
  type Entry,
  type Garage,
  type Plan,
  type Vehicle,
} from "@/lib/apis/iCareXApi"
import type {
  CurrentUserPermissionsResponse,
  VehiclePermissionsResponse,
} from "@shared/vehicle-contracts"
import {
  accessibleGarage,
  canPermission,
  toAuthUser,
  vehicleAccess,
  withoutVehicle,
  type AuthUser,
  type ResourceAuthorship,
} from "@/lib/models/access"
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
  authUser: AuthUser | null
  permissions: CurrentUserPermissionsResponse | undefined
  permissionsLoading: boolean
  permissionsError: Error | null
  refreshPermissions: () => Promise<void>
  globalRoles: string[]
  vehicleAccess: VehiclePermissionsResponse | undefined
  can: (
    permission: string,
    vehicleId?: string,
    resource?: ResourceAuthorship,
  ) => boolean
  hasRole: (role: string, vehicleId?: string) => boolean
  canAccessVehicle: (vehicleId: string) => boolean
  canManageOdo: (vehicleId?: string) => boolean
  session: Account | null
  sessionStatus: "checking" | "authenticated" | "anonymous" | "error"
  sessionError: Error | null
  retrySession: () => void
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
  refetch: () => Promise<void>
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
  const [sessionStatus, setSessionStatus] =
    useState<ContextValue["sessionStatus"]>("checking")
  const [sessionError, setSessionError] = useState<Error | null>(null)
  const sessionRequest = useRef(0)
  const currentAccount = useRef<string | null>(null)
  const [modal, setModal] = useState<ModalState>(null)
  const [month, setMonth] = useState("all")
  const [selectedVehicle, setSelectedVehicle] = useState("all")
  const [toast, setToast] = useState<{
    id: string
    text: string
    severity: "success" | "error" | "info"
  } | null>(null)
  const clearAccount = useCallback(() => {
    resetSessionRequests()
    sessionRequest.current += 1
    currentAccount.current = null
    setSession(null)
    setSessionStatus("anonymous")
    setSessionError(null)
    setModal(null)
    setSelectedVehicle("all")
    setMonth("all")
    client.removeQueries({ queryKey: ["permissions"] })
    client.removeQueries({ queryKey: ["vehicle-members"] })
    client.removeQueries({ queryKey: ["vehicle-invitations"] })
    client.removeQueries({ queryKey: ["invitations"] })
    client.removeQueries({ queryKey: ["garage"] })
    client.getMutationCache().clear()
  }, [client])
  const retrySession = useCallback(() => {
    const request = ++sessionRequest.current
    setSessionStatus("checking")
    setSessionError(null)
    void api
      .me()
      .then((account) => {
        if (request !== sessionRequest.current) return
        currentAccount.current = account.id
        setSession(account)
        setSessionStatus("authenticated")
      })
      .catch((error: unknown) => {
        if (request !== sessionRequest.current) return
        if (error instanceof ApiError && error.status === 401) {
          clearAccount()
          return
        }
        setSessionError(
          error instanceof Error
            ? error
            : new Error("Không thể kiểm tra phiên đăng nhập."),
        )
        setSessionStatus("error")
      })
  }, [clearAccount])
  useEffect(() => {
    retrySession()
    return () => {
      sessionRequest.current += 1
    }
  }, [retrySession])
  useEffect(() => onUnauthorized(clearAccount), [clearAccount])
  const verifiedSession =
    toAuthUser(session)?.emailVerified === true &&
    toAuthUser(session)?.accountStatus === "Active"
  const permissionsQuery = useQuery({
    queryKey: ["permissions", session?.id ?? "anonymous"],
    queryFn: ({ signal }) => api.currentPermissions(signal),
    enabled: sessionStatus === "authenticated" && !!session && verifiedSession,
    retry: (failureCount, error) =>
      !(error instanceof ApiError && [401, 403, 404].includes(error.status)) &&
      failureCount < 1,
    staleTime: 15_000,
    refetchInterval: 30_000,
    refetchOnWindowFocus: true,
  })
  const permissions =
    permissionsQuery.data?.userId === session?.id
      ? permissionsQuery.data
      : undefined
  const accessReady = !!permissions && !permissionsQuery.isError
  const query = useQuery({
    queryKey: ["garage", session?.id ?? "anonymous"],
    queryFn: async ({ signal }) => {
      const actor = session?.id
      const result = await api.get(session, signal)
      if (!actor || currentAccount.current !== actor)
        throw new ApiError("Phiên đăng nhập đã thay đổi.", 401)
      const current = client.getQueryData<CurrentUserPermissionsResponse>([
        "permissions",
        actor,
      ])
      const allowed = accessibleGarage(result, current, actor)
      if (!allowed)
        throw new ApiError("Quyền truy cập đã thay đổi. Vui lòng thử lại.", 403)
      return allowed
    },
    enabled: sessionStatus === "authenticated" && !!session && accessReady,
    retry: (failureCount, error) =>
      !(error instanceof ApiError && [401, 403, 404].includes(error.status)) &&
      failureCount < 1,
    staleTime: 15_000,
    refetchOnWindowFocus: true,
  })
  const data = useMemo(
    () =>
      accessReady
        ? accessibleGarage(query.data, permissions, session?.id)
        : undefined,
    [accessReady, query.data, permissions, session?.id],
  )
  const can: ContextValue["can"] = useCallback(
    (permission, vehicleId, resource) => {
      if (!accessReady) return false
      const target = vehicleId ?? selectedVehicle
      return target !== "all"
        ? canPermission(permissions, session?.id, permission, target, resource)
        : !!permissions?.vehicles.some((item) =>
            canPermission(
              permissions,
              session?.id,
              permission,
              item.vehicleId,
              resource,
            ),
          )
    },
    [accessReady, permissions, session?.id, selectedVehicle],
  )
  const hasRole: ContextValue["hasRole"] = useCallback(
    (role, vehicleId) => {
      if (!accessReady || !permissions) return false
      return vehicleId
        ? vehicleAccess(permissions, session?.id, vehicleId)?.role === role
        : permissions.globalRoles.includes(role)
    },
    [accessReady, permissions, session?.id],
  )
  const canAccessVehicle = useCallback(
    (vehicleId: string) =>
      canPermission(
        accessReady ? permissions : undefined,
        session?.id,
        "vehicle.read",
        vehicleId,
      ),
    [accessReady, permissions, session?.id],
  )
  const canManageOdo = useCallback(
    (vehicleId?: string) => {
      if (!accessReady || !permissions) return false
      const target = vehicleId ?? selectedVehicle
      return permissions.vehicles.some(
        (item) =>
          (target === "all" || item.vehicleId === target) &&
          item.role === "Owner" &&
          !item.isArchived &&
          canPermission(
            permissions,
            session?.id,
            "vehicle.read",
            item.vehicleId,
          ),
      )
    },
    [accessReady, permissions, selectedVehicle, session?.id],
  )
  const refreshPermissions = useCallback(async () => {
    const id = currentAccount.current
    const result = await permissionsQuery.refetch()
    if (!id || currentAccount.current !== id || !result.data || result.error)
      return
    await client.invalidateQueries({ queryKey: ["garage", id] })
  }, [permissionsQuery.refetch, client])
  useEffect(
    () =>
      onVehicleAccessChanged((vehicleId) => {
        const id = currentAccount.current
        if (!id) return
        void client.cancelQueries({ queryKey: ["garage", id] })
        client.setQueryData<CurrentUserPermissionsResponse>(
          ["permissions", id],
          (old) => withoutVehicle(old, vehicleId),
        )
        client.setQueryData<Garage>(
          ["garage", id],
          (old) =>
            old && {
              ...old,
              vehicles: old.vehicles.filter((v) => v.id !== vehicleId),
              entries: old.entries.filter((e) => e.vehicleId !== vehicleId),
              plans: old.plans.filter((p) => p.vehicleId !== vehicleId),
            },
        )
        client.removeQueries({
          predicate: (cached) => cached.queryKey.includes(vehicleId),
        })
        setModal(null)
        void client.invalidateQueries({ queryKey: ["permissions", id] })
        void client.invalidateQueries({ queryKey: ["garage", id] })
      }),
    [client],
  )
  const previousAccess = useRef<CurrentUserPermissionsResponse | undefined>(
    undefined,
  )
  useEffect(() => {
    const previous = previousAccess.current
    previousAccess.current = permissions
    if (!previous || !permissions || previous.userId !== permissions.userId)
      return
    for (const old of previous.vehicles) {
      const next = permissions.vehicles.find(
        (item) => item.vehicleId === old.vehicleId,
      )
      if (!next || JSON.stringify(old) !== JSON.stringify(next)) {
        setModal(null)
        void client.cancelQueries({ queryKey: ["garage", permissions.userId] })
        client.removeQueries({
          predicate: (cached) => cached.queryKey.includes(old.vehicleId),
        })
      }
    }
    client.setQueryData<Garage>(["garage", permissions.userId], (old) =>
      accessibleGarage(old, permissions, permissions.userId),
    )
  }, [permissions, client])
  useEffect(() => {
    if (
      data &&
      selectedVehicle !== "all" &&
      !data.vehicles.some((v) => v.id === selectedVehicle)
    )
      setSelectedVehicle("all")
  }, [data, selectedVehicle])
  const mutation = useMutation({
    mutationFn: ({ id, action }: UpdateRequest) => api.update(id, action),
    onSuccess: (data, { id }) => {
      if (currentAccount.current === id) {
        const current = client.getQueryData<CurrentUserPermissionsResponse>([
          "permissions",
          id,
        ])
        const allowed = accessibleGarage(data, current, id)
        if (allowed) client.setQueryData(["garage", id], allowed)
      }
    },
  })
  const notify: ContextValue["notify"] = (text, severity = "success") =>
    setToast({ id: crypto.randomUUID(), text, severity })
  const open = (m: ModalState) => {
    if (m && m.type !== "login" && m.type !== "help" && !session) {
      setModal({ type: "login" })
      return
    }
    let allowed = true
    if (m?.type === "entry")
      allowed =
        m.kind === "odo" && canManageOdo(m.entry?.vehicleId ?? m.vehicleId)
    if (m?.type === "vehicle" && m.vehicle)
      allowed = can("vehicle.update", m.vehicle.id)
    if (m?.type === "plan") allowed = false // No Plan mutation API exists in this slice.
    if (m?.type === "delete") {
      if (m.entity === "vehicle") allowed = can("vehicle.archive", m.id)
      else if (m.entity === "entry") {
        const entry = data?.entries.find((item) => item.id === m.id)
        allowed =
          !!entry && entry.kind === "odo" && canManageOdo(entry.vehicleId)
      } else allowed = false
    }
    if (!allowed) {
      notify("Thao tác không khả dụng với quyền hiện tại.", "info")
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
        authUser: toAuthUser(session),
        permissions,
        permissionsLoading:
          sessionStatus === "authenticated" && permissionsQuery.isPending,
        permissionsError: permissionsQuery.error,
        refreshPermissions,
        globalRoles: permissions?.globalRoles ?? [],
        vehicleAccess: vehicleAccess(
          accessReady ? permissions : undefined,
          session?.id,
          selectedVehicle,
        ),
        can,
        hasRole,
        canAccessVehicle,
        canManageOdo,
        session,
        sessionStatus,
        sessionError,
        retrySession,
        data: session ? data : undefined,
        loading:
          sessionStatus === "authenticated" &&
          (permissionsQuery.isPending || (accessReady && query.isPending)),
        error: permissionsQuery.error ?? query.error,
        modal,
        setModal,
        open,
        pending: mutation.isPending,
        save,
        notify,
        login: (account) => {
          resetSessionRequests()
          sessionRequest.current += 1
          currentAccount.current = account.id
          client.removeQueries({ queryKey: ["permissions"] })
          client.removeQueries({ queryKey: ["vehicle-members"] })
          client.removeQueries({ queryKey: ["vehicle-invitations"] })
          client.removeQueries({ queryKey: ["invitations"] })
          client.removeQueries({ queryKey: ["garage"] })
          client.getMutationCache().clear()
          setSession(account)
          setSessionStatus("authenticated")
          setSessionError(null)
          setSelectedVehicle("all")
          setMonth("all")
          setModal(null)
          notify(`Xin chào ${account.name}!`)
        },
        logout: async () => {
          try {
            await api.logout()
          } catch (error) {
            if (error instanceof ApiError && error.status === 401) {
              clearAccount()
              return
            }
            notify(
              error instanceof Error ? error.message : "Không thể đăng xuất.",
              "error",
            )
            return
          }
          clearAccount()
          notify(
            "Đã đăng xuất khỏi iCareX.",
            "info",
          )
        },
        refetch: async () => {
          await refreshPermissions()
          if (currentAccount.current) await query.refetch()
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
