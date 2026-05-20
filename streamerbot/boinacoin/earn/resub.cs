// ============================================================
//  BOINACOIN · earn/resub.cs
//  Evento: Subscription renovada en Kick (Resub)
//  Recompensa: +5.000 a +10.000 BoinaCoins según antigüedad
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
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Excluir grupo Chat Bots ────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio BoinaBot ───────────────────
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && userId == botInfo.UserId.ToString()) return false;

        // Meses totales acumulados
        int months = 1;
        if (args.ContainsKey("months"))
            int.TryParse(args["months"].ToString(), out months);

        // ── 1. Determinar base y multiplicador por tramo ─────
        long   baseReward;
        double subMultiplier;
        string tramo;
        int    embedColor;

        if (months >= 12)
        {
            baseReward    = 10_000;
            subMultiplier = 2.5;
            tramo         = $"¡{months} meses! 👑";
            embedColor    = 16766720; // dorado
        }
        else if (months >= 6)
        {
            baseReward    = 7_500;
            subMultiplier = 2.0;
            tramo         = $"¡{months} meses! 💎";
            embedColor    = 10181046; // morado
        }
        else
        {
            baseReward    = 5_000;
            subMultiplier = 1.5;
            tramo         = $"{months} {(months == 1 ? "mes" : "meses")}";
            embedColor    = 7419530;  // morado suave
        }

        CPH.SetKickUserVarById(userId, "boinacoin_multiplier", subMultiplier, true);

        // ── 2. Bonus racha resub ─────────────────────────────
        int resubStreak = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak_sub");
        resubStreak++;
        CPH.SetKickUserVarById(userId, "boinacoin_streak_sub", resubStreak, true);
        long streakBonus = CalculateStreakBonus(resubStreak);

        // ── 3. Recompensa total ──────────────────────────────
        double mult         = GetMultiplier(userId);
        long   earnedBase   = (long)Math.Floor(baseReward * mult);
        long   earnedStreak = (long)Math.Floor(streakBonus * mult);
        long   earned       = earnedBase + earnedStreak;

        // ── 4. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        // ── 5. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 6. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 7. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 7.1 Tracking de sesión ───────────────────────────
        long sSubs = CPH.GetGlobalVar<long>("boinacoin_session_subs", false) + 1;
        CPH.SetGlobalVar("boinacoin_session_subs", sSubs, false);

        // Update session earned & leaderboard
        long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + earned;
        CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

        string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
        var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
        lb[userName] = lb.ContainsKey(userName) ? lb[userName] + earned : earned;
        var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
        CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);

        string subsJson = CPH.GetGlobalVar<string>("boinacoin_session_subs_names", false) ?? "[]";
        var subsList = JsonConvert.DeserializeObject<List<string>>(subsJson) ?? new List<string>();
        if (!subsList.Contains(userName))
        {
            subsList.Add(userName);
            if (subsList.Count > 10) subsList.RemoveAt(0);
            CPH.SetGlobalVar("boinacoin_session_subs_names", JsonConvert.SerializeObject(subsList), false);
        }

        // ── 8. Mensaje en Kick ───────────────────────────────
        string multText   = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        string streakText = streakBonus > 0
            ? $" + {earnedStreak} bonus racha ({resubStreak} meses seguidos 🔥)"
            : "";

        CPH.SendKickMessage(
            $"💜 ¡Gracias por renovar, {userName}! {tramo} · " +
            $"+{earnedBase} BoinaCoins{multText}{streakText} · " +
            $"Saldo: {balance} 🪙 · Multiplicador → x{subMultiplier}");

        // ── 9. Embed Discord #subs-y-follows ─────────────────
        string rankName  = RankName(CPH.GetKickUserVarById<int>(userId, "boinacoin_rank"));
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string streakField = streakBonus > 0
            ? $",{{\"name\": \"Bonus racha\", \"value\": \"+{earnedStreak} 🔥 ({resubStreak} meses seguidos)\", \"inline\": false}}"
            : "";

        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""🔁 ¡Resub! {EscapeJson(tramo)}"",
                ""description"": ""**{EscapeJson(userName)}** lleva {months} {(months == 1 ? "mes" : "meses")} apoyando el canal.\n¡La boina no se quita!"",
                ""color"": {embedColor},
                ""fields"": [
                    {{""name"": ""BoinaCoins ganados"",  ""value"": ""+{earnedBase} 🪙{EscapeJson(multText)}"", ""inline"": true}},
                    {{""name"": ""Saldo total"",         ""value"": ""{balance:N0} 🪙"",                        ""inline"": true}},
                    {{""name"": ""Multiplicador activo"",""value"": ""x{subMultiplier} 💜"",                     ""inline"": true}},
                    {{""name"": ""Rango actual"",        ""value"": ""{EscapeJson(rankName)}"",                 ""inline"": true}}
                    {streakField}
                ],
                ""footer"": {{""text"": ""BoinaCoin · La Chica de la Boina""}},
                ""timestamp"": ""{timestamp}""
            }}]
        }}";

        SendWebhook(WEBHOOK_SUBS_FOLLOWS, payload);

        return true;
    }

    private long CalculateStreakBonus(int streak)
    {
        if (streak >= 24) return 3_000;
        if (streak >= 12) return 2_000;
        if (streak >= 6)  return 1_000;
        if (streak >= 3)  return 500;
        return 0;
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
        CPH.RunAction("BoinaCoin · RankChecker", false);
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
                    CPH.LogWarn($"[Resub] Webhook HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex) { CPH.LogWarn($"[Resub] Webhook error: {ex.Message}"); }
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
