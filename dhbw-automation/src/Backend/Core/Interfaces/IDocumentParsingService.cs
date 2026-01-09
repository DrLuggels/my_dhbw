namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IDocumentParsingService
{
    /// <summary>
    /// Extracts text from a PDF file using UglyToad.PdfPig
    /// Falls back to Gemini OCR if no text can be extracted
    /// </summary>
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);

    /// <summary>
    /// Extracts text from a DOCX file using DocumentFormat.OpenXml
    /// </summary>
    Task<string> ExtractTextFromDocxAsync(Stream docxStream);

    /// <summary>
    /// Extracts text from an image using Gemini 3 Flash OCR
    /// </summary>
    Task<string> ExtractTextFromImageAsync(Stream imageStream);

    /// <summary>
    /// Extracts text from any supported file type and optionally analyzes for errors
    /// </summary>
    Task<(string ExtractedText, string[]? DetectedErrors)> ExtractAndAnalyzeAsync(
        Stream fileStream,
        string fileType,
        bool analyzeErrors = false
    );
}
