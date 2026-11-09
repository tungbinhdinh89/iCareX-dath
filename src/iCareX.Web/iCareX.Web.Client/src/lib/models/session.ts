/** Only allow internal application paths when restoring navigation after login. */
export function sessionReturnPath(value: unknown): string {
  if (typeof value !== "string" || !value.startsWith("/") || value.startsWith("//") || /[\\\u0000-\u0020]/.test(value)) return "/"
  const pathname = value.split(/[?#]/, 1)[0]
  if (pathname === "/dang-nhap" || pathname.startsWith("/dang-nhap/")) return "/"
  return value
}
