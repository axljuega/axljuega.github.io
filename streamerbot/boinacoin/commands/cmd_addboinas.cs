// ============================================================
//  BOINACOIN · commands/cmd_addboinas.cs
//  Comando: !addboinas @usuario cantidad
//  Permiso: mod+  (moderador o streamer)
//
//  Suma (o resta si la cantidad es negativa) Boinacoins a un
//  usuario. El saldo nunca bajará de 0.
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !addboinas"
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

        // ── 1. Verificar permisos (mod o streamer) ────────────
        bool isMod       = args.ContainsKey("isModerator") && (bool)args["isModerator"];
        bool isStreamer   = args.ContainsKey("isOwner")     && (bool)args["isOwner"];
        bool isBroadcaster = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

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
            CPH.SendKickMessage($"❌ Uso: !addboinas @usuario cantidad  (cantidad puede ser negativa)");
            return true;
        }

        // ── 3. Resolver usuario ───────────────────────────────
        string targetName = rawTarget.TrimStart('@');

        if (CPH.UserInGroup(targetName, Platform.Kick, "Chat Bots"))
        {
            CPH.SendKickMessage("⚠️ Los bots del sistema no pueden participar en la economía Boinacoin.");
            return true;
        }

        // ── 4. Validar cantidad ───────────────────────────────
        if (!long.TryParse(rawAmount, out long amount) || amount == 0)
        {
            CPH.SendKickMessage($"❌ Cantidad inválida. Usa un número entero distinto de cero.");
            return true;
        }

        // ── 5. Calcular nuevo saldo (nunca por debajo de 0) ───
        long currentBalance = CPH.GetKickUserVar<long>(targetName, "boinacoin");
        long newBalance     = Math.Max(0, currentBalance + amount);

        CPH.SetKickUserVar(targetName, "boinacoin", newBalance, true);

        // ── 6. Histórico: solo si se añaden puntos ────────────
        if (amount > 0)
        {
            long totalEarned = CPH.GetKickUserVar<long>(targetName, "boinacoin_total_earned") + amount;
            CPH.SetKickUserVar(targetName, "boinacoin_total_earned", totalEarned, true);

            // ── 6.1 Tracking de sesión ───────────────────────────
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + amount;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

            string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
            var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
            lb[targetName] = lb.ContainsKey(targetName) ? lb[targetName] + amount : amount;
            var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
            CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);
        }

        // ── 7. Comprobar subida (o bajada) de rango ───────────
        CheckRankChange(targetName, newBalance);

        // ── 8. Mensaje de confirmación ────────────────────────
        string sign      = amount >= 0 ? "+" : "";
        string operation = amount >= 0 ? "añade" : "retira";

        CPH.SendKickMessage(
            $"🛠️ [{modName}] {operation} {sign}{amount} Boinacoins a {targetName} · " +
            $"Antes: {currentBalance} → Ahora: {newBalance} 🪙");

        return true;
    }

    // ── Gestiona tanto subida como bajada de rango ────────────
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
