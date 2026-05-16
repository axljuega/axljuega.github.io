// ============================================================
//  BOINACOIN · commands/cmd_resetboinas.cs
//  Comando: !resetboinas @usuario
//  Permiso: SOLO streamer (isOwner / isBroadcaster)
//           Los mods NO pueden ejecutarlo.
//
//  Resetea completamente el perfil Boinacoin de un usuario:
//    · boinacoin             → 0
//    · boinacoin_rank        → 0
//    · boinacoin_multiplier  → 0 (sin sub activa)
//    · boinacoin_streak      → 0
//    · boinacoin_streak_sub  → 0
//    · boinacoin_daily_claimed → ""
//    · boinacoin_chat_day    → ""
//    · boinacoin_chat_last   → 0
//    · boinacoin_chat_active → 0
//    · boinacoin_last_seen   → 0
//    CONSERVA boinacoin_total_earned (historial de por vida)
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !resetboinas"
//    Añadir condición: isOwner == true (o isBroadcaster)
// ============================================================

using System;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string callerId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string callerName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(callerId)) return false;

        // ── 1. Solo streamer — mods excluidos expresamente ────
        // FIX: los args de Streamer.bot v1.x llegan como string,
        //      el cast directo (bool)args["isBroadcaster"] lanza
        //      InvalidCastException. Usar .ToString().ToLower().
        bool isStreamer    = args.ContainsKey("isOwner")       &&
                             args["isOwner"].ToString().ToLower()       == "true";
        bool isBroadcaster = args.ContainsKey("isBroadcaster") &&
                             args["isBroadcaster"].ToString().ToLower() == "true";

        if (!isStreamer && !isBroadcaster)
        {
            CPH.LogInfo($"[Boinacoin] !resetboinas denegado a {callerName} (no es streamer).");
            return true;
        }

        // ── 2. Parsear argumento ──────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget))
        {
            CPH.SendKickMessage("❌ Uso: !resetboinas @usuario");
            return true;
        }

        // ── 3. Resolver usuario ───────────────────────────────
        string targetName = rawTarget.TrimStart('@');

        // ── 4. Guardar datos anteriores para el log ───────────
        long oldBalance = CPH.GetKickUserVar<long>(targetName, "boinacoin");
        int  oldRank    = CPH.GetKickUserVar<int>(targetName, "boinacoin_rank");

        // ── 5. Reset completo del perfil ──────────────────────
        CPH.SetKickUserVar(targetName, "boinacoin",               0L,  true);
        CPH.SetKickUserVar(targetName, "boinacoin_rank",          0,   true);
        CPH.SetKickUserVar(targetName, "boinacoin_multiplier",    0.0, true);
        CPH.SetKickUserVar(targetName, "boinacoin_streak",        0,   true);
        CPH.SetKickUserVar(targetName, "boinacoin_streak_sub",    0,   true);
        CPH.SetKickUserVar(targetName, "boinacoin_streak_date",   "",  true);
        CPH.SetKickUserVar(targetName, "boinacoin_daily_claimed", "",  true);
        CPH.SetKickUserVar(targetName, "boinacoin_chat_day",      "",  true);
        CPH.SetKickUserVar(targetName, "boinacoin_chat_last",     0L,  true);
        CPH.SetKickUserVar(targetName, "boinacoin_chat_active",   0L,  true);
        CPH.SetKickUserVar(targetName, "boinacoin_last_seen",     0L,  true);
        CPH.SetKickUserVar(targetName, "boinacoin_apostar_last",  0L,  true);
        CPH.SetKickUserVar(targetName, "boinacoin_regalar_last",  0L,  true);
        // boinacoin_total_earned se conserva intencionalmente

        // ── 6. Log interno ────────────────────────────────────
        CPH.LogInfo(
            $"[Boinacoin] RESET · {targetName} · " +
            $"Saldo borrado: {oldBalance} · Rango borrado: {oldRank} · " +
            $"Ejecutado por: {callerName}");

        // ── 7. Mensaje de confirmación (discreto) ─────────────
        CPH.SendKickMessage(
            $"🛠️ [{callerName}] Perfil Boinacoin de {targetName} reseteado a cero.");

        return true;
    }
}
