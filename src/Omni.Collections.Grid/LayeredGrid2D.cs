using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Grid2D;

/// <summary>
/// A multi-layered 2D grid structure that efficiently manages multiple z-layers of 2D data with contiguous memory layout.
/// Provides O(1) get/set operations with excellent cache locality and memory organization superior to 3D arrays.
/// Ideal for layered game maps (background/foreground), multi-channel image processing, tile-based games,
/// or any 2.5D data structure requiring efficient access across multiple layers.
/// </summary>
public class LayeredGrid2D<T> : IDisposable
{
    private readonly T[] _data;
    private readonly int _width;
    private readonly int _height;
    private readonly int _layerCount;
    private readonly int _layerSize;
    private readonly ArrayPool<T>? _arrayPool;
    private readonly bool _usePooling;
    public int Width => _width;
    public int Height => _height;
    public int Count => _width * _height;
    public int LayerCount => _layerCount;
    public LayeredGrid2D(int width, int height, int layerCount) : this(width, height, layerCount, arrayPool: null)
    {
    }

    public static LayeredGrid2D<T> CreateWithArrayPool(int width, int height, int layerCount)
    {
        return new LayeredGrid2D<T>(width, height, layerCount, ArrayPool<T>.Shared);
    }

    private LayeredGrid2D(int width, int height, int layerCount, ArrayPool<T>? arrayPool)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (layerCount <= 0) throw new ArgumentOutOfRangeException(nameof(layerCount));
        _width = width;
        _height = height;
        _layerCount = layerCount;
        _layerSize = width * height;
        _arrayPool = arrayPool;
        _usePooling = arrayPool != null;
        int totalSize = _layerSize * layerCount;
        if (_usePooling)
        {
            _data = _arrayPool!.Rent(totalSize);
            Array.Clear(_data, 0, totalSize);
        }
        else
        {
            _data = new T[totalSize];
        }
    }

    public T this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[0, x, y];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this[0, x, y] = value;
    }

    public T this[int layer, int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)layer >= (uint)_layerCount)
                throw new ArgumentOutOfRangeException(nameof(layer));
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                throw new IndexOutOfRangeException();
            int index = layer * _layerSize + y * _width + x;
            return _data[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)layer >= (uint)_layerCount)
                throw new ArgumentOutOfRangeException(nameof(layer));
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                throw new IndexOutOfRangeException();
            int index = layer * _layerSize + y * _width + x;
            _data[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(int x, int y)
    {
        return (uint)x < (uint)_width && (uint)y < (uint)_height;
    }

    public void SwapElements(int x1, int y1, int x2, int y2)
    {
        SwapElementsInLayer(0, x1, y1, x2, y2);
    }

    public void SwapElementsInLayer(int layer, int x1, int y1, int x2, int y2)
    {
        (this[layer, x1, y1], this[layer, x2, y2]) = (this[layer, x2, y2], this[layer, x1, y1]);
    }

    public void SwapElementsAcrossLayers(int layer1, int x1, int y1, int layer2, int x2, int y2)
    {
        (this[layer1, x1, y1], this[layer2, x2, y2]) = (this[layer2, x2, y2], this[layer1, x1, y1]);
    }

    public IEnumerable<T> GetNeighbors(int x, int y, bool includeDiagonals = false)
    {
        return GetNeighborsInLayer(0, x, y, includeDiagonals);
    }

    public int GetNeighborsNonAlloc(int x, int y, Span<T> buffer, bool includeDiagonals = false)
    {
        return GetNeighborsInLayerNonAlloc(0, x, y, buffer, includeDiagonals);
    }

    public void ProcessNeighbors(int x, int y, Action<T> processor, bool includeDiagonals = false)
    {
        ProcessNeighborsInLayer(0, x, y, processor, includeDiagonals);
    }

    private IEnumerable<T> GetNeighborsInLayer(int layer, int x, int y, bool includeDiagonals = false)
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
                yield return this[layer, nx, ny];
            }
        }
    }

    public IEnumerable<T> GetNeighborsAllLayers(int x, int y, bool includeDiagonals = false)
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
                for (int layer = 0; layer < _layerCount; layer++)
                {
                    yield return this[layer, nx, ny];
                }
            }
        }
    }

    public IEnumerable<T> GetRow(int y)
    {
        return GetLayerRow(0, y);
    }

    public IEnumerable<T> GetLayerRow(int layer, int y)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        if ((uint)y >= (uint)_height)
            throw new ArgumentOutOfRangeException(nameof(y));
        for (int x = 0; x < _width; x++)
        {
            yield return this[layer, x, y];
        }
    }

    public IEnumerable<T> GetColumn(int x)
    {
        return GetLayerColumn(0, x);
    }

    private IEnumerable<T> GetLayerColumn(int layer, int x)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        if ((uint)x >= (uint)_width)
            throw new ArgumentOutOfRangeException(nameof(x));
        for (int y = 0; y < _height; y++)
        {
            yield return this[layer, x, y];
        }
    }

    public void FillArea(int x, int y, int width, int height, T value)
    {
        FillLayerArea(0, x, y, width, height, value);
    }

    public void FillLayerArea(int layer, int x, int y, int width, int height, T value)
    {
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (IsInBounds(px, py))
                {
                    this[layer, px, py] = value;
                }
            }
        }
    }

    private void FillAllLayersArea(int x, int y, int width, int height, T value)
    {
        for (int layer = 0; layer < _layerCount; layer++)
        {
            FillLayerArea(layer, x, y, width, height, value);
        }
    }

    public void ClearArea(int x, int y, int width, int height)
    {
        FillArea(x, y, width, height, default!);
    }

    public void ClearAllLayersArea(int x, int y, int width, int height)
    {
        FillAllLayersArea(x, y, width, height, default!);
    }

    public ReadOnlySpan<T> GetRowSpan(int y)
    {
        return GetLayerRowSpan(0, y);
    }

    public ReadOnlySpan<T> GetLayerRowSpan(int layer, int y)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        if ((uint)y >= (uint)_height)
            throw new ArgumentOutOfRangeException(nameof(y));
        int startIndex = layer * _layerSize + y * _width;
        return _data.AsSpan(startIndex, _width);
    }

    private ReadOnlySpan<T> GetLayerSpan(int layer)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        int startIndex = layer * _layerSize;
        return _data.AsSpan(startIndex, _layerSize);
    }

    private Span<T> GetLayerSpanMutable(int layer)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        int startIndex = layer * _layerSize;
        return _data.AsSpan(startIndex, _layerSize);
    }

    public IEnumerable<(int x, int y, T value)> EnumerateAll()
    {
        return EnumerateLayer(0);
    }

    private IEnumerable<(int x, int y, T value)> EnumerateLayer(int layer)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                yield return (x, y, this[layer, x, y]);
            }
        }
    }

    public IEnumerable<(int x, int y, int layer, T value)> EnumerateAllLayers()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int layer = 0; layer < _layerCount; layer++)
                {
                    yield return (x, y, layer, this[layer, x, y]);
                }
            }
        }
    }

    public IEnumerable<(int x, int y, T value)> EnumerateArea(int x, int y, int width, int height)
    {
        return EnumerateLayerArea(0, x, y, width, height);
    }

    private IEnumerable<(int x, int y, T value)> EnumerateLayerArea(int layer, int x, int y, int width, int height)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (IsInBounds(px, py))
                {
                    yield return (px, py, this[layer, px, py]);
                }
            }
        }
    }

    public void CopyLayer(int sourceLayer, int destinationLayer)
    {
        if ((uint)sourceLayer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(sourceLayer));
        if ((uint)destinationLayer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(destinationLayer));
        if (sourceLayer == destinationLayer) return;
        ReadOnlySpan<T> sourceSpan = GetLayerSpan(sourceLayer);
        Span<T> destSpan = GetLayerSpanMutable(destinationLayer);
        sourceSpan.CopyTo(destSpan);
    }

    public void ClearLayer(int layer)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        Span<T> layerSpan = GetLayerSpanMutable(layer);
        layerSpan.Clear();
    }

    public void FillLayer(int layer, T value)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        Span<T> layerSpan = GetLayerSpanMutable(layer);
        layerSpan.Fill(value);
    }

    private int GetNeighborsInLayerNonAlloc(int layer, int x, int y, Span<T> buffer, bool includeDiagonals = false)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
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
                buffer[count++] = this[layer, nx, ny];
            }
        }
        return count;
    }

    public void ProcessNeighborsInLayer(int layer, int x, int y, Action<T> processor, bool includeDiagonals = false)
    {
        if ((uint)layer >= (uint)_layerCount)
            throw new ArgumentOutOfRangeException(nameof(layer));
        var nx = x - 1;
        var ny = y;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        nx = x + 1; ny = y;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        nx = x; ny = y - 1;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        nx = x; ny = y + 1;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        if (!includeDiagonals) {
            return;
        }
        nx = x - 1; ny = y - 1;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        nx = x - 1; ny = y + 1;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        nx = x + 1; ny = y - 1;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
        nx = x + 1; ny = y + 1;
        if (IsInBounds(nx, ny)) processor(this[layer, nx, ny]);
    }

    public void Dispose()
    {
        if (_usePooling && _arrayPool != null)
        {
            _arrayPool.Return(_data, clearArray: true);
        }
    }
}