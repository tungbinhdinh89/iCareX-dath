import { createBrowserRouter } from "react-router"
import Layout from "@/components/Layout"
import AuthenticationPage, {
  RequireSession,
} from "@/components/AuthenticationPage"
import {
  RequirePermission,
  RequireVehicleAccess,
} from "@/components/AuthorizationGuards"
import ChangePasswordPage, {
  ForgotPasswordPage,
  ResetPasswordPage,
  VerifyEmailPage,
} from "@/features/auth/AuthPages"
import MembersPage from "@/features/access/MembersPage"
import InvitationPage, {
  InvitationInboxPage,
} from "@/features/access/InvitationPages"
import Dashboard, {
  NotFound,
  Plans,
  Profile,
  Records,
  Reports,
  Settings,
  VehicleDetail,
  Vehicles,
} from "@/features/garage/pages"
export const router = createBrowserRouter([
  { path: "/dang-nhap", Component: AuthenticationPage },
  { path: "/dang-ky", Component: AuthenticationPage },
  { path: "/xac-thuc-email", Component: VerifyEmailPage },
  { path: "/quen-mat-khau", Component: ForgotPasswordPage },
  { path: "/dat-lai-mat-khau", Component: ResetPasswordPage },
  { path: "/loi-moi/:id", Component: InvitationPage },
  {
    Component: RequireSession,
    children: [
      {
        path: "/",
        Component: Layout,
        children: [
          { index: true, Component: Dashboard },
          { path: "xe", Component: Vehicles },
          {
            Component: RequireVehicleAccess,
            children: [{ path: "xe/:id", Component: VehicleDetail }],
          },
          {
            element: <RequireVehicleAccess permission="vehicle.member.read" />,
            children: [{ path: "xe/:id/thanh-vien", Component: MembersPage }],
          },
          {
            path: "nhien-lieu",
            element: (
              <RequirePermission permission="fuel.read">
                <Records key="fuel" kind="fuel" />
              </RequirePermission>
            ),
          },
          {
            path: "bao-duong",
            element: (
              <RequirePermission permission="maintenance.read">
                <Records key="service" kind="service" />
              </RequirePermission>
            ),
          },
          {
            path: "ke-hoach",
            element: (
              <RequirePermission permission="maintenance_plan.read">
                <Plans key="plans" />
              </RequirePermission>
            ),
          },
          {
            path: "nhac-bao-duong",
            element: (
              <RequirePermission permission="reminder.read">
                <Plans key="reminders" reminders />
              </RequirePermission>
            ),
          },
          { path: "lich-su", element: <Records key="history" /> },
          { path: "bao-cao", Component: Reports },
          { path: "ho-so", Component: Profile },
          { path: "ho-so/doi-mat-khau", Component: ChangePasswordPage },
          { path: "loi-moi", Component: InvitationInboxPage },
          { path: "cai-dat", Component: Settings },
          { path: "*", Component: NotFound },
        ],
      },
    ],
  },
])
