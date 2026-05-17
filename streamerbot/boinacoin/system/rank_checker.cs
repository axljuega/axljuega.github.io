// ============================================================
//  BOINACOIN · system/rank_checker.cs
//  Tipo: acción interna (nunca trigger directo de chat)
//
//  Es el punto central al que llaman TODOS los scripts cuando
//  detectan una subida de rango. Recibe los args:
//    · rankUpUserId   → userId del usuario que sube
//    · rankUpUserName → nombre del usuario
//    · rankUpNewRank  → nuevo rango (int 1-4)
//
//  Responsabilidades:
//    1. Verificar que el rango no se haya procesado ya
//       (guard antiduplicado por si dos scripts lo llaman
//        casi simultáneamente)
//    2. Anuncio enriquecido en chat según el rango
//    3. Bonus de Boinacoins por alcanzar el rango
//    4. Disparar discord_webhook.cs para sincronizar roles
//    5. Disparar discord_roles.cs para asignar rol en Discord
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

        if (string.IsNullOrEmpty(userName) || newRank < 1 || newRank > 4) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Guard antiduplicado ────────────────────────────
        int lastAnnounced = !string.IsNullOrEmpty(userId)
            ? CPH.GetKickUserVarById<int>(userId, "boinacoin_rank_announced")
            : CPH.GetKickUserVar<int>(userName, "boinacoin_rank_announced");

        if (lastAnnounced >= newRank)
        {
            CPH.LogInfo($"[Boinacoin] RankChecker: {userName} rango {newRank} ya anunciado, omitiendo.");
            return true;
        }

        if (!string.IsNullOrEmpty(userId))
            CPH.SetKickUserVarById(userId, "boinacoin_rank_announced", newRank, true);
        else
            CPH.SetKickUserVar(userName, "boinacoin_rank_announced", newRank, true);

        // ── 2. Bonus de Boinacoins ────────────────────────────
        long bonus = newRank < RANK_BONUS.Length ? RANK_BONUS[newRank] : 0;

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

            // ── 2.1 Tracking de sesión ───────────────────────
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + bonus;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

            string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
            var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
            lb[userName] = lb.ContainsKey(userName) ? lb[userName] + bonus : bonus;
            var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
            CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);
        }

        // ── 3. Anuncio en chat ────────────────────────────────
        if (newRank < RANK_MESSAGES.Length)
            CPH.SendKickMessage(string.Format(RANK_MESSAGES[newRank], userName, bonus));

        // ── 4. Anuncio especial Boina Legendaria ──────────────
        if (newRank == 4)
        {
            CPH.Wait(1500);
            CPH.SendKickMessage(
                $"👑👑👑 @{userName} ES LA BOINA LEGENDARIA 👑👑👑 " +
                $"¡El rango más alto del canal! Merece un aplauso enorme. 🎩🎩🎩");
        }

        // ── 5. Webhook Discord (embed de rango) ───────────────
        CPH.SetArgument("webhookUserId",   userId);
        CPH.SetArgument("webhookUserName", userName);
        CPH.SetArgument("webhookNewRank",  newRank);
        CPH.RunAction("Boinacoin · DiscordWebhook", false);

        // ── 6. Asignar rol en Discord via GestorDeBoinas ─────
        // Los args webhookUserId/UserName/NewRank ya están en el stack
        CPH.RunAction("Boinacoin · DiscordRoles", false);

        // ── 7. Log ────────────────────────────────────────────
        CPH.LogInfo($"[Boinacoin] RankChecker · {userName} → Rango {newRank} · Bonus: +{bonus}");

        return true;
    }
}
