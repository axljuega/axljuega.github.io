// ============================================================
//  BOINACOIN · commands/cmd_setboinas.cs
//  Comando: !setboinas @usuario cantidad
//  Permiso: mod+  (moderador o streamer)
//
//  Fija el saldo de un usuario a una cantidad exacta,
//  independientemente de lo que tuviera antes.
//  Diferencia con !addboinas: este sobreescribe, no suma.
//
//  Casos de uso típicos:
//    · Corregir un saldo erróneo por bug
//    · Premio especial con valor fijo ("ponle 50.000")
//    · Reseteo parcial (distinto de !resetboinas que va a 0)
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !setboinas"
//    Añadir condición: isModerator == true
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string modId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string modName = args.ContainsKey("userName") ? args["userName"].ToString() : "mod";

        if (string.IsNullOrEmpty(modId)) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(modName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Verificar permisos ─────────────────────────────
        bool isMod         = args.ContainsKey("isModerator")   && (bool)args["isModerator"];
        bool isStreamer     = args.ContainsKey("isOwner")       && (bool)args["isOwner"];
        bool isBroadcaster  = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

        if (!isMod && !isStreamer && !isBroadcaster)
        {
            CPH.SendKickMessage($"🔒 {modName}, este comando es solo para moderadores.");
            return true;
        }

        // ── 2. Parsear argumentos ─────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        string rawAmount = args.ContainsKey("input1") ? args["input1"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget) || string.IsNullOrEmpty(rawAmount))
        {
            CPH.SendKickMessage("❌ Uso: !setboinas @usuario cantidad");
            return true;
        }

        // ── 3. Resolver usuario ───────────────────────────────
        string targetName = rawTarget.TrimStart('@');

        if (CPH.UserInGroup(targetName, Platform.Kick, "Chat Bots"))
        {
            CPH.SendKickMessage("⚠️ Los bots del sistema no pueden participar en la economía Boinacoin.");
            return true;
        }

        // ── 4. Validar cantidad (≥ 0, no negativa) ───────────
        if (!long.TryParse(rawAmount, out long newAmount) || newAmount < 0)
        {
            CPH.SendKickMessage("❌ La cantidad debe ser un número entero positivo (o 0).");
            return true;
        }

        // ── 5. Guardar saldo anterior para el log ─────────────
        long oldBalance = CPH.GetKickUserVar<long>(targetName, "boinacoin");

        // ── 6. Fijar nuevo saldo ──────────────────────────────
        CPH.SetKickUserVar(targetName, "boinacoin", newAmount, true);

        // ── 7. Ajustar histórico total ────────────────────────
        // Si el nuevo saldo es mayor, la diferencia va al histórico.
        // Si es menor, no tocamos el histórico (no restamos ganancia histórica).
        if (newAmount > oldBalance)
        {
            long diff = newAmount - oldBalance;
            long totalEarned = CPH.GetKickUserVar<long>(targetName, "boinacoin_total_earned");
            CPH.SetKickUserVar(targetName, "boinacoin_total_earned", totalEarned + diff, true);

            // ── 7.1 Tracking de sesión ───────────────────────────
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + diff;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

            string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
            var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
            lb[targetName] = lb.ContainsKey(targetName) ? lb[targetName] + diff : diff;
            var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
            CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);
        }

        // ── 8. Comprobar cambio de rango ──────────────────────
        CheckRankChange(targetName, newAmount);

        // ── 9. Mensaje de confirmación ────────────────────────
        string arrow = newAmount > oldBalance ? "⬆️" : newAmount < oldBalance ? "⬇️" : "↔️";
        CPH.SendKickMessage(
            $"🛠️ [{modName}] Saldo de {targetName} fijado a {newAmount} 🪙 " +
            $"{arrow} (antes: {oldBalance})");

        return true;
    }

    // ── Gestiona subida y bajada de rango ─────────────────────
    private void CheckRankChange(string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVar<int>(userName, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank == oldRank) return;

        CPH.SetKickUserVar(userName, "boinacoin_rank", newRank, true);

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
