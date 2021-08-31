using System.Collections;
using System.Collections.Generic;

namespace Smidge.Core
{
    internal class OrderedSet<T> : ICollection<T>
    {
        private readonly IDictionary<T, LinkedListNode<T>> _dictionary;
        private readonly LinkedList<T> _linkedList;

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            _linkedList = new LinkedList<T>();
        }

        public int Count => _dictionary.Count;

        public virtual bool IsReadOnly => false;

        void ICollection<T>.Add(T item) => Add(item);

        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item))
                return false;


            var node = _linkedList.AddLast(item);
            _dictionary.Add(item, node);
            return true;
        }

        public void Clear()
        {
            _linkedList.Clear();
            _dictionary.Clear();
        }

        public bool Remove(T item)
        {
            if (item == null)
                return false;

            if (!_dictionary.TryGetValue(item, out var node))
                return false;


            _dictionary.Remove(item);
            _linkedList.Remove(node);
            return true;
        }

        public IEnumerator<T> GetEnumerator() => _linkedList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item)
        {
            if (item == null)
                return false;


            return _dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex) => _linkedList.CopyTo(array, arrayIndex);
    }
}