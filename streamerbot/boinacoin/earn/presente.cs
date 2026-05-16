// ============================================================
//  BOINACOIN · earn/presente.cs
//  Comando: !presente
//  Recompensa: +50 Boinacoins · 1 vez por stream
//
//  También gestiona:
//    · boinacoin_streak    → racha de streams consecutivos
//    · boinacoin_daily_claimed → guard de una vez por stream
//
//  Lógica de racha:
//    Si el usuario no hizo !presente en los últimos 3 días →
//    racha se reinicia a 1 (tolerancia para streams no diarios).
//    Si sí lo hizo (mismo día) → ya cobrado, mensaje informativo.
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !presente"
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const long REWARD_PRESENTE = 50;

    // Días máximos entre streams para no romper la racha.
    // Ajusta según la frecuencia de emisión del canal.
    private const int STREAK_GAP_TOLERANCE_DAYS = 3;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string userName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // ── 1. Guard: solo una vez por stream (día) ──────────
        string lastPresenteDate = CPH.GetKickUserVar<string>(userId, "boinacoin_daily_claimed") ?? "";

        if (lastPresenteDate == todayDate)
        {
            long balance = CPH.GetKickUserVar<long>(userId, "boinacoin");
            int  streak  = CPH.GetKickUserVar<int>(userId, "boinacoin_streak");
            CPH.SendMessage(
                $"⏳ {userName}, ya hiciste !presente hoy. " +
                $"Saldo: {balance} 🪙 · Racha: {streak} streams 🔥");
            return true;
        }

        // ── 2. Actualizar racha de asistencia ────────────────
        int newStreak = CalculateStreak(userId, todayDate);
        CPH.SetKickUserVar(userId, "boinacoin_streak",       newStreak, true);
        CPH.SetKickUserVar(userId, "boinacoin_streak_date",  todayDate, true);
        CPH.SetKickUserVar(userId, "boinacoin_daily_claimed", todayDate, true);

        // ── 3. Calcular recompensa ───────────────────────────
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(REWARD_PRESENTE * mult);

        // ── 4. Actualizar saldo ──────────────────────────────
        long balance2 = CPH.GetKickUserVar<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVar(userId, "boinacoin", balance2, true);

        // ── 5. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVar<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVar(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 6. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVar(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 7. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance2);

        // ── 8. Mensaje al chat ───────────────────────────────
        string multText    = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        string streakText  = BuildStreakText(newStreak);
        CPH.SendMessage(
            $"✅ ¡Presente, {userName}! " +
            $"+{earned} Boinacoins{multText} · " +
            $"Saldo: {balance2} 🪙 · {streakText}");

        // ── 9. Hito de racha: anuncio especial ───────────────
        AnnounceStreakMilestone(userName, newStreak);

        return true;
    }

    // ── Calcula la nueva racha ────────────────────────────────
    private int CalculateStreak(string userId, string todayDate)
    {
        string lastDateStr = CPH.GetKickUserVar<string>(userId, "boinacoin_streak_date") ?? "";
        int    currentStreak = CPH.GetKickUserVar<int>(userId, "boinacoin_streak");

        if (string.IsNullOrEmpty(lastDateStr)) return 1;

        if (!DateTime.TryParse(lastDateStr, out DateTime lastDate)) return 1;

        DateTime today = DateTime.Parse(todayDate);
        int dayGap     = (int)(today - lastDate).TotalDays;

        if      (dayGap == 0)                              return currentStreak; // mismo día (no debería llegar aquí)
        else if (dayGap <= STREAK_GAP_TOLERANCE_DAYS)      return currentStreak + 1;
        else                                               return 1; // racha rota
    }

    // ── Texto de racha para el mensaje ───────────────────────
    private string BuildStreakText(int streak)
    {
        if (streak >= 30) return $"Racha: {streak} streams 🔥🔥🔥 (x2.0 activo)";
        if (streak >= 7)  return $"Racha: {streak} streams 🔥🔥 (x1.5 activo)";
        if (streak >= 3)  return $"Racha: {streak} streams 🔥";
        return $"Racha: {streak} stream";
    }

    // ── Anuncios en hitos de racha (1 mensaje por hito) ──────
    private void AnnounceStreakMilestone(string userName, int streak)
    {
        switch (streak)
        {
            case 7:
                CPH.SendMessage($"🔥 ¡{userName} lleva 7 streams seguidos! Multiplicador x1.5 desbloqueado.");
                break;
            case 30:
                CPH.SendMessage($"🔥🔥 ¡¡{userName} lleva 30 streams seguidos!! Multiplicador x2.0 desbloqueado.");
                break;
            case 50:
                CPH.SendMessage($"👑 ¡¡¡{userName} lleva 50 streams seguidos!!! Leyenda del canal.");
                break;
        }
    }

    // ── Multiplicador total activo ────────────────────────────
    private double GetMultiplier(string userId)
    {
        double m = 1.0;

        double subMult = CPH.GetKickUserVar<double>(userId, "boinacoin_multiplier");
        if (subMult > 1.0) m *= subMult;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.GetKickUserVar<int>(userId, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m;
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVar(userId, "boinacoin_rank", newRank, true);
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
