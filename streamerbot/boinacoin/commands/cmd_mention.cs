// ============================================================
//  BOINACOIN · commands/cmd_mencion.cs
//  Trigger: Kick → Chat → Message
//            (con filtro: el mensaje contiene "@BoinaBot"
//             Y NO empieza por "!" — ver configuración abajo)
//
//  Lógica de disparo:
//    ✅ "@BoinaBot"                     → trigger
//    ✅ "@BoinaBot eres tonto"          → trigger
//    ✅ "pa tonto el @BoinaBot"         → trigger
//    ✅ "que asco me da el @BoinaBot"   → trigger
//    ❌ "!duelo @BoinaBot 400"          → NO trigger (es comando)
//    ❌ "!boinas @BoinaBot"             → NO trigger (es comando)
//
//  Configuración en Streamer.bot:
//    Acción "Boinacoin · Mención"
//    Trigger: Kick → Chat → Message
//    Criteria (en el trigger):
//      · Message Contains: @BoinaBot   (case-insensitive)
//      · Message Does Not Start With: !
//    Sub-action: Execute C# (este script)
//    Cooldown global: 8 segundos (evita spam de menciones)
//
//  Fuente de frases:
//    https://raw.githubusercontent.com/axljuega/axljuega.github.io
//    /main/data/boinabot_frases.json
//
//  Lógica de selección de frase:
//    1. Extraer palabras clave del mensaje del usuario
//    2. Hacer fuzzy match contra los tags del JSON
//    3. Si hay matches → pool de frases con esos tags
//    4. Si no hay matches → pool genérico completo
//    5. Elegir una frase aleatoria del pool resultante
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

public class CPHInline
{
    private const string JSON_URL =
        "https://raw.githubusercontent.com/axljuega/axljuega.github.io/main/data/boinabot_frases.json";

    // Nombre del bot tal como aparece en Kick (para limpiar la mención del texto)
    private const string BOT_SLUG = "boinabot";

    // Cache del JSON en memoria durante la sesión (global var)
    // Se refresca si lleva más de 10 minutos sin actualizarse
    private const string CACHE_KEY      = "boinabot_frases_cache";
    private const string CACHE_TIME_KEY = "boinabot_frases_cache_time";
    private const int    CACHE_TTL_SECS = 600; // 10 minutos

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";
        string message  = args.ContainsKey("message")  ? args["message"].ToString()  : "";

        // ── 0. Ignorar al propio BoinaBot (evita loop infinito) ─
        if (userName.ToLower() == BOT_SLUG.ToLower()) return false;

        // ── 0.1 Ignorar bots del grupo ────────────────────────
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 0.2 Ignorar si el mensaje empieza por ! ───────────
        // Doble guarda: el trigger de Streamer.bot ya lo filtra,
        // pero lo verificamos aquí por si acaso
        if (message.TrimStart().StartsWith("!")) return true;

        // ── 0.2 Ignorar si BoinaBot no está mencionado ────────
        if (!message.ToLower().Contains("@" + BOT_SLUG) &&
            !message.ToLower().Contains(BOT_SLUG))
            return true;

        CPH.LogInfo($"[Mención] Disparado por {userName}: {message}");

        // ── 1. Obtener JSON (con cache) ───────────────────────
        string jsonRaw = GetJsonCached();
        if (string.IsNullOrEmpty(jsonRaw))
        {
            CPH.LogWarn("[Mención] No se pudo obtener el JSON de frases.");
            CPH.SendKickMessage($"@{userName} 🎩 ...(BoinaBot está pensando y se ha quedado en blanco)");
            return true;
        }

        // ── 2. Parsear frases del JSON ────────────────────────
        List<Frase> frases = ParseFrases(jsonRaw);
        if (frases.Count == 0)
        {
            CPH.LogWarn("[Mención] JSON parseado pero sin frases.");
            return true;
        }

        // ── 3. Extraer palabras del mensaje para fuzzy match ──
        string msgClean = message.ToLower()
            .Replace("@boinabot", "")
            .Replace("boinabot", "");

        List<string> palabras = Regex.Split(msgClean, @"\W+")
            .Where(w => w.Length >= 4) // ignorar palabras muy cortas
            .ToList();

        CPH.LogInfo($"[Mención] Palabras para match: {string.Join(", ", palabras)}");

        // ── 4. Buscar frases con tags que hagan match ─────────
        var pool = new List<Frase>();

        if (palabras.Count > 0)
        {
            pool = frases.Where(f =>
                f.Tags.Any(tag =>
                    palabras.Any(p =>
                        p.Contains(tag) || tag.Contains(p)
                    )
                )
            ).ToList();

            CPH.LogInfo($"[Mención] Frases con match de tags: {pool.Count}");
        }

        // ── 5. Si no hay match, usar pool completo ────────────
        if (pool.Count == 0)
        {
            pool = frases;
            CPH.LogInfo("[Mención] Sin match de tags, usando pool completo.");
        }

        // ── 6. Elegir frase aleatoria ─────────────────────────
        var rng    = new Random();
        var frase  = pool[rng.Next(pool.Count)];
        string msg = frase.Texto;

        // ── 7. Personalizar con nombre del usuario ─────────────
        // Si la frase no menciona explícitamente al usuario,
        // añadimos @usuario al principio para que sea personal
        if (!msg.Contains("%user%") && !msg.ToLower().Contains(userName.ToLower()))
        {
            msg = $"@{userName} " + msg;
        }
        else
        {
            msg = msg.Replace("%user%", userName);
        }

        CPH.LogInfo($"[Mención] Frase seleccionada (tags: [{string.Join(",", frase.Tags)}]): {msg}");
        CPH.SendKickMessage(msg);

        return true;
    }

    // ── Cache del JSON en global var ──────────────────────────
    private string GetJsonCached()
    {
        long now       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long cacheTime = CPH.GetGlobalVar<long>(CACHE_TIME_KEY, false);
        string cached  = CPH.GetGlobalVar<string>(CACHE_KEY, false) ?? "";

        if (!string.IsNullOrEmpty(cached) && (now - cacheTime) < CACHE_TTL_SECS)
        {
            CPH.LogInfo("[Mención] Usando cache del JSON.");
            return cached;
        }

        // Fetch fresco
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                string json = client.GetStringAsync(JSON_URL).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(json))
                {
                    CPH.SetGlobalVar(CACHE_KEY,      json, false);
                    CPH.SetGlobalVar(CACHE_TIME_KEY, now,  false);
                    CPH.LogInfo("[Mención] JSON fetcheado y cacheado.");
                    return json;
                }
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[Mención] Error fetch JSON: {ex.Message}");
            // Si falla el fetch pero hay cache antigua, úsala igualmente
            if (!string.IsNullOrEmpty(cached)) return cached;
        }

        return "";
    }

    // ── Parser JSON minimalista (sin librería externa) ────────
    // Parsea el array "frases" buscando objetos {tags:[...], texto:"..."}
    private List<Frase> ParseFrases(string json)
    {
        var result = new List<Frase>();

        try
        {
            // Extraer el array "frases": [...]
            int frasesStart = json.IndexOf("\"frases\"");
            if (frasesStart < 0) return result;

            int arrStart = json.IndexOf('[', frasesStart);
            if (arrStart < 0) return result;

            // Recorrer objetos dentro del array
            int i = arrStart + 1;
            while (i < json.Length)
            {
                // Buscar inicio de objeto
                int objStart = json.IndexOf('{', i);
                if (objStart < 0) break;

                // Encontrar fin del objeto (respetando anidamiento)
                int objEnd = FindObjectEnd(json, objStart);
                if (objEnd < 0) break;

                string obj = json.Substring(objStart, objEnd - objStart + 1);

                var frase = ParseFraseObj(obj);
                if (frase != null) result.Add(frase);

                i = objEnd + 1;

                // Si llegamos al cierre del array principal, parar
                int nextClose = json.IndexOf(']', i);
                int nextOpen  = json.IndexOf('{', i);
                if (nextClose >= 0 && (nextOpen < 0 || nextClose < nextOpen)) break;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[Mención] Error parseando JSON: {ex.Message}");
        }

        CPH.LogInfo($"[Mención] Frases parseadas: {result.Count}");
        return result;
    }

    private Frase ParseFraseObj(string obj)
    {
        try
        {
            // Extraer tags
            var tags = new List<string>();
            int tagsStart = obj.IndexOf("\"tags\"");
            if (tagsStart >= 0)
            {
                int arrS = obj.IndexOf('[', tagsStart);
                int arrE = obj.IndexOf(']', arrS);
                if (arrS >= 0 && arrE > arrS)
                {
                    string tagsStr = obj.Substring(arrS + 1, arrE - arrS - 1);
                    foreach (Match m in Regex.Matches(tagsStr, "\"([^\"]+)\""))
                        tags.Add(m.Groups[1].Value.ToLower());
                }
            }

            // Extraer texto
            string texto = "";
            int textoIdx = obj.IndexOf("\"texto\"");
            if (textoIdx >= 0)
            {
                int q1 = obj.IndexOf('"', textoIdx + 7);
                if (q1 >= 0)
                {
                    // Buscar el cierre de la cadena respetando escapes
                    int q2 = q1 + 1;
                    while (q2 < obj.Length)
                    {
                        if (obj[q2] == '"' && obj[q2 - 1] != '\\') break;
                        q2++;
                    }
                    texto = obj.Substring(q1 + 1, q2 - q1 - 1)
                               .Replace("\\\"", "\"")
                               .Replace("\\n", "\n")
                               .Replace("\\\\", "\\");
                }
            }

            if (string.IsNullOrEmpty(texto)) return null;
            return new Frase { Tags = tags, Texto = texto };
        }
        catch { return null; }
    }

    private int FindObjectEnd(string json, int start)
    {
        int depth = 0;
        bool inStr = false;
        for (int i = start; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\')) inStr = !inStr;
            if (inStr) continue;
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    // ── Modelo ────────────────────────────────────────────────
    private class Frase
    {
        public List<string> Tags  { get; set; } = new List<string>();
        public string       Texto { get; set; } = "";
    }
}
