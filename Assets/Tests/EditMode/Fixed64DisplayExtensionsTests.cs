using NUnit.Framework;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Unity.Presentation;

namespace TarihStrateji.Tests.EditMode
{
    public class Fixed64DisplayExtensionsTests
    {
        [TestCase("0", 0f)]
        [TestCase("1.5", 1.5f)]
        [TestCase("-2.25", -2.25f)]
        [TestCase("0.000001", 0.000001f)]
        [TestCase("123456.5", 123456.5f)]
        public void ToDisplayFloat_IsCloseToExactValue(string text, float expected)
        {
            Assert.That(Fixed64.Parse(text).ToDisplayFloat(), Is.EqualTo(expected).Within(1e-6f));
        }

        [Test]
        public void ToDisplayFloat_DoesNotOverflowAtExtremes()
        {
            Assert.That(float.IsFinite(Fixed64.MaxValue.ToDisplayFloat()), Is.True);
            Assert.That(float.IsFinite(Fixed64.MinValue.ToDisplayFloat()), Is.True);
        }
    }
}
