# 🎩 Ecosistema Boinacoin — Guía completa DIY

> **Filosofía:** Este sistema está diseñado para que lo entiendas, lo modifiques y lo hagas tuyo. No hay magia negra. Cada pieza hace una cosa concreta y se conecta con las demás de forma explícita.

---

## 📐 Arquitectura general — ¿Qué hace qué?

Antes de instalar nada, entiende el mapa completo:

```
┌─────────────┐     eventos      ┌─────────────────┐
│  Kick.com   │ ──────────────► │  Streamer.bot   │
│  (el canal) │                  │  (el cerebro)   │
└─────────────┘                  └────────┬────────┘
                                          │
              ┌───────────────────────────┼────────────────────────┐
              │                           │                        │
              ▼                           ▼                        ▼
     ┌────────────────┐        ┌──────────────────┐    ┌──────────────────┐
     │   OBS Studio   │        │      n8n          │    │    Discord       │
     │  (las alertas) │        │  (automatización) │    │  (la comunidad)  │
     └────────────────┘        └──────────────────┘    └──────────────────┘
                                          │
                              ┌───────────┴───────────┐
                              ▼                       ▼
                     ┌──────────────┐      ┌──────────────────┐
                     │  Base datos  │      │  Web pública     │
                     │  (opcional)  │      │  (ranking, etc.) │
                     └──────────────┘      └──────────────────┘
```

**Streamer.bot es el hub central.** Recibe eventos de Kick, ejecuta los scripts C# de Boinacoin, habla directamente con OBS, y opcionalmente llama a n8n para tareas externas. n8n **no habla con OBS** — eso es trabajo de Streamer.bot.

---

## 🛣️ Tres caminos de configuración

Elige el nivel que necesitas. Puedes empezar por el 1 y escalar después.

### 🟢 Nivel 1 — Solo Streamer.bot (recomendado para empezar)
**Qué tienes:** Boinacoin completo + alertas en OBS + comandos en Kick.  
**Qué no tienes:** Ranking en web externa, logs en base de datos externa.  
**Dificultad:** Baja. Todo en una sola aplicación.

```
Kick → Streamer.bot → OBS
                    → Chat de Kick (comandos y mensajes)
                    → Discord (via webhook nativo)
```

### 🟡 Nivel 2 — Streamer.bot + n8n
**Qué añades:** Automatizaciones externas, logs en Google Sheets, notificaciones avanzadas en Discord con embeds personalizados, integraciones con cualquier API externa.  
**Dificultad:** Media. Necesitas tener n8n corriendo (self-hosted o cloud).

```
Kick → Streamer.bot → OBS               (Streamer.bot habla con OBS, siempre)
                    → n8n webhook → Discord con embed rico
                                 → Google Sheets (log de eventos)
                                 → Cualquier API externa
```

### 🔴 Nivel 3 — Streamer.bot + n8n + Base de datos externa
**Qué añades:** Ranking en tiempo real en una web, persistencia externa de Boinacoins (útil si quieres migrar de Streamer.bot o tener backup), APIs propias.  
**Dificultad:** Alta. Requiere conocimientos de bases de datos y hosting.

```
Kick → Streamer.bot → OBS
                    → n8n → PostgreSQL / MySQL / Supabase
                           → API propia (ranking web, etc.)
```

---

## 🛠️ Instalación de Streamer.bot

### En Windows
Descarga el instalador desde [streamer.bot](https://streamer.bot) y ejecútalo. Sin más.

### En Linux (Arch / CachyOS)
Streamer.bot corre bajo Wine. Usa el instalador oficial:

```bash
curl -sS https://raw.githubusercontent.com/Streamerbot/sb-linux-installer/main/install.sh | bash
```

Crea un acceso directo en `~/.local/share/applications/streamerbot.desktop`:

```ini
[Desktop Entry]
Name=Streamer.bot
Exec=env DISABLE_MANGOHUD=1 WINEPREFIX=/home/TU_USUARIO/.local/lib/streamer.bot/pfx wine /home/TU_USUARIO/.local/lib/streamer.bot/Streamer.bot.exe
Comment=Bot para Kick/Twitch
GenericName=Chatbot
Icon=/home/TU_USUARIO/.local/lib/streamer.bot/streamer.bot.png
Type=Application
Categories=Network
Path=/home/TU_USUARIO/.local/lib/streamer.bot
```

Sustituye `TU_USUARIO` por tu nombre de usuario real.

---

## 🔌 Conectar Streamer.bot con Kick

1. Abre Streamer.bot → pestaña **Platforms** → **Kick**
2. Haz clic en **Connect** e inicia sesión con tu cuenta de streamer
3. En **Bot Account**, conecta la cuenta del bot (puede ser la misma o una cuenta separada)
4. Activa los eventos que quieras recibir: Follow, Subscribe, Chat Message, etc.

> **Cuenta de bot:** Lo ideal es tener una cuenta de Kick separada para el bot (por ejemplo `BoiiaBot`) para que los mensajes en el chat vengan de esa cuenta y no de la tuya.

---

## 🪙 Instalar el sistema Boinacoin en Streamer.bot

### Paso 1 — Crear las acciones

Cada script C# del repositorio corresponde a una **Action** en Streamer.bot. Para cada script:

1. Ve a **Actions** → botón `+`
2. Ponle nombre (ej. `Boinacoin · Follow`)
3. En la columna de la derecha, haz clic en `+` → **Core** → **C# Execute Code**
4. Pega el contenido del script `.cs` correspondiente
5. Haz clic en **Compile** — debe decir "Compiled Successfully"

### Paso 2 — Asignar triggers

Una vez creada la acción, asígnale el trigger de Kick correspondiente:

| Script | Trigger en Streamer.bot |
|--------|------------------------|
| `earn/follow.cs` | Kick → Follow |
| `earn/sub.cs` | Kick → Subscribe |
| `earn/resub.cs` | Kick → Re-Subscribe |
| `earn/giftsub.cs` | Kick → Gift Subscription |
| `earn/massgift.cs` | Kick → Gift Subscriptions (plural) |
| `earn/kicks.cs` | Kick → Gifts Leaderboard Updated |
| `earn/chat_message.cs` | Kick → Chat Message |
| `earn/timed_payout.cs` | Timer → cada 600 segundos |
| `earn/presente.cs` | Kick → Chat Command `!presente` |
| `moderation/mod_timeout.cs` | Kick → User Banned (filtro: duration > 0) |
| `moderation/mod_ban.cs` | Kick → User Banned (filtro: duration == 0) |
| `moderation/mod_inactividad.cs` | Timer → cada 86.400 s (o Stream Start) |

### Paso 3 — Acciones de comandos

Para los comandos de chat:

| Script | Trigger |
|--------|---------|
| `commands/cmd_boinas.cs` | Chat Command `!boinas` |
| `commands/cmd_top.cs` | Chat Command `!top` (añade cooldown 30s) |
| `commands/cmd_rank.cs` | Chat Command `!rank` |
| `commands/cmd_regalar.cs` | Chat Command `!regalar` |
| `commands/cmd_apostar.cs` | Chat Command `!apostar` |
| `commands/cmd_horafeliz.cs` | Chat Command `!horafeliz` |
| `commands/cmd_cofre.cs` (spawn) | Chat Command `!cofre` + Set Arg `mode=spawn` |
| `commands/cmd_cofre.cs` (open) | Chat Command `!abrir` + Set Arg `mode=open` |
| `commands/cmd_duelo.cs` (reto) | Chat Command `!duelo` + Set Arg `mode=challenge` |
| `commands/cmd_duelo.cs` (acepta) | Chat Command `!aceptar` + Set Arg `mode=accept` |
| `commands/cmd_addboinas.cs` | Chat Command `!addboinas` |
| `commands/cmd_setboinas.cs` | Chat Command `!setboinas` |
| `commands/cmd_resetboinas.cs` | Chat Command `!resetboinas` |

### Paso 4 — Acciones del sistema (sin trigger directo)

Estas acciones son llamadas internamente por los otros scripts. Solo necesitan existir con el nombre exacto:

| Script | Nombre de la acción |
|--------|-------------------|
| `system/rank_checker.cs` | `Boinacoin · RankChecker` |
| `system/discord_webhook.cs` | `Boinacoin · DiscordWebhook` |
| `system/multiplier.cs` | `Boinacoin · Multiplier` |

> ⚠️ El nombre debe coincidir **exactamente** con la cadena en `CPH.RunAction("Boinacoin · RankChecker", false)`.

### Paso 5 — Excluir bots del ranking

En Streamer.bot → **Users** → busca las cuentas de bots → asígnalas al grupo **Bots**. Los scripts de top/ranking filtran automáticamente usuarios sin saldo, pero esto previene que bots acumulen puntos.

---

## 📺 Conectar OBS Studio con Streamer.bot

> **Regla fundamental:** Streamer.bot habla con OBS. n8n no habla con OBS. El protocolo OBS WebSocket (puerto 4455) requiere una conexión WebSocket real — no acepta peticiones HTTP simples.

### Configurar OBS WebSocket

1. En OBS: **Herramientas** → **WebSocket Server Settings**
2. Activa "Enable WebSocket server"
3. Puerto: `4455` (por defecto)
4. Anota la contraseña si activas autenticación

### Conectar Streamer.bot a OBS

1. Streamer.bot → **Stream Apps** → **OBS Studio** → `+`
2. Host: `localhost` (o la IP de OBS si es otra máquina)
3. Puerto: `4455`
4. Contraseña: la que pusiste en OBS
5. Haz clic en **Connect**

### Añadir alertas visuales en OBS

1. En OBS, añade una **Fuente de Navegador** (Browser Source)
2. URL: `https://tu-usuario.github.io/effects/confetti.html`
   - Añade `?prod` al final para ocultar botones de debug: `.../confetti.html?prod`
3. Ancho: `1920`, Alto: `1080`
4. Dale un nombre reconocible, por ejemplo `Crisol`

### Disparar alertas desde Streamer.bot (sin n8n)

En la misma acción de `earn/follow.cs` (o la que quieras), añade un sub-action:

1. `+` → **OBS** → **Raw Request**
2. Pega este JSON (sustituye `Crisol` por el nombre de tu fuente):

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
        "user": "%userName%",
        "message": "¡Gracias por el follow!"
      }
    }
  }
}
```

Streamer.bot reemplaza `%userName%` automáticamente con el valor del argumento del evento. Los tipos de alerta disponibles son `follow`, `sub`, `resub`, `giftsub`, `massgift`, `kicks`.

---

## 🤖 n8n — Para qué sirve y cuándo lo necesitas

### ¿Qué es n8n?

n8n es una herramienta de automatización de flujos de trabajo visual, similar a Zapier o Make pero self-hosted y gratis. En nuestro ecosistema lo usamos para tareas que Streamer.bot no puede hacer bien por sí solo:

- Guardar eventos en una base de datos externa
- Enviar mensajes a Discord con formato rico (embeds con imágenes, campos, colores)
- Publicar en Google Sheets un log de todas las transacciones
- Exponer un ranking en una web pública en tiempo real
- Integrar con cualquier API externa (Spotify, Twitter, etc.)

### ¿Cuándo NO necesitas n8n?

- Si solo quieres el sistema Boinacoin funcionando en Kick → **no lo necesitas**
- Si las alertas de OBS son suficiente → **no lo necesitas**
- Si los webhooks de Discord nativos de Streamer.bot te valen → **no lo necesitas**

### Instalar n8n (self-hosted)

La forma más sencilla es con Docker:

```bash
docker run -it --rm \
  --name n8n \
  -p 5678:5678 \
  -v ~/.n8n:/home/node/.n8n \
  docker.n8n.io/n8nio/n8n
```

Accede en `http://localhost:5678`. Para exponerlo públicamente (necesario para que Streamer.bot le lleguen webhooks desde fuera de tu red), usa un túnel como [ngrok](https://ngrok.com) o [zrok](https://zrok.io):

```bash
# Con zrok (gratuito, open source)
zrok share public http://localhost:5678
```

---

## 🔗 Flujo de trabajo n8n — Ejemplo correcto

### El error 426 y por qué ocurre

El workflow original intentaba hacer un `POST HTTP` directamente a OBS en el puerto 4455. Esto **nunca puede funcionar** porque:

- OBS WebSocket usa el protocolo `ws://` (WebSocket), no `http://`
- Cuando n8n manda un HTTP POST, el servidor de OBS responde `426 Upgrade Required` — es su forma de decir "aquí hay un WebSocket, usa el protocolo correcto"
- n8n no tiene un nodo nativo de cliente WebSocket para enviar mensajes

### La arquitectura correcta

```
Kick evento
    │
    ▼
Streamer.bot ──► OBS (via WebSocket nativo — esto funciona siempre)
    │
    └──► n8n webhook (solo para tareas externas)
              │
              ▼
         Discord / Google Sheets / Base de datos
```

### Workflow de ejemplo — Log de subs en Discord

Este workflow recibe una notificación de Streamer.bot cuando alguien se suscribe y la reenvía a Discord con un embed rico.

Importa este JSON en n8n (**Workflows → Import from JSON**):

```json
{
  "nodes": [
    {
      "parameters": {
        "httpMethod": "POST",
        "path": "boinacoin-events",
        "options": {
          "responseMode": "responseNode"
        }
      },
      "type": "n8n-nodes-base.webhook",
      "typeVersion": 2.1,
      "position": [0, 0],
      "id": "webhook-boinacoin",
      "name": "Boinacoin Events"
    },
    {
      "parameters": {
        "conditions": {
          "string": [
            {
              "value1": "={{$json.body.type}}",
              "value2": "sub"
            }
          ]
        }
      },
      "type": "n8n-nodes-base.if",
      "typeVersion": 1,
      "position": [220, 0],
      "id": "filter-subs",
      "name": "¿Es una sub?"
    },
    {
      "parameters": {
        "method": "POST",
        "url": "TU_WEBHOOK_URL_DE_DISCORD",
        "sendBody": true,
        "specifyBody": "json",
        "jsonBody": "={\n  \"embeds\": [{\n    \"title\": \"💜 ¡Nueva suscripción!\",\n    \"description\": \"**{{$json.body.user}}** acaba de suscribirse al canal.\",\n    \"color\": 10181046,\n    \"fields\": [\n      {\"name\": \"Boinacoins ganados\", \"value\": \"5.000 🪙\", \"inline\": true},\n      {\"name\": \"Multiplicador\", \"value\": \"x1.5 activo\", \"inline\": true}\n    ],\n    \"timestamp\": \"{{new Date().toISOString()}}\"\n  }]\n}",
        "options": {}
      },
      "type": "n8n-nodes-base.httpRequest",
      "typeVersion": 4.3,
      "position": [440, -100],
      "id": "discord-notify",
      "name": "Notificar Discord"
    },
    {
      "parameters": {
        "respondWith": "text",
        "responseBody": "ok"
      },
      "type": "n8n-nodes-base.respondToWebhook",
      "typeVersion": 1,
      "position": [440, 100],
      "id": "respond-ok",
      "name": "Responder OK"
    }
  ],
  "connections": {
    "Boinacoin Events": {
      "main": [[{"node": "¿Es una sub?", "type": "main", "index": 0}]]
    },
    "¿Es una sub?": {
      "main": [
        [{"node": "Notificar Discord", "type": "main", "index": 0}],
        [{"node": "Responder OK", "type": "main", "index": 0}]
      ]
    }
  }
}
```

Sustituye `TU_WEBHOOK_URL_DE_DISCORD` por la URL de tu webhook de Discord (Discord → tu servidor → Configuración de canal → Integraciones → Webhooks).

### Llamar a este webhook desde Streamer.bot

En la acción `earn/sub.cs` de Streamer.bot, añade un sub-action tras el C# execute:

1. `+` → **Network** → **Fetch URL**
2. URL: `https://TU-DOMINIO.n8n.io/webhook/boinacoin-events`
3. Method: `POST`
4. Body:
```json
{
  "type": "sub",
  "user": "%userName%",
  "userId": "%userId%"
}
```

---

## 🗄️ ¿Base de datos interna o externa?

### Base de datos interna de Streamer.bot (Nivel 1)

Streamer.bot guarda todas las variables de usuario automáticamente en su archivo `users.json` local. Es la opción más simple y suficiente para el 90% de los casos:

- ✅ Sin configuración adicional
- ✅ Muy rápido (lectura/escritura en memoria)
- ✅ Backup manual: copia `users.json`
- ❌ No accesible desde fuera de Streamer.bot
- ❌ Sin ranking en web pública
- ❌ Si reinstales Streamer.bot sin hacer backup, pierdes los datos

**Backup automático recomendado:** Crea una tarea en cron (Linux) o Programador de tareas (Windows) que copie `users.json` a una carpeta de backup cada noche.

### Base de datos externa con n8n (Nivel 3)

Si quieres persistencia externa o una web de ranking, usa n8n como intermediario entre Streamer.bot y tu base de datos.

**Opción recomendada: Supabase** (PostgreSQL en la nube, gratuito hasta 500MB)

1. Crea una cuenta en [supabase.com](https://supabase.com)
2. Crea un proyecto y una tabla:

```sql
CREATE TABLE boinacoin_transactions (
  id          SERIAL PRIMARY KEY,
  user_id     TEXT NOT NULL,
  user_name   TEXT NOT NULL,
  event_type  TEXT NOT NULL,          -- 'follow', 'sub', 'chat', etc.
  amount      BIGINT NOT NULL,
  balance_after BIGINT,
  created_at  TIMESTAMP DEFAULT NOW()
);

CREATE TABLE boinacoin_users (
  user_id     TEXT PRIMARY KEY,
  user_name   TEXT NOT NULL,
  balance     BIGINT DEFAULT 0,
  rank        INTEGER DEFAULT 0,
  streak      INTEGER DEFAULT 0,
  total_earned BIGINT DEFAULT 0,
  last_seen   TIMESTAMP,
  updated_at  TIMESTAMP DEFAULT NOW()
);
```

3. En n8n, añade el nodo **Supabase** (o usa HTTP Request a la API REST de Supabase) para insertar cada transacción.

4. En Streamer.bot, tras cada operación de Boinacoin, llama al webhook de n8n con los datos del evento.

**Workflow de n8n para log en Supabase:**

El webhook recibe `{type, user, userId, amount, balanceAfter}` y lo inserta en la tabla. No es más complicado que el workflow de Discord — solo cambia el nodo destino.

---

## ✨ Alertas de OBS — Fuente de navegador

La fuente `confetti.html` escucha eventos de OBS y anima la pantalla. Puede estar en GitHub Pages o servirse localmente.

### Parámetros de URL

| Parámetro | Efecto |
|-----------|--------|
| `?prod` | Oculta los botones de prueba |
| `?debug` | Muestra los botones + logs en consola |

### Tipos de alerta disponibles

Los activas desde Streamer.bot con el Raw Request de OBS:

| Tipo | Cuándo usarlo |
|------|--------------|
| `follow` | Nuevo follow |
| `sub` | Suscripción nueva |
| `resub` | Resuscripción |
| `giftsub` | Sub regalada |
| `massgift` | Mass gift |
| `kicks` | Donación de Kicks |

### Probar las alertas sin Kick

En OBS, haz clic derecho sobre la fuente de navegador → **Interact** → verás los botones de prueba si no añadiste `?prod`.

---

## ✋ Comando !slap

El comando `!slap` permite a los viewers "abofetear" a otros con objetos aleatorios.

**Con Nightbot:**
```
$(user) ha abofeteado a $(touser) con $(eval a=$(urlfetch json https://axljuega.github.io/data/slap_data.txt);a[Math.floor(Math.random() * a.length)];)
```

**Con Streamer.bot (C# inline):**
```csharp
using System;
using System.IO;

public class CPHInline {
    public bool Execute() {
        string target = args.ContainsKey("input0") ? args["input0"].ToString().TrimStart('@') : "alguien";
        string user   = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        // Carga los objetos desde un archivo local (una línea por objeto)
        string filePath = @"C:\ruta\a\slap_data.txt"; // ajusta la ruta
        string[] items  = File.ReadAllLines(filePath);

        string item = items[new Random().Next(items.Length)];
        CPH.SendMessage($"👋 {user} ha abofeteado a {target} con {item}");
        return true;
    }
}
```

---

## 🔍 Troubleshooting rápido

### El bot no responde en el chat de Kick
- Comprueba que la cuenta del bot esté conectada en Streamer.bot → Platforms → Kick
- Verifica que los triggers de los comandos estén activados (el toggle verde a la izquierda de cada acción)
- Revisa el Log de Streamer.bot (pestaña Logs) para ver errores de compilación C#

### Un script C# da error de compilación
- Haz clic en el sub-action C# → **Compile** — el error te indica la línea exacta
- Los errores más comunes: falta un `using System;` o hay un error de tipos (`long` vs `int`)

### Error 426 al intentar conectar n8n con OBS
- **No conectes n8n directamente a OBS.** Usa Streamer.bot como intermediario.
- Streamer.bot → OBS: usa **OBS → Raw Request** en el sub-action
- n8n → OBS: no es posible sin una capa adicional (no lo necesitas)

### Los webhooks de Discord no se envían
- Verifica que las constantes `WEBHOOK_*` en `discord_webhook.cs` tengan las URLs reales
- El script detecta las URLs de ejemplo y avisa en el log sin crashear
- Comprueba en el Log de Streamer.bot que la acción `Boinacoin · DiscordWebhook` se dispara

### `CPH.GetPresentViewers()` devuelve vacío en `timed_payout.cs`
- Este método puede no estar disponible para Kick en todas las versiones de Streamer.bot
- Alternativa: cambia el trigger de timed_payout a `Chat Message` y acumula una lista de usuarios activos en una global var, luego itera sobre ella en el timer

### Los saldos de Boinacoin desaparecieron
- Los datos viven en el archivo `users.json` de Streamer.bot
- Localízalo en `%APPDATA%\Streamer.bot\data\users.json` (Windows) o el equivalente en Wine
- Haz backup periódico de este archivo

---

## 📁 Estructura del repositorio

```
streamerbot/boinacoin/
├── earn/
│   ├── follow.cs          # +250 por follow
│   ├── sub.cs             # +5.000 por sub nueva · mult x1.5
│   ├── resub.cs           # +5k/7.5k/10k según meses · bonus racha
│   ├── giftsub.cs         # +2.500 al gifter
│   ├── massgift.cs        # +5.000 al gifter
│   ├── kicks.cs           # +1 por Kick enviado
│   ├── chat_message.cs    # +5 (cd 60s) · +25 bonus diario
│   ├── presente.cs        # !presente → +50 · gestiona racha
│   └── timed_payout.cs    # +15 cada 10 min si activo en chat
├── commands/
│   ├── cmd_boinas.cs      # !boinas [@user]
│   ├── cmd_top.cs         # !top — top 5 del canal
│   ├── cmd_rank.cs        # !rank — posición propia
│   ├── cmd_regalar.cs     # !regalar @user cantidad
│   ├── cmd_apostar.cs     # !apostar cantidad (Boina Lana+)
│   ├── cmd_duelo.cs       # !duelo / !aceptar (Boina Lana+)
│   ├── cmd_addboinas.cs   # !addboinas @user cantidad (mod+)
│   ├── cmd_setboinas.cs   # !setboinas @user cantidad (mod+)
│   ├── cmd_resetboinas.cs # !resetboinas @user (streamer)
│   ├── cmd_horafeliz.cs   # !horafeliz — x2 global 30 min (streamer)
│   └── cmd_cofre.cs       # !cofre / !abrir — cofre secreto (streamer)
├── moderation/
│   ├── mod_timeout.cs     # -500 por timeout
│   ├── mod_ban.cs         # reset a 0 por ban permanente
│   └── mod_inactividad.cs # -5% por +30 días de inactividad
├── system/
│   ├── rank_checker.cs    # Hub central de subidas de rango
│   ├── multiplier.cs      # Calculadora centralizada de multiplicadores
│   └── discord_webhook.cs # Webhooks a Discord por subida de rango
└── README.md              # Este archivo
```

---

## 🔑 Variables del sistema — referencia rápida

Todas las variables de usuario se leen/escriben con `CPH.GetUserVar` / `CPH.SetUserVar`. Las globales con `CPH.GetGlobalVar` / `CPH.SetGlobalVar`.

| Variable | Tipo | Scope | Descripción |
|----------|------|-------|-------------|
| `boinacoin` | `long` | usuario | Saldo actual |
| `boinacoin_rank` | `int` | usuario | Tier 0-4 |
| `boinacoin_multiplier` | `double` | usuario | Mult. de sub (1.5/2.0/2.5) |
| `boinacoin_streak` | `int` | usuario | Streams consecutivos |
| `boinacoin_streak_sub` | `int` | usuario | Meses de resub consecutivos |
| `boinacoin_streak_date` | `string` | usuario | Fecha del último !presente |
| `boinacoin_daily_claimed` | `string` | usuario | Fecha del último !presente |
| `boinacoin_chat_day` | `string` | usuario | Fecha del bonus diario de chat |
| `boinacoin_chat_last` | `long` | usuario | Unix timestamp último mensaje |
| `boinacoin_chat_active` | `long` | usuario | Unix timestamp actividad (para timed_payout) |
| `boinacoin_last_seen` | `long` | usuario | Unix timestamp última aparición |
| `boinacoin_total_earned` | `long` | usuario | Total histórico ganado |
| `boinacoin_rank_announced` | `int` | usuario | Último rango anunciado (antiduplicado) |
| `boinacoin_apostar_last` | `long` | usuario | Cooldown !apostar |
| `boinacoin_regalar_last` | `long` | usuario | Cooldown !regalar |
| `boinacoin_horafeliz` | `bool` | global | Hora feliz activa |
| `boinacoin_horafeliz_expiry` | `long` | global | Expiración hora feliz |
| `boinacoin_aniversario` | `bool` | global | Mult x3 aniversario |
| `boinacoin_duel_*` | varios | global | Estado del duelo activo |
| `boinacoin_cofre_*` | varios | global | Estado del cofre activo |
| `boinacoin_inactivity_last_run` | `string` | global | Control de ejecución diaria |

---

*README generado automáticamente · Sistema Boinacoin · La Chica de la Boina*
