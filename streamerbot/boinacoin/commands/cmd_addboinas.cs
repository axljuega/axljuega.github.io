// ============================================================
//  BOINACOIN · commands/cmd_addboinas.cs
//  Comando: !addboinas @usuario cantidad
//  Permiso: mod+  (moderador o streamer)
//
//  Añade una cantidad de Boinacoins al saldo de un usuario.
//  Útil para premios manuales, correcciones o eventos especiales.
//  Acepta cantidades negativas para restar (correcciones).
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !addboinas"
//    En la acción añadir condición: isModerator == true
//    (o gestionar la comprobación de permisos aquí mismo)
// ============================================================

using System;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string modId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string modName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "mod";

        if (string.IsNullOrEmpty(modId)) return false;

        // ── 1. Verificar permisos (mod o streamer) ────────────
        bool isMod       = args.ContainsKey("isModerator") && (bool)args["isModerator"];
        bool isStreamer   = args.ContainsKey("isOwner")     && (bool)args["isOwner"];
        bool isBroadcaster = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

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
            CPH.SendMessage($"❌ Uso: !addboinas @usuario cantidad  (cantidad puede ser negativa)");
            return true;
        }

        // ── 3. Resolver usuario ───────────────────────────────
        string targetName = rawTarget.TrimStart('@');
        string targetId   = CPH.KickGetUserIdByUserName(targetName);

        if (string.IsNullOrEmpty(targetId))
        {
            CPH.SendMessage($"❌ No encuentro a @{targetName} en la base de datos.");
            return true;
        }

        // ── 4. Validar cantidad ───────────────────────────────
        if (!long.TryParse(rawAmount, out long amount) || amount == 0)
        {
            CPH.SendMessage($"❌ Cantidad inválida. Usa un número entero distinto de cero.");
            return true;
        }

        // ── 5. Calcular nuevo saldo (nunca por debajo de 0) ───
        long currentBalance = CPH.KickGetUserVar<long>(targetId, "boinacoin", true);
        long newBalance     = Math.Max(0, currentBalance + amount);

        CPH.KickSetUserVar(targetId, "boinacoin", newBalance, true);

        // ── 6. Histórico: solo si se añaden puntos ────────────
        if (amount > 0)
        {
            long totalEarned = CPH.KickGetUserVar<long>(targetId, "boinacoin_total_earned", true) + amount;
            CPH.KickSetUserVar(targetId, "boinacoin_total_earned", totalEarned, true);
        }

        // ── 7. Comprobar subida (o bajada) de rango ───────────
        CheckRankChange(targetId, targetName, newBalance);

        // ── 8. Mensaje de confirmación ────────────────────────
        string sign      = amount >= 0 ? "+" : "";
        string operation = amount >= 0 ? "añade" : "retira";

        CPH.SendMessage(
            $"🛠️ [{modName}] {operation} {sign}{amount} Boinacoins a {targetName} · " +
            $"Antes: {currentBalance} → Ahora: {newBalance} 🪙");

        return true;
    }

    // ── Gestiona tanto subida como bajada de rango ────────────
    private void CheckRankChange(string userId, string userName, long balance)
    {
        int oldRank = CPH.KickGetUserVar<int>(userId, "boinacoin_rank", true);
        int newRank = RankForBalance(balance);

        if (newRank == oldRank) return;

        CPH.KickSetUserVar(userId, "boinacoin_rank", newRank, true);

        if (newRank > oldRank)
        {
            // Subida de rango
            CPH.SendMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

            CPH.SetArgument("rankUpUserId",   userId);
            CPH.SetArgument("rankUpUserName", userName);
            CPH.SetArgument("rankUpNewRank",  newRank);
            CPH.RunAction("Boinacoin · RankChecker", false);
        }
        else
        {
            // Bajada de rango (por resta manual)
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
