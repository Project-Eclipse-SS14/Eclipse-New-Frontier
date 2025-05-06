using Robust.Shared.Configuration;

namespace Content.Server._Eclipse.RateLimiting;

/// <summary>
/// Contains all data necessary to register a rate limit with <see cref="RateLimitManager.Register"/>.
/// </summary>
public sealed class SimpleRateLimitRegistration(
    CVarDef<float> cVarLimitPeriodLength,
    CVarDef<int> cVarLimitCount)
{
    /// <summary>
    /// CVar that controls the period over which the rate limit is counted, measured in seconds.
    /// </summary>
    public readonly CVarDef<float> CVarLimitPeriodLength = cVarLimitPeriodLength;

    /// <summary>
    /// CVar that controls how many actions are allowed in a single rate limit period.
    /// </summary>
    public readonly CVarDef<int> CVarLimitCount = cVarLimitCount;
}