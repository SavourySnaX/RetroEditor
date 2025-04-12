
internal class O65816_ImmediateByteOperand : IOperand
{
    public O65816_ImmediateByteOperand(ulong value) : base(true, false, value) { }
    public override string Text() => $"#${Value:X2}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"#{symbols.GetSymbol(Value, 1)}" : Text();
}

internal class O65816_ImmediateWordOperand : IOperand
{
    public O65816_ImmediateWordOperand(ulong value) : base(true, false, value) { }
    public override string Text() => $"#${Value:X4}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"#{symbols.GetSymbol(Value, 2)}" : Text();
}

internal class O65816_Absolute : IOperand
{
    public O65816_Absolute(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X4}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"{symbols.GetSymbol(Value, 2)}" : Text();
}

internal class O65816_AbsoluteX : IOperand
{
    public O65816_AbsoluteX(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X4},X";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"{symbols.GetSymbol(Value, 2)},X" : Text();
}

internal class O65816_AbsoluteY : IOperand
{
    public O65816_AbsoluteY(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X4},Y";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"{symbols.GetSymbol(Value, 2)},Y" : Text();
}

internal class O65816_AbsoluteLong : IOperand
{
    public O65816_AbsoluteLong(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X6}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 3) ? $"{symbols.GetSymbol(Value, 3)}" : Text();
}

internal class O65816_AbsoluteLongX : IOperand
{
    public O65816_AbsoluteLongX(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X6},X";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 3) ? $"{symbols.GetSymbol(Value, 3)},X" : Text();
}

internal class O65816_AbsoluteIndirect : IOperand
{
    public O65816_AbsoluteIndirect(ulong value) : base(true, false, value) { }
    public override string Text() => $"(${Value:X4})";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"({symbols.GetSymbol(Value, 2)})" : Text();
}

internal class O65816_AbsoluteIndirectX : IOperand
{
    public O65816_AbsoluteIndirectX(ulong value) : base(true, false, value) { }
    public override string Text() => $"(${Value:X4},X)";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"({symbols.GetSymbol(Value, 2)},X)" : Text();
}

internal class O65816_AbsoluteLongIndirect : IOperand
{
    public O65816_AbsoluteLongIndirect(ulong value) : base(true, false, value) { }
    public override string Text() => $"[${Value:X4}]";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"[{symbols.GetSymbol(Value, 2)}]" : Text();
}

internal class O65816_DirectPage : IOperand
{
    public O65816_DirectPage(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X2}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"{symbols.GetSymbol(Value, 1)}" : Text();
}

internal class O65816_DirectPageX : IOperand
{
    public O65816_DirectPageX(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X2},X";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"{symbols.GetSymbol(Value, 1)},X" : Text();
}

internal class O65816_DirectPageY : IOperand
{
    public O65816_DirectPageY(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X2},Y";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"{symbols.GetSymbol(Value, 1)},Y" : Text();
}

internal class O65816_DirectPageIndirect : IOperand
{
    public O65816_DirectPageIndirect(ulong value) : base(true, false, value) { }
    public override string Text() => $"(${Value:X2})";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"({symbols.GetSymbol(Value, 1)})" : Text();
}

internal class O65816_DirectPageIndirectX : IOperand
{
    public O65816_DirectPageIndirectX(ulong value) : base(true, false, value) { }
    public override string Text() => $"(${Value:X2},X)";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"({symbols.GetSymbol(Value, 1)},X)" : Text();
}

internal class O65816_DirectPageIndirectY : IOperand
{
    public O65816_DirectPageIndirectY(ulong value) : base(true, false, value) { }
    public override string Text() => $"(${Value:X2}),Y";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"({symbols.GetSymbol(Value, 1)}),Y" : Text();
}

internal class O65816_DirectPageIndirectLong : IOperand
{
    public O65816_DirectPageIndirectLong(ulong value) : base(true, false, value) { }
    public override string Text() => $"[${Value:X2}]";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"[{symbols.GetSymbol(Value, 1)}]" : Text();
}

internal class O65816_DirectPageIndirectLongY : IOperand
{
    public O65816_DirectPageIndirectLongY(ulong value) : base(true, false, value) { }
    public override string Text() => $"[${Value:X2}],Y";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"[{symbols.GetSymbol(Value, 1)}],Y" : Text();
}

internal class O65816_StackRelative : IOperand
{
    public O65816_StackRelative(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X2},S";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"{symbols.GetSymbol(Value, 1)}, S" : Text();
}

internal class O65816_StackRelativeIndirectY : IOperand
{
    public O65816_StackRelativeIndirectY(ulong value) : base(true, false, value) { }
    public override string Text() => $"(${Value:X2},S),Y";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"({symbols.GetSymbol(Value, 1)},S),Y" : Text();
}

internal class O65816_BlockMove : IOperand
{
    public O65816_BlockMove(ulong value, bool isSource) : base(isSource, !isSource, value) { }
    public override string Text() => $"${Value:X2}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 1) ? $"{symbols.GetSymbol(Value, 1)}" : Text();
}

internal class O65816_PCRelative : IOperand
{
    public O65816_PCRelative(ulong value) : base(true, false, value) { }
    public override string Text() => $"${Value:X4}";
    public override string Text(ISymbolProvider symbols) => symbols.HasSymbol(Value, 2) ? $"{symbols.GetSymbol(Value, 2)}" : Text();
}
