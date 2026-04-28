using System.Text;
using Hangfire.Dashboard;

namespace server_vehicle_parts_ms.Helpers;

public class HangfireDashboardAuthFilter(string username, string password) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var authHeader = http.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = authHeader["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = decoded.Split(':', 2);
                if (parts.Length == 2 && parts[0] == username && parts[1] == password)
                    return true;
            }
            catch
            {
                // fall through to challenge
            }
        }

        http.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire\"";
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }
}
