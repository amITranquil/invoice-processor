using InvoiceProcessor.Api.Data.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace InvoiceProcessor.Api.Services
{
    public class InvoiceParsingService : IInvoiceParsingService
    {
        public async Task<Invoice> ParseInvoiceAsync(string text, string fileName, string? invoiceType = null)
        {
            Console.WriteLine($"[DEBUG] ParseInvoiceAsync called with fileName: '{fileName}', invoiceType: '{invoiceType}'");
            
            return await Task.Run(() =>
            {
                var detectedType = DetectInvoiceType(text, invoiceType);
                Console.WriteLine($"[DEBUG] DetectInvoiceType returned: {detectedType}");
                
                var invoice = new Invoice
                {
                    FileName = fileName,
                    Type = detectedType,
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

                Console.WriteLine($"[DEBUG] Final invoice created with Type: {invoice.Type}");
                return invoice;
            });
        }

        public InvoiceType DetectInvoiceType(string text, string? hintType = null)
        {
            // Log the hint type for debugging
            Console.WriteLine($"[DEBUG] DetectInvoiceType called with hint: '{hintType}' (IsNullOrEmpty: {string.IsNullOrEmpty(hintType)})");
            
            // Eğer kullanıcı fatura türünü belirttiyse, önce onu dikkate al
            if (!string.IsNullOrEmpty(hintType))
            {
                Console.WriteLine($"[DEBUG] Processing hint type: '{hintType.ToLower()}'");
                
                var detectedType = hintType.ToLower().Trim() switch
                {
                    "purchase" => InvoiceType.Purchase,
                    "sale" => InvoiceType.Sale,
                    "purchase_return" => InvoiceType.PurchaseReturn,
                    "sale_return" => InvoiceType.SaleReturn,
                    _ => DetectFromText(text)
                };
                
                Console.WriteLine($"[DEBUG] Final detected type from hint '{hintType}': {detectedType}");
                return detectedType;
            }

            Console.WriteLine($"[DEBUG] No hint provided, detecting from text");
            var typeFromText = DetectFromText(text);
            Console.WriteLine($"[DEBUG] Final detected type from text: {typeFromText}");
            return typeFromText;
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
            // Clean and normalize the line
            line = CleanItemLine(line);
            if (string.IsNullOrWhiteSpace(line)) return null;

            // Skip non-product lines
            if (IsNonProductLine(line)) return null;

            // Try to extract structured data from the line
            var itemData = ExtractItemData(line);
            if (itemData == null) return null;

            return new InvoiceItem
            {
                ProductCode = itemData.Code,
                ProductName = itemData.Name,
                Quantity = itemData.Quantity,
                Unit = itemData.Unit ?? "adet",
                UnitPrice = itemData.UnitPrice,
                TotalPrice = itemData.TotalPrice,
                ConfidenceScore = itemData.ConfidenceScore
            };
        }

        private string CleanItemLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return string.Empty;

            // Remove excessive whitespace and normalize
            line = Regex.Replace(line.Trim(), @"\s+", " ");
            
            // Remove leading numbers that are likely line numbers
            line = Regex.Replace(line, @"^\d{1,2}\s+", "");
            
            return line;
        }

        private bool IsNonProductLine(string line)
        {
            var lowerLine = line.ToLower();
            
            // Skip header lines and total lines
            var skipKeywords = new[] {
                "toplam", "total", "kdv", "vat", "iskonto", "discount",
                "ödenecek", "tutar", "amount", "tarih", "date", "no:",
                "fatura", "invoice", "sayfa", "page", "ettn:", "tckn:",
                "tel:", "phone", "adres", "address", "bank", "iban",
                "açıklama", "description", "miktar", "quantity",
                "birim", "unit", "fiyat", "price", "not:", "note"
            };

            return skipKeywords.Any(keyword => lowerLine.Contains(keyword)) ||
                   line.Length < 3 ||
                   line.All(char.IsDigit) ||
                   Regex.IsMatch(line, @"^[\d\s.,%-]+$"); // Only numbers, spaces, and symbols
        }

        private ItemData? ExtractItemData(string line)
        {
            // Pattern for Turkish invoice lines: [Number] Product Name [Quantity] [Unit] [UnitPrice] [%Discount] [DiscountAmount] [%VAT] [VATAmount] [TotalPrice]
            // Example: "1 BRIO TANK 040 LT DENGE TANKLI OTOM.HIDROFOR 6 Adet 782 TL %45,00 2.111,40 TL %18,00 464,51 TL 4.692,00"
            
            var patterns = new[]
            {
                // Advanced pattern for Turkish invoices with all details
                @"^(?:\d+\s+)?(.+?)\s+(\d+(?:[.,]\d+)?)\s+(adet|kg|lt|m|cm|ad)\s+(\d+(?:[.,]\d+)?)\s*(?:tl)?\s*(?:%[\d.,]+\s+[\d.,]+\s*(?:tl)?\s*)?(?:%[\d.,]+\s+[\d.,]+\s*(?:tl)?\s*)?([\d.,]+)$",
                
                // Simpler pattern: Product Quantity Unit UnitPrice TotalPrice
                @"^(.+?)\s+(\d+(?:[.,]\d+)?)\s+(adet|kg|lt|m|cm|ad)\s+(\d+(?:[.,]\d+)?)\s*(?:tl)?\s*([\d.,]+)\s*(?:tl)?$",
                
                // Product with code: Code Product Quantity UnitPrice TotalPrice
                @"^([A-Z0-9/-]+)\s+(.+?)\s+(\d+(?:[.,]\d+)?)\s+(\d+(?:[.,]\d+)?)\s*([\d.,]+)$",
                
                // Simple pattern: Product TotalPrice
                @"^(.+?)\s+([\d.,]{3,})\s*(?:tl)?$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    try
                    {
                        if (pattern == patterns[0] || pattern == patterns[1])
                        {
                            var productName = ExtractCleanProductName(match.Groups[1].Value);
                            if (string.IsNullOrWhiteSpace(productName)) continue;

                            return new ItemData
                            {
                                Name = productName,
                                Quantity = ParseDecimal(match.Groups[2].Value),
                                Unit = match.Groups[3].Value.ToLower(),
                                UnitPrice = ParseDecimal(match.Groups[4].Value),
                                TotalPrice = ParseDecimal(match.Groups[5].Value),
                                ConfidenceScore = 90
                            };
                        }
                        else if (pattern == patterns[2])
                        {
                            var productName = ExtractCleanProductName(match.Groups[2].Value);
                            if (string.IsNullOrWhiteSpace(productName)) continue;

                            return new ItemData
                            {
                                Code = match.Groups[1].Value.Trim(),
                                Name = productName,
                                Quantity = ParseDecimal(match.Groups[3].Value),
                                Unit = "adet",
                                UnitPrice = ParseDecimal(match.Groups[4].Value),
                                TotalPrice = ParseDecimal(match.Groups[5].Value),
                                ConfidenceScore = 85
                            };
                        }
                        else if (pattern == patterns[3])
                        {
                            var productName = ExtractCleanProductName(match.Groups[1].Value);
                            if (string.IsNullOrWhiteSpace(productName)) continue;

                            var totalPrice = ParseDecimal(match.Groups[2].Value);
                            return new ItemData
                            {
                                Name = productName,
                                Quantity = 1,
                                Unit = "adet",
                                UnitPrice = totalPrice,
                                TotalPrice = totalPrice,
                                ConfidenceScore = 70
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line: {line}, Error: {ex.Message}");
                        continue;
                    }
                }
            }

            return null;
        }

        private string ExtractCleanProductName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

            // Clean the product name
            rawName = rawName.Trim();
            
            // Remove common prefixes/suffixes that are not part of product name
            rawName = Regex.Replace(rawName, @"^\d+\s*[-.]?\s*", ""); // Remove leading numbers
            rawName = Regex.Replace(rawName, @"\s*tl\s*$", "", RegexOptions.IgnoreCase); // Remove trailing TL
            
            // Remove excessive pricing information mixed in name
            rawName = Regex.Replace(rawName, @"\s+%\d+[.,]\d+.*$", ""); // Remove percentage and following text
            rawName = Regex.Replace(rawName, @"\s+\d+[.,]\d+\s*tl.*$", "", RegexOptions.IgnoreCase); // Remove prices
            
            // Clean up multiple spaces
            rawName = Regex.Replace(rawName, @"\s+", " ").Trim();
            
            // Validate minimum length
            if (rawName.Length < 3) return string.Empty;
            
            // Don't return if it's mostly numbers or special characters
            if (Regex.IsMatch(rawName, @"^[\d\s.,%-]{3,}$")) return string.Empty;
            
            return rawName;
        }

        private class ItemData
        {
            public string? Code { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public string? Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public int ConfidenceScore { get; set; }
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