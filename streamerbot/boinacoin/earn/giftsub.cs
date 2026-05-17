// ============================================================
//  BOINACOIN · earn/giftsub.cs
//  Evento: Gift Subscription individual en Kick
//  Recompensa: +2.500 Boinacoins al GIFTER
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

    private const string WEBHOOK_SUBS_FOLLOWS = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string gifterId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string gifterName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(gifterId)) return false;

        // ── 0. Excluir grupo Chat Bots ────────────────────────
        if (CPH.UserInGroup(gifterName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio BoinaBot ───────────────────
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && gifterId == botInfo.UserId.ToString()) return false;

        string recipientName = args.ContainsKey("recipientUserName")
            ? args["recipientUserName"].ToString()
            : "alguien";

        // ── 1. Calcular recompensa ───────────────────────────
        const long BASE = 2_500;
        double mult   = GetMultiplier(gifterId);
        long   earned = (long)Math.Floor(BASE * mult);

        // ── 2. Actualizar saldo del gifter ───────────────────
        long balance = CPH.GetKickUserVarById<long>(gifterId, "boinacoin") + earned;
        CPH.SetKickUserVarById(gifterId, "boinacoin", balance, true);

        // ── 3. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(gifterId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(gifterId, "boinacoin_total_earned", totalEarned, true);

        // ── 4. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(gifterId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 5. Comprobar subida de rango ─────────────────────
        CheckRankUp(gifterId, gifterName, balance);

        // ── 5.1 Tracking de sesión (receptor) ────────────────
        long sSubs = CPH.GetGlobalVar<long>("boinacoin_session_subs", false) + 1;
        CPH.SetGlobalVar("boinacoin_session_subs", sSubs, false);

        // Update session earned & leaderboard (for the GIFTER)
        long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + earned;
        CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

        string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
        var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
        lb[gifterName] = lb.ContainsKey(gifterName) ? lb[gifterName] + earned : earned;
        var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
        CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);

        string subsJson = CPH.GetGlobalVar<string>("boinacoin_session_subs_names", false) ?? "[]";
        var subsList = JsonConvert.DeserializeObject<List<string>>(subsJson) ?? new List<string>();
        if (!subsList.Contains(recipientName))
        {
            subsList.Add(recipientName);
            if (subsList.Count > 10) subsList.RemoveAt(0);
            CPH.SetGlobalVar("boinacoin_session_subs_names", JsonConvert.SerializeObject(subsList), false);
        }

        // ── 6. Mensaje en Kick ───────────────────────────────
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendKickMessage(
            $"🎁 ¡{gifterName} ha regalado una sub a {recipientName}! " +
            $"+{earned} Boinacoins{multText} · Saldo: {balance} 🪙");

        // ── 7. Embed Discord #subs-y-follows ─────────────────
        string rankName  = RankName(CPH.GetKickUserVarById<int>(gifterId, "boinacoin_rank"));
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""🎁 ¡Gift Sub!"",
                ""description"": ""**{EscapeJson(gifterName)}** ha regalado una sub a **{EscapeJson(recipientName)}**.\nQué generoso. O sospechoso. Probablemente las dos cosas."",
                ""color"": 15844367,
                ""fields"": [
                    {{""name"": ""Gifter"",              ""value"": ""{EscapeJson(gifterName)}"",              ""inline"": true}},
                    {{""name"": ""Receptor"",            ""value"": ""{EscapeJson(recipientName)}"",            ""inline"": true}},
                    {{""name"": ""Boinacoins ganados"",  ""value"": ""+{earned} 🪙{EscapeJson(multText)}"",    ""inline"": true}},
                    {{""name"": ""Saldo gifter"",        ""value"": ""{balance:N0} 🪙"",                       ""inline"": true}},
                    {{""name"": ""Rango gifter"",        ""value"": ""{EscapeJson(rankName)}"",                ""inline"": true}}
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
                    CPH.LogWarn($"[GiftSub] Webhook HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex) { CPH.LogWarn($"[GiftSub] Webhook error: {ex.Message}"); }
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
