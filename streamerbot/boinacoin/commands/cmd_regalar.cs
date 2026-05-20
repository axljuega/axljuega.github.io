// ============================================================
//  BOINACOIN · commands/cmd_regalar.cs
//  Comando: !regalar @usuario cantidad
//  Permiso: todos
//
//  Validaciones:
//    · Cantidad mínima: 10 BoinaCoins
//    · Saldo suficiente en el emisor
//    · No regalarse a uno mismo
//    · Cooldown: 60 segundos por usuario emisor
//    · Receptor debe existir en la base de datos local
// ============================================================

using System;

public class CPHInline
{
    private const long MIN_TRANSFER   = 10;
    private const int  COOLDOWN_SECS  = 60;
    private static readonly Random RND = new Random();

    private static readonly string[] GIFT_TEMPLATES = {
        "🎁 {sender} acaba de soltar {amount} Boinacoins encima de {receiver}. Saldos: {senderBal} 🪙 / {receiverBal} 🪙",
        "💸 {receiver} recibe {amount} Boinacoins de {sender}. Nadie sabe por qué. [{senderBal} 🪙 → {receiverBal} 🪙]",
        "🎁 Transferencia aceptada a regañadientes: {sender} → {receiver} · {amount} 🪙 · Saldos: {senderBal} / {receiverBal}",
        "💸 {sender} se siente generoso (o cometió un error) y le da {amount} 🪙 a {receiver}. [Saldos: {senderBal} / {receiverBal}]",
        "🎁 ¡Lluvia de monedas! {sender} lanzó {amount} 🪙 a {receiver}. Ahora tienen {senderBal} y {receiverBal} respectivamente.",
        "💸 Registro contable: {sender} traspasó {amount} Boinacoins a {receiver}. Mi base de datos bosteza. ({senderBal} / {receiverBal})",
        "🎁 {receiver}, hoy es tu día de suerte. {sender} te regaló {amount} 🪙. No lo gastes todo en una sola apuesta. Saldos: {senderBal} / {receiverBal}",
        "💸 {sender} ha reducido su saldo en {amount} 🪙 para dárselo a {receiver}. Qué altruismo más innecesario. [{senderBal} | {receiverBal}]",
        "🎁 {amount} Boinacoins han volado del bolsillo de {sender} al de {receiver}. Magia financiera. {senderBal} 🪙 / {receiverBal} 🪙",
        "💸 {receiver} ahora es {amount} 🪙 más rico gracias a {sender}. La envidia me corroe (es mentira). [{senderBal} → {receiverBal}]",
        "🎁 Confirmado: {sender} → {receiver} por {amount} BoinaCoins. Saldos actualizados: {senderBal} y {receiverBal}.",
        "💸 {sender} se deshace de {amount} 🪙. {receiver} los recoge del suelo. Qué espectáculo. Saldos: {senderBal} / {receiverBal}",
        "🎁 {receiver} recibe un paquete de {sender} con {amount} Boinacoins. No tiene bomba, tranquilo. [Saldos: {senderBal} / {receiverBal}]",
        "💸 Transacción completada: {sender} donó {amount} 🪙 a {receiver}. Mi procesador se siente más ligero. ({senderBal} / {receiverBal})",
        "🎁 {sender} le pasa el sobre a {receiver}: {amount} 🪙. Circulen, aquí no hay nada que ver. Saldos: {senderBal} | {receiverBal}"
    };

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
            CPH.SendKickMessage($"❌ {senderName}, la cantidad mínima para regalar es {MIN_TRANSFER} BoinaCoins.");
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
            CPH.SendKickMessage("⚠️ Los bots del sistema no pueden participar en la economía BoinaCoin.");
            return true;
        }

        // ── 5. No regalarse a uno mismo ───────────────────────
        if (targetName.ToLower() == senderName.ToLower())
        {
            CPH.SendKickMessage($"😅 {senderName}, no puedes regalarte BoinaCoins a ti mismo.");
            return true;
        }

        // ── 6. Validar saldo del emisor ───────────────────────
        long senderBalance = CPH.GetKickUserVarById<long>(senderId, "boinacoin");

        if (senderBalance < amount)
        {
            CPH.SendKickMessage(
                $"❌ {senderName}, no tienes suficientes BoinaCoins. " +
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
        long receiverTotal = CPH.GetKickUserVar<long>(targetName, "boinacoin_total_earned") + amount;
        CPH.SetKickUserVar(targetName, "boinacoin_total_earned", receiverTotal, true);

        // ── 10. Timestamps antiinactividad ────────────────────
        CPH.SetKickUserVarById(senderId, "boinacoin_last_seen", nowUnix, true);
        CPH.SetKickUserVar(targetName, "boinacoin_last_seen", nowUnix, true);

        // ── 11. Comprobar cambios de rango ────────────────────
        CheckRankChange(senderId, senderName, senderNew, isId: true);
        CheckRankChange("", targetName, receiverNew, isId: false);

        // ── 12. Mensaje al chat (Aleatorio) ───────────────────
        string template = GIFT_TEMPLATES[RND.Next(GIFT_TEMPLATES.Length)];
        string finalMsg = template
            .Replace("{sender}", "@" + senderName)
            .Replace("{receiver}", "@" + targetName)
            .Replace("{amount}", amount.ToString())
            .Replace("{senderBal}", senderNew.ToString())
            .Replace("{receiverBal}", receiverNew.ToString());

        CPH.SendKickMessage(finalMsg);

        return true;
    }

    private void CheckRankChange(string userId, string userName, long balance, bool isId)
    {
        int oldRank = isId
            ? CPH.GetKickUserVarById<int>(userId, "boinacoin_rank")
            : CPH.GetKickUserVar<int>(userName, "boinacoin_rank");

        int newRank = RankForBalance(balance);

        if (newRank == oldRank) return;

        if (isId)
            CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);
        else
            CPH.SetKickUserVar(userName, "boinacoin_rank", newRank, true);

        if (isId) CPH.SetArgument("rankUpUserId", userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("BoinaCoin · RankChecker", false);
    }

    private int RankForBalance(long balance)
    {
        if (balance >= 100000) return 4;
        if (balance >= 50000)  return 3;
        if (balance >= 10000)  return 2;
        if (balance >= 1000)   return 1;
        return 0;
    }
}
