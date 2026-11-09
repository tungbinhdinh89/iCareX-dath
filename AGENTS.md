# iCareX repository instructions

Read [rules](doc/rules.md), [implementation playbook](doc/skill.md), [README](README.md) and relevant documentation before implementation. The [reference](doc/reference.md) is a blueprint; runtime and manifests are authoritative.

## Custom skills

Use [Codex skill guidance](doc/codex-skills.md) and the routing table in
[rules](doc/rules.md) to select relevant `SKILL.md` files under
`stephen-skills/.agents/skills/` or `stephen-skills/skills/`. Read the selected
skill before applying it and announce its use. Do not load all skills, assume
nested files are registered, or execute inherited CMS commands against iCareX.
Project architecture, scripts, design guidelines and the user's authorized
scope govern how a reusable skill is applied. A role file is not authorization
to spawn agents or mutate external services.

## Source layout

- `iCareX.slnx`: .NET solution.
- `src/iCareX.Domain/`: entities, enums and pure domain rules; one entity/enum type per file.
- `src/iCareX.Shared/`: transport DTOs and generated TypeScript contracts shared by backend/frontend.
- `src/iCareX.Application/`: service interfaces, internal contracts and ports.
- `src/iCareX.Infrastructure/`: persistence, EF mapping/migrations, repositories and service implementations.
- `src/iCareX.Web/iCareX.Web.Api/`: controllers and composition root.
- `src/iCareX.Web/iCareX.Web.Client/`: React/Vite frontend.
- `src/iCareX.UnitTests/`, `src/iCareX.IntegrationTests/`: test projects.
- `tools/iCareX.DatabaseTool/`: database utility.
- `tools/contracts/generate_typescript.py`: contract generation/check.
- `doc/`: requirements, design and implementation guides; backend pages in `doc/.be/`.

## Backend work

Read [backend README](doc/.be/README.md) and the relevant data/API/testing pages before changing backend code. Update relevant backend documentation and `doc/.be/changelog.md` with each backend change. Use .NET 10 and PostgreSQL; Domain and Shared must not depend on EF, HTTP or Identity.

Put transport request/response models in `iCareX.Shared/Contracts/<Feature>`; keep service interfaces and errors in Application. Generate TypeScript from the C# DTOs and run the generator with `--check`. Do not expose persisted entities, Identity secrets, DbContext or IQueryable to the frontend.

Check ownership, archive, validation, transaction, concurrency and idempotency for business mutations. Never automatically migrate/seed at startup or use the NAS database for destructive tests. Never log passwords, tokens, cookies or connection strings. Existing UserSecretsId/key compatibility is documented in README; renaming the application does not authorize modifying a live database.

## Frontend work

Use React 19, Vite 8, TypeScript and Tailwind v4 through `@tailwindcss/vite`; no Tailwind/PostCSS config is required. Keep global CSS/font wiring in `src/index.css`, CSS imports first. Preserve MUI/Astra styling and Vietnamese UX. Read [design guidelines](doc/guidelines/Guidelines.md) when working on UI.

Composition lives in `src/app/Application.tsx`; routes in `src/app/routes.tsx`; HTTP/DTO mapping in `src/lib/apis`; UI models/helpers in `src/lib/models`. Alias `@shared/*` resolves to `src/iCareX.Shared/typescript/*`.

Use React Hook Form/Zod for forms, TanStack Query/context for server/session state and refetch after writes. Server validation remains authoritative. Export React components as default exports, close JSX tags, and quote strings with apostrophes correctly.

A Vite dev server may already run on PORT (default 8443); use the existing preview instead of starting a second instance. Root pnpm scripts delegate to the frontend package. Do not place application source/dependencies back into a second root layout.

## Verification and handoff

Follow [ticket workflow](doc/ticket-workflow.md): use feature, bug, fix, enhanced,
test, ui, docs, refactor or chore labels and define scope/acceptance criteria.
Create a `<label>/<issue-number>-<slug>` branch before implementing a ticket.
Use `<type>(<scope>): <summary> (#<issue>)` commits and `Refs #<issue>` in PRs.
After creating a PR, wait for owner review. Never self-approve, merge, enable
auto-merge, close issues/PRs or mark Done without explicit owner authorization.
Review alone does not authorize merge. Do not force-add ignored directories
wholesale.

Run relevant unit/host/integration checks and frontend typecheck/build when affected. Report exact results and pre-existing blockers; never mark a skipped or blocked check as passed. Keep user changes; do not commit secrets, logs, dependencies, SDKs or generated local state. Do not approve, merge or close work without explicit owner authorization.
