using System;
using System.Collections.Concurrent;

namespace Spider
{
    //http://msdn.microsoft.com/en-us/library/ff458671(v=vs.110).aspx
    /// <summary>
    /// Class ObjectPool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T>
    {
        /// <summary>
        /// The _object generator
        /// </summary>
        private readonly Func<T> _objectGenerator;
        /// <summary>
        /// The _objects
        /// </summary>
        private readonly ConcurrentBag<T> _objects;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="objectGenerator">The object generator.</param>
        /// <exception cref="ArgumentNullException">objectGenerator</exception>
        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <returns>T.</returns>
        public T GetObject()
        {
            T item;
            return _objects.TryTake(out item) ? item : _objectGenerator();
        }

        /// <summary>
        /// Puts the object.
        /// </summary>
        /// <param name="item">The item.</param>
        public void PutObject(T item)
        {
            _objects.Add(item);
        }
    }
}