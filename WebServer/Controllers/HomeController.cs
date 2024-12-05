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
            // �q _northwind.Customers ���d�߫Ȥ��ơA�ë� CustomerID �Ƨ�
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

            // �N�d���ഫ���r��]�i��A�q�`�Ω�ոա^
            //var queryString = query.ToQueryString();

            // ����d�ߨñN���G�ഫ���C��
            var customers = await query.ToListAsync();

            // ��l�Ƥ@�ӪŪ� byte �}�C�H�s�x CSV �ɮת����e
            byte[] fileStream = Array.Empty<byte>();

            // �ϥ� MemoryStream �Ӽg�J CSV �ɮ�
            using (var memoryStream = new MemoryStream())
            {
                // �]�w�s�X�� Big5�]�c�餤��s�X�^
                using (var streamWriter = new StreamWriter(memoryStream, Encoding.GetEncoding(950)))
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    // �g�J�Ȥ��ƨ� CSV �ɮ�
                    csvWriter.WriteRecords(customers);
                }

                // �N MemoryStream �����e�ഫ�� byte �}�C
                fileStream = memoryStream.ToArray();
            }

            // ��^ CSV �ɮק@���U��
            return new FileStreamResult(new MemoryStream(fileStream), "application/octet-stream")
            {
                FileDownloadName = $"�Ȥ�򥻸��.csv", // �]�w�U���ɮת��W��
            };
        }
        catch (Exception ex)
        {
            // �p�G�o�Ϳ��~�A��^ BadRequest ����ܿ��~�T��
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportPDF()
    {
        try
        {
            // �q Northwind ��Ʈw���d�߫Ȥ�A�ë��Ȥ�ID�Ƨ�
            var query = from n1 in _northwind.Customers
                        orderby n1.CustomerID
                        select n1;

            // �N�d�ߵ��G�ഫ���}�C
            var customers = await query.ToArrayAsync();

            #region ����PDF
            // �إߤ���r���]msjh.ttf �L�n������^
            BaseFont bfChinese = BaseFont.CreateFont(Path.Combine("wwwroot", "fonts", "msjh.ttf"), BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

            // �إ� PDF �ɮת��O����y
            MemoryStream pdfFileStream = new MemoryStream();

            // �]�w�ȱi�j�p�� A4 ���L
            iTextSharp.text.Document doc1 = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

            // �p��C�������e�שM����
            float wcm = (float)Math.Round(doc1.PageSize.Width / (float)21.00 * 1, 3);
            float hcm = (float)Math.Round(doc1.PageSize.Height / (float)29.70 * 1, 3);
            float shiftX = 1 * wcm;

            // ��l�� PdfWriter �å��}���
            iTextSharp.text.pdf.PdfWriter pdfWriter = iTextSharp.text.pdf.PdfWriter.GetInstance(doc1, pdfFileStream);
            doc1.Open();
            iTextSharp.text.pdf.PdfContentByte cb = pdfWriter.DirectContent;

            int pageRecords = 10; // �C�����10�����
            int pages = (customers.Length / pageRecords) + 1;

            // �p��r������
            float bodyFontSize = 14;
            var bodyAscentPoint = bfChinese.GetAscentPoint("�p�Ⱚ�ץ�", bodyFontSize);
            var bodyDescentPoint = bfChinese.GetDescentPoint("�p�Ⱚ�ץ�", bodyFontSize);
            var bodyFontHeight = bodyAscentPoint - bodyDescentPoint + 0.05f * hcm;

            // �ͦ��C�������e
            for (int i = 0; i < pages; i++)
            {
                // �O���ثe��Y�b��m
                float currentBodyY = doc1.PageSize.Height;
                if (i > 0)
                    doc1.NewPage();

                #region Header
                // �K�[����
                cb.BeginText();
                cb.SetFontAndSize(bfChinese, 16);
                cb.SetColorFill(new BaseColor(Color.Black));
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER, $"�Ȥ�򥻸��", doc1.PageSize.Width / 2, doc1.PageSize.Height - 1f * hcm, 0);
                cb.EndText();
                #endregion

                currentBodyY = currentBodyY - 2f * hcm;

                #region Body
                // �e��檺��u
                cb.SetLineWidth(0.1f);
                cb.SetColorStroke(new BaseColor(Color.Black));
                cb.MoveTo(shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY + bodyFontHeight + 0.05f * hcm);
                cb.Stroke();

                // �K�[�����D
                cb.BeginText();
                cb.SetFontAndSize(bfChinese, bodyFontSize);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"#", shiftX, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"�Ȥ�s��", shiftX + 2f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"���q�W��", shiftX + 4.5f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"��a", shiftX + 13f * wcm, currentBodyY, 0);
                cb.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT, $"����", shiftX + 17f * wcm, currentBodyY, 0);
                cb.EndText();

                // �e��檺��u
                cb.SetLineWidth(0.1f);
                cb.SetColorStroke(new BaseColor(Color.Black));
                cb.MoveTo(shiftX, currentBodyY - 0.1f * hcm);
                cb.LineTo(doc1.PageSize.Width - shiftX, currentBodyY - 0.1f * hcm);
                cb.Stroke();

                currentBodyY -= (bodyFontHeight + 0.1f * hcm);

                // �K�[�C���Ȥ���
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
                // �K�[����
                float footerFontSize = 8;
                cb.SetColorFill(new BaseColor(Color.Black));
                cb.SetFontAndSize(bfChinese, footerFontSize);
                float footerY = currentBodyY - 0.1f * hcm;
                var sCurrentDT = $" �C�L���/�ɶ��G{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ��{i + 1}��/�@{pages}�� ----";
                // �p��r���e��
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
            // �[�KPDF�ɮ�
            using (MemoryStream output = new MemoryStream())
            {
                PdfReader reader = new PdfReader(pdfFileStream.ToArray());
                // �]�w�K�X 123456
                PdfEncryptor.Encrypt(reader, output, PdfWriter.ENCRYPTION_AES_128, "123456", null, PdfWriter.AllowPrinting);
                result = output.ToArray();
            }
            // ��^PDF�ɮ׵��Τ�U��
            return new FileStreamResult(new MemoryStream(result), "application/pdf")
            {
                FileDownloadName = $"�Ȥ�򥻸��.pdf",
            };
        }
        catch (Exception ex)
        {
            // �p�G�o�Ϳ��~�A��^ BadRequest ����ܿ��~�T��
            return BadRequest(ex.Message);
        }
    }

}
