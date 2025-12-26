using Hack.io.Interface;
using Hack.io.Utility;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Hack.io.YAZ0;

/// <summary>
/// Class containing methods to compress and decompress Data into Yaz0<para/><strong>Don't double Yaz0 encode data, it doesn't save any space.</strong>
/// </summary>
public static class YAZ0
{
    /// <inheritdoc cref="DocGen.DOC_MAGIC"/>
    public const string MAGIC = "Yaz0";

    #region Public Functions
    /// <summary>
    /// Checks the data to see if it is <see cref="YAZ0"/> encoded.
    /// </summary>
    /// <param name="Data">The stream of data to check</param>
    /// <returns>TRUE if the stream is Yaz0 encoded</returns>
    public static bool Check(Stream Data)
    {
        StreamUtil.PushEndianBig(); // YAZ0 is always Big Endian
        bool v = Data.IsMagicMatch(MAGIC);
        StreamUtil.PopEndian();
        return v;
    }

    /// <summary>
    /// Attempts to decompress the given data as YAZ0.
    /// </summary>
    /// <param name="Data">The data to decode</param>
    /// <returns>The byte[] of decoded data. Will be the same as the input if it is not YAZ0 encoded</returns>
    public static byte[] Decompress(byte[] Data) => Decode(Data);

    /// <summary>
    /// Encodes the given data as Yaz0.<para/>This method is slower, but results in better compressed files.
    /// </summary>
    /// <param name="Data">The data to encode.</param>
    /// <param name="BGW">A <see cref="BackgroundWorker"/> that will report the percentage complete out of 100.<para/>Using this allows cancellation of encoding as well.</param>
    /// <param name="Strength"></param>
    /// <returns>A <see cref="byte"/>[] of encoded data.</returns>
    public static byte[] Compress_Strong(byte[] Data, BackgroundWorker? BGW, uint Strength = 0x1000) => Encode_Strong(Data, BGW, Strength);
    /// <summary>
    /// Encodes the given data as Yaz0.<para/>This method is faster, but results in lesser compressed files.
    /// </summary>
    /// <param name="Data">The data to encode.</param>
    /// <param name="BGW">A <see cref="BackgroundWorker"/> that will report the percentage complete out of 100.<para/>Using this allows cancellation of encoding as well.</param>
    /// <param name="Strength"></param>
    /// <returns>A <see cref="byte"/>[] of encoded data.</returns>
    public static byte[] Compress_Fast(byte[] Data, BackgroundWorker? BGW, uint Strength = 0x400) => Encode_Fast(Data, BGW, Strength);
    /// <summary>
    /// Encodes the given data as Yaz0.<para/>This method is accurate to certain official games such as Galaxy
    /// </summary>
    /// <param name="Data">The data to encode.</param>
    /// <param name="BGW">A <see cref="BackgroundWorker"/> that will report the percentage complete out of 100.<para/>Using this allows cancellation of encoding as well.</param>
    /// <param name="MaxSeekBack"></param>
    /// <returns>A <see cref="byte"/>[] of encoded data.</returns>
    public static byte[] Compress_Official(byte[] Data, BackgroundWorker? BGW, uint MaxSeekBack = 0x1000) => Encode_Official(Data, BGW, MaxSeekBack);

    /// <summary>
    /// Encodes the given data as Yaz0.<para/>Use this if you intend to use <see cref="FileUtil.RunForFileBytes(string, Func{byte[], byte[]})"/> (and don't care about strength or progress reporting)
    /// </summary>
    /// <param name="Data">The data to encode.</param>
    /// <returns>The byte[] of encoded data.</returns>
    public static byte[] Compress_Default(byte[] Data) => Encode_Strong(Data);
    #endregion

    //====================================================================================================

    private static byte[] Decode(byte[] Data)
    {
        using MemoryStream Strm = new(Data);
        if (!Check(Strm))
            return Data; //NO MORE EXCEPTIONS!!!


        StreamUtil.PushEndianBig(); // YAZ0 is always Big Endian
        uint DecompressedSize = Strm.ReadUInt32(),
            CompressedDataOffset = Strm.ReadUInt32(),
            UncompressedDataOffset = Strm.ReadUInt32();

        List<byte> Decoding = [];
        while (Decoding.Count < DecompressedSize)
        {
            byte FlagByte = (byte)Strm.ReadByte();
            BitArray FlagSet = new(new byte[1] { FlagByte });

            for (int i = 7; i > -1 && (Decoding.Count < DecompressedSize); i--)
            {
                if (FlagSet[i] == true)
                    Decoding.Add((byte)Strm.ReadByte());
                else
                {
                    byte Tmp = (byte)Strm.ReadByte();
                    int Offset = (((byte)(Tmp & 0x0F) << 8) | (byte)Strm.ReadByte()) + 1,
                        Length = (Tmp & 0xF0) == 0 ? Strm.ReadByte() + 0x12 : (byte)((Tmp & 0xF0) >> 4) + 2;

                    for (int j = 0; j < Length; j++)
                        Decoding.Add(Decoding[^Offset]);
                }
            }
        }
        StreamUtil.PopEndian();
        return [.. Decoding];
    }

    //====================================================================================================

    private static void ExceptionOnSuperStrength(uint Strength)
    {
        if (Strength > 0x1000 || Strength < 0)
            throw new IndexOutOfRangeException("YAZ0 strength maxes out at 0x1000");
    }

    #region Encode Style: Strong
    private record struct Ret(int SrcPos, int DstPos);

    private static byte[] Encode_Strong(byte[] Src, BackgroundWorker? BGW = null, uint Strength = 0x1000)
    {
        ExceptionOnSuperStrength(Strength);

        StreamUtil.PushEndianBig(); // YAZ0 is always Big Endian
        uint ByteCountA = 0;
        uint MatchPos = 0;
        int PrevFlag = 0;
        List<byte> OutputFile = [0x59, 0x61, 0x7A, 0x30];
        Span<byte> len = BitConverter.GetBytes(Src.Length);
        StreamUtil.ApplyEndian(len);
        OutputFile.AddRange(len.ToArray());
        OutputFile.AddRange(new byte[8]);
        Ret r = new(0, 0);
        byte[] dst = new byte[24];
        int dstSize = 0;
        int lastpercent = -1;

        uint validBitCount = 0;
        byte currCodeByte = 0;

        while (r.SrcPos < Src.Length)
        {
            if (BGW?.CancellationPending ?? false)
            {
                StreamUtil.PopEndian();
                return [];
            }

            uint numBytes;
            uint matchPos = 0;
#if DEBUG
            uint srcPosBak;
#endif

            numBytes = EncodeAdvanced(Src, Src.Length, r.SrcPos, ref matchPos, (int)Strength);
            if (numBytes < 3)
            {
                //straight copy
                dst[r.DstPos] = Src[r.SrcPos];
                r.DstPos++;
                r.SrcPos++;
                //set flag for straight copy
                currCodeByte |= (byte)(0x80 >> (int)validBitCount);
            }
            else
            {
                //RLE part
                uint dist = (uint)(r.SrcPos - matchPos - 1);
                byte byte1;
                byte byte2;
                byte byte3;

                if (numBytes >= 0x12) // 3 byte encoding
                {
                    byte1 = (byte)(0 | (dist >> 8));
                    byte2 = (byte)(dist & 0xff);
                    dst[r.DstPos++] = byte1;
                    dst[r.DstPos++] = byte2;
                    // maximum runlength for 3 byte encoding
                    if (numBytes > 0xff + 0x12)
                    {
                        numBytes = (uint)(0xff + 0x12);
                    }
                    byte3 = (byte)(numBytes - 0x12);
                    dst[r.DstPos++] = byte3;
                }
                else // 2 byte encoding
                {
                    byte1 = (byte)(((numBytes - 2) << 4) | (dist >> 8));
                    byte2 = (byte)(dist & 0xff);
                    dst[r.DstPos++] = byte1;
                    dst[r.DstPos++] = byte2;
                }
                r.SrcPos += (int)numBytes;
            }
            validBitCount++;
            //Write eight codes
            if (validBitCount == 8)
            {
                OutputFile.Add(currCodeByte);
                for (int i = 0; i < r.DstPos; i++)
                    OutputFile.Add(dst[i]);
                dstSize += r.DstPos + 1;

#if DEBUG
                srcPosBak = (uint)r.SrcPos; //This is for debugging purposes
#endif
                currCodeByte = 0;
                validBitCount = 0;
                r.DstPos = 0;
            }
            float percent = MathUtil.GetPercentOf(r.SrcPos + 1, Src.Length);
            int p = (int)percent;
            if (lastpercent != p)
            {
                BGW?.ReportProgress(p);
                lastpercent = p;
            }
        }

        if (validBitCount > 0)
        {
            OutputFile.Add(currCodeByte);
            for (int i = 0; i < r.DstPos; i++)
                OutputFile.Add(dst[i]);
            r.DstPos = 0;
        }

        StreamUtil.PopEndian();
        return [.. OutputFile];

        uint EncodeAdvanced(byte[] src, int size, int pos, ref uint pMatchPos, int Strength)
        {
            // if prevFlag is set, it means that the previous position was determined by look-ahead try.
            // so just use it. this is not the best optimization, but nintendo's choice for speed.
            if (PrevFlag == 1)
            {
                pMatchPos = MatchPos;
                PrevFlag = 0;
                return ByteCountA;
            }
            PrevFlag = 0;
            uint numBytes = EncodeSimple(src, size, pos, ref MatchPos, Strength);
            pMatchPos = MatchPos;

            // if this position is RLE encoded, then compare to copying 1 byte and next position(pos+1) encoding
            if (numBytes >= 3)
            {
                ByteCountA = EncodeSimple(src, size, pos + 1, ref MatchPos, Strength);
                // if the next position encoding is +2 longer than current position, choose it.
                // this does not guarantee the best optimization, but fairly good optimization with speed.
                if (ByteCountA >= numBytes + 2)
                {
                    numBytes = 1;
                    PrevFlag = 1;
                }
            }
            return numBytes;

            static uint EncodeSimple(byte[] src, int size, int pos, ref uint pMatchPos, int Strength)
            {
                int startPos = pos - Strength;
                uint numBytes = 1;
                uint matchPos = 0;

                if (startPos < 0)
                    startPos = 0;
                for (int i = startPos; i < pos; i++)
                {
                    int j;
                    int check = Math.Min(size - pos, Strength);
                    for (j = 0; j < check; j++)
                        if (src[i + j] != src[j + pos])
                            break;

                    if (j > numBytes)
                    {
                        numBytes = (uint)j;
                        matchPos = (uint)i;
                    }
                }
                pMatchPos = matchPos;
                if (numBytes == 2)
                    numBytes = 1;
                return numBytes;
            }
        }
    }
    #endregion

    #region Encode Style: Fast
    private static unsafe byte[] Encode_Fast(byte[] Src, BackgroundWorker? BGW = null, uint Strength = 0x400)
    {
        ExceptionOnSuperStrength(Strength);

        StreamUtil.PushEndianBig();
        int lastpercent = -1;
        byte* dataptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(Src, 0);

        byte[] result = new byte[Src.Length + Src.Length / 8 + 0x10];
        byte* resultptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(result, 0);
        *resultptr++ = (byte)'Y';
        *resultptr++ = (byte)'a';
        *resultptr++ = (byte)'z';
        *resultptr++ = (byte)'0';
        *resultptr++ = (byte)((Src.Length >> 24) & 0xFF);
        *resultptr++ = (byte)((Src.Length >> 16) & 0xFF);
        *resultptr++ = (byte)((Src.Length >> 8) & 0xFF);
        *resultptr++ = (byte)((Src.Length >> 0) & 0xFF);
        for (int i = 0; i < 8; i++)
            *resultptr++ = 0;
        int length = Src.Length;
        int dstoffs = 16;
        int Offs = 0;
        while (true)
        {
            if (BGW?.CancellationPending ?? false)
            {
                StreamUtil.PopEndian();
                return [];
            }

            int headeroffs = dstoffs++;
            resultptr++;
            byte header = 0;
            for (int i = 0; i < 8; i++)
            {
                int comp = 0;
                int back = 1;
                int nr = 2;
                {
                    byte* ptr = dataptr - 1;
                    int maxnum = 0x111;
                    if (length - Offs < maxnum) maxnum = length - Offs;
                    //Use a smaller amount of bytes back to decrease time
                    int maxback = (int)Strength; // 0x400; // 0x1000;
                    if (Offs < maxback) maxback = Offs;
                    maxback = (int)dataptr - maxback;
                    int tmpnr;
                    while (maxback <= (int)ptr)
                    {
                        if (*(ushort*)ptr == *(ushort*)dataptr && ptr[2] == dataptr[2])
                        {
                            tmpnr = 3;
                            while (tmpnr < maxnum && ptr[tmpnr] == dataptr[tmpnr]) tmpnr++;
                            if (tmpnr > nr)
                            {
                                if (Offs + tmpnr > length)
                                {
                                    nr = length - Offs;
                                    back = (int)(dataptr - ptr);
                                    break;
                                }
                                nr = tmpnr;
                                back = (int)(dataptr - ptr);
                                if (nr == maxnum) break;
                            }
                        }
                        --ptr;
                    }
                }
                if (nr > 2)
                {
                    Offs += nr;
                    dataptr += nr;
                    if (nr >= 0x12)
                    {
                        *resultptr++ = (byte)(((back - 1) >> 8) & 0xF);
                        *resultptr++ = (byte)((back - 1) & 0xFF);
                        *resultptr++ = (byte)((nr - 0x12) & 0xFF);
                        dstoffs += 3;
                    }
                    else
                    {
                        *resultptr++ = (byte)((((back - 1) >> 8) & 0xF) | (((nr - 2) & 0xF) << 4));
                        *resultptr++ = (byte)((back - 1) & 0xFF);
                        dstoffs += 2;
                    }
                    comp = 1;
                }
                else
                {
                    *resultptr++ = *dataptr++;
                    dstoffs++;
                    Offs++;
                }
                header = (byte)((header << 1) | ((comp == 1) ? 0 : 1));
                if (Offs >= length)
                {
                    header = (byte)(header << (7 - i));
                    break;
                }
            }
            result[headeroffs] = header;
            if (Offs >= length)
                break;

            float percent = MathUtil.GetPercentOf(Offs + 1, Src.Length);
            int p = (int)percent;
            if (lastpercent != p)
            {
                BGW?.ReportProgress(p);
                lastpercent = p;
            }
        }
        while ((dstoffs % 4) != 0)
            dstoffs++;
        byte[] realresult = new byte[dstoffs];
        Array.Copy(result, realresult, dstoffs);
        StreamUtil.PopEndian();
        return realresult;
    }
    #endregion

    #region Encode Style: Official
    // Heavily based on https://codeberg.org/Humming-Owl/Yaz0
    private static byte[] Encode_Official(byte[] Src, BackgroundWorker? BGW = null, uint MaxSeekBack = 0x1000)
    {
        ExceptionOnSuperStrength(MaxSeekBack);

        StreamUtil.PushEndianBig();

        const int MIN_MATCH = 2;
        const int MAX_COPY_SIZE = 0xFF + 0x12;

        // The code below edits the data so we're copying it
        byte[] SourceCopy = new byte[Src.Length];
        Src.CopyTo(SourceCopy, 0);

        Span<byte> SourceSpan = SourceCopy;

        // YAZ0 header
        List<byte> enc = new(SourceSpan.Length + 0x20) { 0x59, 0x61, 0x7A, 0x30 };
        Span<byte> len = BitConverter.GetBytes(SourceSpan.Length);
        StreamUtil.ApplyEndian(len);
        enc.AddRange(len);
        enc.AddRange(stackalloc byte[8]);

        int lastpercent = -1;
        uint decSize = (uint)SourceSpan.Length, decPos = 0, matchIndex = 0, matchSize = 2;
        bool canBeCompressed = false;

        while (true)
        {
            // Reserve instruction byte for later
            int byteInstPos = enc.Count;
            enc.Add(0);

            for (byte instBit = 0x80; instBit != 0; instBit >>= 1)
            {
                if (BGW?.CancellationPending ?? false)
                {
                    StreamUtil.PopEndian();
                    return [];
                }


                bool oneByteAheadIsBetter = false;

                if (decPos >= decSize)
                    goto end;

                if (canBeCompressed)
                    goto store_match;

                uint minIndex = decPos > MaxSeekBack ? decPos - MaxSeekBack : 0;
                uint maxCopySize = MAX_COPY_SIZE;
                if (decPos + maxCopySize > decSize)
                    maxCopySize = decSize - decPos;

                ushort copySize;
                if (maxCopySize >= 3)
                {
                    uint scan = minIndex;
                    while (scan < decPos)
                    {
                        // ensure we can compare at matchSize position before checking equality
                        if (scan + matchSize < decSize && decPos + matchSize < decSize && SourceSpan[(int)(scan + matchSize)] == SourceSpan[(int)(decPos + matchSize)])
                        {
                            uint available = maxCopySize;
                            if (scan + available > decSize) available = decSize - scan;
                            if (decPos + available > decSize) available = decSize - decPos;

                            // find length of match
                            copySize = 0;
                            while (copySize < available && SourceSpan[(int)(scan + copySize)] == SourceSpan[(int)(decPos + copySize)])
                                copySize++;

                            if (copySize > matchSize)
                            {
                                matchIndex = scan;
                                matchSize = copySize;
                                canBeCompressed = true;

                                if (matchSize == maxCopySize)
                                    goto store_match; // best possible here
                            }
                        }

                        scan++;
                    }
                }

                // check if skipping one byte yields a better match
                if (canBeCompressed && decPos + matchSize + 1 < decSize)
                {
                    uint decPosBak = decPos + 1;
                    uint minIndexbak = decPosBak > MaxSeekBack ? decPosBak - MaxSeekBack : 0;

                    maxCopySize = MAX_COPY_SIZE;
                    if (decPosBak + maxCopySize > decSize)
                        maxCopySize = decSize - decPosBak;

                    uint matchIndexBak = matchIndex;
                    uint matchSizeBak = matchSize + 1u;

                    uint scan = minIndexbak;
                    while (scan < decPosBak)
                    {
                        if (scan + matchSizeBak < decSize && decPosBak + matchSizeBak < decSize && SourceSpan[(int)(scan + matchSizeBak)] == SourceSpan[(int)(decPosBak + matchSizeBak)])
                        {
                            uint available = maxCopySize;
                            if (scan + available > decSize)
                                available = decSize - scan;
                            if (decPosBak + available > decSize)
                                available = decSize - decPosBak;

                            copySize = 0;
                            while (copySize < available && SourceSpan[(int)(scan + copySize)] == SourceSpan[(int)(decPosBak + copySize)])
                                copySize++;

                            if (copySize > matchSizeBak)
                            {
                                matchIndexBak = scan;
                                matchSizeBak = copySize;
                                oneByteAheadIsBetter = true;

                                if (matchSizeBak == maxCopySize)
                                {
                                    matchIndex = matchIndexBak;
                                    matchSize = matchSizeBak;
                                    goto store_match;
                                }
                            }
                        }

                        scan++;
                    }

                    if (oneByteAheadIsBetter)
                    {
                        matchIndex = matchIndexBak;
                        matchSize = matchSizeBak;
                    }
                }

            store_match:
                if (canBeCompressed && !oneByteAheadIsBetter)
                {
                    // emit match
                    // seekback: distance from current pos to match start minus 1
                    ushort seekback = (ushort)(decPos - matchIndex - 1);
                    decPos += matchSize;

                    if (matchSize < 0x12)
                    {
                        // short form: 12-bit length-2 in top 4 bits of first two bytes + 12-bit seekback
                        uint storedLen = matchSize - MIN_MATCH; // matchSize >= 2 guaranteed
                        ushort shortInst = (ushort)((ushort)(storedLen << 12) | (seekback & 0x0FFF));
                        enc.Add((byte)((shortInst & 0xFF00) >> 8));
                        enc.Add((byte)(shortInst & 0x00FF));
                    }
                    else
                    {
                        // long form: two bytes seekback, then 1 byte length-0x12
                        uint storedLen = matchSize - 0x12;
                        enc.Add((byte)((seekback & 0xFF00) >> 8));
                        enc.Add((byte)(seekback & 0x00FF));
                        enc.Add((byte)storedLen);
                    }

                    canBeCompressed = false;
                    matchIndex = 0;
                    matchSize = 2;
                }
                else
                {
                    // emit literal byte
                    enc.Add(SourceSpan[(int)decPos]);
                    enc[byteInstPos] |= instBit;
                    decPos++;
                }
            }

            float percent = MathUtil.GetPercentOf(decPos + 1, Src.Length);
            int p = (int)percent;
            if (lastpercent != p)
            {
                BGW?.ReportProgress(p);
                lastpercent = p;
            }
        }

    end:
        StreamUtil.PopEndian();
        return [.. enc];
    }
    #endregion
}