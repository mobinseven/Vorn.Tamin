namespace Vorn.Tamin;

/// <summary>Represents one structured client-side validation failure detected before transport.</summary>
public sealed record ValidationFailure(string Field, string Code, string Message);
