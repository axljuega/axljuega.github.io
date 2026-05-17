// ============================================================
//  BOINACOIN · system/discord_roles.cs
//  Tipo: acción interna (llamada desde rank_checker.cs)
//
//  Asigna automáticamente el rol de Discord correspondiente
//  al nuevo rango del usuario, y elimina los rangos anteriores
//  para que solo lleve uno activo.
//
//  Requiere:
//    · GestorDeBoinas en el servidor con permiso Manage Roles
//    · El rol de GestorDeBoinas por encima de los 4 roles de
//      Boina en la jerarquía del servidor
//
//  Args que recibe de rank_checker.cs:
//    · webhookUserId   → userId del usuario en Kick
//    · webhookUserName → nombre del usuario en Kick
//    · webhookNewRank  → nuevo rango (int 1-4)
//
//  IMPORTANTE: Para vincular el userId de Kick con el userId
//  de Discord necesitas que el usuario haya usado el comando
//  !vincular (pendiente de implementar). Mientras tanto,
//  el script busca por nombre de usuario exacto.
//
//  Cómo configurarlo en Streamer.bot:
//    Acción "Boinacoin · DiscordRoles"
//    Sub-action: Execute C# (este script)
//    Llamada desde rank_checker.cs via CPH.RunAction
// ============================================================

using System;
using System.Net.Http;
using System.Text;

public class CPHInline
{
    // ── Configuración ─────────────────────────────────────────
    // Sustituye BOT_TOKEN por el token real de GestorDeBoinas
    private const string BOT_TOKEN = "BOT_TOKEN_PLACEHOLDER";
    private const string GUILD_ID  = "GUILD_ID_PLACEHOLDER";

    // IDs de los 4 roles de Boina en Discord
    private const string ROLE_LANA       = "ROLE_ID_PLACEHOLDER";
    private const string ROLE_CUERO      = "ROLE_ID_PLACEHOLDER";
    private const string ROLE_TERCIOPELO = "ROLE_ID_PLACEHOLDER";
    private const string ROLE_LEGENDARIA = "ROLE_ID_PLACEHOLDER";

    // Todos los roles de rango (para limpiar los anteriores)
    private static readonly string[] ALL_RANK_ROLES =
    {
        ROLE_LANA, ROLE_CUERO, ROLE_TERCIOPELO, ROLE_LEGENDARIA
    };

    private static readonly string[] ROLE_BY_RANK =
    {
        "",               // rango 0 — sin rol
        ROLE_LANA,        // rango 1
        ROLE_CUERO,       // rango 2
        ROLE_TERCIOPELO,  // rango 3
        ROLE_LEGENDARIA   // rango 4
    };

    // ────────────────────────────────────────────────────────
    public bool Execute()
    {
        string kickUserName = args.ContainsKey("webhookUserName")
            ? args["webhookUserName"].ToString() : "";
        int newRank = args.ContainsKey("webhookNewRank")
            ? Convert.ToInt32(args["webhookNewRank"]) : 0;

        if (string.IsNullOrEmpty(kickUserName) || newRank < 0 || newRank > 4)
        {
            CPH.LogWarn("[DiscordRoles] Args inválidos — abortando.");
            return false;
        }

        // ── 1. Buscar el miembro en Discord por nombre ────────
        // Buscamos por username exacto (case-insensitive)
        string discordUserId = FindDiscordMember(kickUserName);

        if (string.IsNullOrEmpty(discordUserId))
        {
            CPH.LogWarn($"[DiscordRoles] {kickUserName} no encontrado en Discord. " +
                        "Necesita unirse al servidor o usar !vincular cuando esté disponible.");
            return false;
        }

        CPH.LogInfo($"[DiscordRoles] {kickUserName} → Discord ID: {discordUserId} → Rango {newRank}");

        // ── 2. Quitar todos los roles de rango anteriores ─────
        foreach (var roleId in ALL_RANK_ROLES)
        {
            RemoveRole(discordUserId, roleId);
        }

        // ── 3. Asignar el nuevo rol (si newRank > 0) ──────────
        bool assigned = true;
        if (newRank > 0)
        {
            string newRoleId = ROLE_BY_RANK[newRank];
            assigned = AddRole(discordUserId, newRoleId);

            if (assigned)
                CPH.LogInfo($"[DiscordRoles] Rol asignado correctamente a {kickUserName}.");
            else
                CPH.LogWarn($"[DiscordRoles] Falló la asignación de rol a {kickUserName}.");
        }
        else
        {
            CPH.LogInfo($"[DiscordRoles] Todos los roles eliminados para {kickUserName} (Rango 0).");
        }

        // ── 4. Guardar el Discord ID vinculado al usuario ─────
        // Para futuras llamadas sin necesidad de buscar de nuevo
        string kickUserId = args.ContainsKey("webhookUserId")
            ? args["webhookUserId"].ToString() : "";
        if (!string.IsNullOrEmpty(kickUserId))
        {
            CPH.SetKickUserVarById(kickUserId, "boinacoin_discord_id", discordUserId, true);
        }

        return assigned;
    }

    // ── Buscar miembro en Discord por username ────────────────
    // Usa la API de Discord: GET /guilds/{guild_id}/members/search
    private string FindDiscordMember(string username)
    {
        // Primero intentar con el ID guardado en variable de usuario
        string kickUserId = args.ContainsKey("webhookUserId")
            ? args["webhookUserId"].ToString() : "";

        if (!string.IsNullOrEmpty(kickUserId))
        {
            string savedDiscordId = CPH.GetKickUserVarById<string>(kickUserId, "boinacoin_discord_id", true);
            if (!string.IsNullOrEmpty(savedDiscordId))
            {
                CPH.LogInfo($"[DiscordRoles] Discord ID cacheado para {username}: {savedDiscordId}");
                return savedDiscordId;
            }
        }

        // Buscar por nombre via API
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("Authorization", $"Bot {BOT_TOKEN}");

                string url = $"https://discord.com/api/v10/guilds/{GUILD_ID}/members/search" +
                             $"?query={Uri.EscapeDataString(username)}&limit=5";

                var response = client.GetAsync(url).GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    CPH.LogWarn($"[DiscordRoles] Search HTTP {(int)response.StatusCode}");
                    return "";
                }

                string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // Parse minimalista: buscar "id":"..." dentro del primer resultado
                // que tenga un username que coincida
                string lowerBody     = body.ToLower();
                string lowerUsername = username.ToLower();

                // Buscar el bloque que contenga el username
                int userIdx = lowerBody.IndexOf("\"username\":\"" + lowerUsername + "\"");
                if (userIdx < 0)
                {
                    // Intentar con global_name
                    userIdx = lowerBody.IndexOf("\"global_name\":\"" + lowerUsername + "\"");
                }
                if (userIdx < 0)
                {
                    CPH.LogWarn($"[DiscordRoles] {username} no encontrado en los resultados de búsqueda.");
                    return "";
                }

                // Retroceder para encontrar el "id" de ese usuario
                // El JSON de member tiene estructura: {"user":{"id":"...","username":"..."}}
                int searchFrom = Math.Max(0, userIdx - 500);
                string segment = body.Substring(searchFrom, userIdx - searchFrom + 200);

                int idIdx = segment.LastIndexOf("\"id\":\"");
                if (idIdx < 0) return "";

                int idStart = idIdx + 6;
                int idEnd   = segment.IndexOf('"', idStart);
                if (idEnd < 0) return "";

                return segment.Substring(idStart, idEnd - idStart);
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[DiscordRoles] Error buscando miembro: {ex.Message}");
            return "";
        }
    }

    // ── Asignar rol a un miembro ──────────────────────────────
    // PUT /guilds/{guild_id}/members/{user_id}/roles/{role_id}
    private bool AddRole(string userId, string roleId)
    {
        return ModifyRole(userId, roleId, isPut: true);
    }

    // ── Quitar rol de un miembro ──────────────────────────────
    // DELETE /guilds/{guild_id}/members/{user_id}/roles/{role_id}
    private bool RemoveRole(string userId, string roleId)
    {
        return ModifyRole(userId, roleId, isPut: false);
    }

    private bool ModifyRole(string userId, string roleId, bool isPut)
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("Authorization", $"Bot {BOT_TOKEN}");
                client.DefaultRequestHeaders.Add("X-Audit-Log-Reason", "Boinacoin rank update");

                string url = $"https://discord.com/api/v10/guilds/{GUILD_ID}/members/{userId}/roles/{roleId}";

                HttpResponseMessage response;
                if (isPut)
                {
                    // PUT con body vacío
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    response = client.PutAsync(url, content).GetAwaiter().GetResult();
                }
                else
                {
                    response = client.DeleteAsync(url).GetAwaiter().GetResult();
                }

                // 204 No Content = éxito en ambos casos
                // 404 en DELETE = el usuario no tenía el rol, no es error
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return true;
                if (!isPut && response.StatusCode == System.Net.HttpStatusCode.NotFound) return true;

                CPH.LogWarn($"[DiscordRoles] {(isPut ? "PUT" : "DELETE")} rol {roleId} → HTTP {(int)response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"[DiscordRoles] Error modificando rol: {ex.Message}");
            return false;
        }
    }
}
