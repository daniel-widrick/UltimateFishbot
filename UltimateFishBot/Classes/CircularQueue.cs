using System;
using System.Collections;
using System.Collections.Generic;

namespace UltimateFishBot.Classes
{
    public class CircularQueue<T> : ICollection, IReadOnlyCollection<T>
    {
        private readonly int _maxSize;
        private readonly Queue<T> _queue;

        public CircularQueue(int maxSize)
        {
            _maxSize = maxSize;
            _queue = new Queue<T>(maxSize);
        }

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public void Enqueue(T item)
        {
            if (_queue.Count == _maxSize)
            {
                _queue.Dequeue();
            }

            _queue.Enqueue(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_queue).CopyTo(array, index);
        }

        int ICollection.Count => _queue.Count;
        public object SyncRoot => ((ICollection) _queue).SyncRoot;
        public bool IsSynchronized => ((ICollection) _queue).IsSynchronized;

        int IReadOnlyCollection<T>.Count => ((IReadOnlyCollection<T>) _queue).Count;
    }
}