using ClosedXML.Excel;
using ExcelDataReader;
using Microsoft.Extensions.Logging;
using QuoteManagement.Application.DTOs;
using QuoteManagement.Application.Interfaces;
using System.Data;
using System.Text;

namespace QuoteManagement.Infrastructure.Services;

// ─── Excel Service ──────────────────────────────────────────
public class ExcelService : IExcelService
{
    public byte[] GenerateRequestExcel(ExcelRequestDto dto)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Teklif Formu");

        // Header info
        ws.Cell("A1").Value = "TEKLİF FORMU";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:G1").Merge();

        ws.Cell("A2").Value = "Teklif No:";
        ws.Cell("B2").Value = dto.RequestNo;
        ws.Cell("A3").Value = "Müşteri:";
        ws.Cell("B3").Value = dto.CustomerName;
        ws.Cell("A4").Value = "E-posta:";
        ws.Cell("B4").Value = dto.CustomerEmail;
        ws.Cell("A5").Value = "Tarih:";
        ws.Cell("B5").Value = dto.RequestDate.ToString("dd.MM.yyyy HH:mm");

        // Table header — sadece temel bilgiler
        var headerRow = 7;
        ws.Cell(headerRow, 1).Value = "Kalem ID";
        ws.Cell(headerRow, 2).Value = "Ürün ID";
        ws.Cell(headerRow, 3).Value = "Ürün Adı";
        ws.Cell(headerRow, 4).Value = "Kategori";
        ws.Cell(headerRow, 5).Value = "Miktar";

        var headerRange = ws.Range(headerRow, 1, headerRow, 5);
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;

        // Data rows
        var row = headerRow + 1;
        foreach (var item in dto.Items)
        {
            ws.Cell(row, 1).Value = item.ProductId.ToString();
            ws.Cell(row, 2).Value = item.ProductId.ToString();
            ws.Cell(row, 3).Value = item.ProductName;
            ws.Cell(row, 4).Value = item.Category;
            ws.Cell(row, 5).Value = item.Quantity;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public IEnumerable<ExcelImportRowDto> ParseImportExcel(Stream excelStream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var result = new List<ExcelImportRowDto>();

        using var reader = ExcelReaderFactory.CreateReader(excelStream);
        var dataset = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        if (dataset.Tables.Count == 0) return result;
        var table = dataset.Tables[0];

        foreach (DataRow dr in table.Rows)
        {
            try
            {
                if (dr[0] is DBNull || dr[0].ToString() == "Kalem ID") continue;
                var itemId = Guid.Parse(dr[0].ToString()!);
                var productId = Guid.Parse(dr[1].ToString()!);
                // Eski format: col 7=birim fiyat, col 8=indirim — yeni format birim fiyat yok
                // Yeni Excel'de sadece 7 sütun var, birim fiyat yok
                result.Add(new ExcelImportRowDto(itemId, productId, 0, 0));
            }
            catch { /* skip bad rows */ }
        }
        return result;
    }

    /// <summary>Excel dosyasından müşteri email, ad ve ürün listesini okur</summary>
    public ExcelImportResult ParseImportExcelFull(Stream excelStream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(excelStream);
        var dataset = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        if (dataset.Tables.Count == 0)
            return new ExcelImportResult(null, null, new());

        var raw = dataset.Tables[0];

        // Satır 2 (index 1): "Teklif No:", B2
        // Satır 3 (index 2): "Müşteri:", B3
        // Satır 4 (index 3): "E-posta:", B4
        string? customerName = null;
        string? customerEmail = null;

        for (int r = 0; r < Math.Min(raw.Rows.Count, 6); r++)
        {
            var label = raw.Rows[r][0]?.ToString()?.Trim() ?? "";
            var value = raw.Rows[r].ItemArray.Length > 1 ? raw.Rows[r][1]?.ToString()?.Trim() : null;

            if (label.Contains("Müşteri")) customerName = value;
            if (label.Contains("E-posta") || label.Contains("posta")) customerEmail = value;
        }

        // Şimdi ürün satırlarını oku (UseHeaderRow=true ile tekrar parse et)
        var products = new List<ExcelImportProductRow>();
        excelStream.Position = 0;

        using var reader2 = ExcelReaderFactory.CreateReader(excelStream);
        var ds2 = reader2.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        if (ds2.Tables.Count > 0)
        {
            var table = ds2.Tables[0];
            foreach (DataRow dr in table.Rows)
            {
                try
                {
                    var col0 = dr[0]?.ToString();
                    if (string.IsNullOrWhiteSpace(col0) || col0 == "Kalem ID") continue;
                    if (!Guid.TryParse(dr[1]?.ToString(), out var productId)) continue;
                    var productName = dr[2]?.ToString() ?? "";
                    var category = dr[3]?.ToString() ?? "";
                    var qty = int.TryParse(dr[4]?.ToString(), out var q) ? q : 1;

                    products.Add(new ExcelImportProductRow(
                        productId, productName, category, qty,
                        null, null, false, ""));
                }
                catch { /* skip */ }
            }
        }

        return new ExcelImportResult(customerEmail, customerName, products);
    }
}

// ─── Notification Service (Log Simulation) ──────────────────
public class LogNotificationService : INotificationService
{
    private readonly ILogger<LogNotificationService> _logger;
    public LogNotificationService(ILogger<LogNotificationService> logger) => _logger = logger;

    public Task SendQuoteEmailAsync(string toEmail, string customerName, string requestNo,
        byte[] excelAttachment, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "📧 [EMAIL SIMULATION] Teklif gönderiliyor:\n" +
            "  Alıcı : {Email}\n" +
            "  Müşteri: {Name}\n" +
            "  Teklif No: {No}\n" +
            "  Ek boyutu: {Size} bytes\n" +
            "  ✅ E-posta başarıyla gönderildi (simülasyon).",
            toEmail, customerName, requestNo, excelAttachment.Length);
        return Task.CompletedTask;
    }
}
