using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>
    /// Kayıt dosyası biçimi. Başlık düzeni **tüm sürümlerde sabittir**; yalnız üst veri ve yükün
    /// içeriği sürümle değişir. Böylece her dosyanın bütünlüğü çözülmeden doğrulanabilir.
    /// <code>
    /// 0   8  sihirli dizi "TARIHSTR"
    /// 8   4  biçim sürümü (uint32, little-endian)
    /// 12  4  üst veri uzunluğu (int32)
    /// 16  4  yük uzunluğu (int32)
    /// 20 32  SHA-256(üst veri ‖ yük)
    /// 52  …  üst veri, ardından yük
    /// </code>
    /// </summary>
    public static class SaveFileFormat
    {
        /// <summary>Bu derlemenin yazdığı biçim sürümü.</summary>
        public const int CurrentVersion = 1;

        public const int HeaderSize = 52;

        private static readonly byte[] Magic = { (byte)'T', (byte)'A', (byte)'R', (byte)'I', (byte)'H', (byte)'S', (byte)'T', (byte)'R' };

        public static byte[] Encode(SaveMetadata metadata, byte[] payload)
        {
            if (metadata == null || payload == null)
            {
                throw new ArgumentNullException(metadata == null ? nameof(metadata) : nameof(payload));
            }

            return EncodeRaw(CurrentVersion, metadata.Encode(), payload);
        }

        /// <summary>Belirli bir sürümle ham dosya üretir (göç testleri ve araçlar için).</summary>
        public static byte[] EncodeRaw(int version, byte[] metadata, byte[] payload)
        {
            var body = new byte[metadata.Length + payload.Length];
            Buffer.BlockCopy(metadata, 0, body, 0, metadata.Length);
            Buffer.BlockCopy(payload, 0, body, metadata.Length, payload.Length);
            byte[] hash = Sha256.Compute(body);

            var file = new byte[HeaderSize + body.Length];
            Buffer.BlockCopy(Magic, 0, file, 0, Magic.Length);
            WriteUInt32(file, 8, (uint)version);
            WriteUInt32(file, 12, (uint)metadata.Length);
            WriteUInt32(file, 16, (uint)payload.Length);
            Buffer.BlockCopy(hash, 0, file, 20, hash.Length);
            Buffer.BlockCopy(body, 0, file, HeaderSize, body.Length);
            return file;
        }

        /// <summary>Doğrular, gerekirse göç ettirir ve çözer. Daha yeni sürüm reddedilir.</summary>
        public static Result<SaveFile> Decode(byte[] file, SaveMigrationChain migrations)
        {
            if (file == null || file.Length < HeaderSize)
            {
                return Result<SaveFile>.Failure(PersistenceErrorKeys.UzunlukUyusmuyor);
            }

            for (int i = 0; i < Magic.Length; i++)
            {
                if (file[i] != Magic[i])
                {
                    return Result<SaveFile>.Failure(PersistenceErrorKeys.SihirliDiziYok);
                }
            }

            uint version = ReadUInt32(file, 8);
            uint metadataLength = ReadUInt32(file, 12);
            uint payloadLength = ReadUInt32(file, 16);
            if ((ulong)HeaderSize + metadataLength + payloadLength != (ulong)file.Length)
            {
                return Result<SaveFile>.Failure(PersistenceErrorKeys.UzunlukUyusmuyor);
            }

            var expected = new byte[Sha256.HashSize];
            Buffer.BlockCopy(file, 20, expected, 0, expected.Length);
            if (!Sha256.AreEqual(expected, Sha256.Compute(file, HeaderSize, file.Length - HeaderSize)))
            {
                return Result<SaveFile>.Failure(PersistenceErrorKeys.OzetUyusmuyor);
            }

            if (version > CurrentVersion)
            {
                return Result<SaveFile>.Failure(PersistenceErrorKeys.SurumDahaYeni);
            }

            if (version < 1)
            {
                return Result<SaveFile>.Failure(PersistenceErrorKeys.SurumGecersiz);
            }

            var metadata = new byte[metadataLength];
            var payload = new byte[payloadLength];
            Buffer.BlockCopy(file, HeaderSize, metadata, 0, metadata.Length);
            Buffer.BlockCopy(file, HeaderSize + metadata.Length, payload, 0, payload.Length);

            if (version < CurrentVersion)
            {
                if (migrations == null || !migrations.TryUpgrade((int)version, CurrentVersion, ref metadata, ref payload, out string error))
                {
                    return Result<SaveFile>.Failure(PersistenceErrorKeys.GocEksik);
                }
            }

            try
            {
                return Result<SaveFile>.Success(new SaveFile((int)version, SaveMetadata.Decode(metadata), payload));
            }
            catch (FormatException)
            {
                return Result<SaveFile>.Failure(PersistenceErrorKeys.SurumGecersiz);
            }
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        private static uint ReadUInt32(byte[] buffer, int offset)
        {
            return (uint)buffer[offset] | ((uint)buffer[offset + 1] << 8) | ((uint)buffer[offset + 2] << 16) | ((uint)buffer[offset + 3] << 24);
        }
    }
}
