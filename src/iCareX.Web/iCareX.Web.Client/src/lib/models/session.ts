/** Only allow internal application paths when restoring navigation after login. */

export function sessionReturnPath(value: unknown): string {
  if (
    typeof value !== "string" ||
    !value.startsWith("/") ||
    value.startsWith("//") ||
    /[\\\u0000-\u0020]/.test(value)
  )
    return "/"
  const pathname = value.split(/[?#]/, 1)[0]
  if (
    [
      "/dang-nhap",
      "/dang-ky",
      "/quen-mat-khau",
      "/dat-lai-mat-khau",
      "/xac-thuc-email",
    ].some((path) => pathname === path || pathname.startsWith(`${path}/`))
  )
    return "/"
  if (
    pathname.startsWith("/loi-moi/") ||
    /[?&#](?:token|code|password)=/i.test(value)
  )
    return "/"
  return value
}
