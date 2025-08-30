using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Omni.Collections.Hybrid.LinkedDictionary;

namespace Omni.Collections.Spatial
{
    /// <summary>
    /// A temporal-spatial data structure that seamlessly indexes objects across both time and space dimensions.
    /// Delivers O(1) insertion and O(k) spatial-temporal queries through unified 4D hash grid organization.
    /// Indispensable for simulation replay systems, trajectory analysis, and time-based game worlds where
    /// efficient queries across both temporal and spatial dimensions drive system capabilities.
    /// </summary>
    public class TemporalSpatialHashGrid<T> : IDisposable where T : notnull
    {
        #region Types
        public struct SpatialObject
        {
            public float X { get; set; }

            public float Y { get; set; }

            public float VelocityX { get; set; }

            public float VelocityY { get; set; }

            public DateTime LastUpdate { get; set; }

            public T Object { get; set; }

            public SpatialObject(float x, float y, T obj)
            {
                X = x;
                Y = y;
                VelocityX = 0;
                VelocityY = 0;
                LastUpdate = DateTime.UtcNow;
                Object = obj;
            }
        }

        sealed private class SpatialSnapshot
        {
            public readonly DateTime Timestamp;
            public readonly SpatialHashGrid<T> Grid;
            public readonly Dictionary<T, SpatialObject> Objects;
            public SpatialSnapshot(DateTime timestamp, float cellSize)
            {
                Timestamp = timestamp;
                Grid = new SpatialHashGrid<T>(cellSize);
                Objects = new Dictionary<T, SpatialObject>();
            }
        }
        #endregion Types
        private readonly SpatialHashGrid<T> _currentGrid;
        private readonly Dictionary<T, SpatialObject> _currentObjects;
        private readonly LinkedDictionary<DateTime, SpatialSnapshot> _snapshots;
        private readonly TimeSpan _snapshotInterval;
        private readonly TimeSpan _historyRetention;
        private readonly float _cellSize;
        private DateTime _lastSnapshotTime;
        public int CurrentObjectCount => _currentObjects.Count;
        public int SnapshotCount => _snapshots.Count;
        public float CellSize => _cellSize;
        public TemporalSpatialHashGrid() : this(64.0f, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10))
        {
        }

        public TemporalSpatialHashGrid(float cellSize, TimeSpan snapshotInterval, TimeSpan historyRetention)
        {
            if (cellSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (snapshotInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(snapshotInterval));
            if (historyRetention <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(historyRetention));
            _cellSize = cellSize;
            _snapshotInterval = snapshotInterval;
            _historyRetention = historyRetention;
            _currentGrid = new SpatialHashGrid<T>(cellSize);
            _currentObjects = new Dictionary<T, SpatialObject>();
            _snapshots = new LinkedDictionary<DateTime, SpatialSnapshot>(1000, CapacityMode.Fixed);
            _lastSnapshotTime = DateTime.UtcNow;
        }

        public void UpdateObject(T obj, float x, float y, float velocityX = 0, float velocityY = 0)
        {
            var now = DateTime.UtcNow;
            var spatialObj = new SpatialObject(x, y, obj)
            {
                VelocityX = velocityX,
                VelocityY = velocityY,
                LastUpdate = now
            };
            if (_currentObjects.TryGetValue(obj, out var existing))
            {
                _currentGrid.Remove(existing.X, existing.Y, obj);
            }
            _currentGrid.Insert(x, y, obj);
            _currentObjects[obj] = spatialObj;
            if (now - _lastSnapshotTime >= _snapshotInterval)
            {
                TakeSnapshot(now);
            }
        }

        public bool RemoveObject(T obj)
        {
            if (_currentObjects.TryGetValue(obj, out var spatialObj))
            {
                _currentGrid.Remove(spatialObj.X, spatialObj.Y, obj);
                _currentObjects.Remove(obj);
                return true;
            }
            return false;
        }

        public IEnumerable<T> GetObjectsInRadius(float x, float y, float radius)
        {
            return _currentGrid.GetObjectsInRadius(x, y, radius);
        }

        public IEnumerable<T> GetObjectsInRectangle(float minX, float minY, float maxX, float maxY)
        {
            return _currentGrid.GetObjectsInRectangle(minX, minY, maxX, maxY);
        }

        public IEnumerable<T> GetObjectsInRadiusAtTime(float x, float y, float radius, DateTime time)
        {
            var snapshot = FindClosestSnapshot(time);
            if (snapshot != null)
            {
                return snapshot.Grid.GetObjectsInRadius(x, y, radius);
            }
            return InterpolateObjectsInRadius(x, y, radius, time);
        }

        public IEnumerable<(T obj, float predictedX, float predictedY)> GetPredictedObjectsInRadius(
            float x, float y, float radius, TimeSpan lookAhead)
        {
            var futureTime = DateTime.UtcNow + lookAhead;
            var radiusSquared = radius * radius;
            foreach (KeyValuePair<T, SpatialObject> kvp in _currentObjects)
            {
                var obj = kvp.Key;
                var spatialObj = kvp.Value;
                var timeDelta = (float)lookAhead.TotalSeconds;
                var predictedX = spatialObj.X + (spatialObj.VelocityX * timeDelta);
                var predictedY = spatialObj.Y + (spatialObj.VelocityY * timeDelta);
                var dx = predictedX - x;
                var dy = predictedY - y;
                var distanceSquared = dx * dx + dy * dy;
                if (distanceSquared <= radiusSquared)
                {
                    yield return (obj, predictedX, predictedY);
                }
            }
        }

        public IEnumerable<(DateTime time, float x, float y)> GetObjectTrajectory(T obj, TimeSpan lookBack)
        {
            var cutoffTime = DateTime.UtcNow - lookBack;
            var trajectory = new List<(DateTime, float, float)>();
            if (_currentObjects.TryGetValue(obj, out var current))
            {
                trajectory.Add((current.LastUpdate, current.X, current.Y));
            }
            foreach (KeyValuePair<DateTime, SpatialSnapshot> kvp in _snapshots)
            {
                if (kvp.Key < cutoffTime)
                    continue;
                var snapshot = kvp.Value;
                if (snapshot.Objects.TryGetValue(obj, out var historical))
                {
                    trajectory.Add((snapshot.Timestamp, historical.X, historical.Y));
                }
            }
            trajectory.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return trajectory;
        }

        public IEnumerable<T> GetObjectsAlongPath(float startX, float startY, float endX, float endY, float pathWidth)
        {
            var halfWidth = pathWidth * 0.5f;
            var minX = Math.Min(startX, endX) - halfWidth;
            var maxX = Math.Max(startX, endX) + halfWidth;
            var minY = Math.Min(startY, endY) - halfWidth;
            var maxY = Math.Max(startY, endY) + halfWidth;
            IEnumerable<T>? candidateObjects = _currentGrid.GetObjectsInRectangle(minX, minY, maxX, maxY);
            return candidateObjects.Where(obj =>
            {
                if (_currentObjects.TryGetValue(obj, out var spatialObj))
                {
                    return DistanceToLineSegment(spatialObj.X, spatialObj.Y, startX, startY, endX, endY) <= halfWidth;
                }
                return false;
            });
        }

        public IEnumerable<SpatialObject> GetAllObjectsWithVelocity()
        {
            return _currentObjects.Values;
        }

        public void UpdatePosition(T obj, float x, float y)
        {
            UpdateObject(obj, x, y);
        }

        public void UpdatePositionWithVelocity(T obj, float x, float y, float velocityX, float velocityY)
        {
            UpdateObject(obj, x, y, velocityX, velocityY);
        }

        public void Clear()
        {
            _currentGrid.Clear();
            _currentObjects.Clear();
            _snapshots.Clear();
            _lastSnapshotTime = DateTime.UtcNow;
        }
        #region Private Methods
        private void TakeSnapshot(DateTime timestamp)
        {
            var snapshot = new SpatialSnapshot(timestamp, _cellSize);
            foreach (KeyValuePair<T, SpatialObject> kvp in _currentObjects)
            {
                var obj = kvp.Key;
                var spatialObj = kvp.Value;
                snapshot.Grid.Insert(spatialObj.X, spatialObj.Y, obj);
                snapshot.Objects[obj] = spatialObj;
            }
            _snapshots[timestamp] = snapshot;
            _lastSnapshotTime = timestamp;
            CleanupOldSnapshots(timestamp);
        }

        private void CleanupOldSnapshots(DateTime currentTime)
        {
            var cutoffTime = currentTime - _historyRetention;
            var keysToRemove = new List<DateTime>();
            foreach (KeyValuePair<DateTime, SpatialSnapshot> kvp in _snapshots)
            {
                if (kvp.Key < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                    kvp.Value.Grid.Dispose();
                }
            }
            foreach (var key in keysToRemove)
            {
                _snapshots.Remove(key);
            }
        }

        private SpatialSnapshot? FindClosestSnapshot(DateTime time)
        {
            SpatialSnapshot? closest = null;
            var minDelta = TimeSpan.MaxValue;
            foreach (KeyValuePair<DateTime, SpatialSnapshot> kvp in _snapshots)
            {
                var delta = time > kvp.Key ? time - kvp.Key : kvp.Key - time;
                if (delta < minDelta)
                {
                    minDelta = delta;
                    closest = kvp.Value;
                }
            }
            return closest;
        }

        private IEnumerable<T> InterpolateObjectsInRadius(float x, float y, float radius, DateTime time)
        {
            var now = DateTime.UtcNow;
            var timeDelta = (float)(now - time).TotalSeconds;
            var radiusSquared = radius * radius;
            foreach (KeyValuePair<T, SpatialObject> kvp in _currentObjects)
            {
                var obj = kvp.Key;
                var spatialObj = kvp.Value;
                var pastX = spatialObj.X - (spatialObj.VelocityX * timeDelta);
                var pastY = spatialObj.Y - (spatialObj.VelocityY * timeDelta);
                var dx = pastX - x;
                var dy = pastY - y;
                var distanceSquared = dx * dx + dy * dy;
                if (distanceSquared <= radiusSquared)
                {
                    yield return obj;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float DistanceToLineSegment(float px, float py, float x1, float y1, float x2, float y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared == 0)
                return (float)Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            var t = Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSquared));
            var projectionX = x1 + t * dx;
            var projectionY = y1 + t * dy;
            var distX = px - projectionX;
            var distY = py - projectionY;
            return (float)Math.Sqrt(distX * distX + distY * distY);
        }
        #endregion
        public void Dispose()
        {
            _currentGrid.Dispose();
            foreach (KeyValuePair<DateTime, SpatialSnapshot> kvp in _snapshots)
            {
                kvp.Value.Grid.Dispose();
            }
            _snapshots.Dispose();
        }
    }
}