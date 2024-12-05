using CsvHelper.Configuration.Attributes;

namespace WebServer.Models.CustomModels;

public class CustomerCsvModel
{
    [Name("客戶編號")]
    [NameIndex(0)]
    public string CustomerID { get; set; }

    [Name("公司名稱")]
    [NameIndex(1)]
    public string CompanyName { get; set; }

    [Name("聯絡人姓名")]
    [NameIndex(2)]
    public string ContactName { get; set; }

    [Name("聯絡人職稱")]
    [NameIndex(3)]
    public string ContactTitle { get; set; }

    [Name("地址")]
    [NameIndex(4)]
    public string Address { get; set; }

    [Name("城市")]
    [NameIndex(5)]
    public string City { get; set; }

    [Name("地區")]
    [NameIndex(6)]
    public string Region { get; set; }

    [Name("郵遞區號")]
    [NameIndex(7)]
    public string PostalCode { get; set; }

    [Name("國家")]
    [NameIndex(8)]
    public string Country { get; set; }

    [Name("電話")]
    [NameIndex(9)]
    public string Phone { get; set; }

    [Name("傳真")]
    [NameIndex(10)]
    public string Fax { get; set; }
}
