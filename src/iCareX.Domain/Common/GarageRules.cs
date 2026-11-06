using System.Text.RegularExpressions;
using iCareX.Domain.Entities;
using iCareX.Domain.Exceptions;
using iCareX.Domain.Models;

namespace iCareX.Domain.Common;

public static partial class GarageRules
{
    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex PlateCharacters();
    public static string NormalizePlate(string plate) => PlateCharacters().Replace(plate, "").ToUpperInvariant();
    public static decimal CurrentOdo(Vehicle vehicle, IEnumerable<Entry> entries) =>
        entries.Where(e => e.VehicleId == vehicle.Id).Select(e => e.Odo).Append(vehicle.BaseOdo).Max();

    public static void EnsureActive(Vehicle vehicle)
    {
        if (vehicle.ArchivedAtUtc is not null)
            throw new RuleViolation("VEHICLE_ARCHIVED", "Xe đã lưu trữ, không thể thay đổi.");
    }

    public static void ValidateBaseOdo(decimal baseOdo, IEnumerable<Entry> history)
    {
        Number(baseOdo, 0, 9_999_999, "ODO");
        if (history.Any(e => e.Odo < baseOdo))
            throw new RuleViolation("ODO_OUT_OF_SEQUENCE", "ODO ban đầu vượt lịch sử.");
    }

    public static void ValidateEntry(Vehicle vehicle, Entry entry, IEnumerable<Entry> entries, DateOnly today)
    {
        EnsureActive(vehicle);
        if (entry.VehicleId != vehicle.Id || entry.AccountId != vehicle.AccountId)
            throw new RuleViolation("NOT_FOUND", "Không tìm thấy xe đã chọn.");
        if (!Enum.IsDefined(entry.Kind)) throw new RuleViolation("VALIDATION", "Loại bản ghi không hợp lệ.");
        if (entry.Date == default || entry.Date > today)
            throw new RuleViolation("INVALID_DATE", "Ngày ghi nhận không hợp lệ hoặc ở tương lai.");
        if (entry.Title.Trim().Length is < 2 or > 100 || entry.Note.Length > 500)
            throw new RuleViolation("VALIDATION", "Tiêu đề hoặc ghi chú không hợp lệ.");
        Number(entry.Odo, 0, 9_999_999, "ODO");
        Number(entry.Cost, 0, 10_000_000_000, "Chi phí");
        Number(entry.Liters, 0, 500, "Lít");
        if ((entry.Kind is EntryKind.Fuel or EntryKind.Service) && entry.Cost <= 0 ||
            entry.Kind == EntryKind.Fuel && entry.Liters <= 0 ||
            entry.Kind != EntryKind.Fuel && entry.Liters != 0 ||
            entry.Kind == EntryKind.Odo && entry.Cost != 0 ||
            entry.PlanId is not null && entry.Kind != EntryKind.Service)
            throw new RuleViolation("VALIDATION", "Chi phí, số lít hoặc liên kết kế hoạch không hợp lệ.");
        var history = entries.Where(e => e.VehicleId == vehicle.Id && e.Id != entry.Id).ToArray();
        var lower = history.Where(e => e.Date < entry.Date).Select(e => e.Odo).Append(vehicle.BaseOdo).Max();
        var upper = history.Where(e => e.Date > entry.Date).Select(e => (decimal?)e.Odo).Min();
        if (entry.Odo < lower || upper is not null && entry.Odo > upper)
            throw new RuleViolation("ODO_OUT_OF_SEQUENCE", "ODO không nhất quán với lịch sử.");
    }

    public static void ValidatePlan(Vehicle vehicle, MaintenancePlan plan)
    {
        EnsureActive(vehicle);
        if (plan.VehicleId != vehicle.Id || plan.AccountId != vehicle.AccountId)
            throw new RuleViolation("NOT_FOUND", "Không tìm thấy xe đã chọn.");
        if (plan.Title.Trim().Length is < 2 or > 100 || plan.DueDate == default || plan.IntervalMonths is < 1 or > 120)
            throw new RuleViolation("VALIDATION", "Kế hoạch không hợp lệ.");
        Number(plan.DueOdo, 1, 9_999_999, "ODO hạn");
        Number(plan.IntervalKm, 1, 1_000_000, "Chu kỳ km");
    }

    public static PlanState CalculatePlan(MaintenancePlan plan, Vehicle vehicle, IEnumerable<Entry> entries,
        UserSettings settings, DateOnly today)
    {
        var history = entries.ToArray();
        var latest = history.Where(e => e.PlanId == plan.Id && e.VehicleId == vehicle.Id &&
            e.AccountId == plan.AccountId && e.Kind == EntryKind.Service)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Odo).FirstOrDefault();
        var date = latest?.Date.AddMonths(plan.IntervalMonths) ?? plan.DueDate;
        var odo = latest is null ? plan.DueOdo : latest.Odo + plan.IntervalKm;
        var km = odo - CurrentOdo(vehicle, history);
        var days = date.DayNumber - today.DayNumber;
        var status = days < 0 || km < 0 ? "overdue" :
            days <= settings.ThresholdDays || km <= settings.ThresholdKm ? "soon" : "good";
        return new(date, odo, km, days, status);
    }

    public static void Number(decimal value, decimal min, decimal max, string label)
    {
        if (value < min || value > max || decimal.Round(value, 6) != value)
            throw new RuleViolation("VALIDATION", $"{label} vượt giới hạn hoặc có quá 6 chữ số thập phân.");
    }
}
