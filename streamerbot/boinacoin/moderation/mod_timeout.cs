// ============================================================
//  BOINACOIN · moderation/mod_timeout.cs
//  Evento: usuario recibe timeout de un moderador en Kick
//  Penalización: -500 Boinacoins (nunca por debajo de 0)
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · User Banned" con duración > 0
//    (Kick usa el mismo evento para timeout y ban; filtra por
//     la presencia del campo "duration" para distinguirlos.
//     Los bans permanentes van a mod_ban.cs)
//
//  Filtro recomendado en la acción:
//    Condición: args["duration"] existe Y args["duration"] > 0
//    Así solo se dispara en timeouts, no en bans permanentes.
// ============================================================

using System;

public class CPHInline
{
    private const long PENALTY = 500;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        // En Kick, el evento de ban/timeout incluye los datos
        // del usuario penalizado (no del moderador que lo hizo)
        string userId   = args.ContainsKey("targetUserId")   ? args["targetUserId"].ToString()
                        : args.ContainsKey("userId")         ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("targetUserName") ? args["targetUserName"].ToString()
                        : args.ContainsKey("userName")       ? args["userName"].ToString() : "alguien";

        // Duración del timeout (segundos) — informativo
        string durationStr = args.ContainsKey("duration") ? args["duration"].ToString() : "?";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 1. Leer saldo actual ──────────────────────────────
        long currentBalance = CPH.GetUserVar<long>(userId, "boinacoin", true);

        if (currentBalance <= 0)
        {
            // Sin saldo que penalizar — solo log
            CPH.LogInfo($"[Boinacoin] Timeout {userName}: saldo ya era 0, sin penalización.");
            return true;
        }

        // ── 2. Aplicar penalización (mínimo 0) ────────────────
        long penaltyApplied = Math.Min(PENALTY, currentBalance);
        long newBalance     = currentBalance - penaltyApplied;

        CPH.SetUserVar(userId, "boinacoin", newBalance, true);

        // ── 3. Comprobar bajada de rango ──────────────────────
        CheckRankDown(userId, userName, newBalance);

        // ── 4. Log interno ────────────────────────────────────
        CPH.LogInfo(
            $"[Boinacoin] Timeout · {userName} · " +
            $"Duración: {durationStr}s · " +
            $"Penalización: -{penaltyApplied} · " +
            $"Saldo: {currentBalance} → {newBalance}");

        // ── 5. Mensaje al chat ────────────────────────────────
        // Breve y directo — no queremos darle más protagonismo
        // del necesario al usuario penalizado.
        CPH.SendMessage(
            $"⚠️ {userName} ha sido silenciado · " +
            $"-{penaltyApplied} Boinacoins · Saldo: {newBalance} 🪙");

        return true;
    }

    // ── Gestiona bajada de rango por penalización ─────────────
    private void CheckRankDown(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetUserVar<int>(userId, "boinacoin_rank", true);
        int newRank = RankForBalance(balance);

        if (newRank >= oldRank) return;

        CPH.SetUserVar(userId, "boinacoin_rank", newRank, true);
        CPH.LogInfo(
            $"[Boinacoin] {userName} baja de rango: " +
            $"{GetRankName(oldRank)} → {GetRankName(newRank)}");

        // Bajada de rango por penalización: log pero NO anuncio
        // público en chat. No queremos hacer espectáculo de ello.
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
            case 1: return "Boina de Lana";
            case 2: return "Boina de Cuero";
            case 3: return "Boina de Terciopelo";
            case 4: return "La Boina Legendaria";
            default: return "Boina de Paja";
        }
    }
}
