// ============================================================
//  BOINACOIN · commands/cmd_duelo.cs
//  Comandos: !duelo @usuario cantidad  /  !aceptar
//  Permiso: Boina de Lana+ (rank >= 1)
//
//  Mecánica:
//    1. Retador escribe !duelo @rival cantidad
//    2. Bot anuncia el duelo y da 60s al rival para !aceptar
//    3. Si acepta → bot elige ganador al azar (50/50)
//       El perdedor transfiere la cantidad al ganador.
//    4. Si no acepta en 60s → duelo caduca automáticamente
//       (la caducidad se comprueba en el !aceptar)
//
//  Configuración en Streamer.bot — DOS acciones, mismo código:
//    Acción A → trigger "!duelo"   → Set Argument "mode" = "challenge"
//    Acción B → trigger "!aceptar" → Set Argument "mode" = "accept"
//    Ambas ejecutan este mismo C# inline.
//
//  Estado del duelo (variables globales persistidas):
//    boinacoin_duel_challengerId   → userId del retador
//    boinacoin_duel_challengerName → nombre del retador
//    boinacoin_duel_targetId       → userId del retado
//    boinacoin_duel_targetName     → nombre del retado
//    boinacoin_duel_amount         → cantidad en juego
//    boinacoin_duel_expiry         → unix timestamp de caducidad
//
//  Nota sobre BoinaBot:
//    BoinaBot actúa como "la casa". Se le permite ser target
//    aunque tenga rango 0 y no lleve saldo real en BD.
// ============================================================

using System;

public class CPHInline
{
    private const int  DUEL_TIMEOUT_SECS = 60;
    private const long MIN_BET           = 10;

    // Nombre en minúsculas del bot — ajusta si cambia
    private const string BOT_NAME_LOWER = "boinabot";

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string mode = args.ContainsKey("mode") ? args["mode"].ToString() : "challenge";

        return mode == "accept" ? HandleAccept() : HandleChallenge();
    }

    // ════════════════════════════════════════════════════════
    //  RAMA A · !duelo @usuario cantidad
    // ════════════════════════════════════════════════════════
    private bool HandleChallenge()
    {
        string challengerId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string challengerName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(challengerId)) return false;

        // ── Verificar rango mínimo del retador ────────────────
        int rank = CPH.GetKickUserVarById<int>(challengerId, "boinacoin_rank");
        if (rank < 1)
        {
            CPH.SendKickMessage($"🔒 {challengerName}, necesitas 🧶 Boina de Lana para duelos.");
            return true;
        }

        // ── Parsear argumentos ────────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        string rawAmount = args.ContainsKey("input1") ? args["input1"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget) || string.IsNullOrEmpty(rawAmount))
        {
            CPH.SendKickMessage($"❌ {challengerName}, uso: !duelo @usuario cantidad");
            return true;
        }

        if (!long.TryParse(rawAmount, out long amount) || amount < MIN_BET)
        {
            CPH.SendKickMessage($"❌ {challengerName}, apuesta mínima de duelo: {MIN_BET} Boinacoins.");
            return true;
        }

        // ── Resolver rival ────────────────────────────────────
        string targetName       = rawTarget.TrimStart('@');
        bool   targetIsBoinaBot = targetName.ToLower() == BOT_NAME_LOWER;

        if (targetName.ToLower() == challengerName.ToLower())
        {
            CPH.SendKickMessage($"😅 {challengerName}, no puedes desafiarte a ti mismo.");
            return true;
        }

        // ── Verificar rango del rival ─────────────────────────
        // FIX: BoinaBot (la casa) está exento del check de rango.
        int targetRank = CPH.GetKickUserVar<int>(targetName, "boinacoin_rank");
        if (!targetIsBoinaBot && targetRank < 1)
        {
            CPH.SendKickMessage($"❌ {challengerName}, {targetName} necesita 🧶 Boina de Lana para duelos.");
            return true;
        }

        // ── Verificar que no haya duelo activo ────────────────
        long existingExpiry = CPH.GetGlobalVar<long>("boinacoin_duel_expiry", true);
        long nowUnix        = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (existingExpiry > nowUnix)
        {
            string existingChallenger = CPH.GetGlobalVar<string>("boinacoin_duel_challengerName", true) ?? "";
            CPH.SendKickMessage($"⚔️ Ya hay un duelo activo ({existingChallenger}). Espera a que termine.");
            return true;
        }

        // ── Verificar saldo del retador ───────────────────────
        long challengerBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");
        if (challengerBalance < amount)
        {
            CPH.SendKickMessage(
                $"❌ {challengerName}, no tienes suficientes Boinacoins. " +
                $"Saldo: {challengerBalance} 🪙");
            return true;
        }

        // ── Verificar saldo del rival ─────────────────────────
        // FIX: BoinaBot (la casa) tiene saldo ilimitado implícito.
        long targetBalance = targetIsBoinaBot
            ? long.MaxValue
            : CPH.GetKickUserVar<long>(targetName, "boinacoin");

        if (!targetIsBoinaBot && targetBalance < amount)
        {
            CPH.SendKickMessage(
                $"❌ {targetName} no tiene suficientes Boinacoins para el duelo " +
                $"({targetBalance} 🪙 disponibles).");
            return true;
        }

        // ── Registrar duelo pendiente ─────────────────────────
        long expiry = nowUnix + DUEL_TIMEOUT_SECS;
        CPH.SetGlobalVar("boinacoin_duel_challengerId",   challengerId,   true);
        CPH.SetGlobalVar("boinacoin_duel_challengerName", challengerName, true);
        CPH.SetGlobalVar("boinacoin_duel_targetName",     targetName,     true);
        CPH.SetGlobalVar("boinacoin_duel_amount",         amount,         true);
        CPH.SetGlobalVar("boinacoin_duel_expiry",         expiry,         true);

        // ── Anuncio ───────────────────────────────────────────
        string targetLabel = targetIsBoinaBot ? "¡la CASA!" : $"@{targetName}";
        CPH.SendKickMessage(
            $"⚔️ ¡{challengerName} desafía a {targetLabel} a un duelo de {amount} Boinacoins! " +
            $"@{targetName}, escribe !aceptar en los próximos {DUEL_TIMEOUT_SECS}s. ¿Te atreves? 🎩");

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  RAMA B · !aceptar
    // ════════════════════════════════════════════════════════
    private bool HandleAccept()
    {
        string acceptorId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string acceptorName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(acceptorId)) return false;

        // ── ¿Hay duelo pendiente? ─────────────────────────────
        long expiry  = CPH.GetGlobalVar<long>("boinacoin_duel_expiry", true);
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (expiry == 0 || nowUnix > expiry)
        {
            CPH.SendKickMessage($"❌ {acceptorName}, no hay ningún duelo activo ahora mismo.");
            return true;
        }

        // ── ¿Es el retado quien acepta? ───────────────────────
        string targetName     = CPH.GetGlobalVar<string>("boinacoin_duel_targetName",     true) ?? "";
        string challengerId   = CPH.GetGlobalVar<string>("boinacoin_duel_challengerId",   true) ?? "";
        string challengerName = CPH.GetGlobalVar<string>("boinacoin_duel_challengerName", true) ?? "";
        long   amount         = CPH.GetGlobalVar<long>("boinacoin_duel_amount",           true);

        bool targetIsBoinaBot = targetName.ToLower() == BOT_NAME_LOWER;

        if (acceptorName.ToLower() != targetName.ToLower())
        {
            CPH.SendKickMessage($"❌ {acceptorName}, el duelo es entre {challengerName} y {targetName}.");
            return true;
        }

        // ── Verificar saldos actuales antes de resolver ───────
        string targetId = acceptorId;

        long challengerBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");

        // FIX: BoinaBot no tiene saldo real; se trata como saldo ilimitado.
        long targetBalance = targetIsBoinaBot
            ? long.MaxValue
            : CPH.GetKickUserVarById<long>(targetId, "boinacoin");

        if (challengerBalance < amount)
        {
            CPH.SendKickMessage($"❌ {challengerName} ya no tiene suficientes Boinacoins. Duelo cancelado.");
            ClearDuel();
            return true;
        }
        if (!targetIsBoinaBot && targetBalance < amount)
        {
            CPH.SendKickMessage($"❌ {targetName} ya no tiene suficientes Boinacoins. Duelo cancelado.");
            ClearDuel();
            return true;
        }

        // ── Resolver duelo (50/50) ────────────────────────────
        bool challengerWins = new Random().Next(0, 2) == 1;

        string winnerId, winnerName, loserId, loserName;
        long   winnerOldBalance, loserOldBalance;

        if (challengerWins)
        {
            winnerId = challengerId; winnerName = challengerName;
            loserId  = targetId;    loserName  = targetName;
            winnerOldBalance = challengerBalance;
            loserOldBalance  = targetBalance;
        }
        else
        {
            winnerId = targetId;    winnerName = targetName;
            loserId  = challengerId; loserName = challengerName;
            winnerOldBalance = targetBalance;
            loserOldBalance  = challengerBalance;
        }

        // ── Transferencia ─────────────────────────────────────
        // FIX: si BoinaBot gana no escribimos en BD (no lleva saldo real);
        //      si BoinaBot pierde tampoco descontamos de su BD.
        if (winnerName.ToLower() != BOT_NAME_LOWER)
            CPH.SetKickUserVarById(winnerId, "boinacoin", winnerOldBalance + amount, true);

        if (loserName.ToLower() != BOT_NAME_LOWER)
            CPH.SetKickUserVarById(loserId, "boinacoin", loserOldBalance - amount, true);

        // Histórico del ganador (solo si no es el bot)
        if (winnerName.ToLower() != BOT_NAME_LOWER)
        {
            long winnerTotal = CPH.GetKickUserVarById<long>(winnerId, "boinacoin_total_earned") + amount;
            CPH.SetKickUserVarById(winnerId, "boinacoin_total_earned", winnerTotal, true);
        }

        // Timestamps (solo usuarios reales)
        if (winnerName.ToLower() != BOT_NAME_LOWER)
            CPH.SetKickUserVarById(winnerId, "boinacoin_last_seen", nowUnix, true);
        if (loserName.ToLower() != BOT_NAME_LOWER)
            CPH.SetKickUserVarById(loserId, "boinacoin_last_seen", nowUnix, true);

        // ── Comprobar rango del ganador (solo usuarios reales) ─
        if (winnerName.ToLower() != BOT_NAME_LOWER)
            CheckRankUp(winnerId, winnerName, winnerOldBalance + amount);

        // ── Saldos para el mensaje (BoinaBot no tiene saldo real)
        string winnerBalanceText = winnerName.ToLower() == BOT_NAME_LOWER
            ? "∞"
            : (winnerOldBalance + amount).ToString();
        string loserBalanceText  = loserName.ToLower()  == BOT_NAME_LOWER
            ? "∞"
            : (loserOldBalance  - amount).ToString();

        // ── Anuncio del resultado ─────────────────────────────
        CPH.SendKickMessage(
            $"⚔️ ¡El bot ha lanzado los dados! " +
            $"🏆 GANA {winnerName} · +{amount} Boinacoins · " +
            $"Saldo: {winnerBalanceText} 🪙 · " +
            $"💀 {loserName} pierde {amount} · Saldo: {loserBalanceText} 🪙");

        // ── Limpiar estado del duelo ──────────────────────────
        ClearDuel();

        return true;
    }

    // ── Limpia todas las variables globales del duelo ─────────
    private void ClearDuel()
    {
        CPH.SetGlobalVar("boinacoin_duel_challengerId",   "",  true);
        CPH.SetGlobalVar("boinacoin_duel_challengerName", "",  true);
        CPH.SetGlobalVar("boinacoin_duel_targetId",       "",  true);
        CPH.SetGlobalVar("boinacoin_duel_targetName",     "",  true);
        CPH.SetGlobalVar("boinacoin_duel_amount",         0L,  true);
        CPH.SetGlobalVar("boinacoin_duel_expiry",         0L,  true);
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);
        CPH.SendKickMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

        CPH.SetArgument("rankUpUserId",   userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("Boinacoin · RankChecker", false);
    }

    private int RankForBalance(long balance)
    {
        if (balance >= 100_000) return 4;
        if (balance >= 50_000)  return 3;
        if (balance >= 10_000)  return 2;
        if (balance >= 1_000)   return 1;
        return 0;
    }

    private string GetRankName(int rank)
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
