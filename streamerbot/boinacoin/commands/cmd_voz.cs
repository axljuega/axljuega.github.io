using System;
using System.IO;
using System.Media;
using System.Net;
using System.Text;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        string mensaje;
        if (!CPH.TryGetArg("rawInput", out mensaje) ||
            string.IsNullOrWhiteSpace(mensaje)) return false;

        mensaje = mensaje.Trim();
        if (mensaje.Length > 180) return false;
        try
        {
            using (var cliente = new WebClient())
            {
                cliente.Encoding = Encoding.UTF8;
                cliente.Headers[HttpRequestHeader.ContentType] = "application/json";
                var json = JsonConvert.SerializeObject(new { text = mensaje });
                var wav = cliente.UploadData("http://127.0.0.1:5000/synthesize", "POST",
                                             Encoding.UTF8.GetBytes(json));
                using (var flujo = new MemoryStream(wav))
                using (var voz = new SoundPlayer(flujo))
                {
                    voz.Load();
                    voz.PlaySync();
                }
            }
            return true;
        }
        catch (Exception error)
        {
            CPH.LogError("[BoinaVoz] " + error.Message);
            return false;
        }
    }
}
