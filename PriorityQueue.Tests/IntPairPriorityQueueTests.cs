using System;
using Xunit;
using FsCheck;
using FsCheck.Xunit;
using System.Collections.Generic;
using System.Linq;

namespace PriorityQueue.Tests
{
    public struct IntPriorityPairProvider<T> : IPriorityProvider<PriorityPair<T, int>>
    {
        public void Cleared()
        {
        }

        public int Compare(PriorityPair<T, int> l, PriorityPair<T, int> r)
        {
            return l.Priority.CompareTo(r.Priority);
        }

        public void Moved(PriorityPair<T, int> t, int i)
        {
        }

        public void Removed(PriorityPair<T, int> t, int i)
        {
        }
    }

    public static class IntPairPriorityQueueTests
    {
        [Property(MaxTest = 10_000, Arbitrary = new Type[] { typeof(RandomGenerators) })]
        public static void HeapSort_Should_Work(int[] inputs)
        {
            int[] expected = inputs.OrderBy(inp => inp).ToArray();
            int[] actual = HeapSort(inputs).ToArray();
            Assert.Equal(expected, actual);

            static IEnumerable<int> HeapSort(int[] inputs)
            {
                var pq = new GenericPriorityQueue<PriorityPair<int, int>, IntPriorityPairProvider<int>>(new IntPriorityPairProvider<int>());

                foreach (int input in inputs)
                {
                    pq.Add(new PriorityPair<int, int>(input, input));
                }

                Assert.Equal(inputs.Length, pq.Count);

                while (pq.Count > 0)
                {
                    yield return pq.RemoveMin().Element;
                }
            }
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
