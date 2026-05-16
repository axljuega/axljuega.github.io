# 🚀 Streamer.bot Integrations & Boinacoin System

Guía exhaustiva para la configuración de **Streamer.bot**, el sistema de economía **Boinacoin**, comandos personalizados como `!slap` y alertas visuales con **OBS Studio** y **n8n**.

---

## 🛠️ Instalación en Linux (Arch/CachyOS)

Para ejecutar Streamer.bot en sistemas basados en Arch Linux (como CachyOS), se recomienda el uso del instalador oficial para Linux que configura el entorno Wine necesario.

1.  **Instalación base:**
    Sigue las instrucciones del [repositorio oficial de instalación en Linux](https://github.com/Streamerbot/sb-linux-installer).
    ```bash
    curl -sS https://raw.githubusercontent.com/Streamerbot/sb-linux-installer/main/install.sh | bash
    ```

2.  **Acceso Directo Personalizado:**
    Crea un archivo `.desktop` en `~/.local/share/applications/streamerbot.desktop` para lanzar la aplicación correctamente con Wine, evitando conflictos con utilidades como MangoHud:

    ```ini
    [Desktop Entry]
    Name=Streamer.bot
    Exec=env DISABLE_MANGOHUD=1 WINEPREFIX=/home/axel/.local/lib/streamer.bot/pfx wine /home/axel/.local/lib/streamer.bot/Streamer.bot.exe
    Comment=Bot for Twitch/Kick Streamers
    GenericName=Chatbot
    Icon=/home/axel/.local/lib/streamer.bot/streamer.bot.png
    Type=Application
    Categories=Network
    Path=/home/axel/.local/lib/streamer.bot
    ```

---

## 🪙 Sistema Boinacoin (Economía Flawless)

El **Boinacoin** es el corazón de la interacción en el canal. Es un sistema de economía persistente que utiliza las variables globales y de usuario de Streamer.bot.

### 📈 Rangos y Progresión
El sistema escala automáticamente según el saldo acumulado:
- **🪡 Boina de Paja:** Rango inicial (0 - 999 🪙).
- **🧶 Boina de Lana:** 1,000 🪙 (Desbloquea !apostar y !duelo).
- **🪡 Boina de Cuero:** 10,000 🪙.
- **💎 Boina de Terciopelo:** 50,000 🪙 (Multiplicador x1.25).
- **👑 La Boina Legendaria:** 100,000 🪙 (Multiplicador x1.5 + VIP).

### 💸 Cómo ganar Boinacoins
1.  **Chat activo:** +5 🪙 por mensaje (cooldown 60s).
2.  **Bonus Diario:** +25 🪙 por el primer mensaje del día.
3.  **Timed Payout:** +15 🪙 cada 10 minutos si has estado activo en el chat (últimos 20 min).
4.  **Multiplicadores:** Se acumulan por rango, rachas de streams (7 y 30 días) y eventos de "Hora Feliz".

### 🎮 Comandos Principales
- `!boinas [@usuario]`: Consulta tu saldo, rango y multiplicadores.
- `!apostar <cantidad>`: Apuesta contra la banca (50/50). Máximo 20% de tu saldo o 5,000 🪙.
- `!duelo @usuario <cantidad>`: Desafía a otro viewer. El retado debe escribir `!aceptar`.
- `!top`: Muestra los usuarios más ricos del canal.

---

## ✋ Comando !slap

El comando `!slap` permite a los usuarios "abofetear" a otros con objetos aleatorios extraídos de nuestra base de datos.

**Uso en Nightbot:**
```text
$(user) ha abofeteado a $(touser) con $(eval a=$(urlfetch json https://axljuega.github.io/data/slap_data.txt);a[Math.floor(Math.random() * a.length)];)
```

**Configuración en Streamer.bot:**
Se puede implementar leyendo el archivo `data/slap_data.txt` localmente y seleccionando una línea aleatoria para enviarla al chat de Kick/Twitch.

---

## 🤖 Integración con n8n (Webhooks)

Para integraciones avanzadas (bases de datos externas, logs en Discord, etc.), utilizamos **n8n**.

### Trigger de Webhook en n8n
Crea un nodo de tipo "Webhook" con el siguiente JSON de configuración:

```json
{
  "parameters": {
    "httpMethod": "POST",
    "path": "9db2c0e6-45cf-49ae-8037-1453dc7617ef",
    "options": {}
  },
  "type": "n8n-nodes-base.webhook",
  "typeVersion": 2.1,
  "id": "f982f5a9-a872-45a5-a926-dd4276bbca57",
  "name": "kick-alerts-confetti"
}
```

### Llamada desde Streamer.bot (C# Inline)
Usa este código para enviar datos a n8n cuando ocurra un evento (ej. Follow):

```csharp
using System;
using System.Text;
using System.Net.Http;

public class CPHInline {
  private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

  public bool Execute() {
    if (!CPH.TryGetArg("user", out string user)) user = "test_user";

    // URL de producción ficticia: https://n8n.tu-dominio.com/webhook/9db2...
    // URL de test: https://n8n.tu-dominio.com/webhook-test/9db2...
    string webhookUrl = "https://n8nzimaafaces.share.zrok.io/webhook-test/9db2c0e6-45cf-49ae-8037-1453dc7617ef";

    string json = $"{{\"user\":\"{user}\",\"type\":\"follow\"}}";
    var payload = new StringContent(json, Encoding.UTF8, "application/json");

    try {
      HttpResponseMessage response = _httpClient.PostAsync(webhookUrl, payload).GetAwaiter().GetResult();
      CPH.LogInfo($"Webhook response: {response.StatusCode}");
      return true;
    } catch (Exception e) {
      CPH.LogError($"Webhook error: {e.Message}");
      return false;
    }
  }
}
```

---

## ✨ Alertas de Confetti (OBS Studio)

El archivo `effects/confetti.html` es una fuente de navegador para OBS que reacciona a eventos de WebSocket.

### Configuración en OBS
1.  Añade una **Fuente de Navegador**.
2.  URL: `https://tu-usuario.github.io/effects/confetti.html?prod`
    - El parámetro `?prod` oculta los botones de prueba.
3.  Ancho: `1920`, Alto: `1080`.

### Activación via CallVendorRequest (OBS Raw)
Streamer.bot puede disparar el confetti enviando un evento personalizado a la fuente de navegador de OBS:

**Ejemplo de Follow:**
```json
{
  "requestType": "CallVendorRequest",
  "requestData": {
    "vendorName": "obs-browser",
    "requestType": "emit_event",
    "requestData": {
      "event_name": "stream-alert",
      "event_data": {
        "type": "follow",
        "user": "%user%",
        "message": "¡Gracias por el follow!"
      }
    }
  }
}
```

**Otros tipos de eventos soportados:**
- `sub`: Suscripción normal.
- `resub`: Resuscripción (usa `%monthsSubscribed%`).
- `giftsub`: Sub regalada (usa `%recipient.userName%`).
- `massgift`: Subs masivas regaladas.
- `kicks`: Donación de Kicks (usa `%kicks.amount%`).

---

## 🗄️ ¿Base de Datos o Variables Internas?

- **Sin Base de Datos:** Streamer.bot guarda automáticamente las variables de usuario en su archivo `users.json`. Es rápido, no requiere mantenimiento y es suficiente para el sistema Boinacoin.
- **Con n8n / DB Externa:** Si quieres mostrar un ranking en una web externa o persistir datos fuera de Streamer.bot, usa la integración de n8n explicada arriba para enviar cada transacción a una base de datos SQL o Google Sheets.
