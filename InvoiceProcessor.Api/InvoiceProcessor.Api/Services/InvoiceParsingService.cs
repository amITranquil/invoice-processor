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

                // Extract basic information
                invoice.InvoiceNumber = ExtractInvoiceNumber(text);
                invoice.InvoiceDate = ExtractInvoiceDate(text);
                invoice.SupplierName = ExtractSupplierName(text);
                invoice.CustomerName = ExtractCustomerName(text);
                invoice.TotalAmount = ExtractTotalAmount(text);
                invoice.VatAmount = ExtractVatAmount(text);

                // Extract products
                var items = ExtractItems(text);
                foreach (var item in items)
                {
                    invoice.Items.Add(item);
                }

                // Calculate confidence score
                invoice.ConfidenceScore = CalculateConfidenceScore(invoice, text);

                Console.WriteLine($"[DEBUG] Final invoice created with Type: {invoice.Type}");
                return invoice;
            });
        }

        public InvoiceType DetectInvoiceType(string text, string? hintType = null)
        {
            Console.WriteLine($"[DEBUG] DetectInvoiceType called with hint: '{hintType}'");
            
            // Use explicit user hint if provided
            if (!string.IsNullOrEmpty(hintType))
            {
                var detectedType = hintType.ToLower().Trim() switch
                {
                    "purchase" => InvoiceType.Purchase,
                    "sale" => InvoiceType.Sale,
                    "purchase_return" => InvoiceType.PurchaseReturn,
                    "sale_return" => InvoiceType.SaleReturn,
                    _ => DetectFromTextImproved(text)
                };
                
                Console.WriteLine($"[DEBUG] Type from hint '{hintType}': {detectedType}");
                return detectedType;
            }

            var typeFromText = DetectFromTextImproved(text);
            Console.WriteLine($"[DEBUG] Type from text analysis: {typeFromText}");
            return typeFromText;
        }

        private InvoiceType DetectFromTextImproved(string text)
        {
            var lowerText = text.ToLower();
            Console.WriteLine($"[DEBUG] Analyzing text for invoice type detection...");

            // Return/refund detection
            if (lowerText.Contains("iade") || lowerText.Contains("return") || lowerText.Contains("red"))
            {
                if (lowerText.Contains("satış") || lowerText.Contains("sale"))
                    return InvoiceType.SaleReturn;
                else
                    return InvoiceType.PurchaseReturn;
            }

            // Score-based detection
            int purchaseScore = 0;
            int saleScore = 0;

            // Supplier/vendor indicators
            if (lowerText.Contains("tedarikçi") || lowerText.Contains("supplier") || 
                lowerText.Contains("satıcı firma") || lowerText.Contains("vendor"))
                purchaseScore += 30;

            // Customer indicators  
            if (lowerText.Contains("müşteri") || lowerText.Contains("customer") ||
                lowerText.Contains("alıcı") || lowerText.Contains("buyer"))
                saleScore += 30;

            // Transaction direction
            if (lowerText.Contains("satın aldık") || lowerText.Contains("temin ettik") ||
                lowerText.Contains("purchased") || lowerText.Contains("aldığınız"))
                purchaseScore += 25;

            if (lowerText.Contains("sattık") || lowerText.Contains("teslim ettik") ||
                lowerText.Contains("sold") || lowerText.Contains("satışı"))
                saleScore += 25;

            // Payment direction
            if (lowerText.Contains("ödeyeceğiniz") || lowerText.Contains("borcunuz") ||
                lowerText.Contains("you owe") || lowerText.Contains("payable"))
                purchaseScore += 20;

            if (lowerText.Contains("tahsil") || lowerText.Contains("alacağınız") ||
                lowerText.Contains("receivable") || lowerText.Contains("collection"))
                saleScore += 20;

            Console.WriteLine($"[DEBUG] Purchase score: {purchaseScore}, Sale score: {saleScore}");

            return purchaseScore > saleScore ? InvoiceType.Purchase : InvoiceType.Sale;
        }

        public List<InvoiceItem> ExtractItems(string text)
        {
            var items = new List<InvoiceItem>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            Console.WriteLine("[DEBUG] Starting item extraction...");
            
            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                if (string.IsNullOrWhiteSpace(cleanLine))
                    continue;
                
                // Look for lines that start with numbers (product rows) - no upper limit
                var rowMatch = Regex.Match(cleanLine, @"^(\d{1,3})\s+(.+)");
                if (rowMatch.Success)
                {
                    var rowNumber = int.Parse(rowMatch.Groups[1].Value);
                    if (rowNumber >= 1) // Remove upper limit restriction
                    {
                        var item = TryParseItemLine(cleanLine);
                        if (item != null && !string.IsNullOrWhiteSpace(item.ProductName))
                        {
                            items.Add(item);
                            Console.WriteLine($"[DEBUG] Added item {rowNumber}: {item.ProductName}");
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] Failed to parse row {rowNumber}: {cleanLine}");
                        }
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Total items extracted: {items.Count}");
            return items;
        }

        private bool IsItemSectionStart(string line)
        {
            var lowerLine = line.ToLower();
            return lowerLine.Contains("sıra") && lowerLine.Contains("mal hizmet") ||
                   lowerLine.Contains("no") && lowerLine.Contains("miktar") ||
                   lowerLine.Contains("ürün") && lowerLine.Contains("adet") ||
                   lowerLine.Contains("item") && lowerLine.Contains("quantity") ||
                   lowerLine.Contains("açıklama") && lowerLine.Contains("birim");
        }

        private bool IsItemSectionEnd(string line)
        {
            var lowerLine = line.ToLower();
            return lowerLine.Contains("mal hizmet toplam") ||
                   lowerLine.Contains("toplam tutar") ||
                   lowerLine.Contains("kdv") && lowerLine.Contains("toplam") ||
                   lowerLine.Contains("ara toplam") ||
                   lowerLine.Contains("vergiler dahil") ||
                   lowerLine.Contains("ödenecek tutar") ||
                   lowerLine.Contains("not:") ||
                   lowerLine.Contains("notlar");
        }
        
        private bool HasItemPattern(string line)
        {
            var numberMatches = Regex.Matches(line, @"\d+[.,]?\d*");
            var hasProductName = Regex.IsMatch(line, @"[a-zA-ZğüşıöçĞÜŞİÖÇ]{3,}");
            var hasUnit = Regex.IsMatch(line, @"\b(adet|kg|lt|liter|litre|metre|meter|m|cm|mm|gr|gram|ton|Adet|Kg|Lt|Liter|Litre|Metre|Meter|M|Cm|Mm|Gr|Gram|Ton)\b");
            var hasCurrency = line.ToLower().Contains("tl") || line.Contains("₺");
            
            return numberMatches.Count >= 2 && hasProductName && (hasUnit || hasCurrency) && 
                   line.Length > 10 && !IsNonProductLine(line);
        }

        private InvoiceItem? TryParseItemLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || IsNonProductLine(line)) 
                return null;

            // More flexible patterns to capture different units (not just Adet)
            var patterns = new[]
            {
                // Pattern 1: Row + Product + Quantity + Unit + Price + ... + Total
                @"^(\d+)\s+(.+?)\s+(\d+(?:[.,]\d+)?)\s+(adet|Adet|kg|Kg|lt|Lt|litre|Litre|metre|Metre|cm|Cm|mm|Mm|gr|Gr|gram|Gram|ton|Ton|m2|M2|m²|M²|m3|M3|m³|M³)\s+(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d+)?)\s+.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)$",
                
                // Pattern 2: Row + Product + Quantity + Unit + Price (no total at end)
                @"^(\d+)\s+(.+?)\s+(\d+(?:[.,]\d+)?)\s+(adet|Adet|kg|Kg|lt|Lt|litre|Litre|metre|Metre|cm|Cm|mm|Mm|gr|Gr|gram|Gram|ton|Ton|m2|M2|m²|M²|m3|M3|m³|M³)\s+(\d+(?:[.,]\d+)?)",
                
                // Pattern 3: Fallback for simple cases
                @"^(\d+)\s+(.+?)\s+(\d+(?:[.,]\d+)?)\s+(adet|Adet)\s+(\d+(?:[.,]\d+)?)"
            };

            foreach (var (pattern, index) in patterns.Select((p, i) => (p, i)))
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    try
                    {
                        var rowNumber = int.Parse(match.Groups[1].Value);
                        var productName = ExtractCleanProductName(match.Groups[2].Value);
                        
                        if (string.IsNullOrWhiteSpace(productName) || productName.Length < 3) 
                            continue;

                        decimal quantity = ParseDecimal(match.Groups[3].Value);
                        string unit = NormalizeUnit(match.Groups[4].Value); // Use actual unit from invoice
                        decimal unitPrice = ParseDecimal(match.Groups[5].Value);
                        
                        decimal totalPrice;
                        if (match.Groups.Count > 6 && !string.IsNullOrEmpty(match.Groups[6].Value))
                        {
                            totalPrice = ParseDecimal(match.Groups[6].Value);
                        }
                        else
                        {
                            totalPrice = quantity * unitPrice;
                        }

                        // Validation
                        if (quantity <= 0 || unitPrice <= 0 || totalPrice <= 0) 
                        {
                            Console.WriteLine($"[WARNING] Invalid values - Row: {rowNumber}");
                            continue;
                        }

                        Console.WriteLine($"[SUCCESS] Row {rowNumber}: {productName} | {quantity} {unit} | ₺{unitPrice} | Total: ₺{totalPrice}");

                        return new InvoiceItem
                        {
                            ProductName = productName,
                            Quantity = quantity,
                            Unit = unit, // Keep the actual unit from invoice
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice,
                            ConfidenceScore = 95 - (index * 10)
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Parsing row: {ex.Message}");
                        continue;
                    }
                }
            }

            Console.WriteLine($"[NO_MATCH] Line: {line.Substring(0, Math.Min(50, line.Length))}...");
            return null;
        }

        private string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return "adet";

            // Preserve original case for exact match, then convert to lowercase for comparison
            var originalUnit = unit.Trim();
            unit = unit.ToLower().Trim();

            return unit switch
            {
                "adet" or "ad" or "pcs" or "piece" => "adet",
                "kg" or "kilogram" or "kilo" => "kg",
                "lt" or "liter" or "litre" or "l" => "litre",
                "m" or "metre" or "meter" => "metre",
                "cm" or "santimetre" or "centimeter" => "cm",
                "mm" or "milimetre" or "millimeter" => "mm",
                "gr" or "gram" or "g" => "gram",
                "ton" or "tonne" => "ton",
                "m2" or "m²" or "metrekare" => "m²",
                "m3" or "m³" or "metreküp" => "m³",
                _ => originalUnit.Length <= 10 ? originalUnit.ToLower() : "adet"
            };
        }

        private string CleanItemLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return string.Empty;

            // Normalize whitespace
            line = Regex.Replace(line.Trim(), @"\s+", " ");
            
            // DON'T remove leading line numbers - we need them for pattern matching
            // line = Regex.Replace(line, @"^\d{1,3}\s+", "");
            
            return line;
        }

        private bool IsNonProductLine(string line)
        {
            var lowerLine = line.ToLower();
            
            var skipKeywords = new[] {
                "toplam", "total", "kdv", "vat", "iskonto", "discount",
                "ödenecek", "tutar", "amount", "tarih", "date", "no:",
                "fatura", "invoice", "sayfa", "page", "ettn:", "tckn:",
                "tel:", "phone", "adres", "address", "bank", "iban",
                "sıra", "mal hizmet", "miktar", "birim", "fiyat",
                "not:", "note", "vergi", "tax", "yalnız", "only",
                "ara toplam", "subtotal", "vergiler dahil", "hesaplanan",
                "web sitesi", "website", "e-mail", "email", "fax", "faks",
                "ticaret sicil", "mersis", "banka hesap", "şube",
                "hesap türü", "banka adı", "özelleştirme", "senaryo",
                "fatura tipi", "düzenlenme", "saati", "irsaliye",
                "vade tarihi", "cari bakiye", "platform", "izin",
                "arşiv", "görüntüle", "indir", "kullan", "geçer",
                "elektronik", "ortam", "iletil", "kapsa", "garanti",
                "denizbank", "işbank", "konak", "izmir", "türkiye",
                "ltd", "a.ş", "anonim", "şirket", "limited"
            };

            // Keyword check
            if (skipKeywords.Any(keyword => lowerLine.Contains(keyword)))
                return true;
                
            // Too short or only numbers/symbols
            if (line.Length < 5 || Regex.IsMatch(line, @"^[\d\s.,%-]{3,}$"))
                return true;
                
            // URL/Email pattern
            if (lowerLine.Contains("www.") || lowerLine.Contains("http") || lowerLine.Contains("@"))
                return true;
                
            // IBAN/Account number pattern
            if (Regex.IsMatch(line, @"TR\d{2}") || lowerLine.Contains("iban"))
                return true;
                
            // Phone number pattern
            if (Regex.IsMatch(line, @"\+90|\b0\d{3}\s?\d{3}\s?\d{2}\s?\d{2}\b"))
                return true;
                
            // UUID patterns (ETTN etc.)
            if (Regex.IsMatch(line, @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}"))
                return true;
                
            // Date formats (date-only lines)
            if (Regex.IsMatch(line, @"^\d{1,2}[.-]\d{1,2}[.-]\d{4}\s*$"))
                return true;
                
            // Only uppercase letters and spaces (header lines)
            if (line.Length > 10 && Regex.IsMatch(line, @"^[A-ZĞÜŞIÖÇ\s]+$"))
                return true;

            return false;
        }

        private string ExtractCleanProductName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

            rawName = rawName.Trim();
            
            // Keep size/dimension specifications but clean other unwanted parts
            // Don't remove "/2" or similar dimension specs - they're part of product name
            
            // Remove unwanted prefixes/suffixes but keep dimensions
            rawName = Regex.Replace(rawName, @"^\d+\s*[-.]?\s*", ""); // Remove row numbers only
            rawName = Regex.Replace(rawName, @"\s*tl\s*$", "", RegexOptions.IgnoreCase);
            rawName = Regex.Replace(rawName, @"\s+%\d+[.,]\d+.*$", "");
            rawName = Regex.Replace(rawName, @"\s+\d+[.,]\d+\s*tl.*$", "", RegexOptions.IgnoreCase);
            
            rawName = Regex.Replace(rawName, @"\s+", " ").Trim();
            
            if (rawName.Length < 3 || Regex.IsMatch(rawName, @"^[\d\s.,%-]{3,}$"))
                return string.Empty;
            
            return rawName;
        }

        private string? ExtractCustomerName(string text)
        {
            var lines = text.Split('\n');
            
            // Look for lines after "SAYIN" keyword
            bool foundSayin = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.ToUpper().Contains("SAYIN"))
                {
                    foundSayin = true;
                    continue;
                }
                
                if (foundSayin && !string.IsNullOrWhiteSpace(line) && 
                    !line.ToLower().Contains("vergi") && 
                    !line.ToLower().Contains("tckn") &&
                    line.Length > 5)
                {
                    return line.Trim();
                }
            }
            
            return null;
        }

        private string? ExtractInvoiceNumber(string text)
        {
            var patterns = new[]
            {
                @"Fatura\s*No[:\s]*([A-Z0-9]+)",
                @"Invoice\s*No[:\s]*([A-Z0-9]+)",
                @"Belge\s*No[:\s]*([A-Z0-9]+)"
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
            var patterns = new[]
            {
                @"Fatura\s*Tarihi[:\s]*(\d{1,2})[.-](\d{1,2})[.-](\d{4})",
                @"(\d{1,2})[.-](\d{1,2})[.-](\d{4})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    try
                    {
                        var day = int.Parse(match.Groups[1].Value);
                        var month = int.Parse(match.Groups[2].Value);
                        var year = int.Parse(match.Groups[3].Value);
                        return new DateTime(year, month, day);
                    }
                    catch { continue; }
                }
            }

            return null;
        }

        private string? ExtractSupplierName(string text)
        {
            var lines = text.Split('\n');
            
            // Find company name in first lines (usually first 10 lines)
            for (int i = 0; i < Math.Min(10, lines.Length); i++)
            {
                var line = lines[i].Trim();
                if (line.Length > 10 && 
                    (line.Contains("A.Ş") || line.Contains("LTD") || line.Contains("ŞTİ") ||
                     line.Contains("SİSTEM") || line.Contains("TİCARET") || line.Contains("POMPA")) &&
                    !line.ToLower().Contains("sayin"))
                {
                    return line;
                }
            }
            
            return null;
        }

        private decimal ExtractTotalAmount(string text)
        {
            var patterns = new[]
            {
                @"Ödenecek\s*Tutar[:\s]*([\d.,]+)\s*TL",
                @"Vergiler\s*Dahil\s*Toplam\s*Tutar[:\s]*([\d.,]+)\s*TL",
                @"Toplam\s*Tutar[:\s]*([\d.,]+)\s*TL",
                @"TOPLAM[:\s]*([\d.,]+)\s*TL"
            };

            decimal maxAmount = 0;
            
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var amount = ParseDecimal(match.Groups[1].Value);
                    if (amount > maxAmount)
                        maxAmount = amount;
                }
            }

            return maxAmount;
        }

        private decimal? ExtractVatAmount(string text)
        {
            var patterns = new[]
            {
                @"Hesaplanan\s*KDV\s*\([^)]*\)[:\s]*([\d.,]+)\s*TL",
                @"KDV\s*Tutarı[:\s]*([\d.,]+)\s*TL",
                @"VAT[:\s]*([\d.,]+)"
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
                value = value.Trim().Replace(" ", "");
                
                var lastDotIndex = value.LastIndexOf('.');
                var lastCommaIndex = value.LastIndexOf(',');
                
                if (lastDotIndex > 0 && lastCommaIndex > 0)
                {
                    if (lastDotIndex > lastCommaIndex)
                        value = value.Replace(",", "");
                    else
                        value = value.Substring(0, lastCommaIndex).Replace(".", "") + "." + value.Substring(lastCommaIndex + 1);
                }
                else if (lastCommaIndex > 0)
                {
                    var beforeComma = value.Substring(0, lastCommaIndex);
                    var afterComma = value.Substring(lastCommaIndex + 1);
                    
                    if (afterComma.Length <= 2)
                        value = beforeComma + "." + afterComma;
                    else
                        value = value.Replace(",", "");
                }
                else if (lastDotIndex > 0)
                {
                    var afterDot = value.Substring(lastDotIndex + 1);
                    if (afterDot.Length > 2)
                        value = value.Replace(".", "");
                }
                
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private int CalculateConfidenceScore(Invoice invoice, string text)
        {
            int score = 50;

            if (!string.IsNullOrEmpty(invoice.InvoiceNumber)) score += 15;
            if (invoice.InvoiceDate.HasValue) score += 15;
            if (!string.IsNullOrEmpty(invoice.SupplierName)) score += 10;
            if (!string.IsNullOrEmpty(invoice.CustomerName)) score += 10;
            if (invoice.TotalAmount > 0) score += 10;
            if (invoice.Items.Any()) score += 20;

            if (text.Length > 100) score += 5;
            if (!text.Contains("?") && !text.Contains("�")) score += 10;

            return Math.Min(100, score);
        }
    }
}