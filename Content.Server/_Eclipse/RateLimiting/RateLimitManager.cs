using System.Runtime.InteropServices;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._Eclipse.RateLimiting;

public sealed class RateLimitManager
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly Dictionary<string, RegistrationData> _registrations = new();
    private readonly Dictionary<string, RateLimitDatum> _rateLimitData = new();

    public void Register(string key, SimpleRateLimitRegistration registration)
    {
        if (_registrations.ContainsKey(key))
            throw new InvalidOperationException($"Key already registered: {key}");

        var data = new RegistrationData
        {
            Registration = registration,
        };

        _cfg.OnValueChanged(
            registration.CVarLimitCount,
            i => data.LimitCount = i,
            invokeImmediately: true);
        _cfg.OnValueChanged(
            registration.CVarLimitPeriodLength,
            i => data.LimitPeriod = TimeSpan.FromSeconds(i),
            invokeImmediately: true);

        _registrations.Add(key, data);
    }

    public RateLimitStatus CountAction(string key)
    {
        if (!_registrations.TryGetValue(key, out var registration))
            throw new ArgumentException($"Unregistered key: {key}");

        ref var datum = ref CollectionsMarshal.GetValueRefOrAddDefault(_rateLimitData, key, out _);

        var time = _gameTiming.RealTime;
        if (datum.CountExpires < time)
        {
            // Period expired, reset it.
            datum.CountExpires = time + registration.LimitPeriod;
            datum.Count = 0;
        }

        datum.Count += 1;

        if (datum.Count <= registration.LimitCount)
            return RateLimitStatus.Allowed;

        return RateLimitStatus.Blocked;
    }

    public void DecreaseAction(string key)
    {
        if (!_registrations.TryGetValue(key, out var registration))
            throw new ArgumentException($"Unregistered key: {key}");

        ref var datum = ref CollectionsMarshal.GetValueRefOrAddDefault(_rateLimitData, key, out _);
        datum.Count -= 1;
    }

    private sealed class RegistrationData
    {
        public required SimpleRateLimitRegistration Registration { get; init; }
        public TimeSpan LimitPeriod { get; set; }
        public int LimitCount { get; set; }
    }

    private struct RateLimitDatum
    {
        /// <summary>
        /// Time stamp (relative to <see cref="IGameTiming.RealTime"/>) this rate limit period will expire at.
        /// </summary>
        public TimeSpan CountExpires;

        /// <summary>
        /// How many actions have been done in the current rate limit period.
        /// </summary>
        public int Count;
    }
}