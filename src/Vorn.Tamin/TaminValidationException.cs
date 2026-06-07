namespace Vorn.Tamin;

/// <summary>Thrown when provider-bound values fail pre-send validation.</summary>
public sealed class TaminValidationException : ValidationError
{
    /// <summary>Structured validation failures detected before transport.</summary>
    public IReadOnlyList<ValidationFailure> Failures { get; }

    /// <inheritdoc />
    public TaminValidationException(IReadOnlyList<ValidationFailure> failures)
        : base(BuildMessage(failures))
    {
        Failures = failures.Count == 0
            ? throw new ArgumentException("At least one validation failure is required.", nameof(failures))
            : failures;
    }

    private static string BuildMessage(IReadOnlyList<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        return failures.Count == 0
            ? "Validation failed."
            : $"Validation failed: {string.Join("; ", failures.Select(failure => $"{failure.Field} [{failure.Code}] {failure.Message}"))}";
    }
}
