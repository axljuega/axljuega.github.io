// ============================================================
//  BOINACOIN · commands/cmd_cofre.cs
//  Comandos: !cofre (streamer)  /  !abrir (todos)
//
//  Mecánica:
//    1. Streamer escribe !cofre → se activa un cofre por 5 min.
//    2. El primer viewer que escriba !abrir gana el premio.
//    3. Premio aleatorio: 500 a 5.000 Boinacoins.
//    4. Máximo 1 cofre por stream (día).
//
//  FIX 1: Random.NextInt64() no existe en el runtime de
//         Streamer.bot (pre-.NET 6). Se reemplaza por un
//         cálculo equivalente con Next(int, int).
//
//  FIX 2: args["isBroadcaster"] NO está disponible en los
//         Kick Command Triggers. Se usa KickGetBroadcaster().UserId
//         igual que en cmd_resetboinas.cs.
//
//  Configuración en Streamer.bot:
//    Acción A → trigger "!cofre" → Set Argument "mode" = "spawn"
//    Acción B → trigger "!abrir" → Set Argument "mode" = "open"
// ============================================================

using System;

public class CPHInline
{
    private const long PRIZE_MIN          = 500;
    private const long PRIZE_MAX          = 5_000;
    private const int  COFRE_TIMEOUT_SECS = 300; // 5 minutos

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        string mode = args.ContainsKey("mode") ? args["mode"].ToString() : "spawn";

        return mode == "spawn" ? HandleSpawn() : HandleOpen();
    }

    // ════════════════════════════════════════════════════════
    //  RAMA A · !cofre (solo broadcaster)
    // ════════════════════════════════════════════════════════
    private bool HandleSpawn()
    {
        string callerId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string callerName = args.ContainsKey("userName") ? args["userName"].ToString() : "streamer";

        if (string.IsNullOrEmpty(callerId)) return false;

        // ── Verificar que es el broadcaster ──────────────────
        // FIX: args["isBroadcaster"] no se inyecta en Kick Command
        //      Triggers. Comparación directa con UserId.
        var  broadcasterInfo = CPH.KickGetBroadcaster();
        bool isBroadcaster   = broadcasterInfo != null &&
                               callerId == broadcasterInfo.UserId.ToString();

        if (!isBroadcaster)
        {
            CPH.LogInfo($"[Boinacoin] !cofre denegado a {callerName} (no es broadcaster).");
            return true;
        }

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── Comprobar si ya hay un cofre activo ───────────────
        bool cofreActive = CPH.GetGlobalVar<bool>("boinacoin_cofre_active", true);
        long cofreExpiry = CPH.GetGlobalVar<long>("boinacoin_cofre_expiry", true);

        if (cofreActive && nowUnix < cofreExpiry)
        {
            long secsLeft = cofreExpiry - nowUnix;
            CPH.SendKickMessage(
                $"📦 Ya hay un cofre abierto esperando. " +
                $"Caduca en {secsLeft}s. ¡Escribe !abrir!");
            return true;
        }

        // ── Guardia de 1 cofre por stream (día) ──────────────
        string todayDate   = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string claimedDate = CPH.GetGlobalVar<string>("boinacoin_cofre_claimed", true) ?? "";

        if (claimedDate == todayDate)
        {
            CPH.SendKickMessage("📦 Ya se ha abierto un cofre hoy. ¡Solo 1 por stream!");
            return true;
        }

        // ── Generar premio aleatorio ──────────────────────────
        // FIX: Random.NextInt64() no existe pre-.NET 6.
        //      Usamos Next(int, int) con cast explícito a long.
        long prize = (long)new Random().Next((int)PRIZE_MIN, (int)PRIZE_MAX + 1);

        // ── Activar cofre ─────────────────────────────────────
        long expiry = nowUnix + COFRE_TIMEOUT_SECS;
        CPH.SetGlobalVar("boinacoin_cofre_active", true,   true);
        CPH.SetGlobalVar("boinacoin_cofre_prize",  prize,  true);
        CPH.SetGlobalVar("boinacoin_cofre_expiry", expiry, true);

        // ── Anuncio en chat ───────────────────────────────────
        CPH.SendKickMessage(
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
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── Verificar cofre activo ────────────────────────────
        bool cofreActive = CPH.GetGlobalVar<bool>("boinacoin_cofre_active", true);
        long cofreExpiry = CPH.GetGlobalVar<long>("boinacoin_cofre_expiry", true);

        if (!cofreActive || nowUnix >= cofreExpiry)
        {
            // Silencioso: !abrir puede spamearse sin cofre activo
            return true;
        }

        long prize = CPH.GetGlobalVar<long>("boinacoin_cofre_prize", true);

        // ── Entregar premio ───────────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin") + prize;
        CPH.SetKickUserVarById(userId, "boinacoin", balance, true);

        long totalEarned = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + prize;
        CPH.SetKickUserVarById(userId, "boinacoin_total_earned", totalEarned, true);

        CPH.SetKickUserVarById(userId, "boinacoin_last_seen", nowUnix, true);

        // ── Marcar cofre como reclamado ───────────────────────
        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        CPH.SetGlobalVar("boinacoin_cofre_active",  false,     true);
        CPH.SetGlobalVar("boinacoin_cofre_expiry",  0L,        true);
        CPH.SetGlobalVar("boinacoin_cofre_prize",   0L,        true);
        CPH.SetGlobalVar("boinacoin_cofre_claimed", todayDate, true);

        // ── Comprobar subida de rango ─────────────────────────
        CheckRankUp(userId, userName, balance);

        // ── Anuncio del ganador ───────────────────────────────
        CPH.SendKickMessage(
            $"🎉 ¡¡{userName} ha abierto el cofre secreto y gana {prize} Boinacoins!! " +
            $"Saldo: {balance} 🪙 🎊");

        return true;
    }

    // ── Subida de rango ───────────────────────────────────────
    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);
        CPH.SendKickMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

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
