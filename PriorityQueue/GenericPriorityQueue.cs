using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.XPath;

namespace PriorityQueue
{
    /// <summary>
    /// A pair consisting of an element and its priority.
    /// </summary>
    /// <typeparam name="TElement">The type of element.</typeparam>
    /// <typeparam name="TPriority">The type of priority.</typeparam>
    public struct PriorityPair<TElement, TPriority>
    {
        public PriorityPair(TElement element, TPriority priority)
        {
            Element = element;
            Priority = priority;
        }

        public TElement Element { get; }
        public TPriority Priority { get; }
    }

    /// <summary>
    /// A simple <see cref="IPriorityProvider{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of entries.</typeparam>
    public struct BasicPriorityProvider<T> : IPriorityProvider<T>
    {
        public BasicPriorityProvider(IComparer<T> comparer)
        {
            Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public IComparer<T> Comparer { get; }

        public void Cleared()
        {
            // nop
        }

        public int Compare(T l, T r)
        {
            return Comparer.Compare(l, r);
        }

        public void Moved(T t, int i)
        {
            // nop
        }

        public void Removed(T t, int i)
        {
            // nop
        }
    }

    /// <summary>
    /// A simple <see cref="IPriorityProvider{PriorityPair{TElement, TPriority}}"/> for <see cref="PriorityPair{TElement, TPriority}"/>.
    /// </summary>
    /// <typeparam name="TElement">The type of elements.</typeparam>
    /// <typeparam name="TPriority">The type of priorities.</typeparam>
    public struct BasicPriorityPairProvider<TElement, TPriority> : IPriorityProvider<PriorityPair<TElement, TPriority>>
    {
        public BasicPriorityPairProvider(IComparer<TPriority> priorityComparer)
        {
            PriorityComparer = priorityComparer ?? throw new ArgumentNullException(nameof(priorityComparer));
        }

        public IComparer<TPriority> PriorityComparer { get; }

        public void Cleared()
        {
            // nop
        }

        public int Compare(PriorityPair<TElement, TPriority> l, PriorityPair<TElement, TPriority> r)
        {
            return PriorityComparer.Compare(l.Priority, r.Priority);
        }

        public void Moved(PriorityPair<TElement, TPriority> t, int i)
        {
            // nop
        }

        public void Removed(PriorityPair<TElement, TPriority> t, int i)
        {
            // nop
        }
    }

    /// <summary>
    /// A <see cref="IPriorityProvider{PriorityPair{TElement, TPriority}}"/> for <see cref="PriorityPair{TElement, TPriority}"/> which includes an index to support updating.
    /// </summary>
    /// <typeparam name="TElement">The type of elements.</typeparam>
    /// <typeparam name="TPriority">The type of priorities.</typeparam>
    public struct IndexedPriorityPairProvider<TElement, TPriority> : IPriorityProvider<PriorityPair<TElement, TPriority>>
    {
        private readonly Dictionary<TElement, int> _index;

        public IndexedPriorityPairProvider(IEqualityComparer<TElement> elementComparer, IComparer<TPriority> priorityComparer)
        {
            elementComparer = elementComparer ?? throw new ArgumentNullException(nameof(elementComparer));
            PriorityComparer = priorityComparer ?? throw new ArgumentNullException(nameof(priorityComparer));
            _index = new Dictionary<TElement, int>(elementComparer);
        }

        public IReadOnlyDictionary<TElement, int> Index => _index;

        public IComparer<TPriority> PriorityComparer { get; }

        public void Cleared()
        {
            _index.Clear();
        }

        public int Compare(PriorityPair<TElement, TPriority> l, PriorityPair<TElement, TPriority> r)
        {
            return PriorityComparer.Compare(l.Priority, r.Priority);
        }

        public void Moved(PriorityPair<TElement, TPriority> t, int i)
        {
            _index[t.Element] = i;
        }

        public void Removed(PriorityPair<TElement, TPriority> t, int i)
        {
            _index.Remove(t.Element);
        }
    }

    public struct SimplePriorityQueue<T> : IReadOnlyCollection<T> where T : notnull
    {
        private readonly GenericPriorityQueue<T, BasicPriorityProvider<T>> _queue;

        public SimplePriorityQueue(IComparer<T> comparer)
        {
            _queue = new GenericPriorityQueue<T, BasicPriorityProvider<T>>(new BasicPriorityProvider<T>(comparer));
        }

        public SimplePriorityQueue(IComparer<T> comparer, IEnumerable<T> entries)
        {
            _queue = new GenericPriorityQueue<T, BasicPriorityProvider<T>>(new BasicPriorityProvider<T>(comparer), entries);
        }

        public void Enqueue(T element)
        {
            _queue.Add(element);
        }

        public T RemoveMin()
        {
            if (!TryRemoveMin(out var element))
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return element;
        }

        public bool TryRemoveMin(out T pair)
        {
            return _queue.TryRemoveMin(out pair);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        public int Count => _queue.Count;
    }

    public struct SimplePriorityQueue<TElement, TPriority> : IReadOnlyCollection<PriorityPair<TElement, TPriority>>
    {
        private readonly GenericPriorityQueue<PriorityPair<TElement, TPriority>, BasicPriorityPairProvider<TElement, TPriority>> _queue;

        public SimplePriorityQueue(IComparer<TPriority> priorityComparer)
        {
            _queue = new GenericPriorityQueue<PriorityPair<TElement, TPriority>, BasicPriorityPairProvider<TElement, TPriority>>(new BasicPriorityPairProvider<TElement, TPriority>(priorityComparer));
        }

        public SimplePriorityQueue(IComparer<TPriority> priorityComparer, IEnumerable<PriorityPair<TElement, TPriority>> entries)
        {
            _queue = new GenericPriorityQueue<PriorityPair<TElement, TPriority>, BasicPriorityPairProvider<TElement, TPriority>>(new BasicPriorityPairProvider<TElement, TPriority>(priorityComparer), entries);
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            _queue.Add(new PriorityPair<TElement, TPriority>(element, priority));
        }

        public PriorityPair<TElement, TPriority> RemoveMin()
        {
            if (!TryRemoveMin(out var pair))
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return pair;
        }

        public bool TryRemoveMin(out PriorityPair<TElement, TPriority> pair)
        {
            return _queue.TryRemoveMin(out pair);
        }

        public IEnumerator<PriorityPair<TElement, TPriority>> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        public int Count => _queue.Count;
    }

    public struct UpdateableUniquePriorityQueue<TElement, TPriority> : IReadOnlyCollection<PriorityPair<TElement, TPriority>>
    {
        private readonly GenericPriorityQueue<PriorityPair<TElement, TPriority>, IndexedPriorityPairProvider<TElement, TPriority>> _queue;

        private IReadOnlyDictionary<TElement, int> Index => _queue.Provider.Index;

        public UpdateableUniquePriorityQueue(IEqualityComparer<TElement> elementComparer, IComparer<TPriority> priorityComparer)
        {
            _queue = new GenericPriorityQueue<PriorityPair<TElement, TPriority>, IndexedPriorityPairProvider<TElement, TPriority>>(new IndexedPriorityPairProvider<TElement, TPriority>(elementComparer, priorityComparer));
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (!TryEnqueue(element, priority))
            {
                throw new ArgumentException("Element already present in queue.", nameof(element));
            }
        }

        public bool TryEnqueue(TElement element, TPriority priority)
        {
            if (Index.ContainsKey(element))
            {
                return false;
            }
            else
            {
                _queue.Add(new PriorityPair<TElement, TPriority>(element, priority));
                return true;
            }
        }

        public void Update(TElement element, TPriority priority)
        {
            if (!TryUpdate(element, priority))
            {
                throw new ArgumentException("Element already present in queue.", nameof(element));
            }
        }

        public bool TryUpdate(TElement element, TPriority priority)
        {
            if (!Index.ContainsKey(element))
            {
                return false;
            }
            else
            {
                _queue.Add(new PriorityPair<TElement, TPriority>(element, priority));
                return true;
            }
        }

        public void UpdateOrEnqueue(TElement element, TPriority priority)
        {
            if (!TryUpdate(element, priority))
            {
                Enqueue(element, priority);
            }
        }

        public PriorityPair<TElement, TPriority> RemoveMin()
        {
            if (!TryRemoveMin(out var pair))
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return pair;
        }

        public bool TryRemoveMin(out PriorityPair<TElement, TPriority> pair)
        {
            return _queue.TryRemoveMin(out pair);
        }

        public void Remove(TElement element)
        {
            if (!TryRemove(element))
            {
                throw new ArgumentException("Element not in queue.", nameof(element));
            }
        }

        public bool TryRemove(TElement element)
        {
            if (!Index.ContainsKey(element))
            {
                return false;
            }
            else
            {
                _queue.Remove(Index[element]);
                return true;
            }
        }

        public IEnumerator<PriorityPair<TElement, TPriority>> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        public int Count => _queue.Count;
    }

    public interface IPriorityProvider<T>
    {
        int Compare(T l, T r);
        void Moved(T t, int i);
        void Removed(T t, int i);
        void Cleared();
    }

    public class GenericPriorityQueue<T, TProvider> : IReadOnlyCollection<T> where T : notnull where TProvider : IPriorityProvider<T>
    {
        private const int Shift = 2;
        private const int ChildCount = 1 << Shift;

        private readonly TProvider _provider;

        private T[] _heap;

        private int _count;
        private int _version;

        private bool _supressNotify;

        public TProvider Provider => _provider;

        public GenericPriorityQueue(TProvider provider)
        {
            _provider = provider;
            _heap = Array.Empty<T>();
            _count = 0;
            _version = int.MinValue;
        }

        public GenericPriorityQueue(TProvider provider, IEnumerable<T> entries) : this(provider)
        {
            foreach (T t in entries)
            {
                EnsureCapacity(_count + 1);
                _heap[_count] = t;
                _count++;
            }

            Heapify();
        }

        public GenericPriorityQueue(TProvider provider, IReadOnlyCollection<T> entries) : this(provider)
        {
            EnsureCapacity(entries.Count);

            foreach (T t in entries)
            {
                EnsureCapacity(_count + 1);
                _heap[_count] = t;
                _count++;
            }

            Heapify();
        }

        public int Count => _count;

        public void Add(T t)
        {
            _version++;
            AddInternal(t);
        }

        public T PeekMin()
        {
            if (TryPeekMin(out T found))
            {
                return found;
            }
            else
            {
                throw new InvalidOperationException("Cannot peek into an empty queue.");
            }
        }

        public bool TryPeekMin(out T entry)
        {
            if (_count == 0)
            {
                entry = default!;
                return false;
            }
            else
            {
                entry = _heap[0];
                return true;
            }
        }

        public T RemoveMin()
        {
            if (TryRemoveMin(out var found))
            {
                return found;
            }
            else
            {
                throw new InvalidOperationException("Cannot peek into an empty queue.");
            }
        }

        public bool TryRemoveMin(out T entry)
        {
            if (_count == 0)
            {
                entry = default!;
                return false;
            }
            else
            {
                _version++;
                RemoveInternal(0, out entry);
                return true;
            }
        }

        public void Clear()
        {
            _version++;
            if (_count > 0)
            {
                if (MustClear)
                {
                    Array.Clear(_heap, 0, _count);
                }

                _count = 0;

                _provider.Cleared();
            }
        }

        public T Peek(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            return _heap[index];
        }

        public void Remove(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            _version++;
            RemoveInternal(index, out var _);
        }

        public void Update(int index, T entry)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            _version++;
            UpdateIternal(index, entry);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly GenericPriorityQueue<T, TProvider> _queue;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(GenericPriorityQueue<T, TProvider> queue)
            {
                _version = queue._version;
                _queue = queue;
                _index = 0;
                _current = default!;
            }

            public bool MoveNext()
            {
                if (_queue._version == _version && _index < _queue._count)
                {
                    _current = _queue._heap[_index];
                    _index++;
                    return true;
                }

                if (_queue._version != _version)
                {
                    throw new InvalidOperationException("Collection was modified.");
                }

                return false;
            }

            public T Current => _current;
            object IEnumerator.Current => _current;

            public void Reset()
            {
                if (_queue._version != _version)
                {
                    throw new InvalidOperationException("collection was modified");
                }

                _index = 0;
                _current = default!;
            }

            void IDisposable.Dispose()
            {
            }
        }

        private void Heapify()
        {
            var suppress = _supressNotify;
            _supressNotify = true;

            for (int i = (_count - 1) >> Shift; i >= 0; i--)
            {
                T entry = _heap[i];
                PushDown(i, entry, false);
            }

            _supressNotify = suppress;

            for (int i = 0; i < _count; i++)
            {
                _provider.Moved(_heap[i], i);
            }
        }

        private void AddInternal(T t)
        {
            EnsureCapacity(Count + 1);

            PushUp(_count++, t, true);
        }

        private void RemoveInternal(int index, out T entry)
        {
            Debug.Assert(index < _count);

            entry = _heap[index];

            Provider.Removed(entry, index);

            int lastIndex = --_count;

            if (index < Count)
            {
                T last = _heap[lastIndex];
                PushDown(index, last, true);
            }

            if (MustClear)
            {
                _heap[_count] = default!;
            }
        }

        private void UpdateIternal(int index, T newEntry)
        {
            if (!PushUp(index, newEntry, false))
            {
                PushDown(index, newEntry, true);
            }
        }

        private bool PushUp(int index, T entry, bool moved)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) >> Shift;
                T parent = _heap[parentIndex];

                if (_provider.Compare(parent, entry) <= 0)
                {
                    break;
                }

                _heap[index] = parent;
                _provider.Moved(parent, index);

                index = parentIndex;
                moved = true;
            }

            if (moved)
            {
                _heap[index] = entry;
                _provider.Moved(entry, index);
            }

            return moved;
        }

        private bool PushDown(int index, T entry, bool moved)
        {
            int minChildIndex;
            int count = _count;

            while ((minChildIndex = (index << Shift) + 1) < count)
            {
                T minChild = _heap[minChildIndex];
                int childUpperBound = Math.Min(count, minChildIndex + ChildCount);

                for (int nextChildIndex = minChildIndex + 1; nextChildIndex < childUpperBound; nextChildIndex++)
                {
                    T nextChild = _heap[nextChildIndex];
                    if (_provider.Compare(nextChild, minChild) < 0)
                    {
                        minChildIndex = nextChildIndex;
                        minChild = nextChild;
                    }
                }

                if (_provider.Compare(entry, minChild) <= 0)
                {
                    break;
                }

                _heap[index] = minChild;
                _provider.Moved(minChild, index);

                index = minChildIndex;
                moved = true;
            }

            if (moved)
            {
                _heap[index] = entry;
                _provider.Moved(entry, index);
            }

            return moved;
        }

        private void EnsureCapacity(int required)
        {
            if (_heap.Length < required)
            {
                Array.Resize(ref _heap, Math.Max(_heap.Length * 2, required));
            }
        }

        private bool MustClear => RuntimeHelpers.IsReferenceOrContainsReferences<T>();
    }
}