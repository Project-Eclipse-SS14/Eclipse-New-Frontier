using Robust.Shared.Configuration;

namespace Content.Shared._Eclipse.CCVar;

[CVarDefs]
public sealed class EclipseCCVars
{
    public static readonly CVarDef<bool> RestartWhenServerEmpty =
        CVarDef.Create("eclipse.restart_when_server_empty", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> StartRoundWithNoPlayers =
        CVarDef.Create("eclipse.start_round_with_no_players", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordInteractAlertWebhook =
        CVarDef.Create("eclipse.discord_interact_alert_webhook", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<bool> EmergencyShuttleAutoCallEnabled =
        CVarDef.Create("eclipse.shuttle_auto_call_enabled", true, CVar.SERVERONLY);
    public static readonly CVarDef<string> DiscordBanNotificationWebhook =
        CVarDef.Create("discord.ban_notification_webhook", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<int> BoomBoxAudioLimitConcurrent =
        CVarDef.Create("boombox.audio_limit_concurrent", 4, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum offset for audio to be played at from its full duration. If it's past this then the audio won't be played.
    /// </summary>
    public static readonly CVarDef<float> BoomBoxAudioEndBuffer =
        CVarDef.Create("boombox.audio_end_buffer", 0.01f, CVar.REPLICATED);
}