using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace YtToVkReposter
{
    public class LimitedStack<T> : IEnumerable<T>, ICollection<T> where T:class
    {
        public int Limit { get; }
        private LinkedList<T> _items;
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly { get; }

        public LimitedStack(int limit)
        {
            if(limit <= 0)
                throw new Exception("Invalid initialization: Limit must be more zero");
            Limit = limit;
            _items = new LinkedList<T>();
        }
        public void Push(T obj)
        {
            if (_items.Count == Limit)
            {
                _items.RemoveLast();
                _items.AddFirst(obj);
            }
            else
            {
                _items.AddFirst(obj);
            }
        }
        public void PushRange(IEnumerable<T> obj)
        {
            foreach (var item in obj)
            {
                if (_items.Count == Limit)
                {
                    _items.RemoveLast();
                    _items.AddFirst(item);
                }
                else
                {
                    _items.AddFirst(item);
                }
            }
        }

        public T Pop()
        {
            if (Count > 0)
            {
                var obj = _items.First.Value;
                _items.RemoveFirst(); 
                return obj;
            }
            return null;
        }

        public void Add(T item)
        {
            Push(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(T obj) => _items.Contains(obj);
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            var t = new List<T>();
            foreach (var item in _items)
            {
                t.Add(item);
            }
            return t.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}