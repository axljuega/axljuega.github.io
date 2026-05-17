// ============================================================
//  BOINACOIN · system/stream_off.cs
//  Trigger: Kick → Channel → Stream Offline
//
//  Responsabilidades:
//    1. Leer stats de sesión acumuladas durante el directo
//    2. Calcular duración
//    3. Enviar resumen embed a Discord #eventos-stream
//    4. Limpiar globals de sesión
//
//  Cómo configurarlo en Streamer.bot:
//    Acción "Boinacoin · StreamOff"
//    Trigger: Kick → Channel → Stream Offline
//    Sub-action: Execute C# (este script)
// ============================================================

using System;
using System.Net.Http;
using System.Text;

public class CPHInline
{
    private const string WEBHOOK_EVENTOS =
        "https://discord.com/api/webhooks/1505194926462341210/KkyB6TTJxlG_wnSYfuKshyrltCLxX6z3YkA6gEewLPmASu55ttevsJCMn7dT2oHzgy6i";

    private const int COLOR_OFFLINE = 10197915; // #9B59DB morado suave

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // ── 1. Leer stats de sesión ───────────────────────────
        long sessionStart  = CPH.GetGlobalVar<long>("boinacoin_session_start",   false);
        long follows       = CPH.GetGlobalVar<long>("boinacoin_session_follows", false);
        long subs          = CPH.GetGlobalVar<long>("boinacoin_session_subs",    false);
        long coinsRepartidos = CPH.GetGlobalVar<long>("boinacoin_session_earned", false);

        // ── 2. Calcular duración ──────────────────────────────
        long nowUnix   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long durSecs   = sessionStart > 0 ? nowUnix - sessionStart : 0;
        string durText = FormatDuration(durSecs);

        // ── 3. Embed resumen en Discord ───────────────────────
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""⚫ Directo finalizado · La Chica de la Boina"",
                ""description"": ""¡Gracias a tod@s por el directo! Hasta la próxima. 🎩"",
                ""color"": {COLOR_OFFLINE},
                ""fields"": [
                    {{""name"": ""⏱️ Duración"",              ""value"": ""{durText}"",                    ""inline"": true}},
                    {{""name"": ""❤️ Nuevos follows"",         ""value"": ""{follows}"",                   ""inline"": true}},
                    {{""name"": ""🎟️ Suscripciones"",          ""value"": ""{subs}"",                      ""inline"": true}},
                    {{""name"": ""🪙 Boinacoins repartidas"",  ""value"": ""{coinsRepartidos:N0} 🪙"",      ""inline"": true}}
                ],
                ""footer"": {{""text"": ""Boinacoin · La Chica de la Boina""}},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        SendWebhook(WEBHOOK_EVENTOS, payload);

        // ── 4. Limpiar globals de sesión ──────────────────────
        CPH.UnsetGlobalVar("boinacoin_session_start",   false);
        CPH.UnsetGlobalVar("boinacoin_session_follows", false);
        CPH.UnsetGlobalVar("boinacoin_session_subs",    false);
        CPH.UnsetGlobalVar("boinacoin_session_earned",  false);
        CPH.UnsetGlobalVar("boinacoin_horafeliz",       false);

        CPH.LogInfo($"[Boinacoin] StreamOff · sesión cerrada · duración: {durText}");

        return true;
    }

    // ── Formatea segundos en "Xh Ym" ─────────────────────────
    private string FormatDuration(long secs)
    {
        if (secs <= 0) return "desconocida";
        TimeSpan t = TimeSpan.FromSeconds(secs);
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}h {t.Minutes}m";
        return $"{t.Minutes}m {t.Seconds}s";
    }

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
                    CPH.LogWarn($"[StreamOff] Webhook HTTP {(int)response.StatusCode} · {response.ReasonPhrase}");
                else
                    CPH.LogInfo("[StreamOff] Webhook resumen enviado correctamente.");
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[StreamOff] Webhook error: {ex.Message}");
        }
    }
}
