using Hack.io.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hack.io.Class;

/// <summary>
/// Which endianness to use. Endianness indicates the way numbers are stored.
/// </summary>
public enum StreamEndian
{
    /// <summary>
    /// Little endian. Used by most modern systems, including the Nintendo Switch.
    /// </summary>
    Little = 0,
    /// <summary>
    /// Big endian. Used by older systems, like the Nintendo Wii.
    /// </summary>
    Big = 1
};

/// <summary>
/// A wrapped stream. Allows any base <see cref="Stream"/> to be read/written to, with the specified endian and encoding.
/// </summary>
/// <param name="baseStream">The stream to wrap.</param>
/// <param name="endian"></param>
/// <param name="encoding">The encoding to encode strings with. Null means the encoding is auto-determined.</param>
public class UtilityStream(Stream baseStream, StreamEndian endian = StreamEndian.Big, Encoding? encoding=null) : Stream
{
    public StreamEndian Endian = endian;
    public Encoding? CurrentEncoding = encoding;
    /// <summary>
    /// Converts the endian bytes to the desired endian.
    /// </summary>
    /// <param name="data">The span of bytes to switch</param>
    /// <param name="Invert">If TRUE, the data will come out in the opposite endian</param>
    public void ApplyEndian<T>(Span<T> data, bool Invert = false)
    {
        bool e = BitConverter.IsLittleEndian;
        if (Invert)
            e = !e;
        if (e)
        {
            if (Endian == StreamEndian.Big)
                data.Reverse();
        }
        else
        {
            if (Endian == StreamEndian.Little)
                data.Reverse();
        }
    }
    /// <summary>
    /// Reads a set amount of bytes from memory into an array. Respects Endian.
    /// </summary>
    /// <param name="Count">The number of bytes to read<para/>MAX 8</param>
    /// <returns>a byte[] with the read data.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    public byte[] ReadEndian(int Count)
    {
        if (Count > 8)
            throw new ArgumentException($"\"{nameof(Count)}\" cannot be greater than 8", nameof(Count));
        if (Count < 0)
            throw new ArgumentException($"\"{nameof(Count)}\" cannot be less than 0", nameof(Count));

        long curpos = Position;
        Span<byte> read = stackalloc byte[Count];
        int bytecount = Read(read);

        if (bytecount != Count)
            throw new IOException($"Failed to read the file at {curpos}");

        ApplyEndian(read);
        return read.ToArray();
    }


    /// <summary>
    /// An alternative to <see cref="Stream.ReadByte"/>
    /// </summary>
    /// <returns>The resulting byte</returns>
    [CLSCompliant(false)]
    public sbyte ReadInt8() => (sbyte)ReadByte();
    /// <summary>
    /// An alternative to <see cref="Stream.ReadByte"/>
    /// </summary>
    /// <returns>The resulting byte</returns>
    public byte ReadUInt8() => (byte)ReadByte();
    /// <summary>
    /// Reads an Int16 from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting Int16 from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public short ReadInt16() => BitConverter.ToInt16(ReadEndian(sizeof(short)));
    /// <summary>
    /// Reads an UInt16 from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting UInt16 from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [CLSCompliant(false)]
    public ushort ReadUInt16() => BitConverter.ToUInt16(ReadEndian(sizeof(ushort)));
    /// <summary>
    /// Reads an Int32 from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting Int32 from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public int ReadInt32() => BitConverter.ToInt32(ReadEndian(sizeof(int)));
    /// <summary>
    /// Reads an UInt32 from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting UInt32 from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    [CLSCompliant(false)]
    public uint ReadUInt32() => BitConverter.ToUInt32(ReadEndian(sizeof(uint)));
    /// <summary>
    /// Reads an Int64 from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting Int64 from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public long ReadInt64() => BitConverter.ToInt64(ReadEndian(sizeof(long)));
    /// <summary>
    /// Reads an UInt64 from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting UInt64 from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [CLSCompliant(false)]
    public ulong ReadUInt64() => BitConverter.ToUInt64(ReadEndian(sizeof(ulong)));
    /// <summary>
    /// Reads a Half from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting Half from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public Half ReadHalf() => BitConverter.ToHalf(ReadEndian(sizeof(short)));
    /// <summary>
    /// Reads a Single from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting Single from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public float ReadSingle() => BitConverter.ToSingle(ReadEndian(sizeof(float)));
    /// <summary>
    /// Reads a Double from the stream. Respects Endian.
    /// </summary>
    /// <returns>The resulting Double from the file</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public double ReadDouble() => BitConverter.ToDouble(ReadEndian(sizeof(double)));

    /// <summary>
    /// Reads a value as an Enum instead of the type defined in T
    /// </summary>
    /// <typeparam name="E">The Enum to return</typeparam>
    /// <typeparam name="T">The Datatype to read from the file</typeparam>
    /// <param name="Reader">The function to read a single instance of T from the file.</param>
    /// <returns></returns>
    public E ReadEnum<E, T>(Func<T> Reader)
        where E : Enum
        where T : unmanaged
    {
        return (E)(dynamic)Reader();
    }

    /// <summary>
    /// Reads multiple of the same data type.<para/>Example:<para/>-
    /// <example>MyStream.ReadMulti(3, StreamUtil.ReadSingle);</example>
    /// </summary>
    /// <typeparam name="T">Needs to be one of the supported Read types.</typeparam>
    /// <param name="EntryCount">The number of items of type T to read.</param>
    /// <param name="Reader">The function to read a single instance of T from the file.</param>
    /// <returns>An array of T</returns>
    /// <exception cref="ArgumentException"/>
    public T[] ReadMulti<T>(int EntryCount, Func<T> Reader)
    {
        if (EntryCount < 0)
            throw new ArgumentException($"\"{nameof(EntryCount)}\" cannot be less than 0", nameof(EntryCount));

        T[] values = new T[EntryCount];
        for (int i = 0; i < EntryCount; i++)
            values[i] = Reader();
        return values;
    }
    /// <summary>
    /// Reads multiple Int16
    /// </summary>
    /// <param name="EntryCount">The number of Int16 to read.</param>
    /// <returns>A short[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    public short[] ReadMultiInt16(int EntryCount) => ReadMulti(EntryCount, ReadInt16);
    /// <summary>
    /// Reads multiple UInt16
    /// </summary>
    /// <param name="EntryCount">The number of UInt16 to read.</param>
    /// <returns>A ushort[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    [CLSCompliant(false)]
    public ushort[] ReadMultiUInt16(int EntryCount) => ReadMulti(EntryCount, ReadUInt16);
    /// <summary>
    /// Reads multiple Int32
    /// </summary>
    /// <param name="EntryCount">The number of Int32 to read.</param>
    /// <returns>A int[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    public int[] ReadMultiInt32(int EntryCount) => ReadMulti(EntryCount, ReadInt32);
    /// <summary>
    /// Reads multiple UInt32
    /// </summary>
    /// <param name="EntryCount">The number of UInt32 to read.</param>
    /// <returns>A uint[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    [CLSCompliant(false)]
    public uint[] ReadMultiUInt32(int EntryCount) => ReadMulti(EntryCount, ReadUInt32);
    /// <summary>
    /// Reads multiple Int64
    /// </summary>
    /// <param name="EntryCount">The number of Int64 to read.</param>
    /// <returns>A long[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    public long[] ReadMultiInt64(int EntryCount) => ReadMulti(EntryCount, ReadInt64);
    /// <summary>
    /// Reads multiple UInt64
    /// </summary>
    /// <param name="EntryCount">The number of UInt64 to read.</param>
    /// <returns>A ulong[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    [CLSCompliant(false)]
    public ulong[] ReadMultiUInt64(int EntryCount) => ReadMulti(EntryCount, ReadUInt64);
    /// <summary>
    /// Reads multiple Half
    /// </summary>
    /// <param name="EntryCount">The number of Half to read.</param>
    /// <returns>A Half[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    public Half[] ReadMultiHalf(int EntryCount) => ReadMulti(EntryCount, ReadHalf);
    /// <summary>
    /// Reads multiple Single
    /// </summary>
    /// <param name="EntryCount">The number of Single to read.</param>
    /// <returns>A float[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    public float[] ReadMultiSingle(int EntryCount) => ReadMulti(EntryCount, ReadSingle);
    /// <summary>
    /// Reads multiple Double
    /// </summary>
    /// <param name="EntryCount">The number of Double to read.</param>
    /// <returns>A double[] of length <paramref name="EntryCount"/></returns>
    /// <exception cref="ArgumentException"/>
    public double[] ReadMultiDouble(int EntryCount) => ReadMulti(EntryCount, ReadDouble);

    /// <summary>
    /// !!ADVANCED USERS ONLY!!<para/>
    /// Reads an offset from the current position, jumps to that offset, reads a value there, then jumps back to just after reading the offset.
    /// </summary>
    /// <typeparam name="T">Needs to be one of the supported Read types.</typeparam>
    /// <typeparam name="Tout">Needs to be one of the supported Read types.</typeparam>
    /// <param name="OffsetReader">The function that will read the offset value</param>
    /// <param name="RelativeToPosition">The offset to uses as a base position</param>
    /// <param name="Reader">The function that will read the value at the offset</param>
    /// <returns>The value at the offset</returns>
    public Tout ReadFromOffset<T, Tout>(Func<Stream, T> OffsetReader, long RelativeToPosition, Func<Stream, Tout> Reader)
        where T : unmanaged
    {
        T Offset = OffsetReader(this);
        long PausePosition = Position;
        Position = RelativeToPosition + long.Parse(Offset.ToString() ?? "0"); //Real "non generic numbers" moment...ugh
        Tout result = Reader(this);
        Position = PausePosition;
        return result;
    }
    /// <summary>
    /// !!ADVANCED USERS ONLY!!<para/>
    /// Reads an offset from the current position, jumps to that offset, reads multiple values there, then jumps back to just after reading the offset.
    /// </summary>
    /// <typeparam name="T">Needs to be one of the supported Read types.</typeparam>
    /// <typeparam name="Tout">Needs to be one of the supported Read types.</typeparam>
    /// <param name="OffsetReader">The function that will read the offset value</param>
    /// <param name="RelativeToPosition">The offset to uses as a base position</param>
    /// <param name="MultiReader">The function that will read the values at the offset</param>
    /// <param name="EntryCount">The number of values to read</param>
    /// <returns>An array of values at the offset</returns>
    public Tout[] ReadMultiFromOffset<T, Tout>(Func<Stream, T> OffsetReader, long RelativeToPosition, Func<Stream, int, Tout[]> MultiReader, int EntryCount)
        where T : unmanaged
    {
        T Offset = OffsetReader(this);
        long PausePosition = Position;
        Position = RelativeToPosition + long.Parse(Offset.ToString() ?? "0"); //Real "non generic numbers" moment...ugh
        Tout[] result = MultiReader(this, EntryCount);
        Position = PausePosition;
        return result;
    }
    /// <summary>
    /// !!ADVANCED USERS ONLY!!<para/>
    /// Reads an offset from the current position, jumps to that offset, reads a value there, then jumps back to just after reading the offset.<para/>
    /// This is repeated "<paramref name="OffsetCount"/>" times
    /// </summary>
    /// <typeparam name="T">Needs to be one of the supported Read types.</typeparam>
    /// <typeparam name="Tout">Needs to be one of the supported Read types.</typeparam>
    /// <param name="OffsetReader">The function that will read the offset value</param>
    /// <param name="OffsetCount">The number of offsets to read</param>
    /// <param name="RelativeToPosition">The offset to uses as a base position</param>
    /// <param name="Reader">The function that will read the value at the offset</param>
    /// <returns>An array containing the value at each offset</returns>
    public Tout[] ReadFromOffsetMulti<T, Tout>(Func<Stream, T> OffsetReader, int OffsetCount, long RelativeToPosition, Func<Stream, Tout> Reader)
        where T : unmanaged
    {
        Tout[] results = new Tout[OffsetCount];
        for (int i = 0; i < OffsetCount; i++)
            results[i] = ReadFromOffset(OffsetReader, RelativeToPosition, Reader);
        return results;
    }
    /// <summary>
    /// !!ADVANCED USERS ONLY!!<para/>
    /// Reads an offset from the current position, jumps to that offset, reads multiple values there, then jumps back to just after reading the offset.<para/>
    /// This is repeated "<paramref name="OffsetCount"/>" times
    /// </summary>
    /// <typeparam name="T">Needs to be one of the supported Read types.</typeparam>
    /// <typeparam name="Tout">Needs to be one of the supported Read types.</typeparam>
    /// <param name="OffsetReader">The function that will read the offset value</param>
    /// <param name="OffsetCount">The number of offsets to read</param>
    /// <param name="RelativeToPosition">The offset to uses as a base position</param>
    /// <param name="MultiReader">The function that will read the values at the offset</param>
    /// <param name="EntryCount">The number of values to read</param>
    /// <returns>An array of arrays that contain the values at the offsets</returns>
    public Tout[][] ReadMultiFromOffsetMulti<T, Tout>(Func<Stream, T> OffsetReader, int OffsetCount, long RelativeToPosition, Func<Stream, int, Tout[]> MultiReader, int EntryCount)
        where T : unmanaged
    {
        Tout[][] results = new Tout[OffsetCount][];
        for (int i = 0; i < OffsetCount; i++)
            results[i] = ReadMultiFromOffset(OffsetReader, RelativeToPosition, MultiReader, EntryCount);
        return results;
    }

    /// <summary>
    /// Reads a value at the given absolute offset.<param/>Does not put the Stream Position back where it came from.
    /// </summary>
    /// <typeparam name="Tout">The type to return</typeparam>
    /// <param name="Offset">The position in the stream to read from</param>
    /// <param name="Reader">The function to use to read</param>
    /// <returns>the read value</returns>
    public Tout ReadAtOffset<Tout>(long Offset, Func<Stream, Tout> Reader)
    {
        Position = Offset;
        return Reader(this);
    }
    /// <summary>
    /// Reads multiple values at the given absolute offset.<param/>Does not put the Stream Position back where it came from.
    /// </summary>
    /// <typeparam name="Tout"></typeparam>
    /// <param name="Offset">The position in the stream to read from</param>
    /// <param name="MultiReader">The function that will read the values at the offset</param>
    /// <param name="EntryCount">The number of values to read</param>
    /// <returns>An array of values at the offset</returns>
    public Tout[] ReadMultiAtOffset<Tout>(long Offset, Func<Stream, int, Tout[]> MultiReader, int EntryCount)
    {
        Position = Offset;
        return MultiReader(this, EntryCount);
    }

    /// <summary>
    /// Reads a value that has varying length. Maxes out at 28 bits read.
    /// </summary>
    /// <returns></returns>
    public int? ReadVariableLength()
    {
        int vlq = 0;
        byte temp;
        int counter = 0;
        do
        {
            temp = ReadUInt8();
            vlq = (vlq << 7) | (temp & 0x7F);
            if (++counter >= 4)
                return null;
        } while ((temp & 0x80) > 0);
        return vlq;
    }


    /// <summary>
    /// Reads a string from the Stream that's NULL terminated.<para/>This method is faster for single byte encodings
    /// </summary>
    /// <param name="Enc">The encoding to use. Should only be 1 byte per character.</param>
    /// <returns>The resulting string</returns>
    /// <exception cref="EndOfStreamException"></exception>
    public string ReadString(Encoding Enc)
    {
        List<byte> bytes = [];
        while (ReadByte() != 0)
        {
            Position -= 1;
            int c = ReadByte();
            if (c == -1 || Position >= Length)
                throw new EndOfStreamException($"{nameof(ReadString)} was unable to locate the end of the string before the end of the file.");

            bytes.Add((byte)c);
        }
        byte[] conversionbytes = [.. bytes];
        return Enc.GetString(conversionbytes, 0, conversionbytes.Length);
    }
    /// <summary>
    /// Reads a string from the Stream that's NULL terminated.<para/>This method is for multi-byte encodings
    /// </summary>
    /// <param name="Enc">The encoding to use.</param>
    /// <param name="ByteCount">The stride of the Encoding. Variable Length not supported</param>
    /// <returns>The resulting string</returns>
    /// <exception cref="EndOfStreamException"></exception>
    public string ReadString(Encoding Enc, int ByteCount)
    {
        List<byte> bytes = [];
        byte[] Checker = new byte[ByteCount];
        bool IsDone = false;
        do
        {
            if (Position > Length)
                throw new EndOfStreamException($"{nameof(ReadString)} was unable to locate the end of the string before the end of the file.");
            Read(Checker, 0, ByteCount);
            if (Checker.All(B => B == 0x00))
            {
                IsDone = true;
                break;
            }
            bytes.AddRange(Checker);
        } while (!IsDone);
        byte[] conversionbytes = [.. bytes];
        return Enc.GetString(conversionbytes, 0, conversionbytes.Length);
    }
    /// <summary>
    /// Reads a string from the Stream that's fixed in size.<para/>This method is faster for single byte encodings
    /// </summary>
    /// <param name="Enc">The encoding to use. Should only be 1 byte per character.</param>
    /// <param name="StringLength">The number of characters to read</param>
    /// <returns>The resulting string</returns>
    /// <exception cref="IOException"></exception>
    public string ReadString(int StringLength, Encoding Enc)
    {
        byte[] bytes = new byte[StringLength];
        if (Read(bytes, 0, StringLength) != bytes.Length)
            throw new IOException("Failed to read the string.");
        return Enc.GetString(bytes, 0, StringLength);
    }
    /// <summary>
    /// Reads a string from the Stream that's fixed in size.<para/>This method is for multi-byte encodings
    /// </summary>
    /// <param name="Enc">The encoding to use.</param>
    /// <param name="StringLength">The number of characters to read</param>
    /// <param name="ByteCount">The stride of the Encoding. Variable Length not supported</param>
    /// <returns>The resulting string</returns>
    /// <exception cref="IOException"></exception>
    public string ReadString(int StringLength, Encoding Enc, int ByteCount)
    {
        StringLength *= ByteCount;
        byte[] bytes = new byte[StringLength];
        if (Read(bytes, 0, StringLength) != bytes.Length)
            throw new IOException("Failed to read the string.");
        return Enc.GetString(bytes, 0, StringLength);
    }

    /// <summary>
    /// Reads a "SHIFT-JIS" string from the Stream that's NULL terminated.
    /// </summary>
    /// <returns>The resulting string</returns>
    /// <exception cref="EndOfStreamException"></exception>
    public string ReadStringJIS() => ReadString(CurrentEncoding ?? StreamUtil.ShiftJIS);
    /// <summary>
    /// Reads an "ASCII" string from the Stream that's NULL terminated.
    /// </summary>
    /// <returns>The resulting string</returns>
    /// <exception cref="EndOfStreamException"></exception>
    public string ReadStringASCII() => ReadString(CurrentEncoding ?? Encoding.ASCII);

    /// <summary>
    /// Checks the stream for a given Magic identifier.<para/>Advances the Stream's Position forwards by Magic.Length
    /// </summary>
    /// <param name="Magic">The magic to check</param>
    /// <returns>TRUE if the next bytes match the magic, FALSE otherwise.</returns>
    public bool IsMagicMatch(ReadOnlySpan<byte> Magic)
    {
        Debug.Assert(Magic.Length is > 0 and < 16);

        Span<byte> read = stackalloc byte[Magic.Length]; //Should be fine since MAGIC's are typically only 4 bytes long.
        ReadExactly(read);
        ApplyEndian(read, true);
        return read.SequenceEqual(Magic);
    }
    /// <summary>
    /// Checks the stream for a given Magic identifier.<para/>Advances the Stream's Position forwards by Magic.Length
    /// </summary>
    /// <param name="Magic">The magic to check</param>
    /// <param name="Enc">The encoding that should be used when reading the file</param>
    /// <returns>TRUE if the next bytes match the magic, FALSE otherwise.</returns>
    public bool IsMagicMatch(ReadOnlySpan<char> Magic, Encoding? Enc = null)
    {
        Enc ??= Encoding.ASCII;

        Debug.Assert(Magic.Length is > 0 and < 16);

        string str = ReadString(Magic.Length, Enc);
        Span<char> to = new(str.ToCharArray());
        ApplyEndian(to, true);
        return to.SequenceEqual(Magic);
    }
    /// <summary>
    /// Attempts to read the following bytes as a MAGIC
    /// </summary>
    /// <param name="Length">The length of the magic to read (generally 4)</param>
    /// <param name="Enc">The encoding that should be used when reading the file</param>
    /// <returns>the next <paramref name="Length"/> bytes as a string</returns>
    public string ReadMagic(int Length, Encoding? Enc = null)
    {
        Enc ??= CurrentEncoding ?? Encoding.ASCII;
        Span<char> cr = ReadString(Length, Enc).ToCharArray();
        ApplyEndian(cr, true);
        return cr.ToString();
    }

    //====================================================================================================

    /// <summary>
    /// Writes a set of bytes to the stream
    /// </summary>
    /// <param name="Data">The data to write</param>
    /// <exception cref="ArgumentException"></exception>"
    public void WriteEndian(byte[] Data)
    {
        if (Data.Length > 8)
            throw new ArgumentException($"\"{nameof(Data)}\" cannot be larger than 8", nameof(Data));
        if (Data.Length < 0)
            throw new ArgumentException($"\"{nameof(Data)}\" cannot be smaller than 0", nameof(Data));

        Span<byte> span = stackalloc byte[Data.Length];
        Data.CopyTo(span);
        ApplyEndian(span);
        Write(span);
    }
    /// <summary>
    /// An alternative to <see cref="Stream.WriteByte(byte)"/>
    /// </summary>
    /// <param name="Value">The value to write</param>
    [CLSCompliant(false)]
    public void WriteInt8(sbyte Value) => WriteByte((byte)Value);
    /// <summary>
    /// An alternative to <see cref="Stream.WriteByte(byte)"/>
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteUInt8(byte Value) => WriteByte(Value);
    /// <summary>
    /// Writes an Int16 to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteInt16(short Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes an UInt16 to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    [CLSCompliant(false)]
    public void WriteUInt16(ushort Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes an Int32 to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteInt32(int Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes an UInt32 to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    [CLSCompliant(false)]
    public void WriteUInt32(uint Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes an Int64 to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteInt64(long Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes an UInt64 to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    [CLSCompliant(false)]
    public void WriteUInt64(ulong Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes a Half to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteHalf(Half Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes a single to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteSingle(float Value) => WriteEndian(BitConverter.GetBytes(Value));
    /// <summary>
    /// Writes a double to the stream. Respects Endian.
    /// </summary>
    /// <param name="Value">The value to write</param>
    public void WriteDouble(double Value) => WriteEndian(BitConverter.GetBytes(Value));

    /// <summary>
    /// Writes a value as an Enum instead of the type defined in T
    /// </summary>
    /// <typeparam name="E">The Enum to write</typeparam>
    /// <typeparam name="T">The Datatype to write to the file</typeparam>
    /// <param name="Value">The value to write</param>
    /// <param name="Writer">The action to write a single instance of T to the file.</param>
    public void WriteEnum<E, T>(E Value, Action<T> Writer)
        where E : Enum
        where T : unmanaged
    {
        Writer((T)(dynamic)Value);
    }

    /// <summary>
    /// Writes multiple of the same data type.<para/>Example:<para/>-
    /// <example>MyStream.WriteMulti(MyFloatArray, StreamUtil.WriteSingle);</example> 
    /// </summary>
    /// <typeparam name="T">Needs to be one of the supported Write types.</typeparam>
    /// <param name="Values">The array of values to write</param>
    /// <param name="Writer">The action to write a single instance of T to the file.</param>
    /// <exception cref="ArgumentException"></exception>
    public void WriteMulti<T>(IList<T> Values, Action<T> Writer)
    {
        if (Values.Count < 0)
            throw new ArgumentException($"\"{nameof(Values)}\" cannot be smaller than 0", nameof(Values));

        for (int i = 0; i < Values.Count; i++)
            Writer(Values[i]);
    }
    /// <summary>
    /// Writes multiple Int16
    /// </summary>
    /// <param name="Values">The Int16 values to write</param>
    public void WriteMultiInt16(IList<short> Values) => WriteMulti(Values, WriteInt16);
    /// <summary>
    /// Writes multiple UInt16
    /// </summary>
    /// <param name="Values">The UInt16 values to write</param>
    [CLSCompliant(false)]
    public void WriteMultiUInt16(IList<ushort> Values) => WriteMulti(Values, WriteUInt16);
    /// <summary>
    /// Writes multiple Int32
    /// </summary>
    /// <param name="Values">The Int32 values to write</param>
    public void WriteMultiInt32(IList<int> Values) => WriteMulti(Values, WriteInt32);
    /// <summary>
    /// Writes multiple UInt32
    /// </summary>
    /// <param name="Values">The UInt32 values to write</param>
    [CLSCompliant(false)]
    public void WriteMultiUInt32(IList<uint> Values) => WriteMulti(Values, WriteUInt32);
    /// <summary>
    /// Writes multiple Int64
    /// </summary>
    /// <param name="Values">The Int64 values to write</param>
    public void WriteMultiInt64(IList<long> Values) => WriteMulti(Values, WriteInt64);
    /// <summary>
    /// Writes multiple UInt64
    /// </summary>
    /// <param name="Values">The UInt64 values to write</param>
    [CLSCompliant(false)]
    public void WriteMultiUInt64(IList<ulong> Values) => WriteMulti(Values, WriteUInt64);
    /// <summary>
    /// Writes multiple Half
    /// </summary>
    /// <param name="Values">The Half values to write</param>
    public void WriteMultiHalf(IList<Half> Values) => WriteMulti(Values, WriteHalf);
    /// <summary>
    /// Writes multiple Single
    /// </summary>
    /// <param name="Values">The Single values to write</param>
    public void WriteMultiSingle(IList<float> Values) => WriteMulti(Values, WriteSingle);
    /// <summary>
    /// Writes multiple Double
    /// </summary>
    /// <param name="Values">The Double values to write</param>
    public void WriteMultiDouble(IList<double> Values) => WriteMulti(Values, WriteDouble);

    /// <summary>
    /// Writes a value that has varying length.
    /// </summary>
    /// <param name="value"></param>
    public void WriteVariableLength(long value)
    {
        long buffer;
        byte last;
        buffer = value & 0x7F;
        while ((value >>= 7) > 0)
        {
            buffer <<= 8;
            buffer |= 0x80;
            buffer += (value & 0x7F);
        }
        do
        {
            last = unchecked((byte)buffer);
            WriteByte(last);
            buffer >>= 8;
        } while (unchecked((byte)(buffer & 0x80)) > 0);
        if ((last & 0x80) > 0)
            WriteByte(unchecked((byte)buffer));
    }

    /// <summary>
    /// Writes a string to the Stream that can be NULL terminated.<para/>This method is faster for single byte encodings
    /// </summary>
    /// <param name="String">The string to write</param>
    /// <param name="Enc">The encoding to write the string in</param>
    /// <param name="Terminator">The terminator byte. Set to NULL to dsiable termination (for MAGICs and whatnot)</param>
    public void WriteString(string String, Encoding Enc, byte? Terminator = 0x00)
    {
        byte[] Data = Enc.GetBytes(String);
        Write(Data, 0, Data.Length);
        if (Terminator is not null)
            WriteByte(Terminator.Value);
    }
    /// <summary>
    /// Writes a string to the Stream that can be NULL terminated.<para/>This method is for multi-byte encodings
    /// </summary>
    /// <param name="String">The string to write</param>
    /// <param name="Enc">The encoding to write the string in</param>
    /// <param name="ByteCount">The stride of the encoding. Variable Length not supported</param>
    /// <param name="Terminator">The byte to use for the Terminator. Set to NULL to dsiable termination (for MAGICs and whatnot)</param>
    public void WriteString(string String, Encoding Enc, int ByteCount, byte? Terminator = 0x00)
    {
        byte[] Data = Enc.GetBytes(String);
        Write(Data, 0, Data.Length);

        if (Terminator is not null)
        {
            byte[] Term = new byte[ByteCount];
            for (int i = 0; i < ByteCount; i++)
                Term[i] = Terminator.Value;
            Write(Term, 0, Term.Length);
        }
    }

    /// <summary>
    /// Writes a "SHIFT-JIS" string to the Stream that's be NULL terminated.
    /// </summary>
    /// <param name="String">the string to write</param>
    /// <param name="Terminator">The byte to use for the Terminator. Set to NULL to dsiable termination (for MAGICs and whatnot)</param>
    public void WriteStringJIS(string String, byte? Terminator = 0x00) => WriteString(String, StreamUtil.ShiftJIS, Terminator);

    /// <summary>
    /// Writes a byte[] into the file as Magic
    /// </summary>
    /// <param name="Magic"></param>
    public void WriteMagic(byte[] Magic)
    {
        Span<byte> b = new(Magic);
        ApplyEndian(b, true);
        Write(b);
    }

    /// <summary>
    /// Writes a string into the file as Magic
    /// </summary>
    /// <param name="Magic"></param>
    /// <param name="Enc"></param>
    public void WriteMagic(string Magic, Encoding? Enc = null)
    {
        Enc ??= Encoding.ASCII;

        byte[] Data = Enc.GetBytes(Magic);
        Span<byte> b = new(Data);
        ApplyEndian(b, true);
        Write(b);
    }

    //====================================================================================================

    /// <summary>
    /// Peeks the value at the position of the stream.<para/>The stream position is not advanced
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Reader">The function that determines what gets read</param>
    /// <returns>the value at the position of the stream.</returns>
    public T Peek<T>(Func<T> Reader) where T : struct
    {
        long start = Position;
        T v = Reader();
        Position = start;
        return v;
    }

    //====================================================================================================

    /// <summary>
    /// Adds padding to the current position in the provided stream
    /// </summary>
    /// <param name="Multiple">The byte multiple to pad to</param>
    /// <param name="PadString">The string to use as padding</param>
    [DebuggerStepThrough]
    public void PadTo(int Multiple, string PadString)
    {
        int NeededPadding = StreamUtil.CalculatePaddingLength(Position, Multiple);
        if (PadString.Length < NeededPadding)
            throw new ArgumentException($"The {nameof(PadString)} \"{PadString}\" is too short. ({NeededPadding}/{PadString.Length})", nameof(PadString));

        string UsedPadding = PadString[..NeededPadding];
        WriteString(UsedPadding, Encoding.ASCII, null);
    }

    #region Inherited Sections
    private readonly Stream _baseStream = baseStream;
    /// <inheritdoc/>
    public override bool CanRead => _baseStream.CanRead;
    /// <inheritdoc/>
    public override bool CanSeek => _baseStream.CanSeek;
    /// <inheritdoc/>
    public override bool CanWrite => _baseStream.CanWrite;
    /// <inheritdoc/>
    public override long Length => _baseStream.Length;
    /// <inheritdoc/>
    public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }
    /// <inheritdoc/>
    public override void Flush()
    {
        _baseStream.Flush();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _baseStream.Read(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _baseStream.Seek(offset, origin);
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        _baseStream.SetLength(value);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _baseStream.Write(buffer, offset, count);
    }

    #endregion

}