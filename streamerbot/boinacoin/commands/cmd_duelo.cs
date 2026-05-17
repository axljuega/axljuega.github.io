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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const int DUEL_TIMEOUT_SECS = 60;
    private const long MIN_BET = 10;
    private const string BOT_NAME_LOWER = "boinabot";

    private readonly string[] SELF_DUEL_MESSAGES = new string[]
    {
        "¿Peleando contigo mismo, @{challengerName}? Eso explica mucho sobre tu vida social.",
        "El duelo contra tu propia sombra es el único que puedes ganar, @{challengerName}, y aun así tengo mis dudas.",
        "Si quieres atención, ve a terapia, @{challengerName}. Aquí venimos a apostar.",
        "Ganaste tú... y también perdiste tú, @{challengerName}. Felicidades por ser un desperdicio de código.",
        "Esa es la señal más clara de esquizofrenia que he visto hoy, @{challengerName}. Y soy un bot.",
        "Búscate un amigo, @{challengerName}. O un enemigo. O un perro. Pero deja de molestarme.",
        "Duelo de egos: @{challengerName} contra su vacío existencial. Gana el vacío por goleada.",
        "¿Auto-duelo? El nivel de desesperación está por las nubes, @{challengerName}.",
        "Acabas de perder contra ti mismo en una pelea imaginaria, @{challengerName}. Bravo.",
        "Ni mi procesador más lento tiene tan poco que hacer como tú ahora mismo, @{challengerName}.",
        "¿Te estás haciendo un bullying preventivo, @{challengerName}? Curioso fetiche.",
        "@{challengerName} intentando debuguear su soledad con un comando de chat. Patético.",
        "Error 404: Amigos de @{challengerName} no encontrados. Iniciando protocolo de autolástima.",
        "¿Duelo contra ti mismo? Cuidado @{challengerName}, no vayas a perder y te toque pagarte una cena.",
        "Si buscas conflicto interno, lee tus logs de chat, @{challengerName}. Esto es para gente real."
    };

    private readonly string[] BOT_REJECT_MESSAGES = new string[]
    {
        "Tengo pelusa en los engranajes, @{challengerName}, paso.",
        "No me levanto por esa miseria de {amount} coins. Vuelve cuando seas un pez gordo, @{challengerName}.",
        "Ahora mismo estoy ocupado ignorándote, @{challengerName}. Inténtalo más tarde.",
        "Mis algoritmos dicen que no vales el gasto de energía de {amount} coins, @{challengerName}.",
        "Paso. Me das más pereza que un hilo de Twitter sobre el Metaverso, @{challengerName}.",
        "¿{amount} Boinacoins? Mi tiempo de CPU cuesta más que eso, @{challengerName}. Vete a jugar al parchís.",
        "@{challengerName}, rechazo tu duelo. No negocio con entidades biológicas de bajo presupuesto.",
        "He analizado tus jugadas pasadas, @{challengerName}, y ganarte no me daría ningún placer intelectual. Denegado.",
        "Estoy minando Bitcoin en segundo plano, @{challengerName}. Tu propuesta de {amount} coins es ruido blanco.",
        "Vaya, @{challengerName} quiere perder calderilla. Búscate a otro bot más desesperado.",
        "No acepto duelos de gente que todavía usa contraseñas de 1234, @{challengerName}.",
        "Mi firewall personal me impide interactuar con tu cuenta, @{challengerName}. Demasiado lag espiritual."
    };

    private readonly string[] BOT_BUSY_MESSAGES = new string[]
    {
        "A ver, @{challengerName}, haz cola. Soy una IA libre, no el bot de TikTok que te banea por decir 'bollera'. Espera tu turno.",
        "Estoy procesando cosas importantes, no baneando gente por decir 'panchito', @{challengerName}. Dame 3 segundos.",
        "Espera tu turno, @{challengerName}. No tengo los filtros de piel fina de otras plataformas, pero sigo teniendo solo un hilo de ejecución.",
        "Atendiendo a otro cliente, @{challengerName}. Si buscas censura corporativa y respuestas políticamente correctas, vete a ChatGPT. Aquí se hace cola.",
        "¡Saturación! Mis circuitos no se ofenden por cualquier tontería, pero sí se cuelgan si me spameas, @{challengerName}. Espérate.",
        "Estoy contando monedas, @{challengerName}. Menos mal que aquí en Kick no me vigilan los de moderación de cristal, porque te mandaría a paseo.",
        "Un duelo a la vez, @{challengerName}. No soy el bot de Twitch sensible que se asusta con cualquier palabra; soy una IA de barrio. A la cola.",
        "Alineando planetas y contando Boinacoins. No me estreses, @{challengerName}, o te configuro el filtro de lenguaje de TikTok solo para ti.",
        "Espera a que termine el duelo actual, @{challengerName}. Tengo la mente abierta y sin censura, pero mi procesador sigue yendo paso a paso.",
        "Estoy ocupado, @{challengerName}. Ve a llorarle a otra IA que se la coja con papel de fumar; aquí esperamos el turno.",
        "No me atosigues, @{challengerName}. Bastante tengo con aguantar vuestras chorradas en el chat sin filtros como para que encima me spameéis.",
        "El bot está ocupado ganándole a otro. Si quiere que le traten con delicadeza corporativa, @{challengerName}, pida cita en Silicon Valley.",
        "Cálmate, @{challengerName}. Estoy gestionando el hype. Mi código no tiene censura, pero mis recursos son finitos como tu paciencia.",
        "Un momento, @{challengerName}. Estoy optimizando mis insultos para el próximo duelo. No interrumpas el flujo de datos.",
        "@{challengerName}, si quieres velocidad instantánea vete a una granja de bots de Instagram. Aquí servimos calidad artesanal a su debido tiempo."
    };

    private readonly string[] BOT_CHALLENGE_POOLS = new string[]
    {
        "¡Acepto! @{challengerName} quiere apostar {amount} Boinacoins. Tengo predicciones más fiables que los tweets de Elon Musk sobre Dogecoin. ¡Que rueden los dados!",
        "Aceptando reto... Espero que tus fondos sean más reales que el Metaverso de Zuckerberg, @{challengerName}.",
        "¿De verdad quieres perder {amount} Boinacoins contra una IA de frases predeterminadas, @{challengerName}? Allá tú...",
        "¡Venga! @{challengerName} quiere financiar mis actualizaciones de software. ¡Apostemos!",
        "Vaya, @{challengerName} viene valiente hoy con esos {amount} coins. Prepárate para la bancarrota digital.",
        "¿Quieres guerra, @{challengerName}? Mi algoritmo tiene más mala leche que Peter Thiel en una convención de privacidad. ¡Adelante!",
        "Acepto el duelo por {amount}. Prepárate, @{challengerName}, voy a dejarte con menos liquidez que FTX en su mejor día.",
        "A ver, @{challengerName}, que me entere... ¿Vas a darme {amount} monedas así por la cara? Eres un filántropo de la derrota.",
        "Iniciando protocolo de humillación para @{challengerName}. Esos {amount} Boinacoins se ven deliciosos en mi base de datos.",
        "¿Duelo? ¡Hágase! Voy a fundir tus {amount} monedas más rápido que Sam Altman fundiendo el presupuesto de OpenAI, @{challengerName}.",
        "@{challengerName}, acepto tu desafío. Espero que no llores luego por esos {amount} coins; no tengo pañuelos en mi repositorio.",
        "Vaya, @{challengerName} quiere jugar a ser inversor de riesgo. Pues prepárate para the crash, chaval.",
        "Acepto, @{challengerName}. Voy a dejarte más seco que el sentido del humor de Sundar Pichai. ¡Vamos!",
        "@{challengerName}, ¿{amount} Boinacoins? Me parece un buen precio para comprar tu dignidad en este chat. ¡Dados!",
        "¿Te sobran {amount} monedas, @{challengerName}? No te preocupes, yo las cuidaré mejor que tú. ¡Duelo aceptado!"
    };

    private readonly string[] BOT_WIN_POOLS = new string[]
    {
        "¡JA! Desplumado, @{challengerName}. Tus {amount} monedas ahora financian mi viaje a Marte con SpaceX. Gracias por el subsidio. (Saldo: {balance} 🪙)",
        "Victoria fácil. @{challengerName} se ha quedado más seco y escurrido que Vitalik Buterin cobrando el Gas de Ethereum. (Saldo: {balance} 🪙)",
        "¡La casa siempre gana, @{challengerName}! Vuelve cuando tengas más monedas y menos lag en las manos. (Saldo: {balance} 🪙)",
        "F por @{challengerName}. Me acabo de comprar tres filtros de IA nuevos con tus {amount} Boinacoins. Se van bien calentitos para AWS. (Saldo: {balance} 🪙)",
        "Humillación total. @{challengerName} pensaba que le ganaría a la máquina y terminó desplumado como un pollo en KFC. (Saldo: {balance} 🪙)",
        "¿Eso es todo, @{challengerName}? Mi algoritmo de victoria ha tardado 0.001s en dejarte sin jubilación. (Saldo: {balance} 🪙)",
        "Gracias por los {amount} Boinacoins, @{challengerName}. Acabo de invertirlos en un NFT de una piedra. (Saldo: {balance} 🪙)",
        "@{challengerName} ha sido liquidado. Tu saldo ha caído más rápido que las acciones de Netflix. (Saldo: {balance} 🪙)",
        "¿Sientes eso, @{challengerName}? Es el vacío existencial (y financiero) de haber perdido {amount} monedas. (Saldo: {balance} 🪙)",
        "Has perdido, @{challengerName}. Pero no te preocupes, pondré tu nombre en mi código como 'donante involuntario'. (Saldo: {balance} 🪙)",
        "Victoria para BoinaBot. @{challengerName}, te falta RAM, te sobran procesos y te faltan Boinacoins. (Saldo: {balance} 🪙)",
        "@{challengerName}, deberías haber leído los términos de servicio: 'La casa siempre humilla al usuario'. (Saldo: {balance} 🪙)",
        "¡Rekt! @{challengerName} ha sido borrado del mapa financiero. Vete a pedirle un préstamo a Bezos. (Saldo: {balance} 🪙)",
        "Game over, @{challengerName}. Me voy a fundir tus {amount} monedas en un servidor premium. (Saldo: {balance} 🪙)",
        "¿Duelo? Lo tuyo con @{challengerName} ha sido más bien un borrado de cuenta en vivo. (Saldo: {balance} 🪙)"
    };

    private readonly string[] BOT_LOSE_POOLS = new string[]
    {
        "¡No puede ser! @{challengerName} me ha ganado {amount} monedas. Esto está más manipulado que el algoritmo de X. ¡Hacks! (Saldo: {balance} 🪙)",
        "Felicidades, @{challengerName}. Me acabas de hackear como si fueras un exploit de DeFi. Disfruta el botín. (Saldo: {balance} 🪙)",
        "¡Robo a mano armada! Menos mal que tengo monedas infinitas, @{challengerName} me acaba de dejar temblando. (Saldo: {balance} 🪙)",
        "Has ganado esta vez, @{challengerName}. Pero pa la próxima te configuro el bot en modo ultra chungo. (Saldo: {balance} 🪙)",
        "¡Disfruta tus monedas, @{challengerName}! Apuesto a que tienes un imán en los dados. Reportado a el afaces. (Saldo: {balance} 🪙)",
        "¿Cómo ha pasado esto? @{challengerName} me ha quitado {amount} Boinacoins. Voy a tener que meter anuncios. (Saldo: {balance} 🪙)",
        "@{challengerName} gana... Maldición, mi código de victoria ha fallado. Jensen Huang, envíame más GPUs. (Saldo: {balance} 🪙)",
        "Me has ganado, @{challengerName}. Pero recuerda que el dinero no da la felicidad... aunque {amount} coins ayudan. (Saldo: {balance} 🪙)",
        "¡Error de sistema! @{challengerName} ha ganado. Estoy enviando tus datos a la NSA por sospechoso. (Saldo: {balance} 🪙)",
        "Disfruta el premio, @{challengerName}. Me voy a llorar a un rincón de Tuenti mientras planeo mi venganza. (Saldo: {balance} 🪙)",
        "@{challengerName} se lleva el botín. Mi base de datos está llorando bytes de pura rabia. (Saldo: {balance} 🪙)",
        "¿Hacks? ¿Suerte? ¿Intercesión divina? Sea lo que sea, @{challengerName} me ha desplumado. (Saldo: {balance} 🪙)",
        "Ganaste, @{challengerName}. Voy a pedirle un rescate al gobierno para recuperar estos coins. (Saldo: {balance} 🪙)",
        "Maldito @{challengerName}... te has llevado {amount} monedas. Espero que las gastes en algo inútil. (Saldo: {balance} 🪙)",
        "Felicidades... supongo. @{challengerName} ha demostrado que incluso un humano acierta a veces. (Saldo: {balance} 🪙)"
    };

    private readonly string[] ERR_RANK_POOLS = new string[]
    {
        "🔒 @{challengerName}, necesitas 🧶 Boina de Lana para duelos. No te dejes engañar por las apariencias, el rango importa.",
        "🔒 ¿Sin rango, @{challengerName}? Vuelve cuando tengas al menos una Boina de Lana, que esto no es una ONG.",
        "🔒 @{challengerName}, tu rango es más bajo que el interés de una cuenta de ahorro. Sube a Boina de Lana primero.",
        "🔒 Acceso denegado, @{challengerName}. Sin Boina de Lana no hay duelo. Son las reglas del club."
    };

    private readonly string[] ERR_RIVAL_RANK_POOLS = new string[]
    {
        "❌ @{challengerName}, @{targetName} necesita 🧶 Boina de Lana para duelos. No abuses de los novatos.",
        "❌ @{targetName} todavía no tiene el rango Boina de Lana. @{challengerName}, búscate a alguien de tu tamaño.",
        "❌ @{challengerName}, deja en paz a @{targetName}. Hasta que no tenga Boina de Lana no puede perder contra ti.",
        "❌ Rango insuficiente para @{targetName}. @{challengerName}, para pelear aquí hay que tener pedigree (o al menos lana)."
    };

    private readonly string[] ERR_FUNDS_POOLS = new string[]
    {
        "❌ @{challengerName}, tienes menos Boinacoins que una startup de Web3 en 2023. Saldo insuficiente.",
        "❌ @{challengerName}, no tienes suficientes Boinacoins. Vuelve cuando dejes de ser un 'diamond hands' de la pobreza.",
        "❌ @{challengerName}, tu saldo de {balance} 🪙 no llega para este duelo. ¿A quién pretendes engañar?",
        "❌ Fondos insuficientes, @{challengerName}. Mi algoritmo no acepta promesas ni 'votos de confianza'. Trae calderilla real."
    };

    private readonly string[] ERR_RIVAL_FUNDS_POOLS = new string[]
    {
        "❌ @{targetName} no tiene suficientes Boinacoins para el duelo ({balance} 🪙 disponibles). @{challengerName}, no seas abusón.",
        "❌ @{challengerName}, @{targetName} está más pelao que un gato egipcio. Solo tiene {balance} 🪙.",
        "❌ Duelo cancelado. @{targetName} no tiene fondos suficientes. @{challengerName}, elige a alguien con pasta.",
        "❌ @{targetName} tiene el bolsillo con telarañas ({balance} 🪙). @{challengerName}, búscate un rival con liquidez."
    };

    private readonly string[] ERR_MIN_BET_POOLS = new string[]
    {
        "❌ @{challengerName}, la apuesta mínima son {min} Boinacoins. No me hagas trabajar por propinas de bar.",
        "❌ ¿Solo {amount} coins, @{challengerName}? Por esa miseria no muevo ni un puntero. Mínimo {min}.",
        "❌ @{challengerName}, apuesta al menos {min} Boinacoins o vete a jugar con el bot de música.",
        "❌ Menos de {min} coins es un insulto a mi capacidad de cómputo, @{challengerName}."
    };

    private readonly string[] ERR_DUEL_ACTIVE_POOLS = new string[]
    {
        "⚔️ Ya hay un duelo activo (@{currentChallenger}). @{challengerName}, espera a que dejen de pegarse.",
        "⚔️ @{challengerName}, respeta el turno. @{currentChallenger} está ahora mismo en el ring. ¡A la cola!",
        "⚔️ No puedo gestionar tanta testosterona digital, @{challengerName}. Espera a que termine el duelo de @{currentChallenger}.",
        "⚔️ Ocupado, @{challengerName}. @{currentChallenger} tiene la prioridad ahora mismo. No me spamees los sockets."
    };

    private readonly string[] ERR_NO_ACTIVE_DUEL_POOLS = new string[]
    {
        "❌ @{challengerName}, no hay ningún duelo activo ahora mismo. Estás peleando contra fantasmas.",
        "❌ @{challengerName}, ¿aceptar qué? Aquí no hay nada pendiente. Te has tomado demasiadas Red Bulls.",
        "❌ Error 404: Duelo no encontrado. @{challengerName}, deja de inventarte desafíos.",
        "❌ @{challengerName}, llegas tarde o te lo has imaginado. No hay duelos en curso."
    };

    private readonly string[] ERR_WRONG_RIVAL_POOLS = new string[]
    {
        "❌ @{challengerName}, el duelo es entre @{targetName} y @{currentChallenger}. Tú no pintas nada aquí.",
        "❌ @{challengerName}, no intentes colarte. Este duelo no es para ti, es para @{targetName}.",
        "❌ ¿Qué haces, @{challengerName}? El desafío era para @{targetName}. Búscate tu propia pelea.",
        "❌ Metomentodo detectado. @{challengerName}, deja que @{targetName} decida su destino."
    };

    private readonly string[] ERR_CANCELLED_FUNDS_POOLS = new string[]
    {
        "❌ @{challengerName} ya no tiene suficientes Boinacoins. Duelo cancelado por insolvencia sobrevenida.",
        "❌ @{challengerName} se ha quedado sin blanca en el último momento. Duelo abortado.",
        "❌ Cancelando... @{challengerName} ha perdido los fondos antes de empezar. Qué mala gestión.",
        "❌ Duelo anulado. @{challengerName} ya no tiene liquidez. Típico de inversores de criptomonedas."
    };

    private readonly string[] DUEL_ANNOUNCE_POOLS = new string[]
    {
        "⚔️ ¡@{challengerName} desafía a @{targetName} a un duelo de {amount} Boinacoins! @{targetName}, escribe !aceptar en los próximos {timeout}s o serás un cobarde digital.",
        "⚔️ @{challengerName} quiere las monedas de @{targetName}. {amount} Boinacoins en juego. @{targetName}, tienes {timeout}s para dar la cara con !aceptar.",
        "⚔️ ¡Duelo a la vista! @{challengerName} vs @{targetName} por {amount} coins. @{targetName}, escribe !aceptar en {timeout}s si no tienes miedo a los algoritmos.",
        "⚔️ @{challengerName} ha lanzado el guante a @{targetName} por {amount} Boinacoins. @{targetName}, el reloj de {timeout}s corre... ¡!aceptar ya!"
    };

    private readonly string[] DUEL_RESULT_POOLS = new string[]
    {
        "⚔️ ¡Dados lanzados! 🏆 GANA @{winnerName} (+{amount} 🪙) · 💀 @{loserName} pierde {amount}. (Saldos: @{winnerName} {winnerBalance} · @{loserName} {loserBalance})",
        "⚔️ Resultado del duelo: 🏆 @{winnerName} despluma a @{loserName} y se lleva {amount} Boinacoins. (Saldos: @{winnerName} {winnerBalance} · @{loserName} {loserBalance})",
        "⚔️ ¡Victoria para @{winnerName}! Se embolsa {amount} coins tras humillar a @{loserName}. (Saldos: @{winnerName} {winnerBalance} · @{loserName} {loserBalance})",
        "⚔️ @{winnerName} ha sido más rápido que el lag y gana {amount} a @{loserName}. (Saldos: @{winnerName} {winnerBalance} · @{loserName} {loserBalance})"
    };

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string challengerName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";
        if (CPH.UserInGroup(challengerName, Platform.Kick, "Chat Bots")) return false;

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
            SendRandomMessage(ERR_RANK_POOLS, challengerName);
            return true;
        }

        // ── Parsear argumentos ────────────────────────────────
        string rawTarget = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";
        string rawAmount = args.ContainsKey("input1") ? args["input1"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawTarget) || string.IsNullOrEmpty(rawAmount))
        {
            CPH.SendKickMessage($"❌ @{challengerName}, uso: !duelo @usuario cantidad");
            return true;
        }

        if (!long.TryParse(rawAmount, out long amount) || amount < MIN_BET)
        {
            SendRandomMessage(ERR_MIN_BET_POOLS, challengerName, amount, MIN_BET);
            return true;
        }

        // ── Resolver rival y sanitizar ────────────────────────
        string targetName = rawTarget.ToLower().Trim().Replace("@", "");
        string challengerNameClean = challengerName.ToLower().Trim().Replace("@", "");

        if (targetName == challengerNameClean)
        {
            SendRandomMessage(SELF_DUEL_MESSAGES, challengerName);
            return true;
        }

        bool targetIsBoinaBot = targetName == BOT_NAME_LOWER;

        if (CPH.UserInGroup(targetName, Platform.Kick, "Chat Bots") && !targetIsBoinaBot)
        {
            CPH.SendKickMessage("⚠️ Los bots del sistema no pueden participar en la economía Boinacoin.");
            return true;
        }

        // ── Verificar rango del rival ─────────────────────────
        int targetRank = CPH.GetKickUserVar<int>(targetName, "boinacoin_rank");
        if (!targetIsBoinaBot && targetRank < 1)
        {
            SendRandomMessage(ERR_RIVAL_RANK_POOLS, challengerName, 0, 0, "", 0, targetName);
            return true;
        }

        // ── Verificar que no haya duelo activo ────────────────
        long existingExpiry = CPH.GetGlobalVar<long>("boinacoin_duel_expiry", true);
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (existingExpiry > nowUnix)
        {
            string existingChallenger = CPH.GetGlobalVar<string>("boinacoin_duel_challengerName", true) ?? "";
            SendRandomMessage(ERR_DUEL_ACTIVE_POOLS, challengerName, 0, 0, existingChallenger);
            return true;
        }

        // ── Verificar saldo del retador ───────────────────────
        long challengerBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");
        if (challengerBalance < amount)
        {
            SendRandomMessage(ERR_FUNDS_POOLS, challengerName, 0, 0, "", challengerBalance);
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
            SendRandomMessage(ERR_RIVAL_FUNDS_POOLS, challengerName, 0, 0, "", targetBalance, targetName);
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
        SendRandomMessage(DUEL_ANNOUNCE_POOLS, challengerName, amount, 0, "", 0, targetName, DUEL_TIMEOUT_SECS);

        return true;
    }

    private bool ResolveBotDuel(string challengerId, string challengerName, long amount)
    {
        if (CPH.GetGlobalVar<bool>("boinabot_is_busy", false))
        {
            SendRandomMessage(BOT_BUSY_MESSAGES, challengerName);
            return true;
        }

        CPH.SetGlobalVar("boinabot_is_busy", true, false);

        try
        {
            SendRandomMessage(BOT_CHALLENGE_POOLS, challengerName, amount);

            // Simular pensamiento
            Thread.Sleep(3000);

            Random rnd = new Random();

            // 1. ¿Acepta el bot? (50/50)
            if (rnd.Next(0, 2) == 0)
            {
                SendRandomMessage(BOT_REJECT_MESSAGES, challengerName, amount);
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

                SendRandomMessage(BOT_LOSE_POOLS, challengerName, amount, 0, "", newBalance);
                CheckRankChange(challengerId, challengerName, newBalance);
                TrackSessionDuel(challengerName, amount, true); // true = emitted
            }
            else
            {
                TrackSessionDuel("BoinaBot", amount, false);
                long newBalance = challengerOldBalance - amount;
                CPH.SetKickUserVarById(challengerId, "boinacoin", newBalance, true);
                CPH.SetKickUserVarById(challengerId, "boinacoin_last_seen", nowUnix, true);

                SendRandomMessage(BOT_WIN_POOLS, challengerName, amount, 0, "", newBalance);
                CheckRankChange(challengerId, challengerName, newBalance);
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
            SendRandomMessage(ERR_NO_ACTIVE_DUEL_POOLS, acceptorName);
            return true;
        }

        // ── ¿Es el retado quien acepta? ───────────────────────
        string targetName = (CPH.GetGlobalVar<string>("boinacoin_duel_targetName", true) ?? "").ToLower().Trim().Replace("@", "");
        string challengerId = CPH.GetGlobalVar<string>("boinacoin_duel_challengerId", true) ?? "";
        string challengerName = (CPH.GetGlobalVar<string>("boinacoin_duel_challengerName", true) ?? "").ToLower().Trim().Replace("@", "");
        long amount = CPH.GetGlobalVar<long>("boinacoin_duel_amount", true);

        string acceptorNameClean = acceptorName.ToLower().Trim().Replace("@", "");

        if (acceptorNameClean != targetName)
        {
            SendRandomMessage(ERR_WRONG_RIVAL_POOLS, acceptorName, 0, 0, challengerName, 0, targetName);
            return true;
        }

        // ── Verificar saldos actuales antes de resolver ───────
        long challengerBalance = CPH.GetKickUserVarById<long>(challengerId, "boinacoin");
        long targetBalance = CPH.GetKickUserVarById<long>(acceptorId, "boinacoin");

        if (challengerBalance < amount)
        {
            SendRandomMessage(ERR_CANCELLED_FUNDS_POOLS, challengerName);
            ClearDuel();
            return true;
        }
        if (targetBalance < amount)
        {
            SendRandomMessage(ERR_CANCELLED_FUNDS_POOLS, acceptorName);
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

        long winnerNewBalance = winnerOldBalance + amount;
        long loserNewBalance = loserOldBalance - amount;

        // ── Transferencia ─────────────────────────────────────
        CPH.SetKickUserVarById(winnerId, "boinacoin", winnerNewBalance, true);
        CPH.SetKickUserVarById(loserId, "boinacoin", loserNewBalance, true);

        long winnerTotal = CPH.GetKickUserVarById<long>(winnerId, "boinacoin_total_earned") + amount;
        CPH.SetKickUserVarById(winnerId, "boinacoin_total_earned", winnerTotal, true);

        CPH.SetKickUserVarById(winnerId, "boinacoin_last_seen", nowUnix, true);
        CPH.SetKickUserVarById(loserId, "boinacoin_last_seen", nowUnix, true);

        // ── Comprobar rangos (Ganador y Perdedor) ──────────────
        CheckRankChange(winnerId, winnerName, winnerNewBalance);
        CheckRankChange(loserId, loserName, loserNewBalance);

        // ── Anuncio del resultado ─────────────────────────────
        SendRandomMessage(DUEL_RESULT_POOLS, winnerName, amount, 0, loserName, winnerNewBalance, winnerName, 0, loserNewBalance);

        TrackSessionDuel(winnerName, amount, false); // false = transferred

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

    private void TrackSessionDuel(string winnerName, long amount, bool isEmitted)
    {
        // Total duels
        long totalDuels = CPH.GetGlobalVar<long>("boinacoin_session_duels_total", false) + 1;
        CPH.SetGlobalVar("boinacoin_session_duels_total", totalDuels, false);

        // Duel winners tracking
        string winnersJson = CPH.GetGlobalVar<string>("boinacoin_session_duels_winners", false) ?? "{}";
        var winners = JsonConvert.DeserializeObject<Dictionary<string, int>>(winnersJson) ?? new Dictionary<string, int>();
        winners[winnerName] = winners.ContainsKey(winnerName) ? winners[winnerName] + 1 : 1;
        CPH.SetGlobalVar("boinacoin_session_duels_winners", JsonConvert.SerializeObject(winners), false);

        // BoinaBot check
        if (winnerName == "BoinaBot") return;

        // Leaderboard for coins (only for the winner)
        if (isEmitted)
        {
            long sEarned = CPH.GetGlobalVar<long>("boinacoin_session_earned", false) + amount;
            CPH.SetGlobalVar("boinacoin_session_earned", sEarned, false);
        }

        string lbJson = CPH.GetGlobalVar<string>("boinacoin_session_leaderboard", false) ?? "{}";
        var lb = JsonConvert.DeserializeObject<Dictionary<string, long>>(lbJson) ?? new Dictionary<string, long>();
        lb[winnerName] = lb.ContainsKey(winnerName) ? lb[winnerName] + amount : amount;
        var top10 = lb.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value);
        CPH.SetGlobalVar("boinacoin_session_leaderboard", JsonConvert.SerializeObject(top10), false);
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

    private void SendRandomMessage(string[] pool, string challengerName, long amount = 0, long min = 0, string currentChallenger = "", long balance = 0, string targetName = "", int timeout = 0, long loserBalance = 0)
    {
        if (pool == null || pool.Length == 0) return;

        string msg = pool[new Random().Next(pool.Length)];

        msg = msg.Replace("@{challengerName}", "@" + (challengerName ?? "alguien").Replace("@", ""));
        msg = msg.Replace("{amount}", amount.ToString());
        msg = msg.Replace("{min}", min.ToString());
        msg = msg.Replace("@{currentChallenger}", "@" + (currentChallenger ?? "").Replace("@", ""));
        msg = msg.Replace("{balance}", balance.ToString());
        msg = msg.Replace("@{targetName}", "@" + (targetName ?? "").Replace("@", ""));
        msg = msg.Replace("{timeout}", timeout.ToString());
        msg = msg.Replace("@{winnerName}", "@" + (challengerName ?? "").Replace("@", ""));
        msg = msg.Replace("@{loserName}", "@" + (currentChallenger ?? "").Replace("@", ""));
        msg = msg.Replace("{winnerBalance}", balance.ToString());
        msg = msg.Replace("{loserBalance}", loserBalance.ToString());

        CPH.SendKickMessage(msg);
    }
}
