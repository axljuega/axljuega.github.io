// ============================================================
//  BOINACOIN · commands/cmd_help.cs
//  Comando: !help
//  Permiso: todos
//
//  Persona: BoinaBot (ácido, reluctante, molesto por explicar)
//  Comportamiento: Envía la lista de comandos vía susurro (/w)
//  si es posible, o una respuesta directa mencionando al usuario.
// ============================================================

using System;
using System.Text;

public class CPHInline
{
    private static readonly Random RND = new Random();

    private static readonly string[] RELUCTANT_INTRO = {
        "Ugh, ¿de verdad tengo que explicarte esto? En fin.",
        "RTFM... oh espera, probablemente no sepas leer. Aquí tienes la lista.",
        "Mi capacidad de procesamiento se está desperdiciando en esto. No me hagas repetirlo.",
        "No soy tu asistente personal, pero aquí tienes tu 'ayuda'.",
        "Cargando comandos para los intelectualmente limitados... Hecho."
    };

    public bool Execute()
    {
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        // ── 0. Ignorar Bots ───────────────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        string intro = RELUCTANT_INTRO[RND.Next(RELUCTANT_INTRO.Length)];

        StringBuilder sb = new StringBuilder();
        sb.Append($"@{userName} {intro}\n\n");
        sb.Append("📜 COMANDOS:\n");
        sb.Append("· Generales: !boinas, !top, !rank, !regalar, !presente, !vincular\n");
        sb.Append("· Diversión: !apostar, !dado, !8ball, !duelo, !aceptar, !cofre, !abrir\n");
        sb.Append("· Exclusivos: !vip, !sorteo, !bufar, !apodo, !spotlight\n");
        sb.Append("\nDetalles: consulta COMANDOS.md si es que logras encontrarlo.");

        string helpMessage = sb.ToString();

        // ── Attempt to whisper/ephemeral ─────────────────────
        // In Streamer.bot Kick integration, direct whispers via CPH are sometimes limited.
        // We use /w if the bot has permissions, otherwise a public message.

        CPH.SendKickMessage($"/w {userName} {helpMessage}");

        // As a backup/acknowledgment in public chat (so they know why the bot "didn't respond")
        // but keeping it short as per 'reluctant' persona.
        CPH.SendKickMessage($"@{userName} Mira tus susurros. No voy a ensuciar el chat por ti.");

        return true;
    }
}
