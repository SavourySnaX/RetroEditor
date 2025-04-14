using System;
using System.Collections.Generic;
using Xunit;
using Moq;
namespace RetroEditor.Tests
{
    internal class TestRange : IRange
    {
        public UInt64 AddressStart { get; private set; }
        public UInt64 AddressEnd { get; private set; }
        public UInt64 LineCount { get; private set; }
        public string Text { get; }

        public TestRange(UInt64 start, UInt64 end, UInt64 lineCount, string text)
        {
            AddressStart = start;
            AddressEnd = end;
            LineCount = lineCount;
            Text = text;
        }

        public bool IsSame(IRange other)
        {
            if (other is not TestRange testRange)
                return false;
            return Text == testRange.Text;
        }

        public IRange SplitAfter(ulong position)
        {
            if (position >= AddressEnd)
                return null;

            var newRange = new TestRange(position + 1, AddressEnd, LineCount, Text);
            AddressEnd = position;
            return newRange;
        }

        public IRange SplitBefore(ulong position)
        {
            if (position <= AddressStart)
                return null;

            var newRange = new TestRange(AddressStart, position - 1, LineCount, Text);
            AddressStart = position;
            return newRange;
        }

        public void CombineAdjacent(IRange other)
        {
            if (other is not TestRange testRange)
                throw new ArgumentException("Ranges are not the same type");
            if (!IsSame(other))
                throw new ArgumentException("Ranges are not the same type");

            if (AddressEnd + 1 != other.AddressStart)
                throw new ArgumentException("Ranges are not adjacent");

            AddressEnd = testRange.AddressEnd;
            LineCount += testRange.LineCount;
        }

        public ulong LineOffsetForAddress(ulong address)
        {
            if (address < AddressStart || address > AddressEnd)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of range");

            return address - AddressStart;
        }

        public ulong AddressForLine(ulong line)
        {
            if (line < 0 || line >= LineCount)
                throw new ArgumentOutOfRangeException(nameof(line), "Line is out of range");

            return AddressStart + line;
        }

        public void Overwrite(IRange other)
        {
        }

    }

    public class RangeCollectionTests
    {
        [Fact]
        public void AddRange_UpdatesLineCount()
        {
            var collection = new RangeCollection<TestRange>();
            var range = new TestRange(0, 10, 5, "Hello");

            collection.AddRange(range);
            Assert.Equal(5UL, collection.LineCount);
        }

        [Fact]
        public void AddMultipleRanges_AccumulatesLineCount()
        {
            var collection = new RangeCollection<TestRange>();

            collection.AddRange(new TestRange(0, 10, 5, "Hello"));
            collection.AddRange(new TestRange(11, 20, 3, "World"));
            collection.AddRange(new TestRange(21, 30, 7, "Test"));

            Assert.Equal(15UL, collection.LineCount);
        }

        [Fact]
        public void GetRangeContainingAddress_ReturnsCorrectRange()
        {
            var collection = new RangeCollection<TestRange>();
            var range = new TestRange(0, 10, 5, "Hello");
            collection.AddRange(range);

            var result = collection.GetRangeContainingAddress(5);
            Assert.NotNull(result);
            Assert.Equal(range.AddressStart, result.Value.AddressStart);
            Assert.Equal(range.AddressEnd, result.Value.AddressEnd);
            Assert.Equal(range.LineCount, result.Value.LineCount);
        }

        [Fact]
        public void GetRangeContainingAddress_OutsideRange_ReturnsNull()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 10, 5, "Hello"));

            var result = collection.GetRangeContainingAddress(11);
            Assert.Null(result);
        }

        [Fact]
        public void Count_ReflectsNumberOfRanges()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 10, 5, "Hello"));
            collection.AddRange(new TestRange(11, 20, 3, "World"));

            Assert.Equal(2, collection.Count);
        }

        [Fact]
        public void Enumerator_ReturnsAllRanges()
        {
            var collection = new RangeCollection<TestRange>();
            var ranges = new[]
            {
                new TestRange(0, 10, 5, "Hello"),
                new TestRange(11, 20, 3, "World"),
                new TestRange(21, 30, 7, "Test")
            };

            foreach (var range in ranges)
            {
                collection.AddRange(range);
            }

            var enumeratedRanges = new List<TestRange>();
            foreach (var range in collection)
            {
                enumeratedRanges.Add(range.Value);
            }

            Assert.Equal(ranges.Length, enumeratedRanges.Count);
            for (int i = 0; i < ranges.Length; i++)
            {
                Assert.Equal(ranges[i].AddressStart, enumeratedRanges[i].AddressStart);
                Assert.Equal(ranges[i].AddressEnd, enumeratedRanges[i].AddressEnd);
                Assert.Equal(ranges[i].LineCount, enumeratedRanges[i].LineCount);
            }
        }

        [Fact]
        public void AddRange_OverlappingRanges_UpdatesLineCountCorrectly()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 10, 5, "Hello"));
            collection.AddRange(new TestRange(5, 15, 3, "World"));

            Assert.Equal(8UL, collection.LineCount);
        }

        [Fact]
        public void AddRange_AdjacentRangesWithSameText_MergesRanges()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 5, 3, "Hello"));
            collection.AddRange(new TestRange(6, 10, 2, "Hello"));

            var ranges = new List<TestRange>();
            foreach (var range in collection)
            {
                ranges.Add(range.Value);
            }

            Assert.Single(ranges);
            Assert.Equal(0UL, ranges[0].AddressStart);
            Assert.Equal(10UL, ranges[0].AddressEnd);
            Assert.Equal(5UL, ranges[0].LineCount);
            Assert.Equal("Hello", ranges[0].Text);
        }

        [Fact]
        public void AddRange_AdjacentRangesWithDifferentText_DoesNotMerge()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 5, 3, "Hello"));
            collection.AddRange(new TestRange(6, 10, 2, "World"));

            var ranges = new List<TestRange>();
            foreach (var range in collection)
            {
                ranges.Add(range.Value);
            }

            Assert.Equal(2, ranges.Count);
            Assert.Equal(0UL, ranges[0].AddressStart);
            Assert.Equal(5UL, ranges[0].AddressEnd);
            Assert.Equal(3UL, ranges[0].LineCount);
            Assert.Equal("Hello", ranges[0].Text);
            Assert.Equal(6UL, ranges[1].AddressStart);
            Assert.Equal(10UL, ranges[1].AddressEnd);
            Assert.Equal(2UL, ranges[1].LineCount);
            Assert.Equal("World", ranges[1].Text);
        }

        [Fact]
        public void AddRange_MultipleAdjacentRangesWithSameText_MergesAllRanges()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 5, 2, "Hello"));
            collection.AddRange(new TestRange(6, 10, 2, "Hello"));
            collection.AddRange(new TestRange(11, 15, 2, "Hello"));

            var ranges = new List<TestRange>();
            foreach (var range in collection)
            {
                ranges.Add(range.Value);
            }

            Assert.Single(ranges);
            Assert.Equal(0UL, ranges[0].AddressStart);
            Assert.Equal(15UL, ranges[0].AddressEnd);
            Assert.Equal(6UL, ranges[0].LineCount);
            Assert.Equal("Hello", ranges[0].Text);
        }

        [Fact]
        public void AddRange_NonAdjacentRangesWithSameText_DoesNotMerge()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 5, 2, "Hello"));
            collection.AddRange(new TestRange(7, 10, 2, "Hello"));

            var ranges = new List<TestRange>();
            foreach (var range in collection)
            {
                ranges.Add(range.Value);
            }

            Assert.Equal(2, ranges.Count);
            Assert.Equal(0UL, ranges[0].AddressStart);
            Assert.Equal(5UL, ranges[0].AddressEnd);
            Assert.Equal(2UL, ranges[0].LineCount);
            Assert.Equal("Hello", ranges[0].Text);
            Assert.Equal(7UL, ranges[1].AddressStart);
            Assert.Equal(10UL, ranges[1].AddressEnd);
            Assert.Equal(2UL, ranges[1].LineCount);
            Assert.Equal("Hello", ranges[1].Text);
        }

        [Fact]
        public void AddRange_MixedAdjacentRanges_OnlyMergesSameText()
        {
            var collection = new RangeCollection<TestRange>();
            collection.AddRange(new TestRange(0, 5, 2, "Hello"));
            collection.AddRange(new TestRange(6, 10, 2, "World"));
            collection.AddRange(new TestRange(11, 15, 2, "Hello"));

            var ranges = new List<TestRange>();
            foreach (var range in collection)
            {
                ranges.Add(range.Value);
            }

            Assert.Equal(3, ranges.Count);
            Assert.Equal(0UL, ranges[0].AddressStart);
            Assert.Equal(5UL, ranges[0].AddressEnd);
            Assert.Equal(2UL, ranges[0].LineCount);
            Assert.Equal("Hello", ranges[0].Text);
            Assert.Equal(6UL, ranges[1].AddressStart);
            Assert.Equal(10UL, ranges[1].AddressEnd);
            Assert.Equal(2UL, ranges[1].LineCount);
            Assert.Equal("World", ranges[1].Text);
            Assert.Equal(11UL, ranges[2].AddressStart);
            Assert.Equal(15UL, ranges[2].AddressEnd);
            Assert.Equal(2UL, ranges[2].LineCount);
            Assert.Equal("Hello", ranges[2].Text);
        }
    }
}