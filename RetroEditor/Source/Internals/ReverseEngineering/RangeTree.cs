using System.Collections;

internal interface IRange
{
    UInt64 AddressStart { get; }
    UInt64 AddressEnd { get; }
    UInt64 LineCount { get; }

    // Updates the range to be the range from the start to the given position, and returns a new range that is the range from the given position to the end.
    IRange SplitAfter(UInt64 position);
    // Updates the range to be the range from the given position to the end, and returns a new range that is the range from the start to the given position.
    IRange SplitBefore(UInt64 position);
    // Combines the current range with the given range, and updates the current range to be the combined range.
    void CombineAdjacent(IRange other);

    // Returns true if the current range is the same as the given range.
    bool IsSame(IRange other);
}

internal class Range<T> where T : class, IRange
{
    public UInt64 LineStart { get; set; }
    public UInt64 LineEnd { get; set; }
    public T Value { get; set; }

    public Range(T value)
    {
        Value = value;
    }
}

internal class AVLNode<T> where T : class, IRange
{
    public Range<T> Range { get; set; }
    public AVLNode<T>? Left { get; set; }
    public AVLNode<T>? Right { get; set; }
    public int Height { get; set; }

    public AVLNode(Range<T> range)
    {
        Range = range;
        Height = 1;
    }
}

internal class AVLTree<T> : IEnumerable<Range<T>> where T : class, IRange
{
    private AVLNode<T>? root;
    private UInt64 totalLineCount = 0;

    public UInt64 LineCount => totalLineCount;

    public void AddRange(T value)
    {
        if (value.AddressEnd < value.AddressStart)
            throw new ArgumentException("End should be greater than or equal to start.");

        var newRange = new Range<T>(value);
        root = Insert(root, newRange);
        MergeAdjacentRanges();
    }

    private AVLNode<T>? Insert(AVLNode<T>? node, Range<T> newRange)
    {
        if (node == null)
            return new AVLNode<T>(newRange);

        if (newRange.Value.AddressEnd < node.Range.Value.AddressStart)
            node.Left = Insert(node.Left, newRange);
        else if (newRange.Value.AddressStart > node.Range.Value.AddressEnd)
            node.Right = Insert(node.Right, newRange);
        else
        {
            if (newRange.Value.AddressStart > node.Range.Value.AddressStart)
            {
                var leftRange = new Range<T>((T)node.Range.Value.SplitBefore(newRange.Value.AddressStart));
                node.Left = Insert(node.Left, leftRange);
            }

            if (newRange.Value.AddressEnd < node.Range.Value.AddressEnd)
            {
                var rightRange = new Range<T>((T)node.Range.Value.SplitAfter(newRange.Value.AddressEnd));
                node.Right = Insert(node.Right, rightRange);
            }

            node.Range = newRange;
            return node;
        }

        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
        return Balance(node);
    }

    private AVLNode<T>? Balance(AVLNode<T>? node)
    {
        if (node == null)
            return null;

        int balance = GetBalance(node);

        if (balance > 1)
        {
            if (GetBalance(node.Left) < 0)
                node.Left = RotateLeft(node.Left);

            return RotateRight(node);
        }
        if (balance < -1)
        {
            if (GetBalance(node.Right) > 0)
                node.Right = RotateRight(node.Right);

            return RotateLeft(node);
        }

        return node;
    }

    private AVLNode<T>? RotateLeft(AVLNode<T>? node)
    {
        if (node == null || node.Right == null)
            return node;

        AVLNode<T> newRoot = node.Right;
        node.Right = newRoot.Left;
        newRoot.Left = node;
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
        newRoot.Height = 1 + Math.Max(GetHeight(newRoot.Left), GetHeight(newRoot.Right));
        return newRoot;
    }

    private AVLNode<T>? RotateRight(AVLNode<T>? node)
    {
        if (node == null || node.Left == null)
            return node;

        AVLNode<T> newRoot = node.Left;
        node.Left = newRoot.Right;
        newRoot.Right = node;
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
        newRoot.Height = 1 + Math.Max(GetHeight(newRoot.Left), GetHeight(newRoot.Right));
        return newRoot;
    }

    private int GetHeight(AVLNode<T>? node)
    {
        return node == null ? 0 : node.Height;
    }

    private int GetBalance(AVLNode<T>? node)
    {
        return node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);
    }

    private void MergeAdjacentRanges()
    {
        var ranges = new List<Range<T>>();
        InOrderTraversal(root, ranges);
        root = null;

        totalLineCount = 0;
        for (int i = 0; i < ranges.Count; i++)
        {
            var current = ranges[i];
            while (i + 1 < ranges.Count && ranges[i + 1].Value.AddressStart == current.Value.AddressEnd + 1 && ranges[i+1].Value.IsSame(current.Value))
            {
                current.Value.CombineAdjacent(ranges[i + 1].Value);
                i++;
            }

            current.LineStart=totalLineCount;
            current.LineEnd = totalLineCount + current.Value.LineCount - 1;
            totalLineCount += current.Value.LineCount;
            root = Insert(root, current);
        }
    }


    private void InOrderTraversal(AVLNode<T>? node, List<Range<T>> ranges)
    {
        if (node == null)
            return;

        InOrderTraversal(node.Left, ranges);
        ranges.Add(node.Range);
        InOrderTraversal(node.Right, ranges);
    }

    public Range<T>? GetRangeContainingAddress(UInt64 point)
    {
        return GetRangeContainingAddress(root, point);
    }

    public Range<T>? GetRangeContainingLine(UInt64 point)
    {
        return GetRangeContainingLine(root, point);
    }

    private Range<T>? GetRangeContainingAddress(AVLNode<T>? node, UInt64 point)
    {
        if (node == null)
            return null;

        if (point >= node.Range.Value.AddressStart && point <= node.Range.Value.AddressEnd)
            return node.Range;

        if (point < node.Range.Value.AddressStart)
            return GetRangeContainingAddress(node.Left, point);

        return GetRangeContainingAddress(node.Right, point);
    }

    private Range<T>? GetRangeContainingLine(AVLNode<T>? node, UInt64 point)
    {
        if (node == null)
            return null;

        if (point >= node.Range.LineStart && point <= node.Range.LineEnd)
            return node.Range;

        if (point < node.Range.LineStart)
            return GetRangeContainingLine(node.Left, point);

        return GetRangeContainingLine(node.Right, point);
    }

    public int Count
    {
        get
        {
            var ranges = new List<Range<T>>();
            InOrderTraversal(root, ranges);
            return ranges.Count;
        }
    }

    public IEnumerator<Range<T>> GetEnumerator()
    {
        var ranges = new List<Range<T>>();
        InOrderTraversal(root, ranges);
        return ranges.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class RangeCollection<T> : IEnumerable<Range<T>> where T : class, IRange
{
    private readonly AVLTree<T> avlTree = new AVLTree<T>();

    public void AddRange(T value)
    {
        avlTree.AddRange(value);
    }

    public Range<T>? GetRangeContainingAddress(UInt64 point)
    {
        return avlTree.GetRangeContainingAddress(point);
    }

    public Range<T>? GetRangeContainingLine(UInt64 line, out UInt64 lineOff)
    {
        var range = avlTree.GetRangeContainingLine(line);
        lineOff = 0;
        if (range != null)
        {
            lineOff = line - range.LineStart;
        }
        return range;
    }

    public int Count => avlTree.Count;

    public IEnumerator<Range<T>> GetEnumerator()
    {
        return avlTree.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public UInt64 LineCount => avlTree.LineCount;
}
