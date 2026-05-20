# Configuración de Discord

El ecosistema **BoinaCoin** se integra profundamente con Discord para notificaciones y gestión de roles.

## 1. Webhooks de Notificación
Los webhooks permiten que Streamer.bot envíe mensajes automáticos a canales específicos de tu servidor.

### Canales recomendados
*   `#rangos-boinacoin`: Para subidas de rango.
*   `#subs-y-follows`: Alertas de actividad.
*   `#eventos-stream`: Inicio y fin de stream.
*   `#boinacoin-logs`: Logs de depuración.

### Cómo configurarlos
1.  En Discord: **Editar Canal** -> **Integraciones** -> **Webhooks** -> **Nuevo Webhook**.
2.  Copia la URL del webhook.
3.  En Streamer.bot, abre el script correspondiente (ej. `system/discord_webhook.cs`) y pega la URL en la constante `WEBHOOK_URL`.

## 2. Bot GestorDeBoinas (Roles)
Para que los roles de Discord se asignen automáticamente al subir de rango, necesitas crear un bot en el portal de desarrolladores.

### Paso a paso
1.  **Crear App:** Ve al [Discord Developer Portal](https://discord.com/developers/applications) y crea una "New Application".
2.  **Bot Token:** En la sección "Bot", genera un Token y guárdalo.
3.  **Intents:** Activa el **Server Members Intent** en la configuración del bot.
4.  **Invitar:** Usa el generador de URLs OAuth2 con el permiso `Manage Roles` para añadirlo a tu servidor.
5.  **Jerarquía:** ¡IMPORTANTE! El rol del bot en Discord debe estar **por encima** de los roles de las Boinas para poder asignarlos.

## 3. Vinculación Kick -> Discord
El bot intenta vincular a los usuarios automáticamente si su nombre en Kick coincide exactamente con su nombre de usuario en Discord.

Si los nombres son diferentes, el usuario puede usar el comando:
`!vincular MiUsuarioDeDiscord`

Esto guardará su ID de Discord en las variables de Streamer.bot, permitiendo que la gestión de roles funcione correctamente.

---
*Si el bot no tiene permisos suficientes o la jerarquía es incorrecta, Streamer.bot registrará el error en los logs pero continuará funcionando.*
