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

            // Score-based detection for better accuracy
            int saleScore = 0;
            int purchaseScore = 0;

            // Direct invoice type indicators (high weight)
            if (lowerText.Contains("satış faturası") || lowerText.Contains("sales invoice")) saleScore += 50;
            if (lowerText.Contains("alış faturası") || lowerText.Contains("purchase invoice")) purchaseScore += 50;
            
            // General type indicators (medium weight)
            if (lowerText.Contains("satış") || lowerText.Contains("sale")) saleScore += 20;
            if (lowerText.Contains("alış") || lowerText.Contains("purchase")) purchaseScore += 20;
            
            // Customer/supplier indicators (medium weight)
            if (lowerText.Contains("müşteri") || lowerText.Contains("customer")) saleScore += 15;
            if (lowerText.Contains("tedarikçi") || lowerText.Contains("supplier") || lowerText.Contains("vendor")) purchaseScore += 15;
            
            // Receipt/invoice patterns (low weight)
            if (lowerText.Contains("fiş") || lowerText.Contains("receipt")) saleScore += 10;
            if (lowerText.Contains("fatura") || lowerText.Contains("invoice")) 
            {
                // Generic invoice - slight bias toward purchase if no other indicators
                purchaseScore += 5;
            }
            
            // Direction indicators (medium weight)
            if (lowerText.Contains("satılan") || lowerText.Contains("sold")) saleScore += 15;
            if (lowerText.Contains("satın alınan") || lowerText.Contains("purchased")) purchaseScore += 15;
            
            // Payment direction (low weight)
            if (lowerText.Contains("tahsil") || lowerText.Contains("collection")) saleScore += 5;
            if (lowerText.Contains("ödeme") || lowerText.Contains("payment")) purchaseScore += 5;

            // Return type handling
            if (lowerText.Contains("iade") || lowerText.Contains("return"))
            {
                if (saleScore > purchaseScore)
                    return InvoiceType.SaleReturn;
                else
                    return InvoiceType.PurchaseReturn;
            }

            // Determine final type based on scores
            if (saleScore > purchaseScore)
                return InvoiceType.Sale;
            else if (purchaseScore > saleScore)
                return InvoiceType.Purchase;
            else
                // If scores are equal, default to purchase
                return InvoiceType.Purchase;
        }

        public List<InvoiceItem> ExtractItems(string text)
        {
            var items = new List<InvoiceItem>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            bool inItemSection = false;
            
            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                
                // Skip empty lines and headers
                if (string.IsNullOrWhiteSpace(cleanLine))
                    continue;
                    
                // Detect start of items section
                if (cleanLine.ToLower().Contains("ürün") || cleanLine.ToLower().Contains("malzeme") ||
                    cleanLine.ToLower().Contains("açıklama") || cleanLine.ToLower().Contains("miktar") ||
                    cleanLine.ToLower().Contains("birim") || cleanLine.ToLower().Contains("fiyat"))
                {
                    inItemSection = true;
                    continue;
                }
                
                // Detect end of items section
                if (cleanLine.ToLower().Contains("toplam") || cleanLine.ToLower().Contains("kdv") ||
                    cleanLine.ToLower().Contains("total") || cleanLine.ToLower().Contains("vat"))
                {
                    inItemSection = false;
                    continue;
                }
                
                // Parse item if we're in the items section or if line looks like an item
                if (inItemSection || HasItemPattern(cleanLine))
                {
                    var item = TryParseItemLine(cleanLine);
                    if (item != null && !string.IsNullOrWhiteSpace(item.ProductName))
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }
        
        private bool HasItemPattern(string line)
        {
            // Check if line contains patterns typical of item lines
            var numberCount = Regex.Matches(line, @"\d+[.,]?\d*").Count;
            return numberCount >= 2 && line.Length > 10 && 
                   !line.ToLower().Contains("sayfa") && 
                   !line.ToLower().Contains("tarih");
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
                // Turkish patterns with better number matching
                @"Genel\s*Toplam[:\s]*([\d.,]+)\s*TL",
                @"Toplam[:\s]*([\d.,]+)\s*TL",
                @"TOPLAM[:\s]*([\d.,]+)\s*TL",
                @"Total[:\s]*([\d.,]+)\s*TL",
                @"Ödenecek\s*Tutar[:\s]*([\d.,]+)\s*TL",
                
                // Patterns without TL suffix
                @"Genel\s*Toplam[:\s]*([\d.,]{3,})",
                @"Toplam[:\s]*([\d.,]{3,})",
                @"TOPLAM[:\s]*([\d.,]{3,})",
                @"Total[:\s]*([\d.,]{3,})",
                
                // Last resort - any number followed by TL
                @"([\d.,]+)\s*TL\s*$"
            };

            decimal maxAmount = 0;
            
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        var amount = ParseDecimal(match.Groups[1].Value);
                        if (amount > maxAmount)
                        {
                            maxAmount = amount;
                        }
                    }
                }
            }

            return maxAmount;
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
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            try
            {
                // Clean the value
                value = value.Trim().Replace(" ", "");
                
                // Handle Turkish number format (thousands separator: . or ,, decimal separator: , or .)
                // Example: 1.234,56 or 1,234.56 or 54760 or 54.760
                
                // If there are multiple separators, the last one is decimal
                var lastDotIndex = value.LastIndexOf('.');
                var lastCommaIndex = value.LastIndexOf(',');
                
                if (lastDotIndex > 0 && lastCommaIndex > 0)
                {
                    // Both dot and comma present
                    if (lastDotIndex > lastCommaIndex)
                    {
                        // Dot is decimal separator: 1,234.56
                        value = value.Replace(",", "");
                    }
                    else
                    {
                        // Comma is decimal separator: 1.234,56
                        value = value.Substring(0, lastCommaIndex).Replace(".", "") + "." + value.Substring(lastCommaIndex + 1);
                    }
                }
                else if (lastCommaIndex > 0)
                {
                    // Only comma present
                    var beforeComma = value.Substring(0, lastCommaIndex);
                    var afterComma = value.Substring(lastCommaIndex + 1);
                    
                    if (afterComma.Length <= 2)
                    {
                        // Comma is decimal separator: 54,760 -> 54.760
                        value = beforeComma + "." + afterComma;
                    }
                    else
                    {
                        // Comma is thousands separator: 54,760 -> 54760
                        value = value.Replace(",", "");
                    }
                }
                else if (lastDotIndex > 0)
                {
                    // Only dot present
                    var beforeDot = value.Substring(0, lastDotIndex);
                    var afterDot = value.Substring(lastDotIndex + 1);
                    
                    if (afterDot.Length > 2)
                    {
                        // Dot is thousands separator: 54.760 -> 54760
                        value = value.Replace(".", "");
                    }
                    // else: dot is decimal separator, keep as is
                }
                
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
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