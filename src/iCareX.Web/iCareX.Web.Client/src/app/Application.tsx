import { useMemo } from "react"
import { CssBaseline, ThemeProvider } from "@mui/material"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { RouterProvider } from "react-router"
import { router } from "@/app/routes"
import { makeTheme } from "@/lib/theme/theme"
import GarageProvider from "@/lib/state/store"

const queryClient = new QueryClient({
  defaultOptions: { queries: { refetchOnWindowFocus: false } },
})

export default function Application() {
  const theme = useMemo(makeTheme, [])
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <GarageProvider>
          <RouterProvider router={router} />
        </GarageProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}
