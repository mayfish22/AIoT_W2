using Microsoft.AspNetCore.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text.Json;
using Serilog;
using WebServer.Models.AIoTDB;
using System.Security.Claims;

namespace WebServer.Hubs;

public class ChatHub : Hub
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AIoTDBContext _aiot;
    public ChatHub(IHttpContextAccessor httpContextAccessor, AIoTDBContext aiot)
    {
        _httpContextAccessor = httpContextAccessor;
        _aiot = aiot;
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task SendMessageForUser(string connectionId, string message)
    {
        await Clients.Client(connectionId).SendAsync("ReceiveMessageForUser", message);
    }
    /// <summary>
    /// 連線
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
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
                    {
                        var userProfile = await _aiot.User.FindAsync(userId);
                        Log.Information($"User Account：{userProfile.Account}");
                    } 
                }
            }

            Log.Information($"ConnectionId：{Context.ConnectionId}");

        }
        catch (Exception ex)
        {
            Log.Error(nameof(ChatHub), ex);
        }
    }

    /// <summary>
    /// 斷線
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public override Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            //Do something
        }
        catch (Exception ex)
        {
            Log.Error(nameof(ChatHub), ex);
        }
        return base.OnDisconnectedAsync(exception);
    }
}