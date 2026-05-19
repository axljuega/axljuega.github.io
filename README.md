# 🎩 Ecosistema Boinacoin — Guía completa DIY (Streamer.bot + Kick + Discord)

> **Filosofía:** Este sistema está diseñado para ser modular, transparente y fácil de configurar. Cada pieza hace una cosa concreta y se conecta con las demás de forma explícita a través de variables y acciones.

---

## 📐 Arquitectura del Sistema

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
     │   OBS Studio   │        │      Discord     │    │       n8n        │
     │  (las alertas) │        │   (comunidad)    │    │ (automatización) │
     └────────────────┘        └──────────────────┘    └──────────────────┘
```

**Streamer.bot es el centro neurálgico.** Recibe los eventos de Kick, procesa la lógica económica en C#, persiste los datos localmente y dispara las alertas a OBS o notificaciones a Discord.

---

## 🛠️ Instalación y Configuración Inicial

### 1. Instalación de Streamer.bot
- **Windows:** Descarga desde [streamer.bot](https://streamer.bot) y ejecuta.
- **Linux:** Usa el [sb-linux-installer](https://github.com/Streamerbot/sb-linux-installer).

### 2. Conexión de Cuentas
- Ve a **Platforms** → **Kick**.
- Conecta tu cuenta de **Streamer** y tu cuenta de **Bot** (pueden ser distintas).
- Asegúrate de que el estado sea `Connected` (verde).

### 3. Grupos de Usuarios (¡Crítico!)
Para evitar que los bots participen en la economía:
- Ve a **Settings** → **Groups**.
- Crea un grupo llamado exactamente **`Chat Bots`**.
- Añade tu cuenta de bot, `BotRix` y cualquier bot futuro.
- El código los excluirá automáticamente sin tocar nada.

---

## 🔔 Discord — Configuración de Webhooks

El sistema usa **4 canales de Discord** con webhooks distintos.

| Canal Discord | Qué recibe | Constante en el código |
|:--- |:--- |:--- |
| `#rangos-boinacoin` | Subidas de rango | `WEBHOOK_LANA/CUERO/TERCIOPELO/LEGENDARIA` |
| `#subs-y-follows` | Follows, subs, resubs, giftsubs | `WEBHOOK_SUBS_FOLLOWS` |
| `#eventos-stream` | Stream ON/OFF, resumen de sesión | `WEBHOOK_EVENTOS` |
| `#boinacoin-logs` | Logs internos, mod actions | `WEBHOOK_LOGS` |

### Cómo crear un Webhook
1. Clic derecho sobre el canal → **Editar canal** → **Integraciones** → **Webhooks** → **Crear Webhook**.
2. Ponle nombre (ej. `BoinaBot Rangos`) y copia la URL.
3. Repite para cada canal.

### Dónde pegar cada URL
Las URLs están hardcodeadas como constantes en la parte superior de cada script:

```csharp
// earn/follow.cs, sub.cs, resub.cs, giftsub.cs, massgift.cs
private const string WEBHOOK_SUBS_FOLLOWS = "https://discord.com/api/webhooks/...";

// system/discord_webhook.cs
private const string WEBHOOK_LANA       = "https://discord.com/api/webhooks/...";
private const string WEBHOOK_CUERO      = "https://discord.com/api/webhooks/...";
private const string WEBHOOK_TERCIOPELO = "https://discord.com/api/webhooks/...";
private const string WEBHOOK_LEGENDARIA = "https://discord.com/api/webhooks/...";

// system/stream_on.cs y stream_off.cs
private const string WEBHOOK_EVENTOS = "https://discord.com/api/webhooks/...";
```

> ⚠️ **Seguridad:** No subas las URLs de webhook a GitHub. Si las expones, regéneralas desde Discord.

---

## 🤖 Discord — Bot GestorDeBoinas (Roles Automáticos)

Los webhooks solo envían mensajes. Para **asignar roles automáticamente** al subir de rango necesitas el bot **GestorDeBoinas**.

### Paso 1 — Crear la aplicación en Discord Developer Portal

1. Ve a [discord.com/developers/applications](https://discord.com/developers/applications)
2. Clic en **New Application** → Nombre: `GestorDeBoinas` 🎩 → **Create**
3. Menú izquierdo: **Bot** → **Add Bot** → confirmar
4. En **Privileged Gateway Intents** activa: ✅ `Server Members Intent`
5. En **Token** → **Reset Token** → copia y guarda el token

### Paso 2 — Invitar el bot al servidor

1. Ve a **OAuth2 → URL Generator**
2. Scope: ✅ `bot`
3. Bot Permissions: ✅ `Manage Roles` + ✅ `View Channels`
4. Copia la URL generada → ábrela → selecciona tu servidor → **Autorizar**

### Paso 3 — Crear los roles de Boina en Discord

En tu servidor: **Ajustes → Roles → Crear Rol** × 4

| Nombre | Color hex | Position |
|:--- |:--- |:--- |
| 🧶 Boina de Lana | `#888780` | más baja |
| 🪡 Boina de Cuero | `#185FA5` | encima |
| 💎 Boina de Terciopelo | `#3C3489` | encima |
| 👑 La Boina Legendaria | `#F0A500` | más alta |

⚠️ El rol de **GestorDeBoinas** debe estar **por encima** de los 4 roles de Boina en la jerarquía, o no podrá asignarlos.

### Paso 4 — Obtener los IDs

Activa **Modo Desarrollador** en Discord (Ajustes → Avanzado → Modo Desarrollador).

- **ID del servidor:** clic derecho en el nombre del servidor → Copiar ID
- **ID de cada rol:** Ajustes → Roles → clic derecho en el rol → Copiar ID

### Paso 5 — Configurar discord_roles.cs

Abre `system/discord_roles.cs` y sustituye las constantes:

```csharp
private const string BOT_TOKEN = "TU_TOKEN_AQUI";
private const string GUILD_ID  = "TU_ID_SERVIDOR";

private const string ROLE_LANA       = "ID_ROL_LANA";
private const string ROLE_CUERO      = "ID_ROL_CUERO";
private const string ROLE_TERCIOPELO = "ID_ROL_TERCIOPELO";
private const string ROLE_LEGENDARIA = "ID_ROL_LEGENDARIA";
```

### Cómo funciona la vinculación Kick → Discord

El bot busca al usuario en Discord por su nombre de usuario de Kick. Si el nombre coincide exactamente con su username de Discord, se asigna el rol automáticamente. Si no coincide (porque usan nombres distintos), el bot guarda el intento en el log y continúa — el webhook de Discord seguirá funcionando igualmente.

Una vez encontrado, el Discord ID queda cacheado en la variable `boinacoin_discord_id` del usuario, evitando búsquedas repetidas en cada subida de rango.

---

## 🪙 Paso 1: Creación de Acciones (C# Execute Code)

Debes crear una **Action** por cada script `.cs`.

### Cómo crear una acción:
1. Pestaña **Actions** → Botón derecho → `Add`.
2. En **Sub-Actions** → Botón derecho → `Core` → `C# Execute Code`.
3. Copia y pega el contenido del `.cs` correspondiente.
4. Pulsa **Compile** → debe aparecer en verde: `Compiled Successfully`.

### Catálogo de Acciones:

| Carpeta | Script | Nombre de la Acción |
|:--- |:--- |:--- |
| `earn/` | `follow.cs` | `Boinacoin · Follow` |
| `earn/` | `sub.cs` | `Boinacoin · Sub` |
| `earn/` | `resub.cs` | `Boinacoin · Resub` |
| `earn/` | `giftsub.cs` | `Boinacoin · GiftSub` |
| `earn/` | `massgift.cs` | `Boinacoin · MassGift` |
| `earn/` | `kicks.cs` | `Boinacoin · Kicks` |
| `earn/` | `chat_message.cs` | `Boinacoin · ChatMessage` |
| `earn/` | `timed_payout.cs` | `Boinacoin · TimedPayout` |
| `earn/` | `presente.cs` | `Boinacoin · Presente` |
| `commands/` | `cmd_boinas.cs` | `Boinacoin · Boinas` |
| `commands/` | `cmd_top.cs` | `Boinacoin · Top` |
| `commands/` | `cmd_rank.cs` | `Boinacoin · Rank` |
| `commands/` | `cmd_regalar.cs` | `Boinacoin · Regalar` |
| `commands/` | `cmd_apostar.cs` | `Boinacoin · Apostar` |
| `commands/` | `cmd_duelo.cs` | `Boinacoin · Duelo` |
| `commands/` | `cmd_cofre.cs` | `Boinacoin · Cofre` |
| `commands/` | `cmd_horafeliz.cs` | `Boinacoin · HoraFeliz` |
| `commands/` | `cmd_addboinas.cs` | `Boinacoin · AddBoinas` |
| `commands/` | `cmd_setboinas.cs` | `Boinacoin · SetBoinas` |
| `commands/` | `cmd_resetboinas.cs` | `Boinacoin · ResetBoinas` |
| `commands/` | `cmd_mencion.cs` | `Boinacoin · Mención` |
| `commands/` | `cmd_dado.cs` | `Boinacoin · Dado` |
| `commands/` | `cmd_8ball.cs` | `Boinacoin · 8ball` |
| `commands/` | `cmd_ruleta.cs` | `Boinacoin · Ruleta` |
| `commands/` | `cmd_vincular.cs` | `Boinacoin · Vincular` |
| `commands/` | `cmd_exclusive.cs` | `Boinacoin · Exclusive` |
| `moderation/` | `mod_timeout.cs` | `Boinacoin · Timeout` |
| `moderation/` | `mod_ban.cs` | `Boinacoin · Ban` |
| `moderation/` | `mod_inactividad.cs` | `Boinacoin · Inactividad` |
| `system/` | `rank_checker.cs` | **`Boinacoin · RankChecker`** ⚠️ Exacto |
| `system/` | `multiplier.cs` | **`Boinacoin · Multiplier`** ⚠️ Exacto |
| `system/` | `discord_webhook.cs` | **`Boinacoin · DiscordWebhook`** ⚠️ Exacto |
| `system/` | `discord_roles.cs` | **`Boinacoin · DiscordRoles`** ⚠️ Exacto |
| `system/` | `stream_on.cs` | `Boinacoin · StreamOn` |
| `system/` | `stream_off.cs` | `Boinacoin · StreamOff` |

> ⚠️ Las acciones marcadas como **Exacto** son invocadas internamente por otros scripts mediante `CPH.RunAction("nombre", false)` y deben llamarse exactamente así.

---

## 💬 Paso 2: Configuración de Comandos de Chat

Ve a **Commands** → `Add`. Configura cada comando con su acción correspondiente.

### Comandos estándar (todos los usuarios):

| Comando | Acción | Descripción |
|:--- |:--- |:--- |
| `!boinas` | `Boinacoin · Boinas` | Ver saldo propio o de otro (`!boinas @usuario`) |
| `!top` | `Boinacoin · Top` | Top 5 del canal |
| `!rank` | `Boinacoin · Rank` | Ver posición propia en el ranking |
| `!regalar` | `Boinacoin · Regalar` | Transferir coins (`!regalar @usuario 500`) |
| `!apostar` | `Boinacoin · Apostar` | Apostar al azar (requiere Boina de Lana+) |
| `!presente` | `Boinacoin · Presente` | Check-in diario (+50 Boinacoins, 1 vez por stream) |
| `!dado` | `Boinacoin · Dado` | Lanzar dados (coste base 5). Soporta apuestas: `!dado [caras] apuesta [nº] [cant]` |
| `!8ball` | `Boinacoin · 8ball` | La bola 8 mágica (ácida). Pregunta opcional. |
| `!vincular` | `Boinacoin · Vincular` | Vincular cuenta de Discord (`!vincular usuario`) |
| `!desvincular` | `Boinacoin · Vincular` | Eliminar vínculo de Discord |

### Comandos de Subs y Rangos (Exclusive):

| Comando | Acción | Requisito |
|:--- |:--- |:--- |
| `!vip` | `Boinacoin · Exclusive` | Suscriptor |
| `!sorteo` | `Boinacoin · Exclusive` | Suscriptor |
| `!boinavip` | `Boinacoin · Exclusive` | Suscriptor |
| `!bufar` | `Boinacoin · Exclusive` | Boina de Lana+ |
| `!apodo` | `Boinacoin · Exclusive` | Boina de Cuero+ |
| `!spotlight` | `Boinacoin · Exclusive` | Boina de Terciopelo+ |
| `!oraculo` | `Boinacoin · Exclusive` | La Boina Legendaria |

### Comandos con modo (Set Argument antes del Execute):

| Comando | Argumento | Acción |
|:--- |:--- |:--- |
| `!duelo` | `mode = challenge` | `Boinacoin · Duelo` |
| `!aceptar` | `mode = accept` | `Boinacoin · Duelo` |
| `!cofre` | `mode = spawn` | `Boinacoin · Cofre` |
| `!abrir` | `mode = open` | `Boinacoin · Cofre` |
| `!horafeliz` | — | `Boinacoin · HoraFeliz` |

### Comandos de administración (solo Moderator/Broadcaster):

| Comando | Acción | Descripción |
|:--- |:--- |:--- |
| `!addboinas` | `Boinacoin · AddBoinas` | Añadir puntos (`!addboinas @usuario 1000`) |
| `!setboinas` | `Boinacoin · SetBoinas` | Fijar puntos exactos |
| `!resetboinas` | `Boinacoin · ResetBoinas` | Resetear saldo a cero |

### Mención al bot (sin comando):

El trigger de `cmd_mencion.cs` no es un comando `!` sino un **Chat Message** con criterios:
- **Message Contains:** `@BoinaBot`
- **Message Does Not Start With:** `!`
- **Cooldown global:** 8 segundos

El bot responde con una frase aleatoria del archivo `data/boinabot_frases.json` del repo, con fuzzy match de tags según las palabras del mensaje.

**Ejemplos que SÍ disparan la mención:**
- `@BoinaBot` → trigger ✅
- `@BoinaBot eres tonto` → trigger ✅
- `que asco me da el @BoinaBot` → trigger ✅

**Ejemplos que NO disparan:**
- `!duelo @BoinaBot 400` → NO (empieza por `!`) ❌
- `!boinas @BoinaBot` → NO (empieza por `!`) ❌

---

## ⚡ Paso 3: Triggers de Eventos de Kick

| Acción | Trigger |
|:--- |:--- |
| `Boinacoin · Follow` | Kick → Channel → Follow |
| `Boinacoin · Sub` | Kick → Subscriptions → Subscription |
| `Boinacoin · Resub` | Kick → Subscriptions → Resubscription |
| `Boinacoin · GiftSub` | Kick → Subscriptions → Gift Subscription |
| `Boinacoin · MassGift` | Kick → Subscriptions → Mass Gift Subscription |
| `Boinacoin · Kicks` | Kick → Kicks → Gifted |
| `Boinacoin · ChatMessage` | Kick → Chat → Message |
| `Boinacoin · Mención` | Kick → Chat → Message (con criteria: contains @BoinaBot, not starts with !) |
| `Boinacoin · Timeout` | Kick → Moderation → User Banned + criteria: duration > 0 |
| `Boinacoin · Ban` | Kick → Moderation → User Banned + criteria: duration == 0 |
| `Boinacoin · StreamOn` | Kick → Channel → Stream Online |
| `Boinacoin · StreamOff` | Kick → Channel → Stream Offline |

---

## ⏰ Paso 4: Automatización (Timers)

Ve a **Settings** → **Timed Actions** → `Add`:

| Nombre | Intervalo | Acción |
|:--- |:--- |:--- |
| Ingreso Pasivo | 600s (10 min) | `Boinacoin · TimedPayout` |
| Limpieza Inactividad | 86400s (24h) | `Boinacoin · Inactividad` |

---

## 📺 Paso 5: Alertas Visuales en OBS

### Browser Source
- URL: `https://axljuega.github.io/effects/confetti.html?prod`
- Dimensiones: `1920x1080`

### OBS Raw Request (añadir en cada acción earn):
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

Tipos disponibles: `follow`, `sub`, `resub`, `giftsub`, `massgift`, `kicks`.

> ⚠️ Si OBS no está conectado, Streamer.bot registrará un `NullReferenceException` en el log. No afecta a los Boinacoins. Elimina el sub-action si no usas OBS.

---

## 📖 Diccionario de Variables (Persistentes por usuario)

| Variable | Tipo | Descripción |
|:--- |:--- |:--- |
| `boinacoin` | `long` | Saldo actual |
| `boinacoin_rank` | `int` | Rango (0–4) |
| `boinacoin_rank_announced` | `int` | Último rango anunciado (guard antiduplicado) |
| `boinacoin_multiplier` | `double` | Multiplicador por sub activa |
| `boinacoin_streak` | `int` | Racha de streams asistidos |
| `boinacoin_streak_sub` | `int` | Racha de meses de resub consecutivos |
| `boinacoin_total_earned` | `long` | Total histórico de monedas ganadas |
| `boinacoin_chat_day` | `string` | Fecha del último bonus diario (yyyy-MM-dd) |
| `boinacoin_chat_last` | `long` | Unix timestamp del último mensaje (cooldown 60s) |
| `boinacoin_chat_active` | `long` | Unix timestamp de última actividad (timed_payout) |
| `boinacoin_last_seen` | `long` | Unix timestamp de última aparición (antiinactividad) |
| `boinacoin_daily_claimed` | `bool` | Si ya hizo !presente hoy |
| `boinacoin_discord_id` | `string` | Discord user ID vinculado (cache de discord_roles.cs) |
| `boinacoin_dado_streak` | `int` | Racha de Nat Máx en `!dado` (3 = Modo Dios) |
| `boinacoin_dado_last` | `long` | Timestamp del último `!dado` (cooldown 15s) |
| `boinacoin_8ball_last` | `long` | Timestamp del último `!8ball` (cooldown 45s) |
| `boinacoin_ruleta_last` | `long` | Timestamp de la última `!ruleta` (cooldown 5m) |
| `boinacoin_discord_user` | `string` | Nombre de usuario de Discord vinculado manualmente |
| `boinacoin_session_apodo` | `string` | Apodo de sesión (persitido por stream) |

**Variables globales** (accesibles con `CPH.GetGlobalVar`):

| Variable | Tipo | Descripción |
|:--- |:--- |:--- |
| `boinacoin_horafeliz` | `bool` | Si la Hora Feliz está activa |
| `boinacoin_horafeliz_expiry` | `long` | Unix timestamp de fin de Hora Feliz |
| `boinacoin_session_start` | `long` | Unix timestamp de inicio del stream actual |
| `boinacoin_session_follows` | `long` | Follows acumulados en la sesión |
| `boinacoin_session_subs` | `long` | Subs acumuladas en la sesión |
| `boinacoin_session_earned` | `long` | Boinacoins repartidas en la sesión |
| `boinacoin_session_chatters` | `string` | JSON top 10 chatters de la sesión |
| `boinacoin_session_leaderboard` | `string` | JSON top 10 earners de la sesión |
| `boinacoin_sorteo_entries` | `string` | JSON lista de participantes del sorteo |
| `boinacoin_frases_cache` | `string` | Cache del JSON de frases de BoinaBot |
| `boinacoin_frases_cache_time` | `long` | Timestamp del último fetch del JSON |

---

## 🐛 Fixes Conocidos y Notas de Compatibilidad

### `userType` en Kick no es `"broadcaster"` ni `"moderator"`
Streamer.bot inyecta `userType = "kick"` para el broadcaster. Patrón correcto:
```csharp
CPH.TryGetArg("userType", out string userType);
bool isStreamer = userType == "broadcaster" || userType == "moderator" || userType == "kick";
```

### `CPH.UserInGroup` requiere el parámetro `Platform`
```csharp
if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;
```

### Arg key de `kicks.gifted` es `kicks.amount`
```csharp
int kicksAmount = 0;
if (args.ContainsKey("kicks.amount"))
    int.TryParse(args["kicks.amount"].ToString(), out kicksAmount);
```

### El broadcaster NO debe excluirse en earn/
Solo excluir a BoinaBot:
```csharp
var botInfo = CPH.KickGetBot();
if (botInfo != null && userId == botInfo.UserId.ToString()) return false;
```

### `TimeZoneInfo.FindSystemTimeZoneById` falla en Linux
```csharp
// UTC+2 en verano (CEST), cambiar a +1 en invierno (CET)
string endTime = DateTimeOffset.FromUnixTimeSeconds(newExpiry)
                               .ToOffset(TimeSpan.FromHours(2))
                               .ToString("HH:mm");
```

### BoinaBot no debe disparar su propio trigger de mención
El guard está en la primera línea de `cmd_mencion.cs`:
```csharp
if (userName.ToLower() == "boinabot") return false;
```

### discord_roles.cs busca por username exacto
Si el usuario tiene un nombre distinto en Kick y en Discord, el rol no se asignará automáticamente. El sistema lo registra en el log y continúa. El webhook de rango seguirá funcionando igualmente.

---

## ❓ Preguntas Frecuentes (FAQ)

**P: ¿Por qué el bot no responde en el chat?**
Comprueba que el bot esté conectado en Settings → Kick y que la acción compile sin errores.

**P: ¿Por qué el saldo no sube cuando chateo?**
Verifica que `Boinacoin · ChatMessage` tiene el trigger `Kick → Chat → Message` y que el userId no llega vacío.

**P: ¿Cómo excluyo a otros bots?**
Añádelos al grupo `Chat Bots` en la pestaña Users. Sin tocar código.

**P: ¿Puedo cambiar los nombres de las acciones?**
Sí, salvo las marcadas como ⚠️ Exacto en el catálogo.

**P: El embed de Discord no se envía.**
Revisa el log. `HTTP 401` o `HTTP 404` = URL de webhook incorrecta o regenerada. Cópiala de nuevo desde Discord.

**P: El rol de Discord no se asigna.**
Verifica que GestorDeBoinas tiene el permiso `Manage Roles` y que su rol está por encima de los roles de Boina en la jerarquía del servidor.

**P: BoinaBot entró en un loop infinito respondiendo a sus propias menciones.**
El guard `if (userName.ToLower() == "boinabot") return false;` en `cmd_mencion.cs` lo previene. Asegúrate de tener la versión más reciente del script.

**P: El !horafeliz no funciona para moderadores.**
Incluye `"kick"` en el check de permisos. Ver sección Fixes Conocidos.

---

## 🗺️ Roadmap — Pendiente

- [ ] Testear en directo los comandos exclusivos (`!vip`, `!bufar`, `!oraculo`, etc.)
- [ ] Verificar funcionamiento del `!apodo` persistido en las respuestas del bot
- [ ] Validar el pool de premios de la `Ruleta de la Boina` y su cooldown interno
- [ ] Comprobar flujo de `!vincular` con usuarios reales de Discord

---

*Manual generado para el despliegue del Ecosistema Boinacoin · La Chica de la Boina 🎩*
