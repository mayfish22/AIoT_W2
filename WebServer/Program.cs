using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Text;
using WebServer.Hubs;
using WebServer.Models.AIoTDB;
using WebServer.Models.NorthwindDB;
using WebServer.Services;
using WebSite.Services;

namespace WebServer;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            //安裝額外的字碼頁,解決中文亂碼問題
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            #region Serilog

            // 獲取當前環境變數，通常用於判斷應用程序的運行環境（例如開發、測試或生產）
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // 預設配置為 "Release"
            var confg = "Release";

            // 如果在 DEBUG 模式下，將配置設置為 "Debug"
#if DEBUG
            confg = "Debug";
#endif

            // 獲取名為 "AIoTDBLog" 的連接字符串，通常用於連接到數據庫
            var logDB = builder.Configuration.GetConnectionString("AIoTDBLog");

            // 設置 MSSQL Server 日誌接收器的選項
            var sinkOpts = new MSSqlServerSinkOptions
            {
                // 自動創建 SQL 表
                AutoCreateSqlTable = true,
                // 設置日誌表的架構名稱
                SchemaName = "log",
                // 設置日誌表的名稱，包含環境和配置類型
                TableName = $"WebServer_{env}_{confg}"
            };

            // 設置列選項
            var columnOpts = new ColumnOptions();
            // 移除標準列中的屬性列
            columnOpts.Store.Remove(StandardColumn.Properties);
            // 添加日誌事件列
            columnOpts.Store.Add(StandardColumn.LogEvent);
            // 設置日誌事件的數據長度為無限制
            columnOpts.LogEvent.DataLength = -1;
            // 將時間戳設置為非聚集索引
            columnOpts.TimeStamp.NonClusteredIndex = true;

            // 配置 Serilog 日誌記錄器
            Log.Logger = new LoggerConfiguration()
                // 設置最小日誌級別為 Information
                .MinimumLevel.Information()
                // 覆蓋 Microsoft.AspNetCore 的最小日誌級別為 Warning
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                // 覆蓋 Microsoft.EntityFrameworkCore 的最小日誌級別為 Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                // 覆蓋 Microsoft.EntityFrameworkCore.Database.Command 的最小日誌級別為 Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                // 增強日誌信息，添加機器名稱
                .Enrich.WithMachineName()
                // 增強日誌信息，添加環境名稱
                .Enrich.WithEnvironmentName()
                // 增強日誌信息，添加當前用戶名
                .Enrich.WithEnvironmentUserName()
                // 將日誌寫入控制台
                .WriteTo.Console()
                // 將日誌寫入 MSSQL Server，使用指定的連接字符串和選項
                .WriteTo.MSSqlServer(
                    connectionString: logDB,
                    sinkOptions: sinkOpts,
                    columnOptions: columnOpts)
                // 創建日誌記錄器
                .CreateLogger();

            // 啟用 Serilog 的自我日誌，將錯誤信息輸出到控制台
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

            // 使用 Serilog 作為主機的日誌記錄器
            builder.Host.UseSerilog();

            #endregion

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //設定連線字串
            builder.Services.AddDbContext<AIoTDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("AIoTDB"));
            });

            //北風資料庫
            builder.Services.AddDbContext<NorthwindDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("NorthwindDB"));
            });

            // 使用 Session
            // 使用分散式記憶體快取 (Distributed Memory Cache)，它在應用程式範圍內存儲Session資料。
            builder.Services.AddDistributedMemoryCache();

            // 設置 Session 相關的配置
            builder.Services.AddSession(options =>
            {
                // 設定會話的閒置超時時間為 60 分鐘
                options.IdleTimeout = TimeSpan.FromMinutes(60);

                // 設定會話 Cookie 只能由伺服器端讀取，避免 JavaScript 存取
                options.Cookie.HttpOnly = true;

                // 設定會話 Cookie 為必要的，這表示該 Cookie 必須被用戶端儲存並且在每次請求時發送
                options.Cookie.IsEssential = true;
            });

            // 設定應用程式的認證方式，這裡使用 Cookie 驗證方案
            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme) // 設定認證方案使用 Cookie 認證
                .AddCookie(options =>
                {
                    // 設定當存取被拒絕的頁面時，將轉跳至指定的路徑 (例如，無權限存取頁面會導向至這個頁面)
                    options.AccessDeniedPath = new PathString("/Account/Signin");

                    // 設定當使用者未登入時，將轉跳至登入頁面
                    options.LoginPath = new PathString("/Account/Signin");

                    // 設定登出後的轉跳頁面
                    options.LogoutPath = new PathString("/Account/Signout");
                })
                .AddJwtBearer(options =>
                {
                    // 設定 Token 驗證參數
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // 設定時間偏移，預設為 5 分鐘，這裡設為 0，表示不允許時間偏移
                        ClockSkew = TimeSpan.Zero,

                        // 設定用於獲取用戶名稱的聲明類型，這裡使用 "sub" 聲明
                        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",

                        // 設定用於獲取用戶角色的聲明類型，這裡使用 "roles" 聲明
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

                        // 驗證發行者的設定
                        ValidateIssuer = true, // 啟用發行者驗證
                        ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"), // 從配置中獲取有效的發行者

                        // 驗證受眾的設定
                        ValidateAudience = true, // 啟用受眾驗證
                        ValidAudience = builder.Configuration.GetValue<string>("JwtSettings:Audience"), // 從配置中獲取有效的受眾

                        // 驗證 Token 的有效期間
                        ValidateLifetime = true, // 啟用有效期間驗證

                        // 驗證簽名的設定
                        ValidateIssuerSigningKey = true, // 啟用簽名金鑰驗證
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey"))) // 從配置中獲取簽名金鑰
                    };
                });

            //注入服務
            builder.Services.AddScoped<ValidatorService>();

            builder.Services.AddScoped<JwtService>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSignalR();

            //使用 記憶體快取（In-Memory Cache）
            builder.Services.AddMemoryCache(); 

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseSession();

            #region RequestLogging: 使用JSON記錄
            // 設定一個中間件來記錄每個請求的詳細信息
            app.Use(async (httpContext, next) =>
            {
                // 創建一個匿名對象來存儲請求的相關信息
                var message = new
                {
                    // 獲取用戶的 ID，從 Claims 中提取 NameIdentifier 類型的聲明
                    UserID = httpContext.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,

                    // 獲取用戶的名稱，從 Claims 中提取 Name 類型的聲明
                    Account = httpContext.User.Claims.FirstOrDefault(s => s.Type == System.Security.Claims.ClaimTypes.Name)?.Value,

                    // 獲取請求的來源 IP 地址
                    IP = httpContext.Connection.RemoteIpAddress.ToString(),

                    // 獲取請求的方法（GET、POST 等）
                    Method = httpContext.Request.Method,

                    // 獲取請求的協議（http 或 https）
                    Scheme = httpContext.Request.Scheme,

                    // 獲取請求的主機名稱（如 www.example.com）
                    Host = httpContext.Request.Host.HasValue ? httpContext.Request.Host.Value : null,

                    // 獲取請求的路徑
                    Path = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : null,

                    // 獲取請求的查詢字符串
                    QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
                };

                // 將請求信息序列化為 JSON 格式並記錄到日誌中
                Log.Information(System.Text.Json.JsonSerializer.Serialize(new
                {
                    // 設定日誌的動作名稱
                    Action = "RequestLogging",

                    // 將請求的詳細信息作為日誌數據
                    Data = message,
                }));

                // 調用下一個中間件
                await next();
            });
            #endregion

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
