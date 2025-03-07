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

    /*
    * Shipyard
    */
    /// <summary>
    /// Whether the Self Shipyard is enabled.
    /// </summary>
    public static readonly CVarDef<bool> SelfShipyard =
        CVarDef.Create("shuttle.self_shipyard", true, CVar.SERVERONLY);

    /// <summary>
    /// Constant save rate in spessos (gets added with shuttle.self_shipyard_percent_save_rate to calculate total)
    /// </summary>
    public static readonly CVarDef<int> SelfShipyardConstantSaveRate =
        CVarDef.Create("shuttle.self_shipyard_constant_save_rate", 50000, CVar.SERVERONLY);

    /// <summary>
    /// Percent save rate in spessos (gets added with shuttle.self_shipyard_constant_save_rate to calculate total)
    /// </summary>
    public static readonly CVarDef<float> SelfShipyardPercentSaveRate =
        CVarDef.Create("shuttle.self_shipyard_percent_save_rate", 1.05f, CVar.SERVERONLY);
}