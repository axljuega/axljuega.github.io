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
// ============================================================

using System;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    private const long   REWARD_MESSAGE    = 5;
    private const long   REWARD_DAILY_CHAT = 25;
    private const int    COOLDOWN_SECONDS  = 60;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── 1. Cooldown de 60 segundos ───────────────────────
        long lastMessageTime = CPH.GetUserVar<long>(userId, "boinacoin_chat_last", true);
        bool onCooldown      = (nowUnix - lastMessageTime) < COOLDOWN_SECONDS;

        if (onCooldown)
        {
            // Aunque no gane puntos, actualizamos la actividad
            // para que timed_payout.cs sepa que sigue en chat.
            UpdateChatActivity(userId, nowUnix);
            return true;
        }

        // ── 2. Registrar timestamp del mensaje ───────────────
        CPH.SetUserVar(userId, "boinacoin_chat_last", nowUnix, true);
        UpdateChatActivity(userId, nowUnix);

        // ── 3. Bonus primer mensaje del día ──────────────────
        long dailyBonus   = 0;
        string todayDate  = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastChatDay = CPH.GetUserVar<string>(userId, "boinacoin_chat_day", true) ?? "";

        if (lastChatDay != todayDate)
        {
            dailyBonus = REWARD_DAILY_CHAT;
            CPH.SetUserVar(userId, "boinacoin_chat_day", todayDate, true);
        }

        // ── 4. Calcular recompensa total ─────────────────────
        long   baseReward = REWARD_MESSAGE + dailyBonus;
        double mult       = GetMultiplier(userId);
        long   earned     = (long)Math.Floor(baseReward * mult);

        // ── 5. Actualizar saldo ──────────────────────────────
        long balance = CPH.GetUserVar<long>(userId, "boinacoin", true) + earned;
        CPH.SetUserVar(userId, "boinacoin", balance, true);

        // ── 6. Estadística histórica ─────────────────────────
        long totalEarned = CPH.GetUserVar<long>(userId, "boinacoin_total_earned", true) + earned;
        CPH.SetUserVar(userId, "boinacoin_total_earned", totalEarned, true);

        // ── 7. Timestamp antiinactividad ─────────────────────
        CPH.SetUserVar(userId, "boinacoin_last_seen", nowUnix, true);

        // ── 8. Comprobar subida de rango ─────────────────────
        CheckRankUp(userId, userName, balance);

        // ── 9. Mensaje de bienvenida solo en el bonus diario ─
        // No spameamos el chat en cada mensaje, solo cuando hay
        // algo especial que destacar (el bonus del primer mensaje).
        if (dailyBonus > 0)
        {
            string multText = mult > 1.0 ? $" (x{mult:0.##} ⚡)" : "";
            CPH.SendMessage(
                $"👋 ¡Buen día, {userName}! " +
                $"+{earned} Boinacoins por tu primer mensaje de hoy{multText} · " +
                $"Saldo: {balance} 🪙");
        }

        return true;
    }

    // ── Actualiza el timestamp de actividad en chat ───────────
    // timed_payout.cs lo leerá para decidir si el usuario
    // merece el pago pasivo (+15 cada 10 min).
    // Condición: último mensaje hace menos de 20 min (1.200 s).
    private void UpdateChatActivity(string userId, long nowUnix)
    {
        CPH.SetUserVar(userId, "boinacoin_chat_active", nowUnix, true);
    }

    // ── Multiplicador total activo ────────────────────────────
    private double GetMultiplier(string userId)
    {
        double m = 1.0;

        double subMult = CPH.GetUserVar<double>(userId, "boinacoin_multiplier", true);
        if (subMult > 1.0) m *= subMult;

        bool horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);
        if (horaFeliz) m *= 2.0;

        int streak = CPH.GetUserVar<int>(userId, "boinacoin_streak", true);
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        int rank = CPH.GetUserVar<int>(userId, "boinacoin_rank", true);
        if      (rank == 4) m *= 1.5;
        else if (rank == 3) m *= 1.25;

        return m;
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetUserVar<int>(userId, "boinacoin_rank", true);
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetUserVar(userId, "boinacoin_rank", newRank, true);
        CPH.SendMessage($"🎉 ¡{userName} sube a {RankName(newRank)}!");

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
