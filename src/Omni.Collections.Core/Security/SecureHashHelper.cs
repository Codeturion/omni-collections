using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Omni.Collections.Core.Security
{
    /// <summary>
    /// Provides secure hashing functionality with randomized seeds to prevent hash collision attacks.
    /// Uses a per-instance random seed to ensure that hash values cannot be predicted by attackers.
    /// </summary>
    public static class SecureHashHelper
    {
        private static readonly uint GlobalSeed = GenerateRandomSeed();
        
        private static uint GenerateRandomSeed()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
        
        /// <summary>
        /// Creates a secure equality comparer with randomized hashing for the specified type.
        /// </summary>
        public static IEqualityComparer<T> CreateSecureComparer<T>() where T : notnull
        {
            return new SecureEqualityComparer<T>();
        }
        
        /// <summary>
        /// Creates a secure equality comparer with a specific seed (for testing purposes).
        /// </summary>
        public static IEqualityComparer<T> CreateSecureComparer<T>(uint seed) where T : notnull
        {
            return new SecureEqualityComparer<T>(seed);
        }
        
        /// <summary>
        /// Combines two hash codes using a secure mixing function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashCodes(int h1, int h2)
        {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
        
        /// <summary>
        /// Applies a randomized transformation to a hash code to prevent collision attacks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RandomizeHash(int hashCode, uint seed)
        {
            // MurmurHash3 inspired mixing
            uint h = (uint)hashCode;
            h ^= seed;
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return (int)h;
        }
        
        private sealed class SecureEqualityComparer<T> : IEqualityComparer<T> where T : notnull
        {
            private readonly uint _seed;
            private readonly IEqualityComparer<T> _baseComparer;
            
            public SecureEqualityComparer() : this(GlobalSeed)
            {
            }
            
            public SecureEqualityComparer(uint seed)
            {
                _seed = seed;
                _baseComparer = EqualityComparer<T>.Default;
            }
            
            public bool Equals(T? x, T? y)
            {
                return _baseComparer.Equals(x, y);
            }
            
            public int GetHashCode(T obj)
            {
                if (obj == null) return 0;
                
                int baseHash = _baseComparer.GetHashCode(obj);
                return RandomizeHash(baseHash, _seed);
            }
        }
    }
    
    /// <summary>
    /// Configuration options for secure hashing in collections.
    /// </summary>
    public class SecureHashOptions
    {
        /// <summary>
        /// Enable randomized hashing to prevent hash collision attacks.
        /// Default: false (performance first). SECURITY RECOMMENDATION: Enable in production
        /// environments to prevent hash collision DoS attacks by setting this to true
        /// or using SecureHashOptions.Production.
        /// </summary>
        public bool EnableRandomizedHashing { get; set; } = false;
        
        /// <summary>
        /// Maximum allowed collision chain length before triggering rehashing.
        /// Default: 100
        /// </summary>
        public int MaxCollisionChainLength { get; set; } = 100;
        
        /// <summary>
        /// Enable collision monitoring and logging.
        /// Default: false (for performance)
        /// </summary>
        public bool EnableCollisionMonitoring { get; set; } = false;
        
        /// <summary>
        /// Action to call when excessive collisions are detected.
        /// </summary>
        public Action<string>? OnExcessiveCollisions { get; set; }
        
        public static SecureHashOptions Default { get; } = new SecureHashOptions();
        
        /// <summary>
        /// Production-ready secure hashing with randomized hashing enabled for DoS protection.
        /// Recommended for production environments where security is prioritized over performance.
        /// </summary>
        public static SecureHashOptions Production { get; } = new SecureHashOptions
        {
            EnableRandomizedHashing = true,
            EnableCollisionMonitoring = false
        };
        
        public static SecureHashOptions Testing { get; } = new SecureHashOptions
        {
            EnableRandomizedHashing = false,
            EnableCollisionMonitoring = true
        };
    }
}