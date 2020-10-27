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
    /// A simple <see cref="IPriorityProvider{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of entries.</typeparam>
    public struct BasicPriorityProvider2<T> : IPriorityProvider2<T, T>
    {
        public BasicPriorityProvider2(IComparer<T> comparer)
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

        public T GetPriority(T element)
        {
            return element;
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

    public struct BasicPriorityProvider2<TElement, TPriority> : IPriorityProvider2<TElement, TPriority>
    {
        public BasicPriorityProvider2(IComparer<TPriority> priorityComparer, PrioritySelector<TElement, TPriority> prioritySelector)
        {
            PriorityComparer = priorityComparer ?? throw new ArgumentNullException(nameof(priorityComparer));
            PrioritySelector = prioritySelector ?? throw new ArgumentNullException(nameof(prioritySelector));
        }

        public IComparer<TPriority> PriorityComparer { get; }
        public PrioritySelector<TElement, TPriority> PrioritySelector { get; }

        public void Cleared()
        {
            // nop
        }

        public int Compare(TPriority l, TPriority r)
        {
            return PriorityComparer.Compare(l, r);
        }

        public TPriority GetPriority(TElement element)
        {
            return PrioritySelector(element);
        }

        public void Moved(TElement e, int i)
        {
            // nop
        }

        public void Removed(TElement e, int i)
        {
            // nop
        }
    }

    public delegate TPriority PrioritySelector<TElement, TPriority>(TElement element);

    /// <summary>
    /// A <see cref="IPriorityProvider{PriorityPair{TElement, TPriority}}"/> for <see cref="PriorityPair{TElement, TPriority}"/> which includes an index to support updating.
    /// </summary>
    /// <typeparam name="TElement">The type of elements.</typeparam>
    /// <typeparam name="TPriority">The type of priorities.</typeparam>
    public struct IndexedPriorityPairProvider2<TElement, TPriority> : IPriorityProvider2<TElement, TPriority>
    {
        private readonly Dictionary<TElement, int> _index;

        public IndexedPriorityPairProvider2(IEqualityComparer<TElement> elementComparer, IComparer<TPriority> priorityComparer, PrioritySelector<TElement, TPriority> prioritySelector)
        {
            elementComparer = elementComparer ?? throw new ArgumentNullException(nameof(elementComparer));
            PriorityComparer = priorityComparer ?? throw new ArgumentNullException(nameof(priorityComparer));
            PrioritySelector = prioritySelector ?? throw new ArgumentNullException(nameof(prioritySelector));
            _index = new Dictionary<TElement, int>(elementComparer);
        }

        public IReadOnlyDictionary<TElement, int> Index => _index;

        public IComparer<TPriority> PriorityComparer { get; }
        public PrioritySelector<TElement, TPriority> PrioritySelector { get; }

        public void Cleared()
        {
            _index.Clear();
        }

        public int Compare(TPriority l, TPriority r)
        {
            return PriorityComparer.Compare(l, r);
        }

        public TPriority GetPriority(TElement element)
        {
            return PrioritySelector(element);
        }

        public void Moved(TElement e, int i)
        {
            _index[e] = i;
        }

        public void Removed(TElement e, int i)
        {
            _index.Remove(e);
        }
    }

    public interface IPriorityProvider2<TElement, TPriority>
    {
        int Compare(TPriority l, TPriority r);
        TPriority GetPriority(TElement e);
        void Moved(TElement t, int to);
        void Removed(TElement t, int from);
        void Cleared();
    }

    public struct SimplePriorityQueue2<TElement, TPriority> : IReadOnlyCollection<TElement> where TElement : notnull
    {
        private readonly GenericPriorityQueue2<TElement, TPriority, BasicPriorityProvider2<TElement, TPriority>> _queue;

        public SimplePriorityQueue2(IComparer<TPriority> comparer, PrioritySelector<TElement, TPriority> prioritySelector)
        {
            _queue = new GenericPriorityQueue2<TElement, TPriority, BasicPriorityProvider2<TElement, TPriority>>(new BasicPriorityProvider2<TElement, TPriority>(comparer, prioritySelector));
        }

        public SimplePriorityQueue2(IComparer<TPriority> comparer, PrioritySelector<TElement, TPriority> prioritySelector, IEnumerable<TElement> entries)
        {
            _queue = new GenericPriorityQueue2<TElement, TPriority, BasicPriorityProvider2<TElement, TPriority>>(new BasicPriorityProvider2<TElement, TPriority>(comparer, prioritySelector), entries);
        }

        public void Enqueue(TElement element)
        {
            _queue.Add(element);
        }

        public TElement RemoveMin()
        {
            if (!TryRemoveMin(out var element))
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return element;
        }

        public bool TryRemoveMin(out TElement element)
        {
            return _queue.TryRemoveMin(out element);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        public int Count => _queue.Count;
    }

    public struct UpdateableUniquePriorityQueue2<TElement, TPriority> : IReadOnlyCollection<TElement> where TElement : notnull
    {
        private readonly GenericPriorityQueue2<TElement, TPriority, IndexedPriorityPairProvider2<TElement, TPriority>> _queue;

        private IReadOnlyDictionary<TElement, int> Index => _queue.Provider.Index;

        public UpdateableUniquePriorityQueue2(IEqualityComparer<TElement> elementComparer, IComparer<TPriority> priorityComparer, PrioritySelector<TElement, TPriority> prioritySelector)
        {
            _queue = new GenericPriorityQueue2<TElement, TPriority, IndexedPriorityPairProvider2<TElement, TPriority>>(new IndexedPriorityPairProvider2<TElement, TPriority>(elementComparer, priorityComparer, prioritySelector));
        }

        public void Enqueue(TElement element)
        {
            if (!TryEnqueue(element))
            {
                throw new ArgumentException("Element already present in queue.", nameof(element));
            }
        }

        public bool TryEnqueue(TElement element)
        {
            if (Index.ContainsKey(element))
            {
                return false;
            }
            else
            {
                _queue.Add(element);
                return true;
            }
        }

        public void Update(TElement element)
        {
            if (!TryUpdate(element))
            {
                throw new ArgumentException("Element already present in queue.", nameof(element));
            }
        }

        public bool TryUpdate(TElement element)
        {
            if (!Index.TryGetValue(element, out var index))
            {
                return false;
            }
            else
            {
                _queue.Update(index, element);
                return true;
            }
        }

        public void UpdateOrEnqueue(TElement element)
        {
            if (!TryUpdate(element))
            {
                Enqueue(element);
            }
        }

        public TElement RemoveMin()
        {
            if (!TryRemoveMin(out var element))
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return element;
        }

        public bool TryRemoveMin(out TElement element)
        {
            return _queue.TryRemoveMin(out element);
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

        public IEnumerator<TElement> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        public int Count => _queue.Count;
    }

    public class GenericPriorityQueue2<TElement, TPriority, TProvider> : IReadOnlyCollection<TElement> where TElement : notnull where TProvider : IPriorityProvider2<TElement, TPriority>
    {
        private const int Shift = 2;
        private const int ChildCount = 1 << Shift;

        private readonly TProvider _provider;

        private TElement[] _heap;

        private int _count;
        private int _version;

        private bool _supressNotify;

        public TProvider Provider => _provider;

        public GenericPriorityQueue2(TProvider provider)
        {
            _provider = provider;
            _heap = Array.Empty<TElement>();
            _count = 0;
            _version = int.MinValue;
        }

        public GenericPriorityQueue2(TProvider provider, IEnumerable<TElement> entries) : this(provider)
        {
            foreach (TElement t in entries)
            {
                EnsureCapacity(_count + 1);
                _heap[_count] = t;
                _count++;
            }

            Heapify();
        }

        public GenericPriorityQueue2(TProvider provider, IReadOnlyCollection<TElement> entries) : this(provider)
        {
            EnsureCapacity(entries.Count);

            foreach (TElement t in entries)
            {
                EnsureCapacity(_count + 1);
                _heap[_count] = t;
                _count++;
            }

            Heapify();
        }

        public int Count => _count;

        public void Add(TElement t)
        {
            _version++;
            AddInternal(t);
        }

        public TElement PeekMin()
        {
            if (TryPeekMin(out TElement found))
            {
                return found;
            }
            else
            {
                throw new InvalidOperationException("Cannot peek into an empty queue.");
            }
        }

        public bool TryPeekMin(out TElement entry)
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

        public TElement RemoveMin()
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

        public bool TryRemoveMin(out TElement entry)
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

        public TElement Peek(int index)
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

        public void Update(int index, TElement entry)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            _version++;
            UpdateIternal(index, entry);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<TElement>, IEnumerator
        {
            private readonly GenericPriorityQueue2<TElement, TPriority, TProvider> _queue;
            private readonly int _version;
            private int _index;
            private TElement _current;

            internal Enumerator(GenericPriorityQueue2<TElement, TPriority, TProvider> queue)
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

            public TElement Current => _current;
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
                TElement entry = _heap[i];
                PushDown(i, entry, false);
            }

            _supressNotify = suppress;

            for (int i = 0; i < _count; i++)
            {
                _provider.Moved(_heap[i], i);
            }
        }

        private void AddInternal(TElement t)
        {
            EnsureCapacity(Count + 1);

            PushUp(_count++, t, true);
        }

        private void RemoveInternal(int index, out TElement entry)
        {
            Debug.Assert(index < _count);

            entry = _heap[index];

            Provider.Removed(entry, index);

            int lastIndex = --_count;

            if (index < Count)
            {
                TElement last = _heap[lastIndex];
                PushDown(index, last, true);
            }

            if (MustClear)
            {
                _heap[_count] = default!;
            }
        }

        private void UpdateIternal(int index, TElement newEntry)
        {
            if (!PushUp(index, newEntry, false))
            {
                PushDown(index, newEntry, true);
            }
        }

        private bool PushUp(int index, TElement entry, bool moved)
        {
            var priority = Provider.GetPriority(entry);

            while (index > 0)
            {
                int parentIndex = (index - 1) >> Shift;
                TElement parent = _heap[parentIndex];

                if (_provider.Compare(Provider.GetPriority(parent), priority) <= 0)
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

        private bool PushDown(int index, TElement entry, bool moved)
        {
            var priority = Provider.GetPriority(entry);

            int minChildIndex;
            int count = _count;

            while ((minChildIndex = (index << Shift) + 1) < count)
            {
                TElement minChild = _heap[minChildIndex];
                TPriority minChildPriority = Provider.GetPriority(minChild);
                int childUpperBound = Math.Min(count, minChildIndex + ChildCount);

                for (int nextChildIndex = minChildIndex + 1; nextChildIndex < childUpperBound; nextChildIndex++)
                {
                    TElement nextChild = _heap[nextChildIndex];
                    TPriority nextChildPriority = Provider.GetPriority(_heap[nextChildIndex]);
                    if (_provider.Compare(nextChildPriority, minChildPriority) < 0)
                    {
                        minChildIndex = nextChildIndex;
                        minChild = nextChild;
                        minChildPriority = nextChildPriority;
                    }
                }

                if (_provider.Compare(priority, minChildPriority) <= 0)
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

        private bool MustClear => RuntimeHelpers.IsReferenceOrContainsReferences<TElement>();
    }
}