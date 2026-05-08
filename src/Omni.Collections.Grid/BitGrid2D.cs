using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Grid2D;

/// <summary>
/// A memory-efficient 2D grid that stores boolean values using bit-packed storage to minimize memory usage.
/// Provides O(1) get/set operations with memory usage reduced by a factor of 8 compared to boolean arrays.
/// Perfect for collision maps in games, maze generation, binary image processing, visibility systems,
/// or any scenario where large boolean grids must be stored with minimal memory overhead.
/// </summary>
public class BitGrid2D : IDisposable
{
    private readonly ulong[] _bits;
    private readonly int _width;
    private readonly int _height;
    private readonly int _totalBits;
    private readonly ArrayPool<ulong>? _arrayPool;
    private readonly bool _usePooling;
    public int Width => _width;
    public int Height => _height;
    public int Count => _width * _height;
    public BitGrid2D(int width, int height) : this(width, height, arrayPool: null)
    {
    }

    public static BitGrid2D CreateWithArrayPool(int width, int height)
    {
        return new BitGrid2D(width, height, ArrayPool<ulong>.Shared);
    }

    private BitGrid2D(int width, int height, ArrayPool<ulong>? arrayPool)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _width = width;
        _height = height;
        _totalBits = width * height;
        _arrayPool = arrayPool;
        _usePooling = arrayPool != null;
        int ulongCount = (_totalBits + 63) / 64;
        if (_usePooling)
        {
            _bits = _arrayPool!.Rent(ulongCount);
            Array.Clear(_bits, 0, ulongCount);
        }
        else
        {
            _bits = new ulong[ulongCount];
        }
    }

    public bool this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                throw new IndexOutOfRangeException();
            int bitIndex = y * _width + x;
            int ulongIndex = bitIndex / 64;
            int bitOffset = bitIndex % 64;
            return (_bits[ulongIndex] & (1UL << bitOffset)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                throw new IndexOutOfRangeException();
            int bitIndex = y * _width + x;
            int ulongIndex = bitIndex / 64;
            int bitOffset = bitIndex % 64;
            if (value)
            {
                _bits[ulongIndex] |= (1UL << bitOffset);
            }
            else
            {
                _bits[ulongIndex] &= ~(1UL << bitOffset);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(int x, int y)
    {
        return (uint)x < (uint)_width && (uint)y < (uint)_height;
    }

    public void SwapElements(int x1, int y1, int x2, int y2)
    {
        (this[x1, y1], this[x2, y2]) = (this[x2, y2], this[x1, y1]);
    }

    public IEnumerable<bool> GetNeighbors(int x, int y, bool includeDiagonals = false)
    {
        (int, int)[] directions = includeDiagonals
            ? [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]
            : [(-1, 0), (1, 0), (0, -1), (0, 1)];
        foreach (var (dx, dy) in directions)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (IsInBounds(nx, ny))
            {
                yield return this[nx, ny];
            }
        }
    }

    public IEnumerable<bool> GetRow(int y)
    {
        if ((uint)y >= (uint)_height)
            throw new ArgumentOutOfRangeException(nameof(y));
        for (int x = 0; x < _width; x++)
        {
            yield return this[x, y];
        }
    }

    public IEnumerable<bool> GetColumn(int x)
    {
        if ((uint)x >= (uint)_width)
            throw new ArgumentOutOfRangeException(nameof(x));
        for (int y = 0; y < _height; y++)
        {
            yield return this[x, y];
        }
    }

    public void FillArea(int x, int y, int width, int height, bool value)
    {
        if (width <= 0 || height <= 0) return;
        // Clip the rectangle to grid bounds (matches the previous IsInBounds-skip semantics).
        int x1 = Math.Max(0, x);
        int y1 = Math.Max(0, y);
        int x2 = Math.Min(_width, x + width);
        int y2 = Math.Min(_height, y + height);
        if (x1 >= x2 || y1 >= y2) return;
        // For each row in the rectangle, fill a contiguous bit range using
        // word-level ops. Per-row cost is O((x2 - x1) / 64); total is
        // O(height · width / 64) instead of O(width · height) per-cell.
        int rowSpan = x2 - x1;
        for (int row = y1; row < y2; row++)
        {
            int startBit = row * _width + x1;
            SetBitRange(startBit, startBit + rowSpan - 1, value);
        }
    }

    private void SetBitRange(int startBit, int endBit, bool value)
    {
        int firstWord = startBit / 64;
        int lastWord = endBit / 64;
        int firstOffset = startBit % 64;
        int lastOffset = endBit % 64;
        // Build a mask covering bits [firstOffset..lastOffset] in a single word.
        // upperMask = bits [0..lastOffset];  lowerMask = bits [0..firstOffset-1];
        // mask = upperMask & ~lowerMask = bits [firstOffset..lastOffset].
        if (firstWord == lastWord)
        {
            ulong upperMask = lastOffset == 63 ? ulong.MaxValue : (1UL << (lastOffset + 1)) - 1;
            ulong lowerMask = (1UL << firstOffset) - 1;
            ulong mask = upperMask & ~lowerMask;
            if (value) _bits[firstWord] |= mask;
            else _bits[firstWord] &= ~mask;
            return;
        }
        // First word: bits [firstOffset..63]
        ulong firstWordMask = ~((1UL << firstOffset) - 1);
        if (value) _bits[firstWord] |= firstWordMask;
        else _bits[firstWord] &= ~firstWordMask;
        // Middle words: full ulong fill
        ulong fillVal = value ? ulong.MaxValue : 0UL;
        for (int i = firstWord + 1; i < lastWord; i++)
        {
            _bits[i] = fillVal;
        }
        // Last word: bits [0..lastOffset]
        ulong lastWordMask = lastOffset == 63 ? ulong.MaxValue : (1UL << (lastOffset + 1)) - 1;
        if (value) _bits[lastWord] |= lastWordMask;
        else _bits[lastWord] &= ~lastWordMask;
    }

    public void ClearArea(int x, int y, int width, int height)
    {
        FillArea(x, y, width, height, false);
    }

    public void SetAll(bool value)
    {
        ulong fillValue = value ? ulong.MaxValue : 0UL;
        for (int i = 0; i < _bits.Length; i++)
        {
            _bits[i] = fillValue;
        }
        if (value && _totalBits % 64 != 0)
        {
            int lastUlongIndex = _bits.Length - 1;
            int validBits = _totalBits % 64;
            ulong mask = (1UL << validBits) - 1;
            _bits[lastUlongIndex] &= mask;
        }
    }

    public int CountSetBits()
    {
        int count = 0;
        for (int i = 0; i < _bits.Length - 1; i++)
        {
            count += System.Numerics.BitOperations.PopCount(_bits[i]);
        }
        if (_bits.Length > 0)
        {
            int lastIndex = _bits.Length - 1;
            int validBits = _totalBits - (lastIndex * 64);
            validBits = Math.Min(validBits, 64);
            if (validBits == 64)
            {
                count += System.Numerics.BitOperations.PopCount(_bits[lastIndex]);
            }
            else
            {
                ulong mask = (1UL << validBits) - 1;
                count += System.Numerics.BitOperations.PopCount(_bits[lastIndex] & mask);
            }
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Toggle(int x, int y)
    {
        this[x, y] = !this[x, y];
    }

    public void And(BitGrid2D other)
    {
        if (other._width != _width || other._height != _height)
            throw new ArgumentException("Grid dimensions must match");
        for (int i = 0; i < _bits.Length; i++)
        {
            _bits[i] &= other._bits[i];
        }
    }

    public void Or(BitGrid2D other)
    {
        if (other._width != _width || other._height != _height)
            throw new ArgumentException("Grid dimensions must match");
        for (int i = 0; i < _bits.Length; i++)
        {
            _bits[i] |= other._bits[i];
        }
    }

    public void Xor(BitGrid2D other)
    {
        if (other._width != _width || other._height != _height)
            throw new ArgumentException("Grid dimensions must match");
        for (int i = 0; i < _bits.Length; i++)
        {
            _bits[i] ^= other._bits[i];
        }
    }

    public void CopyRowTo(int y, Span<bool> destination)
    {
        if ((uint)y >= (uint)_height)
            throw new ArgumentOutOfRangeException(nameof(y));
        if (destination.Length < _width)
            throw new ArgumentException("Destination span is too small to hold the row.", nameof(destination));
        for (int x = 0; x < _width; x++)
        {
            destination[x] = this[x, y];
        }
    }

    public bool[] GetRowCopy(int y)
    {
        if ((uint)y >= (uint)_height)
            throw new ArgumentOutOfRangeException(nameof(y));
        var rowData = new bool[_width];
        CopyRowTo(y, rowData);
        return rowData;
    }

    public IEnumerable<(int x, int y, bool value)> EnumerateAll()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                yield return (x, y, this[x, y]);
            }
        }
    }

    public IEnumerable<(int x, int y, bool value)> EnumerateArea(int x, int y, int width, int height)
    {
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (IsInBounds(px, py))
                {
                    yield return (px, py, this[px, py]);
                }
            }
        }
    }

    public IEnumerable<(int x, int y)> EnumerateSetBits()
    {
        // Walk the bit-packed storage word-by-word. For each non-zero word, use
        // TrailingZeroCount to jump straight to the next set bit; clear the bit
        // and repeat. Skips entire 64-bit words of zeros in one comparison.
        // Total work: O(set bits + W·H/64) instead of O(W·H) per-cell scan.
        int wordCount = _bits.Length;
        for (int wordIdx = 0; wordIdx < wordCount; wordIdx++)
        {
            ulong word = _bits[wordIdx];
            // Mask off any bits past _totalBits in the final word (those positions
            // don't correspond to real cells, even if they happened to be set by
            // a word-level write).
            if (wordIdx == wordCount - 1 && (_totalBits & 63) != 0)
            {
                ulong validMask = (1UL << (_totalBits & 63)) - 1;
                word &= validMask;
            }
            int wordBase = wordIdx * 64;
            while (word != 0)
            {
                int bitInWord = BitOperations.TrailingZeroCount(word);
                int bitIndex = wordBase + bitInWord;
                yield return (bitIndex % _width, bitIndex / _width);
                word &= word - 1; // clear lowest set bit
            }
        }
    }

    public int GetNeighborsNonAlloc(int x, int y, Span<bool> buffer, bool includeDiagonals = false)
    {
        int count = 0;
        (int, int)[] directions = includeDiagonals
            ? [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]
            : [(-1, 0), (1, 0), (0, -1), (0, 1)];
        foreach (var (dx, dy) in directions)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (IsInBounds(nx, ny) && count < buffer.Length)
            {
                buffer[count++] = this[nx, ny];
            }
        }
        return count;
    }

    public void ProcessNeighbors(int x, int y, Action<bool> processor, bool includeDiagonals = false)
    {
        var nx = x - 1;
        var ny = y;
        if (IsInBounds(nx, ny)) processor(this[nx, ny]);
        nx = x + 1; ny = y;
        if (IsInBounds(nx, ny)) processor(this[nx, ny]);
        nx = x; ny = y - 1;
        if (IsInBounds(nx, ny)) processor(this[nx, ny]);
        nx = x; ny = y + 1;
        if (IsInBounds(nx, ny)) processor(this[nx, ny]);
        if (includeDiagonals)
        {
            nx = x - 1; ny = y - 1;
            if (IsInBounds(nx, ny)) processor(this[nx, ny]);
            nx = x - 1; ny = y + 1;
            if (IsInBounds(nx, ny)) processor(this[nx, ny]);
            nx = x + 1; ny = y - 1;
            if (IsInBounds(nx, ny)) processor(this[nx, ny]);
            nx = x + 1; ny = y + 1;
            if (IsInBounds(nx, ny)) processor(this[nx, ny]);
        }
    }

    public void Dispose()
    {
        if (_usePooling && _arrayPool != null)
        {
            _arrayPool.Return(_bits, clearArray: true);
        }
    }
}