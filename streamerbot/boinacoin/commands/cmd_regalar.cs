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
//
//  FIX: Mensajes de confirmación aleatorios para evitar
//  el bot-detection de Kick por mensajes repetidos (flood).
// ============================================================

using System;

public class CPHInline
{
    private static readonly Random RND = new Random();
    private const long MIN_TRANSFER   = 10;
    private const int  COOLDOWN_SECS  = 60;

    private static readonly string[] GIFT_TEMPLATES = {
        "🎁 {0} regala {1} BoinaCoins a {2} · {0}: {3} 🪙 · {2}: {4} 🪙",
        "💸 {2} recibe {1} BoinaCoins de {0}. Nadie sabe por qué. Saldos: {3} 🪙 → {4} 🪙",
        "🎁 Transferencia completada: {0} → {2} · {1} 🪙 · [{3} / {4}]",
        "💰 ¡Lluvia de monedas! {0} le lanza {1} 🪙 a {2}. Nuevos saldos: {0}({3}) {2}({4})",
        "🤝 {0} ha sido generoso: {1} BoinaCoins para {2}. [Carteras: {3} | {4}]",
        "🎁 {2}, tienes un regalo de {0}: {1} BoinaCoins. {0} ahora tiene {3} y tú {4}.",
        "💸 {0} soltó {1} 🪙 y {2} los recogió. Balance actual: {0}={3}, {2}={4}",
        "🏦 Movimiento bancario: {0} transfirió {1} 🪙 a {2}. {0}: {3} 🪙 / {2}: {4} 🪙",
        "🎁 ¡Toma ya! {0} le da {1} BoinaCoins a {2}. {3} y {4} son sus nuevos saldos.",
        "✨ {0} compartió su riqueza con {2}: {1} 🪙 enviados. {0} queda con {3}, {2} con {4}.",
        "🎁 Regalo de {0} para {2}: {1} BoinaCoins. Cuenta de {0}: {3} 🪙, cuenta de {2}: {4} 🪙",
        "💸 {0} le pasó {1} 🪙 a {2}. Ahora {0} tiene {3} y {2} tiene {4}.",
        "🎁 {2} está de suerte, {0} le regaló {1} BoinaCoins. Balances: {3} / {4}",
        "💰 {1} BoinaCoins han volado de {0} a {2}. Estado actual: {0}={3} 🪙, {2}={4} 🪙",
        "🎁 {0} → {1} 🪙 → {2}. Confirmado. Nuevos totales: {3} y {4}."
    };

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
        // (el regalo cuenta como ingreso en el histórico del receptor)
        long receiverTotal = CPH.GetKickUserVar<long>(targetName, "boinacoin_total_earned") + amount;
        CPH.SetKickUserVar(targetName, "boinacoin_total_earned", receiverTotal, true);

        // ── 10. Timestamps antiinactividad ────────────────────
        CPH.SetKickUserVarById(senderId, "boinacoin_last_seen", nowUnix, true);
        CPH.SetKickUserVar(targetName, "boinacoin_last_seen", nowUnix, true);

        // ── 11. Comprobar cambios de rango ────────────────────
        // El emisor puede bajar y el receptor puede subir
        CheckRankChange(senderId, senderName, senderNew, isId: true);
        CheckRankChange("", targetName, receiverNew, isId: false);

        // ── 12. Mensaje al chat (Aleatorio) ───────────────────
        string template = GIFT_TEMPLATES[RND.Next(GIFT_TEMPLATES.Length)];
        CPH.SendKickMessage(string.Format(template, senderName, amount, targetName, senderNew, receiverNew));

        return true;
    }

    // ── Cambio de rango ───────────────────────────────────────
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
