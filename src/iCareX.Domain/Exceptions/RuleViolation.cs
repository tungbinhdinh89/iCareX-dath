namespace iCareX.Domain.Exceptions;

public sealed class RuleViolation(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
