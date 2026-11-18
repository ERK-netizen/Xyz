using System;
using System.Text;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Domain.Serialization
{
    /// <summary>
    /// Kanonik ikili yazıcı: little-endian, sabit genişlikli tamsayılar, UTF-8 metin. Aynı durum
    /// her platformda aynı baytları üretir; kayıt dosyası ve durum hash'i (SHA-256) buna dayanır.
    /// Kayan nokta yazma yöntemi yoktur.
    /// </summary>
    public sealed class CanonicalWriter
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        private byte[] _buffer;
        private int _length;

        public CanonicalWriter(int initialCapacity = 4096)
        {
            _buffer = new byte[initialCapacity < 16 ? 16 : initialCapacity];
        }

        public int Length => _length;

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buffer[_length++] = value;
        }

        public void WriteBool(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint)value));
        }

        public void WriteUInt32(uint value)
        {
            Ensure(4);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 24);
        }

        public void WriteInt64(long value)
        {
            WriteUInt64(unchecked((ulong)value));
        }

        public void WriteUInt64(ulong value)
        {
            Ensure(8);
            for (int i = 0; i < 8; i++)
            {
                _buffer[_length++] = (byte)(value >> (8 * i));
            }
        }

        public void WriteFixed64(Fixed64 value)
        {
            WriteInt64(value.Raw);
        }

        public void WriteDate(GameDate date)
        {
            WriteInt32(date.DayNumber);
        }

        public void WriteId<TId>(TId id)
            where TId : struct, IStableId
        {
            WriteUInt64(id.Value);
        }

        /// <summary>Uzunluk önekli UTF-8; <c>null</c> için -1.</summary>
        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            int count = Utf8.GetByteCount(value);
            WriteInt32(count);
            Ensure(count);
            Utf8.GetBytes(value, 0, value.Length, _buffer, _length);
            _length += count;
        }

        public void WriteBytes(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            WriteInt32(data.Length);
            WriteRaw(data, 0, data.Length);
        }

        public void WriteRaw(byte[] data, int offset, int count)
        {
            Ensure(count);
            Buffer.BlockCopy(data, offset, _buffer, _length, count);
            _length += count;
        }

        /// <summary>Bölüm etiketi: okurken hizalama hatasını erken yakalamak için.</summary>
        public void WriteTag(uint tag)
        {
            WriteUInt32(tag);
        }

        /// <summary>İçeriği siler, tamponu korur (günlük hash için tahsissiz yeniden kullanım).</summary>
        public void Reset()
        {
            _length = 0;
        }

        /// <summary>Yazılan içeriğin SHA-256'sı; kopya üretmeden tampondan hesaplanır.</summary>
        public byte[] ComputeSha256()
        {
            return Sha256.Compute(_buffer, 0, _length);
        }

        public byte[] ToArray()
        {
            var result = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, result, 0, _length);
            return result;
        }

        private void Ensure(int extra)
        {
            int required = checked(_length + extra);
            if (required <= _buffer.Length)
            {
                return;
            }

            int capacity = _buffer.Length;
            while (capacity < required)
            {
                capacity = capacity > int.MaxValue / 2 ? int.MaxValue : capacity * 2;
            }

            Array.Resize(ref _buffer, capacity);
        }
    }
}
