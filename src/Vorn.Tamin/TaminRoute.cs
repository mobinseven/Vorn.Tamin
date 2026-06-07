namespace Vorn.Tamin;

/// <summary>Represents one provider operation route in one EP.Tamin environment.</summary>
public sealed record TaminRoute(TaminEndpoint Environment, TaminOperation Operation, Uri Uri);
