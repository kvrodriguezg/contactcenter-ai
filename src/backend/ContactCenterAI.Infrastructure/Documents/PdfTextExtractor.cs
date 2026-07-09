using System.Text;
using ContactCenterAI.Application.Common.Interfaces;
using PDFiumZ;

namespace ContactCenterAI.Infrastructure.Documents;

public class PdfTextExtractor : IPdfTextExtractor
{
    private static readonly object LibraryLock = new();
    private static bool _libraryInitialized;

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ExtractText(filePath, cancellationToken), cancellationToken);
    }

    private static string ExtractText(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLibraryInitialized();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("No se encontró el archivo PDF.", filePath);
        }

        var document = fpdfview.FPDF_LoadDocument(filePath, string.Empty);
        if (document == null)
        {
            throw new InvalidOperationException($"No fue posible abrir el PDF. Código de error: {fpdfview.FPDF_GetLastError()}");
        }

        try
        {
            var builder = new StringBuilder();
            var pageCount = fpdfview.FPDF_GetPageCount(document);

            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = fpdfview.FPDF_LoadPage(document, pageIndex);
                if (page == null)
                {
                    continue;
                }

                try
                {
                    var pageText = ExtractPageText(page);
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        if (builder.Length > 0)
                        {
                            builder.AppendLine();
                        }

                        builder.Append(pageText);
                    }
                }
                finally
                {
                    fpdfview.FPDF_ClosePage(page);
                }
            }

            return builder.ToString().Trim();
        }
        finally
        {
            fpdfview.FPDF_CloseDocument(document);
        }
    }

    private static string ExtractPageText(FpdfPageT page)
    {
        var textPage = fpdf_text.FPDFTextLoadPage(page);
        if (textPage == null)
        {
            return string.Empty;
        }

        try
        {
            var charCount = fpdf_text.FPDFTextCountChars(textPage);
            if (charCount <= 0)
            {
                return string.Empty;
            }

            var buffer = new ushort[charCount + 1];
            var extractedChars = fpdf_text.FPDFTextGetText(textPage, 0, charCount, ref buffer[0]);

            if (extractedChars <= 0)
            {
                return string.Empty;
            }

            var bytes = new byte[extractedChars * 2];
            Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        finally
        {
            fpdf_text.FPDFTextClosePage(textPage);
        }
    }

    private static void EnsureLibraryInitialized()
    {
        if (_libraryInitialized)
        {
            return;
        }

        lock (LibraryLock)
        {
            if (_libraryInitialized)
            {
                return;
            }

            fpdfview.FPDF_InitLibrary();
            _libraryInitialized = true;
        }
    }
}
