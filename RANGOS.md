# Sistema de Rangos

El ecosistema **BoinaCoin** premia la lealtad y la participación a través de un sistema de rangos automáticos. A medida que acumulas monedas, tu estatus en la comunidad evoluciona.

## Rangos Disponibles

| Rango | Umbral | Multiplicador | Beneficios |
|:--- |:--- |:--- |:--- |
| 🧶 **Boina de Lana** | 1,000 🪙 | x1.0 | Acceso a `!apostar`, `!slap` y `!bufar`. |
| 🪡 **Boina de Cuero** | 10,000 🪙 | x1.0 | Acceso a `!apodo`. |
| 💎 **Boina de Terciopelo** | 50,000 🪙 | x1.25 | Acceso a `!spotlight`. |
| 👑 **La Boina Legendaria** | 100,000 🪙 | x1.5 | Acceso a `!oraculo`. |

## Cómo subir de rango
La subida de rango es **automática**. Streamer.bot comprueba tu saldo cada vez que ganas monedas (por chatear, ingresos pasivos o eventos).

1.  **Acumula monedas:** Participa en el chat y eventos del stream.
2.  **Notificación:** Cuando alcances un umbral, el bot lo anunciará en el chat de Kick.
3.  **Discord:** Si tienes tu cuenta vinculada, recibirás automáticamente el rol correspondiente en el servidor de Discord.

## Multiplicadores
Los rangos superiores (**Terciopelo** y **Legendaria**) ofrecen un multiplicador permanente sobre todas las monedas que ganes de forma pasiva o por actividad en el chat.

*   **Boina de Terciopelo:** Ganas un 25% más de monedas.
*   **La Boina Legendaria:** Ganas un 50% más de monedas.

## Insignias de Discord
Al subir de rango, el bot **GestorDeBoinas** intentará asignarte uno de los siguientes roles en Discord (siempre que el nombre de usuario coincida o hayas usado `!vincular`):
*   `🧶 Boina de Lana`
*   `🪡 Boina de Cuero`
*   `💎 Boina de Terciopelo`
*   `👑 La Boina Legendaria`

---
*Nota: Los umbrales de rango son configurables editando las constantes en los scripts C# de la carpeta `earn/`.*
