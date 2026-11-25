# La Chica de la Boina · Manual de cabina

**Objetivo del primer ensayo.** Desde otra cuenta de Kick, un espectador escribe `!presente`, consulta su saldo con `!boinas` y escribe `!voz Hola, Boina`. El bot responde en el chat y la voz se oye una sola vez en una grabación de OBS.

Esta guía supone Windows, OBS y Streamer.bot en el mismo PC. Las instrucciones de instalación de Piper usan PowerShell. Si ya hay una instancia de Streamer.bot en producción, haz una copia de su carpeta de datos con la aplicación cerrada. Abre después esa misma instancia y reutiliza las acciones existentes. Importar o crear un segundo juego de triggers puede duplicar pagos y alertas.

## 1. Preparar la mesa

1. Instala OBS Studio y Streamer.bot 1.x con soporte para Kick. Abre una escena de OBS llamada `Prueba Boina`. Para el ensayo basta una grabación local.
2. Descarga el repositorio desde **Code → Download ZIP**, descomprímelo y abre `streamerbot/boinacoin/`. Aquí están los C# que vas a pegar en Streamer.bot. La web de GitHub Pages sirve el overlay; los saldos se guardan en la instancia local de Streamer.bot.
3. Ten a mano la cuenta de Kick del canal y una cuenta de espectador para probar. Si BoinaBot tiene cuenta propia, úsala para las respuestas. Para la voz elige la ruta A de este manual si quieres el timbre de los Shorts, o la B si ya utilizas Speaker.bot.

[Repositorio y descarga](https://github.com/axljuega/axljuega.github.io) · [Scripts de BoinaCoin](https://github.com/axljuega/axljuega.github.io/tree/main/streamerbot/boinacoin)

## 2. Conectar Kick y comprobar que llega el chat

1. En Streamer.bot inicia sesión desde la esquina inferior izquierda. En **Platforms → Kick**, conecta como **Broadcaster** la cuenta de La Chica de la Boina. Conecta también la cuenta bot si la utilizas.
2. En la web de Streamer.bot abre **Account Settings → Connections** y vincula la **misma cuenta de Kick del canal**. Comprueba su estado en **Account → Integrations → Kick**. Este segundo enlace es el que permite recibir los eventos a través de la integración web.
3. En **Actions** crea `Prueba · Entrada Kick`. En su panel **Triggers** añade **Kick → Chat → Message**. En **Sub-Actions** añade una entrada de log con el texto fijo `Ha entrado un mensaje de Kick`.
4. Envía un mensaje desde la cuenta de espectador. En **Action History** debe aparecer la acción; abre la ejecución para inspeccionar los argumentos. Comprueba que pertenecen al canal y al espectador correctos. Desactiva la acción de prueba cuando termines.

[Configuración oficial de Kick](https://docs.streamer.bot/guide/platforms/kick)

## 3. Conectar OBS y probar el overlay sin tocar saldos

1. En OBS abre **Herramientas → Ajustes del servidor WebSocket**. Actívalo y anota el puerto y la contraseña. El puerto habitual de WebSocket v5 es `4455`.
2. En Streamer.bot abre **Stream Apps → OBS** y añade una conexión v5 a `127.0.0.1` con esos mismos datos. Conecta y activa la reconexión automática.
3. En la escena de ensayo añade una **Fuente de navegador** llamada `Alertas Boina`, de **1920 × 1080**, con esta URL: `https://axljuega.github.io/effects/confetti.html?prod`.
4. Crea una acción de prueba independiente. Añade **OBS → Raw**, selecciona la conexión y envía esta petición con la fuente visible. Debe aparecer la alerta de ensayo; esta acción no ejecuta ningún script económico.

```json
{"requestType":"CallVendorRequest","requestData":{"vendorName":"obs-browser","requestType":"emit_event","requestData":{"event_name":"stream-alert","event_data":{"type":"follow","user":"Ensayo","message":"Prueba de la boina"}}}}
```

[Conexión oficial de OBS](https://docs.streamer.bot/guide/stream-apps/obs-studio) · [Overlay del proyecto](https://axljuega.github.io/effects/confetti.html?prod)

## 4. Activar BoinaCoin con tres acciones

En **Settings → Groups**, crea el grupo exacto `Chat Bots` y añade BoinaBot y las cuentas automáticas del canal. Usa un espectador humano para las comprobaciones.

Para cada fila crea o abre la **Action**, añade **Core → C# → Execute C# Code**, pega el archivo completo y pulsa **Compile** y **Save**. Para los dos comandos, crea primero la entrada en **Commands**, selecciona **Kick** como origen y en la acción añade **Core → Commands → Command Triggered** escogiendo el comando correspondiente.

| Acción | Archivo dentro de streamerbot/boinacoin | Qué la dispara |
|---|---|---|
| BoinaCoin · Boinas | commands/cmd_boinas.cs | Comando `!boinas` |
| BoinaCoin · Presente | earn/presente.cs | Comando `!presente` |
| BoinaCoin · ChatMessage | earn/chat_message.cs | Kick → Chat → Message |

**Ensayo reproducible.** Empieza con **ChatMessage desactivada**. Desde la cuenta de prueba consulta `!boinas`, escribe `!presente` y consulta otra vez. La recompensa base es 50 y pueden intervenir multiplicadores. Repite `!presente` con un espectador normal el mismo día: el segundo intento debe indicar que ya lo reclamó. El script usa una fecha y contiene una excepción de depuración, por lo que la cuenta de pruebas importa. Activa ahora **ChatMessage** y prueba con mensajes normales. El código fija 5 de base por mensaje elegible, 60 segundos de espera y un extra diario de 25. Al aislar ambas pruebas se entiende de dónde sale cada incremento.

Antes de activar las subidas de rango, configura las acciones auxiliares con los nombres literales del README, incluido el punto medio: `BoinaCoin · RankChecker`, `BoinaCoin · DiscordRoles`, `BoinaCoin · DiscordWebhook` y, para las acciones que lo invoquen, `BoinaCoin · Multiplier`. Revisa los `CPH.RunAction(...)` de cada script que incorpores. Las credenciales de Discord se configuran en las copias locales. Para el primer ensayo de saldo elige una cuenta nueva, lejos del primer umbral de 1.000.

[Scripts de economía y comandos](https://github.com/axljuega/axljuega.github.io/tree/main/streamerbot/boinacoin) · [Trigger oficial de comandos](https://docs.streamer.bot/api/triggers/core/commands/command-triggered)

## 5. Darle voz a BoinaBot

Crea el comando `!voz` con origen Kick y una acción nueva llamada `BoinaBot · Voz`. Vincúlalos mediante **Command Triggered**. Configura la coincidencia al principio del mensaje para permitir texto después del comando. Usa una cola dedicada, bloqueante y secuencial para la acción de voz. Así una lectura larga no retiene las acciones económicas. Durante el ensayo limita el comando al equipo del canal. Como punto de partida para abrirlo al público, aplica 20 segundos de espera por usuario y activa los filtros de moderación que use el canal. En esta primera versión la voz es gratuita.

### Ruta A · Piper con la voz DaveFX de los Shorts

1. Instala Python 3.12 de 64 bits, abre PowerShell y ejecuta lo siguiente. El entorno y la voz quedan en la carpeta del usuario.

```powershell
New-Item -ItemType Directory -Force "$HOME\BoinaVoz" | Out-Null
Set-Location "$HOME\BoinaVoz"
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install "piper-tts[http]==1.8.0"
.\.venv\Scripts\python.exe -m piper.download_voices es_ES-davefx-medium
.\.venv\Scripts\python.exe -m piper.http_server -m es_ES-davefx-medium --host 127.0.0.1 --port 5000
```

2. Deja esa ventana abierta. Visita `http://127.0.0.1:5000`, escribe «La Chica de la Boina ya está en directo» y pulsa **Speak**. En los siguientes arranques solo necesitas volver a la carpeta y ejecutar la última línea.
3. En la acción `BoinaBot · Voz`, añade **Execute C# Code** y pega este adaptador. Envía el argumento como JSON a Piper y reproduce el WAV desde Streamer.bot. Los primeros ensayos deben hacerse con la salida predeterminada de Windows que uses en cabina.

[Abrir el adaptador cmd_voz.cs](streamerbot/boinacoin/commands/cmd_voz.cs)

4. Pulsa **Compile** y **Save**. Si faltan referencias, usa **Find Refs** en el editor y vuelve a compilar. Prueba `!voz Hola, Boina`. En **Action History** comprueba que `rawInput` contiene el texto posterior al comando. Si la prueba del navegador sonaba y esta falla, revisa el log `[BoinaVoz]` y el dispositivo de salida de **Streamer.bot**.
5. En OBS, añade **Captura de audio de aplicación** para Streamer.bot si quieres un fader independiente, y llámalo `Voz BoinaBot`. Evita capturar a la vez la misma salida por audio de escritorio. Graba diez segundos y escucha el archivo. El ejemplo reproduce por la aplicación Streamer.bot: si esa aplicación genera otros sonidos, compartirán el fader.

[API HTTP oficial de Piper](https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/API_HTTP.md) · [Modelo DaveFX](https://huggingface.co/rhasspy/piper-voices/tree/main/es/es_ES/davefx/medium)

### Ruta B · Speaker.bot con una voz de Windows

1. Abre Speaker.bot. En la configuración de motores añade **SAPI5**, elige una voz española disponible y crea un **Voice Alias** con el nombre `Boina`. Selecciona el dispositivo de salida y reproduce una frase de prueba.
2. En **Speaker.bot → Settings → WebSocket Server**, activa el servidor y anota su puerto. En **Streamer.bot → Integrations → Speaker.bot** usa `127.0.0.1`, introduce ese puerto y activa **Auto Connect** y **Auto Reconnect**.
3. En la acción del comando añade **Speaker.bot → Speak**, selecciona el alias `Boina` y escribe `%rawInput%` como mensaje. Usa una sola ruta de reproducción para el comando. En OBS captura **Speaker.bot** y comprueba el audio con la misma grabación de diez segundos. La voz de esta ruta será la que hayas seleccionado en SAPI5.

[Motores](https://speaker.bot/get-started/speech-engines) · [Crear y probar el alias](https://speaker.bot/get-started/quick-start) · [Conectar las aplicaciones](https://docs.streamer.bot/guide/integrations/speakerbot/) · [Subacción Speak](https://docs.streamer.bot/api/sub-actions/speakerbot/speak/)

## 6. Ampliar el directo por módulos

| Cuando funcione… | Activa después | Prueba de aceptación |
|---|---|---|
| Saldo y presencia | Eventos de follow, sub y regalos de earn/ | Un evento produce un abono y una alerta. Usa la alerta simulada del paso 3 para ensayar el overlay: el script real de follow puede penalizar un follow repetido. |
| Rangos | DiscordWebhook y DiscordRoles | Una cuenta de ensayo vinculada recibe el aviso y el rol previsto. Sitúa GestorDeBoinas por encima de los roles que debe gestionar. |
| Sesiones | StreamOn y StreamOff y después TimedPayout | El comienzo y el fin se registran una vez. Activa el temporizador de ingresos cuando puedas comprobar los destinatarios. |
| Comandos básicos | Top, Rank y los juegos que quiera el canal | Revisa saldo antes y después con un espectador de ensayo. Restringe los comandos administrativos mediante los permisos del comando en Streamer.bot. |
| Voz gratuita | Un eventual canje de voz con BoinaCoins | Es una función adicional. Antes de activarla define el precio y qué pasa si hay saldo insuficiente, Piper falla o llega un evento duplicado. |

[Catálogo de acciones](https://github.com/axljuega/axljuega.github.io/blob/main/README.md) · [Comandos](https://github.com/axljuega/axljuega.github.io/blob/main/COMANDOS.md) · [Discord](https://github.com/axljuega/axljuega.github.io/blob/main/DISCORD.md) · [Rangos](https://github.com/axljuega/axljuega.github.io/blob/main/RANGOS.md)

## 7. Antes de abrir directo

- **La streamer:** abre OBS, la aplicación de voz y Streamer.bot. Comprueba el indicador de conexión y graba una frase. Verifica que puedes silenciar el fader de voz.
- **Moderación:** desde una cuenta autorizada escribe `!voz Prueba de cabina` y `!boinas`. Comprueba una sola lectura y una sola respuesta. Para detener nuevas lecturas, deshabilita el comando; silencia el fader si ya hay mensajes en cola.
- **Si falla algo:** sin ejecución en Action History, revisa Kick, el origen del comando y el trigger. Con ejecución y sin sonido, prueba primero el motor TTS. Si se oye en auriculares pero no en la grabación, revisa la fuente, el mute y la pista de salida de OBS. Si hay eco, comprueba captura doble y subacciones duplicadas. Si el saldo cambia dos veces, revisa el historial de las acciones económicas antes de tocar las variables.

**Alcance de la verificación.** Los nombres, archivos y reglas de BoinaCoin se contrastaron con main y las conexiones con las guías oficiales. El adaptador Piper se incluye como configuración propuesta. Su compilación en la versión de Streamer.bot de la cabina, los permisos de Kick y la mezcla real de OBS se comprueban durante el ensayo.