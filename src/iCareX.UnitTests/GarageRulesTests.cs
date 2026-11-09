using iCareX.Domain.Common;
using iCareX.Domain.Entities;
using iCareX.Domain.Exceptions;
using iCareX.Domain.Models;
using iCareX.Application.Abstractions;
using Xunit;

namespace iCareX.UnitTests;

public sealed class GarageRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);
    private readonly Vehicle vehicle = new() { AccountId = Guid.NewGuid(), Odometer = 100 };
    private Entry Record(int day, decimal odo, EntryKind kind = EntryKind.Odo) => new()
    {
        VehicleId = vehicle.Id, AccountId = vehicle.AccountId, Title = "Bản ghi", Date = Today.AddDays(day),
        Odo = odo, Kind = kind, Cost = kind == EntryKind.Odo ? 0 : 100, Liters = kind == EntryKind.Fuel ? 1 : 0
    };

    [Fact]
    public void OdoUsesEarlierAndLaterDaysButAllowsDifferentValuesOnSameDay()
    {
        Entry[] history = [Record(-3, 150), Record(-1, 300), Record(-2, 250)];
        GarageRules.ValidateEntry(vehicle, Record(-2, 200), history, Today);
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateEntry(vehicle, Record(-2, 140), history, Today));
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateEntry(vehicle, Record(-2, 310), history, Today));
    }

    [Fact]
    public void EditingExcludesOriginalEntryAndDeletingMaximumLowersCurrentOdo()
    {
        var entry = Record(-1, 300);
        var edited = Record(-2, 200);
        edited.Id = entry.Id;
        GarageRules.ValidateEntry(vehicle, edited, [entry], Today);
        Assert.Equal(300, GarageRules.CurrentOdo(vehicle, [entry]));
        Assert.Equal(100, GarageRules.CurrentOdo(vehicle, []));
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateBaseOdo(301, [entry]));
    }

    [Fact]
    public void InvalidFinancialValuesArchiveAndPrecisionAreRejected()
    {
        var entry = Record(0, 100, EntryKind.Service);
        entry.Cost = 0;
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateEntry(vehicle, entry, [], Today));
        entry.Cost = 100;
        entry.Liters = 1;
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateEntry(vehicle, entry, [], Today));
        entry.Kind = EntryKind.Fuel;
        entry.Liters = 0;
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateEntry(vehicle, entry, [], Today));
        Assert.Throws<RuleViolation>(() => GarageRules.Number(1.0000001m, 0, 10, "ODO"));
        vehicle.ArchivedAtUtc = DateTimeOffset.UtcNow;
        Assert.Throws<RuleViolation>(() => GarageRules.ValidateEntry(vehicle, Record(0, 100), [], Today));
    }

    [Theory]
    [InlineData(2026, 28)]
    [InlineData(2024, 29)]
    public void CompletionClampsMonthEndAndRemovingServiceRestoresInitialDue(int year, int day)
    {
        var plan = new MaintenancePlan { AccountId = vehicle.AccountId, VehicleId = vehicle.Id,
            DueDate = new(year, 1, 1), DueOdo = 500, IntervalKm = 1000, IntervalMonths = 1 };
        var service = Record(0, 200, EntryKind.Service);
        service.Date = new(year, 1, 31);
        service.PlanId = plan.Id;
        var state = GarageRules.CalculatePlan(plan, vehicle, [service], new(), new(year, 1, 31));
        Assert.Equal(new DateOnly(year, 2, day), state.DueDate);
        Assert.Equal(1200, state.DueOdo);
        Assert.Equal(100, new[] { service }.Sum(e => e.Cost));
        Assert.Equal(plan.DueDate, GarageRules.CalculatePlan(plan, vehicle, [], new(), Today).DueDate);
    }

    [Fact]
    public void LatestServiceUsesDateThenOdoAndRecalculatesAfterEdit()
    {
        var plan = new MaintenancePlan { AccountId = vehicle.AccountId, VehicleId = vehicle.Id,
            DueDate = Today, DueOdo = 500, IntervalKm = 1000, IntervalMonths = 1 };
        var a = Record(-2, 200, EntryKind.Service); a.PlanId = plan.Id;
        var b = Record(-2, 250, EntryKind.Service); b.PlanId = plan.Id;
        Assert.Equal(1250, GarageRules.CalculatePlan(plan, vehicle, [a, b], new(), Today).DueOdo);
        b.Odo = 150;
        Assert.Equal(1200, GarageRules.CalculatePlan(plan, vehicle, [a, b], new(), Today).DueOdo);
    }

    [Theory]
    [InlineData(0, 1000, "soon")]
    [InlineData(100, 100, "soon")]
    [InlineData(-1, 1000, "overdue")]
    [InlineData(100, 99, "overdue")]
    [InlineData(100, 1000, "good")]
    public void DueConditionsUseOrAndZeroIsSoon(int days, int dueOdo, string expected)
    {
        var plan = new MaintenancePlan { DueDate = Today.AddDays(days), DueOdo = dueOdo };
        Assert.Equal(expected, GarageRules.CalculatePlan(plan, vehicle, [], new(), Today).Status);
    }

    [Fact]
    public void DomainAndApplicationCannotReferenceOuterLayers()
    {
        var domainReferences = typeof(Vehicle).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");
        Assert.DoesNotContain(domainReferences, n => n.Contains("EntityFramework") || n.Contains("AspNetCore") || n.StartsWith("iCareX."));
        var appReferences = typeof(IBusinessClock).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "");
        Assert.DoesNotContain(appReferences, n => n.Contains("Infrastructure") || n.Contains("iCareX.Web.Api") || n.Contains("EntityFramework"));
    }
}
