using Microsoft.AspNetCore.Authentication.JwtBearer; // 引入 JWT Bearer 認證相關的命名空間
using Microsoft.AspNetCore.Authorization; // 引入授權相關的命名空間
using Microsoft.AspNetCore.Mvc; // 引入 MVC 控制器相關的命名空間
using Microsoft.EntityFrameworkCore; // 引入 Entity Framework Core 相關的命名空間
using WebServer.Models.AIoTDB; // 引入資料模型命名空間

namespace WebServer.Controllers;

// 設定路由為 "api"，並要求使用 JWT Bearer 認證
[Route("api")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WebApiController : Controller
{
    // 私有只讀變數，用於存取資料庫上下文
    private readonly AIoTDBContext _aiot;

    // 建構函式，用來初始化資料庫上下文
    public WebApiController(AIoTDBContext aiot)
    {
        _aiot = aiot; // 將傳入的資料庫上下文賦值給私有變數
    }

    // 定義一個 HTTP POST 方法，路由為 "api/Test"
    [HttpPost("Test")]
    public async Task<IActionResult> Test()
    {
        // 獲取當前用戶的帳號（從 JWT Token 中提取）
        var account = User.Identity.Name;

        // 使用 LINQ 查詢從資料庫中查找用戶
        var user = await _aiot.User.Where(s => s.Account == account).FirstOrDefaultAsync();

        // 返回用戶的基本資訊作為 JSON 格式的響應
        return Json(new
        {
            name = user.Name, // 用戶姓名
            email = user.Email, // 用戶電子郵件
            mobile = user.Mobile, // 用戶手機號碼
        });
    }
}
