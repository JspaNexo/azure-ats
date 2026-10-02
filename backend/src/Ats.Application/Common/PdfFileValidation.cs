namespace Ats.Application.Common;

internal static class PdfFileValidation
{
    public static async Task<bool> HasValidHeaderAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[5];
        var originalPosition = stream.Position;
        try
        {
            var bytesRead = await stream.ReadAtLeastAsync(header, header.Length, false, cancellationToken);
            return bytesRead == header.Length && header.AsSpan().SequenceEqual("%PDF-"u8);
        }
        finally { stream.Position = originalPosition; }
    }
}
