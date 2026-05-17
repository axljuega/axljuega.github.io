// ============================================================
//  BOINACOIN · earn/kicks.cs
//  Evento: Kicks Gifted en Kick (equivalente a Bits/Cheers)
//  Recompensa: +1 Boinacoin por cada Kick enviado
//
//  Ejemplo: 500 Kicks enviados → +500 Boinacoins (base)
//           Con hora feliz x2  → +1.000 Boinacoins
//
//  FIX: Eliminado bloque de exclusión del broadcaster.
//  FIX: Añadido dump de args para encontrar el key correcto
//       del campo "amount" en el evento kicks.gifted.
//       Una vez confirmado el key, eliminar el bloque DEBUG.
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const int MIN_KICKS = 1;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Excluir grupo Chat Bots ────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.1 Excluir al propio BoinaBot ───────────────────
        var botInfo = CPH.KickGetBot();
        if (botInfo != null && userId == botInfo.UserId.ToString()) return false;

        // ── NOTA: El broadcaster (afaces) SÍ gana Boinacoins ─

        // ── DEBUG: Dump de todos los args del evento ─────────
        // Necesario para encontrar el key correcto de "amount"
        // en el evento kicks.gifted. Eliminar tras confirmarlo.
        CPH.LogInfo("[Kicks DEBUG] === ARG DUMP ===");
        foreach (var k in args.Keys)
            CPH.LogInfo($"[Kicks DEBUG] {k} = {args[k]}");
        CPH.LogInfo("[Kicks DEBUG] === FIN DUMP ===");

        // ── 1. Leer cantidad de Kicks ────────────────────────
        // CANDIDATOS conocidos — se prueba por orden:
        int kicksAmount = 0;
        string[] candidateKeys = { "amount", "kicksAmount", "giftAmount", "kicks", "giftsAmount" };
        foreach (var key in candidateKeys)
        {
            if (args.ContainsKey(key))
            {
                int.TryParse(args[key].ToString(), out kicksAmount);
                CPH.LogInfo($"[Kicks DEBUG] Key encontrado: '{key}' = {kicksAmount}");
                break;
            }
        }

        if (kicksAmount < MIN_KICKS)
        {
            CPH.LogInfo($"[Kicks DEBUG] kicksAmount={kicksAmount} < MIN_KICKS={MIN_KICKS} → abort");
            return false;
        }

        // ── 2. Calcular recompensa (+1 por Kick) ─────────────
        long   baseReward = kicksAmount;
        double mult       = GetMultiplier(userId);
        long   earned     = (long)Math.Floor(baseReward * mult);

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

        // ── 7. Mensaje al chat ───────────────────────────────
        if (kicksAmount >= 50)
        {
            string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
            CPH.SendKickMessage(
                $"💥 {userName} acaba de tirar {kicksAmount} Kicks. " +
                $"Alguien tiene el carrete suelto. " +
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
