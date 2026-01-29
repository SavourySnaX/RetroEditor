// Operand classes for the 68000 disassembler

internal class OM68000_DataRegister : IOperand
{
    public OM68000_DataRegister(int register) : base(true, true, (ulong)register) { }
    public override string Text() => $"D{Value}";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AddressRegister : IOperand
{
    public OM68000_AddressRegister(int register) : base(true, true, (ulong)register) { }
    public override string Text() => $"A{Value}";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AddressIndirect : IOperand
{
    public OM68000_AddressIndirect(int register) : base(true, true, (ulong)register) { }
    public override string Text() => $"(A{Value})";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AddressPostInc : IOperand
{
    public OM68000_AddressPostInc(int register) : base(true, true, (ulong)register) { }
    public override string Text() => $"(A{Value})+";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AddressPreDec : IOperand
{
    public OM68000_AddressPreDec(int register) : base(true, true, (ulong)register) { }
    public override string Text() => $"-(A{Value})";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AddressDisplacement : IOperand
{
    private short _displacement;
    
    public OM68000_AddressDisplacement(int register, short displacement) : base(true, true, (ulong)register)
    {
        _displacement = displacement;
    }

    public short Displacement => _displacement;
    
    public override string Text() => $"{_displacement}(A{Value})";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AddressIndex : IOperand
{
    private int _indexReg;
    private bool _isAddress;
    private bool _isLong;
    private sbyte _offset;
    
    public OM68000_AddressIndex(int baseReg, int indexReg, bool isAddress, bool isLong, sbyte offset) 
        : base(true, true, (ulong)baseReg)
    {
        _indexReg = indexReg;
        _isAddress = isAddress;
        _isLong = isLong;
        _offset = offset;
    }

    public int IndexRegister => _indexReg;
    public bool IsAddressRegister => _isAddress;
    public bool IsLong => _isLong;
    public sbyte Offset => _offset;
    
    public override string Text()
    {
        string indexRegName = _isAddress ? $"A{_indexReg}" : $"D{_indexReg}";
        string size = _isLong ? ".L" : ".W";
        return $"{_offset}(A{Value},{indexRegName}{size})";
    }
    
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_AbsoluteShort : IOperand
{
    public OM68000_AbsoluteShort(ulong address) : base(true, true, address) { }
    public override string Text() => $"${Value:X4}.W";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? symbols.GetSymbol(Value, 2) : Text();
}

internal class OM68000_AbsoluteLong : IOperand
{
    public OM68000_AbsoluteLong(ulong address) : base(true, true, address) { }
    public override string Text() => $"${Value:X8}.L";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 4) ? symbols.GetSymbol(Value, 4) : Text();
}

internal class OM68000_BranchTarget : IOperand
{
    public OM68000_BranchTarget(ulong address) : base(true, true, address) { }
    public override string Text() => $"${Value:X6}.L";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 3) ? symbols.GetSymbol(Value, 3) : Text();
}

internal class OM68000_PCDisplacement : IOperand
{
    private short _displacement;
    
    public OM68000_PCDisplacement(short displacement) : base(true, false, 0)
    {
        _displacement = displacement;
    }

    public short Displacement => _displacement;
    
    public override string Text() => $"{_displacement}(PC)";
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_PCIndex : IOperand
{
    private int _indexReg;
    private bool _isAddress;
    private bool _isLong;
    private sbyte _offset;
    
    public OM68000_PCIndex(int indexReg, bool isAddress, bool isLong, sbyte offset) 
        : base(true, false, 0)
    {
        _indexReg = indexReg;
        _isAddress = isAddress;
        _isLong = isLong;
        _offset = offset;
    }

    public int IndexRegister => _indexReg;
    public bool IsAddressRegister => _isAddress;
    public bool IsLong => _isLong;
    public sbyte Offset => _offset;
    
    public override string Text()
    {
        string indexRegName = _isAddress ? $"A{_indexReg}" : $"D{_indexReg}";
        string size = _isLong ? ".L" : ".W";
        return $"{_offset}(PC,{indexRegName}{size})";
    }
    
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_Immediate : IOperand
{
    private SizeCode _size;
    
    public OM68000_Immediate(ulong value, SizeCode size) : base(true, false, value)
    {
        _size = size;
    }
    
    public override string Text()
    {
        return _size switch
        {
            SizeCode.Byte => $"#${Value:X2}",
            SizeCode.Word => $"#${Value:X4}",
            SizeCode.Long => $"#${Value:X8}",
            _ => $"#{Value}"
        };
    }
    
    public override string Text(ISymbolProvider symbols)
    {
        int size = _size switch
        {
            SizeCode.Byte => 1,
            SizeCode.Word => 2,
            SizeCode.Long => 4,
            _ => 0
        };
        
        return symbols.HasSymbol(Value, size) ? $"#{symbols.GetSymbol(Value, size)}" : Text();
    }
}

internal class OM68000_StatusRegister : IOperand
{
    private string _name;
    
    public OM68000_StatusRegister(string name) : base(true, true, 0)
    {
        _name = name;
    }
    
    public override string Text() => _name;
    public override string Text(ISymbolProvider symbols) => Text();
}

internal class OM68000_RegisterList : IOperand
{
    private ushort _mask;
    
    public OM68000_RegisterList(ushort mask) : base(true, true, mask)
    {
        _mask = mask;
    }
    
    public override string Text()
    {
        var registers = new List<string>();
        
        // Data registers (D0-D7)
        for (int i = 0; i < 8; i++)
        {
            if ((_mask & (1 << i)) != 0)
                registers.Add($"D{i}");
        }
        
        // Address registers (A0-A7)
        for (int i = 0; i < 8; i++)
        {
            if ((_mask & (1 << (i + 8))) != 0)
                registers.Add($"A{i}");
        }
        
        return string.Join("/", registers);
    }
    
    public override string Text(ISymbolProvider symbols) => Text();
}
