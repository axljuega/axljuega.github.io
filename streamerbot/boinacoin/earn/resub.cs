// ============================================================
//  BOINACOIN · earn/resub.cs
//  Evento: Resubscription en Kick
//
//  Tramos de recompensa base:
//    < 6 meses  → +5.000  · multiplicador x1.5
//    ≥ 6 meses  → +7.500  · multiplicador x2.0
//    ≥ 12 meses → +10.000 · multiplicador x2.5
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Re-Subscribe"
//    El evento pasa "months" en args (meses acumulados totales)
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string userName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        // Kick envía los meses acumulados totales del suscriptor
        int months = 1;
        if (args.ContainsKey("months"))
            int.TryParse(args["months"].ToString(), out months);

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 1. Determinar tramo y actualizar multiplicador ───
        long   baseReward;
        double subMultiplier;
        string tramo;

        if (months >= 12)
        {
            baseReward     = 10_000;
            subMultiplier  = 2.5;
            tramo          = $"¡{months} meses! 👑";
        }
        else if (months >= 6)
        {
            baseReward     = 7_500;
            subMultiplier  = 2.0;
            tramo          = $"¡{months} meses! 💎";
        }
        else
        {
            baseReward     = 5_000;
            subMultiplier  = 1.5;
            tramo          = $"{months} {(months == 1 ? "mes" : "meses")}";
        }

        // El multiplicador de sub se actualiza ANTES de calcular
        // la recompensa, igual que en sub.cs
        CPH.KickSetUserVar(userId, "boinacoin_multiplier", subMultiplier, true);

        // ── 2. Bonus de racha de resub ───────────────────────
        // Racha = meses consecutivos sin perder la sub
        // Se guarda en boinacoin_streak_sub (distinto de boinacoin_streak
        // que mide asistencia a streams)
        int resubStreak = CPH.KickGetUserVar<int>(userId, "boinacoin_streak_sub", true);
        resubStreak++;
        CPH.KickSetUserVar(userId, "boinacoin_streak_sub", resubStreak, true);

        long streakBonus = CalculateStreakBonus(resubStreak);

        // ── 3. Recompensa total con multiplicadores ──────────
        double mult         = GetMultiplier(userId);
        long   earnedBase   = (long)Math.Floor(baseReward * mult);
        long   earnedStreak = (long)Math.Floor(streakBonus * mult);
        long   earned       = earnedBase + earnedStreak;

        // ── 4. Actualizar saldo ──────────────────────────────
        long balance = CPH.KickGetUserVar<long>(userId, "boinacoin", true) + earned;
        CPH.KickSetUserVar(userId, "boinacoin", balance, true);

        // ── 5. Estadística histórica ─────────────────────────
        long totalEarned = CPH.KickGetUserVar<long>(userId, "boinacoin_total_earned", true) + earned;
        CPH.KickSetUserVar(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 6. Timestamp antiinactividad ─────────────────────
        CPH.KickSetUserVar(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 7. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 8. Mensaje al chat ───────────────────────────────
        string multText    = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        string streakText  = streakBonus > 0
            ? $" + {earnedStreak} bonus racha ({resubStreak} meses seguidos 🔥)"
            : "";
        string multiplierChange = $"Multiplicador → x{subMultiplier}";

        CPH.SendMessage(
            $"💜 ¡Gracias por renovar, {userName}! {tramo} · " +
            $"+{earnedBase} Boinacoins{multText}{streakText} · " +
            $"Saldo: {balance} 🪙 · {multiplierChange}");

        return true;
    }

    // ── Bonus de racha de resub (meses consecutivos) ─────────
    // Escala suave para no romper la economía
    private long CalculateStreakBonus(int streak)
    {
        if (streak >= 24) return 3_000;  // 2 años+
        if (streak >= 12) return 2_000;  // 1 año+
        if (streak >= 6)  return 1_000;  // 6 meses+
        if (streak >= 3)  return 500;    // 3 meses+
        return 0;
    }

    // ── Multiplicador total activo ────────────────────────────
    private double GetMultiplier(string userId)
    {
        double m = 1.0;

        double subMult = CPH.KickGetUserVar<double>(userId, "boinacoin_multiplier", true);
        if (subMult > 1.0) m *= subMult;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.KickGetUserVar<int>(userId, "boinacoin_streak", true);
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.KickGetUserVar<int>(userId, "boinacoin_rank", true);
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m;
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.KickGetUserVar<int>(userId, "boinacoin_rank", true);
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.KickSetUserVar(userId, "boinacoin_rank", newRank, true);
        CPH.SendMessage($"🎉 ¡{userName} sube a {RankName(newRank)}!");

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
