// ============================================================
//  BOINACOIN · commands/cmd_regalar.cs
//  Comando: !regalar @usuario cantidad
//  Permiso: todos
//
//  Validaciones:
//    · Cantidad mínima: 10 Boinacoins
//    · Saldo suficiente en el emisor
//    · No regalarse a uno mismo
//    · Cooldown: 60 segundos por usuario emisor
//    · Receptor debe existir en la base de datos local
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !regalar"
//    Parse Input activado: input0 = @usuario, input1 = cantidad
// ============================================================

using System;

public class CPHInline
{
    private const long MIN_TRANSFER   = 10;
    private const int  COOLDOWN_SECS  = 60;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string senderId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string senderName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(senderId)) return false;

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(senderName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Parsear argumentos ─────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        string rawAmount = args.ContainsKey("input1") ? args["input1"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget) || string.IsNullOrEmpty(rawAmount))
        {
            CPH.SendKickMessage($"❌ {senderName}, uso correcto: !regalar @usuario cantidad");
            return true;
        }

        // ── 2. Validar cantidad ───────────────────────────────
        if (!long.TryParse(rawAmount, out long amount) || amount < MIN_TRANSFER)
        {
            CPH.SendKickMessage($"❌ {senderName}, la cantidad mínima para regalar es {MIN_TRANSFER} Boinacoins.");
            return true;
        }

        // ── 3. Cooldown del emisor ────────────────────────────
        long nowUnix      = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastGift     = CPH.GetKickUserVarById<long>(senderId, "boinacoin_regalar_last");
        long secondsLeft  = COOLDOWN_SECS - (nowUnix - lastGift);

        if (secondsLeft > 0)
        {
            CPH.SendKickMessage($"⏳ {senderName}, espera {secondsLeft}s antes de volver a regalar.");
            return true;
        }

        // ── 4. Resolver usuario receptor ─────────────────────
        string targetName = rawTarget.TrimStart('@');

        if (CPH.UserInGroup(targetName, Platform.Kick, "Chat Bots"))
        {
            CPH.SendKickMessage("⚠️ Los bots del sistema no pueden participar en la economía Boinacoin.");
            return true;
        }

        // ── 5. No regalarse a uno mismo ───────────────────────
        if (targetName.ToLower() == senderName.ToLower())
        {
            CPH.SendKickMessage($"😅 {senderName}, no puedes regalarte Boinacoins a ti mismo.");
            return true;
        }

        // ── 6. Validar saldo del emisor ───────────────────────
        long senderBalance = CPH.GetKickUserVarById<long>(senderId, "boinacoin");

        if (senderBalance < amount)
        {
            CPH.SendKickMessage(
                $"❌ {senderName}, no tienes suficientes Boinacoins. " +
                $"Saldo actual: {senderBalance} 🪙");
            return true;
        }

        // ── 7. Ejecutar transferencia ─────────────────────────
        long senderNew = senderBalance - amount;
        CPH.SetKickUserVarById(senderId, "boinacoin", senderNew, true);

        long receiverBalance = CPH.GetKickUserVar<long>(targetName, "boinacoin");
        long receiverNew     = receiverBalance + amount;
        CPH.SetKickUserVar(targetName, "boinacoin", receiverNew, true);

        // ── 8. Actualizar cooldown del emisor ─────────────────
        CPH.SetKickUserVarById(senderId, "boinacoin_regalar_last", nowUnix, true);

        // ── 9. Estadística histórica del receptor ─────────────
        // (el regalo cuenta como ingreso en el histórico del receptor)
        long receiverTotal = CPH.GetKickUserVar<long>(targetName, "boinacoin_total_earned") + amount;
        CPH.SetKickUserVar(targetName, "boinacoin_total_earned", receiverTotal, true);

        // ── 10. Timestamps antiinactividad ────────────────────
        CPH.SetKickUserVarById(senderId, "boinacoin_last_seen", nowUnix, true);
        CPH.SetKickUserVar(targetName, "boinacoin_last_seen", nowUnix, true);

        // ── 11. Comprobar subida de rango del receptor ────────
        CheckRankUp(targetName, receiverNew);

        // ── 12. Mensaje al chat ───────────────────────────────
        CPH.SendKickMessage(
            $"🎁 {senderName} regala {amount} Boinacoins a {targetName} · " +
            $"{senderName}: {senderNew} 🪙 · {targetName}: {receiverNew} 🪙");

        return true;
    }

    // ── Subida de rango del receptor ──────────────────────────
    private void CheckRankUp(string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVar<int>(userName, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVar(userName, "boinacoin_rank", newRank, true);
        CPH.SendKickMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

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
