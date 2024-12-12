using Microsoft.IdentityModel.Tokens; // 引入 JWT 所需的安全性模型
using System.IdentityModel.Tokens.Jwt; // 引入 JWT 相關的類別
using System.Security.Claims; // 引入聲明 (Claims) 相關的類別
using System.Text; // 引入字串編碼相關的類別

namespace WebSite.Services;

// JwtService 類別負責生成 JWT Token
public class JwtService
{
    private readonly int _timeout; // Token 的有效時間（秒）
    private readonly string _issuer; // JWT 的發行者
    private readonly string _audience; // JWT 的受眾
    private readonly string _signKey; // 用於簽署 JWT 的密鑰

    // JwtService 的建構函數，從配置中讀取 JWT 設定
    public JwtService(IConfiguration configuration)
    {
        _timeout = configuration.GetValue<int>("JwtSettings:TokenTimeout"); // 讀取 Token 超時設定
        _issuer = configuration.GetValue<string>("JwtSettings:Issuer"); // 讀取發行者設定
        _audience = configuration.GetValue<string>("JwtSettings:Audience"); // 讀取受眾設定
        _signKey = configuration.GetValue<string>("JwtSettings:SignKey"); // 讀取簽名金鑰設定
    }

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    /// <param name="userName">用戶名稱</param>
    /// <param name="timeout">可選的 Token 有效時間（秒），如果未提供則使用預設值</param>
    /// <returns>序列化後的 JWT Token 字串</returns>
    public string GenerateToken(string userName, int? timeout = null)
    {
        // 如果未提供 timeout，則使用預設的 _timeout
        timeout ??= _timeout;

        // 設定要加入到 JWT Token 中的聲明資訊 (Claims)
        var claims = new List<Claim>
        {
            // 用戶名稱作為主體聲明
            new Claim(JwtRegisteredClaimNames.Sub, userName), // User.Identity.Name
            // 生成一個唯一的 JWT ID
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID
        };

        // 創建 ClaimsIdentity，將聲明資訊封裝在內
        var userClaimsIdentity = new ClaimsIdentity(claims);

        // 建立一組對稱式加密的金鑰，主要用於 JWT 簽章之用
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signKey));

        // HmacSha256 有要求必須要大於 128 bits，所以 key 不能太短，至少要 16 字元以上
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        // 建立 SecurityTokenDescriptor，描述 Token 的屬性
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer, // 設定發行者
            Audience = _audience, // 設定受眾
            NotBefore = DateTime.Now, // Token 生效的時間，預設為當前時間
            IssuedAt = DateTime.Now, // Token 發行的時間，預設為當前時間
            Subject = userClaimsIdentity, // 設定 Token 的主體
            Expires = DateTime.Now.AddSeconds(timeout.Value), // 設定 Token 的過期時間
            SigningCredentials = signingCredentials // 設定簽名憑證
        };

        // 產出所需要的 JWT securityToken 物件
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        // 將 Token 物件序列化為字串格式
        var serializeToken = tokenHandler.WriteToken(securityToken);

        // 返回序列化後的 Token 字串
        return serializeToken;
    }
}
