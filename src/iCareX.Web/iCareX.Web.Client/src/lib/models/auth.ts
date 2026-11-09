import { z } from "zod"

export const emailSchema = z.string().trim().email("Email không hợp lệ.")
export const newPasswordSchema = z
  .string()
  .min(12, "Mật khẩu cần ít nhất 12 ký tự.")
  .regex(/[a-z]/, "Mật khẩu cần chữ thường.")
  .regex(/[A-Z]/, "Mật khẩu cần chữ hoa.")
  .regex(/[0-9]/, "Mật khẩu cần chữ số.")
  .regex(/[^A-Za-z0-9]/, "Mật khẩu cần ký tự đặc biệt.")

export function credentialsSchema(register: boolean) {
  return z
    .object({
      name: z.string().trim(),
      phone: z.string().trim(),
      email: emailSchema,
      password: z.string().min(1, "Nhập mật khẩu."),
    })
    .superRefine((values, ctx) => {
      if (!register) return
      if (values.name.length < 2 || values.name.length > 60)
        ctx.addIssue({
          code: "custom",
          path: ["name"],
          message: "Họ tên cần từ 2 đến 60 ký tự.",
        })
      if (values.phone && !/^0\d{9}$/.test(values.phone))
        ctx.addIssue({
          code: "custom",
          path: ["phone"],
          message: "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.",
        })
      const password = newPasswordSchema.safeParse(values.password)
      if (!password.success)
        ctx.addIssue({
          code: "custom",
          path: ["password"],
          message: password.error.issues[0].message,
        })
    })
}

export function readSecretFragment(
  fragment: string,
): { email: string; token: string } {
  const values = new URLSearchParams(
    fragment.startsWith("#") ? fragment.slice(1) : fragment,
  )
  return { email: values.get("email") ?? "", token: values.get("token") ?? "" }
}

export function consumeSecretFragment() {
  const values = readSecretFragment(window.location.hash)
  if (window.location.hash)
    window.history.replaceState(
      window.history.state,
      "",
      window.location.pathname + window.location.search,
    )
  return values
}
