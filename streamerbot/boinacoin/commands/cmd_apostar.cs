// ============================================================
//  BOINACOIN · commands/cmd_apostar.cs
//  Comando: !apostar cantidad
//  Permiso: Boina de Lana+ (rank >= 1)
//
//  Mecánica:
//    El bot lanza una moneda.
//    · Cara (50%) → gana: saldo + cantidad apostada (x2)
//    · Cruz (50%) → pierde: saldo - cantidad apostada (x0)
//
//  Límites antiinflación:
//    · Mínimo apostable: 10 Boinacoins
//    · Máximo apostable: el menor valor entre 5.000 y el 20%
//      del saldo actual del usuario
//    · Cooldown: 5 minutos por usuario
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !apostar"
//    Parse Input activado: input0 = cantidad
// ============================================================

using System;

public class CPHInline
{
    private const long   MIN_BET          = 10;
    private const long   MAX_BET_ABSOLUTE = 5_000;
    private const double MAX_BET_PERCENT  = 0.20;   // 20% del saldo
    private const int    COOLDOWN_SECS    = 300;     // 5 minutos

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string userName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 1. Verificar rango mínimo (Boina de Lana+) ───────
        int rank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        if (rank < 1)
        {
            long toLana = 1_000 - CPH.GetKickUserVar<long>(userId, "boinacoin");
            CPH.SendMessage(
                $"🔒 {userName}, necesitas ser 🧶 Boina de Lana para apostar. " +
                $"Te faltan {Math.Max(0, toLana)} Boinacoins.");
            return true;
        }

        // ── 2. Parsear cantidad ───────────────────────────────
        string rawAmount = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";

        if (!long.TryParse(rawAmount, out long bet) || bet <= 0)
        {
            CPH.SendMessage($"❌ {userName}, uso correcto: !apostar cantidad");
            return true;
        }

        // ── 3. Cooldown de 5 minutos ──────────────────────────
        long nowUnix     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastBet     = CPH.GetKickUserVar<long>(userId, "boinacoin_apostar_last");
        long secsLeft    = COOLDOWN_SECS - (nowUnix - lastBet);

        if (secsLeft > 0)
        {
            int minsLeft = (int)Math.Ceiling(secsLeft / 60.0);
            CPH.SendMessage(
                $"⏳ {userName}, cooldown activo. " +
                $"Puedes volver a apostar en {minsLeft} min.");
            return true;
        }

        // ── 4. Validar límites de apuesta ─────────────────────
        long balance = CPH.GetKickUserVar<long>(userId, "boinacoin");

        if (balance < MIN_BET)
        {
            CPH.SendMessage($"❌ {userName}, necesitas al menos {MIN_BET} Boinacoins para apostar.");
            return true;
        }

        long maxBet = Math.Min(MAX_BET_ABSOLUTE, (long)Math.Floor(balance * MAX_BET_PERCENT));
        maxBet      = Math.Max(maxBet, MIN_BET); // garantiza mínimo apostable

        if (bet < MIN_BET)
        {
            CPH.SendMessage($"❌ {userName}, apuesta mínima: {MIN_BET} Boinacoins.");
            return true;
        }

        if (bet > maxBet)
        {
            CPH.SendMessage(
                $"❌ {userName}, tu apuesta máxima ahora es {maxBet} Boinacoins " +
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
            resultMsg  = $"🪙 ¡CARA! {userName} gana {bet} Boinacoins · Saldo: {newBalance} 🎉";
        }
        else
        {
            newBalance = balance - bet;
            resultMsg  = $"💀 ¡CRUZ! {userName} pierde {bet} Boinacoins · Saldo: {newBalance} 😬";
        }

        // ── 6. Guardar nuevo saldo ────────────────────────────
        CPH.SetKickUserVar(userId, "boinacoin", newBalance, true);

        // ── 7. Registrar cooldown ─────────────────────────────
        CPH.SetKickUserVar(userId, "boinacoin_apostar_last", nowUnix, true);

        // ── 8. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVar(userId, "boinacoin_last_seen", nowUnix, true);

        // ── 9. Si gana, actualizar histórico ──────────────────
        if (win)
        {
            long total = CPH.GetKickUserVar<long>(userId, "boinacoin_total_earned") + bet;
            CPH.SetKickUserVar(userId, "boinacoin_total_earned", total, true);

            // Comprobar subida de rango solo al ganar
            CheckRankUp(userId, userName, newBalance);
        }

        // ── 10. Mensaje al chat ───────────────────────────────
        CPH.SendMessage(resultMsg);

        return true;
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVar(userId, "boinacoin_rank", newRank, true);
        CPH.SendMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

        CPH.SetArgument("rankUpUserId",   userId);
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
