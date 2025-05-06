using Content.Server._Eclipse.RateLimiting;
using Content.Server.Chat.Managers;
using Content.Server.Players.RateLimiting;
using Content.Shared._Corvax.CCVar;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;

namespace Content.Server._Corvax.TTS;

public sealed partial class TTSSystem
{
    [Dependency] private readonly RateLimitManager _rateLimitManager = default!;
    [Dependency] private readonly PlayerRateLimitManager _playerRateLimitManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    private const string RateLimitKey = "TTS";

    private void RegisterRateLimits()
    {
        _rateLimitManager.Register(RateLimitKey,
            new SimpleRateLimitRegistration(
                CorvaxCCVars.TTSApiRateLimitPeriod,
                CorvaxCCVars.TTSApiRateLimitCount
            ));
        _playerRateLimitManager.Register(RateLimitKey,
            new RateLimitRegistration(
                CorvaxCCVars.TTSRateLimitPeriod,
                CorvaxCCVars.TTSRateLimitCount,
                RateLimitPlayerLimited)
            );
    }

    private void RateLimitPlayerLimited(ICommonSession player)
    {
        _chat.DispatchServerMessage(player, Loc.GetString("tts-rate-limited"), suppressLog: true);
    }

    private RateLimitStatus HandleRateLimit(ICommonSession player)
    {
        return _playerRateLimitManager.CountAction(player, RateLimitKey);
    }

    private RateLimitStatus CheckQueueRateLimit()
    {
        return _rateLimitManager.CountAction(RateLimitKey);
    }

    private void ReturnQueueRateLimitTicket()
    {
        _rateLimitManager.DecreaseAction(RateLimitKey);
    }
}
