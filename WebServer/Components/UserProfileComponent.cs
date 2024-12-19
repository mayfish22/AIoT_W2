using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using WebServer.Models.AIoTDB;

namespace WebServer.Components
{
    [ViewComponent(Name = "UserProfile")]
    public class UserProfileComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AIoTDBContext _aiot;
        public UserProfileComponent(IHttpContextAccessor httpContextAccessor, AIoTDBContext aiot)
        {
            _httpContextAccessor = httpContextAccessor;
            _aiot = aiot;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            User? userProfile = null;
            try
            {
                // 獲取當前 HttpContext
                var httpContext = _httpContextAccessor.HttpContext;

                // 確保 HttpContext 不為 null
                if (httpContext != null)
                {
                    // 獲取當前使用者的 ClaimsPrincipal
                    var user = httpContext.User;

                    // 確保使用者已登入
                    if (user.Identity.IsAuthenticated)
                    {
                        // 從 Claims 中獲取使用者 ID
                        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                            userProfile = await _aiot.User.FindAsync(userId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(UserProfileComponent), ex);
            }
            return View("Default", userProfile);
        }
    }
}