// ============================================================
//  BOINACOIN · system/multiplier.cs
//  Tipo: acción interna de utilidad
//
//  Fuente de verdad única para el cálculo del multiplicador.
//  Centraliza la lógica que en la v1 está duplicada en cada
//  script, facilitando ajustes futuros desde un solo lugar.
//
//  Cómo usarlo desde otro script:
//    CPH.SetArgument("multUserId", userId);
//    CPH.RunAction("Boinacoin · Multiplier", true);  // true = esperar
//    double mult = CPH.GetGlobalVar<double>("boinacoin_calc_mult_" + userId, false);
//    CPH.UnsetGlobalVar("boinacoin_calc_mult_" + userId, false);  // limpiar
//
//  Nota sobre la arquitectura:
//    Streamer.bot no permite importar clases entre acciones C#.
//    Este script resuelve eso escribiendo el resultado en una
//    GlobalVar temporal (no persistida) que el script llamante
//    lee inmediatamente después. La clave usa el userId para
//    evitar colisiones si dos eventos se procesan en paralelo.
//
//  Reglas de multiplicador (todas acumulativas por producto):
//    · Sub activa         x1.5  (6m → x2.0 · 12m → x2.5)
//    · Hora feliz         x2.0  (global, activada por streamer)
//    · Racha 7 streams    x1.5
//    · Racha 30 streams   x2.0  (sustituye al de 7)
//    · Rango Terciopelo   x1.25 (permanente)
//    · Rango Legendaria   x1.5  (permanente, sustituye Terciopelo)
//    · Aniversario canal  x3.0  (global manual, fuera de este script)
// ============================================================

using System;

public class CPHInline
{
    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string userId = args.ContainsKey("multUserId") ? args["multUserId"].ToString() : "";

        if (string.IsNullOrEmpty(userId))
        {
            CPH.LogWarn("[Boinacoin] Multiplier: llamado sin multUserId.");
            return false;
        }

        double multiplier = Calculate(userId);

        // ── Escribir resultado en GlobalVar temporal ──────────
        // No persistida (false) → solo vive en memoria hasta que
        // el script llamante la lea y limpie.
        string resultKey = "boinacoin_calc_mult_" + userId;
        CPH.SetGlobalVar(resultKey, multiplier, false);

        CPH.LogInfo($"[Boinacoin] Multiplier · {userId} → x{multiplier:0.##}");

        return true;
    }

    // ════════════════════════════════════════════════════════
    //  Lógica de cálculo — fuente de verdad única
    // ════════════════════════════════════════════════════════
    public double Calculate(string userId)
    {
        double m = 1.0;

        // ── 1. Multiplicador de suscripción ───────────────────
        // Escrito por sub.cs / resub.cs según los meses acumulados:
        //   Sub activa   → 1.5
        //   ≥ 6 meses    → 2.0
        //   ≥ 12 meses   → 2.5
        double subMult = CPH.GetKickUserVar<double>(userId, "boinacoin_multiplier");
        if (subMult > 1.0) m *= subMult;

        // ── 2. Hora Feliz (global) ────────────────────────────
        bool   horaFeliz = CPH.GetGlobalVar<bool>("boinacoin_horafeliz",        true);
        long   hfExpiry  = CPH.GetGlobalVar<long>("boinacoin_horafeliz_expiry", true);
        long   nowUnix   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (horaFeliz && nowUnix < hfExpiry) m *= 2.0;

        // ── 3. Racha de asistencia a streams ─────────────────
        int streak = CPH.GetKickUserVar<int>(userId, "boinacoin_streak");
        if      (streak >= 30) m *= 2.0;
        else if (streak >= 7)  m *= 1.5;

        // ── 4. Bonus permanente por rango alto ────────────────
        int rank = CPH.GetKickUserVar<int>(userId, "boinacoin_rank");
        if      (rank == 4) m *= 1.5;   // La Boina Legendaria
        else if (rank == 3) m *= 1.25;  // Boina de Terciopelo

        // ── 5. Aniversario del canal (global manual) ──────────
        // La streamer activa: CPH.SetGlobalVar("boinacoin_aniversario", true, false)
        // Se limpia automáticamente al reiniciar Streamer.bot (no persistida).
        bool aniversario = CPH.GetGlobalVar<bool>("boinacoin_aniversario", false);
        if (aniversario) m *= 3.0;

        return m;
    }

    // ════════════════════════════════════════════════════════
    //  Tabla de referencia — todos los multiplicadores posibles
    // ════════════════════════════════════════════════════════
    //
    //  Caso                           Factores          Total
    //  ─────────────────────────────────────────────────────
    //  Sub 12m + HoraFeliz + Ley.     2.5 × 2.0 × 1.5  = x7.5
    //  Sub 12m + Racha30 + Ley.       2.5 × 2.0 × 1.5  = x7.5
    //  Sub 6m  + HoraFeliz + Terc.    2.0 × 2.0 × 1.25 = x5.0
    //  Sub act + Racha7               1.5 × 1.5         = x2.25
    //  Sin sub + HoraFeliz            1.0 × 2.0         = x2.0
    //  Sin sub + sin eventos          1.0               = x1.0
    //
    //  Máximo teórico (sub 12m + HoraFeliz + Racha30 + Ley + Aniv):
    //    2.5 × 2.0 × 2.0 × 1.5 × 3.0 = x45.0
    //    → Considera poner un cap si esto te parece excesivo.
    // ════════════════════════════════════════════════════════
}
