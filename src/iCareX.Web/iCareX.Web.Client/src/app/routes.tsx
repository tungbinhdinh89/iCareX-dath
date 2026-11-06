import { createBrowserRouter } from "react-router"
import Layout from "@/components/Layout"
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
  {
    path: "/",
    Component: Layout,
    children: [
      { index: true, Component: Dashboard },
      { path: "xe", Component: Vehicles },
      { path: "xe/:id", Component: VehicleDetail },
      { path: "nhien-lieu", element: <Records key="fuel" kind="fuel" /> },
      { path: "bao-duong", element: <Records key="service" kind="service" /> },
      { path: "ke-hoach", element: <Plans key="plans" /> },
      { path: "nhac-bao-duong", element: <Plans key="reminders" reminders /> },
      { path: "lich-su", element: <Records key="history" /> },
      { path: "bao-cao", Component: Reports },
      { path: "ho-so", Component: Profile },
      { path: "cai-dat", Component: Settings },
      { path: "*", Component: NotFound },
    ],
  },
])
