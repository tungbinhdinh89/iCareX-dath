using System.Text;
using System.Text.RegularExpressions;
using iCareX.Domain.Entities;
using iCareX.Domain.Exceptions;

namespace iCareX.Domain.Common;

public static partial class VehicleRegistrationRules
{
    public const decimal MaximumOdometer = 9_999_999_999.99m;
    public static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static string NormalizeName(string value) => Spaces().Replace(value.Trim().Normalize(NormalizationForm.FormC), " ").ToUpperInvariant();
    public static string? NormalizePlate(string? value) => OptionalText(value) is { } text ? GarageRules.NormalizePlate(text) : null;
    public static string? NormalizeVin(string? value) => OptionalText(value) is { } text ? Spaces().Replace(text, "").ToUpperInvariant() : null;

    public static void Validate(Vehicle vehicle, int currentYear)
    {
        if (!Enum.IsDefined(vehicle.VehicleType) || !Enum.IsDefined(vehicle.FuelType) || !Enum.IsDefined(vehicle.OdometerUnit))
            Fail("Loại phương tiện, nhiên liệu hoặc đơn vị ODO không hợp lệ.");
        if (vehicle.VehicleBrandId == Guid.Empty || vehicle.VehicleModelId == Guid.Empty) Fail("Vui lòng chọn hãng và dòng xe.");
        if (vehicle.Year is { } year && (year < 1 || year > currentYear + 1)) Fail("Năm sản xuất phải từ 1 đến năm sau.");
        Length(vehicle.Name, 150, "Tên xe"); Length(vehicle.Plate, 30, "Biển số");
        Length(vehicle.VIN, 50, "VIN"); Length(vehicle.RegistrationId, 100, "Mã đăng ký");
        Length(vehicle.Notes, 2000, "Ghi chú"); Length(vehicle.ImageUrl, 2048, "Ảnh xe");
        if (vehicle.Plate is not null && string.IsNullOrEmpty(NormalizePlate(vehicle.Plate))) Fail("Biển số phải có chữ hoặc số.");
        Number(vehicle.Odometer, MaximumOdometer, false, "ODO");
        if (vehicle.FuelCapacity is { } capacity) Number(capacity, 99_999_999.99m, true, "Dung tích nhiên liệu");
        if (vehicle.PurchasePrice is { } purchase) Number(purchase, 9_999_999_999_999_999.99m, false, "Giá mua");
        if (vehicle.SalePrice is { } sale) Number(sale, 9_999_999_999_999_999.99m, false, "Giá bán");
    }

    private static void Number(decimal value, decimal max, bool positive, string field)
    {
        if (value < 0 || positive && value == 0 || value > max || decimal.Round(value, 2) != value)
            Fail($"{field} phải {(positive ? "lớn hơn 0" : "không âm")}, đúng giới hạn và tối đa 2 chữ số thập phân.");
    }
    private static void Length(string? value, int max, string field) { if (value?.Length > max) Fail($"{field} tối đa {max} ký tự."); }
    private static void Fail(string message) => throw new RuleViolation("VALIDATION", message);
    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
