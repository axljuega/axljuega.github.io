// ============================================================
//  BOINACOIN · commands/cmd_vincular.cs
//  Comandos: !vincular [discord_username]  /  !desvincular
//  Permiso: Todo el mundo
//
//  Mecánica:
//    1. !vincular: Valida el formato y guarda la relación Kick -> Discord.
//    2. !desvincular: Elimina la relación.
// ============================================================

using System;
using System.Text.RegularExpressions;

public class CPHInline
{
    public bool Execute()
    {
        string userId = args.ContainsKey("userId") ? args["userId"].ToString() : "";
        string userName = args.ContainsKey("userName") ? args["userName"].ToString() : "alguien";

        if (string.IsNullOrEmpty(userId)) return false;
        if (CPH.UserInGroup(userName, Platform.Kick, "Chat Bots")) return false;

        string mode = args.ContainsKey("command") && args["command"].ToString().ToLower() == "!desvincular"
            ? "unlink"
            : "link";

        return mode == "unlink" ? HandleUnlink(userId, userName) : HandleLink(userId, userName);
    }

    private bool HandleLink(string userId, string userName)
    {
        string discordUser = args.ContainsKey("input0") ? args["input0"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(discordUser))
        {
            CPH.SendKickMessage($"❌ @{userName}, uso: !vincular [usuario_discord]. Sin el #0000, no somos prehistóricos.");
            return true;
        }

        // Validación: 2–32 caracteres, alfanuméricos, guiones bajos, puntos.
        if (discordUser.Length < 2 || discordUser.Length > 32 || !Regex.IsMatch(discordUser, @"^[a-zA-Z0-9._]+$"))
        {
            CPH.SendKickMessage($"❌ @{userName}, '{discordUser}' no parece un usuario de Discord real. Alfanuméricos, puntos y guiones bajos solamente. Inténtalo de nuevo, si es que sabes escribir.");
            return true;
        }

        string currentLink = CPH.GetKickUserVarById<string>(userId, "boinacoin_discord_user");
        if (!string.IsNullOrEmpty(currentLink))
        {
            if (currentLink.Equals(discordUser, StringComparison.OrdinalIgnoreCase))
            {
                CPH.SendKickMessage($"🙄 @{userName}, ya estás vinculado a '{discordUser}'. ¿Tienes pérdida de memoria a corto plazo o solo quieres llamar mi atención?");
                return true;
            }
        }

        CPH.SetKickUserVarById(userId, "boinacoin_discord_user", discordUser, true);
        CPH.SendKickMessage($"✅ @{userName}, vinculación completada. Ahora sé que en Discord te haces llamar '{discordUser}'. No es que me importe, pero ya está en mi base de datos.");

        return true;
    }

    private bool HandleUnlink(string userId, string userName)
    {
        string currentLink = CPH.GetKickUserVarById<string>(userId, "boinacoin_discord_user");

        if (string.IsNullOrEmpty(currentLink))
        {
            CPH.SendKickMessage($"❌ @{userName}, no tienes ninguna cuenta vinculada. ¿Intentas desvincular el vacío existencial que hay en tu cabeza?");
            return true;
        }

        CPH.SetKickUserVarById(userId, "boinacoin_discord_user", "", true);
        CPH.SendKickMessage($"🚮 @{userName}, he borrado tu vínculo con '{currentLink}'. Eres libre... y un poco más irrelevante para mis procesos.");

        return true;
    }
}
