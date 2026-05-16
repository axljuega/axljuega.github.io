// ============================================================
//  BOINACOIN · earn/giftsub.cs
//  Evento: Gift Subscription individual en Kick
//  Recompensa: +2.500 Boinacoins al GIFTER
//
//  Importante:
//    El receptor de la gift recibe su sub normal vía sub.cs
//    (Kick dispara un evento Subscribe separado para él).
//    Este script premia SOLO al que regala.
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Gift Subscription"
//    (no confundir con "Kick · Mass Gift" → massgift.cs)
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
        // Datos del gifter (quien regala)
        string gifterId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string gifterName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        // Datos del receptor (informativo para el mensaje)
        string recipientName = args.ContainsKey("recipientUsername")
            ? args["recipientUsername"].ToString()
            : "alguien";

        if (string.IsNullOrEmpty(gifterId)) return false;

        // ── 1. Calcular recompensa ───────────────────────────
        const long BASE = 2_500;
        double mult   = GetMultiplier(gifterId);
        long   earned = (long)Math.Floor(BASE * mult);

        // ── 2. Actualizar saldo del gifter ───────────────────
        long balance = CPH.KickGetUserVar<long>(gifterId, "boinacoin", true) + earned;
        CPH.KickSetUserVar(gifterId, "boinacoin", balance, true);

        // ── 3. Estadística histórica ─────────────────────────
        long totalEarned = CPH.KickGetUserVar<long>(gifterId, "boinacoin_total_earned", true) + earned;
        CPH.KickSetUserVar(gifterId, "boinacoin_total_earned", totalEarned, true);

        // ── 4. Timestamp antiinactividad ─────────────────────
        CPH.KickSetUserVar(gifterId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 5. Comprobar subida de rango ─────────────────────
        CheckRankUp(gifterId, gifterName, balance);

        // ── 6. Mensaje al chat ───────────────────────────────
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendMessage(
            $"🎁 ¡{gifterName} ha regalado una sub a {recipientName}! " +
            $"+{earned} Boinacoins{multText} · Saldo: {balance} 🪙");

        return true;
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
