# Guía Rápida

Poner en marcha el ecosistema **BoinaCoin** te llevará menos de 10 minutos si sigues estos pasos.

## 1. Requisitos Previos
*   **Sistema Operativo:** Windows 10/11 o Linux.
*   **Software:** OBS Studio instalado (opcional, para alertas visuales).
*   **Cuentas:** Una cuenta de Streamer en Kick y, preferiblemente, una cuenta secundaria para el Bot.

## 2. Descarga de Streamer.bot
1.  Ve a [streamer.bot](https://streamer.bot/).
2.  Descarga la última versión estable.
3.  Descomprime y ejecuta `Streamer.bot.exe`.

## 3. Conexión de Kick
1.  En Streamer.bot, ve a **Platforms** -> **Kick** -> **Accounts**.
2.  Haz clic derecho en la lista y selecciona **Add**.
3.  Añade tu cuenta de **Streamer** y tu cuenta de **Bot**.
4.  Asegúrate de que ambas aparezcan como `Connected` en verde.

## 4. Importación de la Lógica
1.  Crea una nueva acción en la pestaña **Actions** (ej. `BoinaCoin · Boinas`).
2.  En **Sub-Actions**, añade `Core` -> `C# Execute Code`.
3.  Copia el contenido del archivo correspondiente en la carpeta `streamerbot/boinacoin/` de este repositorio.
4.  Pulsa **Compile**. Si aparece en verde, ¡está listo!

## 5. Tu primer comando funcionando
1.  Ve a la pestaña **Commands**.
2.  Haz clic derecho -> **Add**.
3.  En **Command**, escribe `!boinas`.
4.  En **Action**, selecciona la acción que creaste en el paso anterior.
5.  ¡Prueba a escribir `!boinas` en tu chat de Kick!

---
*Para una configuración avanzada (Discord, rangos, etc.), consulta el resto de la documentación.*
