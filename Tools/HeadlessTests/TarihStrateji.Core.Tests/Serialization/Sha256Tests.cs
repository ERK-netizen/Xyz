using System;
using System.Text;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Tests.Support;

namespace TarihStrateji.Core.Tests.Serialization
{
    [TestFixture]
    public class Sha256Tests
    {
        [TestCase("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [TestCase("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
        [TestCase("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")]
        public void NistVectors(string input, string expected)
        {
            Assert.That(Sha256.ToHex(Sha256.Compute(Encoding.ASCII.GetBytes(input))), Is.EqualTo(expected));
        }

        [Test]
        public void MatchesPlatformImplementation_ForEveryLengthAroundBlockBoundaries()
        {
            var rng = new TestRng(256);
            for (int length = 0; length <= 300; length++)
            {
                var data = new byte[length + 7];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)rng.Next(256);
                }

                byte[] expected = System.Security.Cryptography.SHA256.HashData(data.AsSpan(3, length));
                Assert.That(Sha256.Compute(data, 3, length), Is.EqualTo(expected), $"uzunluk {length}");
            }
        }

        [Test]
        public void AreEqual_ComparesContent()
        {
            byte[] a = Sha256.Compute(new byte[] { 1 });
            Assert.That(Sha256.AreEqual(a, (byte[])a.Clone()), Is.True);
            Assert.That(Sha256.AreEqual(a, Sha256.Compute(new byte[] { 2 })), Is.False);
            Assert.That(Sha256.AreEqual(a, null), Is.False);
            Assert.That(Sha256.AreEqual(a, new byte[31]), Is.False);
        }
    }
}
