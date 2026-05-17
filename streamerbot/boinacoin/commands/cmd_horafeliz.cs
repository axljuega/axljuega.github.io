// ============================================================
//  BOINACOIN · commands/cmd_horafeliz.cs
//  Comando: !horafeliz
//  Permiso: activar/desactivar → SOLO broadcaster
//           consultar estado   → todos
//
//  Activa el multiplicador global x2 durante 30 minutos.
//  Si se llama de nuevo mientras está activa → la desactiva.
//  Si se llama con !horafeliz fin → la desactiva manualmente.
//  Si lo usa alguien sin permisos → informa del estado actual.
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
        CPH.TryGetArg("userId",   out string callerId);
        CPH.TryGetArg("userName", out string callerName);
        CPH.TryGetArg("mode",     out string mode);
        CPH.TryGetArg("userType", out string userType);

        if (string.IsNullOrEmpty(callerName)) callerName = "streamer";
        if (string.IsNullOrEmpty(mode))       mode       = "toggle";

        // ── Excluir bots del grupo "Chat Bots" ───────────────
        if (mode != "end")
        {
            Enum.TryParse(userType, out Platform platform);
            if (CPH.UserInGroup(callerName, platform, "Chat Bots")) return false;
        }

        // ── Rama de fin automático (llamada desde timer) ──────
        if (mode == "end")
        {
            return HandleEnd();
        }

        // ── Verificar permisos ────────────────────────────────
        bool isStreamer = userType == "broadcaster" || userType == "moderator";

        if (!isStreamer)
        {
            // Informar del estado actual con humor seco
            return SendStatusMessage(callerName);
        }

        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // ── ¿Está activa ya? ──────────────────────────────────
        bool isActive   = CPH.GetGlobalVar<bool>("boinacoin_horafeliz",        true);
        long expiry     = CPH.GetGlobalVar<long>("boinacoin_horafeliz_expiry", true);
        bool notExpired = nowUnix < expiry;

        CPH.TryGetArg("input0", out string input0Raw);
        string input0   = (input0Raw ?? "").Trim().ToLower();
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
            CPH.SetGlobalVar("boinacoin_horafeliz",        true,      true);
            CPH.SetGlobalVar("boinacoin_horafeliz_expiry", newExpiry, true);

            string endTime = DateTimeOffset.FromUnixTimeSeconds(newExpiry)
                                           .ToString("HH:mm") + " UTC";

            CPH.SendKickMessage(
                $"⚡ ¡¡HORA FELIZ activada!! " +
                $"Todos los Boinacoins x2 durante 30 minutos · " +
                $"Termina a las {endTime} 🎉🎉");
        }

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  Estado actual para usuarios sin permisos (dry humor)
    // ════════════════════════════════════════════════════════
    private bool SendStatusMessage(string callerName)
    {
        long nowUnix    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool isActive   = CPH.GetGlobalVar<bool>("boinacoin_horafeliz",        true);
        long expiry     = CPH.GetGlobalVar<long>("boinacoin_horafeliz_expiry", true);
        bool notExpired = nowUnix < expiry;

        if (isActive && notExpired)
        {
            long remaining = expiry - nowUnix;
            long mins      = remaining / 60;
            long secs      = remaining % 60;
            CPH.SendKickMessage(
                $"⚡ Hora Feliz ACTIVA · x2 en todos los Boinacoins · " +
                $"quedan {mins} min {secs}s · " +
                $"tú no la has activado, {callerName}, pero disfrútala igual 🎩");
        }
        else
        {
            CPH.SendKickMessage(
                $"💤 Hora Feliz inactiva, {callerName} · " +
                $"aquí nadie manda todavía · " +
                $"paciencia, que ya llegará ⌛");
        }

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  Fin automático (llamado desde timer o acción encadenada)
    // ════════════════════════════════════════════════════════
    private bool HandleEnd()
    {
        long nowUnix     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiry      = CPH.GetGlobalVar<long>("boinacoin_horafeliz_expiry", true);
        bool stillActive = CPH.GetGlobalVar<bool>("boinacoin_horafeliz",        true);

        if (!stillActive)
        {
            CPH.LogInfo("[Boinacoin] HoraFeliz Fin: ya estaba inactiva, nada que hacer.");
            return true;
        }

        if (nowUnix < expiry)
        {
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
