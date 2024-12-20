using Microsoft.AspNetCore.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text.Json;
using Serilog;
using WebServer.Models.AIoTDB;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Org.BouncyCastle.Tls;

namespace WebServer.Hubs;

public class ChatHub : Hub
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AIoTDBContext _aiot;
    private readonly IMemoryCache _cache;
    public ChatHub(IHttpContextAccessor httpContextAccessor, AIoTDBContext aiot, IMemoryCache cache)
    {
        _httpContextAccessor = httpContextAccessor;
        _aiot = aiot;
        _cache = cache;
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task SendMessageForAccount(string account, string message)
    {
        if (_cache.TryGetValue((account??string.Empty).Trim().ToUpper(), out string value))
        {
            await Clients.Client(value).SendAsync("ReceiveMessageForAccount", message);
        }
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
                        Log.Information($"ConnectionId：{Context.ConnectionId}");
                        //記錄連線
                        _cache.Set(userProfile.AccountNormalize, Context.ConnectionId);
                    } 
                }
            }
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