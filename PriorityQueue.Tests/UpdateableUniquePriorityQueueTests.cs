using System;
using Xunit;
using FsCheck;
using FsCheck.Xunit;
using System.Collections.Generic;
using System.Linq;

namespace PriorityQueue.Tests
{
    public static class UpdateableUniquePriorityQueueTests
    {
        [Fact]
        public static void Simple_Priority_Queue()
        {
            var pq = new UpdateableUniquePriorityQueue<string, int>(EqualityComparer<string>.Default, Comparer<int>.Default);

            pq.Enqueue("John", 1940);
            pq.Enqueue("Paul", 1942);
            pq.Enqueue("George", 1943);
            pq.Enqueue("Ringo", 1940);

            Assert.Equal("John", pq.RemoveMin().Element);
            Assert.Equal("Ringo", pq.RemoveMin().Element);
            Assert.Equal("Paul", pq.RemoveMin().Element);
            Assert.Equal("George", pq.RemoveMin().Element);
        }

        [Property(MaxTest = 10_000, Arbitrary = new Type[] { typeof(RandomGenerators) })]
        public static void HeapSort_Should_Work(int[] inputs)
        {
            int[] expected = inputs.OrderBy(inp => inp).ToArray();
            int[] actual = HeapSort(inputs).ToArray();
            Assert.Equal(expected, actual);

            static IEnumerable<int> HeapSort(int[] inputs)
            {
                var pq = new UpdateableUniquePriorityQueue<int, int>(EqualityComparer<int>.Default, Comparer<int>.Default);

                foreach (int input in inputs)
                {
                    pq.Enqueue(input, input);
                }

                Assert.Equal(inputs.Length, pq.Count);

                while (pq.Count > 0)
                {
                    yield return pq.RemoveMin().Element;
                }
            }
        }

        [Property(MaxTest = 10_000, Arbitrary = new Type[] { typeof(RandomGenerators) })]
        public static void Removing_Elements_Should_Work(int[] inputs)
        {
            var pq = new UpdateableUniquePriorityQueue<int, int>(EqualityComparer<int>.Default, Comparer<int>.Default);

            foreach (int input in inputs)
            {
                pq.Enqueue(input, input);
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.True(pq.TryRemove(inputs[i]));
                Assert.Equal(inputs.Length - i - 1, pq.Count);
            }

            Assert.Empty(pq);

            foreach (int input in inputs)
            {
                pq.Enqueue(input, input);
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.True(pq.TryRemove(inputs[i]));
                Assert.Equal(inputs.Length - i - 1, pq.Count);
            }

            Assert.Empty(pq);
        }

        public static class RandomGenerators
        {
            public static Arbitrary<string[]> DeduplicatedStringArray() => DeDuplicatedArray<string>();
            public static Arbitrary<int[]> DeduplicatedIntArray() => DeDuplicatedArray<int>();

            private static Arbitrary<T[]> DeDuplicatedArray<T>()
            {
                Arbitrary<T[]> defaultArray = Arb.Default.Array<T>();

                Gen<T[]> dedupedArray =
                    from array in defaultArray.Generator
                    select DeDup(array);

                return Arb.From(dedupedArray, Shrink);

                IEnumerable<T[]> Shrink(T[] input)
                {
                    foreach (T[] shrunk in defaultArray.Shrinker(input))
                    {
                        yield return DeDup(shrunk);
                    }
                }

                T[] DeDup (T[] input)
                {
                    return input
                        .Where(input => input != null)
                        .Distinct()
                        .ToArray();
                }
            }
        }
    }
}
