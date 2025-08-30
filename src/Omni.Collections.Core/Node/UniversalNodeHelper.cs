using System.Runtime.CompilerServices;

namespace Omni.Collections.Core.Node;

public static class UniversalNodeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetAccessCount<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node)
        where TKey : notnull => node.AccessData;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAccessCount<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node, long count)
        where TKey : notnull => node.AccessData = count;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UniversalDictionaryNode<TKey, TValue>? GetNextInFrequency<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node)
        where TKey : notnull => node.Link1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetNextInFrequency<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node, UniversalDictionaryNode<TKey, TValue>? next)
        where TKey : notnull => node.Link1 = next;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UniversalDictionaryNode<TKey, TValue>? GetPrevInFrequency<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node)
        where TKey : notnull => node.Link2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPrevInFrequency<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node, UniversalDictionaryNode<TKey, TValue>? prev)
        where TKey : notnull => node.Link2 = prev;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetAccessTime<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node)
        where TKey : notnull => node.AccessData;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAccessTime<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node, long timestamp)
        where TKey : notnull => node.AccessData = timestamp;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UniversalDictionaryNode<TKey, TValue>? GetNextLru<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node)
        where TKey : notnull => node.Link1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetNextLru<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node, UniversalDictionaryNode<TKey, TValue>? next)
        where TKey : notnull => node.Link1 = next;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UniversalDictionaryNode<TKey, TValue>? GetPrevLru<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node)
        where TKey : notnull => node.Link2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPrevLru<TKey, TValue>(UniversalDictionaryNode<TKey, TValue> node, UniversalDictionaryNode<TKey, TValue>? prev)
        where TKey : notnull => node.Link2 = prev;
}