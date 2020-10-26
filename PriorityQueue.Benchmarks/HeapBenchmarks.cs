using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Collections.Generic;
using AlgoKit.Collections.Heaps;

namespace PriorityQueue.Benchmarks
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

    [MemoryDiagnoser]
    public class HeapBenchmarks
    {
        [Params(30, 1_000, 30_000, 100_100, 3_000_000)]
        public int Size;

        private int[] _priorities;
        //private PriorityQueue<int> _priorityQueue2;
        private PriorityQueue<int, int> _priorityQueue;
        private PrioritySet<int, int> _prioritySet;
        private PairingHeap<int, int> _pairingHeap;
        private SimplePriorityQueue<int, int> _simplePriorityQueue;
        private UpdateableUniquePriorityQueue<int, int> _updateableUniquePriorityQueue;
        private GenericPriorityQueue<PriorityPair<int, int>, IntPriorityPairProvider<int>> _intPairPriorityQueue;

        [GlobalSetup]
        public void Initialize()
        {
            var random = new Random(42);
            _priorities = new int[2 * Size];
            for (int i = 0; i < 2 * Size; i++)
            {
                _priorities[i] = random.Next();
            }

            //_priorityQueue2 = new PriorityQueue<int>(initialCapacity: Size);
            _priorityQueue = new PriorityQueue<int, int>(initialCapacity: Size);
            _prioritySet = new PrioritySet<int, int>(initialCapacity: Size);
            _pairingHeap = new PairingHeap<int, int>(Comparer<int>.Default);
            _simplePriorityQueue = new SimplePriorityQueue<int, int>(Comparer<int>.Default);
            _updateableUniquePriorityQueue = new UpdateableUniquePriorityQueue<int, int>(EqualityComparer<int>.Default, Comparer<int>.Default);
            _intPairPriorityQueue = new GenericPriorityQueue<PriorityPair<int, int>, IntPriorityPairProvider<int>>(new IntPriorityPairProvider<int>());
        }

        [Benchmark]
        public void PriorityQueue()
        {
            var queue = _priorityQueue;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                queue.Enqueue(i, priorities[i]);
            }

            for (int i = Size; i < 2 * Size; i++)
            {
                queue.Dequeue();
                queue.Enqueue(i, priorities[i]);
            }

            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void PrioritySet()
        {
            var queue = _prioritySet;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                queue.Enqueue(i, priorities[i]);
            }

            for (int i = Size; i < 2 * Size; i++)
            {
                queue.Dequeue();
                queue.Enqueue(i, priorities[i]);
            }

            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void PairingHeap()
        {
            var heap = _pairingHeap;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                heap.Add(priorities[i], i);
            }

            for (int i = Size; i < 2 * Size; i++)
            {
                heap.Pop();
                heap.Add(priorities[i], i);
            }

            while (heap.Count > 0)
            {
                heap.Pop();
            }
        }

        [Benchmark]
        public void SimplePriorityQueue()
        {
            var queue = _simplePriorityQueue;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                queue.Enqueue(i, priorities[i]);
            }

            for (int i = Size; i < 2 * Size; i++)
            {
                queue.RemoveMin();
                queue.Enqueue(i, priorities[i]);
            }

            while (queue.Count > 0)
            {
                queue.RemoveMin();
            }
        }

        [Benchmark]
        public void UpdateableUniquePriorityQueue()
        {
            var queue = _updateableUniquePriorityQueue;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                queue.Enqueue(i, priorities[i]);
            }

            for (int i = Size; i < 2 * Size; i++)
            {
                queue.RemoveMin();
                queue.Enqueue(i, priorities[i]);
            }

            while (queue.Count > 0)
            {
                queue.RemoveMin();
            }
        }

        [Benchmark]
        public void IntPairPriorityQueue()
        {
            var queue = _intPairPriorityQueue;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                queue.Add(new PriorityPair<int, int>(i, priorities[i]));
            }

            for (int i = Size; i < 2 * Size; i++)
            {
                queue.RemoveMin();
                queue.Add(new PriorityPair<int, int>(i, priorities[i]));
            }

            while (queue.Count > 0)
            {
                queue.RemoveMin();
            }
        }
    }
}
