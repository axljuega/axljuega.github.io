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
// ============================================================

using System;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // Quien ejecuta el comando
        string callerId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string callerName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(callerId)) return false;

        // ── ¿Hay argumento @usuario? ─────────────────────────
        // Streamer.bot pone el primer token tras el comando en input0.
        // Admitimos tanto "@nombre" como "nombre" sin arroba.
        string rawInput = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        bool   isLookup = !string.IsNullOrEmpty(rawInput);

        string targetId;
        string targetName;

        if (isLookup)
        {
            // Normalizar: quitar @ si lo incluye
            string lookupName = rawInput.TrimStart('@');

            // Buscar usuario por nombre en Streamer.bot
            var targetUser = CPH.KickGetUserIdByUserName(lookupName);

            if (targetUser == null)
            {
                CPH.SendMessage($"❌ {callerName}, no encuentro a @{lookupName} en la base de datos.");
                return true;
            }

            targetId   = targetUser;
            targetName = lookupName;
        }
        else
        {
            // Sin argumento → consulta propia
            targetId   = callerId;
            targetName = callerName;
        }

        // ── Leer datos del objetivo ───────────────────────────
        long   balance  = CPH.KickGetUserVar<long>(targetId,   "boinacoin",        true);
        int    rank     = CPH.KickGetUserVar<int>(targetId,    "boinacoin_rank",   true);
        int    streak   = CPH.KickGetUserVar<int>(targetId,    "boinacoin_streak", true);
        double subMult  = CPH.KickGetUserVar<double>(targetId, "boinacoin_multiplier", true);
        long   total    = CPH.KickGetUserVar<long>(targetId,   "boinacoin_total_earned", true);

        string rankName  = GetRankName(rank);
        string rankEmoji = GetRankEmoji(rank);
        string multText  = BuildMultiplierText(targetId, subMult);
        string streakText = streak >= 3 ? $" · Racha: {streak} 🔥" : "";

        // ── Mensaje al chat ───────────────────────────────────
        if (isLookup)
        {
            CPH.SendMessage(
                $"🪙 {rankEmoji} {targetName} · " +
                $"Saldo: {balance} Boinacoins · " +
                $"Rango: {rankName}{streakText}");
        }
        else
        {
            CPH.SendMessage(
                $"🪙 {rankEmoji} {callerName} · " +
                $"Saldo: {balance} Boinacoins · " +
                $"Rango: {rankName} · " +
                $"Total histórico: {total}{multText}{streakText}");
        }

        return true;
    }

    // ── Construye el texto del multiplicador activo ───────────
    private string BuildMultiplierText(string userId, double subMult)
    {
        double m = subMult > 1.0 ? subMult : 1.0;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.KickGetUserVar<int>(userId, "boinacoin_streak", true);
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.KickGetUserVar<int>(userId, "boinacoin_rank", true);
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
