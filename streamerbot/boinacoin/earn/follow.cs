// ============================================================
//  BOINACOIN · earn/follow.cs
//  Evento: nuevo Follow en Kick
//  Recompensa: +250 Boinacoins (antes de multiplicadores)
//
//  FIX: Eliminado bloque de exclusión del broadcaster.
//  NEW: Envía embed a Discord #subs-y-follows.
// ============================================================

using System;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const string WEBHOOK_SUBS_FOLLOWS = "https://discord.com/api/webhooks/1505195847103811616/JHAHJXRCGFJ99vyvtTJuFbL-io4Ff-9zgYdzenPa0taTZVXGlG3EqbFGjWhS15RK2Oc_";

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Excluir grupo Chat Bots ────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio BoinaBot ───────────────────
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && userId == botInfo.UserId.ToString()) return false;

        // ── 1. Calcular recompensa ───────────────────────────
        const long BASE = 250;
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(BASE * mult);

        // ── 2. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        // ── 3. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 4. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 5. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 5.1 Tracking de sesión ───────────────────────────
        long sFollows = CPH.GetGlobalVar<long>("boinacoin_session_follows", false) + 1;
        CPH.SetGlobalVar("boinacoin_session_follows", sFollows, false);

        // Update session earned & leaderboard
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

        // ── 6. Mensaje de bienvenida en Kick ─────────────────
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendKickMessage(
            $"🎩 ¡Bienvenid@ {userName}! +{earned} Boinacoins por el follow{multText} · " +
            $"Saldo total: {balance} 🪙");

        // ── 7. Embed Discord #subs-y-follows ─────────────────
        string rankName = RankName(CPH.GetKickUserVarById<int>(userId, "boinacoin_rank"));
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""🎩 ¡Nuevo Follow!"",
                ""description"": ""**{EscapeJson(userName)}** acaba de seguir el canal.\n¡Bienvenid@ a la comunidad de la Boina!"",
                ""color"": 5763719,
                ""fields"": [
                    {{""name"": ""Boinacoins ganados"", ""value"": ""+{earned} 🪙{EscapeJson(multText)}"", ""inline"": true}},
                    {{""name"": ""Saldo total"",        ""value"": ""{balance:N0} 🪙"",            ""inline"": true}},
                    {{""name"": ""Rango actual"",       ""value"": ""{EscapeJson(rankName)}"",     ""inline"": true}}
                ],
                ""footer"": {{""text"": ""Boinacoin · La Chica de la Boina""}},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        SendWebhook(WEBHOOK_SUBS_FOLLOWS, payload);

        return true;
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
        CPH.SendKickMessage($"🎉 ¡{userName} sube a {RankName(newRank)}!");
        CPH.SetArgument("rankUpUserId",   userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("Boinacoin · RankChecker", false);
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
                    CPH.LogWarn($"[Follow] Webhook HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex) { CPH.LogWarn($"[Follow] Webhook error: {ex.Message}"); }
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

    private int RankForBalance(long b)
    {
        if (b >= RANK_LEGENDARIA) return 4;
        if (b >= RANK_TERCIOPELO) return 3;
        if (b >= RANK_CUERO)      return 2;
        if (b >= RANK_LANA)       return 1;
        return 0;
    }

    private string RankName(int rank)
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
