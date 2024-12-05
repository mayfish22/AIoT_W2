using Microsoft.EntityFrameworkCore;
using WebServer.Models.AIoTDB;
using WebServer.Services;

namespace WebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //設定連線字串
            builder.Services.AddDbContext<AIoTDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("AIoTDB"));
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
                });

            //注入服務
            builder.Services.AddScoped<ValidatorService>();

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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
