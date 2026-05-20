// ============================================================
//  BOINACOIN · commands/cmd_apostar.cs
//  Comando: !apostar cantidad
//  Permiso: Boina de Lana+ (rank >= 1)
//
//  Mecánica:
//    1. Usuario apuesta X BoinaCoins
//    2. Probabilidad 50% de ganar (dobla apuesta) o perder.
//    3. Límites:
//       - Mínimo: 10
//       - Máximo: 20% del saldo (o 5.000 absoluto)
//    4. Cooldown: 5 minutos por usuario
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !apostar"
//    Habilitar "Parse Input" para capturar la cantidad
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const long   MIN_BET          = 10;
    private const long   MAX_BET_ABSOLUTE = 5_000;
    private const double MAX_BET_PERCENT  = 0.20;   // 20% del saldo
    private const int    COOLDOWN_SECS    = 300;     // 5 minutos

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Verificar rango mínimo (Boina de Lana+) ───────
        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        if (rank < 1)
        {
            long toLana = 1_000 - CPH.GetKickUserVarById<long>(userId, "boinacoin");
            CPH.SendKickMessage(
                $"🔒 {userName}, necesitas ser 🧶 Boina de Lana para apostar. " +
                $"Te faltan {Math.Max(0, toLana)} BoinaCoins.");
            return true;
        }

        // ── 2. Parsear cantidad ───────────────────────────────
        string rawAmount = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";

        if (!long.TryParse(rawAmount, out long bet) || bet <= 0)
        {
            CPH.SendKickMessage($"❌ {userName}, uso correcto: !apostar cantidad");
            return true;
        }

        // ── 3. Cooldown de 5 minutos ──────────────────────────
        long nowUnix     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastBet     = CPH.GetKickUserVarById<long>(userId, "boinacoin_apostar_last");
        long secsLeft    = COOLDOWN_SECS - (nowUnix - lastBet);

        if (secsLeft > 0)
        {
            int minsLeft = (int)Math.Ceiling(secsLeft / 60.0);
            CPH.SendKickMessage(
                $"⏳ {userName}, cooldown activo. " +
                $"Puedes volver a apostar en {minsLeft} min.");
            return true;
        }

        // ── 4. Validar límites de apuesta ─────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");

        if (balance < MIN_BET)
        {
            CPH.SendKickMessage($"❌ {userName}, necesitas al menos {MIN_BET} BoinaCoins para apostar.");
            return true;
        }

        long maxBet = Math.Min(MAX_BET_ABSOLUTE, (long)Math.Floor(balance * MAX_BET_PERCENT));
        maxBet      = Math.Max(maxBet, MIN_BET); // garantiza mínimo apostable

        if (bet < MIN_BET)
        {
            CPH.SendKickMessage($"❌ {userName}, apuesta mínima: {MIN_BET} BoinaCoins.");
            return true;
        }

        if (bet > maxBet)
        {
            CPH.SendKickMessage(
                $"❌ {userName}, tu apuesta máxima ahora es {maxBet} BoinaCoins " +
                $"(20% de tu saldo o {MAX_BET_ABSOLUTE}, lo que sea menor).");
            return true;
        }

        // ── 5. Lanzar la moneda ───────────────────────────────
        bool win       = new Random().Next(0, 2) == 1; // 50/50
        long newBalance;
        string resultMsg;

        if (win)
        {
            newBalance = balance + bet;
            resultMsg  = $"🪙 ¡CARA! {userName} gana {bet} BoinaCoins · Saldo: {newBalance} 🎉";
        }
        else
        {
            newBalance = balance - bet;
            resultMsg  = $"💀 ¡CRUZ! {userName} pierde {bet} BoinaCoins · Saldo: {newBalance} 😬";
        }

        // ── 6. Guardar nuevo saldo ────────────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin", newBalance, true);

        // ── 7. Registrar cooldown ─────────────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_apostar_last", nowUnix, true);

        // ── 8. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen", nowUnix, true);

        // ── 9. Si gana, actualizar histórico ──────────────────
        if (win)
        {
            long total = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + bet;
            CPH.SetKickUserVarById(userId, "boinacoin_total_earned", total, true);

            // ── 9.1 Tracking de sesión ───────────────────────────
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + bet;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

            string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
            var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
            lb[userName] = lb.ContainsKey(userName) ? lb[userName] + bet : bet;
            var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
            CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);
        }

        // ── 10. Comprobar cambio de rango (Subida o Bajada) ───
        CheckRankChange(userId, userName, newBalance);

        // ── 11. Mensaje al chat ───────────────────────────────
        CPH.SendKickMessage(resultMsg);

        return true;
    }

    // ── Cambio de rango ───────────────────────────────────────
    private void CheckRankChange(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank == oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);

        CPH.SetArgument("rankUpUserId",   userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("BoinaCoin · RankChecker", false);
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
