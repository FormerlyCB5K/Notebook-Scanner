using System.Buffers.Binary;
using System.Security.Cryptography;

namespace NotebookScanner;

public static class Wire
{
    public static readonly Guid ServiceId = new("e286e3de-3194-4e95-ae08-7ad9f621aba0");
    public static readonly byte[] Magic = "NS01"u8.ToArray();
    public const int MaxBytes = 8 * 1024 * 1024;

    // A new timeout covers each frame, beginning after its first byte arrives.
    public static async Task<byte[]> Receive(Stream stream, CancellationToken stop)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header.AsMemory(0, 1), stop);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
        deadline.CancelAfter(TimeSpan.FromSeconds(120));
        await stream.ReadExactlyAsync(header.AsMemory(1), deadline.Token);
        uint size = BinaryPrimitives.ReadUInt32BigEndian(header);
        if (size == 0 || size > MaxBytes) throw new InvalidDataException("Invalid image length.");
        var hash = new byte[32];
        await stream.ReadExactlyAsync(hash, deadline.Token);
        var jpeg = new byte[(int)size];
        await stream.ReadExactlyAsync(jpeg, deadline.Token);
        if (!CryptographicOperations.FixedTimeEquals(hash, SHA256.HashData(jpeg)))
            throw new InvalidDataException("Image checksum failed.");
        ValidateJpeg(jpeg);
        return jpeg;
    }

    // Inspect SOF dimensions before handing compressed data to the native decoder.
    public static void ValidateJpeg(byte[] data)
    {
        if (data.Length < 4 || data[0] != 255 || data[1] != 216)
            throw new InvalidDataException("Expected JPEG.");
        int i = 2;
        while (i + 3 < data.Length)
        {
            if (data[i++] != 255) break;
            while (i < data.Length && data[i] == 255) i++;
            if (i >= data.Length) break;
            int marker = data[i++];
            if (marker is 0xda or 0xd9) break;
            if (marker == 0x01 || marker is >= 0xd0 and <= 0xd7) continue;
            if (i + 2 > data.Length) break;
            int length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i));
            if (length < 2 || i + length > data.Length) break;
            // Our sender emits ordinary baseline JPEG; reject other encodings.
            if (marker == 0xc0)
            {
                if (length < 8 || data[i + 2] != 8) break;
                int h = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 3));
                int w = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 5));
                if (w < 1 || h < 1 || w > 4096 || h > 4096 || (long)w * h > 12_000_000)
                    throw new InvalidDataException("Image dimensions exceed limits.");
                return;
            }
            i += length;
        }
        throw new InvalidDataException("Unsupported or damaged JPEG header.");
    }
}
