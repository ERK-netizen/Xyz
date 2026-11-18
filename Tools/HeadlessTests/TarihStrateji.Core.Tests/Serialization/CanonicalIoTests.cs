using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Tests.Serialization
{
    [TestFixture]
    public class CanonicalIoTests
    {
        [Test]
        public void RoundTrip_AllTypes_LittleEndian()
        {
            var w = new CanonicalWriter(16);
            w.WriteTag(0xAABBCCDD);
            w.WriteByte(7);
            w.WriteBool(true);
            w.WriteInt32(-2);
            w.WriteUInt32(uint.MaxValue);
            w.WriteInt64(long.MinValue);
            w.WriteUInt64(0x0102030405060708UL);
            w.WriteFixed64(Fixed64.Parse("-12.5"));
            w.WriteDate(GameDate.FromYmd(1923, 10, 29));
            w.WriteId(new CountryId(90));
            w.WriteString("Ankara — İzmir ğüşöç");
            w.WriteString(null);
            w.WriteString(string.Empty);
            w.WriteBytes(new byte[] { 9, 8, 7 });
            byte[] data = w.ToArray();
            Assert.That(data[0], Is.EqualTo(0xDD), "little-endian");

            var r = new CanonicalReader(data);
            r.ExpectTag(0xAABBCCDD);
            Assert.That(r.ReadByte(), Is.EqualTo(7));
            Assert.That(r.ReadBool(), Is.True);
            Assert.That(r.ReadInt32(), Is.EqualTo(-2));
            Assert.That(r.ReadUInt32(), Is.EqualTo(uint.MaxValue));
            Assert.That(r.ReadInt64(), Is.EqualTo(long.MinValue));
            Assert.That(r.ReadUInt64(), Is.EqualTo(0x0102030405060708UL));
            Assert.That(r.ReadFixed64(), Is.EqualTo(Fixed64.Parse("-12.5")));
            Assert.That(r.ReadDate(), Is.EqualTo(GameDate.FromYmd(1923, 10, 29)));
            Assert.That(r.ReadUInt64(), Is.EqualTo(90UL));
            Assert.That(r.ReadString(), Is.EqualTo("Ankara — İzmir ğüşöç"));
            Assert.That(r.ReadString(), Is.Null);
            Assert.That(r.ReadString(), Is.EqualTo(string.Empty));
            Assert.That(r.ReadBytes(), Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(r.IsAtEnd, Is.True);
            r.ExpectEnd();
        }

        [Test]
        public void Reader_RejectsTruncatedAndMalformedData()
        {
            var w = new CanonicalWriter();
            w.WriteInt64(5);
            byte[] data = w.ToArray();
            Assert.That(() => new CanonicalReader(data, 0, 7).ReadInt64(), Throws.TypeOf<FormatException>().With.Message.EqualTo(DomainErrorKeys.KayitBeklenmeyenSon));
            Assert.That(() => new CanonicalReader(new byte[] { 2 }).ReadBool(), Throws.TypeOf<FormatException>());
            Assert.That(() => new CanonicalReader(new byte[] { 1, 0, 0, 0 }).ExpectTag(2), Throws.TypeOf<FormatException>().With.Message.EqualTo(DomainErrorKeys.KayitEtiketUyusmuyor));
            Assert.That(() => new CanonicalReader(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }).ReadString(), Throws.TypeOf<FormatException>());
            Assert.That(() => new CanonicalReader(new byte[] { 2, 0, 0, 0, 0xC3, 0x28 }).ReadString(), Throws.TypeOf<FormatException>(), "geçersiz UTF-8");
            Assert.That(() => new CanonicalReader(new byte[] { 5, 0, 0, 0 }).ReadCount(4), Throws.TypeOf<FormatException>());
            Assert.That(() => new CanonicalReader(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).ReadDate(), Throws.TypeOf<FormatException>());
            Assert.That(() => new CanonicalReader(new byte[] { 1 }).ExpectEnd(), Throws.TypeOf<FormatException>());
        }
    }
}
