// ============================================================
//  BOINACOIN · commands/cmd_top.cs
//  Comando: !top
//  Permiso: todos
//  Muestra el top 5 de viewers por saldo de BoinaCoins
//
//  Nota técnica:
//    Usa CPH.GetKickUsersVar<long>() para leer la variable
//    "boinacoin" de todos los usuarios registrados en la
//    base de datos local de Streamer.bot, luego ordena en
//    memoria. El grupo "Bots" se excluye automáticamente
//    porque esos usuarios no tienen la variable persisted.
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !top"
//    Cooldown recomendado: 30 s (evitar spam del comando)
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CPHInline
{
    private const int TOP_SIZE = 5;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string callerName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        // ── Obtener todos los registros de "boinacoin" ───────
        // GetUsersVar devuelve List<UserVariableValue<T>> con
        // propiedades: UserId, UserName, Value
        var allVars = CPH.GetKickUsersVar<long>("boinacoin", true);

        if (allVars == null || allVars.Count == 0)
        {
            CPH.SendKickMessage($"📊 Todavía no hay nadie en el ranking, {callerName}. ¡Sé el primero!");
            return true;
        }

        // ── Filtrar bots y usuarios sin saldo ────────────────
        // Excluimos entradas con nombre vacío, saldo 0 o pertenecientes al grupo "Chat Bots"
        var filtered = allVars
            .Where(u => !string.IsNullOrEmpty(u.UserName) && u.Value > 0 && !CPH.UserInGroup(u.UserName, Platform.Kick, "Chat Bots"))
            .ToList();

        if (filtered.Count == 0)
        {
            CPH.SendKickMessage("📊 Aún nadie tiene BoinaCoins. ¡El ranking está vacío!");
            return true;
        }

        // ── Ordenar descendente y tomar top N ────────────────
        var top = filtered
            .OrderByDescending(u => u.Value)
            .Take(TOP_SIZE)
            .ToList();

        // ── Construir mensaje ─────────────────────────────────
        // Formato compacto para que quepa en una sola línea de chat:
        // 🏆 TOP 5 | 🥇 User1 (12.500) 🥈 User2 (9.800) ...
        var sb = new StringBuilder();
        sb.Append($"🏆 TOP {Math.Min(TOP_SIZE, top.Count)} BoinaCoins · ");

        string[] medals = { "🥇", "🥈", "🥉", "4️⃣", "5️⃣" };

        for (int i = 0; i < top.Count; i++)
        {
            string medal    = i < medals.Length ? medals[i] : $"{i + 1}.";
            string name     = top[i].UserName;
            long   balance  = top[i].Value;
            int    rank     = CPH.GetKickUserVarById<int>(top[i].UserId, "boinacoin_rank");
            string rankEmoji = GetRankEmoji(rank);

            sb.Append($"{medal} {name} {rankEmoji} ({FormatNumber(balance)})  ");
        }

        CPH.SendKickMessage(sb.ToString().TrimEnd());

        return true;
    }

    // ── Formato compacto de números grandes ──────────────────
    // 12500 → "12.5k"   100000 → "100k"   999 → "999"
    private string FormatNumber(long n)
    {
        if (n >= 1_000_000) return $"{n / 1_000_000.0:0.#}M";
        if (n >= 1_000)     return $"{n / 1_000.0:0.#}k";
        return n.ToString();
    }

    private string GetRankEmoji(int rank)
    {
        switch (rank)
        {
            case 1: return "🧶";
            case 2: return "🪡";
            case 3: return "💎";
            case 4: return "👑";
            default: return "";
        }
    }
}
