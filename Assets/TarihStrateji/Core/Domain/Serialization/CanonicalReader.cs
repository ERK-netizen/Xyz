using System;
using System.Text;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Domain.Serialization
{
    /// <summary>
    /// <see cref="CanonicalWriter"/> çıktısını okur. Her okuma sınır denetimlidir; bozuk veya
    /// kesik veri <see cref="FormatException"/> (Türkçe anahtar) fırlatır, asla yarım durum üretmez.
    /// </summary>
    public sealed class CanonicalReader
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        private readonly byte[] _data;
        private readonly int _end;
        private int _position;

        public CanonicalReader(byte[] data)
            : this(data, 0, data?.Length ?? 0)
        {
        }

        public CanonicalReader(byte[] data, int offset, int count)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            if (offset < 0 || count < 0 || offset > data.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _position = offset;
            _end = offset + count;
        }

        public int Remaining => _end - _position;

        public bool IsAtEnd => _position == _end;

        public byte ReadByte()
        {
            Require(1);
            return _data[_position++];
        }

        public bool ReadBool()
        {
            byte b = ReadByte();
            if (b > 1)
            {
                throw new FormatException(DomainErrorKeys.KayitBozuk);
            }

            return b == 1;
        }

        public uint ReadUInt32()
        {
            Require(4);
            uint value = (uint)_data[_position]
                | ((uint)_data[_position + 1] << 8)
                | ((uint)_data[_position + 2] << 16)
                | ((uint)_data[_position + 3] << 24);
            _position += 4;
            return value;
        }

        public int ReadInt32() => unchecked((int)ReadUInt32());

        public ulong ReadUInt64()
        {
            Require(8);
            ulong value = 0UL;
            for (int i = 0; i < 8; i++)
            {
                value |= (ulong)_data[_position + i] << (8 * i);
            }

            _position += 8;
            return value;
        }

        public long ReadInt64() => unchecked((long)ReadUInt64());

        public Fixed64 ReadFixed64() => Fixed64.FromRaw(ReadInt64());

        public GameDate ReadDate()
        {
            int day = ReadInt32();
            if (day < 0 || day > GameDate.MaxValue.DayNumber)
            {
                throw new FormatException(DomainErrorKeys.KayitBozuk);
            }

            return GameDate.FromDayNumber(day);
        }

        /// <summary>Negatif olmayan ve <paramref name="max"/> değerini aşmayan bir sayı (eleman sayısı vb.).</summary>
        public int ReadCount(int max)
        {
            int count = ReadInt32();
            if (count < 0 || count > max)
            {
                throw new FormatException(DomainErrorKeys.KayitBozuk);
            }

            return count;
        }

        public string ReadString()
        {
            int count = ReadInt32();
            if (count == -1)
            {
                return null;
            }

            if (count < 0)
            {
                throw new FormatException(DomainErrorKeys.KayitBozuk);
            }

            Require(count);
            try
            {
                string value = Utf8.GetString(_data, _position, count);
                _position += count;
                return value;
            }
            catch (ArgumentException)
            {
                throw new FormatException(DomainErrorKeys.KayitBozuk);
            }
        }

        public byte[] ReadBytes()
        {
            int count = ReadCount(Remaining);
            var result = new byte[count];
            Buffer.BlockCopy(_data, _position, result, 0, count);
            _position += count;
            return result;
        }

        public void ExpectTag(uint tag)
        {
            if (ReadUInt32() != tag)
            {
                throw new FormatException(DomainErrorKeys.KayitEtiketUyusmuyor);
            }
        }

        public void ExpectEnd()
        {
            if (!IsAtEnd)
            {
                throw new FormatException(DomainErrorKeys.KayitBozuk);
            }
        }

        private void Require(int count)
        {
            if (count > _end - _position)
            {
                throw new FormatException(DomainErrorKeys.KayitBeklenmeyenSon);
            }
        }
    }
}
