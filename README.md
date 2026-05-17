# 🎩 Ecosistema Boinacoin — Guía completa DIY (Streamer.bot + Kick)

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

**Streamer.bot es el centro neurálgico.** Recibe los eventos de Kick, procesa la lógica económica en C#, persiste los datos localmente y dispara las alertas a OBS o notificaciones a Discord/n8n.

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
Para evitar que los bots (incluido el tuyo o BotRix) participen en la economía:
- Ve a **Settings** → **Groups**.
- Crea un grupo llamado exactamente **`Chat Bots`**.
- En la pestaña **Users**, busca a tu cuenta de bot y a `BotRix`, haz clic derecho → **Add to Group** → `Chat Bots`.

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
| `commands/` | `cmd_resetboinas.cs`| `Boinacoin · ResetBoinas` |
| `moderation/`| `mod_timeout.cs` | `Boinacoin · Timeout` |
| `moderation/`| `mod_ban.cs` | `Boinacoin · Ban` |
| `moderation/`| `mod_inactividad.cs`| `Boinacoin · Inactividad` |
| `system/` | `rank_checker.cs` | **`Boinacoin · RankChecker`** (Exacto) |
| `system/` | `multiplier.cs` | **`Boinacoin · Multiplier`** (Exacto) |
| `system/` | `discord_webhook.cs`| **`Boinacoin · DiscordWebhook`** (Exacto) |

---

## 💬 Paso 2: Configuración de Comandos de Chat

Ve a la pestaña **Commands** → `Add`. Configura cada comando con su acción. **IMPORTANTE:** Algunos comandos usan la misma acción para dos cosas distintas y necesitan un **Argumento** previo.

### Comandos Especiales (con Argumentos):
Para estos comandos, en la lista de **Sub-Actions**, añade primero `Core` → `Set Argument` y luego el `C# Execute Code`.

- **!duelo**:
  - `Set Argument`: `mode` = `challenge`
  - Acción: `Boinacoin · Duelo`
- **!aceptar**:
  - `Set Argument`: `mode` = `accept`
  - Acción: `Boinacoin · Duelo`
- **!cofre**:
  - `Set Argument`: `mode` = `spawn`
  - Acción: `Boinacoin · Cofre`
- **!abrir**:
  - `Set Argument`: `mode` = `open`
  - Acción: `Boinacoin · Cofre`

### Comandos Estándar:
(Solo asignar la acción correspondiente)
- `!boinas`, `!top`, `!rank`, `!regalar`, `!apostar`, `!presente`, `!horafeliz`.

### Comandos de Administración:
(Configura en la pestaña `Permissions` para que solo Mods o Broadcaster los usen)
- `!addboinas`, `!setboinas`, `!resetboinas`.

---

## ⚡ Paso 3: Triggers de Eventos de Kick

Ve a la pestaña **Actions**, selecciona la acción y añade el Trigger en la columna central:

1. **Follows:** Acción `Boinacoin · Follow` → `Kick` → `Follow`.
2. **Suscripciones:**
   - Acción `Boinacoin · Sub` → `Kick` → `Subscribe`.
   - Acción `Boinacoin · Resub` → `Kick` → `Re-Subscription`.
   - Acción `Boinacoin · GiftSub` → `Kick` → `Gift Subscription`.
   - Acción `Boinacoin · MassGift` → `Kick` → `Gift Subscriptions` (plural).
3. **Monedas (Kicks):** Acción `Boinacoin · Kicks` → `Kick` → `Kicks Gifted`.
4. **Chat:** Acción `Boinacoin · ChatMessage` → `Kick` → `Chat Message`.
5. **Moderación:**
   - Acción `Boinacoin · Timeout` → `Kick` → `User Banned`.
     - Añadir **Criteria**: `duration > 0`.
   - Acción `Boinacoin · Ban` → `Kick` → `User Banned`.
     - Añadir **Criteria**: `duration == 0` (o no existe).

---

## ⏰ Paso 4: Automatización (Timers)

Ve a **Settings** → **Timed Actions** → `Add`:

1. **Boinacoin · Ingreso Pasivo:**
   - Intervalo: `600` segundos (10 min).
   - Acción: `Boinacoin · TimedPayout`.
2. **Boinacoin · Limpieza Inactividad:**
   - Intervalo: `86400` segundos (24 h).
   - Acción: `Boinacoin · Inactividad`.

---

## 📺 Paso 5: Alertas Visuales en OBS

### 1. Fuente de Navegador
En OBS Studio, añade una **Browser Source**:
- URL: `https://tu-usuario.github.io/effects/confetti.html?prod`
- Dimensiones: `1920x1080`.
- Nombre de la fuente: `Crisol` (u otro, pero anótalo).

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
*(Cambia el `type` a `sub`, `resub`, etc., según corresponda).*

---

## 🔧 Configuración Avanzada de Scripts

### Discord Webhooks
Edita el script `system/discord_webhook.cs` y sustituye estas constantes con tus URLs reales:
```csharp
private const string WEBHOOK_LANA = "TU_URL_AQUÍ";
private const string WEBHOOK_CUERO = "TU_URL_AQUÍ";
// ... etc
```

### Multiplicadores Centralizados
Si quieres cambiar cuánto vale cada racha o rango, edita `system/multiplier.cs`. Este script es la "fuente de la verdad" para todos los cálculos de puntos.

---

## 📖 Diccionario de Variables (Persistentes)

Streamer.bot guarda estas variables en `users.json`. Puedes verlas en la pestaña **Users** → Botón derecho sobre un usuario → **Variables**.

- `boinacoin`: Saldo actual (long).
- `boinacoin_rank`: Rango (0-4).
- `boinacoin_multiplier`: Multiplicador por sub activa (double).
- `boinacoin_streak`: Racha de días de asistencia (int).
- `boinacoin_total_earned`: Histórico total de monedas ganadas (long).
- `boinacoin_daily_claimed`: Fecha del último `!presente` (string).

---

## ❓ Preguntas Frecuentes (FAQ)

**P: ¿Por qué el bot no responde en el chat?**
R: Asegúrate de que en `Settings` → `Kick`, el bot esté conectado. También revisa que el comando tenga activado el "Enabled" y que la acción compile sin errores.

**P: ¿Cómo excluyo a otros bots?**
R: Añádelos al grupo `Chat Bots` en la pestaña `Users`. Los scripts leen este grupo y cancelan la ejecución automáticamente.

**P: ¿Puedo cambiar los nombres de las acciones?**
R: Sí, PERO las acciones `Boinacoin · RankChecker`, `Boinacoin · Multiplier` y `Boinacoin · DiscordWebhook` deben llamarse **exactamente así** porque son invocadas internamente por otros scripts mediante su nombre.

---
*Manual generado para el despliegue del Ecosistema Boinacoin · La Chica de la Boina 🎩*
