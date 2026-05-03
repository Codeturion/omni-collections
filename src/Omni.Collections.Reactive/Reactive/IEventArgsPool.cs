using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Omni.Collections.Reactive
{
    /// <summary>
    /// Pool/cache abstraction for the EventArgs types used by INotifyPropertyChanged and
    /// INotifyCollectionChanged. The intent is to avoid per-notification heap allocations
    /// when subscribers are present on hot paths.
    ///
    /// Design constraint:
    /// <para>
    /// <see cref="NotifyCollectionChangedEventArgs"/> is sealed in the BCL and exposes its
    /// state (Action, NewItems, OldItems, NewStartingIndex, OldStartingIndex) only through
    /// constructor-time initialization. There is no public mutable surface, and reflection
    /// on the internal fields is brittle and slow. Subclassing is permitted, but a subclass
    /// still has to delegate to the sealed base constructors which capture the values once.
    /// </para>
    /// <para>
    /// Consequences for pooling:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Args whose contents are CONSTANT across calls (e.g., a Reset, or a property name
    /// that is fixed) can be safely cached and shared between invocations. We do that.
    /// </item>
    /// <item>
    /// Args whose contents VARY per call (Add(item, index), Remove(items, startIndex), etc.)
    /// cannot be safely reused: a fresh instance has to be constructed for every call,
    /// because the state is immutable post-construction. The Rent methods for those shapes
    /// allocate fresh args and return them. The matching Return is a no-op. The methods
    /// exist as call-site abstractions so future improvements (e.g., a custom subclass with
    /// a back-channel mutability contract) can plug in without touching consumers.
    /// </item>
    /// </list>
    /// </summary>
    public interface IEventArgsPool
    {
        PropertyChangedEventArgs RentPropertyChangedEventArgs(string propertyName);
        void ReturnPropertyChangedEventArgs(PropertyChangedEventArgs eventArgs);

        NotifyCollectionChangedEventArgs RentCollectionChangedEventArgs(NotifyCollectionChangedAction action);
        void ReturnCollectionChangedEventArgs(NotifyCollectionChangedEventArgs eventArgs);

        NotifyCollectionChangedEventArgs RentAdd(object? item, int index);
        NotifyCollectionChangedEventArgs RentRemove(object? item, int index);
        NotifyCollectionChangedEventArgs RentReplace(object? newItem, object? oldItem, int index);

        NotifyCollectionChangedEventArgs RentAddRange(IList items, int startIndex);
        NotifyCollectionChangedEventArgs RentRemoveRange(IList items, int startIndex);

        NotifyCollectionChangedEventArgs RentAdd(object? item);
        NotifyCollectionChangedEventArgs RentRemove(object? item);

        NotifyCollectionChangedEventArgs RentAddRange(IList items);
        NotifyCollectionChangedEventArgs RentRemoveRange(IList items);
    }

    /// <summary>
    /// Default <see cref="IEventArgsPool"/> implementation.
    ///
    /// Pools (caches) only what is safely poolable:
    /// <list type="bullet">
    /// <item><see cref="PropertyChangedEventArgs"/> by property name — keyed cache, args are immutable and reusable.</item>
    /// <item>The single shared "Reset" <see cref="NotifyCollectionChangedEventArgs"/> instance — also immutable and reusable.</item>
    /// </list>
    ///
    /// All other Rent* methods construct new args. See <see cref="IEventArgsPool"/> docs for why
    /// per-call NotifyCollectionChangedEventArgs cannot be reused safely.
    /// </summary>
    public class OmniEventArgsPool : IEventArgsPool
    {
        public static readonly OmniEventArgsPool Shared = new OmniEventArgsPool();
        private readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _propertyEventCache = new ConcurrentDictionary<string, PropertyChangedEventArgs>();
        private static readonly NotifyCollectionChangedEventArgs ResetArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

        public PropertyChangedEventArgs RentPropertyChangedEventArgs(string propertyName)
        {
            return _propertyEventCache.GetOrAdd(propertyName, static name => new PropertyChangedEventArgs(name));
        }

        public void ReturnPropertyChangedEventArgs(PropertyChangedEventArgs eventArgs)
        {
            // PropertyChangedEventArgs is cached by name and shared; nothing to return.
        }

        public NotifyCollectionChangedEventArgs RentCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            // Only Reset is shareable (no item state). Other actions require concrete items
            // we don't have here, so callers should use the typed Rent overloads instead.
            if (action == NotifyCollectionChangedAction.Reset)
            {
                return ResetArgs;
            }
            return new NotifyCollectionChangedEventArgs(action);
        }

        public void ReturnCollectionChangedEventArgs(NotifyCollectionChangedEventArgs eventArgs)
        {
            // ResetArgs is a static singleton; per-call args are not poolable (see interface docs).
        }

        // The Rent overloads below allocate. They exist so call sites in ObservableList /
        // ObservableHashSet route notifications through the pool abstraction and any future
        // pooling improvement can be made centrally.

        public NotifyCollectionChangedEventArgs RentAdd(object? item, int index)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);

        public NotifyCollectionChangedEventArgs RentRemove(object? item, int index)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);

        public NotifyCollectionChangedEventArgs RentReplace(object? newItem, object? oldItem, int index)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index);

        public NotifyCollectionChangedEventArgs RentAddRange(IList items, int startIndex)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, startIndex);

        public NotifyCollectionChangedEventArgs RentRemoveRange(IList items, int startIndex)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, startIndex);

        public NotifyCollectionChangedEventArgs RentAdd(object? item)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item);

        public NotifyCollectionChangedEventArgs RentRemove(object? item)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item);

        public NotifyCollectionChangedEventArgs RentAddRange(IList items)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items);

        public NotifyCollectionChangedEventArgs RentRemoveRange(IList items)
            => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items);
    }
}
