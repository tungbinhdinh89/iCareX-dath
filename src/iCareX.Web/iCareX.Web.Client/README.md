# iCareX frontend

React 19 + TypeScript strict + Vite 8 frontend, build/deploy độc lập với
ASP.NET Core API. Đây là stack hiện tại; `reference.md` có mô tả Next.js cũ chỉ
để tham khảo pattern, không phải runtime requirement.

## Teaching stack

- UI: MUI Community 7, Material Icons, Astra Make Kit.
- Styling: Tailwind CSS v4 và token/font tập trung tại `src/index.css`.
- Routing: React Router 7 Data Mode.
- Server state: TanStack Query 5; session/UI context ở `store.tsx`.
- Forms/validation: React Hook Form 7 + Zod 4.
- Charts: Chart.js 4 + `react-chartjs-2`.
- Tooling: pnpm, TypeScript, Vite, oxfmt; Node test runner.

## Folder and naming convention

```text
src/app/          # app composition and routes
src/components/   # layout and shared visual components
src/features/garage/ # route-level garage screens
src/lib/apis/     # fetch clients and API DTO-to-model mapping
src/lib/models/   # UI/domain models and pure business helpers
src/lib/state/    # session, query and UI state
src/lib/theme/    # MUI/token bridge
src/index.css     # global CSS/font/design tokens
tests/            # focused Node tests
```

Composition and routing live under `app/`; feature code stays under
`features/`; API clients never expose backend DTOs directly to UI. Use the
`@/*` alias for frontend modules (and relative imports with explicit `.ts`
extensions where Node tests load modules directly). Use PascalCase for React component files, camelCase for functions/variables,
and explicit TypeScript types. Prefer feature modules only when a feature's
API/schema/query/UI complexity justifies extraction; do not reorganize the
whole app for a small fix.

Auth uses `/api/v1/auth` with HttpOnly cookie session and CSRF. Vehicle catalog
and Vehicle/ODO APIs use the same proxy boundary. Keep Vietnamese UX,
accessible labels, loading/error/empty states, responsive layout, and shared
formatters. Never put secrets in `VITE_*` variables.

## Shared contracts and package commands

The package owns its dependencies, `pnpm-lock.yaml` and `node_modules` here.
Root pnpm scripts delegate to this package. Vite/TypeScript alias `@shared/*`
resolves to `../../iCareX.Shared/typescript/*`; those types are generated from
the C# transport records in `iCareX.Shared/Contracts`.

Run `python tools/contracts/generate_typescript.py` and `--check` from the
repository root after editing DTOs. `ICAREX_API_URL` configures the dev API
proxy; the previous `VEHICLECARE_API_URL` is accepted as a fallback.

The formerly corrupted `src/lib/apis/iCareXApi.ts` and `src/lib/models/garage.ts`
were recovered from Git commit `0e1c3566`, with Shared type imports restored.
Typecheck, three Node tests and production build pass. Chrome verified ten guest
routes and the login dialog at desktop/mobile sizes without JavaScript exceptions.
Authenticated database transactions still need separate acceptance testing.

The frontend follows the owner's `Design/Design_system_1.png` and
`Design/Design_system_2.png`: Inter, a #F4F8F1 canvas, a #1F4D3A navigation
sidebar, and #2F6B4A primary actions. Desktop uses a 224px sidebar; tablet uses
a 72px accessible icon rail; phones use a 320px drawer. Supplied SVG logo and
symbol variants live in `public/brand/`. Global layout, spacing and typography stay
in `src/index.css`; MUI controls share the theme in `src/lib/theme/theme.ts`.
Dashboard, vehicle detail, records, plans, reports, profile/settings and dialogs
use this same hierarchy. Features awaiting completion show an explicit notice;
vehicle and ODO operations retain their existing API integration.

All application routes require a server session. The standalone `/dang-nhap`
screen appears only after the initial cookie check resolves; a valid restored
session opens the requested internal route without a guest/dashboard flash.
Network errors provide retry, and logout/API 401 clear private cached data.
Authenticated headers show only the real account, never a login button.
`src/components/MaintenanceStatusChip.tsx` provides the five visual maintenance
states from the design system; callers must supply a real evaluation. It does
not add missing backend states or synthesize reminder data.

Figma Make scripts, site configuration and dev.json live in `.figma/make/` here.
The repository-root forwarding hooks were removed; open this Client directory
as the Figma Make project root when those hooks are needed. Local builds and
root pnpm commands use the Client configuration directly. The scripts resolve
their own directory. `.figaro` was an unused runtime mount marker and removed.
Local development: run `pnpm dev` at the repository root and open
http://localhost:8443/. Figma hosting/deployment requires the Figma environment.
