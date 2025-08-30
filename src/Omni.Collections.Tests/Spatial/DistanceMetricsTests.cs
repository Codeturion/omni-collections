using System;
using FluentAssertions;
using Omni.Collections.Spatial.DistanceMetrics;
using Xunit;

namespace Omni.Collections.Tests.Spatial;

public class DistanceMetricsTests
{
    #region EuclideanDistance Tests

    /// <summary>
    /// Tests that EuclideanDistance calculates standard geometric distance correctly.
    /// The metric should return the straight-line distance between two points.
    /// </summary>
    [Fact]
    public void EuclideanDistance_Calculate2DDistance_ReturnsCorrectResult()
    {
        var euclidean = new EuclideanDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distance = euclidean.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(5.0, 0.0001); // 3-4-5 triangle
    }

    /// <summary>
    /// Tests that EuclideanDistance calculates squared distance efficiently.
    /// The squared distance should avoid the expensive square root calculation.
    /// </summary>
    [Fact]
    public void EuclideanDistance_CalculateSquaredDistance_ReturnsCorrectResult()
    {
        var euclidean = new EuclideanDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distanceSquared = euclidean.CalculateDistanceSquared(point1, point2);

        distanceSquared.Should().BeApproximately(25.0, 0.0001); // 3² + 4² = 25
    }

    /// <summary>
    /// Tests EuclideanDistance with 3D points works correctly.
    /// The metric should handle multi-dimensional space calculations.
    /// </summary>
    [Fact]
    public void EuclideanDistance_Calculate3DDistance_ReturnsCorrectResult()
    {
        var euclidean = new EuclideanDistance();
        var point1 = new double[] { 0, 0, 0 };
        var point2 = new double[] { 1, 1, 1 };

        var distance = euclidean.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(Math.Sqrt(3), 0.0001);
    }

    /// <summary>
    /// Tests EuclideanDistance name property returns correct identifier.
    /// The name should clearly identify the distance metric type.
    /// </summary>
    [Fact]
    public void EuclideanDistance_Name_ReturnsCorrectIdentifier()
    {
        var euclidean = new EuclideanDistance();

        euclidean.Name.Should().Be("Euclidean");
    }

    /// <summary>
    /// Tests EuclideanDistance with mismatched dimensions throws exception.
    /// The metric should validate that point dimensions match.
    /// </summary>
    [Fact]
    public void EuclideanDistance_MismatchedDimensions_ThrowsArgumentException()
    {
        var euclidean = new EuclideanDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 1, 1, 1 };

        var act = () => euclidean.CalculateDistance(point1, point2);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Point dimensions must match");
    }

    #endregion

    #region ManhattanDistance Tests

    /// <summary>
    /// Tests that ManhattanDistance calculates city-block distance correctly.
    /// The metric should return the sum of absolute differences along each axis.
    /// </summary>
    [Fact]
    public void ManhattanDistance_Calculate2DDistance_ReturnsCorrectResult()
    {
        var manhattan = new ManhattanDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distance = manhattan.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(7.0, 0.0001); // |3-0| + |4-0| = 7
    }

    /// <summary>
    /// Tests that ManhattanDistance handles negative coordinates correctly.
    /// The metric should use absolute differences for all coordinates.
    /// </summary>
    [Fact]
    public void ManhattanDistance_WithNegativeCoordinates_ReturnsCorrectResult()
    {
        var manhattan = new ManhattanDistance();
        var point1 = new double[] { -2, -3 };
        var point2 = new double[] { 4, 1 };

        var distance = manhattan.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(10.0, 0.0001); // |4-(-2)| + |1-(-3)| = 6 + 4 = 10
    }

    /// <summary>
    /// Tests ManhattanDistance with multi-dimensional points.
    /// The metric should sum absolute differences across all dimensions.
    /// </summary>
    [Fact]
    public void ManhattanDistance_CalculateHighDimensionalDistance_ReturnsCorrectResult()
    {
        var manhattan = new ManhattanDistance();
        var point1 = new double[] { 1, 2, 3, 4 };
        var point2 = new double[] { 2, 4, 1, 6 };

        var distance = manhattan.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(7.0, 0.0001); // |1-2| + |2-4| + |3-1| + |4-6| = 1+2+2+2 = 7
    }

    /// <summary>
    /// Tests ManhattanDistance squared calculation.
    /// The squared distance should be the square of the regular Manhattan distance.
    /// </summary>
    [Fact]
    public void ManhattanDistance_CalculateSquaredDistance_ReturnsCorrectResult()
    {
        var manhattan = new ManhattanDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distanceSquared = manhattan.CalculateDistanceSquared(point1, point2);

        distanceSquared.Should().BeApproximately(49.0, 0.0001); // 7² = 49
    }

    /// <summary>
    /// Tests ManhattanDistance name property returns correct identifier.
    /// The name should clearly identify the distance metric type.
    /// </summary>
    [Fact]
    public void ManhattanDistance_Name_ReturnsCorrectIdentifier()
    {
        var manhattan = new ManhattanDistance();

        manhattan.Name.Should().Be("Manhattan");
    }

    #endregion

    #region ChebyshevDistance Tests

    /// <summary>
    /// Tests that ChebyshevDistance calculates maximum difference correctly.
    /// The metric should return the maximum absolute difference along any axis.
    /// </summary>
    [Fact]
    public void ChebyshevDistance_Calculate2DDistance_ReturnsCorrectResult()
    {
        var chebyshev = new ChebyshevDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distance = chebyshev.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(4.0, 0.0001); // max(|3-0|, |4-0|) = max(3, 4) = 4
    }

    /// <summary>
    /// Tests ChebyshevDistance with equal maximum differences.
    /// The metric should handle cases where multiple dimensions have the same maximum difference.
    /// </summary>
    [Fact]
    public void ChebyshevDistance_WithEqualMaxDifferences_ReturnsCorrectResult()
    {
        var chebyshev = new ChebyshevDistance();
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 5, 5 };

        var distance = chebyshev.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(5.0, 0.0001); // max(|5-0|, |5-0|) = max(5, 5) = 5
    }

    /// <summary>
    /// Tests ChebyshevDistance with multi-dimensional points.
    /// The metric should find the maximum difference across all dimensions.
    /// </summary>
    [Fact]
    public void ChebyshevDistance_CalculateHighDimensionalDistance_ReturnsCorrectResult()
    {
        var chebyshev = new ChebyshevDistance();
        var point1 = new double[] { 1, 2, 3, 4, 5 };
        var point2 = new double[] { 2, 8, 1, 6, 7 };

        var distance = chebyshev.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(6.0, 0.0001); // max(1, 6, 2, 2, 2) = 6
    }

    /// <summary>
    /// Tests ChebyshevDistance with negative coordinates.
    /// The metric should use absolute differences when finding maximum.
    /// </summary>
    [Fact]
    public void ChebyshevDistance_WithNegativeCoordinates_ReturnsCorrectResult()
    {
        var chebyshev = new ChebyshevDistance();
        var point1 = new double[] { -5, 2 };
        var point2 = new double[] { 3, -4 };

        var distance = chebyshev.CalculateDistance(point1, point2);

        distance.Should().BeApproximately(8.0, 0.0001); // max(|3-(-5)|, |-4-2|) = max(8, 6) = 8
    }

    /// <summary>
    /// Tests ChebyshevDistance name property returns correct identifier.
    /// The name should clearly identify the distance metric type.
    /// </summary>
    [Fact]
    public void ChebyshevDistance_Name_ReturnsCorrectIdentifier()
    {
        var chebyshev = new ChebyshevDistance();

        chebyshev.Name.Should().Be("Chebyshev");
    }

    #endregion

    #region MinkowskiDistance Tests

    /// <summary>
    /// Tests MinkowskiDistance constructor with valid p parameter.
    /// The constructor should accept p values greater than or equal to 1.
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    [InlineData(10.0)]
    public void MinkowskiDistance_ValidPParameter_CreatesSuccessfully(double p)
    {
        var act = () => new MinkowskiDistance(p);

        act.Should().NotThrow();
        var minkowski = act();
        minkowski.Name.Should().Be($"Minkowski(p={p})");
    }

    /// <summary>
    /// Tests MinkowskiDistance constructor with invalid p parameter throws exception.
    /// The constructor should reject p values less than 1.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.5)]
    public void MinkowskiDistance_InvalidPParameter_ThrowsArgumentException(double p)
    {
        var act = () => new MinkowskiDistance(p);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Minkowski p parameter must be >= 1*")
            .WithParameterName("p");
    }

    /// <summary>
    /// Tests MinkowskiDistance with p=1 equals Manhattan distance.
    /// The Minkowski distance with p=1 should be identical to Manhattan distance.
    /// </summary>
    [Fact]
    public void MinkowskiDistance_WithP1_EqualsManhattanDistance()
    {
        var minkowski = new MinkowskiDistance(1.0);
        var manhattan = new ManhattanDistance();
        var point1 = new double[] { 1, 2, 3 };
        var point2 = new double[] { 4, 6, 1 };

        var minkowskiDistance = minkowski.CalculateDistance(point1, point2);
        var manhattanDistance = manhattan.CalculateDistance(point1, point2);

        minkowskiDistance.Should().BeApproximately(manhattanDistance, 0.0001);
    }

    /// <summary>
    /// Tests MinkowskiDistance with p=2 equals Euclidean distance.
    /// The Minkowski distance with p=2 should be identical to Euclidean distance.
    /// </summary>
    [Fact]
    public void MinkowskiDistance_WithP2_EqualsEuclideanDistance()
    {
        var minkowski = new MinkowskiDistance(2.0);
        var euclidean = new EuclideanDistance();
        var point1 = new double[] { 1, 2, 3 };
        var point2 = new double[] { 4, 6, 1 };

        var minkowskiDistance = minkowski.CalculateDistance(point1, point2);
        var euclideanDistance = euclidean.CalculateDistance(point1, point2);

        minkowskiDistance.Should().BeApproximately(euclideanDistance, 0.0001);
    }

    /// <summary>
    /// Tests MinkowskiDistance with higher p values calculates correctly.
    /// The metric should handle arbitrary p values with proper mathematical calculation.
    /// </summary>
    [Fact]
    public void MinkowskiDistance_WithHigherP_CalculatesCorrectly()
    {
        var minkowski = new MinkowskiDistance(3.0);
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 2, 2 };

        var distance = minkowski.CalculateDistance(point1, point2);

        // (|2|³ + |2|³)^(1/3) = (8 + 8)^(1/3) = 16^(1/3) ≈ 2.52
        distance.Should().BeApproximately(Math.Pow(16, 1.0/3.0), 0.0001);
    }

    /// <summary>
    /// Tests MinkowskiDistance squared calculation with p=2 optimization.
    /// The squared distance should be optimized for the Euclidean case (p=2).
    /// </summary>
    [Fact]
    public void MinkowskiDistance_SquaredDistanceWithP2_OptimizesCalculation()
    {
        var minkowski = new MinkowskiDistance(2.0);
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distanceSquared = minkowski.CalculateDistanceSquared(point1, point2);

        distanceSquared.Should().BeApproximately(25.0, 0.0001); // Should avoid square root
    }

    /// <summary>
    /// Tests MinkowskiDistance squared calculation with non-Euclidean p values.
    /// The squared distance should fall back to squaring the regular distance.
    /// </summary>
    [Fact]
    public void MinkowskiDistance_SquaredDistanceWithNonEuclideanP_SquaresRegularDistance()
    {
        var minkowski = new MinkowskiDistance(1.0);
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var distance = minkowski.CalculateDistance(point1, point2);
        var distanceSquared = minkowski.CalculateDistanceSquared(point1, point2);

        distanceSquared.Should().BeApproximately(distance * distance, 0.0001);
    }

    #endregion

    #region Comparison Tests

    /// <summary>
    /// Tests that different distance metrics produce different results for the same points.
    /// Each metric should have distinct mathematical behavior.
    /// </summary>
    [Fact]
    public void DistanceMetrics_SamePoints_ProduceDifferentResults()
    {
        var euclidean = new EuclideanDistance();
        var manhattan = new ManhattanDistance();
        var chebyshev = new ChebyshevDistance();
        
        var point1 = new double[] { 0, 0 };
        var point2 = new double[] { 3, 4 };

        var euclideanDist = euclidean.CalculateDistance(point1, point2);     // ≈ 5.0
        var manhattanDist = manhattan.CalculateDistance(point1, point2);     // = 7.0
        var chebyshevDist = chebyshev.CalculateDistance(point1, point2);     // = 4.0

        euclideanDist.Should().NotBe(manhattanDist);
        euclideanDist.Should().NotBe(chebyshevDist);
        manhattanDist.Should().NotBe(chebyshevDist);

        // Verify expected relationships
        chebyshevDist.Should().BeLessThan(euclideanDist);
        euclideanDist.Should().BeLessThan(manhattanDist);
    }

    /// <summary>
    /// Tests distance metric inequality relationships for non-axis-aligned vectors.
    /// The metrics should maintain mathematical relationships: Chebyshev ≤ Euclidean ≤ Manhattan.
    /// </summary>
    [Fact]
    public void DistanceMetrics_InequalityRelationships_MaintainMathematicalOrder()
    {
        var euclidean = new EuclideanDistance();
        var manhattan = new ManhattanDistance();
        var chebyshev = new ChebyshevDistance();
        
        var testCases = new[]
        {
            (new double[] { 0, 0 }, new double[] { 1, 1 }),
            (new double[] { 2, 3 }, new double[] { 5, 7 }),
            (new double[] { -1, -2 }, new double[] { 3, 1 })
        };

        foreach (var (point1, point2) in testCases)
        {
            var euclideanDist = euclidean.CalculateDistance(point1, point2);
            var manhattanDist = manhattan.CalculateDistance(point1, point2);
            var chebyshevDist = chebyshev.CalculateDistance(point1, point2);

            // Mathematical inequality: Chebyshev ≤ Euclidean ≤ Manhattan
            chebyshevDist.Should().BeLessOrEqualTo(euclideanDist + 0.0001);
            euclideanDist.Should().BeLessOrEqualTo(manhattanDist + 0.0001);
        }
    }

    /// <summary>
    /// Tests that all metrics return zero distance for identical points.
    /// Distance from a point to itself should always be zero.
    /// </summary>
    [Fact]
    public void DistanceMetrics_IdenticalPoints_ReturnZeroDistance()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(1.5),
            new MinkowskiDistance(3.0)
        };

        var point = new double[] { 5, -3, 2 };

        foreach (var metric in metrics)
        {
            var distance = metric.CalculateDistance(point, point);
            var distanceSquared = metric.CalculateDistanceSquared(point, point);

            distance.Should().BeApproximately(0.0, 0.0001, $"{metric.Name} should return zero for identical points");
            distanceSquared.Should().BeApproximately(0.0, 0.0001, $"{metric.Name} squared should return zero for identical points");
        }
    }

    /// <summary>
    /// Tests that all metrics satisfy the symmetry property.
    /// Distance from A to B should equal distance from B to A.
    /// </summary>
    [Fact]
    public void DistanceMetrics_SymmetryProperty_SatisfiedByAllMetrics()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(2.5)
        };

        var point1 = new double[] { 1, 2, 3 };
        var point2 = new double[] { 4, 5, 6 };

        foreach (var metric in metrics)
        {
            var distance1to2 = metric.CalculateDistance(point1, point2);
            var distance2to1 = metric.CalculateDistance(point2, point1);

            distance1to2.Should().BeApproximately(distance2to1, 0.0001, 
                $"{metric.Name} should satisfy symmetry property");
        }
    }

    #endregion

    #region Edge Cases and Error Handling

    /// <summary>
    /// Tests that all metrics handle zero vectors correctly.
    /// Distance calculations with zero vectors should return correct results.
    /// </summary>
    [Fact]
    public void DistanceMetrics_ZeroVectors_HandleCorrectly()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(2.0)
        };

        var zeroPoint = new double[] { 0, 0, 0 };
        var nonZeroPoint = new double[] { 3, 4, 5 };

        foreach (var metric in metrics)
        {
            var distance = metric.CalculateDistance(zeroPoint, nonZeroPoint);
            
            distance.Should().BeGreaterThan(0, $"{metric.Name} should return positive distance from zero to non-zero point");
        }
    }

    /// <summary>
    /// Tests that all metrics handle very large coordinate values.
    /// Distance calculations should work with extreme coordinate values.
    /// </summary>
    [Fact]
    public void DistanceMetrics_VeryLargeCoordinates_HandleCorrectly()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(2.0)
        };

        var point1 = new double[] { 1e10, 1e10 };
        var point2 = new double[] { 1e10 + 1, 1e10 + 1 };

        foreach (var metric in metrics)
        {
            var act = () => metric.CalculateDistance(point1, point2);
            
            act.Should().NotThrow($"{metric.Name} should handle large coordinates");
            var distance = act();
            distance.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Tests that all metrics handle single-dimensional points.
    /// Distance calculations should work correctly for 1D space.
    /// </summary>
    [Fact]
    public void DistanceMetrics_SingleDimension_CalculateCorrectly()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(3.0)
        };

        var point1 = new double[] { 5 };
        var point2 = new double[] { 8 };
        var expectedDistance = 3.0;

        foreach (var metric in metrics)
        {
            var distance = metric.CalculateDistance(point1, point2);
            
            distance.Should().BeApproximately(expectedDistance, 0.0001, 
                $"{metric.Name} should calculate 1D distance correctly");
        }
    }

    /// <summary>
    /// Tests dimension mismatch error handling for all metrics.
    /// All metrics should validate that point dimensions match.
    /// </summary>
    [Fact]
    public void DistanceMetrics_DimensionMismatch_ThrowArgumentException()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(2.0)
        };

        var point2D = new double[] { 1, 2 };
        var point3D = new double[] { 1, 2, 3 };

        foreach (var metric in metrics)
        {
            var act = () => metric.CalculateDistance(point2D, point3D);
            
            act.Should().Throw<ArgumentException>()
                .WithMessage("Point dimensions must match", 
                $"{metric.Name} should validate dimension consistency");
        }
    }

    /// <summary>
    /// Tests that metrics handle high-dimensional spaces correctly.
    /// Distance calculations should work efficiently in high dimensions.
    /// </summary>
    [Fact]
    public void DistanceMetrics_HighDimensionalSpace_CalculateCorrectly()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(2.0)
        };

        // Create 10-dimensional points
        var point1 = new double[10];
        var point2 = new double[10];
        
        for (int i = 0; i < 10; i++)
        {
            point1[i] = i;
            point2[i] = i + 1;
        }

        foreach (var metric in metrics)
        {
            var act = () => metric.CalculateDistance(point1, point2);
            
            act.Should().NotThrow($"{metric.Name} should handle high-dimensional spaces");
            var distance = act();
            distance.Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region Performance Characteristics Tests

    /// <summary>
    /// Tests that squared distance calculations are consistent with regular distance.
    /// The squared distance should equal the square of the regular distance.
    /// </summary>
    [Fact]
    public void DistanceMetrics_SquaredDistanceConsistency_MaintainsCorrectRelationship()
    {
        var metrics = new IDistanceMetric[]
        {
            new EuclideanDistance(),
            new ManhattanDistance(),
            new ChebyshevDistance(),
            new MinkowskiDistance(1.5)
        };

        var point1 = new double[] { 1, 2, 3 };
        var point2 = new double[] { 4, 6, 2 };

        foreach (var metric in metrics)
        {
            var distance = metric.CalculateDistance(point1, point2);
            var distanceSquared = metric.CalculateDistanceSquared(point1, point2);
            var expectedSquared = distance * distance;

            distanceSquared.Should().BeApproximately(expectedSquared, 0.0001,
                $"{metric.Name} squared distance should equal regular distance squared");
        }
    }

    /// <summary>
    /// Tests MinkowskiDistance special case optimizations.
    /// The MinkowskiDistance should optimize for common p values (1 and 2).
    /// </summary>
    [Fact]
    public void MinkowskiDistance_SpecialCaseOptimizations_WorkCorrectly()
    {
        var point1 = new double[] { 1, 2 };
        var point2 = new double[] { 4, 6 };

        // Test p=1 optimization (Manhattan)
        var minkowskiP1 = new MinkowskiDistance(1.0);
        var manhattan = new ManhattanDistance();
        
        var minkowskiResult = minkowskiP1.CalculateDistance(point1, point2);
        var manhattanResult = manhattan.CalculateDistance(point1, point2);
        
        minkowskiResult.Should().BeApproximately(manhattanResult, 0.0001);

        // Test p=2 optimization (Euclidean)
        var minkowskiP2 = new MinkowskiDistance(2.0);
        var euclidean = new EuclideanDistance();
        
        var minkowskiEuclidean = minkowskiP2.CalculateDistance(point1, point2);
        var euclideanResult = euclidean.CalculateDistance(point1, point2);
        
        minkowskiEuclidean.Should().BeApproximately(euclideanResult, 0.0001);

        // Test p=2 squared distance optimization
        var minkowskiSquared = minkowskiP2.CalculateDistanceSquared(point1, point2);
        var euclideanSquared = euclidean.CalculateDistanceSquared(point1, point2);
        
        minkowskiSquared.Should().BeApproximately(euclideanSquared, 0.0001);
    }

    #endregion
}