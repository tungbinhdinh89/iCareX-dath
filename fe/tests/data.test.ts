import assert from "node:assert/strict"
import test from "node:test"
import {
  accounts,
  api,
  dateAt,
  odoOf,
  planState,
  today,
  validateEntry,
  type Entry,
} from "../src/data.ts"

test("auth accounts expose the shared response shape", () => {
  assert.ok(accounts.every((account) => account.roles.includes("User")))
})

test("mock results are copied and accounts remain isolated", async () => {
  const first = await api.get(accounts[0].id)
  const second = await api.get(accounts[1].id)
  first.vehicles[0].name = "External mutation"
  assert.notEqual(
    (await api.get(accounts[0].id)).vehicles[0].name,
    "External mutation",
  )
  await api.update(accounts[1].id, (data) => {
    data.profile.phone = "0987654321"
  })
  assert.equal((await api.get(accounts[0].id)).profile.phone, "093 456 7890")
  assert.notEqual(first.vehicles[0].id, second.vehicles[0].id)
})

test("ODO validation checks both earlier and later days", async () => {
  const data = await api.get(accounts[0].id)
  const vehicle = data.vehicles[0]
  const entry: Entry = {
    id: "test-odo",
    vehicleId: vehicle.id,
    kind: "odo",
    title: "ODO",
    date: dateAt(-150),
    odo: vehicle.baseOdo,
    cost: 0,
    liters: 0,
    note: "",
  }
  assert.throws(() => validateEntry(data, entry), /ODO phải/)
  assert.throws(
    () => validateEntry(data, { ...entry, odo: 900000 }),
    /ODO phải/,
  )
  assert.throws(
    () => validateEntry(data, { ...entry, date: dateAt(1), odo: 900000 }),
    /tương lai/,
  )
})

test("multiple entries on one day can record different trip ODOs", async () => {
  const data = await api.get(accounts[0].id)
  const vehicle = data.vehicles[0]
  const entry: Entry = {
    id: "test-today",
    vehicleId: vehicle.id,
    kind: "odo",
    title: "ODO",
    date: today(),
    odo: odoOf(vehicle, data.entries) + 100,
    cost: 0,
    liters: 0,
    note: "",
  }
  assert.doesNotThrow(() => validateEntry(data, entry))
  data.entries.push(entry)
  assert.equal(odoOf(vehicle, data.entries), entry.odo)
  data.entries = data.entries.filter((e) => e.id !== entry.id)
  assert.equal(odoOf(vehicle, data.entries), entry.odo - 100)
})

test("services require a cost and fuel requires a positive volume", async () => {
  const data = await api.get(accounts[0].id)
  const entry: Entry = {
    id: "test-invalid",
    vehicleId: data.vehicles[0].id,
    kind: "service",
    title: "Service",
    date: today(),
    odo: odoOf(data.vehicles[0], data.entries),
    cost: 0,
    liters: 0,
    note: "",
  }
  assert.throws(() => validateEntry(data, entry), /Chi phí/)
  assert.throws(
    () => validateEntry(data, { ...entry, kind: "fuel", cost: 100000 }),
    /Số lít/,
  )
})

test("completion advances a plan without duplicating service cost", async () => {
  const data = await api.get(accounts[0].id)
  const plan = data.plans[0]
  const vehicle = data.vehicles.find((v) => v.id === plan.vehicleId)!
  const totalBefore = data.entries.reduce((sum, entry) => sum + entry.cost, 0)
  const entry: Entry = {
    id: "test-service",
    vehicleId: vehicle.id,
    kind: "service",
    title: plan.title,
    date: today(),
    odo: odoOf(vehicle, data.entries),
    cost: 400000,
    liters: 0,
    note: "",
    planId: plan.id,
  }
  validateEntry(data, entry)
  data.entries.push(entry)
  assert.equal(planState(plan, data).dueOdo, entry.odo + plan.intervalKm)
  assert.equal(planState(plan, data).status, "good")
  assert.equal(
    data.entries.reduce((sum, e) => sum + e.cost, 0),
    totalBefore + entry.cost,
  )
  entry.cost = 500000
  assert.equal(
    data.entries.reduce((sum, e) => sum + e.cost, 0),
    totalBefore + 500000,
  )
  data.entries = data.entries.filter((e) => e.id !== entry.id)
  assert.equal(planState(plan, data).status, "overdue")
  assert.equal(
    data.entries.reduce((sum, e) => sum + e.cost, 0),
    totalBefore,
  )
})

test("a rejected mock transaction does not change the stored data", async () => {
  const before = await api.get(accounts[0].id)
  await assert.rejects(
    api.update(accounts[0].id, (data) => {
      data.vehicles = []
      throw new Error("Reject transaction")
    }),
    /Reject transaction/,
  )
  assert.deepEqual((await api.get(accounts[0].id)).vehicles, before.vehicles)
})

test("monthly maintenance clamps a month-end service to the last valid day", async () => {
  const data = await api.get(accounts[0].id)
  const plan = data.plans[0]
  plan.intervalMonths = 1
  data.entries.push({
    id: "month-end",
    vehicleId: plan.vehicleId,
    planId: plan.id,
    kind: "service",
    title: plan.title,
    date: "2026-01-31",
    odo: 40000,
    cost: 400000,
    liters: 0,
    note: "",
  })
  assert.equal(planState(plan, data).dueDate, "2026-02-28")
})
