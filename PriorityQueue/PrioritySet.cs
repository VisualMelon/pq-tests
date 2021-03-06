﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PriorityQueue
{
    public class PrioritySet<TElement, TPriority> : IReadOnlyCollection<(TElement Element, TPriority Priority)> where TElement : notnull
    {
        private const int DefaultCapacity = 4;

        private readonly IComparer<TPriority> _priorityComparer;
        private readonly Dictionary<TElement, int> _index;

        private TPriority[] _priorities;
        private TElement[] _elements;
        private int _count;
        private int _version;

        #region Constructors
        public PrioritySet() : this(0, null, null)
        {

        }

        public PrioritySet(int initialCapacity) : this(initialCapacity, null, null)
        {

        }

        public PrioritySet(IComparer<TPriority> comparer) : this(0, null, null)
        {

        }

        public PrioritySet(int initialCapacity, IComparer<TPriority>? priorityComparer, IEqualityComparer<TElement>? elementComparer)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            if (initialCapacity == 0)
            {
                _priorities = Array.Empty<TPriority>();
                _elements = Array.Empty<TElement>();
            }
            else
            {
                _priorities = new TPriority[initialCapacity];
                _elements = new TElement[initialCapacity];
            }

            _index = new Dictionary<TElement, int>(initialCapacity, comparer: elementComparer);
            _priorityComparer = priorityComparer ?? Comparer<TPriority>.Default;
        }

        public PrioritySet(IEnumerable<(TElement Element, TPriority Priority)> values) : this(values, null, null)
        {

        }

        public PrioritySet(IEnumerable<(TElement Element, TPriority Priority)> values, IComparer<TPriority>? comparer, IEqualityComparer<TElement>? elementComparer)
        {
            var priorities = Array.Empty<TPriority>();
            var elements = Array.Empty<TElement>();
            var heapIndex = new Dictionary<TElement, int>(elementComparer);
            int count = 0;

            foreach ((TElement element, TPriority priority) in values)
            {
                if (count == priorities.Length)
                {
                    Resize(ref priorities, ref elements);
                }

                if (!heapIndex.TryAdd(element, count))
                {
                    throw new ArgumentException("duplicate elements", nameof(values));
                }

                priorities[count] = priority;
                elements[count] = element;
                count++;
            }

            _priorities = priorities;
            _elements = elements;
            _index = heapIndex;
            _priorityComparer = comparer ?? Comparer<TPriority>.Default;
            _count = count;

            Heapify();
        }
        #endregion

        public int Count => _count;
        public IComparer<TPriority> Comparer => _priorityComparer;

        public void Enqueue(TElement element, TPriority priority)
        {
            if (_index.ContainsKey(element))
            {
                throw new InvalidOperationException("Duplicate element");
            }

            _version++;
            Insert(element, priority);
        }

        public TElement Peek()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("queue is empty");
            }

            return _elements[0];
        }

        public bool TryPeek(out TElement element, out TPriority priority)
        {
            if (_count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            element = _elements[0];
            priority = _priorities[0];
            return true;
        }

        public TElement Dequeue()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("queue is empty");
            }

            _version++;
            RemoveIndex(index: 0, out TElement result, out TPriority _);
            return result;
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (_count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            _version++;
            RemoveIndex(index: 0, out element, out priority);
            return true;
        }

        public TElement EnqueueDequeue(TElement element, TPriority priority)
        {
            if (_count == 0 || _priorityComparer.Compare(priority, _priorities[0]) <= 0)
            {
                return element;
            }

            _version++;
            TElement minElement = _elements[0];
            _index.Remove(minElement);
            SiftDown(index: 0, in element, in priority);

            return minElement;
        }

        public void Clear()
        {
            _version++;
            if (_count > 0)
            {
                //TODO: guard with RuntimeHelpers.IsReferenceOrContainsReferences<>()
                Array.Clear(_priorities, 0, _count);
                Array.Clear(_elements, 0, _count);
                _index.Clear();
                _count = 0;
            }
        }

        public bool Contains(TElement element) => _index.ContainsKey(element);

        public bool TryRemove(TElement element)
        {
            if (!_index.TryGetValue(element, out int index))
            {
                return false;
            }

            _version++;
            RemoveIndex(index, out var _, out var _);
            return true;
        }

        public bool TryUpdate(TElement element, TPriority priority)
        {
            if (!_index.TryGetValue(element, out int index))
            {
                return false;
            }

            _version++;
            UpdateIndex(index, priority);
            return true;
        }

        public void EnqueueOrUpdate(TElement element, TPriority priority)
        {
            _version++;
            if (_index.TryGetValue(element, out int index))
            {
                UpdateIndex(index, priority);
            }
            else
            {
                Insert(element, priority);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<(TElement Element, TPriority Priority)>, IEnumerator
        {
            private readonly PrioritySet<TElement, TPriority> _queue;
            private readonly int _version;
            private int _index;
            private (TElement Element, TPriority Priority) _current;

            internal Enumerator(PrioritySet<TElement, TPriority> queue)
            {
                _version = queue._version;
                _queue = queue;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                PrioritySet<TElement, TPriority> queue = _queue;

                if (queue._version == _version && _index < queue._count)
                {
                    _current = (queue._elements[_index], queue._priorities[_index]);
                    _index++;
                    return true;
                }

                if (queue._version != _version)
                {
                    throw new InvalidOperationException("collection was modified");
                }

                return false;
            }

            public (TElement Element, TPriority Priority) Current => _current;
            object IEnumerator.Current => _current;

            public void Reset()
            {
                if (_queue._version != _version)
                {
                    throw new InvalidOperationException("collection was modified");
                }

                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }

        #region Private Methods

        private void Heapify()
        {
            for (int i = (_count  - 1) >> 2; i >= 0; i--)
            {
                TElement element = _elements[i];
                TPriority priority = _priorities[i];
                SiftDown(index: i, element, priority);
            }
        }

        private void Insert(in TElement element, in TPriority priority)
        {
            if (_count == _priorities.Length)
            {
                Resize(ref _priorities, ref _elements);
            }

            SiftUp(index: _count++, in element, in priority);
        }

        private void RemoveIndex(int index, out TElement element, out TPriority priority)
        {
            Debug.Assert(index < _count);

            element = _elements[index];
            priority = _priorities[index];

            int lastElementPos = --_count;
            ref TPriority lastPriority = ref _priorities[lastElementPos];
            ref TElement lastElement = ref _elements[lastElementPos];

            if (lastElementPos > 0)
            {
                SiftDown(index, in lastElement, in lastPriority);
            }

            lastPriority = default!;
            lastElement = default!;
            _index.Remove(element);
        }

        private void UpdateIndex(int index, TPriority newPriority)
        {
            TElement element;

            switch (_priorityComparer.Compare(newPriority, _priorities[index]))
            {
                // priority is decreased, sift upward
                case < 0:
                    element = _elements[index];
                    SiftUp(index, element, newPriority);
                    return;

                // priority is increased, sift downward
                case > 0:
                    element = _elements[index];
                    SiftDown(index, element, newPriority);
                    return;

                // priority is same as before, take no action
                case 0: return;
            }
        }

        private void SiftUp(int index, in TElement element, in TPriority priority)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) >> 2;
                ref TPriority parentPriority = ref _priorities[parentIndex];

                if (_priorityComparer.Compare(parentPriority, priority) <= 0)
                {
                    // parentPriority <= priority, heap property is satisfed
                    break;
                }

                ref TElement parentElement = ref _elements[parentIndex];
                _priorities[index] = parentPriority;
                _elements[index] = parentElement;
                _index[parentElement] = index;
                index = parentIndex;
            }

            _priorities[index] = priority;
            _elements[index] = element;
            _index[element] = index;
        }

        private void SiftDown(int index, in TElement element, in TPriority priority)
        {
            int minChildIndex;
            int count = _count;
            TPriority[] priorities = _priorities;
            TElement[] elements = _elements;

            while ((minChildIndex = (index << 2) + 1) < count)
            {
                // find the child with the minimal priority
                ref TPriority minChildPriority = ref priorities[minChildIndex];
                int childUpperBound = Math.Min(count, minChildIndex + 4);

                for (int nextChildIndex = minChildIndex + 1; nextChildIndex < childUpperBound; nextChildIndex++)
                {
                    ref TPriority nextChildPriority = ref priorities[nextChildIndex];
                    if (_priorityComparer.Compare(nextChildPriority, minChildPriority) < 0)
                    {
                        minChildIndex = nextChildIndex;
                        minChildPriority = ref nextChildPriority;
                    }
                }

                // compare with inserted priority
                if (_priorityComparer.Compare(priority, minChildPriority) <= 0)
                {
                    // priority <= childPriority, heap property is satisfied
                    break;
                }

                ref TElement childElement = ref _elements[minChildIndex];
                priorities[index] = minChildPriority;
                elements[index] = elements[minChildIndex];
                _index[childElement] = index;
                index = minChildIndex;
            }

            _priorities[index] = priority;
            _elements[index] = element;
            _index[element] = index;
        }

        private void Resize(ref TPriority[] priorities, ref TElement[] elements)
        {
            Debug.Assert(priorities.Length == elements.Length);

            int newSize = priorities.Length == 0 ? DefaultCapacity : 2 * priorities.Length;

            Array.Resize(ref priorities, newSize);
            Array.Resize(ref elements, newSize);
        }

#if DEBUG
        public void ValidateInternalState()
        {
            if (_elements.Length < _count)
            {
                throw new Exception("invalid elements array length");
            }

            if (_priorities.Length < _count)
            {
                throw new Exception("invalid priorities array length");
            }

            if (_index.Count != _count)
            {
                throw new Exception("Invalid heap index count");
            }

            foreach ((var element, var idx) in _elements.Select((x, i) => (x, i)).Skip(_count))
            {
                if (!IsDefault(element))
                {
                    throw new Exception($"Non-zero element '{element}' at index {idx}.");
                }
            }

            foreach ((var priority, var idx) in _priorities.Select((x, i) => (x, i)).Skip(_count))
            {
                if (!IsDefault(priority))
                {
                    throw new Exception($"Non-zero priority '{priority}' at index {idx}.");
                }
            }

            foreach (var kvp in _index)
            {
                if (!_index.Comparer.Equals(_elements[kvp.Value], kvp.Key))
                {
                    throw new Exception($"Element '{kvp.Key}' maps to invalid heap location {kvp.Value} which contains '{_elements[kvp.Value]}'");
                }
            }

            static bool IsDefault<T>(T value)
            {
                T defaultVal = default;

                if (defaultVal is null)
                {
                    return value is null;
                }

                return value!.Equals(defaultVal);
            }
        }
#endif
        #endregion
    }
}