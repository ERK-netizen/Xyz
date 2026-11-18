using System;

namespace TarihStrateji.Core.Domain.Hashing
{
    /// <summary>
    /// FIPS 180-4 SHA-256, saf C#. Platform kripto sağlayıcısına (CryptoConfig yansıması, IL2CPP
    /// kod ayıklaması) bağımlı değildir; kayıt bütünlüğü ve durum hash'i için kullanılır.
    /// Testlerde .NET'in kendi SHA-256'sı kâhin olarak doğrular.
    /// </summary>
    public static class Sha256
    {
        public const int HashSize = 32;

        private static readonly uint[] K =
        {
            0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
            0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
            0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
            0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
            0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
            0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
            0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
            0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
        };

        public static byte[] Compute(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return Compute(data, 0, data.Length);
        }

        public static byte[] Compute(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || count < 0 || offset > data.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            uint[] h =
            {
                0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19,
            };
            var w = new uint[64];

            int fullBlocks = count / 64;
            for (int b = 0; b < fullBlocks; b++)
            {
                ProcessBlock(data, offset + (b * 64), h, w);
            }

            // Son blok(lar): kalan baytlar + 0x80 + sıfırlar + 64-bit bit uzunluğu (big-endian).
            int remaining = count - (fullBlocks * 64);
            var tail = new byte[remaining < 56 ? 64 : 128];
            Buffer.BlockCopy(data, offset + (fullBlocks * 64), tail, 0, remaining);
            tail[remaining] = 0x80;
            ulong bitLength = (ulong)count * 8UL;
            for (int i = 0; i < 8; i++)
            {
                tail[tail.Length - 1 - i] = (byte)(bitLength >> (8 * i));
            }

            for (int b = 0; b < tail.Length; b += 64)
            {
                ProcessBlock(tail, b, h, w);
            }

            var result = new byte[HashSize];
            for (int i = 0; i < 8; i++)
            {
                result[(i * 4) + 0] = (byte)(h[i] >> 24);
                result[(i * 4) + 1] = (byte)(h[i] >> 16);
                result[(i * 4) + 2] = (byte)(h[i] >> 8);
                result[(i * 4) + 3] = (byte)h[i];
            }

            return result;
        }

        /// <summary>Küçük harfli onaltılık gösterim.</summary>
        public static string ToHex(byte[] hash)
        {
            const string digits = "0123456789abcdef";
            var chars = new char[hash.Length * 2];
            for (int i = 0; i < hash.Length; i++)
            {
                chars[i * 2] = digits[hash[i] >> 4];
                chars[(i * 2) + 1] = digits[hash[i] & 0xF];
            }

            return new string(chars);
        }

        /// <summary>Sabit zamanlı karşılaştırma.</summary>
        public static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }

        private static void ProcessBlock(byte[] block, int offset, uint[] h, uint[] w)
        {
            for (int t = 0; t < 16; t++)
            {
                int i = offset + (t * 4);
                w[t] = ((uint)block[i] << 24) | ((uint)block[i + 1] << 16) | ((uint)block[i + 2] << 8) | block[i + 3];
            }

            for (int t = 16; t < 64; t++)
            {
                uint s0 = RotR(w[t - 15], 7) ^ RotR(w[t - 15], 18) ^ (w[t - 15] >> 3);
                uint s1 = RotR(w[t - 2], 17) ^ RotR(w[t - 2], 19) ^ (w[t - 2] >> 10);
                w[t] = unchecked(w[t - 16] + s0 + w[t - 7] + s1);
            }

            uint a = h[0], b = h[1], c = h[2], d = h[3], e = h[4], f = h[5], g = h[6], hh = h[7];
            for (int t = 0; t < 64; t++)
            {
                uint S1 = RotR(e, 6) ^ RotR(e, 11) ^ RotR(e, 25);
                uint ch = (e & f) ^ (~e & g);
                uint temp1 = unchecked(hh + S1 + ch + K[t] + w[t]);
                uint S0 = RotR(a, 2) ^ RotR(a, 13) ^ RotR(a, 22);
                uint maj = (a & b) ^ (a & c) ^ (b & c);
                uint temp2 = unchecked(S0 + maj);
                hh = g;
                g = f;
                f = e;
                e = unchecked(d + temp1);
                d = c;
                c = b;
                b = a;
                a = unchecked(temp1 + temp2);
            }

            h[0] = unchecked(h[0] + a);
            h[1] = unchecked(h[1] + b);
            h[2] = unchecked(h[2] + c);
            h[3] = unchecked(h[3] + d);
            h[4] = unchecked(h[4] + e);
            h[5] = unchecked(h[5] + f);
            h[6] = unchecked(h[6] + g);
            h[7] = unchecked(h[7] + hh);
        }

        private static uint RotR(uint x, int n) => (x >> n) | (x << (32 - n));
    }
}
