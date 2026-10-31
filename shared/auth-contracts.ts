export type RegisterRequest = {
  name: string
  email: string
  password: string
  phone?: string
}

export type LoginRequest = {
  email: string
  password: string
}

export type AccountResponse = {
  id: string
  name: string
  email: string
  phone: string
  roles: string[]
}

export type CsrfResponse = { token: string }