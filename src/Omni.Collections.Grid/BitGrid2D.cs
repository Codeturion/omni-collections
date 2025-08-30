using System;
using System.Buffers;
using System.Collections.Generic;
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
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (IsInBounds(px, py))
                {
                    this[px, py] = value;
                }
            }
        }
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

    public ReadOnlySpan<bool> GetRowSpan(int y)
    {
        if ((uint)y >= (uint)_height)
            throw new ArgumentOutOfRangeException(nameof(y));
        var rowData = new bool[_width];
        for (int x = 0; x < _width; x++)
        {
            rowData[x] = this[x, y];
        }
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
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (this[x, y])
                {
                    yield return (x, y);
                }
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