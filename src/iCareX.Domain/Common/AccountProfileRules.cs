using System.Text.RegularExpressions;
using iCareX.Domain.Exceptions;

namespace iCareX.Domain.Common;

public static partial class AccountProfileRules
{
    [GeneratedRegex("^0[0-9]{9}$")]
    private static partial Regex VietnamesePhone();

    public static (string Name, string Phone) Normalize(string? name, string? phone)
    {
        name = name?.Trim() ?? "";
        phone = phone?.Trim() ?? "";
        if (name.Length is < 2 or > 60 || name.Any(char.IsControl))
            throw new RuleViolation("INVALID_PROFILE", "Tên phải có từ 2 đến 60 ký tự và không chứa ký tự điều khiển.");
        if (phone.Length > 0 && !VietnamesePhone().IsMatch(phone))
            throw new RuleViolation("INVALID_PROFILE", "Nhập số điện thoại Việt Nam gồm 10 chữ số, bắt đầu bằng 0.");
        return (name, phone);
    }
}
