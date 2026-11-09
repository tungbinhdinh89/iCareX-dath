import assert from "node:assert/strict"
import test from "node:test"
import {
  odoOf,
  today,
  validateEntry,
  type Garage,
  type Vehicle,
} from "../src/lib/models/garage.ts"
import { api, ApiError, onUnauthorized, resetSessionRequests } from "../src/lib/apis/iCareXApi.ts"
import { sessionReturnPath } from "../src/lib/models/session.ts"

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
  baseOdo: 30000,
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
  assert.throws(() => validateEntry(garage, { ...entry, date: "2999-01-01" }), /tương lai/)
  assert.throws(() => validateEntry(garage, { ...entry, odo: -1 }), /không âm/)
})

test("current ODO is derived from vehicle base and readings", () => {
  assert.equal(odoOf(vehicle, []), 30000)
  assert.equal(
    odoOf(vehicle, [{
      id: "reading-2",
      vehicleId: vehicle.id,
      kind: "odo",
      title: "ODO",
      date: today(),
      odo: 30125,
      cost: 0,
      liters: 0,
      note: "",
    }]),
    30125,
  )
})

test("login return preserves internal filters and rejects external or recursive navigation", () => {
  assert.equal(sessionReturnPath("/xe/vehicle-1?tab=odo#history"), "/xe/vehicle-1?tab=odo#history")
  for (const value of ["https://example.com", "//example.com", "/\\example.com", "/dang-nhap", "/dang-nhap?from=/xe", "/\nexample.com", null, {}]) {
    assert.equal(sessionReturnPath(value), "/")
  }
})

test("session restoration distinguishes expired cookies from server and network errors", async () => {
  const originalFetch = globalThis.fetch
  try {
    for (const status of [401, 503]) {
      globalThis.fetch = async () => new Response(JSON.stringify({ detail: "Không thể xác thực." }), { status })
      await assert.rejects(api.me(), (error: unknown) => error instanceof ApiError && error.status === status)
    }
    globalThis.fetch = async () => { throw new TypeError("Network unavailable") }
    await assert.rejects(api.me(), (error: unknown) => error instanceof TypeError && !(error instanceof ApiError))
  } finally {
    globalThis.fetch = originalFetch
  }
})

test("protected API expiry notifies session boundary while other failures keep the account", async () => {
  const originalFetch = globalThis.fetch
  let expired = 0
  const unsubscribe = onUnauthorized(() => { expired += 1 })
  const account = { id: "account-1", name: "Test", email: "test@example.com", phone: "", roles: [] }
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
  const unsubscribe = onUnauthorized(() => { expired += 1 })
  let complete: (response: Response) => void = () => {}
  try {
    globalThis.fetch = () => new Promise<Response>((resolve) => { complete = resolve })
    const request = api.get({ id: "previous-account", name: "Old", email: "old@example.com", phone: "", roles: [] })
    resetSessionRequests()
    complete(new Response("{}", { status: 401 }))
    await assert.rejects(request, ApiError)
    assert.equal(expired, 0)
  } finally {
    unsubscribe()
    globalThis.fetch = originalFetch
  }
})
