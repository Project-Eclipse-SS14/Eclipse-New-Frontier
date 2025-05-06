using System.Threading.Tasks;
using Content.Server.Discord;
using Content.Shared._Eclipse.CCVar;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    [Dependency] private readonly DiscordWebhook _discord = default!;

    private string _webhookUrl = default!;

    private void InitializeDiscord()
    {
        _cfg.OnValueChanged(EclipseCCVars.DiscordBanNotificationWebhook, (webhookUrl) => _webhookUrl = webhookUrl, true);
    }

    public async Task SendDiscordNotification(string adminName, string targetName, DateTimeOffset? expires, string reason)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        try
        {
            var webhookData = await _discord.GetWebhook(_webhookUrl);

            var webhookIdentifier = webhookData.Value.ToIdentifier();

            var expiresAt = expires == null ? Loc.GetString("server-ban-string-never") : $"<t:{expires.Value.ToUnixTimeSeconds()}:R>";

            var message = Loc.GetString(
                "discord-ban-notification-message",
                ("username", targetName),
                ("expiresAt", expiresAt),
                ("reason", reason),
                ("adminUsername", adminName)
                );

            var payload = new WebhookPayload
            {
                Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Title = Loc.GetString("discord-ban-notification-title"),
                    Description = message,
                    Color = 0xFF0000, // red
                },
            },
            };

            await _discord.CreateMessage(webhookIdentifier, payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending discord ban message:\n{e}");
        }
    }
}
