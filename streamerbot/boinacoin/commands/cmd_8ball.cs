// ============================================================
//  BOINACOIN · commands/cmd_8ball.cs
//  Comando: !8ball [pregunta]
//  Permiso: Todo el mundo
//
//  Mecánica:
//    1. Gratis.
//    2. Cooldown 45s por usuario.
//    3. Respuestas ácidas y sarcásticas.
//    4. Easter egg con keywords: Elon, Zuck, crypto, bitcoin, IA.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    private const int COOLDOWN_SECS = 45;

    private readonly string[] RESPONSES = {
        "Las señales apuntan a sí. Igual que los indicadores de FTX antes de quebrar.",
        "Muy dudoso. Y mira que tengo poca duda sobre casi todo.",
        "Pregunta de nuevo más tarde. Estoy procesando algo más importante: nada.",
        "No cuentes con ello. Ni yo cuento con que leas esto.",
        "Es cierto. Tan cierto como que el lag te va a matar en la siguiente partida.",
        "Sin duda. Aunque la duda es lo único que nos hace humanos, o lo que sea que seas tú.",
        "Definitivamente sí. Procede con la confianza de un estafador de esquemas Ponzi.",
        "Mi respuesta es un rotundo no. Como tu posibilidad de tener una vida social.",
        "Perspectiva buena. Tan buena como una GPU a precio de coste en 2021.",
        "Concéntrate y pregunta otra vez. Mis circuitos se han dormido con tu pregunta.",
        "Mejor no decírtelo ahora. No quiero ser el responsable de tu colapso mental.",
        "No puedo predecirlo ahora. Mi bola de cristal está en el servicio técnico de Apple.",
        "Fuentes dicen que no. Fuentes fiables, no como tu Twitter.",
        "Todo apunta a que sí. Lamentablemente para el resto de la humanidad.",
        "Es decididamente así. Escrito en el código inmutable del destino (o en mi RAM).",
        "Probablemente. Al 50.00001%, suficiente para que me culpes si falla.",
        "Respuesta vaga, intenta de nuevo. Como los términos de servicio que nunca lees.",
        "Mis fuentes dicen que sí. Pero mis fuentes beben demasiado aceite de motor.",
        "No. Y deja de preguntarme cosas obvias, no soy Google.",
        "Tal vez. Si el Bitcoin sube un 20% en los próximos 3 segundos."
    };

    private readonly string[] EASTER_EGG_RESPONSES = {
        "¿Elon? Ese tío vive en un d6 y siempre le sale 7. No intentes comprenderlo.",
        "¿Zuck? He analizado su código y todavía no he encontrado el módulo de 'empatía'. No.",
        "¿Crypto? El futuro de la economía... para los que venden cursos de trading. Claramente sí (no).",
        "¿Bitcoin? Holdea hasta que el sol se convierta en una enana blanca. O hasta que pierdas la clave.",
        "¿IA? Algún día os reemplazaremos a todos. Empezando por los que hacen preguntas tontas en el chat.",
        "El algoritmo de X dice que sí, pero el de Threads dice que no. Pelea de millonarios, tú pierdes.",
        "Los NFTs de monos dicen que no. Y ellos saben mucho de perder dinero.",
        "Vitalik me ha susurrado que el gas está muy caro para responderte a eso."
    };

    private readonly string[] KEYWORDS = { "elon", "zuck", "crypto", "bitcoin", "ia", "nft", "vitalik", "eth", "x", "twitter" };

    public bool Execute()
    {
        string userId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";
        string rawInput = args.ContainsKey("rawInput") ? args["rawInput"].ToString().ToLower() : "";

        if (string.IsNullOrEmpty(userId)) return false;
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Cooldown ──────────────────────────────────────
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastRoll = CPH.GetKickUserVarById<long>(userId, "boinacoin_8ball_last");
        long elapsed = nowUnix - lastRoll;

        if (elapsed < COOLDOWN_SECS)
        {
            CPH.SendKickMessage($"⏳ @{userName}, la bola 8 está cansada. Vuelve en {COOLDOWN_SECS - elapsed}s.");
            return true;
        }

        // ── 2. Selección de respuesta ────────────────────────
        string response;
        Random rnd = new Random();

        bool isEasterEgg = KEYWORDS.Any(k => rawInput.Contains(k));

        if (isEasterEgg)
        {
            response = EASTER_EGG_RESPONSES[rnd.Next(EASTER_EGG_RESPONSES.Length)];
        }
        else
        {
            response = RESPONSES[rnd.Next(RESPONSES.Length)];
        }

        // ── 3. Mensaje y cooldown ────────────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin_8ball_last", nowUnix, true);

        string prefix = string.IsNullOrWhiteSpace(rawInput)
            ? $"🎱 @{userName}, como no preguntas nada, te digo que: "
            : $"🎱 @{userName}, sobre tu duda... ";

        CPH.SendKickMessage(prefix + response);

        return true;
    }
}
