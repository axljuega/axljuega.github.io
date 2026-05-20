// ============================================================
//  BOINACOIN · commands/cmd_boinas.cs
//  Comandos: !boinas  /  !boinas @usuario
//  Permiso: todos
//
//  Sin argumento  → muestra saldo y rango propio
//  Con @usuario   → muestra saldo y rango de otro viewer
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !boinas"
//    Habilitar "Parse Input" para que llegue input0
//
//  Arg keys confirmadas via dump (CommandTriggered / Kick):
//    userId   → ID numérico del sender (String)
//    userName → login name del sender  (String)
// ============================================================

using System;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // Mitigar race condition si se ejecuta junto a chat_message.cs
        // Damos margen suficiente para que el script de ganancia persista el saldo
        CPH.Wait(1000);

        // Quien ejecuta el comando
        // Arg keys confirmadas: 'userId' y 'userName' (sin prefijo kick)
        string callerId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string callerName = args.ContainsKey("userName") ? args["userName"].ToString() : "";

        if (string.IsNullOrEmpty(callerId) || string.IsNullOrEmpty(callerName))
        {
            CPH.LogWarn($"[BOINAS] Guardia null: callerId='{callerId}' callerName='{callerName}'");
            return false;
        }

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(callerName, Platform.Kick, "Chat Bots")) return false;

        try
        {
            // ── ¿Hay argumento @usuario? ─────────────────────────
            string rawInput = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
            bool   isLookup = !string.IsNullOrEmpty(rawInput);
            CPH.LogInfo($"[BOINAS] callerId='{callerId}' callerName='{callerName}' isLookup={isLookup} rawInput='{rawInput}'");

            if (isLookup)
            {
                string lookupName = rawInput.TrimStart('@');
                CPH.LogInfo($"[BOINAS] Lookup branch → lookupName='{lookupName}'");

                if (CPH.UserInGroup(lookupName, Platform.Kick, "Chat Bots"))
                {
                    CPH.SendKickMessage("⚠️ Los bots del sistema no pueden participar en la economía BoinaCoin.");
                    return true;
                }

                long   balance  = CPH.GetKickUserVar<long>(lookupName,   "boinacoin");
                int    rank     = CPH.GetKickUserVar<int>(lookupName,    "boinacoin_rank");
                int    streak   = CPH.GetKickUserVar<int>(lookupName,    "boinacoin_streak");
                double subMult  = CPH.GetKickUserVar<double>(lookupName, "boinacoin_multiplier");

                string rankName   = GetRankName(rank);
                string rankEmoji  = GetRankEmoji(rank);
                string multText   = BuildMultiplierTextByName(lookupName, subMult);
                string streakText = streak >= 3 ? $" · Racha: {streak} 🔥" : "";

                CPH.LogInfo($"[BOINAS] Lookup OK → balance={balance} rank={rank} streak={streak}");
                CPH.SendKickMessage(
                    $"🪙 {rankEmoji} {lookupName} · " +
                    $"Saldo: {balance} BoinaCoins · " +
                    $"Rango: {rankName}{streakText}");
            }
            else
            {
                CPH.LogInfo($"[BOINAS] Self branch → callerId='{callerId}'");

                long   balance  = CPH.GetKickUserVarById<long>(callerId,   "boinacoin");
                CPH.LogInfo($"[BOINAS] balance leído: {balance}");
                int    rank     = CPH.GetKickUserVarById<int>(callerId,    "boinacoin_rank");
                CPH.LogInfo($"[BOINAS] rank leído: {rank}");
                int    streak   = CPH.GetKickUserVarById<int>(callerId,    "boinacoin_streak");
                double subMult  = CPH.GetKickUserVarById<double>(callerId, "boinacoin_multiplier");
                long   total    = CPH.GetKickUserVarById<long>(callerId,   "boinacoin_total_earned");

                string rankName   = GetRankName(rank);
                string rankEmoji  = GetRankEmoji(rank);
                string multText   = BuildMultiplierTextById(callerId, subMult);
                string streakText = streak >= 3 ? $" · Racha: {streak} 🔥" : "";

                CPH.LogInfo($"[BOINAS] Enviando mensaje → {callerName} balance={balance} rank={rankName}");
                CPH.SendKickMessage(
                    $"🪙 {rankEmoji} {callerName} · " +
                    $"Saldo: {balance} BoinaCoins · " +
                    $"Rango: {rankName} · " +
                    $"Total histórico: {total}{multText}{streakText}");
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"[BOINAS] EXCEPCIÓN: {ex.GetType().Name}: {ex.Message}");
            CPH.LogError($"[BOINAS] StackTrace: {ex.StackTrace}");
            return false;
        }

        return true;
    }

    // ── Multiplicador por ID (auto-consulta) ─────────────────
    // FIX #3: separamos los helpers según si tenemos ID o userName
    private string BuildMultiplierTextById(string userId, double subMult)
    {
        double m = subMult > 1.0 ? subMult : 1.0;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m > 1.0 ? $" · Mult activo: x{m:0.##} ⚡" : "";
    }

    // ── Multiplicador por nombre (@lookup) ───────────────────
    private string BuildMultiplierTextByName(string userName, double subMult)
    {
        double m = subMult > 1.0 ? subMult : 1.0;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.GetKickUserVar<int>(userName, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.GetKickUserVar<int>(userName, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m > 1.0 ? $" · Mult activo: x{m:0.##} ⚡" : "";
    }

    private string GetRankName(int rank)
    {
        switch (rank)
        {
            case 1: return "Boina de Lana";
            case 2: return "Boina de Cuero";
            case 3: return "Boina de Terciopelo";
            case 4: return "La Boina Legendaria";
            default: return "Boina de Paja";
        }
    }

    private string GetRankEmoji(int rank)
    {
        switch (rank)
        {
            case 1: return "🧶";
            case 2: return "🪡";
            case 3: return "💎";
            case 4: return "👑";
            default: return "🪡";
        }
    }
}
