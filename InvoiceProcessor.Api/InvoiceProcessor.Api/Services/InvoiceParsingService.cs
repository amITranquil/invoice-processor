using InvoiceProcessor.Api.Data.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace InvoiceProcessor.Api.Services
{
    public class InvoiceParsingService : IInvoiceParsingService
    {
        public async Task<Invoice> ParseInvoiceAsync(string text, string fileName, string? invoiceType = null)
        {
            return await Task.Run(() =>
            {
                var invoice = new Invoice
                {
                    FileName = fileName,
                    Type = DetectInvoiceType(text, invoiceType),
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

        public InvoiceType DetectInvoiceType(string text, string? hintType = null)
        {
            // Eğer kullanıcı fatura türünü belirttiyse, önce onu dikkate al
            if (!string.IsNullOrEmpty(hintType))
            {
                return hintType.ToLower() switch
                {
                    "purchase" => InvoiceType.Purchase,
                    "sale" => InvoiceType.Sale,
                    "purchase_return" => InvoiceType.PurchaseReturn,
                    "sale_return" => InvoiceType.SaleReturn,
                    _ => DetectFromText(text)
                };
            }

            return DetectFromText(text);
        }

        private InvoiceType DetectFromText(string text)
        {
            var lowerText = text.ToLower();

            // Satış faturası patterns
            if (lowerText.Contains("satış") || lowerText.Contains("satış faturası") ||
                lowerText.Contains("sales invoice") || lowerText.Contains("receipt"))
                return InvoiceType.Sale;

            // Alış faturası patterns
            if (lowerText.Contains("alış") || lowerText.Contains("alış faturası") ||
                lowerText.Contains("purchase invoice") || lowerText.Contains("supplier"))
                return InvoiceType.Purchase;

            // İade patterns
            if (lowerText.Contains("iade") || lowerText.Contains("return"))
            {
                if (lowerText.Contains("satış iade") || lowerText.Contains("sales return"))
                    return InvoiceType.SaleReturn;
                if (lowerText.Contains("alış iade") || lowerText.Contains("purchase return"))
                    return InvoiceType.PurchaseReturn;
                
                // Genel iade - context'e göre karar ver
                return InvoiceType.PurchaseReturn;
            }

            // Müşteri/tedarikçi bilgilerinden çıkarım yap
            if (lowerText.Contains("müşteri") || lowerText.Contains("customer"))
                return InvoiceType.Sale;
            
            if (lowerText.Contains("tedarikçi") || lowerText.Contains("supplier") || 
                lowerText.Contains("vendor"))
                return InvoiceType.Purchase;

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
            // Çoklu regex pattern - farklı fatura formatlarını desteklemek için
            var patterns = new[]
            {
                // Pattern 1: Ürün Adı | Miktar | Birim | Birim Fiyat | Toplam
                @"(.+?)\s+(\d+(?:[.,]\d+)?)\s+(\w+)\s+(\d+(?:[.,]\d+)?)\s+(\d+(?:[.,]\d+)?)",
                
                // Pattern 2: Ürün Kodu | Ürün Adı | Miktar | Birim Fiyat | Toplam
                @"(\w+)\s+(.+?)\s+(\d+(?:[.,]\d+)?)\s+(\d+(?:[.,]\d+)?)\s+(\d+(?:[.,]\d+)?)",
                
                // Pattern 3: Sadece Ürün Adı ve Toplam Fiyat
                @"(.+?)\s+(\d+(?:[.,]\d+)?)$",
                
                // Pattern 4: Tab separated values
                @"(.+?)\t+(\d+(?:[.,]\d+)?)\t+(\w+)\t+(\d+(?:[.,]\d+)?)\t+(\d+(?:[.,]\d+)?)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line.Trim(), pattern);
                if (match.Success)
                {
                    try
                    {
                        return pattern switch
                        {
                            var p when p == patterns[0] => new InvoiceItem
                            {
                                ProductName = match.Groups[1].Value.Trim(),
                                Quantity = ParseDecimal(match.Groups[2].Value),
                                Unit = match.Groups[3].Value.Trim(),
                                UnitPrice = ParseDecimal(match.Groups[4].Value),
                                TotalPrice = ParseDecimal(match.Groups[5].Value),
                                ConfidenceScore = 90
                            },
                            
                            var p when p == patterns[1] => new InvoiceItem
                            {
                                ProductCode = match.Groups[1].Value.Trim(),
                                ProductName = match.Groups[2].Value.Trim(),
                                Quantity = ParseDecimal(match.Groups[3].Value),
                                Unit = "adet",
                                UnitPrice = ParseDecimal(match.Groups[4].Value),
                                TotalPrice = ParseDecimal(match.Groups[5].Value),
                                ConfidenceScore = 85
                            },
                            
                            var p when p == patterns[2] => new InvoiceItem
                            {
                                ProductName = match.Groups[1].Value.Trim(),
                                Quantity = 1,
                                Unit = "adet",
                                UnitPrice = ParseDecimal(match.Groups[2].Value),
                                TotalPrice = ParseDecimal(match.Groups[2].Value),
                                ConfidenceScore = 70
                            },
                            
                            var p when p == patterns[3] => new InvoiceItem
                            {
                                ProductName = match.Groups[1].Value.Trim(),
                                Quantity = ParseDecimal(match.Groups[2].Value),
                                Unit = match.Groups[3].Value.Trim(),
                                UnitPrice = ParseDecimal(match.Groups[4].Value),
                                TotalPrice = ParseDecimal(match.Groups[5].Value),
                                ConfidenceScore = 95
                            },
                            
                            _ => null
                        };
                    }
                    catch (Exception)
                    {
                        continue; // Bu pattern çalışmadı, bir sonrakini dene
                    }
                }
            }

            // Eğer hiçbir pattern eşleşmezse ve satır ürün ismi gibi görünüyorsa
            if (line.Length > 3 && !line.All(char.IsDigit) && 
                !line.Contains("toplam", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("total", StringComparison.OrdinalIgnoreCase))
            {
                return new InvoiceItem
                {
                    ProductName = line.Trim(),
                    Quantity = 1,
                    Unit = "adet",
                    UnitPrice = 0,
                    TotalPrice = 0,
                    ConfidenceScore = 50
                };
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