using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace InvoiceProcessor.Api.Services
{
    public class PdfService : IPdfService
    {
        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var reader = new PdfReader(filePath);
                    using var pdfDoc = new PdfDocument(reader);

                    var text = string.Empty;
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        var page = pdfDoc.GetPage(i);
                        text += PdfTextExtractor.GetTextFromPage(page);
                    }

                    return text;
                }
                catch
                {
                    return string.Empty;
                }
            });
        }

        public bool IsPdfTextBased(string filePath)
        {
            try
            {
                using var reader = new PdfReader(filePath);
                using var pdfDoc = new PdfDocument(reader);

                var firstPage = pdfDoc.GetPage(1);
                var text = PdfTextExtractor.GetTextFromPage(firstPage);

                return !string.IsNullOrWhiteSpace(text) && text.Length > 50;
            }
            catch
            {
                return false;
            }
        }
    }
}