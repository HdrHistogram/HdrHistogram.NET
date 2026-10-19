/*
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 * and released to the public domain, as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 */
using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using HdrHistogram.Utilities;
using Xunit;

namespace HdrHistogram.UnitTests.Utilities
{
    public class ArrayExtensionsTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(23552)]
        public void IsSequenceEqual_returns_true_for_equal_long_arrays(int length)
        {
            var source = new long[length];
            for (int i = 0; i < length; i++)
            {
                source[i] = long.MaxValue - i;
            }
            var other = (long[])source.Clone();

            source.IsSequenceEqual(other).Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(16)]
        [InlineData(32)]
        public void IsSequenceEqual_returns_false_for_different_long_elements(int index)
        {
            var source = new long[33];
            var other = new long[33];
            other[index] = long.MinValue;

            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_returns_false_for_different_lengths()
        {
            new long[] { 1 }.IsSequenceEqual(new long[] { 1, 2 }).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_returns_false_for_different_order()
        {
            new long[] { 1, 2 }.IsSequenceEqual(new long[] { 2, 1 }).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_compares_null_elements_in_reference_arrays()
        {
            string?[] source = { "first", null, "last" };
            string?[] other = { "first", null, "last" };

            source.IsSequenceEqual(other).Should().BeTrue();
            other[1] = "different";
            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_compares_nullable_value_elements()
        {
            long?[] source = { long.MinValue, null, long.MaxValue };
            long?[] other = { long.MinValue, null, long.MaxValue };

            source.IsSequenceEqual(other).Should().BeTrue();
            other[1] = 0;
            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_compares_types_used_by_constructor_lookup()
        {
            Type[] source = { typeof(long), typeof(long), typeof(int) };
            Type[] other = { typeof(long), typeof(long), typeof(int) };

            source.IsSequenceEqual(other).Should().BeTrue();
            other[2] = typeof(long);
            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_compares_types_without_IEquatable()
        {
            ObjectEqualityValue[] source = { new ObjectEqualityValue(1) };
            ObjectEqualityValue[] other = { new ObjectEqualityValue(1) };

            source.IsSequenceEqual(other).Should().BeTrue();
        }

        [Fact]
        public void IsSequenceEqual_preserves_object_equality_for_equatable_classes()
        {
            EquatableReference[] source = { new EquatableReference() };
            EquatableReference[] other = { new EquatableReference() };

            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_preserves_object_equality_for_equatable_structs()
        {
            EquatableValue[] source = { new EquatableValue(1, 2) };
            EquatableValue[] other = { new EquatableValue(1, 3) };

            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Fact]
        public void IsSequenceEqual_preserves_object_equality_for_nullable_structs()
        {
            EquatableValue?[] source = { null, new EquatableValue(1, 2) };
            EquatableValue?[] other = { null, new EquatableValue(1, 3) };

            source.IsSequenceEqual(other).Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void IsSequenceEqual_accepts_covariant_reference_arrays(int length)
        {
            object[] source = new string[length];
            object[] other = new string[length];
            if (length > 0)
            {
                source[0] = "same";
                other[0] = "same";
            }

            source.IsSequenceEqual(other).Should().BeTrue();
        }

        [Fact]
        public void IsSequenceEqual_compares_floating_point_values_using_equality()
        {
            double[] source = { double.NaN, -0.0 };
            double[] other = { BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000000000001)), 0.0 };
            float[] floatSource = { float.NaN, -0.0f };
            float[] floatOther = { float.NaN, 0.0f };

            source.IsSequenceEqual(other).Should().BeTrue();
            floatSource.IsSequenceEqual(floatOther).Should().BeTrue();
        }

        [Fact]
        public void IsSequenceEqual_rejects_null_source()
        {
            long[] source = null!;
            Action compare = () => source.IsSequenceEqual(Array.Empty<long>());

            compare.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void IsSequenceEqual_rejects_null_other()
        {
            long[] other = null!;
            Action compare = () => Array.Empty<long>().IsSequenceEqual(other);

            compare.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void IsSequenceEqual_rejects_two_null_arrays()
        {
            long[] source = null!;
            long[] other = null!;
            Action compare = () => source.IsSequenceEqual(other);

            compare.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void IsSequenceEqual_does_not_allocate_for_long_elements()
        {
            var source = new long[1024];
            var other = new long[1024];
            bool equal = true;
            for (int i = 0; i < 16; i++)
            {
                equal &= source.IsSequenceEqual(other);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 16; i++)
            {
                equal &= source.IsSequenceEqual(other);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            equal.Should().BeTrue();
            allocated.Should().Be(0);
        }

        private sealed class ObjectEqualityValue
        {
            private readonly int _value;

            public ObjectEqualityValue(int value)
            {
                _value = value;
            }

            public override bool Equals(object? obj)
            {
                return obj is ObjectEqualityValue other && _value == other._value;
            }

            public override int GetHashCode()
            {
                return _value;
            }
        }

        private sealed class EquatableReference : IEquatable<EquatableReference>
        {
            public bool Equals(EquatableReference? other)
            {
                return other is not null;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj);
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }
        }

        private readonly struct EquatableValue : IEquatable<EquatableValue>
        {
            private readonly int _key;
            private readonly int _detail;

            public EquatableValue(int key, int detail)
            {
                _key = key;
                _detail = detail;
            }

            public bool Equals(EquatableValue other)
            {
                return _key == other._key;
            }

            public override bool Equals(object? obj)
            {
                return obj is EquatableValue other && _key == other._key && _detail == other._detail;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_key, _detail);
            }
        }
    }
}
