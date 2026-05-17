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
    private const int STREAK_GAP_TOLERANCE_DAYS = 3;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        CPH.LogInfo($"[Boinacoin Debug] Iniciando !presente para {userName} ({userId})");

        if (string.IsNullOrEmpty(userId))
        {
            CPH.LogInfo("[Boinacoin Debug] Saliendo por verificación de ID nulo.");
            return false;
        }

        // ── 0. Excluir Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio bot (el streamer ahora puede jugar)
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && userId == botInfo.UserId.ToString())
        {
            CPH.LogInfo("[Boinacoin Debug] Saliendo: El bot no puede usar !presente.");
            return false;
        }

        // Bypass de cooldown para afaces y LaChicaDeLaBoina (Testing/Admin)
        string lowerUser = userName.ToLower().Replace("@", "");
        if (lowerUser == "afaces" || lowerUser == "lachicadelaboina")
        {
            CPH.LogInfo($"[Boinacoin Debug] {userName} detectado. Forzando limpieza de cooldown diario.");
            CPH.SetKickUserVarById(userId, "boinacoin_daily_claimed", "", true);
        }

        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // ── 1. Guard: solo una vez por stream (día) ──────────
        string lastPresenteDate = CPH.GetKickUserVarById<string>(userId, "boinacoin_daily_claimed") ?? "";

        if (lastPresenteDate == todayDate)
        {
            long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");
            int  streak  = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak");
            CPH.SendKickMessage(
                $"⏳ {userName}, ya hiciste !presente hoy. " +
                $"Saldo: {balance} 🪙 · Racha: {streak} streams 🔥");

            CPH.LogInfo("[Boinacoin Debug] Saliendo por cooldown activo (ya reclamado hoy).");
            return true;
        }

        // ── 2. Actualizar racha de asistencia ────────────────
        int newStreak = CalculateStreak(userId, todayDate);
        CPH.SetKickUserVarById(userId, "boinacoin_streak",        newStreak, true);
        CPH.SetKickUserVarById(userId, "boinacoin_streak_date",   todayDate, true);
        CPH.SetKickUserVarById(userId, "boinacoin_daily_claimed", todayDate, true);

        // ── 3. Calcular recompensa ───────────────────────────
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(REWARD_PRESENTE * mult);

        // ── 4. Actualizar saldo ──────────────────────────────
        long balance2 = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance2, true);

        // ── 5. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 6. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 7. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance2);

        // ── 8. Mensaje al chat (Persona Inmersiva) ───────────
        string rollCallMessage = GetRandomRollCallMessage(userName);
        string multText   = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        string streakText = BuildStreakText(newStreak);
        CPH.SendKickMessage(
            $"{rollCallMessage} " +
            $"+{earned} Boinacoins{multText} · " +
            $"Saldo: {balance2} 🪙 · {streakText}");

        // ── 9. Hito de racha: anuncio especial ───────────────
        AnnounceStreakMilestone(userName, newStreak);

        CPH.LogInfo("[Boinacoin Debug] !presente ejecutado con éxito.");
        return true;
    }

    // ── Mensajes aleatorios de pasar lista ────────────────────
    private string GetRandomRollCallMessage(string userName)
    {
        string[] messages = new string[]
        {
            $"¡Presente! *bostezo*... Apuntado en la lista, @{userName}. No te acostumbres.",
            $"A ver... @{userName}... Sí, ya veo tu boina por aquí. Avanzamos.",
            $"¡Presente! Otro día más pasando lista... Aquí tienes tus monedas, @{userName}.",
            $"Te puse el positivo, @{userName}. Deja de gritar en clase.",
            $"@{userName}, llegas justo a tiempo. No hagas ruido al sentarte.",
            $"¿@{userName}? Sí, presente. Sigamos con la lección.",
            $"Anotado, @{userName}. A ver si mañana vienes con la boina más limpia.",
            $"¡@{userName}! Te he visto entrar por los pelos. Presente.",
            $"Presente... @{userName}, deja de pasar papelitos a los demás.",
            $"¡Presente! @{userName}, siéntate de una vez, que distraes al personal."
        };
        Random rnd = new Random();
        return messages[rnd.Next(messages.Length)];
    }

    // ── Calcula la nueva racha ────────────────────────────────
    private int CalculateStreak(string userId, string todayDate)
    {
        string lastDateStr   = CPH.GetKickUserVarById<string>(userId, "boinacoin_streak_date") ?? "";
        int    currentStreak = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak");

        if (string.IsNullOrEmpty(lastDateStr)) return 1;
        if (!DateTime.TryParse(lastDateStr, out DateTime lastDate)) return 1;

        DateTime today  = DateTime.Parse(todayDate);
        int      dayGap = (int)(today - lastDate).TotalDays;

        if      (dayGap == 0)                         return currentStreak;
        else if (dayGap <= STREAK_GAP_TOLERANCE_DAYS) return currentStreak + 1;
        else                                          return 1;
    }

    // ── Texto de racha para el mensaje ───────────────────────
    private string BuildStreakText(int streak)
    {
        if (streak >= 30) return $"Racha: {streak} streams 🔥🔥🔥 (x2.0 activo)";
        if (streak >= 7)  return $"Racha: {streak} streams 🔥🔥 (x1.5 activo)";
        if (streak >= 3)  return $"Racha: {streak} streams 🔥";
        return $"Racha: {streak} stream";
    }

    // ── Anuncios en hitos de racha ────────────────────────────
    private void AnnounceStreakMilestone(string userName, int streak)
    {
        switch (streak)
        {
            case 7:
                CPH.SendKickMessage($"🔥 ¡{userName} lleva 7 streams seguidos! Multiplicador x1.5 desbloqueado.");
                break;
            case 30:
                CPH.SendKickMessage($"🔥🔥 ¡¡{userName} lleva 30 streams seguidos!! Multiplicador x2.0 desbloqueado.");
                break;
            case 50:
                CPH.SendKickMessage($"👑 ¡¡¡{userName} lleva 50 streams seguidos!!! Leyenda del canal.");
                break;
        }
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

    // ── Subida de rango ───────────────────────────────────────
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
