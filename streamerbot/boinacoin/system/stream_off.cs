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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const string WEBHOOK_EVENTOS =
        "https://discord.com/api/webhooks/1505194926462341210/KkyB6TTJxlG_wnSYfuKshyrltCLxX6z3YkA6gEewLPmASu55ttevsJCMn7dT2oHzgy6i";

    private const int COLOR_OFFLINE = 10197915; // #9B59DB morado suave

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // ── 1. Leer stats de sesión ───────────────────────────
        long sessionStart      = CPH.GetGlobalVar<long>("boinacoin_session_start",   false);
        long followsCount      = CPH.GetGlobalVar<long>("boinacoin_session_follows", false);
        long subsCount         = CPH.GetGlobalVar<long>("boinacoin_session_subs",    false);
        long coinsRepartidos   = CPH.GetGlobalVar<long>("boinacoin_session_earned",  false);
        long duelsTotal        = CPH.GetGlobalVar<long>("boinacoin_session_duels_total", false);

        string followsNamesRaw = CPH.GetGlobalVar<string>("boinacoin_session_follows_names", false) ?? "[]";
        string subsNamesRaw    = CPH.GetGlobalVar<string>("boinacoin_session_subs_names",    false) ?? "[]";
        string lbRaw           = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard",   false) ?? "{}";
        string chattersRaw     = CPH.GetGlobalVar<string>("boinacoin_session_chatters",      false) ?? "{}";
        string winnersRaw      = CPH.GetGlobalVar<string>("boinacoin_session_duels_winners", false) ?? "{}";

        var followsNames = JsonConvert.DeserializeObject<List<string>>(followsNamesRaw);
        var subsNames    = JsonConvert.DeserializeObject<List<string>>(subsNamesRaw);
        var lb           = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbRaw);
        var chatters     = JsonConvert.DeserializeObject<Dictionary<string, int>>(chattersRaw);
        var winners      = JsonConvert.DeserializeObject<Dictionary<string, int>>(winnersRaw);

        // ── 2. Calcular duración ──────────────────────────────
        long nowUnix   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long durSecs   = sessionStart > 0 ? nowUnix - sessionStart : 0;
        string durText = FormatDuration(durSecs);

        // ── 3. Construir Mensajes para Kick ───────────────────
        string title = args.ContainsKey("title") ? args["title"].ToString() : "Fin del directo";

        // Mensaje 1: Stats Generales
        string msg1 = $"🎩 DIRECTO FINALIZADO — {title}\n" +
                      $"⏱ Duración: {durText} | 👥 Follows: {followsCount} | ⭐ Subs: {subsCount} | 🪙 Boinacoins repartidas: {coinsRepartidos:N0}";

        if (followsNames.Any()) {
            var lastFollows = followsNames.Skip(Math.Max(0, followsNames.Count - 5)).ToList();
            string names = string.Join(", ", lastFollows);
            if (followsCount > lastFollows.Count) names += $" y {followsCount - lastFollows.Count} más";
            msg1 += $"\n💚 Follows: {names}";
        }

        if (subsNames.Any()) {
            var lastSubs = subsNames.Skip(Math.Max(0, subsNames.Count - 5)).ToList();
            string names = string.Join(", ", lastSubs);
            if (subsCount > lastSubs.Count) names += $" y {subsCount - lastSubs.Count} más";
            msg1 += $"\n⭐ Subs: {names}";
        }

        CPH.SendKickMessage(msg1);

        // Mensaje 2: Rankings (solo si hay datos)
        var parts2 = new List<string>();

        if (lb.Any()) {
            var topEarners = lb.OrderByDescending(kv => kv.Value).Take(3).ToList();
            var earnerLines = topEarners.Select((kv, i) => $"{(i==0?"🥇":i==1?"🥈":"🥉")} {kv.Key} ({kv.Value:N0}🪙)");
            parts2.Add($"🏆 Top Earners: {string.Join(" | ", earnerLines)}");
        }

        if (chatters.Any()) {
            var topChatters = chatters.OrderByDescending(kv => kv.Value).Take(3).ToList();
            var chatterLines = topChatters.Select((kv, i) => $"{(i==0?"📢":i==1?"💬":"💭")} {kv.Key} ({kv.Value} msgs)");
            parts2.Add($"🗣 Top Chatters: {string.Join(" | ", chatterLines)}");
        }

        if (duelsTotal > 0) {
            string topDuelist = winners.Any() ? winners.OrderByDescending(kv => kv.Value).First().Key : "Nadie";
            parts2.Add($"⚔️ Duelos: {duelsTotal} | 👑 MVP: {topDuelist}");
        }

        if (parts2.Any()) {
            parts2.Add("🎩 ¡Gracias por estar ahí! Que vuestras Boinacoins descansen... por ahora.");
            CPH.SendKickMessage(string.Join("\n", parts2));
        } else {
            CPH.SendKickMessage("🎩 ¡Gracias por estar ahí! Que vuestras Boinacoins descansen... por ahora.");
        }

        // ── 4. Embed resumen en Discord ───────────────────────
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string followsStr = followsNames.Any() ? string.Join(", ", followsNames) : "Ninguno";
        string subsStr    = subsNames.Any()    ? string.Join(", ", subsNames)    : "Ninguna";

        string lbStr = lb.Any()
            ? string.Join("\n", lb.OrderByDescending(kv => kv.Value).Take(10).Select((kv, i) => $"{i+1}. **{kv.Key}**: {kv.Value:N0} 🪙"))
            : "Sin actividad económica.";

        string chattersStr = chatters.Any()
            ? string.Join("\n", chatters.OrderByDescending(kv => kv.Value).Take(10).Select((kv, i) => $"{i+1}. **{kv.Key}**: {kv.Value} mensajes"))
            : "Chat silencioso.";

        string winnersStr = winners.Any()
            ? string.Join("\n", winners.OrderByDescending(kv => kv.Value).Take(5).Select((kv, i) => $"{i+1}. **{kv.Key}**: {kv.Value} victorias"))
            : "Sin duelos hoy.";

        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""⚫ Directo finalizado · La Chica de la Boina"",
                ""description"": ""¡Gracias a tod@s por el directo! Hasta la próxima. 🎩\n\n**Título:** {EscapeJson(title)}"",
                ""color"": {COLOR_OFFLINE},
                ""fields"": [
                    {{""name"": ""⏱️ Duración"",              ""value"": ""{durText}"",                    ""inline"": true}},
                    {{""name"": ""❤️ Follows ({followsCount})"", ""value"": ""{EscapeJson(followsStr)}"",     ""inline"": false}},
                    {{""name"": ""🎟️ Subs ({subsCount})"",    ""value"": ""{EscapeJson(subsStr)}"",        ""inline"": false}},
                    {{""name"": ""🪙 Boinacoins repartidas"",  ""value"": ""{coinsRepartidos:N0} 🪙"",      ""inline"": true}},
                    {{""name"": ""⚔️ Duelos totales"",         ""value"": ""{duelsTotal}"",                  ""inline"": true}},
                    {{""name"": ""🏆 Top 10 Earners"",         ""value"": ""{EscapeJson(lbStr)}"",           ""inline"": true}},
                    {{""name"": ""📢 Top 10 Chatters"",        ""value"": ""{EscapeJson(chattersStr)}"",     ""inline"": true}},
                    {{""name"": ""👑 Top Duelistas"",          ""value"": ""{EscapeJson(winnersStr)}"",      ""inline"": true}}
                ],
                ""footer"": {{""text"": ""Boinacoin · La Chica de la Boina""}},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        SendWebhook(WEBHOOK_EVENTOS, payload);

        // ── 5. Limpiar globals de sesión ──────────────────────
        CPH.UnsetGlobalVar("boinacoin_session_start",         false);
        CPH.UnsetGlobalVar("boinacoin_session_follows",       false);
        CPH.UnsetGlobalVar("boinacoin_session_subs",          false);
        CPH.UnsetGlobalVar("boinacoin_session_earned",        false);
        CPH.UnsetGlobalVar("boinacoin_horafeliz",             false);
        CPH.UnsetGlobalVar("boinacoin_session_follows_names", false);
        CPH.UnsetGlobalVar("boinacoin_session_subs_names",    false);
        CPH.UnsetGlobalVar("boinacoin_session_leaderboard",   false);
        CPH.UnsetGlobalVar("boinacoin_session_chatters",      false);
        CPH.UnsetGlobalVar("boinacoin_session_duels_total",   false);
        CPH.UnsetGlobalVar("boinacoin_session_duels_winners", false);

        CPH.LogInfo($"[Boinacoin] StreamOff · sesión cerrada · duración: {durText}");

        return true;
    }

    // ── Formatea segundos en "Xh Ym" ─────────────────────────
    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

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
