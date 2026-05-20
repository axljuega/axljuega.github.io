// ============================================================
//  BOINACOIN · moderation/mod_ban.cs
//  Evento: usuario recibe ban permanente en Kick
//  Penalización: reset completo del saldo a 0
//
//  Diferencia con mod_timeout.cs:
//    · Timeout → -500 (parcial, usuario vuelve)
//    · Ban     → reset total (usuario no vuelve)
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · User Banned"
//    Filtro: args["duration"] NO existe  O  args["duration"] == 0
//    (complementario al filtro de mod_timeout.cs)
//
//  Qué resetea:
//    · boinacoin             → 0
//    · boinacoin_rank        → 0
//    · boinacoin_multiplier  → 0
//    · boinacoin_streak      → 0
//    CONSERVA boinacoin_total_earned (auditoría interna)
// ============================================================

using System;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // Resolver usuario baneado (misma lógica dual que mod_timeout)
        string userId   = args.ContainsKey("targetUserId")   ? args["targetUserId"].ToString()
                        : args.ContainsKey("userId")         ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("targetUserName") ? args["targetUserName"].ToString()
                        : args.ContainsKey("userName")       ? args["userName"].ToString() : "usuario";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Leer datos anteriores para el log ──────────────
        long oldBalance = CPH.GetKickUserVarById<long>(userId, "boinacoin");
        int  oldRank    = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");

        // Si ya estaba en 0 no hay nada que hacer
        if (oldBalance == 0 && oldRank == 0)
        {
            CPH.LogInfo($"[BoinaCoin] Ban {userName}: perfil ya estaba en cero.");
            return true;
        }

        // ── 2. Reset del perfil económico ────────────────────
        // Solo variables de saldo y progreso — no cooldowns ni
        // timestamps (el usuario no volverá a usarlos de todas formas)
        CPH.SetKickUserVarById(userId, "boinacoin",            0L,  true);
        CPH.SetKickUserVarById(userId, "boinacoin_rank",       0,   true);
        CPH.SetKickUserVarById(userId, "boinacoin_multiplier", 0.0, true);
        CPH.SetKickUserVarById(userId, "boinacoin_streak",     0,   true);
        CPH.SetKickUserVarById(userId, "boinacoin_streak_sub", 0,   true);
        // boinacoin_total_earned se conserva para auditoría

        // ── 3. Log de auditoría ───────────────────────────────
        CPH.LogInfo(
            $"[BoinaCoin] BAN PERMANENTE · {userName} (id:{userId}) · " +
            $"Saldo borrado: {oldBalance} · Rango borrado: {GetRankName(oldRank)} · " +
            $"Total histórico conservado: {CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned")}");

        // ── 4. Sin mensaje al chat ────────────────────────────
        // El ban ya es visible para todos. Anunciar además la
        // penalización económica daría protagonismo innecesario
        // al usuario baneado. Solo log interno.

        return true;
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
}
