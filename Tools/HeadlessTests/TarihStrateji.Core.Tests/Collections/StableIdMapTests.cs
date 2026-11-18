using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Tests.Collections
{
    [TestFixture]
    public class StableIdMapTests
    {
        private static List<ulong> Keys(StableIdMap<CountryId, string> map)
        {
            var keys = new List<ulong>();
            foreach (KeyValuePair<CountryId, string> pair in map)
            {
                keys.Add(pair.Key.Value);
            }

            return keys;
        }

        [Test]
        public void Enumerates_InAscendingIdOrder_RegardlessOfInsertionOrder()
        {
            var map = new StableIdMap<CountryId, string>(0);
            foreach (ulong v in new ulong[] { 42, 7, 19, 1, 100, 8 })
            {
                map.Add(new CountryId(v), "u" + v);
            }

            Assert.That(Keys(map), Is.EqualTo(new ulong[] { 1, 7, 8, 19, 42, 100 }));
            Assert.That(map.KeyAt(0), Is.EqualTo(new CountryId(1)));
            Assert.That(map.ValueAt(5), Is.EqualTo("u100"));
            Assert.That(map.Count, Is.EqualTo(6));
        }

        [Test]
        public void Add_Get_Remove_Contracts()
        {
            var map = new StableIdMap<CountryId, string>();
            var id = new CountryId(3);
            map.Add(id, "a");
            Assert.Throws<ArgumentException>(() => map.Add(id, "b"));
            Assert.That(map.TryAdd(id, "b"), Is.False);
            Assert.That(map[id], Is.EqualTo("a"));
            map[id] = "c";
            Assert.That(map.TryGetValue(id, out string value) && value == "c", Is.True);
            map[new CountryId(1)] = "yeni";
            Assert.That(map.Count, Is.EqualTo(2));
            Assert.That(map.Remove(id), Is.True);
            Assert.That(map.Remove(id), Is.False);
            Assert.That(map.ContainsKey(id), Is.False);
            Assert.Throws<KeyNotFoundException>(() => _ = map[id]);
            Assert.Throws<ArgumentException>(() => map.Add(CountryId.None, "x"));
            Assert.That(map.ContainsKey(CountryId.None), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => map.KeyAt(5));
            map.Clear();
            Assert.That(map.Count, Is.EqualTo(0));
        }

        [Test]
        public void StructuralChange_DuringEnumeration_Throws()
        {
            var map = new StableIdMap<CountryId, string>();
            map.Add(new CountryId(1), "a");
            map.Add(new CountryId(2), "b");
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<CountryId, string> pair in map)
                {
                    map.Add(new CountryId(pair.Key.Value + 10), "x");
                }
            });
        }

        [Test]
        public void ValueUpdate_DuringEnumeration_IsAllowed()
        {
            var map = new StableIdMap<CountryId, string>();
            map.Add(new CountryId(1), "a");
            map.Add(new CountryId(2), "b");
            int index = 0;
            foreach (KeyValuePair<CountryId, string> pair in map)
            {
                map[pair.Key] = pair.Value.ToUpperInvariant();
                map.SetValueAt(index, map.ValueAt(index) + "!");
                index++;
            }

            Assert.That(map.ValueAt(0), Is.EqualTo("A!"));
            Assert.That(map.ValueAt(1), Is.EqualTo("B!"));
        }

        [Test]
        public void RandomOperations_MatchSortedReference()
        {
            var rng = new Random(1453);
            var map = new StableIdMap<CountryId, string>();
            var reference = new SortedDictionary<ulong, string>();
            for (int i = 0; i < 20_000; i++)
            {
                ulong key = (ulong)rng.Next(1, 500);
                int op = rng.Next(0, 3);
                if (op == 0)
                {
                    Assert.That(map.TryAdd(new CountryId(key), "v" + i), Is.EqualTo(reference.TryAdd(key, "v" + i)));
                }
                else if (op == 1)
                {
                    Assert.That(map.Remove(new CountryId(key)), Is.EqualTo(reference.Remove(key)));
                }
                else
                {
                    map[new CountryId(key)] = "w" + i;
                    reference[key] = "w" + i;
                }
            }

            Assert.That(Keys(map), Is.EqualTo(reference.Keys.ToList()));
            for (int i = 0; i < map.Count; i++)
            {
                Assert.That(map.ValueAt(i), Is.EqualTo(reference[map.KeyAt(i).Value]));
            }
        }
    }
}
