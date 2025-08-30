using System.Runtime.CompilerServices;

namespace Omni.Collections.Core.Node
{
    public sealed class UniversalDictionaryNode<TKey, TValue>
        where TKey : notnull
    {
        public TKey Key { get; set; } = default!;
        public TValue Value { get; set; } = default!;
        public UniversalDictionaryNode<TKey, TValue>? Link0;
        public UniversalDictionaryNode<TKey, TValue>? Link1;
        public UniversalDictionaryNode<TKey, TValue>? Link2;
        public UniversalDictionaryNode<TKey, TValue>? Link3;
        public long Metadata0;
        public long Metadata1;
        public UniversalDictionaryNode<TKey, TValue>? Next
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Link0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Link0 = value;
        }

        public UniversalDictionaryNode<TKey, TValue>? NextOrdering
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Link1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Link1 = value;
        }

        public UniversalDictionaryNode<TKey, TValue>? PrevOrdering
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Link2;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Link2 = value;
        }

        public int HashCode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)Metadata0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Metadata0 = value;
        }

        public long AccessData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Metadata1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Metadata1 = value;
        }

        public UniversalDictionaryNode()
        {
        }

        public UniversalDictionaryNode(TKey key, TValue value, int hashCode)
        {
            Key = key;
            Value = value;
            HashCode = hashCode;
        }
    }
}