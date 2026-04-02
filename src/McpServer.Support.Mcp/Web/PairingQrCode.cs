using QRCoder;

namespace McpServer.Support.Mcp.Web;

/// <summary>
/// Generates QR code SVG images for the pairing flow.
/// </summary>
internal static class PairingQrCode
{
    /// <summary>Generates an SVG string containing a QR code for the given <paramref name="text"/>.</summary>
    public static string GenerateSvg(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(data);
        return svg.GetGraphic(8);
    }
}
