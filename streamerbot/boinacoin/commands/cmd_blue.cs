// ============================================================
//  BOINACOIN · commands/cmd_blue.cs
//  Comando: !blue
//  Coste: Gratis
//  Permiso: Todos
//
//  Mecánica:
//    Devuelve una frase aleatoria de la asulita desde un JSON.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class CPHInline
{
    private const string BLUE_JSON_PATH = "data/blue_frases.json";
    private static readonly Random RND = new Random();

    public bool Execute()
    {
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        BlueData data = LoadBlueData();
        if (data == null || data.Frases.Count == 0)
        {
            CPH.LogWarn("[BLUE] No se pudo cargar data/blue_frases.json o está vacío.");
            return false;
        }

        string frase = data.Frases[RND.Next(data.Frases.Count)];

        CPH.SendKickMessage($"💙 @{userName}, la asulita dice: {frase}");

        return true;
    }

    private BlueData LoadBlueData()
    {
        try
        {
            string json = File.ReadAllText(BLUE_JSON_PATH);
            return JsonConvert.DeserializeObject<BlueData>(json);
        }
        catch (Exception ex)
        {
            CPH.LogError($"[BLUE] Error cargando JSON: {ex.Message}");
            return null;
        }
    }

    private class BlueData
    {
        [JsonProperty("frases")]
        public List<string> Frases { get; set; } = new List<string>();
    }
}
