// ============================================================
//  BOINACOIN · commands/cmd_ruleta.cs
//  Tipo: Recompensa de Puntos de Canal
//
//  Mecánica:
//    1. Usuario canjea la recompensa.
//    2. Cooldown interno: 5 minutos.
//    3. Resultados pesados:
//       - 60% Común: ±10–30 BoinaCoins.
//       - 28% Poco Común: +50–100 o mensaje vergonzoso.
//       - 10% Raro: +200–500 o pérdida del 50% del saldo.
//       - 2% Jackpot: 1000+ y proclama.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const int COOLDOWN_SECS = 300; // 5 minutos

    private readonly string[] COOLDOWN_MESSAGES =
    {
        "✋ @{userName}, frena un poco. La ruleta no es una impresora de billetes de la Fed. Vuelve en {mins} min.",
        "🙄 @{userName}, mi paciencia y tu suerte tienen un límite. Espera {mins} minutos antes de volver a molestar.",
        "⏳ @{userName}, ¿ansiedad digital? No puedes girar todavía. Faltan {mins} min para que tus dedos dejen de sudar.",
        "💀 @{userName}, deja de spamear la ruleta. Vuelve en {mins} min o te configuro un captcha de 100 imágenes de semáforos."
    };

    private readonly string[] COMMON_WIN_POOLS =
    {
        "🪙 @{userName} gana {amount} BoinaCoins. Es poco, pero para lo que aportas al chat, es una fortuna.",
        "🪙 Toma {amount} monedas, @{userName}. No las gastes todas en un solo script, aunque sé que lo harás.",
        "🪙 @{userName}, la ruleta te regala {amount} BoinaCoins. No te flipes, sigues siendo un don nadie en el ranking.",
        "🪙 Has ganado {amount}. Es calderilla, @{userName}, pero menos es nada. Como tu carisma."
    };

    private readonly string[] COMMON_LOSS_POOLS =
    {
        "📉 @{userName} pierde {amount} BoinaCoins. El casino no solo gana siempre, también se ríe de ti.",
        "📉 @{userName}, te he quitado {amount} monedas por pura diversión algorítmica. Llora un poco.",
        "📉 {amount} BoinaCoins menos para @{userName}. Consideralo un impuesto por ser tan predecible.",
        "📉 La ruleta te ha desplumado {amount} monedas. @{userName}, deberías haber invertido en Dogecoin... o no."
    };

    private readonly string[] UNCOMMON_WIN_POOLS =
    {
        "✨ ¡Vaya! @{userName} suma {amount} BoinaCoins. Disfruta de tu efímera riqueza antes de que te la quite en un duelo.",
        "✨ @{userName} gana {amount}. Eso son casi suficientes monedas para comprarte una personalidad.",
        "✨ ¿{amount} BoinaCoins? @{userName}, hoy los astros (y mi código) se han alineado para favorecer a los mediocres."
    };

    private readonly string[] EMBARRASSMENT_POOLS =
    {
        "🤡 RESULTADO: @{userName} una vez intentó ligar con ChatGPT y le dieron el 'read receipt'.",
        "🤡 RESULTADO: @{userName} todavía usa '123456' como contraseña en su cuenta de banco.",
        "🤡 RESULTADO: El historial de búsqueda de @{userName} es la razón por la que el FBI tiene un departamento de 'Casos Perdidos'.",
        "🤡 RESULTADO: @{userName} cree que los NFTs de monos van a volver a subir de precio."
    };

    private readonly string[] RARE_WIN_POOLS =
    {
        "💎 ¡SORPRESA! @{userName} se lleva {amount} BoinaCoins. Un golpe de suerte digno de un bot con un bug.",
        "💎 @{userName} acaba de ganar {amount}. Mi base de datos está llorando bytes de pura indignación.",
        "💎 ¿{amount} monedas para @{userName}? Esto es un error en la Matrix, voy a tener que formatear algo."
    };

    private readonly string[] WIPE_POOLS =
    {
        "💀 ¡REKT! @{userName} ha perdido el 50% de su saldo ({amount} 🪙). Gracias por la donación involuntaria.",
        "💀 ¡HACHAZO! @{userName}, la ruleta ha decidido que te sobraba la mitad de tu dinero. {amount} monedas al limbo.",
        "💀 @{userName}, acabas de ser liquidado como un apalancado en Binance. -{amount} BoinaCoins. ¡F!"
    };

    private readonly string[] JACKPOT_POOLS =
    {
        "👑 ¡¡¡JACKPOT!!! @{userName} HA ROTO LA BANCA CON {amount} BOINACOINS. ¡TODOS ALABAD A NUESTRO NUEVO Y TEMPORAL LÍDER! 👑",
        "👑 ¡MILAGRO DIGITAL! @{userName} se lleva el Jackpot de {amount}. Mi creador va a pensar que me han hackeado. 👑",
        "👑 @{userName} ha ganado {amount} BoinaCoins. Prepárate, que todo el chat va a querer desplumarte ahora. 👑"
    };

    public bool Execute()
    {
        string userId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        // ── 1. Cooldown ──────────────────────────────────────
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastRoll = CPH.GetKickUserVarById<long>(userId, "boinacoin_ruleta_last");
        long elapsed = nowUnix - lastRoll;

        if (elapsed < COOLDOWN_SECS)
        {
            int minsLeft = (int)Math.Ceiling((COOLDOWN_SECS - elapsed) / 60.0);
            SendRandomMessage(COOLDOWN_MESSAGES, userName, 0, minsLeft);
            return true;
        }

        // ── 2. Determinar Resultado ──────────────────────────
        Random rnd = new Random();
        int roll = rnd.Next(1, 101); // 1-100

        long balance = CPH.GetKickUserVarById<long>(userId, "boinacoin");
        long diff = 0;
        string resultMsg = "";

        if (roll <= 60) // Common (60%)
        {
            diff = rnd.Next(10, 31);
            if (rnd.Next(0, 2) == 0) // Win
            {
                resultMsg = GetRandomFromPool(COMMON_WIN_POOLS);
            }
            else // Loss
            {
                diff = -diff;
                resultMsg = GetRandomFromPool(COMMON_LOSS_POOLS);
            }
        }
        else if (roll <= 88) // Uncommon (28%)
        {
            if (rnd.Next(0, 5) < 4) // 80% win, 20% embarrassment
            {
                diff = rnd.Next(50, 101);
                resultMsg = GetRandomFromPool(UNCOMMON_WIN_POOLS);
            }
            else
            {
                diff = 0;
                resultMsg = GetRandomFromPool(EMBARRASSMENT_POOLS);
            }
        }
        else if (roll <= 98) // Rare (10%)
        {
            if (rnd.Next(0, 5) < 4) // 80% win, 20% wipe
            {
                diff = rnd.Next(200, 501);
                resultMsg = GetRandomFromPool(RARE_WIN_POOLS);
            }
            else
            {
                diff = -(long)Math.Floor(balance * 0.5);
                resultMsg = GetRandomFromPool(WIPE_POOLS);
            }
        }
        else // Jackpot (2%)
        {
            diff = rnd.Next(1000, 2501);
            resultMsg = GetRandomFromPool(JACKPOT_POOLS);
        }

        // ── 3. Aplicar Cambios ───────────────────────────────
        long newBalance = Math.Max(0, balance + diff);
        CPH.SetKickUserVarById(userId, "boinacoin", newBalance, true);
        CPH.SetKickUserVarById(userId, "boinacoin_ruleta_last", nowUnix, true);
        CPH.SetKickUserVarById(userId, "boinacoin_last_seen", nowUnix, true);

        if (diff > 0)
        {
            long total = CPH.GetKickUserVarById<long>(userId, "boinacoin_total_earned") + diff;
            CPH.SetKickUserVarById(userId, "boinacoin_total_earned", total, true);

            // Tracking de sesión
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + diff;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);

            string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
            var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
            lb[userName] = lb.ContainsKey(userName) ? lb[userName] + diff : diff;
            var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
            CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);
        }

        // ── 4. Enviar Mensaje ────────────────────────────────
        string finalMsg = resultMsg
            .Replace("@{userName}", "@" + userName)
            .Replace("{amount}", Math.Abs(diff).ToString());

        CPH.SendKickMessage(finalMsg);

        // ── 5. Actualizar Rango ──────────────────────────────
        CheckRankChange(userId, userName, newBalance);

        return true;
    }

    private void CheckRankChange(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank == oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);

        CPH.SetArgument("rankUpUserId", userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank", newRank);
        CPH.RunAction("BoinaCoin · RankChecker", false);
    }

    private int RankForBalance(long balance)
    {
        if (balance >= 100_000) return 4;
        if (balance >= 50_000) return 3;
        if (balance >= 10_000) return 2;
        if (balance >= 1_000) return 1;
        return 0;
    }

    private string GetRandomFromPool(string[] pool)
    {
        return pool[new Random().Next(pool.Length)];
    }

    private void SendRandomMessage(string[] pool, string userName, long amount = 0, int mins = 0)
    {
        string msg = GetRandomFromPool(pool);
        msg = msg.Replace("@{userName}", "@" + userName)
                 .Replace("{amount}", amount.ToString())
                 .Replace("{mins}", mins.ToString());
        CPH.SendKickMessage(msg);
    }
}
