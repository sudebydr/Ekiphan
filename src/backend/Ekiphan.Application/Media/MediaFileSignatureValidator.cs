using System.IO.Compression;
using System.Text;
using Ekiphan.Domain.Media;

namespace Ekiphan.Application.Media;

public sealed class MediaFileSignatureValidator
    : IMediaFileSignatureValidator
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Pdf = "%PDF-"u8.ToArray();
    private static readonly byte[] Ole =
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public bool IsValid(
        Stream content,
        MediaAssetType assetType,
        string contentType,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || !content.CanSeek)
        {
            return false;
        }

        var originalPosition = content.Position;
        try
        {
            content.Position = 0;
            var header = new byte[12];
            var read = content.Read(header, 0, header.Length);
            var mime = contentType?.Trim().ToLowerInvariant();
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return (assetType, mime, extension) switch
            {
                (MediaAssetType.Image, "image/jpeg", ".jpg" or ".jpeg" or ".jfif") =>
                    StartsWith(header, read, Jpeg),
                (MediaAssetType.Image, "image/png", ".png") =>
                    StartsWith(header, read, Png),
                (MediaAssetType.Image, "image/webp", ".webp") =>
                    IsWebP(header, read),
                (MediaAssetType.Pdf, "application/pdf", ".pdf") =>
                    StartsWith(header, read, Pdf),
                (MediaAssetType.Document, "application/msword", ".doc") =>
                    StartsWith(header, read, Ole),
                (
                    MediaAssetType.Document,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".docx") => IsDocx(content),
                _ => false,
            };
        }
        catch (InvalidDataException)
        {
            return false;
        }
        finally
        {
            content.Position = originalPosition;
        }
    }

    private static bool IsDocx(Stream content)
    {
        content.Position = 0;
        using var archive = new ZipArchive(
            content,
            ZipArchiveMode.Read,
            leaveOpen: true);
        return archive.Entries.Any(entry =>
                entry.FullName.Equals(
                    "[Content_Types].xml",
                    StringComparison.OrdinalIgnoreCase)) &&
            archive.Entries.Any(entry =>
                entry.FullName.Equals(
                    "word/document.xml",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWebP(byte[] header, int read) =>
        read >= 12 &&
        Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
        Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

    private static bool StartsWith(
        byte[] header,
        int read,
        byte[] signature) =>
        read >= signature.Length &&
        header.AsSpan(0, signature.Length).SequenceEqual(signature);
}
