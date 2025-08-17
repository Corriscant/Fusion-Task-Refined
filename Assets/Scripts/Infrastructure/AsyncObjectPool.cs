using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Generic asynchronous object pool used for reusing Unity objects.
    /// </summary>
    /// <typeparam name="T">Type of object to pool.</typeparam>
    public class AsyncObjectPool<T> where T : Component
    {
        private readonly Queue<T> _objects = new Queue<T>();
        private readonly Func<Task<T>> _factory;

        /// <summary>
        /// Creates a new pool using the provided factory function.
        /// </summary>
        /// <param name="factory">Factory used to create new instances when the pool is empty.</param>
        public AsyncObjectPool(Func<Task<T>> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Retrieves an object from the pool or creates a new one if necessary.
        /// </summary>
        public async Task<T> Get()
        {
            if (_objects.Count > 0)
            {
                var instance = _objects.Dequeue();
                if (instance != null)
                {
                    instance.gameObject.SetActive(true);
                    return instance;
                }
            }

            var created = await _factory();
            created.gameObject.SetActive(true);
            return created;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public void Release(T obj)
        {
            if (obj == null)
            {
                return;
            }

            obj.gameObject.SetActive(false);
            _objects.Enqueue(obj);
        }

        /// <summary>
        /// Pre-creates the specified number of objects and immediately releases them back to the pool.
        /// </summary>
        public async Task Warmup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = await Get();
                Release(instance);
            }
        }
    }
}

