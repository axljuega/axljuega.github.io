// ============================================================
//  BOINACOIN · commands/cmd_horafeliz.cs
//  Comando: !horafeliz
//  Permiso: SOLO streamer (isOwner / isBroadcaster)
//
//  Activa el multiplicador global x2 durante 30 minutos.
//  Si se llama de nuevo mientras está activa → la desactiva.
//  Si se llama con !horafeliz fin → la desactiva manualmente.
//
//  Variables globales que gestiona:
//    boinacoin_horafeliz         → bool activo/inactivo
//    boinacoin_horafeliz_expiry  → unix timestamp de fin
//
//  IMPORTANTE — Anuncio de fin automático:
//    Este script activa la hora feliz pero NO puede programar
//    un timer de 30 min desde C#. Para el anuncio de fin,
//    crea en Streamer.bot una acción "Boinacoin · HoraFeliz Fin"
//    con un trigger Timer de 1.800 s (one-shot) que llame a
//    este mismo script con Set Argument "mode" = "end".
//    Alternativamente usa el timer watchdog de timed_watchdog.cs
//    (ver sección system/).
//
//  Cómo conectarlo en Streamer.bot:
//    Acción A → trigger "!horafeliz"     → (sin args extra)
//    Acción B → trigger interno/timer    → Set Argument "mode" = "end"
// ============================================================

using System;

public class CPHInline
{
    private const int DURATION_SECS = 1_800; // 30 minutos

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string callerId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string callerName = args.ContainsKey("userName") ? args["userName"].ToString() : "streamer";
        string mode       = args.ContainsKey("mode")     ? args["mode"].ToString()     : "toggle";

        if (mode != "end" && CPH.UserInGroup(callerName, Platform.Kick, "Chat Bots")) return false;

        // ── Rama de fin automático (llamada desde timer) ──────
        if (mode == "end")
        {
            return HandleEnd();
        }

        // ── Verificar permisos (toggle manual) ────────────────
        bool isStreamer    = args.ContainsKey("isOwner")       && (bool)args["isOwner"];
        bool isBroadcaster = args.ContainsKey("isBroadcaster") && (bool)args["isBroadcaster"];

        if (!isStreamer && !isBroadcaster)
        {
            CPH.LogInfo($"[Boinacoin] !horafeliz denegado a {callerName}.");
            return true;
        }

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── ¿Está activa ya? ──────────────────────────────────
        bool   isActive = CPH.GetGlobalVar<bool>("boinacoin_horafeliz",        true);
        long   expiry   = CPH.GetGlobalVar<long>("boinacoin_horafeliz_expiry", true);
        bool   notExpired = nowUnix < expiry;

        // Detectar si el argumento extra es "fin" para forzar desactivación
        string input0 = args.ContainsKey("input0") ? args["input0"].ToString().Trim().ToLower() : "";
        bool   forceEnd = (input0 == "fin" || input0 == "end");

        if ((isActive && notExpired) || forceEnd)
        {
            // ── Desactivar ────────────────────────────────────
            DeactivateHoraFeliz();
            long remaining = Math.Max(0, expiry - nowUnix);
            CPH.SendKickMessage(
                $"⏹️ Hora Feliz desactivada manualmente por {callerName}. " +
                $"Quedaban {remaining / 60} min {remaining % 60}s.");
        }
        else
        {
            // ── Activar ───────────────────────────────────────
            long newExpiry = nowUnix + DURATION_SECS;
            CPH.SetGlobalVar("boinacoin_horafeliz",        true,     true);
            CPH.SetGlobalVar("boinacoin_horafeliz_expiry", newExpiry, true);

            // Calcular hora local aproximada de fin (UTC)
            string endTime = DateTimeOffset.FromUnixTimeSeconds(newExpiry)
                                           .ToString("HH:mm") + " UTC";

            CPH.SendKickMessage(
                $"⚡ ¡¡HORA FELIZ activada por {callerName}!! " +
                $"Todos los Boinacoins x2 durante 30 minutos · " +
                $"Termina a las {endTime} 🎉🎉");
        }

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  Fin automático (llamado desde timer o acción encadenada)
    // ════════════════════════════════════════════════════════
    private bool HandleEnd()
    {
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiry  = CPH.GetGlobalVar<long>("boinacoin_horafeliz_expiry", true);

        // Comprobar que realmente ha expirado
        // (podría haberse desactivado manualmente antes)
        bool stillActive = CPH.GetGlobalVar<bool>("boinacoin_horafeliz", true);

        if (!stillActive)
        {
            CPH.LogInfo("[Boinacoin] HoraFeliz Fin: ya estaba inactiva, nada que hacer.");
            return true;
        }

        if (nowUnix < expiry)
        {
            // Aún no ha expirado (el timer llegó antes)
            CPH.LogInfo($"[Boinacoin] HoraFeliz Fin: todavía activa ({expiry - nowUnix}s restantes).");
            return true;
        }

        DeactivateHoraFeliz();

        CPH.SendKickMessage(
            "⏰ ¡La Hora Feliz ha terminado! " +
            "Los multiplicadores vuelven a la normalidad. " +
            "¡Gracias por estar aquí! 🎩");

        return true;
    }

    // ── Desactiva y limpia las variables globales ─────────────
    private void DeactivateHoraFeliz()
    {
        CPH.SetGlobalVar("boinacoin_horafeliz",        false, true);
        CPH.SetGlobalVar("boinacoin_horafeliz_expiry", 0L,    true);
    }
}
