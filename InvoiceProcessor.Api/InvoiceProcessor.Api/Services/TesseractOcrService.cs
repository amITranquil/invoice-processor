using Tesseract;

namespace InvoiceProcessor.Api.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tessDataPath;

        public TesseractOcrService()
        {
            _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        public async Task<string> ExtractTextAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var engine = new TesseractEngine(_tessDataPath, "tur+eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(filePath);
                using var page = engine.Process(img);

                return page.GetText();
            });
        }

        public async Task<int> GetConfidenceScoreAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var engine = new TesseractEngine(_tessDataPath, "tur+eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(filePath);
                using var page = engine.Process(img);

                return (int)(page.GetMeanConfidence() * 100);
            });
        }
    }
}