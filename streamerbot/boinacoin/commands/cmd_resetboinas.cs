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
        bool isStreamer    = args.ContainsKey("isOwner")       && (bool)args["isOwner"];
        bool isBroadcaster = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

        if (!isStreamer && !isBroadcaster)
        {
            // Silencioso para mods: no anunciamos en chat que
            // existe este comando ni que no tienen permiso.
            CPH.LogInfo($"[Boinacoin] !resetboinas denegado a {callerName} (no es streamer).");
            return true;
        }

        // ── 2. Parsear argumento ──────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget))
        {
            CPH.SendMessage("❌ Uso: !resetboinas @usuario");
            return true;
        }

        // ── 3. Resolver usuario ───────────────────────────────
        string targetName = rawTarget.TrimStart('@');
        string targetId   = CPH.GetUserIdByUserName(targetName);

        if (string.IsNullOrEmpty(targetId))
        {
            CPH.SendMessage($"❌ No encuentro a @{targetName} en la base de datos.");
            return true;
        }

        // ── 4. Guardar datos anteriores para el log ───────────
        long oldBalance = CPH.GetUserVar<long>(targetId, "boinacoin",      true);
        int  oldRank    = CPH.GetUserVar<int>(targetId,  "boinacoin_rank", true);

        // ── 5. Reset completo del perfil ──────────────────────
        CPH.SetUserVar(targetId, "boinacoin",              0L,   true);
        CPH.SetUserVar(targetId, "boinacoin_rank",         0,    true);
        CPH.SetUserVar(targetId, "boinacoin_multiplier",   0.0,  true);
        CPH.SetUserVar(targetId, "boinacoin_streak",       0,    true);
        CPH.SetUserVar(targetId, "boinacoin_streak_sub",   0,    true);
        CPH.SetUserVar(targetId, "boinacoin_streak_date",  "",   true);
        CPH.SetUserVar(targetId, "boinacoin_daily_claimed","",   true);
        CPH.SetUserVar(targetId, "boinacoin_chat_day",     "",   true);
        CPH.SetUserVar(targetId, "boinacoin_chat_last",    0L,   true);
        CPH.SetUserVar(targetId, "boinacoin_chat_active",  0L,   true);
        CPH.SetUserVar(targetId, "boinacoin_last_seen",    0L,   true);
        CPH.SetUserVar(targetId, "boinacoin_apostar_last", 0L,   true);
        CPH.SetUserVar(targetId, "boinacoin_regalar_last", 0L,   true);
        // boinacoin_total_earned se conserva intencionalmente

        // ── 6. Log interno (no al chat público) ──────────────
        CPH.LogInfo(
            $"[Boinacoin] RESET · {targetName} (id:{targetId}) · " +
            $"Saldo borrado: {oldBalance} · Rango borrado: {oldRank} · " +
            $"Ejecutado por: {callerName}");

        // ── 7. Mensaje de confirmación (discreto) ─────────────
        // Mensaje mínimo: no conviene publicitar el reset en el chat.
        CPH.SendMessage(
            $"🛠️ [{callerName}] Perfil Boinacoin de {targetName} reseteado a cero.");

        return true;
    }
}
