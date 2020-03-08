using System;
using System.Linq;
using NUnit.Framework;
using TzspServer;

namespace TestTzspServer.Tests
{
    [TestFixture]
    public class TestCircularBuffer
    {
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-2)]
        public void ThrowsOnInvalidSize(int size)
        {
            Assert.That(() => new CircularBuffer<int>(size), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(17)]
        [TestCase(31845)]
        [TestCase(1_826_952)]
        public void ValidCircle(int size)
        {
            Random rnd = new Random();
            var inputs = Enumerable.Range(1, size * 5).Select(_ => rnd.Next()).ToArray();
            var buffer = new CircularBuffer<int>(size);

            foreach (var input in inputs)
                buffer.Add(input);

            foreach (var expected in inputs.TakeLast(size))
            {
                Assert.That(buffer.TryTake(out var actual), Is.True);
                Assert.That(actual, Is.EqualTo(expected));
            }
            for(int i = 0; i < size; ++i)
                Assert.That(buffer.TryTake(out _), Is.False);
        }
    }
}
