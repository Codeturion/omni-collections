using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Core.Node;

/// <summary>
/// Aliasing helpers for <see cref="UniversalDictionaryNode{TKey, TValue}"/>'s reusable Link/Metadata slots.
/// The node carries Link0..Link3 plus Metadata0/Metadata1; consumers reinterpret the same slots per role.
/// LRU role (ConcurrentLinkedDictionary): Link1=NextLru, Link2=PrevLru, Metadata1=access timestamp (AccessTime).
/// LFU role (CounterDictionary): Link1=NextInFrequency, Link2=PrevInFrequency, Metadata1=access count (AccessCount).
/// Link0 is always the hash-bucket chain (Next), Metadata0 is always the cached HashCode.
/// Calling the wrong getter for a node's role silently corrupts state — use the AssertXxxRole guards in DEBUG.
/// </summary>
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

    /// <summary>
    /// Debug-only sanity check for callers that access LRU-role aliases (NextLru/PrevLru/AccessTime).
    /// Verifies the node is non-null. Pure documentation aid — does not validate the semantic role
    /// because Link1/Link2/Metadata1 are physically indistinguishable across roles.
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertLruRole<TKey, TValue>(UniversalDictionaryNode<TKey, TValue>? node)
        where TKey : notnull
    {
        Debug.Assert(node != null, "AssertLruRole: node is null. LRU helpers (GetNextLru/GetPrevLru/GetAccessTime) require a non-null node.");
    }

    /// <summary>
    /// Debug-only sanity check for callers that access LFU-role aliases (NextInFrequency/PrevInFrequency/AccessCount).
    /// Verifies the node is non-null and AccessData (AccessCount) is non-negative. Pure documentation aid —
    /// cannot validate that Link1/Link2 are LFU pointers and not LRU pointers (they share storage).
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertLfuRole<TKey, TValue>(UniversalDictionaryNode<TKey, TValue>? node)
        where TKey : notnull
    {
        Debug.Assert(node != null, "AssertLfuRole: node is null. LFU helpers (GetNextInFrequency/GetPrevInFrequency/GetAccessCount) require a non-null node.");
        Debug.Assert(node!.AccessData >= 0, "AssertLfuRole: AccessCount must be non-negative for LFU role.");
    }

    /// <summary>
    /// Debug-only sanity check for callers that access linked-ordering aliases (NextOrdering/PrevOrdering on the node).
    /// Verifies the node is non-null. Pure documentation aid — Link1/Link2 are reused across roles.
    /// </summary>
    [Conditional("DEBUG")]
    public static void AssertLinkedRole<TKey, TValue>(UniversalDictionaryNode<TKey, TValue>? node)
        where TKey : notnull
    {
        Debug.Assert(node != null, "AssertLinkedRole: node is null. Linked-ordering accessors require a non-null node.");
    }
}
