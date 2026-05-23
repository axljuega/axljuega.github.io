# Comandos

Esta es la lista completa de comandos disponibles en el ecosistema **BoinaCoin**.

## Comandos Generales (Todos los usuarios)

| Comando | Acción | Descripción |
|:--- |:--- |:--- |
| `!boinas` | `BoinaCoin · Boinas` | Consulta tu saldo actual o el de otro usuario (`!boinas @usuario`). |
| `!top` | `BoinaCoin · Top` | Muestra el ranking de los 5 usuarios con más monedas. |
| `!rank` | `BoinaCoin · Rank` | Muestra tu posición exacta en el ranking global. |
| `!regalar` | `BoinaCoin · Regalar` | Transfiere tus monedas a otro usuario (`!regalar @usuario 500`). |
| `!apostar` | `BoinaCoin · Apostar` | Apuesta una cantidad al azar (Requiere Boina de Lana+). |
| `!presente` | `BoinaCoin · Presente` | Check-in diario (+50 BoinaCoins, 1 vez por stream). |
| `!dado` | `BoinaCoin · Dado` | Lanza dados. Soporta apuestas: `!dado [caras] apuesta [nº] [cant]`. |
| `!8ball` | `BoinaCoin · 8ball` | Pregunta a la bola 8 mágica (respuestas ácidas). |
| `!blue` | `BoinaCoin · Blue` | Frases aleatorias de la asulita (moderación). |
| `!slap` | `BoinaCoin · Slap` | Da un bofetón a alguien (Coste: 10, Rango 1+). |
| `!help` | `BoinaCoin · Help` | Muestra la lista de comandos disponibles para tu rango (efímero). |
| `!vincular` | `BoinaCoin · Vincular` | Vincula tu cuenta de Discord (`!vincular usuario`). |
| `!desvincular` | `BoinaCoin · Vincular` | Elimina el vínculo con tu cuenta de Discord. |

## Comandos Exclusivos (Subs y Rangos)

| Comando | Acción | Requisito | Descripción |
|:--- |:--- |:--- |:--- |
| `!vip` | `BoinaCoin · Exclusive` | Suscriptor | Acceso a funciones VIP. |
| `!sorteo` | `BoinaCoin · Exclusive` | Suscriptor | Participar en sorteos activos. |
| `!boinavip` | `BoinaCoin · Exclusive` | Suscriptor | Comando estético exclusivo. |
| `!bufar` | `BoinaCoin · Exclusive` | Boina de Lana+ | Aumentar temporalmente la ganancia. |
| `!apodo` | `BoinaCoin · Exclusive` | Boina de Cuero+ | Cambiar tu apodo en las respuestas del bot. |
| `!spotlight` | `BoinaCoin · Exclusive` | Boina de Terciopelo+ | Destacar un mensaje en el stream. |
| `!oraculo` | `BoinaCoin · Exclusive` | La Boina Legendaria | Consulta al oráculo supremo. |

## Comandos con Modo (Set Argument)

| Comando | Argumento | Acción |
|:--- |:--- |:--- |
| `!duelo` | `mode = challenge` | `BoinaCoin · Duelo` |
| `!aceptar` | `mode = accept` | `BoinaCoin · Duelo` |
| `!cofre` | `mode = spawn` | `BoinaCoin · Cofre` |
| `!abrir` | `mode = open` | `BoinaCoin · Cofre` |
| `!horafeliz` | — | `BoinaCoin · HoraFeliz` |

## Comandos Administrativos (Moderadores/Broadcaster)

| Comando | Acción | Descripción |
|:--- |:--- |:--- |
| `!addboinas` | `BoinaCoin · AddBoinas` | Añade monedas a un usuario (`!addboinas @usuario 1000`). |
| `!setboinas` | `BoinaCoin · SetBoinas` | Fija el saldo exacto de un usuario. |
| `!resetboinas` | `BoinaCoin · ResetBoinas` | Resetea el saldo de un usuario a cero. |

---
*Nota: Los comandos marcados como "Exclusive" verifican automáticamente el rango o estado de suscripción antes de ejecutarse.*
