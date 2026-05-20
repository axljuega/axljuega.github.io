// ============================================================
//  BOINACOIN · system/discord_webhook.cs
//  Tipo: acción interna (llamada desde rank_checker.cs)
//
//  Envía una notificación a Discord cuando un usuario sube
//  o BAJA de rango.
//
//  Args que recibe de rank_checker.cs:
//    · webhookUserId      → userId del usuario
//    · webhookUserName    → nombre del usuario
//    · webhookNewRank     → nuevo rango (int 0-4)
//    · webhookOldRank     → rango anterior (int 0-4, solo en bajadas)
//    · webhookBonus       → BoinaCoins de bonus (long, 0 en bajadas)
//    · webhookIsDowngrade → true si es bajada, false si es subida
// ============================================================

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class CPHInline
{
    // ── URLs de webhook por rango (subidas) ───────────────────
    private const string WEBHOOK_LANA       = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";
    private const string WEBHOOK_CUERO      = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";
    private const string WEBHOOK_TERCIOPELO = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";
    private const string WEBHOOK_LEGENDARIA = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";

    // ── URL de webhook para bajadas de rango ──────────────────
    // Apúntalo al mismo canal que las subidas o a uno distinto.
    // Si quieres que todo vaya al mismo canal, pon la misma URL
    // que cualquiera de los de arriba.
    private const string WEBHOOK_DOWNGRADE  = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";

    // ── IDs de roles de Discord por rango ─────────────────────
    private const string ROLE_ID_LANA       = "ROLE_ID_PLACEHOLDER";
    private const string ROLE_ID_CUERO      = "ROLE_ID_PLACEHOLDER";
    private const string ROLE_ID_TERCIOPELO = "ROLE_ID_PLACEHOLDER";
    private const string ROLE_ID_LEGENDARIA = "ROLE_ID_PLACEHOLDER";

    // ── Colores de embed (decimal) ────────────────────────────
    private const int COLOR_LANA       = 8947848;   // #888780 gris cálido
    private const int COLOR_CUERO      = 1597093;   // #185FA5 azul
    private const int COLOR_TERCIOPELO = 3948425;   // #3C3489 morado
    private const int COLOR_LEGENDARIA = 6504454;   // #633806 ámbar
    private const int COLOR_DOWNGRADE  = 15158332;  // #E74C3C rojo

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId      = args.ContainsKey("webhookUserId")      ? args["webhookUserId"].ToString()                : "";
        string userName    = args.ContainsKey("webhookUserName")    ? args["webhookUserName"].ToString()              : "alguien";
        int    newRank     = args.ContainsKey("webhookNewRank")     ? Convert.ToInt32(args["webhookNewRank"])          : 0;
        int    oldRank     = args.ContainsKey("webhookOldRank")     ? Convert.ToInt32(args["webhookOldRank"])          : -1;
        long   bonus       = args.ContainsKey("webhookBonus")       ? Convert.ToInt64(args["webhookBonus"])            : 0;
        bool   isDowngrade = args.ContainsKey("webhookIsDowngrade") && Convert.ToBoolean(args["webhookIsDowngrade"]);

        if (string.IsNullOrEmpty(userName)) return false;

        // ── Leer stats del usuario ────────────────────────────
        long balance, total;
        int  streak;

        if (!string.IsNullOrEmpty(userId))
        {
            balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");
            streak  = CPH.GetKickUserVarById<int>(userId,  "boinacoin_streak");
            total   = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned");
        }
        else
        {
            balance = CPH.GetKickUserVar<long>(userName, "boinacoin");
            streak  = CPH.GetKickUserVar<int>(userName,  "boinacoin_streak");
            total   = CPH.GetKickUserVar<long>(userName, "boinacoin_total_earned");
        }

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        bool   sent;

        if (!isDowngrade)
        {
            // ════════════════════════════════════════════════
            //  SUBIDA DE RANGO  (lógica original intacta)
            // ════════════════════════════════════════════════
            if (newRank < 1 || newRank > 4) return false;

            string webhookUrl, rankName, rankEmoji, roleId, description;
            int    color;

            switch (newRank)
            {
                case 1:
                    webhookUrl  = WEBHOOK_LANA;
                    rankName    = "Boina de Lana";
                    rankEmoji   = "🧶";
                    roleId      = ROLE_ID_LANA;
                    color       = COLOR_LANA;
                    description = $"**{userName}** acaba de unirse a la comunidad BoinaCoin.\\n" +
                                  $"Ya puede usar `!dado` y `!8ball` en el canal.";
                    break;
                case 2:
                    webhookUrl  = WEBHOOK_CUERO;
                    rankName    = "Boina de Cuero";
                    rankEmoji   = "🪡";
                    roleId      = ROLE_ID_CUERO;
                    color       = COLOR_CUERO;
                    description = $"**{userName}** ha alcanzado la Boina de Cuero.\\n" +
                                  $"Acceso a recompensas Nivel 2 desbloqueado.\\n" +
                                  $"Rol asignado automáticamente en el servidor.";
                    break;
                case 3:
                    webhookUrl  = WEBHOOK_TERCIOPELO;
                    rankName    = "Boina de Terciopelo";
                    rankEmoji   = "💎";
                    roleId      = ROLE_ID_TERCIOPELO;
                    color       = COLOR_TERCIOPELO;
                    description = $"**{userName}** ha llegado a la Boina de Terciopelo.\\n" +
                                  $"Multiplicador x1.25 permanente activado.\\n" +
                                  $"Acceso al canal secreto del servidor. 🎩";
                    break;
                default: // case 4
                    webhookUrl  = WEBHOOK_LEGENDARIA;
                    rankName    = "La Boina Legendaria";
                    rankEmoji   = "👑";
                    roleId      = ROLE_ID_LEGENDARIA;
                    color       = COLOR_LEGENDARIA;
                    description = $"**{userName}** ha alcanzado el rango máximo.\\n" +
                                  $"¡La Boina Legendaria! Multiplicador x1.5 · VIP en el canal.\\n" +
                                  $"Una leyenda viva de la comunidad. 👑🎩👑";
                    break;
            }

            string bonusField  = bonus > 0
                ? $",{{\"name\": \"Bonus\", \"value\": \"+{bonus:N0} 🪙\", \"inline\": true}}"
                : "";

            string roleContent = !string.IsNullOrEmpty(roleId) && !roleId.Contains("PLACEHOLDER")
                ? $"\"content\":\"<@&{roleId}> — nuevo miembro: **{userName}**\","
                : "";

            string payload = "{" +
                roleContent +
                "\"embeds\": [{" +
                    $"\"title\": \"{rankEmoji} ¡Nuevo {rankName}!\"," +
                    $"\"description\": \"{EscapeJson(description)}\"," +
                    $"\"color\": {color}," +
                    "\"fields\": [" +
                        $"{{\"name\": \"Saldo actual\",    \"value\": \"{balance:N0} 🪙\", \"inline\": true}}," +
                        $"{{\"name\": \"Total histórico\", \"value\": \"{total:N0} 🪙\",   \"inline\": true}}," +
                        $"{{\"name\": \"Racha\",           \"value\": \"{streak} streams 🔥\", \"inline\": true}}" +
                        bonusField +
                    "]," +
                    $"\"footer\": {{\"text\": \"BoinaCoin · La Chica de la Boina\"}}," +
                    $"\"timestamp\": \"{timestamp}\"" +
                "}]}";

            sent = SendWebhook(webhookUrl, payload);

            if (sent) CPH.LogInfo($"[BoinaCoin] Webhook subida · {userName} → {rankName}");
            else      CPH.LogWarn($"[BoinaCoin] Webhook subida FALLIDO · {userName} → {rankName}");
        }
        else
        {
            // ════════════════════════════════════════════════
            //  BAJADA DE RANGO
            // ════════════════════════════════════════════════
            if (newRank < 0 || newRank > 4) return false;

            string newRankName = RankName(newRank);
            string oldRankName = (oldRank >= 0 && oldRank <= 4) ? RankName(oldRank) : "rango anterior";

            string title, description;

            if (newRank == 0)
            {
                title       = $"📉 {userName} pierde todos sus rangos";
                description = $"**{userName}** ha caído por debajo de los 1.000 BoinaCoins.\\n" +
                              $"Ha perdido su rango **{oldRankName}** y vuelve a **Boina de Paja**.\\n" +
                              $"*Todos los roles de rango han sido eliminados en el servidor.*";
            }
            else
            {
                title       = $"📉 {userName} baja de rango";
                description = $"**{userName}** ha pasado de **{oldRankName}** a **{newRankName}**.\\n" +
                              $"*Recupera las BoinaCoins perdidas para volver a subir.*";
            }

            string payload = "{\"embeds\": [{" +
                $"\"title\": \"{EscapeJson(title)}\"," +
                $"\"description\": \"{EscapeJson(description)}\"," +
                $"\"color\": {COLOR_DOWNGRADE}," +
                "\"fields\": [" +
                    $"{{\"name\": \"Saldo actual\",    \"value\": \"{balance:N0} 🪙\", \"inline\": true}}," +
                    $"{{\"name\": \"Total histórico\", \"value\": \"{total:N0} 🪙\",   \"inline\": true}}," +
                    $"{{\"name\": \"Racha\",           \"value\": \"{streak} streams 🔥\", \"inline\": true}}" +
                "]," +
                $"\"footer\": {{\"text\": \"BoinaCoin · La Chica de la Boina\"}}," +
                $"\"timestamp\": \"{timestamp}\"" +
            "}]}";

            sent = SendWebhook(WEBHOOK_DOWNGRADE, payload);

            if (sent) CPH.LogInfo($"[BoinaCoin] Webhook bajada · {userName} · {oldRankName} → {newRankName}");
            else      CPH.LogWarn($"[BoinaCoin] Webhook bajada FALLIDO · {userName} · {oldRankName} → {newRankName}");
        }

        return sent;
    }

    // ── Envío HTTP POST al webhook de Discord ─────────────────
    private bool SendWebhook(string url, string jsonPayload)
    {
        if (url.Contains("WEBHOOK_ID") || url.Contains("WEBHOOK_TOKEN"))
        {
            CPH.LogWarn("[BoinaCoin] Webhook URL sin configurar.");
            return false;
        }

        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var content  = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = client.PostAsync(url, content).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode) return true;

                CPH.LogWarn($"[BoinaCoin] Webhook HTTP {(int)response.StatusCode} · {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[BoinaCoin] Webhook excepción: {ex.Message}");
            return false;
        }
    }

    // ── Escapa caracteres especiales JSON ─────────────────────
    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
    }

    private string RankName(int rank)
    {
        switch (rank)
        {
            case 1: return "🧶 Boina de Lana";
            case 2: return "🪡 Boina de Cuero";
            case 3: return "💎 Boina de Terciopelo";
            case 4: return "👑 La Boina Legendaria";
            default: return "Boina de Paja";
        }
    }
}
