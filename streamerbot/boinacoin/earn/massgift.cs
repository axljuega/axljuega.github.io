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
        string gifterId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string gifterName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(gifterId)) return false;

        // ── 0. Excluir Bots ───────────────────────────────────
        if (CPH.UserInGroup(gifterName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio bot y al streamer ───────────
        // FIX: .UserId en lugar de .Id (KickUserInfo v1.x)
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && gifterId == botInfo.UserId.ToString()) return false;

        var broadcasterInfo = CPH.KickGetBroadcaster();
        if (broadcasterInfo != null && gifterId == broadcasterInfo.UserId.ToString()) return false;

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
        long balance = CPH.GetKickUserVarById<long>(gifterId, "boinacoin") + earned;
        CPH.SetKickUserVarById(gifterId, "boinacoin", balance, true);

        // ── 3. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(gifterId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(gifterId, "boinacoin_total_earned", totalEarned, true);

        // ── 4. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(gifterId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 5. Comprobar subida de rango ─────────────────────
        CheckRankUp(gifterId, gifterName, balance);

        // ── 6. Mensaje al chat ───────────────────────────────
        string subWord  = quantity == 1 ? "sub" : "subs";
        string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
        CPH.SendKickMessage(
            $"🎁🎁 ¡¡{gifterName} acaba de regalar {quantity} {subWord} al canal!! " +
            $"+{earned} Boinacoins{multText} · Saldo: {balance} 🪙 · " +
            $"¡¡GRACIAS!!");

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
