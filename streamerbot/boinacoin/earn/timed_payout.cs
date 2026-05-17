// ============================================================
//  BOINACOIN · earn/timed_payout.cs
//  Tipo: acción temporizada (cada 10 min)
//  Recompensa: +15 Boinacoins (base) a TODOS los espectadores
//              que hayan chateado en los últimos 20 min.
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Timer" (cada 600 segundos)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const long REWARD_PASSIVE       = 15;
    private const int  ACTIVITY_WINDOW_SECS = 1_200; // 20 minutos

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // Obtener lista de espectadores presentes en el canal.
        // CPH.GetPresentViewers() devuelve los viewers que Streamer.bot
        // tiene registrados como activos en el stream actual.
        var viewers = CPH.GetPresentViewers();

        if (viewers == null || viewers.Count == 0) return true;

        long nowUnix       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int  rewardedCount = 0;

        var botInfo = CPH.KickGetBot();
        string botId = botInfo?.UserId.ToString() ?? "";

        var broadcasterInfo = CPH.KickGetBroadcaster();
        string broadcasterId = broadcasterInfo?.UserId.ToString() ?? "";

        foreach (var viewer in viewers)
        {
            // Streamer.bot expone userName y userId en el objeto viewer
            string userId   = viewer.UserId?.ToString()   ?? "";
            string userName = viewer.UserName?.ToString() ?? "";

            if (string.IsNullOrEmpty(userId)) continue;

            // ── Excluir Bots y al propio bot y al streamer ────
            if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) continue;
            if (userId == botId || userId == broadcasterId) continue;

            // ── Condición de actividad ───────────────────────
            // Solo cobra quien haya chateado en los últimos 20 min.
            long lastActive = CPH.GetKickUserVarById<long>(userId, "boinacoin_chat_active");
            bool isActive   = (nowUnix - lastActive) <= ACTIVITY_WINDOW_SECS;

            if (!isActive) continue;

            // ── Calcular recompensa ──────────────────────────
            double mult   = GetMultiplier(userId);
            long   earned = (long)Math.Floor(REWARD_PASSIVE * mult);

            // ── Actualizar saldo ─────────────────────────────
            long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
            CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

            // ── Estadística histórica ────────────────────────
            long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
            CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

            // ── Timestamp antiinactividad ────────────────────
            CPH.SetKickUserVarById(userId, "boinacoin_last_seen", nowUnix, true);

            // ── Comprobar subida de rango ────────────────────
            // Sin mensaje de rango aquí para no spamear el chat
            // cada 10 min. El mensaje se emite igualmente desde
            // CheckRankUp ya que llama a RankChecker.
            CheckRankUp(userId, userName, balance);

            // ── Tracking de sesión ───────────────────────────
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + earned;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

            string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
            var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
            lb[userName] = lb.ContainsKey(userName) ? lb[userName] + earned : earned;
            var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
            CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);

            rewardedCount++;
        }

        // ── Resumen silencioso en log (no al chat) ───────────
        // Útil para depurar desde la consola de Streamer.bot.
        CPH.LogInfo($"[Boinacoin] Timed payout: +{REWARD_PASSIVE} base a {rewardedCount} viewers activos.");

        return true;
    }

    // ── Multiplicador total activo ────────────────────────────
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

    // ── Subida de rango (sin mensaje inline, solo RankChecker) ─
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);

        CPH.SetArgument("rankUpUserId",   userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("Boinacoin · RankChecker", false);
    }

    private int RankForBalance(long balance)
    {
        if (balance >= RANK_LEGENDARIA) return 4;
        if (balance >= RANK_TERCIOPELO) return 3;
        if (balance >= RANK_CUERO)      return 2;
        if (balance >= RANK_LANA)       return 1;
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
