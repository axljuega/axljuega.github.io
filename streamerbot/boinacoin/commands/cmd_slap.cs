// ============================================================
//  BOINACOIN · commands/cmd_slap.cs
//  Comando: !slap @usuario
//  Coste: 10 BoinaCoins
//  Permiso: Boina de Lana+ (rank >= 1)
//
//  Mecánica:
//    1. Usuario paga 10 BoinaCoins para dar un bofetón.
//    2. Se elige un objeto aleatorio y una plantilla del JSON.
//    3. Si el objetivo es BoinaBot, hay una respuesta especial.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const long SLAP_COST = 10;
    private const string SLAP_JSON_PATH = "data/slap_frases.json";
    private static readonly Random RND = new Random();

    public bool Execute()
    {
        string userId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Verificar rango mínimo (Boina de Lana+) ───────
        int rank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        if (rank < 1)
        {
            CPH.SendKickMessage($"🔒 @{userName}, necesitas ser 🧶 Boina de Lana para dar bofetones. ¡Ahorra un poco!");
            return true;
        }

        // ── 2. Verificar saldo ───────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");
        if (balance < SLAP_COST)
        {
            CPH.SendKickMessage($"❌ @{userName}, no tienes suficientes BoinaCoins ({SLAP_COST}) para este nivel de violencia gratuita.");
            return true;
        }

        // ── 3. Parsear objetivo ──────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        if (string.IsNullOrEmpty(rawTarget))
        {
            CPH.SendKickMessage($"❌ @{userName}, uso: !slap @usuario");
            return true;
        }

        string targetName = rawTarget.TrimStart('@');
        string targetNameLower = targetName.ToLower();

        // No puedes pegarte a ti mismo
        if (targetNameLower == userName.ToLower())
        {
            CPH.SendKickMessage($"🤨 @{userName}, ¿pegarte a ti mismo? Eso es nuevo. Busca ayuda.");
            return true;
        }

        // ── 4. Lógica de BoinaBot ────────────────────────────
        if (targetNameLower == "boinabot")
        {
            CPH.SendKickMessage($"🤖 @{userName} intenta pegarme... Pero mis reflejos de silicio son superiores. ¡ZAS! Me quedo con tus {SLAP_COST} coins y tú te quedas con la mano doliendo.");
            CPH.SetKickUserVarById(userId, "boinacoin", balance - SLAP_COST, true);
            return true;
        }

        // ── 5. Cargar frases y ejecutar ──────────────────────
        SlapData data = LoadSlapData();
        if (data == null || data.Templates.Count == 0 || data.Items.Count == 0)
        {
            CPH.LogWarn("[SLAP] No se pudo cargar data/slap_frases.json o está vacío.");
            return false;
        }

        string template = data.Templates[RND.Next(data.Templates.Count)];
        string item = data.Items[RND.Next(data.Items.Count)];

        // ── 6. Cobrar y enviar mensaje ───────────────────────
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CPH.SetKickUserVarById(userId, "boinacoin", balance - SLAP_COST, true);
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen", nowUnix, true);

        string message = template
            .Replace("@{attacker}", userName)
            .Replace("@{target}", targetName)
            .Replace("{item}", item);

        CPH.SendKickMessage(message);

        // ── 7. Rank Check ────────────────────────────────────
        CPH.SetArgument("rankUpUserId", userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.RunAction("BoinaCoin · RankChecker", false);

        return true;
    }

    private SlapData LoadSlapData()
    {
        try
        {
            // En Streamer.bot, la ruta base es la del ejecutable.
            // Intentamos leer el archivo.
            string json = File.ReadAllText(SLAP_JSON_PATH);
            return JsonConvert.DeserializeObject<SlapData>(json);
        }
        catch (Exception ex)
        {
            CPH.LogError($"[SLAP] Error cargando JSON: {ex.Message}");
            return null;
        }
    }

    private class SlapData
    {
        [JsonProperty("templates")]
        public List<string> Templates { get; set; } = new List<string>();
        [JsonProperty("items")]
        public List<string> Items { get; set; } = new List<string>();
    }
}
