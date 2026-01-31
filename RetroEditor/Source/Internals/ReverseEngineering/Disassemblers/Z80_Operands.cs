using RetroEditor.Plugins;

/// <summary>
/// Z80 register indices matching the Z80.cs arrays
/// </summary>
internal enum Z80Register : byte
{
    B = 0, C = 1, D = 2, E = 3, H = 4, L = 5, IndirectHL = 6, A = 7
}

/// <summary>
/// Z80 16-bit register indices 
/// </summary>
internal enum Z80Register16 : byte
{
    BC = 0, DE = 1, HL = 2, SP = 3
}

/// <summary>
/// Z80 index registers
/// </summary>
internal enum Z80IndexRegister : byte
{
    IX = 0, IY = 1
}

/// <summary>
/// Represents a Z80 register operand
/// </summary>
internal class Z80RegisterOperand : IOperand
{
    private static readonly string[] RegisterNames = { "B", "C", "D", "E", "H", "L", "(HL)", "A" };

    public Z80RegisterOperand(Z80Register register) : base(true, true, (ulong)register)
    {
    }

    public override string Text() => RegisterNames[Value];
    public override string Text(ISymbolProvider symbols) => RegisterNames[Value];
}

/// <summary>
/// Represents a Z80 16-bit register operand
/// </summary>
internal class Z80Register16Operand : IOperand
{
    private static readonly string[] Register16Names = { "BC", "DE", "HL", "SP" };

    public Z80Register16Operand(Z80Register16 register) : base(true, true, (ulong)register)
    {
    }

    public override string Text() => Register16Names[Value];
    public override string Text(ISymbolProvider symbols) => Register16Names[Value];
}

/// <summary>
/// Represents a Z80 immediate value operand
/// </summary>
internal class Z80ImmediateOperand : IOperand
{
    private readonly int _size;

    public Z80ImmediateOperand(ulong value, int size = 1) : base(false, false, value)
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
/// Represents a Z80 indirect address operand like ($1234)
/// </summary>
internal class Z80IndirectAddressOperand : IOperand
{
    public Z80IndirectAddressOperand(ulong address) : base(true, true, address)
    {
    }

    public override string Text() => $"(${Value:X4})";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"({symbols.GetSymbol(Value, 2)})" : Text();
}

/// <summary>
/// Represents a Z80 displacement operand (like d16(HL) or relative jumps)
/// </summary>
internal class Z80DisplacementOperand : IOperand
{
    private readonly sbyte _displacement;

    public Z80DisplacementOperand(sbyte displacement) : base(false, false, 0)
    {
        _displacement = displacement;
    }

    public override string Text() => _displacement.ToString();
    public override string Text(ISymbolProvider symbols) => _displacement.ToString();
}

/// <summary>
/// Represents a Z80 literal value operand (for things like bit numbers in BIT/SET/RES)
/// </summary>
internal class Z80LiteralOperand : IOperand
{
    public Z80LiteralOperand(ulong value) : base(false, false, value)
    {
    }

    public override string Text() => Value.ToString();
    public override string Text(ISymbolProvider symbols) => Value.ToString();
}

/// <summary>
/// Represents a Z80 indirect memory operand like (HL)
/// </summary>
internal class Z80IndirectOperand : IOperand
{
    private static readonly string[] Register16Names = { "BC", "DE", "HL", "SP" };

    public Z80IndirectOperand(Z80Register16 register) : base(true, true, (ulong)register)
    {
    }

    public override string Text() => $"({Register16Names[Value]})";
    public override string Text(ISymbolProvider symbols) => Text();
}

/// <summary>
/// Represents a Z80 IX/IY+displacement operand
/// </summary>
internal class Z80IndexedOperand : IOperand
{
    private static readonly string[] IndexRegisterNames = { "IX", "IY" };
    private readonly sbyte _displacement;

    public Z80IndexedOperand(Z80IndexRegister indexRegister, sbyte displacement) : base(true, true, (ulong)indexRegister)
    {
        _displacement = displacement;
    }

    public sbyte Displacement => _displacement;

    public override string Text()
    {
        if (_displacement >= 0)
            return $"({IndexRegisterNames[Value]}+${_displacement:X2})";
        else
            return $"({IndexRegisterNames[Value]}-${-_displacement:X2})";
    }

    public override string Text(ISymbolProvider symbols) => Text();
}

/// <summary>
/// Represents a Z80 condition code operand
/// </summary>
internal class Z80ConditionOperand : IOperand
{
    private static readonly string[] ConditionNames = { "NZ", "Z", "NC", "C", "PO", "PE", "P", "M" };

    public Z80ConditionOperand(int condition) : base(false, false, (ulong)condition)
    {
    }

    public override string Text() => ConditionNames[Value];
    public override string Text(ISymbolProvider symbols) => ConditionNames[Value];
}

/// <summary>
/// Represents special Z80 register operands like IX, IY, IXH, IXL, I, R, etc. that don't fit into standard enums
/// This stores the register name as a hash for efficiency
/// </summary>
internal class Z80SpecialRegisterOperand : IOperand
{
    private static readonly Dictionary<ulong, string> _registerNames = new()
    {
        { 0, "IX" }, { 1, "IY" }, { 2, "IXH" }, { 3, "IXL" }, { 4, "IYH" }, { 5, "IYL" },
        { 6, "I" }, { 7, "R" }, { 8, "AF'" }, { 9, "(IX)" }, { 10, "(IY)" }, { 11, "(SP)" }
    };
    
    private static ulong GetHashForRegister(string name)
    {
        return name switch
        {
            "IX" => 0, "IY" => 1, "IXH" => 2, "IXL" => 3, "IYH" => 4, "IYL" => 5,
            "I" => 6, "R" => 7, "AF'" => 8, "(IX)" => 9, "(IY)" => 10, "(SP)" => 11,
            _ => (ulong)name.GetHashCode()  // Fallback for unknown registers
        };
    }

    private readonly string _registerName;

    public Z80SpecialRegisterOperand(string registerName) : base(true, true, GetHashForRegister(registerName))
    {
        _registerName = registerName;
    }

    public override string Text() => _registerName;
    public override string Text(ISymbolProvider symbols) => _registerName;
}

/// <summary>
/// Represents a Z80 relative branch target operand
/// </summary>
internal class Z80RelativeOperand : IOperand
{
    public Z80RelativeOperand(ulong targetAddress) : base(false, false, targetAddress)
    {
    }

    public override string Text() => $"${Value:X4}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? symbols.GetSymbol(Value, 2) : Text();
}
