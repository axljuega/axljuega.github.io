// ============================================================
//  BOINACOIN · earn/follow.cs
//  Evento: nuevo Follow en Kick
//  Recompensa: +250 Boinacoins (antes de multiplicadores)
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Follow"
//    Ejecutar este inline C# action
// ============================================================

using System;

public class CPHInline
{
    // ── Constantes de rango ──────────────────────────────────
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // Datos del evento
        string userId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string userName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 1. Calcular recompensa con multiplicadores ───────
        const long BASE = 250;
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(BASE * mult);

        // ── 2. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetKickUserVar<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVar(userId, "boinacoin", balance, true);

        // ── 3. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVar<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVar(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 4. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVar(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 5. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 6. Mensaje de bienvenida ─────────────────────────
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendMessage(
            $"🎩 ¡Bienvenid@ {userName}! +{earned} Boinacoins por el follow{multText} · " +
            $"Saldo total: {balance} 🪙");

        return true;
    }

    // ── Calcula el multiplicador total activo para un usuario ─
    private double GetMultiplier(string userId)
    {
        double m = 1.0;

        // Multiplicador de sub tier (guardado en boinacoin_multiplier por sub.cs / resub.cs)
        double subMult = CPH.GetKickUserVar<double>(userId, "boinacoin_multiplier");
        if (subMult > 1.0) m *= subMult;

        // Hora feliz global (activada por cmd_horafeliz.cs)
        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        // Racha de asistencia (boinacoin_streak actualizado por presente.cs)
        int streak = CPH.GetKickUserVar<int>(userId, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        // Bonus de rango alto
        int rank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;   // Legendaria
        else if (rank == 3) m *= 1.25;  // Terciopelo

        return m;
    }

    // ── Comprueba si el usuario sube de rango ────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVar(userId, "boinacoin_rank", newRank, true);
        CPH.SendMessage($"🎉 ¡{userName} sube a {RankName(newRank)}!");

        // Almacena el nuevo rango como argumento para que rank_checker.cs
        // (acción encadenada) dispare el webhook de Discord si corresponde.
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
