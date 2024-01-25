

using System.Text.Json;

public class MemoryblockCollection
{
    public struct MemoryBlock
    {
        public uint address { get; set; }
        public byte[] data { get; set; }
    }

    SortedDictionary<uint, MemoryBlock> memoryBlocks;
    public MemoryblockCollection()
    {
        memoryBlocks = new SortedDictionary<uint, MemoryBlock>();
    }

    public IEnumerable<MemoryBlock> Blocks => memoryBlocks.Values;

    private void InternalAddRegion(uint address, ReadOnlySpan<byte> span)
    {
        foreach (var block in memoryBlocks)
        {
            if ((block.Value.address < address) && (block.Value.address + block.Value.data.Length - 1 < address))
            {
                continue;
            }
            if (block.Value.address > address + span.Length - 1)
            {
                // safe to insert as a whole 
                memoryBlocks.Add(address, new MemoryBlock() { address = address, data = span.ToArray() });
                return;
            }
            if (block.Value.address <= address)
            {
                int overlap =(int)(address - block.Value.address);
                int remain = block.Value.data.Length - overlap;
                if (remain <= span.Length)
                {
                    span.CopyTo(block.Value.data.AsSpan(overlap, remain));
                    address += (uint)remain;
                    span = span.Slice(remain);
                    if (span.Length == 0)
                    {
                        return;
                    }
                    continue;
                }
                else
                {
                    // replace part of block
                    span.CopyTo(block.Value.data.AsSpan(overlap, span.Length));
                    return;
                }
            }

            // If we reach here, we have some sort of overlap with a block
            throw new Exception("Overlapping memory blocks (unreachable)");
        }
        memoryBlocks.Add(address, new MemoryBlock() { address = address, data = span.ToArray() });
    }

    private bool InternalJoinASegment()
    {
        var iter = memoryBlocks.GetEnumerator();
        if (!iter.MoveNext())
        {
            return false;
        }
        var current = iter.Current;
        while (iter.MoveNext())
        {
            var next = iter.Current;
            if ((current.Value.address + current.Value.data.Length) == next.Value.address)
            {
                var joined = new MemoryBlock() { address = current.Value.address, data = current.Value.data.Concat(next.Value.data).ToArray() };
                memoryBlocks.Remove(current.Value.address);
                memoryBlocks.Remove(next.Value.address);
                memoryBlocks.Add(joined.address, joined);
                return true;
            }
            else
            {
                current = next;
            }
        }
        return false;
    }

    private void InternalJoinSegments()
    {
        bool modified = true;
        while (modified)
        {
            modified = InternalJoinASegment();
        }
    }

    public void AddRegion(uint address, ReadOnlySpan<byte> data)
    {
        if (memoryBlocks.Count==0)
        {
            memoryBlocks.Add(address, new MemoryBlock() { address = address, data = data.ToArray() });
            return;
        }
        var span = data;
        InternalAddRegion(address, span);
        InternalJoinSegments();
    }

    public string Serialise()
    {
        return JsonSerializer.Serialize(memoryBlocks);
    }

    public void Deserialise(string json)
    {
        memoryBlocks = JsonSerializer.Deserialize<SortedDictionary<uint, MemoryBlock>>(json);
    }
}