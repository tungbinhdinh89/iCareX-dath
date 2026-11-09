import assert from "node:assert/strict"
import test from "node:test"
import { matchesVehicleTab, vehicleFormDefaults, vehicleMaintenanceSummary } from '../src/lib/models/vehiclePresentation.ts'
import {
  odoOf,
  today,
  validateEntry,
  type Garage,
  type Vehicle,
} from "../src/lib/models/garage.ts"
import {
  api,
  ApiError,
  onUnauthorized,
  onVehicleAccessChanged,
  resetSessionRequests,
} from "../src/lib/apis/iCareXApi.ts"
import { sessionReturnPath } from "../src/lib/models/session.ts"
import {
  accessibleGarage,
  canPermission,
  vehicleAccess,
  withoutVehicle,
} from "../src/lib/models/access.ts"
import {
  credentialsSchema,
  newPasswordSchema,
  readSecretFragment,
  registrationNotice,
} from "../src/lib/models/auth.ts"
import type { CurrentUserPermissionsResponse } from "../../../iCareX.Shared/typescript/vehicle-contracts.ts"
const vehicle: Vehicle = {
  id: "vehicle-1",
  name: "Toyota Vios",
  plate: "51G-123.45",
  type: "Ô tô",
  vehicleTypeId: "type-car",
  vehicleBrandId: "brand-toyota",
  vehicleBrandName: "Toyota",
  vehicleModelId: "model-vios",
  vehicleModelName: "Vios",
  year: 2021,
  baseOdo: 30000, fuelType: "Gasoline", odometerUnit: "Kilometres",
  currentOdo: 30000,
  image: "/images/car.jpg",
  isArchived: false,
  version: 0,
}
const garage: Garage = {
  vehicles: [vehicle],
  entries: [],
  plans: [],
  profile: { name: "Test", email: "test@example.com", phone: "" },
  settings: { reminders: true, thresholdKm: 500, thresholdDays: 30 },
}

test("vehicle writes send only the Shared DTO fields, excluding form/catalog metadata", async () => {
  const originalFetch = globalThis.fetch
  resetSessionRequests()
  const bodies: unknown[] = []
  try {
    globalThis.fetch = async (input, init) => {
      if (String(input).endsWith("/csrf")) return Response.json({ token: "csrf-test" })
      bodies.push(JSON.parse(String(init?.body)))
      return Response.json({ id: "saved-vehicle", vehicleTypeName: "Ô tô", vehicleType: "Car", odometer: 50000, currentOdo: 50000, fuelType: "Gasoline", odometerUnit: "Kilometres" })
    }
    const form = { name: "Ford EcoSport 2019", plate: "51G-87624",
      vehicleModelId: "30000000-0000-0000-0000-000000000003", year: 2019, baseOdo: 50000,
      image: "/images/car.jpg", vehicleTypeId: "Car", vehicleBrandId: "internal-brand", fuelType: "Gasoline", odometerUnit: "Kilometres" }
    await api.createVehicle(form)
    await api.updateVehicle("saved-vehicle", { ...form, version: 3 })
    const expected = { name: form.name, plate: form.plate, vehicleModelId: form.vehicleModelId,
      year: form.year, odometer: form.baseOdo, imageUrl: form.image, vehicleType: form.vehicleTypeId, vehicleBrandId: form.vehicleBrandId, fuelType: form.fuelType, odometerUnit: form.odometerUnit }
    assert.deepEqual(bodies, [expected, { ...expected, version: 3 }])
  } finally {
    globalThis.fetch = originalFetch
    resetSessionRequests()
  }
})

test("profile updates whitelist editable fields, send cookie CSRF and refetch current user", async () => {
  const originalFetch = globalThis.fetch
  resetSessionRequests()
  const requests: Array<{ url: string; init?: RequestInit }> = []
  const saved = { id: "actor", name: "Tên mới", email: "original@example.test", phone: "0912345678", roles: ["User"], accountStatus: "Active", emailVerified: true }
  try {
    globalThis.fetch = async (input, init) => {
      const url = String(input)
      requests.push({ url, init })
      return Response.json(url.endsWith("/csrf") ? { token: "csrf-profile" } : saved)
    }
    const result = await api.updateProfile({ name: saved.name, phone: saved.phone, userId: "victim", roles: ["SystemAdmin"] } as Parameters<typeof api.updateProfile>[0])
    assert.equal(result.email, saved.email)
    const write = requests.find((r) => r.init?.method === "PUT")!
    assert.equal(write.url, "/api/v1/auth/me")
    assert.equal(write.init?.credentials, "include")
    assert.equal((write.init?.headers as Record<string, string>)["X-CSRF-TOKEN"], "csrf-profile")
    assert.deepEqual(JSON.parse(String(write.init?.body)), { name: saved.name, phone: saved.phone })
    assert.equal(requests.at(-1)?.url, "/api/v1/auth/me")
    assert.equal(requests.at(-1)?.init?.method, undefined)
  } finally {
    globalThis.fetch = originalFetch
    resetSessionRequests()
  }
})

test("registration reports durable account creation separately from email delivery", () => {
  assert.match(registrationNotice(false), /Tài khoản đã được tạo/)
  assert.match(registrationNotice(false), /chưa thể gửi email/)
  assert.match(registrationNotice(false), /vẫn cần xác thực/)
  assert.match(registrationNotice(true), /Kiểm tra email/)
})

test("vehicle grouping uses membership role and keeps archives in their own tab", () => {
  assert.equal(matchesVehicleTab({ isArchived: false }, 'Owner', 'owned'), true)
  assert.equal(matchesVehicleTab({ isArchived: false }, 'Viewer', 'owned'), false)
  assert.equal(matchesVehicleTab({ isArchived: false }, 'Manager', 'shared'), true)
  assert.equal(matchesVehicleTab({ isArchived: true }, 'Owner', 'all'), false)
  assert.equal(matchesVehicleTab({ isArchived: true }, 'Viewer', 'archived'), true)
  assert.equal(matchesVehicleTab({ isArchived: false }, undefined, 'all'), false)
})

test("editing prefills optional metadata and VND prices while blank new fields stay optional", () => {
  const values = vehicleFormDefaults({ ...vehicle, registrationName: 'Xe của tôi', year: null,
    fuelCapacity: 12.5, vin: 'ABC123', registrationId: 'DK123', purchasePrice: 120000000, salePrice: 0, notes: 'Đã có ghi chú' })
  assert.equal(values.name, 'Xe của tôi')
  assert.equal(values.year, null)
  assert.equal(values.purchasePrice, 120000000)
  assert.equal(values.salePrice, 0)
  assert.equal(values.vin, 'ABC123')
  assert.equal(values.registrationId, 'DK123')
  assert.equal(values.fuelCapacity, 12.5)
  assert.equal(values.notes, 'Đã có ghi chú')
  assert.equal(vehicleFormDefaults().year, null)
  assert.equal(vehicleFormDefaults().plate, '')
  assert.equal('speedLimit' in values, false)
  assert.equal('hasTwoTanks' in values, false)
})

test("maintenance status is derived only when a real schedule source is available", () => {
  const schedule = { id: 'schedule', vehicleId: vehicle.id, title: 'Thay dầu', dueDate: today(), dueOdo: 30000, intervalKm: 1000, intervalMonths: 1 }
  assert.equal(vehicleMaintenanceSummary({ ...garage, plans: [schedule] }, vehicle), null)
  assert.equal(vehicleMaintenanceSummary({ ...garage, maintenanceAvailable: true }, vehicle)?.status, 'NoSchedule')
  assert.equal(vehicleMaintenanceSummary({ ...garage, maintenanceAvailable: true, plans: [schedule] }, vehicle)?.status, 'Due')
  assert.equal(vehicleMaintenanceSummary({ ...garage, maintenanceAvailable: true, plans: [{ ...schedule, dueOdo: 29999 }] }, vehicle)?.status, 'Overdue')
})

test("guest garage contains no seeded mock data", async () => {
  const data = await api.get(null)
  assert.deepEqual(data.vehicles, [])
  assert.deepEqual(data.entries, [])
  assert.deepEqual(data.plans, [])
})

test("ODO validation rejects future dates and invalid values", () => {
  const entry = {
    id: "reading-1",
    vehicleId: vehicle.id,
    kind: "odo" as const,
    title: "ODO",
    date: today(),
    odo: 30000,
    cost: 0,
    liters: 0,
    note: "",
  }
  assert.doesNotThrow(() => validateEntry(garage, entry))
  assert.throws(
    () => validateEntry(garage, { ...entry, date: "2999-01-01" }),
    /tương lai/,
  )
  assert.throws(() => validateEntry(garage, { ...entry, odo: -1 }), /không âm/)
})

test("current ODO is derived from vehicle base and readings", () => {
  assert.equal(odoOf(vehicle, []), 30000)
  assert.equal(
    odoOf(vehicle, [
      {
        id: "reading-2",
        vehicleId: vehicle.id,
        kind: "odo",
        title: "ODO",
        date: today(),
        odo: 30125,
        cost: 0,
        liters: 0,
        note: "",
      },
    ]),
    30125,
  )
})

test("login return preserves internal filters and rejects external or recursive navigation", () => {
  assert.equal(
    sessionReturnPath("/xe/vehicle-1?tab=odo#history"),
    "/xe/vehicle-1?tab=odo#history",
  )
  for (const value of [
    "https://example.com",
    "//example.com",
    "/\\example.com",
    "/dang-nhap",
    "/dang-nhap?from=/xe",
    "/dang-ky",
    "/xac-thuc-email",
    "/loi-moi/123#token=secret",
    "/xe?token=secret",
    "/dat-lai-mat-khau#token=secret",
    "/\nexample.com",
    null,
    {},
  ]) {
    assert.equal(sessionReturnPath(value), "/")
  }
})

test("session restoration distinguishes expired cookies from server and network errors", async () => {
  const originalFetch = globalThis.fetch
  try {
    for (const status of [401, 503]) {
      globalThis.fetch = async () =>
        new Response(JSON.stringify({ detail: "Không thể xác thực." }), {
          status,
        })
      await assert.rejects(
        api.me(),
        (error: unknown) =>
          error instanceof ApiError && error.status === status,
      )
    }
    globalThis.fetch = async () => {
      throw new TypeError("Network unavailable")
    }
    await assert.rejects(
      api.me(),
      (error: unknown) =>
        error instanceof TypeError && !(error instanceof ApiError),
    )
  } finally {
    globalThis.fetch = originalFetch
  }
})

test("protected API expiry notifies session boundary while other failures keep the account", async () => {
  const originalFetch = globalThis.fetch
  let expired = 0
  const unsubscribe = onUnauthorized(() => {
    expired += 1
  })
  const account = {
    id: "account-1",
    name: "Test",
    email: "test@example.com",
    phone: "",
    roles: [],
    accountStatus: "Active",
    emailVerified: true,
  }
  try {
    globalThis.fetch = async () => new Response("{}", { status: 503 })
    await assert.rejects(api.get(account), ApiError)
    assert.equal(expired, 0)
    globalThis.fetch = async () => new Response("{}", { status: 401 })
    await assert.rejects(api.get(account), ApiError)
    assert.equal(expired, 1)
    unsubscribe()
    await assert.rejects(api.get(account), ApiError)
    assert.equal(expired, 1)
  } finally {
    unsubscribe()
    globalThis.fetch = originalFetch
  }
})

test("an expired response from a previous account cannot expire the new session", async () => {
  const originalFetch = globalThis.fetch
  let expired = 0
  const unsubscribe = onUnauthorized(() => {
    expired += 1
  })
  let complete: (response: Response) => void = () => {}
  try {
    globalThis.fetch = () =>
      new Promise<Response>((resolve) => {
        complete = resolve
      })
    const request = api.get({
      id: "previous-account",
      name: "Old",
      email: "old@example.com",
      phone: "",
      roles: [],
      accountStatus: "Active",
      emailVerified: true,
    })
    resetSessionRequests()
    complete(new Response("{}", { status: 401 }))
    await assert.rejects(request, ApiError)
    assert.equal(expired, 0)
  } finally {
    unsubscribe()
    globalThis.fetch = originalFetch
  }
})
const grants: CurrentUserPermissionsResponse = {
  userId: "user-1",
  globalRoles: ["User"],
  vehicles: [
    {
      vehicleId: "vehicle-1",
      role: "Contributor",
      accessType: "Shared",
      isArchived: false,
      permissions: [
        { code: "vehicle.read", scope: "all" },
        { code: "fuel.update", scope: "own" },
        { code: "reminder.complete", scope: "ownOrAssigned" },
      ],
    },
    {
      vehicleId: "vehicle-2",
      role: "Viewer",
      accessType: "Shared",
      isArchived: false,
      permissions: [{ code: "vehicle.read", scope: "all" }],
    },
  ],
}

test("vehicle grants remain scoped to account, vehicle and actual authorship", () => {
  assert.equal(
    canPermission(grants, "user-1", "fuel.update", "vehicle-1"),
    false,
  )
  assert.equal(
    canPermission(grants, "user-1", "fuel.update", "vehicle-1", {
      createdByUserId: "user-1",
    }),
    true,
  )
  assert.equal(
    canPermission(grants, "user-1", "fuel.update", "vehicle-1", {
      createdByUserId: "other",
      assignedToUserId: "user-1",
    }),
    false,
  )
  assert.equal(
    canPermission(grants, "user-1", "reminder.complete", "vehicle-1", {
      assignedToUserId: "user-1",
    }),
    true,
  )
  assert.equal(
    canPermission(grants, "user-1", "reminder.complete", "vehicle-2", {
      assignedToUserId: "user-1",
    }),
    false,
  )
  assert.equal(
    canPermission(grants, "user-1", "vehicle.archive", "vehicle-1"),
    false,
  )
  assert.equal(
    canPermission(grants, "other", "vehicle.read", "vehicle-1"),
    false,
  )
  assert.equal(vehicleAccess(grants, "user-1", "missing"), undefined)
  const invalid = {
    ...grants,
    vehicles: [
      {
        ...grants.vehicles[0],
        permissions: [{ code: "vehicle.read", scope: "anything" }],
      },
    ],
  }
  assert.equal(
    canPermission(invalid, "user-1", "vehicle.read", "vehicle-1"),
    false,
  )
})

test("revocation removes vehicle, entries and plans without carrying another account cache", () => {
  const populated: Garage = {
    ...garage,
    entries: [
      {
        id: "e",
        vehicleId: "vehicle-1",
        kind: "odo",
        title: "ODO",
        date: "2026-01-01",
        odo: 10,
        cost: 0,
        liters: 0,
        note: "",
      },
    ],
    plans: [
      {
        id: "p",
        vehicleId: "vehicle-1",
        title: "Plan",
        dueDate: "2026-01-01",
        dueOdo: 10,
        intervalKm: 100,
        intervalMonths: 1,
      },
    ],
  }
  const revoked = accessibleGarage(
    populated,
    withoutVehicle(grants, "vehicle-1"),
    "user-1",
  )!
  assert.equal(revoked.vehicles.length, 0)
  assert.equal(revoked.entries.length, 0)
  assert.equal(revoked.plans.length, 0)
  assert.equal(
    accessibleGarage(populated, grants, "different-account"),
    undefined,
  )
  assert.equal(accessibleGarage(populated, undefined, "user-1"), undefined)
})

test("role demotion also purges cached resource data whose read grant was removed", () => {
  const populated: Garage = {
    ...garage,
    entries: [{ id: "fuel", vehicleId: "vehicle-1", kind: "fuel", title: "Fuel", date: "2026-01-01", odo: 10, cost: 100, liters: 2, note: "" }],
    plans: [{ id: "plan", vehicleId: "vehicle-1", title: "Plan", dueDate: "2026-01-01", dueOdo: 10, intervalKm: 100, intervalMonths: 1 }],
  }
  const reduced = accessibleGarage(populated, grants, "user-1")!
  assert.equal(reduced.vehicles.length, 1)
  assert.equal(reduced.entries.length, 0)
  assert.equal(reduced.plans.length, 0)
})

test("member HTTP mutations send target role/version, cookie credentials and CSRF", async () => {
  const originalFetch = globalThis.fetch
  const calls: { path: string; init?: RequestInit }[] = []
  resetSessionRequests()
  try {
    globalThis.fetch = async (input, init) => {
      calls.push({ path: String(input), init })
      if (String(input).endsWith("/csrf"))
        return Response.json({ token: "test-csrf" })
      if (init?.method === "DELETE") return new Response(null, { status: 204 })
      return Response.json({
        userId: "target",
        role: "Viewer",
        name: "Member",
        version: 3,
        joinedAtUtc: "2026-01-01T00:00:00Z",
      })
    }
    await api.updateVehicleMember("vehicle-1", "target", {
      role: "Viewer",
      version: 2,
    })
    await api.revokeVehicleMember("vehicle-1", "target", 3)
    assert.equal(calls[1].path, "/api/v1/vehicles/vehicle-1/members/target")
    assert.equal(calls[1].init?.method, "PATCH")
    assert.equal(calls[1].init?.credentials, "include")
    assert.deepEqual(JSON.parse(String(calls[1].init?.body)), {
      role: "Viewer",
      version: 2,
    })
    assert.equal(
      (calls[1].init?.headers as Record<string, string>)["X-CSRF-TOKEN"],
      "test-csrf",
    )
    assert.equal(
      calls[2].path,
      "/api/v1/vehicles/vehicle-1/members/target?version=3",
    )
  } finally {
    globalThis.fetch = originalFetch
    resetSessionRequests()
  }
})

test("forbidden/not-found vehicle responses invalidate only the current account vehicle", async () => {
  const originalFetch = globalThis.fetch
  const invalidated: string[] = []
  const unsubscribe = onVehicleAccessChanged((id) => invalidated.push(id))
  try {
    globalThis.fetch = async () => new Response("{}", { status: 403 })
    await assert.rejects(api.vehicleMembers("vehicle-1"), ApiError)
    assert.deepEqual(invalidated, ["vehicle-1"])
    let complete: (response: Response) => void = () => {}
    globalThis.fetch = () =>
      new Promise<Response>((resolve) => {
        complete = resolve
      })
    const old = api.vehicleMembers("old-vehicle")
    resetSessionRequests()
    complete(new Response("{}", { status: 404 }))
    await assert.rejects(old, ApiError)
    assert.deepEqual(invalidated, ["vehicle-1"])
  } finally {
    unsubscribe()
    globalThis.fetch = originalFetch
  }
})

test("successful data from a previous session cannot repopulate the new session", async () => {
  const originalFetch = globalThis.fetch
  let complete: (response: Response) => void = () => {}
  try {
    globalThis.fetch = () =>
      new Promise<Response>((resolve) => {
        complete = resolve
      })
    const old = api.currentPermissions()
    resetSessionRequests()
    complete(Response.json(grants))
    await assert.rejects(
      old,
      (error: unknown) => error instanceof ApiError && error.status === 401,
    )
  } finally {
    globalThis.fetch = originalFetch
  }
})

test("auth forms validate registration locally and secret fragments preserve encoded tokens", () => {
  assert.equal(newPasswordSchema.safeParse("weak").success, false)
  assert.equal(newPasswordSchema.safeParse("ValidPassword@123").success, true)
  assert.equal(
    credentialsSchema(true).safeParse({
      name: "A",
      email: "bad",
      phone: "1",
      password: "weak",
    }).success,
    false,
  )
  assert.equal(
    credentialsSchema(false).safeParse({
      name: "",
      email: "a@example.com",
      phone: "",
      password: "old-password",
    }).success,
    true,
  )
  assert.deepEqual(
    readSecretFragment("#email=a%40example.com&token=abc%2B%2F%3D"),
    { email: "a@example.com", token: "abc+/=" },
  )
})

test("recovery and invitation APIs keep one-use secrets in POST bodies and cookie CSRF", async () => {
  const originalFetch = globalThis.fetch
  const calls: { path: string; init?: RequestInit }[] = []
  resetSessionRequests()
  try {
    globalThis.fetch = async (input, init) => {
      const path = String(input)
      calls.push({ path, init })
      if (path.endsWith("/csrf")) return Response.json({ token: "csrf-value" })
      if (
        path.endsWith("/forgot-password") ||
        path.endsWith("/verification-email")
      )
        return Response.json({ message: "Accepted" }, { status: 202 })
      if (path.endsWith("/preview"))
        return Response.json({
          id: "invite",
          vehicleId: "vehicle",
          vehicleName: "Car",
          role: "Viewer",
          status: "Pending",
          expiresAtUtc: "2026-11-10T00:00:00Z",
          version: 0,
        })
      return new Response(null, { status: 204 })
    }
    await api.forgotPassword("a@example.com")
    await api.requestVerification("a@example.com")
    await api.verifyEmail({ email: "a@example.com", token: "verify-secret" })
    await api.resetPassword({
      email: "a@example.com",
      token: "reset-secret",
      newPassword: "NewPassword@123",
    })
    await api.previewInvitation("invite", "invite-secret")
    await api.acceptInvitation("invite", "invite-secret")
    await api.rejectInvitation("invite", "invite-secret")
    assert.equal(calls.filter((call) => call.path.endsWith("/csrf")).length, 1)
    assert.equal(
      calls.some((call) => /secret|password=|token=/.test(call.path)),
      false,
    )
    for (const call of calls.slice(1)) {
      assert.equal(call.init?.method, "POST")
      assert.equal(call.init?.credentials, "include")
      assert.equal(
        (call.init?.headers as Record<string, string>)["X-CSRF-TOKEN"],
        "csrf-value",
      )
    }
    assert.deepEqual(JSON.parse(String(calls[3].init?.body)), {
      email: "a@example.com",
      token: "verify-secret",
    })
    assert.deepEqual(JSON.parse(String(calls[6].init?.body)), {
      token: "invite-secret",
    })
  } finally {
    globalThis.fetch = originalFetch
    resetSessionRequests()
  }
})

test("concurrent first mutations share CSRF and an account change cancels a pending command", async () => {
  const originalFetch = globalThis.fetch
  resetSessionRequests()
  let csrfCalls = 0
  let mutations = 0
  let complete: (response: Response) => void = () => {}
  try {
    globalThis.fetch = async (input) => {
      if (String(input).endsWith("/csrf")) {
        csrfCalls += 1
        return new Promise<Response>((resolve) => {
          complete = resolve
        })
      }
      mutations += 1
      return new Response(null, { status: 204 })
    }
    const first = api.acceptInvitation("invite", "secret")
    const second = api.rejectInvitation("invite", "secret")
    assert.equal(csrfCalls, 1)
    resetSessionRequests()
    complete(Response.json({ token: "old-account-token" }))
    const results = await Promise.allSettled([first, second])
    assert.equal(
      results.every((result) => result.status === "rejected"),
      true,
    )
    assert.equal(mutations, 0)
  } finally {
    globalThis.fetch = originalFetch
    resetSessionRequests()
  }
})
