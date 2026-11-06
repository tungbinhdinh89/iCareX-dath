namespace iCareX.Domain.Models;

public sealed record PlanState(DateOnly DueDate, decimal DueOdo, decimal Km, int Days, string Status);
