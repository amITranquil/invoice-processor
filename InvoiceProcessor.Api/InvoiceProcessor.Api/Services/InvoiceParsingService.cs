using InvoiceProcessor.Api.Data.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace InvoiceProcessor.Api.Services
{
    public class InvoiceParsingService : IInvoiceParsingService
    {
        public async Task<Invoice> ParseInvoiceAsync(string text, string fileName)
        {
            return await Task.Run(() =>
            {
                var invoice = new Invoice
                {
                    FileName = fileName,
                    Type = DetectInvoiceType(text),
                    ProcessedDate = DateTime.Now,
                    Status = ProcessingStatus.PendingReview
                };

                // Temel bilgileri çıkar
                invoice.InvoiceNumber = ExtractInvoiceNumber(text);
                invoice.InvoiceDate = ExtractInvoiceDate(text);
                invoice.SupplierName = ExtractSupplierName(text);
                invoice.TotalAmount = ExtractTotalAmount(text);
                invoice.VatAmount = ExtractVatAmount(text);

                // Ürünleri çıkar
                var items = ExtractItems(text);
                foreach (var item in items)
                {
                    invoice.Items.Add(item);
                }

                // Güven skoru hesapla
                invoice.ConfidenceScore = CalculateConfidenceScore(invoice, text);

                return invoice;
            });
        }

        public InvoiceType DetectInvoiceType(string text)
        {
            var lowerText = text.ToLower();

            if (lowerText.Contains("satış") || lowerText.Contains("satış faturası"))
                return InvoiceType.Sale;

            if (lowerText.Contains("alış") || lowerText.Contains("alış faturası"))
                return InvoiceType.Purchase;

            if (lowerText.Contains("iade"))
            {
                if (lowerText.Contains("satış iade"))
                    return InvoiceType.SaleReturn;
                return InvoiceType.PurchaseReturn;
            }

            // Varsayılan olarak alış faturası
            return InvoiceType.Purchase;
        }

        public List<InvoiceItem> ExtractItems(string text)
        {
            var items = new List<InvoiceItem>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var item = TryParseItemLine(line);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private InvoiceItem? TryParseItemLine(string line)
        {
            // Basit regex pattern - gerçek uygulamada daha karmaşık olacak
            var pattern = @"(.+?)\s+(\d+(?:[.,]\d+)?)\s+(\w+)\s+(\d+(?:[.,]\d+)?)\s+(\d+(?:[.,]\d+)?)";
            var match = Regex.Match(line, pattern);

            if (match.Success)
            {
                try
                {
                    var productName = match.Groups[1].Value.Trim();
                    var quantity = ParseDecimal(match.Groups[2].Value);
                    var unit = match.Groups[3].Value.Trim();
                    var unitPrice = ParseDecimal(match.Groups[4].Value);
                    var totalPrice = ParseDecimal(match.Groups[5].Value);

                    return new InvoiceItem
                    {
                        ProductName = productName,
                        Quantity = quantity,
                        Unit = unit,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice,
                        ConfidenceScore = 85
                    };
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private string? ExtractInvoiceNumber(string text)
        {
            var patterns = new[]
            {
                @"Fatura\s*No[:\s]*(\w+)",
                @"Invoice\s*No[:\s]*(\w+)",
                @"Belge\s*No[:\s]*(\w+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        private DateTime? ExtractInvoiceDate(string text)
        {
            var datePattern = @"(\d{1,2})[./](\d{1,2})[./](\d{4})";
            var match = Regex.Match(text, datePattern);

            if (match.Success)
            {
                try
                {
                    var day = int.Parse(match.Groups[1].Value);
                    var month = int.Parse(match.Groups[2].Value);
                    var year = int.Parse(match.Groups[3].Value);

                    return new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private string? ExtractSupplierName(string text)
        {
            var lines = text.Split('\n');
            foreach (var line in lines.Take(10)) // İlk 10 satırda ara
            {
                if (line.Contains("Firma") || line.Contains("Şirket") ||
                    line.Contains("Company") || line.Length > 20)
                {
                    return line.Trim();
                }
            }
            return null;
        }

        private decimal ExtractTotalAmount(string text)
        {
            var patterns = new[]
            {
                @"Toplam[:\s]*(\d+(?:[.,]\d+)?)",
                @"Total[:\s]*(\d+(?:[.,]\d+)?)",
                @"Genel\s*Toplam[:\s]*(\d+(?:[.,]\d+)?)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return ParseDecimal(match.Groups[1].Value);
                }
            }

            return 0;
        }

        private decimal? ExtractVatAmount(string text)
        {
            var patterns = new[]
            {
                @"KDV[:\s]*(\d+(?:[.,]\d+)?)",
                @"VAT[:\s]*(\d+(?:[.,]\d+)?)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return ParseDecimal(match.Groups[1].Value);
                }
            }

            return null;
        }

        private decimal ParseDecimal(string value)
        {
            // Türkçe sayı formatını handle et
            value = value.Replace(',', '.');
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        private int CalculateConfidenceScore(Invoice invoice, string text)
        {
            int score = 50; // Base score

            if (!string.IsNullOrEmpty(invoice.InvoiceNumber)) score += 15;
            if (invoice.InvoiceDate.HasValue) score += 15;
            if (!string.IsNullOrEmpty(invoice.SupplierName)) score += 10;
            if (invoice.TotalAmount > 0) score += 10;
            if (invoice.Items.Any()) score += 20;

            // Text kalitesi kontrolü
            if (text.Length > 100) score += 5;
            if (!text.Contains("?") && !text.Contains("�")) score += 10;

            return Math.Min(100, score);
        }
    }
}