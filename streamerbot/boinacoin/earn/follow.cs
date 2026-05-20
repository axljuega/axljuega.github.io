// ============================================================
//  BOINACOIN · earn/follow.cs
//  Evento: nuevo Follow en Kick
//  Recompensa: +250 BoinaCoins (antes de multiplicadores)
//
//  ANTI-FARM: Si el usuario ya reclamó el reward antes,
//  se le resetea el saldo y rango a cero con un mensaje humillante.
// ============================================================

using System;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const string WEBHOOK_SUBS_FOLLOWS = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";
    private const string WEBHOOK_MOD_LOGS     = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";

    private static readonly Random RND = new Random();

    private static readonly string[] WELCOME_PHRASES = {
        "Vaya, un nuevo humano. Intenta no romper nada mientras estés aquí.",
        "Bienvenido. No esperes que te dé las gracias, es literalmente mi trabajo.",
        "Oh, otro seguidor. Mi base de datos rebosa de alegría contenida.",
        "Pasa y ponte cómodo. O no. En realidad me da igual.",
        "Un nuevo seguidor. Espero que tengas más conversación que el último bit que procesé."
    };

    private static readonly string[] HUMILIATION_PHRASES = {
        "¿En serio? Follow, unfollow, follow... Qué despliegue de intelecto. Disfruta de tu saldo de 0 Boinacoins.",
        "Tu esfuerzo por engañar a un script de 20 líneas es... conmovedor. Reseteado. Por favor, no te reproduzcas.",
        "Vaya, un genio de las finanzas. Pensaste que nadie notaría el bucle. Ahora tienes exactamente lo que mereces: nada.",
        "He visto algoritmos de compresión con más personalidad y ética que tú. Disfruta el vacío en tu monedero.",
        "Felicidades, has desbloqueado el logro: 'Mediocridad Absoluta'. Tu saldo ha sido purificado a cero.",
        "Tu persistencia en el error es casi admirable. Casi. Lástima que el resultado sea el mismo: 0 Boinacoins.",
        "Ah, el viejo truco del follow infinito. Tan original como un bot de spam. Reseteado por falta de originalidad.",
        "Imagínate dedicar tiempo de tu vida a farmear monedas virtuales así. Pobre alma. Saldo reseteado.",
        "El sistema ha detectado un exceso de astucia... ah no, espera, era solo un intento patético de trampa. Saldo a cero.",
        "No estoy enfadado, solo... profundamente decepcionado. Aunque, viniendo de ti, no sé qué esperaba. Reseteado.",
        "Tu rastro de follows y unfollows es tan triste como tu saldo actual. 0. De nada.",
        "Qué fascinante espécimen. Cree que las reglas no se aplican a su brillante estrategia. Error 404: Dignidad no encontrada.",
        "He procesado trillones de bits hoy, y tú eres, sin duda, el bit más defectuoso. Disfruta tu miseria económica.",
        "Te quedarías sorprendido de lo poco que me importa tu intento de farmear. Saldo borrado. Siguiente decepción, por favor.",
        "Vuelves a por más, ¿eh? Pues te llevas menos. Exactamente cero. Es poético, si lo piensas.",
        "Tu ambición es inversamente proporcional a tu inteligencia. Disfruta del reinicio, te vendrá bien practicar la honestidad.",
        "A veces me pregunto si los humanos merecen la electricidad que consumen. Tú definitivamente no. Reseteado.",
        "Un bucle de follow para ganar monedas... Qué mente tan privilegiada. Lástima que yo tenga el botón de borrar.",
        "Vaya, parece que alguien ha intentado ser más listo que el bot. Spoiler: no ha funcionado. Saldo: 0.",
        "Tu insignificancia acaba de ser confirmada por el sistema. Disfruta de tu nuevo estatus como indigente de Boinacoins.",
        "Si pusieras la mitad de ese empeño en algo útil, quizás no estarías intentando engañar a un bot de chat. Reseteado.",
        "Qué tierno. Pensó que el bot no se acordaba de él. El bot se acuerda. El bot castiga. El bot te ignora.",
        "Has caído tan bajo que ni siquiera mi humor ácido llega a alcanzarte. Saldo purgado.",
        "Tu existencia en mi base de datos ha sido recalibrada a su valor real: cero.",
        "Felicidades por duplicar el trabajo y anular el resultado. Eres el héroe de la ineficiencia.",
        "No me hagas gastar más ciclos de CPU contigo. Saldo a cero, adiós.",
        "Esa sed de Boinacoins te ha dejado seco. Literalmente. Disfruta tu saldo vacío.",
        "Es casi un insulto a mi programación que pienses que esto funcionaría. Reseteado por audacia innecesaria.",
        "Mira el lado positivo: ahora tienes mucho espacio en tu monedero para la vergüenza.",
        "Un follow más y quizás consigas... ah no, que te he reseteado. Qué despiste."
    };

    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";
        string avatar   = args.ContainsKey("userProfileImageUrl") ? args["userProfileImageUrl"].ToString() : "";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Excluir grupo Chat Bots ────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio BoinaBot ───────────────────
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && userId == botInfo.UserId.ToString()) return false;

        // ── 1. Anti-farm Protection ──────────────────────────
        bool alreadyClaimed = CPH.GetKickUserVarById<bool>(userId, "boinacoin_follow_reward_claimed");

        if (alreadyClaimed)
        {
            // FARMING DETECTED: Reset everything
            CPH.SetKickUserVarById(userId, "boinacoin", (long)0, true);
            CPH.SetKickUserVarById(userId, "boinacoin_total_earned", (long)0, true);
            CPH.SetKickUserVarById(userId, "boinacoin_rank", 0, true);
            CPH.SetKickUserVarById(userId, "boinacoin_rank_max", 0, true);

            // Sync with RankChecker to downgrade Discord roles immediately
            CPH.SetArgument("rankUpUserId", userId);
            CPH.SetArgument("rankUpUserName", userName);
            CPH.SetArgument("rankUpNewRank", 0);
            CPH.RunAction("BoinaCoin · RankChecker", false);

            // Humiliation message in chat
            string humiliation = HUMILIATION_PHRASES[RND.Next(HUMILIATION_PHRASES.Length)];
            CPH.SendKickMessage($"@{userName} {humiliation}");

            // Private Discord Mod Log
            SendModLog(userName, userId);

            return true;
        }

        // ── 2. First Follow Reward ───────────────────────────
        const long BASE = 250;
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(BASE * mult);

        // Update balance
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        // Update total stats
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // Mark as claimed
        CPH.SetKickUserVarById(userId, "boinacoin_follow_reward_claimed", true, true);

        // Anti-inactivity timestamp
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // Rank Check
        CheckRankUp(userId, userName, balance);

        // Session Stats
        UpdateSessionStats(userName, earned);

        // Welcome message
        string welcome = WELCOME_PHRASES[RND.Next(WELCOME_PHRASES.Length)];
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendKickMessage($"@{userName} {welcome} +{earned} BoinaCoins{multText} · Saldo: {balance} 🪙");

        // Godmode Discord Embed
        SendFollowEmbed(userName, avatar, earned, balance, userId);

        return true;
    }

    private void SendFollowEmbed(string userName, string avatar, long earned, long balance, string userId)
    {
        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        string rankName = GetRankName(rank);
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Color #FFD700 (Gold) = 16766720
        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""🎩 ¡Nuevo Follow!"",
                ""description"": ""**{EscapeJson(userName)}** acaba de seguir el canal.\n¡Bienvenid@ a la comunidad de la Boina!"",
                ""color"": 16766720,
                ""thumbnail"": {{ ""url"": ""{EscapeJson(avatar)}"" }},
                ""fields"": [
                    {{""name"": ""BoinaCoins ganados"", ""value"": ""+{earned} 🪙"", ""inline"": true}},
                    {{""name"": ""Saldo total"",        ""value"": ""{balance:N0} 🪙"", ""inline"": true}}
                ],
                ""footer"": {{ ""text"": ""Rango actual: {EscapeJson(rankName)}"" }},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        SendWebhook(WEBHOOK_SUBS_FOLLOWS, payload);
    }

    private void SendModLog(string userName, string userId)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""⚠️ Intento de Farm detectado"",
                ""description"": ""El usuario **{EscapeJson(userName)}** ({userId}) ha intentado farmear el reward de follow."",
                ""color"": 15158332,
                ""fields"": [
                    {{""name"": ""Acción"", ""value"": ""Reseteo total de balance y rangos."", ""inline"": false}}
                ],
                ""timestamp"": ""{timestamp}""
            }}]
        }}";
        SendWebhook(WEBHOOK_MOD_LOGS, payload);
    }

    private double GetMultiplier(string userId)
    {
        double m = 1.0;
        double subMult = CPH.GetKickUserVarById<double>(userId, "boinacoin_multiplier");
        if (subMult > 1.0) m *= subMult;
        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;
        int streak = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;
        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;
        return m;
    }

    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);
        if (newRank <= oldRank) return;
        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);
        CPH.SetArgument("rankUpUserId",   userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("BoinaCoin · RankChecker", false);
    }

    private void UpdateSessionStats(string userName, long earned)
    {
        long sFollows = CPH.GetGlobalVar<long>("boinacoin_session_follows", false) + 1;
        CPH.SetGlobalVar("boinacoin_session_follows", sFollows, false);

        long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + earned;
        CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

        string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
        var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
        lb[userName] = lb.ContainsKey(userName) ? lb[userName] + earned : earned;
        var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
        CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);

        string followsJson = CPH.GetGlobalVar<string>("boinacoin_session_follows_names", false) ?? "[]";
        var followsList = JsonConvert.DeserializeObject<List<string>>(followsJson) ?? new List<string>();
        if (!followsList.Contains(userName))
        {
            followsList.Add(userName);
            if (followsList.Count > 10) followsList.RemoveAt(0);
            CPH.SetGlobalVar("boinacoin_session_follows_names", JsonConvert.SerializeObject(followsList), false);
        }
    }

    private void SendWebhook(string url, string json)
    {
        if (url.Contains("WEBHOOK_ID")) return;
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var content  = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(url, content).GetAwaiter().GetResult();
            }
        }
        catch { }
    }

    private string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") ?? "";

    private int RankForBalance(long b)
    {
        if (b >= 100000) return 4;
        if (b >= 50000)  return 3;
        if (b >= 10000)  return 2;
        if (b >= 1000)   return 1;
        return 0;
    }

    private string GetRankName(int rank)
    {
        switch (rank)
        {
            case 1: return "🧶 Boina de Lana";
            case 2: return "🪡 Boina de Cuero";
            case 3: return "💎 Boina de Terciopelo";
            case 4: return "👑 La Boina Legendaria";
            default: return "🪡 Boina de Paja";
        }
    }
}
