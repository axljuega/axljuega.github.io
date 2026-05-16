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

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string modId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string modName = args.ContainsKey("userName") ? args["userName"].ToString() : "mod";

        if (string.IsNullOrEmpty(modId)) return false;

        // ── 1. Verificar permisos ─────────────────────────────
        bool isMod         = args.ContainsKey("isModerator")   && (bool)args["isModerator"];
        bool isStreamer     = args.ContainsKey("isOwner")       && (bool)args["isOwner"];
        bool isBroadcaster  = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

        if (!isMod && !isStreamer && !isBroadcaster)
        {
            CPH.SendMessage($"🔒 {modName}, este comando es solo para moderadores.");
            return true;
        }

        // ── 2. Parsear argumentos ─────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        string rawAmount = args.ContainsKey("input1") ? args["input1"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget) || string.IsNullOrEmpty(rawAmount))
        {
            CPH.SendMessage("❌ Uso: !setboinas @usuario cantidad");
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

        // ── 4. Validar cantidad (≥ 0, no negativa) ───────────
        if (!long.TryParse(rawAmount, out long newAmount) || newAmount < 0)
        {
            CPH.SendMessage("❌ La cantidad debe ser un número entero positivo (o 0).");
            return true;
        }

        // ── 5. Guardar saldo anterior para el log ─────────────
        long oldBalance = CPH.GetUserVar<long>(targetId, "boinacoin", true);

        // ── 6. Fijar nuevo saldo ──────────────────────────────
        CPH.SetUserVar(targetId, "boinacoin", newAmount, true);

        // ── 7. Ajustar histórico total ────────────────────────
        // Si el nuevo saldo es mayor, la diferencia va al histórico.
        // Si es menor, no tocamos el histórico (no restamos ganancia histórica).
        if (newAmount > oldBalance)
        {
            long totalEarned = CPH.GetUserVar<long>(targetId, "boinacoin_total_earned", true);
            CPH.SetUserVar(targetId, "boinacoin_total_earned", totalEarned + (newAmount - oldBalance), true);
        }

        // ── 8. Comprobar cambio de rango ──────────────────────
        CheckRankChange(targetId, targetName, newAmount);

        // ── 9. Mensaje de confirmación ────────────────────────
        string arrow = newAmount > oldBalance ? "⬆️" : newAmount < oldBalance ? "⬇️" : "↔️";
        CPH.SendMessage(
            $"🛠️ [{modName}] Saldo de {targetName} fijado a {newAmount} 🪙 " +
            $"{arrow} (antes: {oldBalance})");

        return true;
    }

    // ── Gestiona subida y bajada de rango ─────────────────────
    private void CheckRankChange(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetUserVar<int>(userId, "boinacoin_rank", true);
        int newRank = RankForBalance(balance);

        if (newRank == oldRank) return;

        CPH.SetUserVar(userId, "boinacoin_rank", newRank, true);

        if (newRank > oldRank)
        {
            CPH.SendMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

            CPH.SetArgument("rankUpUserId",   userId);
            CPH.SetArgument("rankUpUserName", userName);
            CPH.SetArgument("rankUpNewRank",  newRank);
            CPH.RunAction("Boinacoin · RankChecker", false);
        }
        else
        {
            CPH.SendMessage(
                $"⬇️ {userName} baja a {GetRankName(newRank)} " +
                $"(antes: {GetRankName(oldRank)}).");
        }
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
