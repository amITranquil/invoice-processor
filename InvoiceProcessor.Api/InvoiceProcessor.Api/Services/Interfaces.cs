﻿using InvoiceProcessor.Api.Data.Models;

namespace InvoiceProcessor.Api.Services
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(string filePath);
        Task<int> GetConfidenceScoreAsync(string text);
    }

    public interface IPdfService
    {
        Task<string> ExtractTextFromPdfAsync(string filePath);
        bool IsPdfTextBased(string filePath);
    }

    public interface IInvoiceParsingService
    {
        Task<Invoice> ParseInvoiceAsync(string text, string fileName, string? invoiceType = null);
        InvoiceType DetectInvoiceType(string text, string? hintType = null);
        List<InvoiceItem> ExtractItems(string text);
    }

    public interface IStockService
    {
        Task UpdateStockAsync(Invoice invoice);
        Task<List<StockMovement>> GetMovementsAsync(int? productId = null);
        Task<Product> GetOrCreateProductAsync(string productName, string? productCode = null, string? unit = null);
    }

    public interface IFileProcessingService
    {
        Task<Invoice> ProcessFileAsync(Stream fileStream, string fileName, string? invoiceType = null);
        bool IsValidFileType(string fileName);
        Task<string> SaveFileAsync(Stream fileStream, string fileName);
    }
}