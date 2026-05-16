// ============================================================
//  BOINACOIN · earn/chat_message.cs
//  Evento: mensaje en el chat de Kick
//
//  Lógica en tres capas:
//    1. Primer mensaje del día  → +25 (bonus diario)
//    2. Mensaje normal          → +5  (cooldown 60s)
//    3. Actualiza timestamp de actividad para timed_payout.cs
//       (el pago pasivo de +15 cada 10 min solo se da si el
//        usuario ha chateado en los últimos 20 min)
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Message"
//    Añadir filtro: excluir grupo "Bots"
//
//  Arg keys confirmadas via dump (KickChatMessage / CommandTriggered):
//    userId   → ID numérico del sender (String)
//    userName → login name del sender  (String)
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const long REWARD_MESSAGE    = 5;
    private const long REWARD_DAILY_CHAT = 25;
    private const int  COOLDOWN_SECONDS  = 60;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // FIX: arg keys correctas (sin prefijo "kick")
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── 1. Cooldown de 60 segundos ───────────────────────
        // FIX: GetKickUserVarById (userId numérico, no userName)
        long lastMessageTime = CPH.GetKickUserVarById<long>(userId, "boinacoin_chat_last");
        bool onCooldown      = (nowUnix - lastMessageTime) < COOLDOWN_SECONDS;

        if (onCooldown)
        {
            UpdateChatActivity(userId, nowUnix);
            return true;
        }

        // ── 2. Registrar timestamp del mensaje ───────────────
        // FIX: SetKickUserVarById
        CPH.SetKickUserVarById(userId, "boinacoin_chat_last", nowUnix, true);
        UpdateChatActivity(userId, nowUnix);

        // ── 3. Bonus primer mensaje del día ──────────────────
        long   dailyBonus  = 0;
        string todayDate   = DateTime.UtcNow.ToString("yyyy-MM-dd");
        // FIX: GetKickUserVarById
        string lastChatDay = CPH.GetKickUserVarById<string>(userId, "boinacoin_chat_day") ?? "";

        if (lastChatDay != todayDate)
        {
            dailyBonus = REWARD_DAILY_CHAT;
            CPH.SetKickUserVarById(userId, "boinacoin_chat_day", todayDate, true);
        }

        // ── 4. Calcular recompensa total ─────────────────────
        long   baseReward = REWARD_MESSAGE + dailyBonus;
        double mult       = GetMultiplier(userId);
        long   earned     = (long)Math.Floor(baseReward * mult);

        // ── 5. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        // ── 6. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + earned;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 7. Timestamp antiinactividad ─────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen", nowUnix, true);

        // ── 8. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 9. Mensaje de bienvenida solo en el bonus diario ─
        if (dailyBonus > 0)
        {
            string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
            // FIX: SendKickMessage (no SendMessage genérico)
            CPH.SendKickMessage(
                $"👋 ¡Buen día, {userName}! " +
                $"+{earned} Boinacoins por tu primer mensaje de hoy{multText} · " +
                $"Saldo: {balance} 🪙");
        }

        return true;
    }

    // ── Actualiza el timestamp de actividad en chat ───────────
    private void UpdateChatActivity(string userId, long nowUnix)
    {
        CPH.SetKickUserVarById(userId, "boinacoin_chat_active", nowUnix, true);
    }

    // ── Multiplicador total activo ────────────────────────────
    private double GetMultiplier(string userId)
    {
        double m = 1.0;

        double subMult = CPH.GetKickUserVarById<double>(userId, "boinacoin_multiplier");
        if (subMult > 1.0) m *= subMult;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m;
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);
        // FIX: SendKickMessage
        CPH.SendKickMessage($"🎉 ¡{userName} sube a {RankName(newRank)}!");

        CPH.SetArgument("rankUpUserId",   userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank",  newRank);
        CPH.RunAction("Boinacoin · RankChecker", false);
    }

    private int RankForBalance(long balance)
    {
        if (balance >= RANK_LEGENDARIA) return 4;
        if (balance >= RANK_TERCIOPELO) return 3;
        if (balance >= RANK_CUERO)      return 2;
        if (balance >= RANK_LANA)       return 1;
        return 0;
    }

    private string RankName(int rank)
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
