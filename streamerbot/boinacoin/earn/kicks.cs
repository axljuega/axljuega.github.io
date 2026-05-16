// ============================================================
//  BOINACOIN · earn/kicks.cs
//  Evento: Kicks Gifted en Kick (equivalente a Bits/Cheers)
//  Recompensa: +1 Boinacoin por cada Kick enviado
//
//  Ejemplo: 500 Kicks enviados → +500 Boinacoins (base)
//           Con hora feliz x2  → +1.000 Boinacoins
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Gifts Leaderboard Updated"
//    o el trigger equivalente de Kicks en tu versión de SB.
//    El evento pasa "amount" con el nº de Kicks enviados.
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    // Umbral mínimo de Kicks para procesar el evento.
    // Evita ruido de eventos de 1-2 Kicks accidentales.
    private const int MIN_KICKS = 1;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        // Nº de Kicks enviados en este evento
        int kicksAmount = 0;
        if (args.ContainsKey("amount"))
            int.TryParse(args["amount"].ToString(), out kicksAmount);

        if (string.IsNullOrEmpty(userId)) return false;
        if (kicksAmount < MIN_KICKS)      return false;

        // ── 1. Calcular recompensa (+1 por Kick) ─────────────
        long   baseReward = kicksAmount;           // 1:1
        double mult       = GetMultiplier(userId);
        long   earned     = (long)Math.Floor(baseReward * mult);

        // ── 2. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        // ── 3. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 4. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), true);

        // ── 5. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 6. Mensaje al chat ───────────────────────────────
        // Solo si los Kicks son suficientes para merecer mención
        // (evita spam en chat con envíos de 1-2 Kicks)
        if (kicksAmount >= 50)
        {
            string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
            CPH.SendKickMessage(
                $"💥 ¡{userName} ha enviado {kicksAmount} Kicks! " +
                $"+{earned} Boinacoins{multText} · Saldo: {balance} 🪙");
        }

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
