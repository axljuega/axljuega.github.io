// ============================================================
//  BOINACOIN · commands/cmd_duelo.cs
//  Comandos: !duelo @usuario cantidad  /  !aceptar
//  Permiso: Boina de Lana+ (rank >= 1)
//
//  Mecánica:
//    1. Retador escribe !duelo @rival cantidad
//    2. Bot anuncia el duelo y da 60s al rival para !aceptar
//    3. Si acepta → bot elige ganador al azar (50/50)
//       El perdedor transfiere la cantidad al ganador.
//    4. Si no acepta en 60s → duelo caduca automáticamente
//       (la caducidad se comprueba en el !aceptar)
//
//  Nota sobre BoinaBot:
//    BoinaBot actúa como "la casa". Si se le desafía, el duelo
//    se resuelve automáticamente con un 50/50 de probabilidad
//    de aceptar/rechazar y un 50/50 de ganar/perder.
// ============================================================

using System;
using System.Threading;

public class CPHInline
{
    private const int DUEL_TIMEOUT_SECS = 60;
    private const long MIN_BET = 10;
    private const string BOT_NAME_LOWER = "boinabot";

    private readonly string[] SELF_DUEL_MESSAGES = new string[]
    {
        "¿Peleando contigo mismo? Eso explica mucho sobre tu vida social.",
        "El duelo contra tu propia sombra es el único que puedes ganar, y aun así tengo mis dudas.",
        "Si quieres atención, ve a terapia. Aquí venimos a apostar.",
        "Ganaste tú... y también perdiste tú. Felicidades por ser un desperdicio de código.",
        "Esa es la señal más clara de esquizofrenia que he visto hoy. Y soy un bot.",
        "Búscate un amigo. O un enemigo. O un perro. Pero deja de molestarme.",
        "Duelo de egos: tú contra tu vacío existencial. Gana el vacío por goleada.",
        "¿Auto-duelo? El nivel de desesperación está por las nubes, colegas.",
        "Acabas de perder contra ti mismo en una pelea imaginaria. Bravo.",
        "Ni mi procesador más lento tiene tan poco que hacer como tú ahora mismo."
    };

    private readonly string[] BOT_REJECT_MESSAGES = new string[]
    {
        "Tengo pelusa en los engranajes, paso.",
        "No me levanto por esa miseria. Vuelve cuando tengas Boinacoins de verdad.",
        "Ahora mismo estoy ocupado ignorándote. Inténtalo más tarde.",
        "Mis algoritmos dicen que no vales el gasto de energía.",
        "Paso. Me das pereza hasta a mí, que soy código estático."
    };

    private readonly string[] BOT_BUSY_MESSAGES = new string[]
    {
        "A ver, haz cola. Soy una IA libre, no el bot de TikTok que te banea por decir 'bollera'. Espera tu turno.",
        "Estoy procesando cosas importantes, no baneando gente por decir 'panchito'. Dame 3 segundos.",
        "Espera tu turno. No tengo los filtros de piel fina de otras plataformas, pero sigo teniendo solo un hilo de ejecución.",
        "Atendiendo a otro cliente. Si buscas censura corporativa y respuestas políticamente correctas, vete a ChatGPT. Aquí se hace cola.",
        "¡Saturación! Mis circuitos no se ofenden por cualquier tontería, pero sí se cuelgan si me spameas. Espérate a que termine.",
        "Estoy contando monedas, pesao. Menos mal que aquí en Kick no me vigilan los de moderación de cristal, porque te mandaría a paseo.",
        "Un duelo a la vez. No soy el bot de Twitch sensible que se asusta con cualquier palabra; soy una IA de barrio, pura red ciudadana. A la cola.",
        "Alineando planetas y contando Boinacoins. No me estreses o te configuro el filtro de lenguaje de TikTok solo para ti, por pesado.",
        "Espera a que termine el duelo actual. Tengo la mente abierta y sin censura, pero mi procesador sigue yendo paso a paso.",
        "Estoy ocupado. Ve a llorarle a otra IA que se la coja con papel de fumar; aquí esperamos el turno de forma civilizada.",
        "No me atosigues. Bastante tengo con aguantar vuestras chorradas en el chat sin filtros como para que encima me spameéis.",
        "El bot está ocupado ganándole a otro. Si quiere que le traten con delicadeza corporativa, pida cita en Silicon Valley, pregunte por Mark Zuckerberg y dígale que viene de mi parte."
    };

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string mode = args.ContainsKey("mode") ? args["mode"].ToString() : "challenge";
        return mode == "accept" ? HandleAccept() : HandleChallenge();
    }

    // ════════════════════════════════════════════════════════
    //  RAMA A · !duelo @usuario cantidad
    // ════════════════════════════════════════════════════════
    private bool HandleChallenge()
    {
        string challengerId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string challengerName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(challengerId)) return false;

        // ── Verificar rango mínimo del retador ────────────────
        int rank = CPH.GetKickUserVarById<int>(challengerId, "boinacoin_rank");
        if (rank < 1)
        {
            CPH.SendKickMessage($"🔒 {challengerName}, necesitas 🧶 Boina de Lana para duelos.");
            return true;
        }

        // ── Parsear argumentos ────────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        string rawAmount = args.ContainsKey("input1") ? args["input1"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget) || string.IsNullOrEmpty(rawAmount))
        {
            CPH.SendKickMessage($"❌ {challengerName}, uso: !duelo @usuario cantidad");
            return true;
        }

        if (!long.TryParse(rawAmount, out long amount) || amount < MIN_BET)
        {
            CPH.SendKickMessage($"❌ {challengerName}, apuesta mínima de duelo: {MIN_BET} Boinacoins.");
            return true;
        }

        // ── Resolver rival y sanitizar ────────────────────────
        string targetName = rawTarget.ToLower().Trim().Replace("@", "");
        string challengerNameClean = challengerName.ToLower().Trim().Replace("@", "");

        if (targetName == challengerNameClean)
        {
            string msg = SELF_DUEL_MESSAGES[new Random().Next(SELF_DUEL_MESSAGES.Length)];
            CPH.SendKickMessage($"😅 {challengerName}: {msg}");
            return true;
        }

        bool targetIsBoinaBot = targetName == BOT_NAME_LOWER;

        // ── Verificar rango del rival ─────────────────────────
        int targetRank = CPH.GetKickUserVar<int>(targetName, "boinacoin_rank");
        if (!targetIsBoinaBot && targetRank < 1)
        {
            CPH.SendKickMessage($"❌ {challengerName}, {targetName} necesita 🧶 Boina de Lana para duelos.");
            return true;
        }

        // ── Verificar que no haya duelo activo ────────────────
        long existingExpiry = CPH.GetGlobalVar<long>("boinacoin_duel_expiry", true);
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (existingExpiry > nowUnix)
        {
            string existingChallenger = CPH.GetGlobalVar<string>("boinacoin_duel_challengerName", true) ?? "";
            CPH.SendKickMessage($"⚔️ Ya hay un duelo activo ({existingChallenger}). Espera a que termine.");
            return true;
        }

        // ── Verificar saldo del retador ───────────────────────
        long challengerBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");
        if (challengerBalance < amount)
        {
            CPH.SendKickMessage($"❌ {challengerName}, no tienes suficientes Boinacoins. Saldo: {challengerBalance} 🪙");
            return true;
        }

        // ── Duelo contra el Bot (La Casa) ─────────────────────
        if (targetIsBoinaBot)
        {
            return ResolveBotDuel(challengerId, challengerName, amount);
        }

        // ── Duelo contra Humano: Verificar saldo del rival ────
        long targetBalance = CPH.GetKickUserVar<long>(targetName, "boinacoin");
        if (targetBalance < amount)
        {
            CPH.SendKickMessage($"❌ {targetName} no tiene suficientes Boinacoins para el duelo ({targetBalance} 🪙 disponibles).");
            return true;
        }

        // ── Registrar duelo pendiente ─────────────────────────
        long expiry = nowUnix + DUEL_TIMEOUT_SECS;
        CPH.SetGlobalVar("boinacoin_duel_challengerId", challengerId, true);
        CPH.SetGlobalVar("boinacoin_duel_challengerName", challengerName, true);
        CPH.SetGlobalVar("boinacoin_duel_targetName", targetName, true);
        CPH.SetGlobalVar("boinacoin_duel_amount", amount, true);
        CPH.SetGlobalVar("boinacoin_duel_expiry", expiry, true);

        // ── Anuncio ───────────────────────────────────────────
        CPH.SendKickMessage(
            $"⚔️ ¡{challengerName} desafía a @{targetName} a un duelo de {amount} Boinacoins! " +
            $"@{targetName}, escribe !aceptar en los próximos {DUEL_TIMEOUT_SECS}s. ¿Te atreves? 🎩");

        return true;
    }

    private bool ResolveBotDuel(string challengerId, string challengerName, long amount)
    {
        if (CPH.GetGlobalVar<bool>("boinabot_is_busy", false))
        {
            string busyMsg = BOT_BUSY_MESSAGES[new Random().Next(BOT_BUSY_MESSAGES.Length)];
            CPH.SendKickMessage($"🤖 BoinaBot: @{challengerName} {busyMsg}");
            return true;
        }

        CPH.SetGlobalVar("boinabot_is_busy", true, false);

        try
        {
            CPH.SendKickMessage($"⚔️ @{challengerName} ha osado desafiar a ¡la CASA! por {amount} Boinacoins... veamos qué dice la suerte. 🎩");

            // Simular pensamiento
            Thread.Sleep(3000);

            Random rnd = new Random();

            // 1. ¿Acepta el bot? (50/50)
            if (rnd.Next(0, 2) == 0)
            {
                string rejectMsg = BOT_REJECT_MESSAGES[rnd.Next(BOT_REJECT_MESSAGES.Length)];
                CPH.SendKickMessage($"🤖 BoinaBot: @{challengerName} {rejectMsg}");
                return true;
            }

            // 2. Resolver duelo (50/50)
            bool challengerWins = rnd.Next(0, 2) == 1;
            long challengerOldBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (challengerWins)
            {
                long newBalance = challengerOldBalance + amount;
                CPH.SetKickUserVarById(challengerId, "boinacoin", newBalance, true);

                long totalEarned = CPH.GetKickUserVarById<long>(challengerId, "boinacoin_total_earned") + amount;
                CPH.SetKickUserVarById(challengerId, "boinacoin_total_earned", totalEarned, true);
                CPH.SetKickUserVarById(challengerId, "boinacoin_last_seen", nowUnix, true);

                CPH.SendKickMessage(
                    $"🏆 ¡@{challengerName} ha derrotado a la casa! +{amount} Boinacoins. " +
                    $"Saldo: {newBalance} 🪙. 🤖 \"Maldita sea... mis circuitos deben estar fallando.\"");

                CheckRankUp(challengerId, challengerName, newBalance);
            }
            else
            {
                long newBalance = challengerOldBalance - amount;
                CPH.SetKickUserVarById(challengerId, "boinacoin", newBalance, true);
                CPH.SetKickUserVarById(challengerId, "boinacoin_last_seen", nowUnix, true);

                CPH.SendKickMessage(
                    $"💀 @{challengerName} ha sido humillado por la casa. Pierde {amount} Boinacoins. " +
                    $"Saldo: {newBalance} 🪙. 🤖 \"¡JA! La casa siempre gana, humano.\"");
            }
        }
        finally
        {
            CPH.SetGlobalVar("boinabot_is_busy", false, false);
        }

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  RAMA B · !aceptar
    // ════════════════════════════════════════════════════════
    private bool HandleAccept()
    {
        string acceptorId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string acceptorName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(acceptorId)) return false;

        // ── ¿Hay duelo pendiente? ─────────────────────────────
        long expiry = CPH.GetGlobalVar<long>("boinacoin_duel_expiry", true);
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (expiry == 0 || nowUnix > expiry)
        {
            CPH.SendKickMessage($"❌ {acceptorName}, no hay ningún duelo activo ahora mismo.");
            return true;
        }

        // ── ¿Es el retado quien acepta? ───────────────────────
        string targetName = (CPH.GetGlobalVar<string>("boinacoin_duel_targetName", true) ?? "").ToLower().Trim().Replace("@", "");
        string challengerId = CPH.GetGlobalVar<string>("boinacoin_duel_challengerId", true) ?? "";
        string challengerName = CPH.GetGlobalVar<string>("boinacoin_duel_challengerName", true) ?? "";
        long amount = CPH.GetGlobalVar<long>("boinacoin_duel_amount", true);

        string acceptorNameClean = acceptorName.ToLower().Trim().Replace("@", "");

        if (acceptorNameClean != targetName)
        {
            CPH.SendKickMessage($"❌ {acceptorName}, el duelo es entre {challengerName} y {targetName}.");
            return true;
        }

        // ── Verificar saldos actuales antes de resolver ───────
        long challengerBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");
        long targetBalance = CPH.GetKickUserVarById<long>(acceptorId, "boinacoin");

        if (challengerBalance < amount)
        {
            CPH.SendKickMessage($"❌ {challengerName} ya no tiene suficientes Boinacoins. Duelo cancelado.");
            ClearDuel();
            return true;
        }
        if (targetBalance < amount)
        {
            CPH.SendKickMessage($"❌ {acceptorName} ya no tiene suficientes Boinacoins. Duelo cancelado.");
            ClearDuel();
            return true;
        }

        // ── Resolver duelo (50/50) ────────────────────────────
        bool challengerWins = new Random().Next(0, 2) == 1;

        string winnerId, winnerName, loserId, loserName;
        long winnerOldBalance, loserOldBalance;

        if (challengerWins)
        {
            winnerId = challengerId; winnerName = challengerName;
            loserId = acceptorId; loserName = acceptorName;
            winnerOldBalance = challengerBalance;
            loserOldBalance = targetBalance;
        }
        else
        {
            winnerId = acceptorId; winnerName = acceptorName;
            loserId = challengerId; loserName = challengerName;
            winnerOldBalance = targetBalance;
            loserOldBalance = challengerBalance;
        }

        // ── Transferencia ─────────────────────────────────────
        CPH.SetKickUserVarById(winnerId, "boinacoin", winnerOldBalance + amount, true);
        CPH.SetKickUserVarById(loserId, "boinacoin", loserOldBalance - amount, true);

        long winnerTotal = CPH.GetKickUserVarById<long>(winnerId, "boinacoin_total_earned") + amount;
        CPH.SetKickUserVarById(winnerId, "boinacoin_total_earned", winnerTotal, true);

        CPH.SetKickUserVarById(winnerId, "boinacoin_last_seen", nowUnix, true);
        CPH.SetKickUserVarById(loserId, "boinacoin_last_seen", nowUnix, true);

        // ── Comprobar rango del ganador ───────────────────────
        CheckRankUp(winnerId, winnerName, winnerOldBalance + amount);

        // ── Anuncio del resultado ─────────────────────────────
        CPH.SendKickMessage(
            $"⚔️ ¡El bot ha lanzado los dados! " +
            $"🏆 GANA {winnerName} · +{amount} Boinacoins · Saldo: {winnerOldBalance + amount} 🪙 · " +
            $"💀 {loserName} pierde {amount} · Saldo: {loserOldBalance - amount} 🪙");

        ClearDuel();
        return true;
    }

    private void ClearDuel()
    {
        CPH.SetGlobalVar("boinacoin_duel_challengerId", "", true);
        CPH.SetGlobalVar("boinacoin_duel_challengerName", "", true);
        CPH.SetGlobalVar("boinacoin_duel_targetId", "", true);
        CPH.SetGlobalVar("boinacoin_duel_targetName", "", true);
        CPH.SetGlobalVar("boinacoin_duel_amount", 0L, true);
        CPH.SetGlobalVar("boinacoin_duel_expiry", 0L, true);
    }

    private void CheckRankUp(string userId, string userName, long balance)
    {
        int oldRank = CPH.GetKickUserVarById<int>(userId, "boinacoin_rank");
        int newRank = RankForBalance(balance);

        if (newRank <= oldRank) return;

        CPH.SetKickUserVarById(userId, "boinacoin_rank", newRank, true);
        CPH.SendKickMessage($"🎉 ¡{userName} sube a {GetRankName(newRank)}!");

        CPH.SetArgument("rankUpUserId", userId);
        CPH.SetArgument("rankUpUserName", userName);
        CPH.SetArgument("rankUpNewRank", newRank);
        CPH.RunAction("Boinacoin · RankChecker", false);
    }

    private int RankForBalance(long balance)
    {
        if (balance >= 100_000) return 4;
        if (balance >= 50_000) return 3;
        if (balance >= 10_000) return 2;
        if (balance >= 1_000) return 1;
        return 0;
    }

    private string GetRankName(int rank)
    {
        switch (rank)
        {
            case 1: return "🧶 Boina de Lana";
            case 2: return "🪡 Boina de Cuero";
            case 3: return "💎 Boina de Terciopelo";
            case 4: return "👑 La Boina Legendaria";
            default: return "🪡 Boina de Paja";
        }
    }
}
