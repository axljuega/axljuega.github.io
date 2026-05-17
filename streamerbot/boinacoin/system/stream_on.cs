// ============================================================
//  BOINACOIN · system/stream_on.cs
//  Trigger: Kick → Channel → Stream Online
//
//  Responsabilidades:
//    1. Resetear stats de sesión (globals)
//    2. Enviar embed @everyone a Discord #eventos-stream
//    3. Mensaje de bienvenida en chat de Kick
//
//  Cómo configurarlo en Streamer.bot:
//    Acción "Boinacoin · StreamOn"
//    Trigger: Kick → Channel → Stream Online
//    Sub-action: Execute C# (este script)
// ============================================================

using System;
using System.Net.Http;
using System.Text;

public class CPHInline
{
    private const string WEBHOOK_EVENTOS = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";

    // ── Color verde streaming ─────────────────────────────────
    private const int COLOR_LIVE = 5763719; // #57F287

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // ── 1. Resetear stats de sesión ───────────────────────
        CPH.SetGlobalVar("boinacoin_session_start",    DateTimeOffset.UtcNow.ToUnixTimeSeconds(), false);
        CPH.SetGlobalVar("boinacoin_session_follows",  0L, false);
        CPH.SetGlobalVar("boinacoin_session_subs",     0L, false);
        CPH.SetGlobalVar("boinacoin_session_earned",   0L, false);
        CPH.SetGlobalVar("boinacoin_horafeliz",        false, false);

        // Nuevos trackings de sesión
        CPH.SetGlobalVar("boinacoin_session_follows_names", "[]", false);
        CPH.SetGlobalVar("boinacoin_session_subs_names",    "[]", false);
        CPH.SetGlobalVar("boinacoin_session_leaderboard",   "{}", false); // JSON dict: username -> amount
        CPH.SetGlobalVar("boinacoin_session_chatters",      "{}", false); // JSON dict: username -> count
        CPH.SetGlobalVar("boinacoin_session_duels_total",   0L,   false);
        CPH.SetGlobalVar("boinacoin_session_duels_winners", "{}", false); // JSON dict: username -> wins

        CPH.LogInfo("[Boinacoin] StreamOn · stats de sesión reseteadas.");

        // ── 2. Obtener título del stream (si está disponible) ─
        string streamTitle = args.ContainsKey("streamTitle")
            ? args["streamTitle"].ToString()
            : "¡Directo en marcha!";
        string channelName = args.ContainsKey("channelName")
            ? args["channelName"].ToString()
            : "La Chica de la Boina";

        // ── 3. Embed Discord con @everyone ────────────────────
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string payload = $@"{{
            ""content"": ""@everyone 🎩 **{EscapeJson(channelName)}** está en directo ahora mismo en Kick!"",
            ""embeds"": [{{
                ""title"": ""🔴 {EscapeJson(streamTitle)}"",
                ""description"": ""¡El directo ha comenzado! Entra y gana Boinacoins solo por ver el stream.\n\n🔗 [Ver directo en Kick](https://kick.com/LaChicaDeLaBoina)"",
                ""color"": {COLOR_LIVE},
                ""fields"": [
                    {{""name"": ""📅 Hora de inicio"", ""value"": ""<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:t>"", ""inline"": true}},
                    {{""name"": ""🪙 Boinacoins"",     ""value"": ""Activas · gana por ver y chatear"", ""inline"": true}}
                ],
                ""footer"": {{""text"": ""Boinacoin · La Chica de la Boina""}},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        SendWebhook(WEBHOOK_EVENTOS, payload);

        // ── 4. Mensaje de bienvenida en Kick ──────────────────
        CPH.SendKickMessage(
            "🎩 ¡El directo ha comenzado! Escribe en el chat para ganar Boinacoins. " +
            "Usa !boinas para ver tu saldo y !top para el ranking. ¡Buenas a tod@s! 🪙");

        return true;
    }

    // ── Envío HTTP POST al webhook de Discord ─────────────────
    private void SendWebhook(string url, string json)
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var content  = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(url, content).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    CPH.LogWarn($"[StreamOn] Webhook HTTP {(int)response.StatusCode} · {response.ReasonPhrase}");
                else
                    CPH.LogInfo("[StreamOn] Webhook Discord enviado correctamente.");
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[StreamOn] Webhook error: {ex.Message}");
        }
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
}
