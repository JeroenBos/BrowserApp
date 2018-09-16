using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JBSnorro.View
{
    public class ProcessingQueue<T>
    {
        private readonly object _lock = new object();
        private readonly Queue<T> queue = new Queue<T>();
        private readonly List<T> processing = new List<T>();
        private readonly IEqualityComparer<T> equalityComparer;
        private readonly Action start;
        private int count;
        public int Count => count;
        public IReadOnlyCollection<T> CurrentlyProcessingTasks => processing;

        public ProcessingQueue(Action callback, IEqualityComparer<T> equalityComparer = null)
        {
            Contract.Requires(callback != null);

            this.start = callback;
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        public void Enqueue(T item)
        {
            bool isFirst;
            lock (_lock)
            {
                isFirst = this.Count == 0;
                this.queue.Enqueue(item);
                this.count++;
            }
            if (isFirst)
            {
                this.start();
            }
        }
        public bool TryDequeue(out T item)
        {
            lock (_lock)
            {
                if (queue.Count == 0)
                {
                    item = default;
                    return false;
                }
                item = queue.Dequeue();
                this.processing.Add(item);
                return true;
            }
        }
        public void OnProcessed(T item)
        {
            lock (_lock)
            {
                int index = this.processing.IndexOf(item, this.equalityComparer);
                if (index == -1)
                {
                    throw new InvalidOperationException("The specified item was not a currently running task");
                }
                count--;
            }
        }
    }
}