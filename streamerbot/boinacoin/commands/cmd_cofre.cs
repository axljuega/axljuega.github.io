// ============================================================
//  BOINACOIN · commands/cmd_cofre.cs
//  Comandos: !cofre  (streamer) /  !abrir  (todos)
//
//  Mecánica:
//    1. Streamer escribe !cofre → bot anuncia cofre en chat
//    2. El primer viewer en escribir !abrir se lleva el premio
//    3. Premio aleatorio entre 500 y 5.000 Boinacoins
//    4. El cofre caduca a los 5 min si nadie lo abre
//    5. Solo 1 cofre activo a la vez · 1 vez por stream
//
//  Variables globales:
//    boinacoin_cofre_active  → bool
//    boinacoin_cofre_prize   → long (generado al activar)
//    boinacoin_cofre_expiry  → unix timestamp de caducidad
//    boinacoin_cofre_claimed → fecha "yyyy-MM-dd" (1/stream)
//
//  Configuración en Streamer.bot — DOS acciones, mismo código:
//    Acción A → trigger "!cofre"  → Set Argument "mode" = "spawn"
//    Acción B → trigger "!abrir"  → Set Argument "mode" = "open"
// ============================================================

using System;

public class CPHInline
{
    private const int  COFRE_TIMEOUT_SECS = 300;  // 5 minutos
    private const long PRIZE_MIN          = 500;
    private const long PRIZE_MAX          = 5_000;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string mode = args.ContainsKey("mode") ? args["mode"].ToString() : "spawn";
        return mode == "open" ? HandleOpen() : HandleSpawn();
    }

    // ════════════════════════════════════════════════════════
    //  RAMA A · !cofre  (streamer activa el cofre)
    // ════════════════════════════════════════════════════════
    private bool HandleSpawn()
    {
        string callerId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string callerName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "streamer";

        // ── Verificar permisos (solo streamer) ────────────────
        bool isStreamer    = args.ContainsKey("isOwner")       && (bool)args["isOwner"];
        bool isBroadcaster = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

        if (!isStreamer && !isBroadcaster)
        {
            CPH.LogInfo($"[Boinacoin] !cofre denegado a {callerName}.");
            return true;
        }

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── Comprobar si ya hay un cofre activo ───────────────
        bool cofreActive  = CPH.GetGlobalVar<bool>("boinacoin_cofre_active",  true);
        long cofreExpiry  = CPH.GetGlobalVar<long>("boinacoin_cofre_expiry",  true);
        bool notExpired   = nowUnix < cofreExpiry;

        if (cofreActive && notExpired)
        {
            long secsLeft = cofreExpiry - nowUnix;
            CPH.SendMessage(
                $"📦 Ya hay un cofre abierto esperando. " +
                $"Caduca en {secsLeft}s. ¡Escribe !abrir!");
            return true;
        }

        // ── Guardia de 1 cofre por stream (día) ──────────────
        string todayDate   = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string claimedDate = CPH.GetGlobalVar<string>("boinacoin_cofre_claimed", true) ?? "";

        if (claimedDate == todayDate)
        {
            CPH.SendMessage("📦 Ya se ha abierto un cofre hoy. ¡Solo 1 por stream!");
            return true;
        }

        // ── Generar premio aleatorio ──────────────────────────
        long prize = new Random().NextInt64(PRIZE_MIN, PRIZE_MAX + 1);

        // ── Activar cofre ─────────────────────────────────────
        long expiry = nowUnix + COFRE_TIMEOUT_SECS;
        CPH.SetGlobalVar("boinacoin_cofre_active", true,    true);
        CPH.SetGlobalVar("boinacoin_cofre_prize",  prize,   true);
        CPH.SetGlobalVar("boinacoin_cofre_expiry", expiry,  true);

        // ── Anuncio en chat ───────────────────────────────────
        CPH.SendMessage(
            $"📦✨ ¡¡HA APARECIDO UN COFRE SECRETO!! " +
            $"El primero en escribir !abrir se llevará entre " +
            $"{PRIZE_MIN} y {PRIZE_MAX} Boinacoins. " +
            $"¡Tienes {COFRE_TIMEOUT_SECS / 60} minutos! ⏳");

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  RAMA B · !abrir  (primer viewer que lo escriba gana)
    // ════════════════════════════════════════════════════════
    private bool HandleOpen()
    {
        string userId   = args.ContainsKey("kickUserId")   ? args["kickUserId"].ToString()   : "";
        string userName = args.ContainsKey("kickUserName") ? args["kickUserName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── Verificar cofre activo ────────────────────────────
        bool cofreActive = CPH.GetGlobalVar<bool>("boinacoin_cofre_active",  true);
        long cofreExpiry = CPH.GetGlobalVar<long>("boinacoin_cofre_expiry",  true);

        if (!cofreActive || nowUnix >= cofreExpiry)
        {
            // Silencioso: !abrir puede spamearse sin cofre activo
            return true;
        }

        long prize = CPH.GetGlobalVar<long>("boinacoin_cofre_prize", true);

        // ── Entregar premio ───────────────────────────────────
        long balance = CPH.GetKickUserVar<long>(userId, "boinacoin") + prize;
        CPH.SetKickUserVar(userId, "boinacoin", balance, true);

        long totalEarned = CPH.GetKickUserVar<long>(userId, "boinacoin_total_earned") + prize;
        CPH.SetKickUserVar(userId, "boinacoin_total_earned", totalEarned, true);

        CPH.SetKickUserVar(userId, "boinacoin_last_seen", nowUnix, true);

        // ── Marcar cofre como reclamado ───────────────────────
        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        CPH.SetGlobalVar("boinacoin_cofre_active",  false,     true);
        CPH.SetGlobalVar("boinacoin_cofre_expiry",  0L,        true);
        CPH.SetGlobalVar("boinacoin_cofre_prize",   0L,        true);
        CPH.SetGlobalVar("boinacoin_cofre_claimed", todayDate, true);

        // ── Comprobar subida de rango ─────────────────────────
        CheckRankUp(userId, userName, balance);

        // ── Anuncio del ganador ───────────────────────────────
        CPH.SendMessage(
            $"🎉 ¡¡{userName} ha abierto el cofre secreto y gana {prize} Boinacoins!! " +
            $"Saldo: {balance} 🪙 🎊");

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
