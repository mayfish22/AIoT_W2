using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebServer.Models.AIoTDB;

namespace WebServer.Controllers;

[Authorize]
public class CommonController : Controller
{
    private readonly AIoTDBContext _aiot;

    public CommonController(AIoTDBContext aiot)
    {
        _aiot = aiot;
    }

    public class Select2ProcessResults
    {
        [JsonPropertyName("results")]
        public object? Results { get; set; }
        [JsonPropertyName("pagination")]
        public bool Pagination { get; set; }
        [JsonPropertyName("error")]
        public string? ErrorMessage { get; set; }
    }
    public class Select2Result
    {
        [JsonPropertyName("id")]
        public string? ID { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    #region TestRemoteData
    public class TestRemoteDataPara
    {
        //關鍵字查詢
        public string? Parameter { get; set; }
        //分頁頁碼
        public int Page { get; set; }
        //顯示筆數
        public int Rows { get; set; }
    }
    public class TestRemoteDataResult
    {
        [JsonPropertyName("id")]
        public string? ID { get; set; }
        [JsonPropertyName("account")]
        public string? Account { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
    [HttpPost]
    public async Task<IActionResult> TestRemoteData([FromBody] TestRemoteDataPara info)
    {
        try
        {
            //後續資料比對都用小寫
            info.Parameter = (info.Parameter ?? string.Empty).ToUpper();

            var results = from a in _aiot.User
                          where a.AccountNormalize.Contains(info.Parameter)
                            || a.EmailNormalize.Contains(info.Parameter)
                            || a.Name.ToUpper().Contains(info.Parameter)
                          orderby a.Account
                          select new TestRemoteDataResult
                          {
                              ID = a.ID.ToString(),
                              Account = a.Account,
                              Name = a.Name,
                              Email = a.Email,
                          };
            //總筆數
            var nTotalCount = await results.CountAsync();
            //要顯示的起始筆數
            var start = (info.Page - 1) * info.Rows;
            //顯示的筆數
            var r = await results.Skip(start).Take(info.Rows).ToListAsync();
            //是否還有資料
            var p = (nTotalCount - start) > info.Rows;

            return new SystemTextJsonResult(new Select2ProcessResults
            {
                Results = r.Select(s => new Select2Result
                {
                    ID = s.ID,
                    Text = s.Name,
                }),
                Pagination = p
            });
        }
        catch (Exception e)
        {
            return new SystemTextJsonResult(new Select2ProcessResults
            {
                Results = new List<Select2Result>(),
                Pagination = false,
                ErrorMessage = e.Message,
            });
        }
    }
    #endregion
}

public class SystemTextJsonResult : ContentResult
{
    private const string ContentTypeApplicationJson = "application/json";

    public SystemTextJsonResult(object value, JsonSerializerOptions? options = null)
    {
        ContentType = ContentTypeApplicationJson;
        Content = options == null ? JsonSerializer.Serialize(value) : JsonSerializer.Serialize(value, options);
    }
}
