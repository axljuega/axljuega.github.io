# Personalización

BoinaCoin es un ecosistema abierto. Aquí aprenderás cómo adaptarlo totalmente a la personalidad de tu canal.

## 1. Editar el JSON de Frases
El bot utiliza el archivo `data/boinabot_frases.json` para sus respuestas aleatorias. Puedes modificarlo para que use tus expresiones, memes internos o estilo.

*   Localización: `data/boinabot_frases.json`.
*   Formato: Es un array de objetos con `tags` y `texto`.
*   Validación: Asegúrate de que el JSON sea válido tras editarlo (puedes usar herramientas online como JSONLint).

## 2. Cambiar Multiplicadores y Recompensas
Actualmente, los valores de las recompensas y multiplicadores están definidos directamente en el código de los scripts C#.

### Ejemplo: Cambiar pago pasivo
Abre la acción `BoinaCoin · TimedPayout` (archivo `earn/timed_payout.cs`) y localiza la constante:
```csharp
private const long REWARD_PASSIVE = 15; // Cambia este valor
```

### Ejemplo: Cambiar multiplicador de Rango
En el mismo archivo o en `system/multiplier.cs`, busca la lógica de rangos:
```csharp
if (rank == 4) m *= 1.5; // Cambia 1.5 por el multiplicador deseado
```

## 3. Añadir Comandos Propios
Puedes crear nuevas acciones que utilicen el saldo de BoinaCoins. Para interactuar con el saldo desde C#, usa:

*   **Leer saldo:** `long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");`
*   **Modificar saldo:** `CPH.SetKickUserVarById(userId, "boinacoin", nuevoSaldo, true);`

## Hoja de Ruta (Roadmap)
*   **Próximamente:** Estamos trabajando para externalizar toda la configuración (multiplicadores, tiempos, umbrales) a un archivo JSON externo, permitiendo cambios sin necesidad de editar el código C#.

---
*Recuerda pulsar "Compile" en Streamer.bot cada vez que realices un cambio en el código de una acción.*
