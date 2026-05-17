// ============================================================
//  BOINACOIN · system/rank_checker.cs
//  Tipo: acción interna (nunca trigger directo de chat)
//
//  Punto central al que llaman TODOS los scripts cuando
//  detectan un cambio de rango (subida o bajada).
//
//  Args que recibe:
//    · rankUpUserId   → userId del usuario (puede estar vacío
//                       en scripts que solo tienen userName)
//    · rankUpUserName → nombre del usuario
//    · rankUpNewRank  → nuevo rango (int 0-4)
//
//  Responsabilidades:
//    1. Guard antiduplicado via boinacoin_rank_announced.
//    2. Bonus de Boinacoins: solo si newRank supera el récord
//       histórico (boinacoin_rank_max) → sin abuso por re-subida.
//    3. Anuncio en chat de Kick.
//    4. Anuncio en Discord (DiscordWebhook) → tanto en subidas
//       como en BAJADAS. Flag webhookIsDowngrade diferencia el embed.
//    5. Sincronización de rol en Discord (DiscordRoles) → tanto
//       en subidas como en BAJADAS. Rango 0 = quita todos los roles.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private static readonly long[] RANK_BONUS = { 0, 500, 2_000, 10_000, 25_000 };

    private static readonly string[] RANK_UP_MESSAGES =
    {
        "",
        "🧶 ¡{0} acaba de conseguir la Boina de Lana! Ya eres parte de la comunidad. +{1} Boinacoins de bienvenida.",
        "🪡 ¡¡{0} asciende a Boina de Cuero!! Acceso a recompensas Nivel 2 desbloqueado. +{1} Boinacoins.",
        "💎 ¡¡¡{0} alcanza la Boina de Terciopelo!!! Multiplicador x1.25 permanente activado. +{1} Boinacoins.",
        "👑 ¡¡¡¡{0} entra en la LEYENDA como La Boina Legendaria!!!! Multiplicador x1.5 · VIP · +{1} Boinacoins. ¡INCREÍBLE!"
    };

    // Mensajes de bajada: discretos pero visibles en el chat de Kick
    private static readonly string[] RANK_DOWN_MESSAGES =
    {
        "📉 @{0} cae a Boina de Paja. Sin rango activo.",      // 0
        "📉 @{0} baja a 🧶 Boina de Lana.",                    // 1
        "📉 @{0} baja a 🪡 Boina de Cuero.",                   // 2
        "📉 @{0} baja a 💎 Boina de Terciopelo.",              // 3
        "📉 @{0} baja a 👑 La Boina Legendaria.",              // 4 (improbable, pero por completitud)
    };

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("rankUpUserId")   ? args["rankUpUserId"].ToString()   : "";
        string userName = args.ContainsKey("rankUpUserName") ? args["rankUpUserName"].ToString() : "alguien";
        int    newRank  = args.ContainsKey("rankUpNewRank")  ? Convert.ToInt32(args["rankUpNewRank"]) : 0;

        if (string.IsNullOrEmpty(userName) || newRank < 0 || newRank > 4) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Guard antiduplicado ────────────────────────────
        // Evita anunciar el mismo rango dos veces seguidas.
        int lastAnnounced = !string.IsNullOrEmpty(userId)
            ? CPH.GetKickUserVarById<int>(userId, "boinacoin_rank_announced")
            : CPH.GetKickUserVar<int>(userName, "boinacoin_rank_announced");

        if (lastAnnounced == newRank) return true;

        if (!string.IsNullOrEmpty(userId))
            CPH.SetKickUserVarById(userId, "boinacoin_rank_announced", newRank, true);
        else
            CPH.SetKickUserVar(userName, "boinacoin_rank_announced", newRank, true);

        // ── 2. Gestión de Subida vs Bajada ───────────────────
        bool isDowngrade = newRank < lastAnnounced;

        if (!isDowngrade)
            HandleRankUp(userId, userName, newRank);
        else
            HandleRankDown(userId, userName, newRank, lastAnnounced);

        // ── 3. Sincronizar rol en Discord (subida Y bajada) ───
        // DiscordRoles elimina todos los roles de rango y asigna
        // el nuevo. Si newRank == 0, elimina todos y no asigna ninguno.
        CPH.SetArgument("webhookUserId",       userId);
        CPH.SetArgument("webhookUserName",     userName);
        CPH.SetArgument("webhookNewRank",      newRank);
        CPH.SetArgument("webhookIsDowngrade",  isDowngrade);
        CPH.RunAction("Boinacoin · DiscordRoles", false);

        CPH.LogInfo($"[Boinacoin] RankChecker · {userName} · {lastAnnounced} → {newRank} · downgrade={isDowngrade}");

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  SUBIDA DE RANGO
    // ════════════════════════════════════════════════════════
    private void HandleRankUp(string userId, string userName, int newRank)
    {
        // ── 1. Bonus: solo si supera el récord histórico ──────
        int rankMax = !string.IsNullOrEmpty(userId)
            ? CPH.GetKickUserVarById<int>(userId, "boinacoin_rank_max")
            : CPH.GetKickUserVar<int>(userName, "boinacoin_rank_max");

        long bonus = 0;
        if (newRank > rankMax)
        {
            bonus = newRank < RANK_BONUS.Length ? RANK_BONUS[newRank] : 0;

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
                CPH.SetKickUserVarById(userId, "boinacoin",             balance,     true);
                CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);
            }
            else
            {
                long balance     = CPH.GetKickUserVar<long>(userName, "boinacoin") + bonus;
                long totalEarned = CPH.GetKickUserVar<long>(userName, "boinacoin_total_earned") + bonus;
                CPH.SetKickUserVar(userName, "boinacoin",             balance,     true);
                CPH.SetKickUserVar(userName, "boinacoin_total_earned", totalEarned, true);
            }

            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + bonus;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);
        }

        // ── 2. Anuncio en chat de Kick ────────────────────────
        if (newRank < RANK_UP_MESSAGES.Length)
            CPH.SendKickMessage(string.Format(RANK_UP_MESSAGES[newRank], "@" + userName, bonus));

        if (newRank == 4)
        {
            CPH.Wait(1500);
            CPH.SendKickMessage($"👑👑👑 @{userName} ES LA BOINA LEGENDARIA 👑👑👑");
        }

        // ── 3. Anuncio en Discord (embed celebratorio) ────────
        // webhookIsDowngrade = false → embed verde/dorado
        CPH.SetArgument("webhookUserId",      userId);
        CPH.SetArgument("webhookUserName",    userName);
        CPH.SetArgument("webhookNewRank",     newRank);
        CPH.SetArgument("webhookBonus",       bonus);
        CPH.SetArgument("webhookIsDowngrade", false);
        CPH.RunAction("Boinacoin · DiscordWebhook", false);
    }

    // ════════════════════════════════════════════════════════
    //  BAJADA DE RANGO
    // ════════════════════════════════════════════════════════
    private void HandleRankDown(string userId, string userName, int newRank, int oldRank)
    {
        // ── 1. Anuncio en chat de Kick (discreto) ─────────────
        if (newRank < RANK_DOWN_MESSAGES.Length)
            CPH.SendKickMessage(string.Format(RANK_DOWN_MESSAGES[newRank], userName));

        // ── 2. Anuncio en Discord (embed de degradación) ──────
        // FIX: HandleRankDown ahora también dispara DiscordWebhook.
        // webhookIsDowngrade = true → discord_webhook.cs renderiza
        // un embed rojo/oscuro en lugar del embed celebratorio.
        // webhookOldRank se pasa para que el embed pueda mostrar
        // "De X a Y" en la descripción.
        CPH.SetArgument("webhookUserId",      userId);
        CPH.SetArgument("webhookUserName",    userName);
        CPH.SetArgument("webhookNewRank",     newRank);
        CPH.SetArgument("webhookOldRank",     oldRank);
        CPH.SetArgument("webhookBonus",       0L);
        CPH.SetArgument("webhookIsDowngrade", true);
        CPH.RunAction("Boinacoin · DiscordWebhook", false);
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
