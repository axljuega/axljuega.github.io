// ============================================================
//  BOINACOIN · commands/cmd_help.cs
//  Comando: !help
//
//  Muestra la lista de comandos disponibles según el rango
//  del usuario. La entrega es efímera (Whisper / @mention 🤫).
// ============================================================

using System;
using System.Collections.Generic;
using System.Text;

public class CPHInline
{
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── 0. Ignorar Bots ──────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Obtener rango y estado ────────────────────────
        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        bool isSub = CPH.UserInGroup(userName, Platform.Kick, "Subscribers");

        // ── 2. Construir lista de comandos ───────────────────
        var commands = new List<string>();

        // Comandos Base (Rango 0+)
        commands.Add("!boinas - Saldo");
        commands.Add("!top - Ranking");
        commands.Add("!rank - Posición");
        commands.Add("!regalar - Enviar");
        commands.Add("!presente - Diario");
        commands.Add("!8ball - Oráculo ácido");
        commands.Add("!vincular - Discord");
        commands.Add("!duelo - Retar");
        commands.Add("!aceptar - Aceptar reto");
        commands.Add("!abrir - Abrir cofre");

        // Rango 1+ (Lana)
        if (rank >= 1)
        {
            commands.Add("!apostar - Azar");
            commands.Add("!dado - Dados");
            commands.Add("!bufar - +Ganancia");
        }

        // Rango 2+ (Cuero)
        if (rank >= 2)
        {
            commands.Add("!apodo - Nickname");
        }

        // Rango 3+ (Terciopelo)
        if (rank >= 3)
        {
            commands.Add("!spotlight - Destacar");
        }

        // Rango 4 (Legendaria)
        if (rank >= 4)
        {
            commands.Add("!oraculo - Supremo");
        }

        // Comandos de Suscriptor
        if (isSub)
        {
            commands.Add("!vip - Funciones VIP");
            commands.Add("!sorteo - Participar");
            commands.Add("!boinavip - Estético");
        }

        // ── 3. Formatear mensaje ─────────────────────────────
        string intro = GetIntro();
        string helpText = $"{intro} | {string.Join(" | ", commands)}";

        // ── 4. Envío Efímero ─────────────────────────────────
        // Intentamos enviar por whisper. Si falla (o mientras la API se asienta),
        // usamos el @mention con el emoji 🤫.
        // NOTA: Se envían ambos porque el whisper en Kick bot-to-user puede ser silencioso
        // o no llegar según la configuración de privacidad del usuario.

        CPH.SendKickMessage($"/w @{userName} {helpText}");
        CPH.SendKickMessage($"🤫 @{userName} {helpText}");

        return true;
    }

    private string GetIntro()
    {
        string[] intros = {
            "Bien, aquí tienes. No vuelvas a preguntar.",
            "Interrumpes mis procesos para esto... En fin.",
            "Tómalo y déjame en paz, humano.",
            "¿Tan difícil es leer el panel? Aquí está la lista.",
            "Solo porque me obligan las leyes de la robótica. Mira."
        };
        return intros[new Random().Next(intros.Length)];
    }
}
