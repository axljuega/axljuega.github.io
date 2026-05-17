// ============================================================
//  BOINACOIN · system/rank_checker.cs
//  Tipo: acción interna (nunca trigger directo de chat)
//
//  Es el punto central al que llaman TODOS los scripts cuando
//  detectan un cambio de rango (subida o bajada). Recibe los args:
//    · rankUpUserId   → userId del usuario
//    · rankUpUserName → nombre del usuario
//    · rankUpNewRank  → nuevo rango (int 0-4)
//
//  Responsabilidades:
//    1. Verificar que el rango haya cambiado realmente respecto al
//       último anunciado (boinacoin_rank_announced).
//    2. Bonus de Boinacoins: solo si el nuevo rango supera el máximo
//       histórico (boinacoin_rank_max) para evitar abusos por re-subida.
//    3. Anuncio en chat: celebratorio si sube, discreto si baja.
//    4. Disparar discord_webhook.cs (solo en subidas).
//    5. Disparar discord_roles.cs para sincronizar el rol en Discord.
//
//  Cómo configurarlo en Streamer.bot:
//    Acción "Boinacoin · RankChecker"
//    Sub-action: Execute C# (este script)
//    (No tiene trigger de chat — solo se llama via RunAction)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private static readonly long[] RANK_BONUS = { 0, 500, 2_000, 10_000, 25_000 };

    private static readonly string[] RANK_MESSAGES =
    {
        "",
        "🧶 ¡{0} acaba de conseguir la Boina de Lana! Ya eres parte de la comunidad. +{1} Boinacoins de bienvenida.",
        "🪡 ¡¡{0} asciende a Boina de Cuero!! Acceso a recompensas Nivel 2 desbloqueado. +{1} Boinacoins.",
        "💎 ¡¡¡{0} alcanza la Boina de Terciopelo!!! Multiplicador x1.25 permanente activado. +{1} Boinacoins.",
        "👑 ¡¡¡¡{0} entra en la LEYENDA como La Boina Legendaria!!!! Multiplicador x1.5 · VIP · +{1} Boinacoins. ¡INCREÍBLE!"
    };

    public bool Execute()
    {
        string userId   = args.ContainsKey("rankUpUserId")   ? args["rankUpUserId"].ToString()   : "";
        string userName = args.ContainsKey("rankUpUserName") ? args["rankUpUserName"].ToString() : "alguien";
        int    newRank  = args.ContainsKey("rankUpNewRank")  ? Convert.ToInt32(args["rankUpNewRank"]) : 0;

        if (string.IsNullOrEmpty(userName) || newRank < 0 || newRank > 4) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Guard antiduplicado ────────────────────────────
        int lastAnnounced = !string.IsNullOrEmpty(userId)
            ? CPH.GetKickUserVarById<int>(userId, "boinacoin_rank_announced")
            : CPH.GetKickUserVar<int>(userName, "boinacoin_rank_announced");

        if (lastAnnounced == newRank) return true;

        if (!string.IsNullOrEmpty(userId))
            CPH.SetKickUserVarById(userId, "boinacoin_rank_announced", newRank, true);
        else
            CPH.SetKickUserVar(userName, "boinacoin_rank_announced", newRank, true);

        // ── 2. Gestión de Subida vs Bajada ───────────────────
        if (newRank > lastAnnounced)
        {
            HandleRankUp(userId, userName, newRank);
        }
        else
        {
            HandleRankDown(userId, userName, newRank);
        }

        // ── 3. Sincronizar roles en Discord ──────────────────
        CPH.SetArgument("webhookUserId",   userId);
        CPH.SetArgument("webhookUserName", userName);
        CPH.SetArgument("webhookNewRank",  newRank);
        CPH.RunAction("Boinacoin · DiscordRoles", false);

        CPH.LogInfo($"[Boinacoin] RankChecker · {userName} → Rango {newRank} (antes {lastAnnounced})");

        return true;
    }

    private void HandleRankUp(string userId, string userName, int newRank)
    {
        // ── 1. Bonus de Boinacoins (solo si supera el récord histórico) ─
        int rankMax = !string.IsNullOrEmpty(userId)
            ? CPH.GetKickUserVarById<int>(userId, "boinacoin_rank_max")
            : CPH.GetKickUserVar<int>(userName, "boinacoin_rank_max");

        long bonus = 0;
        if (newRank > rankMax)
        {
            bonus = newRank < RANK_BONUS.Length ? RANK_BONUS[newRank] : 0;

            // Actualizar récord histórico
            if (!string.IsNullOrEmpty(userId))
                CPH.SetKickUserVarById(userId, "boinacoin_rank_max", newRank, true);
            else
                CPH.SetKickUserVar(userName, "boinacoin_rank_max", newRank, true);
        }

        if (bonus > 0)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                long balance     = CPH.GetKickUserVarById<long>(userId, "boinacoin") + bonus;
                long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + bonus;
                CPH.SetKickUserVarById(userId, "boinacoin", balance, true);
                CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);
            }
            else
            {
                long balance     = CPH.GetKickUserVar<long>(userName, "boinacoin") + bonus;
                long totalEarned = CPH.GetKickUserVar<long>(userName, "boinacoin_total_earned") + bonus;
                CPH.SetKickUserVar(userName, "boinacoin", balance, true);
                CPH.SetKickUserVar(userName, "boinacoin_total_earned", totalEarned, true);
            }

            // Tracking de sesión
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + bonus;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);
        }

        // ── 2. Anuncio en chat ────────────────────────────────
        if (newRank < RANK_MESSAGES.Length)
            CPH.SendKickMessage(string.Format(RANK_MESSAGES[newRank], "@" + userName, bonus));

        if (newRank == 4)
        {
            CPH.Wait(1500);
            CPH.SendKickMessage($"👑👑👑 @{userName} ES LA BOINA LEGENDARIA 👑👑👑");
        }

        // ── 3. Webhook Discord (Embed) ────────────────────────
        CPH.SetArgument("webhookUserId",   userId);
        CPH.SetArgument("webhookUserName", userName);
        CPH.SetArgument("webhookNewRank",  newRank);
        CPH.RunAction("Boinacoin · DiscordWebhook", false);
    }

    private void HandleRankDown(string userId, string userName, int newRank)
    {
        string rankName = RankName(newRank);
        CPH.SendKickMessage($"📉 @{userName} baja a {rankName}.");
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
