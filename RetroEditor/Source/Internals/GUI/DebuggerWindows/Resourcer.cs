using ImGuiNET;
using System.Numerics;

/*

 Rough plan here - 

 Run MAME debugger via console with 

 trace somefile.txt,,,{tracelog "A=%04X ",a} -- replace "",a with system specific needs
 gvblank
 traceflush

 Then pull in the file generated, parse it, use it to hint at the internal resource version of the file

 The resourcer will need storage representing the ROM, the disassembled information (we can initial seed this from the entry point 
 although that would imply we know the rom format .. hmm, perhaps need to think about this some more). 

  Step one - 

   set the console commands
   run a single frame trace
   pull in the log

*/

internal class Resourcer : IWindow
{
    public float UpdateInterval => 1.0f;
    
    LibMameDebugger debugger;
    string traceFile = "trace.txt";
    List<string> traceLog;
    bool traceInProgress = false;

    // Memory map data
    private const int MEMORY_MAP_HEIGHT = 50;
    private const uint ROM_SIZE = 512*1024; 
    private HashSet<uint> codeAddresses = new HashSet<uint>();
    private HashSet<uint> dataAddresses = new HashSet<uint>();
    private Vector4 unknownColor = new Vector4(0, 0, 0, 1); // Black
    private Vector4 codeColor = new Vector4(0, 0, 1, 1);    // Blue
    private Vector4 dataColor = new Vector4(1, 0, 0, 1);    // Red

    // Disassembly data structures
    private class DisassemblyInstruction
    {
        public uint Address { get; set; }
        public string Mnemonic { get; set; }
        public List<DisassemblyOperand> Operands { get; set; } = new List<DisassemblyOperand>();
        public bool IsTerminator { get; set; }
        public int Size { get; set; }
        public bool IsWD65816 { get; set; }
    }

    private class DisassemblyOperand
    {
        public enum OperandType
        {
            Register,
            Immediate,
            Memory,
            Label,
            Condition,
            Port,
            Accumulator,
            Index,
            Stack,
            ProgramBank,
            DataBank,
            DirectPage,
            LongAddress,
            BlockMove
        }

        public enum OperandRole
        {
            Source,
            Destination,
            Both,
            None
        }

        public enum AddressingMode
        {
            Implied,
            Immediate,
            Direct,
            DirectX,
            DirectY,
            DirectIndirect,
            DirectIndirectLong,
            DirectIndirectY,
            DirectIndirectX,
            Absolute,
            AbsoluteX,
            AbsoluteY,
            AbsoluteLong,
            AbsoluteLongX,
            Stack,
            StackIndirectY,
            BlockMove,
            ProgramCounterRelative,
            ProgramCounterRelativeLong
        }

        public string Text { get; set; }
        public OperandType Type { get; set; }
        public OperandRole Role { get; set; }
        public uint? Value { get; set; }
        public AddressingMode Mode { get; set; }
    }

    private Dictionary<uint, DisassemblyInstruction> disassembly = new Dictionary<uint, DisassemblyInstruction>();

    private void ParseDisassembly(string line)
    {
        // Format: BANK:OFFSET mnemonic operands
        var parts = line.Split(' ');
        if (parts.Length < 2) return;

        var addressParts = parts[0].Split(':');
        if (addressParts.Length < 2) return;

        if (!uint.TryParse(addressParts[0], System.Globalization.NumberStyles.HexNumber, null, out uint bank) ||
            !uint.TryParse(addressParts[1], System.Globalization.NumberStyles.HexNumber, null, out uint offset))
            return;

        uint address = (bank << 16) | offset;
        var instruction = new DisassemblyInstruction
        {
            Address = address,
            Mnemonic = parts[1].ToUpper(),
            IsWD65816 = true // Assuming WD65816 for now
        };

        // Parse operands if present
        if (parts.Length > 2)
        {
            var operandText = string.Join(" ", parts.Skip(2));
            ParseOperands(instruction, operandText);
        }

        // Calculate instruction size
        instruction.Size = CalculateInstructionSize(instruction);

        // Mark terminators
        instruction.IsTerminator = IsTerminatorInstruction(instruction.Mnemonic);

        disassembly[address] = instruction;

        // Update memory map data
        // Add all addresses from this instruction to code addresses
        for (uint i = 0; i < instruction.Size; i++)
        {
            codeAddresses.Add(address + i);
        }

        // Check operands for data references
        foreach (var operand in instruction.Operands)
        {
            if (operand.Type == DisassemblyOperand.OperandType.Memory ||
                operand.Type == DisassemblyOperand.OperandType.LongAddress)
            {
                // Try to extract the address value from the operand
                if (operand.Text.StartsWith("$"))
                {
                    var addrStr = operand.Text.TrimStart('$', '(', ')', '[', ']');
                    if (uint.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null, out uint dataAddr))
                    {
                        dataAddresses.Add(dataAddr);
                    }
                }
            }
        }
    }

    private int CalculateInstructionSize(DisassemblyInstruction instruction)
    {
        int size = 1; // Opcode byte

        foreach (var operand in instruction.Operands)
        {
            switch (operand.Mode)
            {
                case DisassemblyOperand.AddressingMode.Immediate:
                    size += operand.Type == DisassemblyOperand.OperandType.Accumulator ? 1 : 2;
                    break;
                case DisassemblyOperand.AddressingMode.Direct:
                case DisassemblyOperand.AddressingMode.DirectX:
                case DisassemblyOperand.AddressingMode.DirectY:
                    size += 1; // Direct page offset
                    break;
                case DisassemblyOperand.AddressingMode.Absolute:
                case DisassemblyOperand.AddressingMode.AbsoluteX:
                case DisassemblyOperand.AddressingMode.AbsoluteY:
                    size += 2; // 16-bit address
                    break;
                case DisassemblyOperand.AddressingMode.AbsoluteLong:
                case DisassemblyOperand.AddressingMode.AbsoluteLongX:
                    size += 3; // 24-bit address
                    break;
                case DisassemblyOperand.AddressingMode.DirectIndirect:
                case DisassemblyOperand.AddressingMode.DirectIndirectY:
                    size += 1; // Direct page offset
                    break;
                case DisassemblyOperand.AddressingMode.DirectIndirectLong:
                    size += 1; // Direct page offset
                    break;
                case DisassemblyOperand.AddressingMode.ProgramCounterRelative:
                    size += 1; // 8-bit offset
                    break;
                case DisassemblyOperand.AddressingMode.ProgramCounterRelativeLong:
                    size += 2; // 16-bit offset
                    break;
                case DisassemblyOperand.AddressingMode.BlockMove:
                    size += 2; // Source and destination direct page offsets
                    break;
            }
        }

        return size;
    }

    private void ParseOperands(DisassemblyInstruction instruction, string operandText)
    {
        var operands = operandText.Split(',').Select(o => o.Trim()).ToList();
        
        foreach (var op in operands)
        {
            var operand = new DisassemblyOperand { Text = op };

            // Determine addressing mode and operand type
            if (op.StartsWith("#"))
            {
                operand.Mode = DisassemblyOperand.AddressingMode.Immediate;
                operand.Type = DisassemblyOperand.OperandType.Immediate;
                if (uint.TryParse(op.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out uint value))
                    operand.Value = value;
            }
            else if (op.StartsWith("("))
            {
                if (op.EndsWith("),Y"))
                {
                    operand.Mode = DisassemblyOperand.AddressingMode.DirectIndirectY;
                    operand.Type = DisassemblyOperand.OperandType.Memory;
                }
                else if (op.EndsWith(")"))
                {
                    operand.Mode = DisassemblyOperand.AddressingMode.DirectIndirect;
                    operand.Type = DisassemblyOperand.OperandType.Memory;
                }
                else if (op.EndsWith("),S),Y"))
                {
                    operand.Mode = DisassemblyOperand.AddressingMode.StackIndirectY;
                    operand.Type = DisassemblyOperand.OperandType.Stack;
                }
            }
            else if (op.StartsWith("["))
            {
                operand.Mode = DisassemblyOperand.AddressingMode.DirectIndirectLong;
                operand.Type = DisassemblyOperand.OperandType.LongAddress;
            }
            else if (op.StartsWith("$"))
            {
                if (op.Contains(",X"))
                {
                    operand.Mode = DisassemblyOperand.AddressingMode.DirectX;
                }
                else if (op.Contains(",Y"))
                {
                    operand.Mode = DisassemblyOperand.AddressingMode.DirectY;
                }
                else
                {
                    operand.Mode = DisassemblyOperand.AddressingMode.Direct;
                }
                operand.Type = DisassemblyOperand.OperandType.Memory;
            }
            else if (IsWD65816Register(op))
            {
                operand.Type = GetWD65816RegisterType(op);
                operand.Mode = DisassemblyOperand.AddressingMode.Implied;
            }
            else if (op.StartsWith("."))
            {
                operand.Type = DisassemblyOperand.OperandType.Label;
                operand.Mode = DisassemblyOperand.AddressingMode.ProgramCounterRelative;
            }

            // Determine operand role based on instruction
            operand.Role = DetermineOperandRole(instruction.Mnemonic, operand);

            instruction.Operands.Add(operand);
        }
    }

    private bool IsWD65816Register(string op)
    {
        var registers = new[] { 
            "A", "X", "Y", "S", "D", "DB", "PB", "P", "C", "M", "X", "E"
        };
        return registers.Contains(op.ToUpper());
    }

    private DisassemblyOperand.OperandType GetWD65816RegisterType(string op)
    {
        return op.ToUpper() switch
        {
            "A" => DisassemblyOperand.OperandType.Accumulator,
            "X" => DisassemblyOperand.OperandType.Index,
            "Y" => DisassemblyOperand.OperandType.Index,
            "S" => DisassemblyOperand.OperandType.Stack,
            "DB" => DisassemblyOperand.OperandType.DataBank,
            "PB" => DisassemblyOperand.OperandType.ProgramBank,
            "D" => DisassemblyOperand.OperandType.DirectPage,
            "P" => DisassemblyOperand.OperandType.Register,
            "C" => DisassemblyOperand.OperandType.Register,
            "M" => DisassemblyOperand.OperandType.Register,
            "E" => DisassemblyOperand.OperandType.Register,
            _ => DisassemblyOperand.OperandType.Register
        };
    }

    private DisassemblyOperand.OperandRole DetermineOperandRole(string mnemonic, DisassemblyOperand operand)
    {
        // Common WD65816 instruction patterns
        var destinationInstructions = new[] { "STA", "STX", "STY", "STZ", "STP", "STP", "STP" };
        var sourceInstructions = new[] { "LDA", "LDX", "LDY", "LDA", "LDA", "LDA", "LDA" };
        var bothInstructions = new[] { "ADC", "SBC", "AND", "ORA", "EOR", "CMP", "CPX", "CPY" };

        if (destinationInstructions.Contains(mnemonic))
            return DisassemblyOperand.OperandRole.Destination;
        if (sourceInstructions.Contains(mnemonic))
            return DisassemblyOperand.OperandRole.Source;
        if (bothInstructions.Contains(mnemonic))
            return DisassemblyOperand.OperandRole.Both;

        return DisassemblyOperand.OperandRole.None;
    }

    private bool IsTerminatorInstruction(string mnemonic)
    {
        var terminators = new[] { 
            "JMP", "JML", "JSR", "JSL", "RTS", "RTL", "RTI", "BRA", "BRL",
            "BEQ", "BNE", "BCS", "BCC", "BVS", "BVC", "BMI", "BPL", "BGE",
            "BLT", "BGT", "BLE", "BNE", "BEQ", "BPL", "BMI", "BVC", "BVS",
            "BCC", "BCS", "BRL", "BRA", "JMP", "JML", "JSR", "JSL", "RTS",
            "RTL", "RTI"
        };
        return terminators.Contains(mnemonic);
    }

    public Resourcer(LibMameDebugger debugger)
    {
        this.debugger = debugger;
        traceLog = new List<string>();
    }

    public void Close()
    {
    }

    private void DrawMemoryMap()
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();
        size.Y = MEMORY_MAP_HEIGHT;

        // Draw background (unknown regions)
        drawList.AddRectFilled(pos, new Vector2(pos.X + size.X, pos.Y + size.Y), ImGui.GetColorU32(unknownColor));

        // Draw code regions
        foreach (var addr in codeAddresses)
        {
            float x = pos.X + (addr * size.X / ROM_SIZE);
            drawList.AddRectFilled(
                new Vector2(x, pos.Y),
                new Vector2(x + 1, pos.Y + size.Y),
                ImGui.GetColorU32(codeColor)
            );
        }

        // Draw data regions
        foreach (var addr in dataAddresses)
        {
            float x = pos.X + (addr * size.X / ROM_SIZE);
            drawList.AddRectFilled(
                new Vector2(x, pos.Y),
                new Vector2(x + 1, pos.Y + size.Y),
                ImGui.GetColorU32(dataColor)
            );
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + MEMORY_MAP_HEIGHT);
    }

    public bool Draw()
    {
        DrawMemoryMap();

        var traceDisable = traceInProgress;
        if (traceDisable)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Start Capture"))
        {
            traceInProgress = true;
            // Set up trace logging
            debugger.SendCommand($"trace {traceFile},,noloop");
            // Wait for vblank
            debugger.SendCommand("gvblank");
        }
        if (traceDisable)
        {
            ImGui.EndDisabled();
        }


        // Display trace log
        if (ImGui.BeginChild("TraceLog", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]); // Use monospace font
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            // Table flags for resizable columns and borders
            ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | 
                                       ImGuiTableFlags.Resizable | 
                                       ImGuiTableFlags.RowBg | 
                                       ImGuiTableFlags.NoBordersInBody;

            if (ImGui.BeginTable("Disassembly", 4, tableFlags))
            {
                // Set up columns
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Bytes", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Mnemonic", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Operands", ImGuiTableColumnFlags.WidthStretch);

                // Header row
                ImGui.TableHeadersRow();

                foreach (var instruction in disassembly.Values.OrderBy(i => i.Address))
                {
                    ImGui.TableNextRow();

                    // Address column
                    ImGui.TableNextColumn();
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), 
                        $"{instruction.Address >> 16:X2}:{instruction.Address & 0xFFFF:X4}");

                    // Bytes column
                    ImGui.TableNextColumn();
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), 
                        $"{instruction.Size:X1}");

                    // Mnemonic column
                    ImGui.TableNextColumn();
                    ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1.0f), 
                        instruction.Mnemonic);

                    // Operands column
                    ImGui.TableNextColumn();
                    for (int i = 0; i < instruction.Operands.Count; i++)
                    {
                        var operand = instruction.Operands[i];
                        var color = operand.Role switch
                        {
                            DisassemblyOperand.OperandRole.Source => new Vector4(0.7f, 1.0f, 0.7f, 1.0f), // Green for source
                            DisassemblyOperand.OperandRole.Destination => new Vector4(1.0f, 0.7f, 0.7f, 1.0f), // Red for destination
                            DisassemblyOperand.OperandRole.Both => new Vector4(1.0f, 1.0f, 0.7f, 1.0f), // Yellow for both
                            _ => new Vector4(0.7f, 0.7f, 0.7f, 1.0f) // Gray for none
                        };

                        ImGui.TextColored(color, operand.Text);
                        if (i < instruction.Operands.Count - 1)
                        {
                            ImGui.SameLine();
                            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), ",");
                            ImGui.SameLine();
                        }
                    }

                    // Add terminator indicator
                    if (instruction.IsTerminator)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.5f, 1.0f), "†");
                    }
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar();
            ImGui.PopFont();
            ImGui.EndChild();
        }

        return false;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {
        // TODO
        if (traceInProgress)
        {
            if (debugger.IsStopped)
            {
                debugger.SendCommand("trace off");

                // Read and parse trace file
                if (File.Exists(traceFile))
                {
                    traceLog.Clear();
                    traceLog.AddRange(File.ReadAllLines(traceFile));

                    // Parse disassembly
                    disassembly.Clear();
                    foreach (var line in traceLog)
                    {
                        ParseDisassembly(line);
                    }
                }
                traceInProgress = false;
            }
        }
    }
}