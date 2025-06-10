using InvoiceProcessor.Api.Data.Models;

namespace InvoiceProcessor.Api.Services
{
    public class FileProcessingService : IFileProcessingService
    {
        private readonly IOcrService _ocrService;
        private readonly IPdfService _pdfService;
        private readonly IInvoiceParsingService _invoiceParsingService;

        public FileProcessingService(
            IOcrService ocrService,
            IPdfService pdfService,
            IInvoiceParsingService invoiceParsingService)
        {
            _ocrService = ocrService;
            _pdfService = pdfService;
            _invoiceParsingService = invoiceParsingService;
        }

        public async Task<Invoice> ProcessFileAsync(Stream fileStream, string fileName, string? invoiceType = null)
        {
            Console.WriteLine($"[DEBUG] FileProcessingService.ProcessFileAsync called with invoiceType: '{invoiceType}'");
            
            var filePath = await SaveFileAsync(fileStream, fileName);

            string extractedText;
            var isPdf = Path.GetExtension(fileName).ToLower() == ".pdf";

            if (isPdf && _pdfService.IsPdfTextBased(filePath))
            {
                extractedText = await _pdfService.ExtractTextFromPdfAsync(filePath);
            }
            else
            {
                extractedText = await _ocrService.ExtractTextAsync(filePath);
            }

            Console.WriteLine($"[DEBUG] About to call ParseInvoiceAsync with invoiceType: '{invoiceType}'");
            var invoice = await _invoiceParsingService.ParseInvoiceAsync(extractedText, fileName, invoiceType);
            Console.WriteLine($"[DEBUG] ParseInvoiceAsync returned invoice with Type: {invoice.Type}");
            
            invoice.RawText = extractedText;

            // Geçici dosyayı sil
            if (File.Exists(filePath))
                File.Delete(filePath);

            return invoice;
        }

        public bool IsValidFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff" }.Contains(extension);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "InvoiceProcessor");
            Directory.CreateDirectory(tempDir);

            var filePath = Path.Combine(tempDir, Guid.NewGuid() + Path.GetExtension(fileName));

            using var fileStreamOut = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamOut);

            return filePath;
        }
    }
}