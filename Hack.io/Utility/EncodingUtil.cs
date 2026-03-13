using System.Text;

namespace Hack.io.Utility;

/// <summary>
/// Extra Encoding Functions
/// </summary>
public static class EncodingUtil
{
    /// <summary>
    /// Gets the amount of bytes this Encoding uses.<para/>Note: This retursn 2 for SHIFT-JIS.
    /// </summary>
    /// <param name="enc"></param>
    /// <returns></returns>
    public static int GetStride(this Encoding enc) => enc.GetMaxByteCount(0);
    //Registers the code page provider so we can use SHIFT-JIS
    static EncodingUtil() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>
    /// Shortcut to Encoding.GetEncoding("Shift-JIS")
    /// </summary>
    public static Encoding ShiftJIS => Encoding.GetEncoding("Shift-JIS");
}
