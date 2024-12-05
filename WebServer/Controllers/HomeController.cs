using System.Diagnostics;
using CsvHelper;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Models;
using WebServer.Models.NorthwindDB;
using WebServer.Models.CustomModels;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Reflection.PortableExecutable;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace WebServer.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly NorthwindDBContext _northwind;

    public HomeController(ILogger<HomeController> logger, NorthwindDBContext northwind)
    {
        _logger = logger;
        _northwind = northwind;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCSV()
    {
        try
        {
            // 從 _northwind.Customers 中查詢客戶資料，並按 CustomerID 排序
            var query = from n1 in _northwind.Customers
                        orderby n1.CustomerID
                        select new CustomerCsvModel
                        {
                            CustomerID = n1.CustomerID,
                            CompanyName = n1.CompanyName,
                            ContactName = n1.ContactName,
                            ContactTitle = n1.ContactTitle,
                            Address = n1.Address,
                            City = n1.City,
                            Region = n1.Region,
                            PostalCode = n1.PostalCode,
                            Country = n1.Country,
                            Phone = n1.Phone,
                            Fax = n1.Fax,
                        };

            // 將查詢轉換為字串（可選，通常用於調試）
            //var queryString = query.ToQueryString();

            // 執行查詢並將結果轉換為列表
            var customers = await query.ToListAsync();

            // 初始化一個空的 byte 陣列以存儲 CSV 檔案的內容
            byte[] fileStream = Array.Empty<byte>();

            // 使用 MemoryStream 來寫入 CSV 檔案
            using (var memoryStream = new MemoryStream())
            {
                // 設定編碼為 Big5（繁體中文編碼）
                using (var streamWriter = new StreamWriter(memoryStream, Encoding.GetEncoding(950)))
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    // 寫入客戶資料到 CSV 檔案
                    csvWriter.WriteRecords(customers);
                }

                // 將 MemoryStream 的內容轉換為 byte 陣列
                fileStream = memoryStream.ToArray();
            }

            // 返回 CSV 檔案作為下載
            return new FileStreamResult(new MemoryStream(fileStream), "application/octet-stream")
            {
                FileDownloadName = $"客戶基本資料.csv", // 設定下載檔案的名稱
            };
        }
        catch (Exception ex)
        {
            // 如果發生錯誤，返回 BadRequest 並顯示錯誤訊息
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportPDF()
    {
        try
        {
            // 從 Northwind 資料庫中查詢客戶，並按客戶ID排序
            var query = from n1 in _northwind.Customers
                        orderby n1.CustomerID
                        select n1;

            // 將查詢結果轉換為陣列
            var customers = await query.ToArrayAsync();

            #region 產生PDF
            // 建立中文字型（msjh.ttf 微軟正黑體）
            BaseFont bfChinese = BaseFont.CreateFont(Path.Combine("wwwroot", "fonts", "msjh.ttf"), BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

            // 建立 PDF 檔案的記憶體流
            MemoryStream pdfFileStream = new MemoryStream();

            // 設定紙張大小為 A4 直印
            iTextSharp.text.Document doc1 = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

            // 計算每公分的寬度和高度
            float wcm = (float)Math.Round(doc1.PageSize.Width / (float)21.00 * 1, 3);
            float hcm = (float)Math.Round(doc1.PageSize.Height / (float)29.70 * 1, 3);
            float shiftX = 1 * wcm;

            // 初始化 PdfWriter 並打開文件
            iTextSharp.text.pdf.PdfWriter pdfWriter = iTextSharp.text.pdf.PdfWriter.GetInstance(doc1, pdfFileStream);
            doc1.Open();
            iTextSharp.text.pdf.PdfContentByte cb = pdfWriter.DirectContent;

            int pageRecords = 10; // 每頁顯示10筆資料
            int pages = (customers.Length / pageRecords) + 1;

            // 計算字的高度
            float bodyFontSize = 14;
            var bodyAscentPoint = bfChinese.GetAscentPoint("計算高度用", bodyFontSize);
            var bodyDescentPoint = bfChinese.GetDescentPoint("計算高度用", bodyFontSize);
            var bodyFontHeight = bodyAscentPoint - bodyDescentPoint + 0.05f * hcm;

            // 生成每頁的內容
            for (int i = 0; i < pages; i++)
            {
                // 記錄目前的Y軸位置
                float currentBodyY = doc1.PageSize.Height;
                if (i > 0)
                    doc1.NewPage();

                #region Header
                // 添加頁首
                cb.BeginText();
                cb.SetFontAndSize(bfChinese, 16);
                cb.SetColorFill(new BaseColor(Color.Black));
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER, $"客戶基本資料", doc1.PageSize.Width / 2, doc1.PageSize.Height - 1f * hcm, 0);
                cb.EndText();
                #endregion

                currentBodyY = currentBodyY - 2f * hcm;

                #region Body
                // 畫表格的橫線
                cb.SetLineWidth(0.1f);
                cb.SetColorStroke(new BaseColor(Color.Black));
                cb.MoveTo(shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                cb.Stroke();

                // 添加表格標題
                cb.BeginText();
                cb.SetFontAndSize(bfChinese, bodyFontSize);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"#", shiftX, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"客戶編號", shiftX + 2f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"公司名稱", shiftX + 4.5f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"國家", shiftX + 13f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"城市", shiftX + 17f * wcm, currentBodyY, 0);
                cb.EndText();

                // 畫表格的橫線
                cb.SetLineWidth(0.1f);
                cb.SetColorStroke(new BaseColor(Color.Black));
                cb.MoveTo(shiftX, currentBodyY - 0.1f * hcm);
                cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY - 0.1f * hcm);
                cb.Stroke();

                currentBodyY -= (bodyFontHeight + 0.1f * hcm);

                // 添加每筆客戶資料
                cb.BeginText();
                for (int j = 0; j < pageRecords && (i * pageRecords + j) < customers.Length; j++)
                {
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{(i * pageRecords + j + 1)}", shiftX, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{customers[i * pageRecords + j].CustomerID}", shiftX + 2f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{customers[i * pageRecords + j].CompanyName}", shiftX + 4.5f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{customers[i * pageRecords + j].Country}", shiftX + 13f * wcm, currentBodyY, 0);
                    cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"{customers[i * pageRecords + j].City}", shiftX + 17f * wcm, currentBodyY, 0);

                    currentBodyY -= bodyFontHeight;
                }
                cb.EndText();
                #endregion

                #region Footer
                // 添加頁尾
                float footerFontSize = 8;
                cb.SetColorFill(new BaseColor(Color.Black));
                cb.SetFontAndSize(bfChinese, footerFontSize);
                float footerY = currentBodyY - 0.1f * hcm;
                var sCurrentDT = $" 列印日期/時間：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 第{i + 1}頁/共{pages}頁 ----";
                // 計算字的寬度
                var sCurrentDT_width = bfChinese.GetWidthPoint(sCurrentDT, footerFontSize);
                var dash_width = bfChinese.GetWidthPoint("-", footerFontSize);
                var dashCount = Convert.ToInt32((doc1.PageSize.Width - 2 * shiftX - sCurrentDT_width) / dash_width);
                cb.BeginText();
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_RIGHT, new String('-', dashCount) + sCurrentDT, doc1.PageSize.Width - shiftX, footerY, 0);
                cb.EndText();
                #endregion
            }
            doc1.Close();
            #endregion

            var result = Array.Empty<byte>();
            // 加密PDF檔案
            using (MemoryStream output = new MemoryStream())
            {
                PdfReader reader = new PdfReader(pdfFileStream.ToArray());
                // 設定密碼 123456
                PdfEncryptor.Encrypt(reader, output, PdfWriter.ENCRYPTION_AES_128, "123456", null, PdfWriter.AllowPrinting);
                result = output.ToArray();
            }
            // 返回PDF檔案給用戶下載
            return new FileStreamResult(new MemoryStream(result), "application/pdf")
            {
                FileDownloadName = $"客戶基本資料.pdf",
            };
        }
        catch (Exception ex)
        {
            // 如果發生錯誤，返回 BadRequest 並顯示錯誤訊息
            return BadRequest(ex.Message);
        }
    }

}
