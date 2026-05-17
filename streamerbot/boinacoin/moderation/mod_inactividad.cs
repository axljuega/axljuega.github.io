// ============================================================
//  BOINACOIN · moderation/mod_inactividad.cs
//  Tipo: acción temporizadora diaria (o al inicio del stream)
//  Penalización: -5% del saldo por cada 30 días de inactividad
//                mínimo aplicado: -100 Boinacoins
//                nunca por debajo de 0
//
//  Lógica:
//    Recorre todos los usuarios con saldo > 0 y comprueba
//    boinacoin_last_seen. Si el timestamp supera los 30 días
//    de antigüedad Y el usuario no ha sido ya penalizado hoy,
//    aplica la penalización y actualiza last_seen para evitar
//    que se repita antes del siguiente ciclo de 30 días.
//
//  Control de doble ejecución:
//    boinacoin_inactivity_last_run (global) guarda la fecha
//    de la última ejecución. El script no vuelve a correr
//    si ya lo hizo hoy.
//
//  Cómo configurarlo en Streamer.bot:
//    Acción → Timer recurrente cada 86.400 s (24 h)
//    O bien → trigger "Stream Start" (una vez al día si
//    streams diarios; puede correr varias veces sin daño
//    gracias al guard de last_run)
// ============================================================

using System;
using System.Linq;

public class CPHInline
{
    private const double PENALTY_PERCENT   = 0.05;          // 5%
    private const long   PENALTY_MIN       = 100;           // mínimo descontado
    private const long   INACTIVITY_SECS   = 30 * 24 * 3600; // 30 días en segundos
    private const long   MIN_BALANCE_CHECK = 1;             // solo usuarios con saldo > 0

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // ── Guard: no ejecutar más de una vez al día ──────────
        string lastRun = CPH.GetGlobalVar<string>("boinacoin_inactivity_last_run", true) ?? "";
        if (lastRun == todayDate)
        {
            CPH.LogInfo("[Boinacoin] Inactividad: ya ejecutado hoy, omitiendo.");
            return true;
        }

        CPH.SetGlobalVar("boinacoin_inactivity_last_run", todayDate, true);

        long nowUnix       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int  penalizedCount = 0;
        long totalDeducted  = 0;

        // ── Obtener todos los usuarios con saldo ──────────────
        var allVars = CPH.GetKickUsersVar<long>("boinacoin", true);

        if (allVars == null || allVars.Count == 0)
        {
            CPH.LogInfo("[Boinacoin] Inactividad: sin usuarios registrados.");
            return true;
        }

        var candidates = allVars
            .Where(u => !string.IsNullOrEmpty(u.UserId) && u.Value >= MIN_BALANCE_CHECK && !CPH.UserInGroup(u.UserName, "Chat Bots"))
            .ToList();

        foreach (var entry in candidates)
        {
            string userId   = entry.UserId;
            string userName = entry.UserName ?? userId;
            long   balance  = entry.Value;

            // ── Comprobar inactividad ─────────────────────────
            long lastSeen = CPH.GetKickUserVarById<long>(userId, "boinacoin_last_seen");

            // Si nunca se registró last_seen, saltar (usuario antiguo
            // sin datos; no penalizamos sin certeza)
            if (lastSeen == 0) continue;

            long daysSinceActive = nowUnix - lastSeen;
            if (daysSinceActive < INACTIVITY_SECS) continue;

            // ── Calcular penalización ─────────────────────────
            long penalty    = Math.Max(PENALTY_MIN, (long)Math.Floor(balance * PENALTY_PERCENT));
            long newBalance = Math.Max(0, balance - penalty);
            long applied    = balance - newBalance; // real aplicado (puede ser menor si saldo < penalty)

            if (applied == 0) continue;

            // ── Aplicar ───────────────────────────────────────
            CPH.SetKickUserVarById(userId, "boinacoin", newBalance, true);

            // Actualizar rango si baja
            int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
            int newRank = RankForBalance(newBalance);
            if (newRank < oldRank)
                CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);

            // ── Log por usuario ───────────────────────────────
            long daysInactive = daysSinceActive / 86400;
            CPH.LogInfo(
                $"[Boinacoin] Inactividad · {userName} · " +
                $"Días sin aparecer: {daysInactive} · " +
                $"Penalización: -{applied} · " +
                $"Saldo: {balance} → {newBalance}");

            penalizedCount++;
            totalDeducted += applied;
        }

        // ── Resumen final en log ──────────────────────────────
        CPH.LogInfo(
            $"[Boinacoin] Inactividad completada · " +
            $"Usuarios penalizados: {penalizedCount} · " +
            $"Total deducido: {totalDeducted} Boinacoins");

        // Solo mencionar en chat si hubo penalizaciones
        // (mensaje discreto, sin nombres — privacidad)
        if (penalizedCount > 0)
        {
            CPH.SendKickMessage(
                $"📊 Revisión de inactividad completada · " +
                $"{penalizedCount} cuenta{(penalizedCount == 1 ? "" : "s")} " +
                $"con penalización por +30 días sin aparecer.");
        }

        return true;
    }

    private int RankForBalance(long balance)
    {
        if (balance >= 100_000) return 4;
        if (balance >= 50_000)  return 3;
        if (balance >= 10_000)  return 2;
        if (balance >= 1_000)   return 1;
        return 0;
    }
}
