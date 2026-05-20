// ============================================================
//  BOINACOIN · earn/follow.cs
//  Evento: nuevo Follow en Kick
//  Recompensa: +250 BoinaCoins (solo la primera vez)
//
//  ANTI-FARM: Si el usuario ya reclamó la recompensa y vuelve
//  a dar follow (unfollow/follow), se le resetea el saldo a 0
//  y se le humilla públicamente.
// ============================================================

using System;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private static readonly Random RND = new Random();

    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const string WEBHOOK_SUBS_FOLLOWS = "https://discord.com/api/webhooks/WEBHOOK_ID/WEBHOOK_TOKEN";

    private static readonly string[] HUMILIATION_POOL = {
        "Congratulations, you've reset yourself to zero. Twice the effort, zero the dignity.",
        "Farming Boinacoins. In a Kick chat. Let that sink in.",
        "Your greed is only matched by your incompetence. Enjoy the 0 balance.",
        "The Boina sees through your cheap tricks. Back to the bottom, peasant.",
        "A pathetic attempt at farming. Your balance is now as empty as your prospects.",
        "Did you think I wouldn't notice? Your BoinaCoins are gone, just like your self-respect.",
        "Error 404: Integrity not found. Resetting your existence to zero.",
        "Imagine trying to exploit a bot and still failing this hard. Zeroed.",
        "The House always wins, especially against bottom-feeders like you.",
        "Your Boina rank has been demoted to 'Disappointment'. Balance: 0.",
        "I’d call you a clown, but clowns actually get paid. You get nothing.",
        "Farming follows is for the weak. Losing everything is for the stupid.",
        "Your digital wallet is now as vacant as your head. Enjoy the reset.",
        "Play stupid games, win zero Boinacoins. Hope it was worth it.",
        "A bold move, being this dishonest and this bad at it. Back to zero.",
        "Your contribution to this channel is now officially 0. Literally.",
        "The blockchain of your life just suffered a 51% attack. You lost everything.",
        "I expected nothing and you still disappointed me. Welcome back to zero.",
        "Some people farm for a living. You farm for a ban. I’ll start with a reset.",
        "You tried to outsmart the system. The system just deleted your progress.",
        "Your Boinacoin balance is now reflecting your IQ. Zero.",
        "Was it worth the 250 coins? Because it just cost you everything.",
        "The Boina doesn't reward rats. Back to the starting line, rodent.",
        "You've been liquidated. Your dignity was the collateral.",
        "Nice try, farm boy. Now go cry in a corner with your 0 coins.",
        "Cheaters never prosper. In your case, they also lose their historical progress.",
        "I could explain why this happened, but I doubt you’d understand. 0 coins for you.",
        "Your status has been updated from 'Follower' to 'Leech'. Balance cleared.",
        "Look at you, all that effort just to end up with nothing. Peak efficiency.",
        "The Boina has spoken. Your greed is your undoing. Reset complete."
    };

    private static readonly string[] WELCOME_POOL = {
        "Welcome to the elite circle of Boina wearers.",
        "Another soul joins the collective. Make yourself useful.",
        "The Boina recognizes your presence. For now.",
        "Welcome. Try not to embarrass yourself in front of the others.",
        "A new apprentice appears. Let's see if you last a week."
    };

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

        // ── 1. Anti-Farm Check ───────────────────────────────
        bool alreadyClaimed = CPH.GetKickUserVarById<bool>(userId, "boinacoin_follow_reward_claimed");

        if (alreadyClaimed)
        {
            // PUNISHMENT
            CPH.SetKickUserVarById(userId, "boinacoin", 0L, true);
            CPH.SetKickUserVarById(userId, "boinacoin_total_earned", 0L, true);
            CPH.SetKickUserVarById(userId, "boinacoin_rank", 0, true);
            CPH.SetKickUserVarById(userId, "boinacoin_rank_max", 0, true);

            // Update Rank / Discord immediately
            CPH.SetArgument("rankUpUserId", userId);
            CPH.SetArgument("rankUpUserName", userName);
            CPH.SetArgument("rankUpNewRank", 0);
            CPH.RunAction("BoinaCoin · RankChecker", false);

            string msg = HUMILIATION_POOL[RND.Next(HUMILIATION_POOL.Length)];
            CPH.SendKickMessage($"@{userName} {msg}");

            return true;
        }

        // ── 2. Calcular recompensa ───────────────────────────
        const long BASE = 250;
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(BASE * mult);

        // ── 3. Actualizar saldo y flag ──────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);
        CPH.SetKickUserVarById(userId, "boinacoin_follow_reward_claimed", true, true);

        // ── 4. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 5. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 6. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 6.1 Tracking de sesión ───────────────────────────
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

        // ── 7. Mensaje de bienvenida en Kick ─────────────────
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendKickMessage(
            $"🎩 ¡Bienvenid@ @{userName}! +{earned} BoinaCoins por el follow{multText} · " +
            $"Saldo total: {balance} 🪙");

        // ── 8. Embed Discord #subs-y-follows (Godmode) ───────
        string welcomePhrase = WELCOME_POOL[RND.Next(WELCOME_POOL.Length)];
        int currentRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        string rankName = RankName(currentRank);
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        string payload = $@"{{
            ""embeds"": [{{
                ""title"": ""🎩 ¡Nuevo Follow!"",
                ""description"": ""**{EscapeJson(userName)}** acaba de seguir el canal.\n_{EscapeJson(welcomePhrase)}_"",
                ""color"": 5763719,
                ""fields"": [
                    {{""name"": ""Recompensa"",     ""value"": ""+{earned} 🪙{EscapeJson(multText)}"", ""inline"": true}},
                    {{""name"": ""Saldo total"",    ""value"": ""{balance:N0} 🪙"",            ""inline"": true}},
                    {{""name"": ""Rango inicial"",  ""value"": ""{EscapeJson(rankName)}"",     ""inline"": true}}
                ],
                ""footer"": {{""text"": ""BoinaCoin · La Chica de la Boina""}},
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
