using System.Security.Claims;

namespace PresentationLayer.Helpers
{
    public static class JwtHelper
    {
        public static Guid? GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }
            return userId;
        }

        public static string? GetUsername(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }

        public static string? GetUserRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}





