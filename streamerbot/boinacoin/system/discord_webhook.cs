// ============================================================
//  BOINACOIN · system/discord_webhook.cs
//  Tipo: acción interna (llamada desde rank_checker.cs)
//
//  Envía una notificación a Discord cuando un usuario sube
//  de rango. Cada rango tiene su propio webhook URL y embed
//  personalizado. Los webhooks SOLO se disparan la primera
//  vez que se cruza el umbral (garantizado por el guard de
//  boinacoin_rank_announced en rank_checker.cs).
//
//  Configuración requerida:
//    Sustituir las constantes WEBHOOK_* por las URLs reales
//    de tus webhooks de Discord (Settings → Integrations →
//    Webhooks en tu servidor de Discord).
//
//  Args que recibe de rank_checker.cs:
//    · webhookUserId   → userId del usuario
//    · webhookUserName → nombre del usuario
//    · webhookNewRank  → nuevo rango (int 1-4)
//
//  Cómo configurarlo en Streamer.bot:
//    Acción "Boinacoin · DiscordWebhook"
//    Sub-action: Execute C# (este script)
// ============================================================

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class CPHInline
{
    // ── URLs de webhook por rango ─────────────────────────────
    // Crea un webhook distinto por canal/rango en Discord para
    // poder dirigir cada anuncio al canal correspondiente.
    private const string WEBHOOK_LANA       = "https://discord.com/api/webhooks/TU_WEBHOOK_LANA";
    private const string WEBHOOK_CUERO      = "https://discord.com/api/webhooks/TU_WEBHOOK_CUERO";
    private const string WEBHOOK_TERCIOPELO = "https://discord.com/api/webhooks/TU_WEBHOOK_TERCIOPELO";
    private const string WEBHOOK_LEGENDARIA = "https://discord.com/api/webhooks/TU_WEBHOOK_LEGENDARIA";

    // ── IDs de roles de Discord por rango ─────────────────────
    // Obtén los IDs activando Modo Desarrollador en Discord
    // (Ajustes → Avanzado → Modo Desarrollador) y haciendo
    // clic derecho sobre el rol → Copiar ID.
    // Si no usas asignación automática de roles, deja en "".
    private const string ROLE_ID_LANA       = "";   // ID del rol Boina de Lana
    private const string ROLE_ID_CUERO      = "";   // ID del rol Boina de Cuero
    private const string ROLE_ID_TERCIOPELO = "";   // ID del rol Boina de Terciopelo
    private const string ROLE_ID_LEGENDARIA = "";   // ID del rol La Boina Legendaria

    // ── Colores de embed por rango (formato decimal) ──────────
    private const int COLOR_LANA       = 8947848;  // #888780 gris cálido
    private const int COLOR_CUERO      = 1597093;  // #185FA5 azul
    private const int COLOR_TERCIOPELO = 3948425;  // #3C3489 morado
    private const int COLOR_LEGENDARIA = 6504454;  // #633806 ámbar

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("webhookUserId")   ? args["webhookUserId"].ToString()   : "";
        string userName = args.ContainsKey("webhookUserName") ? args["webhookUserName"].ToString() : "alguien";
        int    newRank  = args.ContainsKey("webhookNewRank")  ? Convert.ToInt32(args["webhookNewRank"]) : 0;

        if (string.IsNullOrEmpty(userId) || newRank < 1 || newRank > 4) return false;

        // ── Seleccionar webhook y datos según rango ───────────
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
                description = $"**{userName}** acaba de unirse a la comunidad Boinacoin.\n" +
                              $"Ya puede usar `!dado` y `!8ball` en el canal.";
                break;

            case 2:
                webhookUrl  = WEBHOOK_CUERO;
                rankName    = "Boina de Cuero";
                rankEmoji   = "🪡";
                roleId      = ROLE_ID_CUERO;
                color       = COLOR_CUERO;
                description = $"**{userName}** ha alcanzado la Boina de Cuero.\n" +
                              $"Acceso a recompensas Nivel 2 desbloqueado.\n" +
                              $"Rol asignado automáticamente en el servidor.";
                break;

            case 3:
                webhookUrl  = WEBHOOK_TERCIOPELO;
                rankName    = "Boina de Terciopelo";
                rankEmoji   = "💎";
                roleId      = ROLE_ID_TERCIOPELO;
                color       = COLOR_TERCIOPELO;
                description = $"**{userName}** ha llegado a la Boina de Terciopelo.\n" +
                              $"Multiplicador x1.25 permanente activado.\n" +
                              $"Acceso al canal secreto del servidor. 🎩";
                break;

            case 4:
                webhookUrl  = WEBHOOK_LEGENDARIA;
                rankName    = "La Boina Legendaria";
                rankEmoji   = "👑";
                roleId      = ROLE_ID_LEGENDARIA;
                color       = COLOR_LEGENDARIA;
                description = $"**{userName}** ha alcanzado el rango máximo.\n" +
                              $"¡La Boina Legendaria! Multiplicador x1.5 · VIP en el canal.\n" +
                              $"Una leyenda viva de la comunidad. 👑🎩👑";
                break;

            default:
                return false;
        }

        // ── Construir payload JSON del embed ──────────────────
        long balance = CPH.GetUserVar<long>(userId, "boinacoin", true);
        int  streak  = CPH.GetUserVar<int>(userId,  "boinacoin_streak", true);
        long total   = CPH.GetUserVar<long>(userId,  "boinacoin_total_earned", true);

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string roleText  = !string.IsNullOrEmpty(roleId)
            ? $",\"content\":\"<@&{roleId}> — nuevo miembro: **{userName}**\""
            : "";

        string payload = $@"{{
            {roleText.TrimStart(',')}
            ""embeds"": [{{
                ""title"": ""{rankEmoji} ¡Nuevo {rankName}!"",
                ""description"": ""{EscapeJson(description)}"",
                ""color"": {color},
                ""fields"": [
                    {{""name"": ""Saldo actual"",   ""value"": ""{balance:N0} 🪙"", ""inline"": true}},
                    {{""name"": ""Total histórico"", ""value"": ""{total:N0} 🪙"",  ""inline"": true}},
                    {{""name"": ""Racha"",           ""value"": ""{streak} streams 🔥"", ""inline"": true}}
                ],
                ""footer"": {{""text"": ""Boinacoin · La Chica de la Boina""}},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        // ── Enviar webhook ────────────────────────────────────
        bool sent = SendWebhook(webhookUrl, payload);

        if (sent)
            CPH.LogInfo($"[Boinacoin] Webhook enviado · {userName} → {rankName}");
        else
            CPH.LogWarn($"[Boinacoin] Webhook FALLIDO · {userName} → {rankName}");

        return sent;
    }

    // ── Envío HTTP POST al webhook de Discord ─────────────────
    private bool SendWebhook(string url, string jsonPayload)
    {
        if (url.Contains("TU_WEBHOOK"))
        {
            CPH.LogWarn("[Boinacoin] Webhook URL sin configurar. Edita las constantes WEBHOOK_* en discord_webhook.cs");
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

                CPH.LogWarn(
                    $"[Boinacoin] Webhook HTTP {(int)response.StatusCode} · " +
                    $"{response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[Boinacoin] Webhook excepción: {ex.Message}");
            return false;
        }
    }

    // ── Escapa caracteres especiales JSON en strings ──────────
    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
    }
}
