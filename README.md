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
- En la pestaña **Users**, busca a tu cuenta de bot y a `BotRix`, haz clic derecho → **Add to Group** → `Chat Bots`.
- Cualquier bot futuro: añádelo a este mismo grupo. El código lo excluirá automáticamente sin tocar nada.

---

## 🔔 Discord — Configuración de Webhooks

El sistema usa **4 canales de Discord** con webhooks distintos. Cada canal recibe un tipo específico de información.

### Canales y su función

| Canal Discord | Qué recibe | Webhook a usar |
|:--- |:--- |:--- |
| `#rangos-boinacoin` | Subidas de rango (Lana, Cuero, Terciopelo, Legendaria) | `WEBHOOK_RANGOS` |
| `#subs-y-follows` | Follows nuevos, subs, resubs, giftsubs, massgifts | `WEBHOOK_SUBS_FOLLOWS` |
| `#eventos-stream` | Stream ON/OFF, resumen de sesión con stats | `WEBHOOK_EVENTOS` |
| `#boinacoin-logs` | Logs internos, mod actions, errores | `WEBHOOK_LOGS` |

### Cómo crear un Webhook en Discord
1. En tu servidor de Discord, ve al canal deseado.
2. Haz clic derecho sobre el canal → **Editar canal**.
3. Ve a la pestaña **Integraciones** → **Webhooks** → **Crear Webhook**.
4. Ponle el nombre que quieras (ej. `BoinaBot Rangos`), copia la URL.
5. Repite para cada canal.

### Dónde pegar cada URL

Las URLs de webhook están hardcodeadas como constantes en la parte superior de cada script. Localiza esta línea en cada archivo y sustitúyela por tu URL real:

**`earn/follow.cs`, `earn/sub.cs`, `earn/resub.cs`, `earn/giftsub.cs`, `earn/massgift.cs`:**
```csharp
private const string WEBHOOK_SUBS_FOLLOWS = "https://discord.com/api/webhooks/TU_ID/TU_TOKEN";
```

**`system/discord_webhook.cs`** (rangos):
```csharp
private const string WEBHOOK_LANA       = "https://discord.com/api/webhooks/TU_ID/TU_TOKEN";
private const string WEBHOOK_CUERO      = "https://discord.com/api/webhooks/TU_ID/TU_TOKEN";
private const string WEBHOOK_TERCIOPELO = "https://discord.com/api/webhooks/TU_ID/TU_TOKEN";
private const string WEBHOOK_LEGENDARIA = "https://discord.com/api/webhooks/TU_ID/TU_TOKEN";
```
> Todos los rangos pueden apuntar al mismo webhook (`#rangos-boinacoin`) o a canales separados si lo prefieres.

**`system/stream_events.cs`** (stream ON/OFF — pendiente de implementar):
```csharp
private const string WEBHOOK_EVENTOS = "https://discord.com/api/webhooks/TU_ID/TU_TOKEN";
```

### ⚠️ Seguridad
Las URLs de webhook son secretas. Cualquiera con la URL puede publicar en tu canal. **No las subas a GitHub.** Si las expones accidentalmente, regenera el webhook desde Discord.

---

## 🪙 Paso 1: Creación de Acciones (C# Execute Code)

Debes crear una **Action** por cada script `.cs` en la carpeta `streamerbot/boinacoin/`.

### Cómo crear una acción:
1. Pestaña **Actions** → Botón derecho → `Add`.
2. Ponle el nombre sugerido abajo.
3. En la columna **Sub-Actions** → Botón derecho → `Core` → `C# Execute Code`.
4. Copia y pega el contenido del archivo `.cs` correspondiente.
5. Pulsa **Compile**. Debe aparecer un mensaje en verde: `Compiled Successfully`.

### Catálogo de Acciones:

| Carpeta | Script | Nombre de la Acción Recomendado |
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
| `moderation/` | `mod_timeout.cs` | `Boinacoin · Timeout` |
| `moderation/` | `mod_ban.cs` | `Boinacoin · Ban` |
| `moderation/` | `mod_inactividad.cs` | `Boinacoin · Inactividad` |
| `system/` | `rank_checker.cs` | **`Boinacoin · RankChecker`** (Exacto) |
| `system/` | `multiplier.cs` | **`Boinacoin · Multiplier`** (Exacto) |
| `system/` | `discord_webhook.cs` | **`Boinacoin · DiscordWebhook`** (Exacto) |

> ⚠️ Las acciones marcadas como **(Exacto)** deben llamarse exactamente así porque son invocadas internamente por otros scripts mediante `CPH.RunAction("nombre", false)`.

---

## 💬 Paso 2: Configuración de Comandos de Chat

Ve a la pestaña **Commands** → `Add`. Configura cada comando con su acción.

### Comandos Especiales (con Argumentos):
Para estos comandos, en la lista de **Sub-Actions**, añade primero `Core` → `Set Argument` y luego el `C# Execute Code`.

- **!duelo**: `Set Argument` → `mode` = `challenge` → Acción: `Boinacoin · Duelo`
- **!aceptar**: `Set Argument` → `mode` = `accept` → Acción: `Boinacoin · Duelo`
- **!cofre**: `Set Argument` → `mode` = `spawn` → Acción: `Boinacoin · Cofre`
- **!abrir**: `Set Argument` → `mode` = `open` → Acción: `Boinacoin · Cofre`

### Comandos Estándar:
`!boinas`, `!top`, `!rank`, `!regalar`, `!apostar`, `!presente`, `!horafeliz`

### Comandos de Administración:
Configura en la pestaña `Permissions` para que solo Mods o Broadcaster los puedan usar:
`!addboinas`, `!setboinas`, `!resetboinas`

---

## ⚡ Paso 3: Triggers de Eventos de Kick

Ve a la pestaña **Actions**, selecciona la acción y añade el Trigger en la columna central:

1. **Follows:** `Boinacoin · Follow` → `Kick` → `Follow`
2. **Suscripciones:**
   - `Boinacoin · Sub` → `Kick` → `Subscribe`
   - `Boinacoin · Resub` → `Kick` → `Re-Subscription`
   - `Boinacoin · GiftSub` → `Kick` → `Gift Subscription`
   - `Boinacoin · MassGift` → `Kick` → `Gift Subscriptions` (plural)
3. **Monedas (Kicks):** `Boinacoin · Kicks` → `Kick` → `Kicks Gifted`
4. **Chat:** `Boinacoin · ChatMessage` → `Kick` → `Chat Message`
5. **Moderación:**
   - `Boinacoin · Timeout` → `Kick` → `User Banned` + Criteria: `duration > 0`
   - `Boinacoin · Ban` → `Kick` → `User Banned` + Criteria: `duration == 0`

---

## ⏰ Paso 4: Automatización (Timers)

Ve a **Settings** → **Timed Actions** → `Add`:

1. **Boinacoin · Ingreso Pasivo** — Intervalo: `600` segundos (10 min) → Acción: `Boinacoin · TimedPayout`
2. **Boinacoin · Limpieza Inactividad** — Intervalo: `86400` segundos (24 h) → Acción: `Boinacoin · Inactividad`

---

## 📺 Paso 5: Alertas Visuales en OBS

### 1. Fuente de Navegador
En OBS Studio, añade una **Browser Source**:
- URL: `https://tu-usuario.github.io/effects/confetti.html?prod`
- Dimensiones: `1920x1080`
- Nombre de la fuente: `Crisol` (anótalo)

### 2. Configurar el Raw Request
En tus acciones de Follow/Sub, añade un Sub-Action `OBS` → `Raw Request` con este JSON:
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
> Cambia el `type` a `sub`, `resub`, etc., según corresponda.

> ⚠️ **Nota:** Si OBS no está conectado cuando se dispara este sub-action, Streamer.bot registrará un `NullReferenceException` en el log. Esto **no afecta a los Boinacoins** — el script C# ya terminó antes — pero si no usas OBS, elimina este sub-action de cada acción para evitar el ruido en los logs.

---

## 🐛 Fixes Conocidos y Notas de Compatibilidad

Esta sección documenta comportamientos específicos de Streamer.bot v1.x con Kick que difieren de lo esperado. Léela antes de modificar scripts.

### `userType` en Kick no es `"broadcaster"` ni `"moderator"`
Al leer el rol del usuario desde el evento de Kick, Streamer.bot inyecta `userType = "kick"` para el broadcaster del canal (la cuenta propietaria). Esto afecta a cualquier script que compruebe permisos por rol.

**Patrón correcto para comprobar permisos en scripts de comandos:**
```csharp
CPH.TryGetArg("userType", out string userType);
bool isStreamer = userType == "broadcaster" || userType == "moderator" || userType == "kick";
```

### `CPH.UserInGroup` requiere el parámetro `Platform`
El método `UserInGroup` tiene tres parámetros obligatorios. Omitir `Platform` causa que el método falle silenciosamente o bloquee a todos los usuarios.

**Uso correcto:**
```csharp
if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;
```

### Arg key de `kicks.gifted` es `kicks.amount`, no `amount`
El evento `Kicks Gifted` de Kick expone la cantidad de Kicks enviados bajo la clave `kicks.amount` (con prefijo de punto), no `amount` ni `kicksAmount`.

```csharp
int kicksAmount = 0;
if (args.ContainsKey("kicks.amount"))
    int.TryParse(args["kicks.amount"].ToString(), out kicksAmount);
```

### El broadcaster NO debe excluirse de los scripts de `earn/`
Los scripts de `earn/` (chat_message, follow, sub, etc.) **no deben excluir al broadcaster**. La exclusión del broadcaster solo aplica en comandos de administración (`cmd_addboinas`, `cmd_setboinas`, etc.) donde no tiene sentido que se ejecute sobre sí mismo.

**El único usuario a excluir en earn/ es BoinaBot:**
```csharp
var botInfo = CPH.KickGetBot();
if (botInfo != null && userId == botInfo.UserId.ToString()) return false;
// No añadir bloque de KickGetBroadcaster() aquí
```

### `TimeZoneInfo.FindSystemTimeZoneById` falla en Linux
Streamer.bot corre sobre .NET en Linux y no tiene la base de datos de zonas horarias del sistema. Usar `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")` lanza `TimeZoneNotFoundException`.

**Solución — offset manual:**
```csharp
// UTC+2 en verano (CEST), cambiar a +1 en invierno (CET)
string endTime = DateTimeOffset.FromUnixTimeSeconds(newExpiry)
                               .ToOffset(TimeSpan.FromHours(2))
                               .ToString("HH:mm");
```

---

## 📖 Diccionario de Variables (Persistentes)

Streamer.bot guarda estas variables en `users.json`. Se pueden consultar en la pestaña **Users** → clic derecho sobre un usuario → **Variables**.

| Variable | Tipo | Descripción |
|:--- |:--- |:--- |
| `boinacoin` | `long` | Saldo actual |
| `boinacoin_rank` | `int` | Rango (0–4) |
| `boinacoin_multiplier` | `double` | Multiplicador por sub activa |
| `boinacoin_streak` | `int` | Racha de streams asistidos |
| `boinacoin_streak_sub` | `int` | Racha de meses de resub consecutivos |
| `boinacoin_total_earned` | `long` | Total histórico de monedas ganadas |
| `boinacoin_chat_day` | `string` | Fecha del último bonus diario de chat (yyyy-MM-dd) |
| `boinacoin_chat_last` | `long` | Unix timestamp del último mensaje (cooldown 60s) |
| `boinacoin_chat_active` | `long` | Unix timestamp de última actividad (para timed_payout) |
| `boinacoin_last_seen` | `long` | Unix timestamp de última aparición (antiinactividad) |
| `boinacoin_daily_claimed` | `bool` | Si ya hizo !presente hoy |

**Variables globales** (no por usuario, accesibles con `CPH.GetGlobalVar`):

| Variable | Tipo | Descripción |
|:--- |:--- |:--- |
| `boinacoin_horafeliz` | `bool` | Si la Hora Feliz está activa |
| `boinacoin_horafeliz_expiry` | `long` | Unix timestamp de fin de Hora Feliz |

---

## ❓ Preguntas Frecuentes (FAQ)

**P: ¿Por qué el bot no responde en el chat?**
Asegúrate de que en `Settings` → `Kick`, el bot esté conectado. Revisa que el comando tenga activado el toggle `Enabled` y que la acción compile sin errores (botón **Compile** → texto verde).

**P: ¿Por qué el saldo no sube cuando chateo?**
Comprueba que la acción `Boinacoin · ChatMessage` está conectada al trigger `Kick → Chat Message`. Si compila bien pero devuelve `False` en el log, revisa que el `userId` no esté vacío y que el usuario no esté en el grupo `Chat Bots`.

**P: ¿Cómo excluyo a otros bots?**
Añádelos al grupo `Chat Bots` en la pestaña `Users`. Los scripts leen este grupo y cancelan la ejecución automáticamente. No hace falta tocar el código.

**P: ¿Puedo cambiar los nombres de las acciones?**
Sí, SALVO las marcadas como **(Exacto)** en el catálogo. `Boinacoin · RankChecker`, `Boinacoin · Multiplier` y `Boinacoin · DiscordWebhook` son invocadas internamente por otros scripts mediante su nombre literal.

**P: ¿Cómo actualizo los webhooks de Discord sin romper nada?**
Cada archivo de `earn/` tiene la URL en la constante `WEBHOOK_SUBS_FOLLOWS` al principio del archivo. `system/discord_webhook.cs` tiene las cuatro constantes `WEBHOOK_LANA/CUERO/TERCIOPELO/LEGENDARIA`. Edita solo esas constantes, recompila y ya está.

**P: El !horafeliz no funciona para mi moderador / la streamer.**
En Kick, Streamer.bot inyecta `userType = "kick"` para el broadcaster, no `"broadcaster"`. El check de permisos debe incluir los tres valores: `"broadcaster"`, `"moderator"` y `"kick"`. Ver sección **Fixes Conocidos**.

**P: Aparece un error `NullReferenceException` en el log de Follow/Sub.**
Es el sub-action de OBS intentando conectar cuando OBS no está abierto. No afecta a la lógica de Boinacoins. Elimina el sub-action `OBS Raw Request` de esas acciones si no usas OBS.

**P: El embed de Discord no se envía.**
Revisa el log de Streamer.bot. Si aparece `Webhook HTTP 401` o `HTTP 404`, la URL del webhook es incorrecta o fue regenerada en Discord. Cópiala de nuevo desde `Editar canal → Integraciones → Webhooks`.

---

## 🗺️ Roadmap — Pendiente de Implementar

- `system/stream_events.cs` — Aviso `@everyone` en `#eventos-stream` cuando el stream arranca, con embed de cierre al final con estadísticas de sesión (duración, Boinacoins repartidos, follows del stream, subs del stream, ranking interno de la sesión).
- `system/discord_roles.cs` — Asignación automática de roles de Discord al subir de rango (requiere Discord Bot con permisos de gestión de roles, distinto de webhook).
- `commands/cmd_ruleta.cs` — Ruleta de la Boina (recompensa de canal).

---

*Manual generado para el despliegue del Ecosistema Boinacoin · La Chica de la Boina 🎩*
