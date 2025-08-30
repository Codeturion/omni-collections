using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Omni.Collections.Reactive
{
    public interface IEventArgsPool
    {
        PropertyChangedEventArgs RentPropertyChangedEventArgs(string propertyName);

        void ReturnPropertyChangedEventArgs(PropertyChangedEventArgs eventArgs);
        NotifyCollectionChangedEventArgs RentCollectionChangedEventArgs(NotifyCollectionChangedAction action);
        void ReturnCollectionChangedEventArgs(NotifyCollectionChangedEventArgs eventArgs);
    }

    public class OmniEventArgsPool : IEventArgsPool
    {
        public static readonly OmniEventArgsPool Shared = new OmniEventArgsPool();
        private readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _propertyEventCache = new ConcurrentDictionary<string, PropertyChangedEventArgs>();
        private readonly ConcurrentQueue<NotifyCollectionChangedEventArgs> _collectionEventPool = new ConcurrentQueue<NotifyCollectionChangedEventArgs>();
        public PropertyChangedEventArgs RentPropertyChangedEventArgs(string propertyName)
        {
            return _propertyEventCache.GetOrAdd(propertyName, static name => new PropertyChangedEventArgs(name));
        }

        public void ReturnPropertyChangedEventArgs(PropertyChangedEventArgs eventArgs)
        {
        }

        public NotifyCollectionChangedEventArgs RentCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            if (action == NotifyCollectionChangedAction.Reset)
            {
                return _collectionEventPool.TryDequeue(out var eventArgs)
                    ? eventArgs
                    : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            }
            else
            {
                return new NotifyCollectionChangedEventArgs(action);
            }
        }

        public void ReturnCollectionChangedEventArgs(NotifyCollectionChangedEventArgs eventArgs)
        {
            if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
            {
                _collectionEventPool.Enqueue(eventArgs);
            }
        }
    }
}