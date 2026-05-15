// ============================================================
//  BOINACOIN · earn/timed_payout.cs
//  Tipo: acción temporizadora (Timer Action)
//  Intervalo: cada 600 segundos (10 minutos)
//  Recompensa: +15 por usuario activo en chat
//
//  Condición antiabuso:
//    Solo recibe el pago quien haya escrito en el chat en los
//    últimos 20 minutos (boinacoin_chat_active, escrito por
//    chat_message.cs). Así se evita dejar el stream abierto
//    en segundo plano sin interactuar.
//
//  Cómo configurarlo en Streamer.bot:
//    1. Actions → New Action → "Boinacoin · Timed Payout"
//    2. Add sub-action → Execute C# Code → este script
//    3. Settings → Timer → cada 600 s · solo si stream activo
// ============================================================

using System;
using System.Collections.Generic;

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

        foreach (var viewer in viewers)
        {
            // Streamer.bot expone userName y userId en el objeto viewer
            string userId   = viewer.UserId?.ToString()   ?? "";
            string userName = viewer.UserName?.ToString() ?? "";

            if (string.IsNullOrEmpty(userId)) continue;

            // ── Condición de actividad ───────────────────────
            // Solo cobra quien haya chateado en los últimos 20 min.
            long lastActive = CPH.GetUserVar<long>(userId, "boinacoin_chat_active", true);
            bool isActive   = (nowUnix - lastActive) <= ACTIVITY_WINDOW_SECS;

            if (!isActive) continue;

            // ── Calcular recompensa ──────────────────────────
            double mult   = GetMultiplier(userId);
            long   earned = (long)Math.Floor(REWARD_PASSIVE * mult);

            // ── Actualizar saldo ─────────────────────────────
            long balance = CPH.GetUserVar<long>(userId, "boinacoin", true) + earned;
            CPH.SetUserVar(userId, "boinacoin", balance, true);

            // ── Estadística histórica ────────────────────────
            long totalEarned = CPH.GetUserVar<long>(userId, "boinacoin_total_earned", true) + earned;
            CPH.SetUserVar(userId, "boinacoin_total_earned", totalEarned, true);

            // ── Timestamp antiinactividad ────────────────────
            CPH.SetUserVar(userId, "boinacoin_last_seen", nowUnix, true);

            // ── Comprobar subida de rango ────────────────────
            // Sin mensaje de rango aquí para no spamear el chat
            // cada 10 min. El mensaje se emite igualmente desde
            // CheckRankUp porque llama a RankChecker.
            CheckRankUp(userId, userName, balance);

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

        double subMult = CPH.GetUserVar<double>(userId, "boinacoin_multiplier", true);
        if (subMult > 1.0) m *= subMult;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.GetUserVar<int>(userId, "boinacoin_streak", true);
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.GetUserVar<int>(userId, "boinacoin_rank", true);
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m;
    }

    // ── Subida de rango (sin mensaje inline, solo RankChecker) ─
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetUserVar<int>(userId, "boinacoin_rank", true);
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetUserVar(userId, "boinacoin_rank", newRank, true);

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
