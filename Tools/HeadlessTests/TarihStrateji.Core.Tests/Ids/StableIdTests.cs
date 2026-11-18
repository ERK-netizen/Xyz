using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Tests.Ids
{
    [TestFixture]
    public class StableIdTests
    {
        [Test]
        public void None_IsInvalid_AndZero()
        {
            Assert.That(CountryId.None.IsValid, Is.False);
            Assert.That(CountryId.None.Value, Is.EqualTo(0UL));
            Assert.That(default(HexId), Is.EqualTo(HexId.None));
            Assert.That(new CityId(1).IsValid, Is.True);
        }

        [Test]
        public void Equality_Ordering_AndFormatting()
        {
            var a = new LandUnitId(5);
            var b = new LandUnitId(7);
            Assert.That(a == new LandUnitId(5), Is.True);
            Assert.That(a != b, Is.True);
            var a2 = new LandUnitId(5);
            Assert.That(a < b && b > a && a <= a2 && a >= a2 && !(b <= a), Is.True);
            Assert.That(a.CompareTo(b), Is.LessThan(0));
            Assert.That(a.Equals((object)new LandUnitId(5)), Is.True);
            Assert.That(a.Equals((object)new FleetId(5)), Is.False, "farklı kimlik tipleri asla eşit değildir");
            Assert.That(a.ToString(), Is.EqualTo("LandUnitId(5)"));
        }

        [Test]
        public void AllIdTypes_ImplementTheSameContract()
        {
            Type[] idTypes = typeof(IStableId).Assembly.GetTypes()
                .Where(t => t.IsValueType && typeof(IStableId).IsAssignableFrom(t))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(idTypes.Length, Is.GreaterThanOrEqualTo(15));
            foreach (Type t in idTypes)
            {
                Assert.That(typeof(IEquatable<>).MakeGenericType(t).IsAssignableFrom(t), Is.True, t.Name);
                Assert.That(typeof(IComparable<>).MakeGenericType(t).IsAssignableFrom(t), Is.True, t.Name);
                Assert.That(t.GetField("None")?.IsStatic, Is.True, t.Name + ".None");
                Assert.That(t.IsDefined(typeof(SerializableAttribute), false), Is.True, t.Name);
            }
        }

        [Test]
        public void Allocator_IsMonotonic_AndNeverReuses()
        {
            var allocator = new StableIdAllocator();
            var seen = new HashSet<ulong>();
            ulong previous = 0;
            for (int i = 0; i < 1000; i++)
            {
                ulong v = allocator.Allocate();
                Assert.That(v, Is.GreaterThan(previous));
                Assert.That(seen.Add(v), Is.True);
                previous = v;
            }

            Assert.That(allocator.NextValue, Is.EqualTo(1001UL));
        }

        [Test]
        public void Allocator_RestoresFromSave_AndReserves()
        {
            var restored = new StableIdAllocator(500);
            Assert.That(restored.Allocate(), Is.EqualTo(500UL));

            restored.Reserve(900);
            Assert.That(restored.Allocate(), Is.EqualTo(901UL));

            restored.Reserve(10); // geride kalan rezervasyon geri sarmaz
            Assert.That(restored.Allocate(), Is.EqualTo(902UL));

            Assert.Throws<ArgumentOutOfRangeException>(() => new StableIdAllocator(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => restored.Reserve(0));
            Assert.Throws<InvalidOperationException>(() => restored.Reserve(ulong.MaxValue));
        }

        [Test]
        public void Allocator_Exhaustion_Throws()
        {
            var allocator = new StableIdAllocator(ulong.MaxValue - 1UL);
            Assert.That(allocator.Allocate(), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.Throws<InvalidOperationException>(() => allocator.Allocate());
        }
    }
}
