using Hack.io.Class;
using Hack.io.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hack.io.Interface;

/// <summary>
/// The interface indicating that this type can be Loaded and Saved
/// </summary>
public interface ILoadSaveFile
{
    /// <summary>
    /// Loads the format data off a <see cref="FileStream"/>, <see cref="MemoryStream"/>, or other <see cref="Stream"/> class.
    /// </summary>
    /// <param name="Strm">The stream to read from</param>
    [Obsolete("Stream will be auto-wrapped into a UtilityStream using the default endian and encoding. To hide this warning, explicitly wrap your stream via yourStream.Wrap();")]
    public void Load(Stream Strm)
    {
        Load(Strm.Wrap());
    }
    /// <summary>
    /// Loads the format data off a wrapped stream via the <see cref="UtilityStream"/> class.
    /// </summary>
    /// <param name="Strm">The stream to read from</param>
    public void Load(UtilityStream Strm);
    /// <summary>
    /// Saves the format data to a <see cref="FileStream"/>, <see cref="MemoryStream"/>, or other <see cref="Stream"/> class.
    /// </summary>
    /// <param name="Strm">The stream to write to</param>
    [Obsolete("Stream will be auto-wrapped into a UtilityStream using the default endian and encoding. To hide this warning, explicitly wrap your stream via yourStream.Wrap();")]
    public void Save(Stream Strm)
    {
        Save(Strm.Wrap());
    }
    /// <summary>
    /// Saves the format data to a wrapped stream via the <see cref="UtilityStream"/> class.
    /// </summary>
    /// <param name="Strm">The stream to write to</param>
    public void Save(UtilityStream Strm);
}

sealed partial class DocGen
{
    /// <summary>
    /// The file identifier
    /// </summary>
    public const string DOC_MAGIC = "";
}