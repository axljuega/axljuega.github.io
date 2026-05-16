// ============================================================
//  BOINACOIN · earn/massgift.cs
//  Evento: Mass Gift Subscription en Kick
//  Recompensa: +5.000 Boinacoins al GIFTER (fija, independiente
//              del número de subs regaladas)
//
//  Importante:
//    Cada receptor individual recibe su sub vía sub.cs
//    (Kick dispara un Subscribe por cada regalo del mass gift).
//    Este script premia SOLO al gifter por el gesto masivo.
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Gift Subscriptions" (plural)
//    El evento incluye "quantity" con el nº de subs regaladas
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
        string gifterId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string gifterName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        // Cantidad de subs del mass gift (informativo para el mensaje)
        int quantity = 1;
        if (args.ContainsKey("quantity"))
            int.TryParse(args["quantity"].ToString(), out quantity);

        if (string.IsNullOrEmpty(gifterId)) return false;

        // ── 1. Calcular recompensa ───────────────────────────
        // La recompensa es fija en +5.000 independientemente
        // de cuántas subs se regalen — el coste en dinero real
        // ya es el incentivo; aquí premiamos el gesto, no la escala.
        const long BASE = 5_000;
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
        string subWord  = quantity == 1 ? "sub" : "subs";
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendMessage(
            $"🎁🎁 ¡¡{gifterName} acaba de regalar {quantity} {subWord} al canal!! " +
            $"+{earned} Boinacoins{multText} · Saldo: {balance} 🪙 · " +
            $"¡¡GRACIAS!!");

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
