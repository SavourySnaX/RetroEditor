using System.Collections;

internal interface IRange
{
    UInt64 Start { get; }
    UInt64 End { get; }

    IRange CreateRange(UInt64 start, UInt64 end);

    bool IsSame(IRange other);
}

internal class Range<T> where T : struct, IRange
{
    public UInt64 Start => Value.Start;
    public UInt64 End => Value.End;
    public T Value { get; set; }

    public Range(T value)
    {
        Value = value;
    }
}

internal class AVLNode<T> where T : struct, IRange
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

internal class AVLTree<T> : IEnumerable<Range<T>> where T : struct, IRange
{
    private AVLNode<T>? root;

    public void AddRange(T value)
    {
        if (value.End < value.Start)
            throw new ArgumentException("End should be greater than or equal to start.");

        var newRange = new Range<T>(value);
        root = Insert(root, newRange);
        MergeAdjacentRanges();
    }

    public void RemoveRange(UInt64 start, UInt64 end)
    {
        if (end < start)
            throw new ArgumentException("End should be greater than or equal to start.");

        root = Remove(root, start, end);
    }

    private AVLNode<T>? Insert(AVLNode<T>? node, Range<T> newRange)
    {
        if (node == null)
            return new AVLNode<T>(newRange);

        if (newRange.Value.End < node.Range.Value.Start)
            node.Left = Insert(node.Left, newRange);
        else if (newRange.Value.Start > node.Range.Value.End)
            node.Right = Insert(node.Right, newRange);
        else
        {
            if (newRange.Value.Start > node.Range.Value.Start)
            {
                var leftRange = CreateRange(node.Range.Value.Start, newRange.Value.Start - 1, node.Range.Value);
                node.Left = Insert(node.Left, leftRange);
            }

            if (newRange.Value.End < node.Range.Value.End)
            {
                var rightRange = CreateRange(newRange.Value.End + 1, node.Range.Value.End, node.Range.Value);
                node.Right = Insert(node.Right, rightRange);
            }

            node.Range = newRange;
            return node;
        }

        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
        return Balance(node);
    }

    private AVLNode<T>? Remove(AVLNode<T>? node, UInt64 start, UInt64 end)
    {
        if (node == null)
            return null;

        if (end < node.Range.Value.Start)
        {
            node.Left = Remove(node.Left, start, end);
        }
        else if (start > node.Range.Value.End)
        {
            node.Right = Remove(node.Right, start, end);
        }
        else
        {
            if (start > node.Range.Value.Start)
            {
                var leftRange = CreateRange(node.Range.Value.Start, start - 1, node.Range.Value);
                node.Left = Insert(node.Left, leftRange);
            }

            if (end < node.Range.Value.End)
            {
                var rightRange = CreateRange(end + 1, node.Range.Value.End, node.Range.Value);
                node.Right = Insert(node.Right, rightRange);
            }
            node = MergeNodes(node.Left, node.Right);

            if (node != null)
            {
                start = Math.Max(start, node.Range.Value.Start);
                end = Math.Min(end, node.Range.Value.End);

                if (start < end)
                {
                    node = Remove(node, start, end);
                }
            }
        }

        if (node != null)
        {
            node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
            node = Balance(node);
        }

        return node;
    }

    private Range<T> CreateRange(UInt64 start, UInt64 end, T template)
    {
        var irange = template.CreateRange(start, end);
        return new Range<T>((T)irange);
    }

    private AVLNode<T>? MergeNodes(AVLNode<T>? left, AVLNode<T>? right)
    {
        if (left == null)
            return right;

        if (right == null)
            return left;

        var maxLeft = FindMax(left);
        maxLeft.Right = right;
        return Balance(maxLeft);
    }

    private AVLNode<T> FindMax(AVLNode<T> node)
    {
        while (node.Right != null)
            node = node.Right;
        return node;
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

        for (int i = 0; i < ranges.Count; i++)
        {
            var current = ranges[i];
            var end = current.End;
            while (i + 1 < ranges.Count && ranges[i + 1].Start == current.End + 1 && ranges[i+1].Value.IsSame(current.Value))
            {
                end = ranges[i + 1].End;
                i++;
            }

            var newRange = CreateRange(current.Start, end, current.Value);
            root = Insert(root, newRange);
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

    public Range<T>? GetRangeContainingPoint(UInt64 point)
    {
        return GetRangeContainingPoint(root, point);
    }

    private Range<T>? GetRangeContainingPoint(AVLNode<T>? node, UInt64 point)
    {
        if (node == null)
            return null;

        if (point >= node.Range.Value.Start && point <= node.Range.Value.End)
            return node.Range;

        if (point < node.Range.Value.Start)
            return GetRangeContainingPoint(node.Left, point);

        return GetRangeContainingPoint(node.Right, point);
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

internal class RangeCollection<T> : IEnumerable<Range<T>> where T : struct, IRange
{
    private readonly AVLTree<T> avlTree = new AVLTree<T>();

    public void AddRange(T value)
    {
        avlTree.AddRange(value);
    }

    public void RemoveRange(UInt64 start, UInt64 end)
    {
        avlTree.RemoveRange(start, end);
    }

    public Range<T>? GetRangeContainingPoint(UInt64 point)
    {
        return avlTree.GetRangeContainingPoint(point);
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
}
