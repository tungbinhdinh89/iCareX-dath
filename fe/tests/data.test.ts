import assert from "node:assert/strict"
import test from "node:test"
import {
  api,
  odoOf,
  today,
  validateEntry,
  type Garage,
  type Vehicle,
} from "../src/data.ts"

const vehicle: Vehicle = {
  id: "vehicle-1",
  name: "Toyota Vios",
  plate: "51G-123.45",
  type: "Ô tô",
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
