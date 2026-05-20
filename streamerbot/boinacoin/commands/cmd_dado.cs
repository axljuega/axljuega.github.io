// ============================================================
//  BOINACOIN · commands/cmd_dado.cs
//  Comando: !dado [caras] apuesta [número] [cantidad]
//  Permiso: Todo el mundo
//
//  Mecánica:
//    1. Coste base: ceil(caras / 6) * 5.
//    2. Nat 1: Pierde doble coste base.
//    3. Nat Máximo: Recupera coste + 15 bonus.
//    4. Racha 3 Nat Máx: 4º tiro gratis.
//    5. Apuesta: Si acierta número, gana cantidad * caras.
//
//  Cooldown: 15s por usuario.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    private const int DEFAULT_CARAS = 6;
    private const int BASE_COST_UNIT = 5;
    private const int BONUS_MAX = 15;
    private const int COOLDOWN_SECS = 15;

    public bool Execute()
    {
        string userId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Cooldown ──────────────────────────────────────
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastRoll = CPH.GetKickUserVarById<long>(userId, "boinacoin_dado_last");
        long elapsed = nowUnix - lastRoll;

        if (elapsed < COOLDOWN_SECS)
        {
            CPH.SendKickMessage($"⏳ @{userName}, tus dedos están echando humo. Espera {COOLDOWN_SECS - elapsed}s.");
            return true;
        }

        // ── 2. Parseo de argumentos ──────────────────────────
        int caras = DEFAULT_CARAS;
        int betNum = -1;
        long betAmount = 0;

        List<string> inputs = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            if (args.ContainsKey("input" + i))
                inputs.Add(args["input" + i].ToString().ToLower().Trim());
        }

        int apuestaIdx = inputs.IndexOf("apuesta");

        if (apuestaIdx == -1)
        {
            if (inputs.Count > 0 && int.TryParse(inputs[0], out int c))
                caras = Math.Max(2, c);
        }
        else
        {
            if (apuestaIdx > 0 && int.TryParse(inputs[0], out int c))
                caras = Math.Max(2, c);

            if (inputs.Count > apuestaIdx + 1 && int.TryParse(inputs[apuestaIdx + 1], out int n))
                betNum = n;

            if (inputs.Count > apuestaIdx + 2 && long.TryParse(inputs[apuestaIdx + 2], out long a))
                betAmount = Math.Max(0, a);
        }

        if (betNum != -1 && (betNum < 1 || betNum > caras))
        {
            CPH.SendKickMessage($"❌ @{userName}, el número de la apuesta debe estar entre 1 y {caras}.");
            return true;
        }

        // ── 3. Gestión de Racha y Costes ──────────────────────
        int streak = CPH.GetKickUserVarById<int>(userId, "boinacoin_dado_streak");
        bool isFree = (streak >= 3);

        int baseCost = (int)Math.Ceiling(caras / 6.0) * BASE_COST_UNIT;
        long totalCost = (isFree ? 0 : baseCost) + betAmount;

        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");

        if (balance < totalCost)
        {
            CPH.SendKickMessage($"❌ @{userName}, no tienes suficiente liquidez. Necesitas {totalCost} BoinaCoins.");
            return true;
        }

        // ── 4. El Lanzamiento ────────────────────────────────
        Random rnd = new Random();
        int result = rnd.Next(1, caras + 1);

        long newBalance = balance - totalCost;
        string msg = "";

        if (isFree)
        {
            msg += "✨ [MODO DIOS: GRATIS] ";
            streak = 0;
            CPH.SetKickUserVarById(userId, "boinacoin_dado_streak", 0, true);
        }

        msg += $"🎲 @{userName} lanza un d{caras} y obtiene un... ¡{result}! ";

        // ── 5. Eventos Especiales ────────────────────────────
        bool isMax = (result == caras);
        bool isMin = (result == 1);

        if (isMax)
        {
            streak++;
            long prize = baseCost + BONUS_MAX;
            newBalance += prize;
            msg += $"🌟 ¡NAT MÁXIMO! Recuperas el coste y sumas +{BONUS_MAX} BoinaCoins. ";
            if (streak == 3) msg += "🔥 ¡MODO DIOS ACTIVADO! Siguiente lanzamiento GRATIS. ";
        }
        else if (isMin)
        {
            streak = 0;
            newBalance -= baseCost; // Penalización Nat 1 (doble coste: el ya pagado + este)
            msg += "💀 ¡NAT 1! " + GetAcidInsult();
        }
        else
        {
            streak = 0;
        }

        // ── 6. Resolución de Apuesta ─────────────────────────
        if (betNum != -1 && betAmount > 0)
        {
            if (result == betNum)
            {
                long profit = (long)(betAmount * (caras / 6.0));
                newBalance += (betAmount + profit);
                msg += $"💰 ¡VATICINIO CORRECTO! Ganas {profit} BoinaCoins (más tu apuesta). ";
            }
            else
            {
                msg += $"📉 Fallaste la apuesta de {betAmount}. ";
            }
        }

        // ── 7. Guardado y Rango ──────────────────────────────
        CPH.SetKickUserVarById(userId, "boinacoin", newBalance, true);
        CPH.SetKickUserVarById(userId, "boinacoin_dado_streak", streak, true);
        CPH.SetKickUserVarById(userId, "boinacoin_dado_last", nowUnix, true);

        // Tracking de ganancias
        long netDiff = newBalance - balance;
        if (netDiff > 0)
        {
            long total = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + netDiff;
            CPH.SetKickUserVarById(userId, "boinacoin_total_earned", total, true);
        }

        CPH.SendKickMessage(msg);

        // Actualizar rango
        CPH.SetArgument("rankUpUserId", userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.RunAction("BoinaCoin · RankChecker", false);

        return true;
    }

    private string GetAcidInsult()
    {
        string[] insults = {
            "Tu suerte es tan miserable como tu capacidad de ahorro.",
            "Ese 1 representa tu utilidad en este chat.",
            "¿Has probado a no ser una decepción algorítmica?",
            "El dado ha hablado: eres un fracaso digital.",
            "Incluso mi basura de logs tiene más valor que ese tiro.",
            "Vuelve cuando dejes de dar pena en binario."
        };
        return insults[new Random().Next(insults.Length)];
    }
}
