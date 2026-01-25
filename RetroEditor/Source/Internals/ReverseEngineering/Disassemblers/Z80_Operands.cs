using RetroEditor.Plugins;

/// <summary>
/// Represents a Z80 register operand
/// </summary>
internal class Z80RegisterOperand : IOperand
{
    private readonly string _register;

    public Z80RegisterOperand(string register) : base(true, true)
    {
        _register = register;
    }

    public override string Text() => _register;
    public override string Text(ISymbolProvider symbols) => _register;
}

/// <summary>
/// Represents a Z80 immediate value operand
/// </summary>
internal class Z80ImmediateOperand : IOperand
{
    private readonly int _size;

    public Z80ImmediateOperand(ulong value, int size = 1) : base(true, true, value)
    {
        _size = size;
    }

    public override string Text()
    {
        if (_size == 1)
            return $"#{Value:X2}";
        else if (_size == 2)
            return $"#{Value:X4}";
        else
            return $"#{Value:X}";
    }

    public override string Text(ISymbolProvider symbols) => Text();
}

/// <summary>
/// Represents a Z80 address operand
/// </summary>
internal class Z80AddressOperand : IOperand
{
    public Z80AddressOperand(ulong address) : base(true, true, address)
    {
    }

    public override string Text() => $"${Value:X4}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? symbols.GetSymbol(Value, 2) : Text();
}

/// <summary>
/// Represents a Z80 displacement operand (like d16(HL) or relative jumps)
/// </summary>
internal class Z80DisplacementOperand : IOperand
{
    private readonly string _displacement;

    public Z80DisplacementOperand(string displacement) : base(true, true)
    {
        _displacement = displacement;
    }

    public override string Text() => _displacement;
    public override string Text(ISymbolProvider symbols) => _displacement;
}

/// <summary>
/// Represents a Z80 literal value operand (for things like bit numbers in BIT/SET/RES)
/// </summary>
internal class Z80LiteralOperand : IOperand
{
    private readonly string _literalValue;

    public Z80LiteralOperand(string value) : base(true, true)
    {
        _literalValue = value;
    }

    public override string Text() => _literalValue;
    public override string Text(ISymbolProvider symbols) => _literalValue;
}

/// <summary>
/// Represents a Z80 indirect memory operand like (HL)
/// </summary>
internal class Z80IndirectOperand : IOperand
{
    private readonly string _register;

    public Z80IndirectOperand(string register) : base(true, true)
    {
        _register = register;
    }

    public override string Text() => $"({_register})";
    public override string Text(ISymbolProvider symbols) => Text();
}

/// <summary>
/// Represents a Z80 IX/IY+displacement operand
/// </summary>
internal class Z80IndexedOperand : IOperand
{
    private readonly string _indexRegister;
    private readonly sbyte _displacement;

    public Z80IndexedOperand(string indexRegister, sbyte displacement) : base(true, true)
    {
        _indexRegister = indexRegister;
        _displacement = displacement;
    }

    public override string Text()
    {
        if (_displacement >= 0)
            return $"({_indexRegister}+${_displacement:X2})";
        else
            return $"({_indexRegister}-${-_displacement:X2})";
    }

    public override string Text(ISymbolProvider symbols) => Text();
}
