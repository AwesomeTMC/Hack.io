namespace Hack.io.GX;

public enum GXComponentCount
{
    Position_XY = 0,
    Position_XYZ,

    Normal_XYZ = 0,
    Normal_NBT,
    Normal_NBT3,

    /// <summary>
    /// Allows for <see cref="GXComponentType"/>s: RGB565, RGB8, RGBX8
    /// </summary>
    Color_RGB = 0,
    /// <summary>
    /// Allows for <see cref="GXComponentType"/>s: RGBA4, RGBA6, RGBA8
    /// </summary>
    Color_RGBA,

    TexCoord_S = 0,
    TexCoord_ST
}