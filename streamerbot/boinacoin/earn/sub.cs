// ============================================================
//  BOINACOIN · earn/sub.cs
//  Evento: nueva Subscription en Kick (primera vez)
//  Recompensa: +5.000 Boinacoins · activa multiplicador x1.5
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Subscribe"
//    (las resubs van a resub.cs — trigger separado)
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    // Multiplicador que otorga una sub activa (sin meses acumulados aún)
    private const double SUB_MULTIPLIER = 1.5;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 1. Actualizar multiplicador de sub ───────────────
        // Lo guardamos ANTES de calcular la recompensa para que
        // el propio sub ya se beneficie de su nuevo multiplicador.
        CPH.SetKickUserVarById(userId, "boinacoin_multiplier", SUB_MULTIPLIER, true);

        // ── 2. Calcular recompensa ───────────────────────────
        const long BASE = 5_000;
        double mult   = GetMultiplier(userId);
        long   earned = (long)Math.Floor(BASE * mult);

        // ── 3. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        // ── 4. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 5. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 6. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 7. Mensaje de agradecimiento ─────────────────────
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendKickMessage(
            $"🎉 ¡Gracias por suscribirte, {userName}! " +
            $"+{earned} Boinacoins{multText} · Saldo: {balance} 🪙 · " +
            $"Multiplicador permanente activado: x{SUB_MULTIPLIER} 💜");

        return true;
    }

    // ── Multiplicador total activo ────────────────────────────
    // NOTA: ya incluye SUB_MULTIPLIER porque se guardó en el paso 1.
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
