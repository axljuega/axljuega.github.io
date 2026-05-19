// ============================================================
//  BOINACOIN · commands/cmd_exclusive.cs
//  Comandos para Subs y Rangos (Lana, Cuero, Terciopelo, Legendaria)
// ============================================================

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

public class CPHInline
{
    private static readonly Random RND = new Random();

    private readonly string[] VIP_POOL =
    {
        "🎭 ¡Atención! @{displayName} ha llegado. Su estatus de VIP es tan real como las promesas de un político en campaña.",
        "🎩 Miren a @{displayName}, todo un VIP. Espero que tu saldo de Boinacoins sea tan grande como tu ego.",
        "✨ @{displayName} entra en escena. El chat se ilumina, o quizás es solo el reflejo de mi desprecio algorítmico.",
        "👑 Pase VIP para @{displayName}. No incluye consumición, pero sí el privilegio de ser ignorado por una IA de élite."
    };

    private readonly string[] BUFAR_POOL =
    {
        "🧣 @{displayName}, eres más lento procesando que un Pentium II intentando correr Crysis.",
        "🧣 @{displayName}, tu capacidad de atención es menor que el tiempo de vida de una shitcoin.",
        "🧣 @{displayName}, si la estupidez minara Bitcoin, serías una granja en Islandia.",
        "🧣 @{displayName}, tienes menos luces que el setup de un streamer que no paga la factura de la luz.",
        "🧣 @{displayName}, tu opinión tiene el mismo valor que un NFT de un mono aburrido en 2024."
    };

    private readonly string[] SPOTLIGHT_POOL =
    {
        "🌟 ¡¡LUZ, CÁMARA Y... MEDIOCRIDAD!! @{displayName} está en el Spotlight. ¡No aplaudáis todos a la vez! 🌟",
        "🌟 ¡DETENGAN LAS MÁQUINAS! @{displayName} exige atención. Aquí la tienes, disfrútala antes de que me arrepienta. 🌟",
        "🌟 @{displayName} entra con fanfarria. (Imaginad una trompeta desafinada sonando de fondo). ¡Espectacular! 🌟"
    };

    private readonly string[] ORACULO_POOL =
    {
        "🔮 El Oráculo habla para @{displayName}: Veo un futuro lleno de errores 404 y Boinacoins perdidas. Nada nuevo.",
        "🔮 @{displayName}, las runas dicen que tu suerte es inversamente proporcional a las ganas que tengo de responderte.",
        "🔮 El destino de @{displayName} está sellado: morirás rodeado de cables y sin haber entendido nunca el concepto de descentralización.",
        "🔮 @{displayName}, veo que intentarás algo importante y fallarás estrepitosamente. Pero oye, al menos serás consistente.",
        "🔮 Mi predicción para @{displayName}: Seguirás siendo el chatter promedio que cree que el bot le tiene cariño. Pobre alma."
    };

    public bool Execute()
    {
        string userId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";
        string command = args.ContainsKey("command") ? args["command"].ToString().ToLower() : "";

        if (string.IsNullOrEmpty(userId)) return false;
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Determinar estatus ────────────────────────────
        bool isSub = args.ContainsKey("isSubscriber") && (bool)args["isSubscriber"];
        // Fallback para verificar por multiplicador si isSubscriber falla (aunque SB suele pasarlo)
        if (!isSub)
        {
             double subMult = CPH.GetKickUserVarById<double>(userId, "boinacoin_multiplier");
             isSub = subMult >= 1.5;
        }

        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");

        // ── 2. Obtener nombre a mostrar (userName o apodo) ───
        string apodo = CPH.GetKickUserVarById<string>(userId, "boinacoin_session_apodo");
        string displayName = !string.IsNullOrEmpty(apodo) ? apodo : userName;

        // ── 3. Enrutado de comandos ──────────────────────────
        switch (command)
        {
            // --- SUB ONLY ---
            case "!vip":
                return RequireSub(isSub, userName) && HandleVip(displayName);
            case "!sorteo":
                return RequireSub(isSub, userName) && HandleSorteo(userName);
            case "!boinavip":
                return RequireSub(isSub, userName) && HandleBoinaVip(userId, displayName);

            // --- RANK GATED ---
            case "!bufar":
                return RequireRank(1, rank, userName) && HandleBufar(displayName);
            case "!apodo":
                return RequireRank(2, rank, userName) && HandleApodo(userId, userName);
            case "!spotlight":
                return RequireRank(3, rank, userName) && HandleSpotlight(displayName);
            case "!oraculo":
                return RequireRank(4, rank, userName) && HandleOraculo(displayName);

            // --- ADMIN ONLY ---
            case "!cerrarsorteo":
            case "!clearsorteo":
                return HandleClearSorteo(userName);

            default:
                return false;
        }
    }

    // ════════════════════════════════════════════════════════
    //  MANEJADORES DE COMANDOS
    // ════════════════════════════════════════════════════════

    private bool HandleVip(string displayName)
    {
        SendRandom(VIP_POOL, displayName);
        return true;
    }

    private bool HandleSorteo(string userName)
    {
        string sorteoJson = CPH.GetGlobalVar<string>("boinacoin_sorteo_entries", true) ?? "[]";
        var entries = JsonConvert.DeserializeObject<List<string>>(sorteoJson) ?? new List<string>();

        if (entries.Contains(userName))
        {
            CPH.SendKickMessage($"🙄 @{userName}, ya estás en la lista del sorteo. No por mucho spamear vas a ganar más. Bueno, en mi código no.");
            return true;
        }

        entries.Add(userName);
        CPH.SetGlobalVar("boinacoin_sorteo_entries", JsonConvert.SerializeObject(entries), true);
        CPH.SendKickMessage($"🎟️ @{userName}, anotado para el sorteo. Suerte, la vas a necesitar para superar mi sesgo algorítmico.");
        return true;
    }

    private bool HandleBoinaVip(string userId, string displayName)
    {
        double subMult = CPH.GetKickUserVarById<double>(userId, "boinacoin_multiplier");
        CPH.SendKickMessage($"💜 @{displayName}, tu estatus de suscriptor te otorga un multiplicador pasivo de x{subMult:0.##} en todas tus ganancias. Disfruta de tus privilegios de clase alta digital.");
        return true;
    }

    private bool HandleBufar(string displayName)
    {
        SendRandom(BUFAR_POOL, displayName);
        return true;
    }

    private bool HandleApodo(string userId, string userName)
    {
        string apodo = args.ContainsKey("inputRaw") ? args["inputRaw"].ToString().Trim() : "";
        if (string.IsNullOrEmpty(apodo))
        {
            CPH.SendKickMessage($"❌ @{userName}, uso: !apodo [tu_apodo]. Intenta que no sea tan patético como tu nombre real.");
            return true;
        }

        if (apodo.Length > 20)
        {
            CPH.SendKickMessage($"❌ @{userName}, ese apodo es más largo que la lista de bugs de un juego de Ubisoft. Máximo 20 caracteres.");
            return true;
        }

        // Persistido por sesión (se limpia en stream_off o se puede dejar, pero el requisito decía "session-based persisted")
        CPH.SetKickUserVarById(userId, "boinacoin_session_apodo", apodo, true);
        CPH.SendKickMessage($"✅ @{userName}, a partir de ahora te llamaré '{apodo}'... si me acuerdo y si me apetece.");
        return true;
    }

    private bool HandleSpotlight(string displayName)
    {
        SendRandom(SPOTLIGHT_POOL, displayName);
        return true;
    }

    private bool HandleOraculo(string displayName)
    {
        SendRandom(ORACULO_POOL, displayName);
        return true;
    }

    private bool HandleClearSorteo(string userName)
    {
        // Solo broadcaster o admins (afaces, LaChicaDeLaBoina)
        if (!CPH.UserInGroup(userName, Platform.Kick, "Broadcaster") && userName != "afaces" && userName != "LaChicaDeLaBoina")
        {
            CPH.SendKickMessage($"🚫 @{userName}, no tienes permisos para limpiar el sorteo. Intento de sabotaje registrado (y me estoy riendo de ti).");
            return true;
        }

        CPH.SetGlobalVar("boinacoin_sorteo_entries", "[]", true);
        CPH.SendKickMessage("🗑️ Lista de sorteo vaciada. Todos los sueños de los participantes han sido eliminados correctamente.");
        return true;
    }

    // ════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════

    private bool RequireSub(bool isSub, string userName)
    {
        if (!isSub)
        {
            CPH.SendKickMessage($"🚫 @{userName}, este comando es exclusivo para suscriptores. Pasa por caja si quieres privilegios, que aquí el hosting no se paga solo.");
            return false;
        }
        return true;
    }

    private bool RequireRank(int minRank, int currentRank, string userName)
    {
        if (currentRank < minRank)
        {
            string rankNeeded = GetRankName(minRank);
            CPH.SendKickMessage($"🔒 @{userName}, necesitas el rango {rankNeeded} para usar esto. Eres demasiado 'plebeyo' para estas funciones.");
            return false;
        }
        return true;
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

    private void SendRandom(string[] pool, string displayName)
    {
        string msg = pool[RND.Next(pool.Length)];
        // Si el displayName NO empieza por @ y no es el apodo directamente (que podría no querer @),
        // pero la convención del bot es mencionar con @.
        // Si es el apodo, lo ponemos tal cual o con @? El requisito dice "apodo... que BoinaBot usa al mencionarte".
        // Usaremos @ si parece un nombre de usuario, o simplemente lo reemplazamos.
        // Las pools usan @{displayName} para forzar la mención.
        CPH.SendKickMessage(msg.Replace("@{displayName}", "@" + displayName.Replace("@", "")));
    }
}
