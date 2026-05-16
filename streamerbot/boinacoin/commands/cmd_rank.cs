// ============================================================
//  BOINACOIN · commands/cmd_rank.cs
//  Comando: !rank
//  Permiso: todos
//  Muestra la posición exacta del usuario en el ranking global
//
//  Ejemplo de salida:
//    "🪙 PepeViewer · Posición #12 de 87 viewers · 3.400 🪙
//     Boina de Cuero 🪡 · Siguiente rango: 46.600 para Terciopelo"
//
//  Cómo conectarlo en Streamer.bot:
//    Acción → trigger "Kick · Chat Command · !rank"
//    Cooldown recomendado: 15 s por usuario
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    private const long RANK_LANA       =  1_000;
    private const long RANK_CUERO      = 10_000;
    private const long RANK_TERCIOPELO = 50_000;
    private const long RANK_LEGENDARIA = 100_000;

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId   = args.ContainsKey("userId")   ? args["userId"].ToString()   : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;

        // ── Datos propios ─────────────────────────────────────
        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");
        int  rank    = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int  streak  = CPH.GetKickUserVarById<int>(userId, "boinacoin_streak");

        // ── Obtener ranking global ────────────────────────────
        var allVars = CPH.GetKickUsersVar<long>("boinacoin", true);

        int position   = 1;
        int totalUsers = 0;

        if (allVars != null && allVars.Count > 0)
        {
            var filtered = allVars
                .Where(u => !string.IsNullOrEmpty(u.UserName) && u.Value > 0)
                .ToList();

            totalUsers = filtered.Count;

            // Posición = nº de usuarios con saldo MAYOR que el propio + 1
            position = filtered.Count(u => u.Value > balance) + 1;
        }

        // ── Calcular distancia al siguiente rango ─────────────
        string nextRankText = BuildNextRankText(rank, balance);

        // ── Construir mensaje ─────────────────────────────────
        string rankName  = GetRankName(rank);
        string rankEmoji = GetRankEmoji(rank);
        string streakText = streak >= 3 ? $" · Racha: {streak} 🔥" : "";
        string posText    = totalUsers > 0
            ? $"Posición #{position} de {totalUsers}"
            : $"Posición #{position}";

        CPH.SendKickMessage(
            $"📊 {rankEmoji} {userName} · {posText} · " +
            $"Saldo: {balance} 🪙 · {rankName}{streakText}{nextRankText}");

        return true;
    }

    // ── Texto de progreso hacia el siguiente rango ────────────
    private string BuildNextRankText(int rank, long balance)
    {
        switch (rank)
        {
            case 0:
                long toLana = RANK_LANA - balance;
                return toLana > 0 ? $" · Faltan {toLana} para 🧶 Boina de Lana" : "";
            case 1:
                long toCuero = RANK_CUERO - balance;
                return toCuero > 0 ? $" · Faltan {toCuero} para 🪡 Boina de Cuero" : "";
            case 2:
                long toTerciopelo = RANK_TERCIOPELO - balance;
                return toTerciopelo > 0 ? $" · Faltan {toTerciopelo} para 💎 Terciopelo" : "";
            case 3:
                long toLegendaria = RANK_LEGENDARIA - balance;
                return toLegendaria > 0 ? $" · Faltan {toLegendaria} para 👑 Legendaria" : "";
            case 4:
                return " · 👑 Rango máximo alcanzado";
            default:
                return "";
        }
    }

    private string GetRankName(int rank)
    {
        switch (rank)
        {
            case 1: return "Boina de Lana";
            case 2: return "Boina de Cuero";
            case 3: return "Boina de Terciopelo";
            case 4: return "La Boina Legendaria";
            default: return "Boina de Paja";
        }
    }

    private string GetRankEmoji(int rank)
    {
        switch (rank)
        {
            case 1: return "🧶";
            case 2: return "🪡";
            case 3: return "💎";
            case 4: return "👑";
            default: return "🪡";
        }
    }
}
