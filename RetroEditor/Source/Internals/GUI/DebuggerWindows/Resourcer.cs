using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using System.Text;

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
    RomDataParser romData;
    string traceFile = "trace.txt";
    List<string> traceLog;
    bool traceInProgress = false;
    bool romLoaded = false;

    // Memory map data
    private const int MEMORY_MAP_HEIGHT = 50;
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
        romData.AddCodeRange(address, (uint)(address + instruction.Size));

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
                        romData.AddDataRange(dataAddr, dataAddr);   // TODO - need to add the size of the data reference
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

        var parser = new RomDataParser();
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


        // Compute the size of the memory map
        var minAddress = romData.GetMinAddress;
        var maxAddress = romData.GetMaxAddress;
        var range = maxAddress - minAddress;
        var scale = size.X / range;

        // Draw background (unknown regions)
        drawList.AddRectFilled(pos, new Vector2(pos.X + size.X, pos.Y + size.Y), ImGui.GetColorU32(unknownColor));

        foreach (var region in romData.GetRomRanges)
        {
            var color = unknownColor;
            switch (region.Value.Region)
            {
                case Regions.Code:
                    color = codeColor;
                    break;
                case Regions.Data:
                    color = dataColor;
                    break;
            }
            drawList.AddRectFilled(
                new Vector2(pos.X + region.Start * scale, pos.Y),
                new Vector2(pos.X + region.End * scale, pos.Y + size.Y),
                ImGui.GetColorU32(color)
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
        if (ImGui.Button("Capture Frame"))
        {
            traceInProgress = true;
            // Set up trace logging
            debugger.SendCommand($"trace {traceFile},,noloop");
            // Wait for vblank
            debugger.SendCommand("gvblank");
        }
        if (ImGui.Button("Capture 100 Milliseconds"))
        {
            traceInProgress = true;
            // Set up trace logging
            debugger.SendCommand($"trace {traceFile},,noloop");
            // Wait for vblank
            debugger.SendCommand("gtime 100");
        }
        if (traceDisable)
        {
            ImGui.EndDisabled();
        }

//        DisplayRomData();       
        ScrollableTableView();

        return false;
    }

    internal unsafe class ImGuiClipper : IDisposable
    {
        public ImGuiClipper(int itemsCount, float itemsHeight)
        {
            _itemsCount = itemsCount;
            _itemsHeight = itemsHeight;
            _clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
        }

        public void Begin()
        {
            ImGuiNative.ImGuiListClipper_Begin(_clipper, _itemsCount, _itemsHeight);
        }

        public bool Step()
        {
            return ImGuiNative.ImGuiListClipper_Step(_clipper) != 0;
        }

        public void End()
        {
            ImGuiNative.ImGuiListClipper_End(_clipper);
        }

        public void Dispose()
        {
            ImGuiNative.ImGuiListClipper_destroy(_clipper);
        }

        public int DisplayStart => _clipper->DisplayStart;
        public int DisplayEnd => _clipper->DisplayEnd;

        private int _itemsCount;
        private float _itemsHeight;
        private ImGuiListClipper* _clipper;
    }

    private void ScrollableTableView()
    {
        // Start Table and render header
        float availableHeight = ImGui.GetContentRegionAvail().Y;
        float tableHeight = availableHeight;
        var lineHeight = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y;

        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders |
                                    ImGuiTableFlags.Resizable;
        if (ImGui.BeginTable("RomDataHeader", 3, tableFlags, new Vector2(0, lineHeight)))
        {
            // Set up columns
            ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn($"Bytes", ImGuiTableColumnFlags.WidthFixed, 30 * 16);
            ImGui.TableSetupColumn("ASCII", ImGuiTableColumnFlags.WidthStretch);

            // Header row
            ImGui.TableHeadersRow();

            ImGui.EndTable();
        }

        var contentSize = ImGui.GetContentRegionAvail();
        if (ImGui.BeginChild("Virtual Table", contentSize, ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            var scroll = ImGui.GetScrollY();

            var rom = romData.GetRomData;
            var romStartOffset = romData.GetMinAddress;
            var romEndOffset = romData.GetMaxAddress;

            var regions = romData.GetRomRanges;
            var romDataSize = rom.Length;

            var address = 0 + scroll/lineHeight;
            if (ImGui.BeginTable("RomDataVirt", 3, tableFlags, new Vector2(0, tableHeight)))
            {
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn($"Bytes", ImGuiTableColumnFlags.WidthFixed, 30 * 16);
                ImGui.TableSetupColumn("ASCII", ImGuiTableColumnFlags.WidthStretch);

                using var clipper = new ImGuiClipper((int)romDataSize, lineHeight);
                clipper.Begin();
                while (clipper.Step())
                {
                    UInt64 curAddress = (UInt64)address;
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        ImGui.TableNextRow();
                        var fetched = regions.GetRangeContainingPoint(curAddress);

                        if (fetched==null)
                        {
                            Console.WriteLine($"WTF");
                            break;
                        }
                        if (fetched.Value.Region == Regions.Code)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(codeColor));
                        }
                        else if (fetched.Value.Region == Regions.Data)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(dataColor));
                        }
                        else
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(unknownColor));
                        }
                        ImGui.TableNextColumn();
                        ImGui.Text($"{curAddress:X8}");
                        ImGui.TableNextColumn();
                        var s = new StringBuilder();
                        if (fetched != null)
                        {
                            byte value = romData.GetByte(curAddress);
                            s.Append($"{value:X2} ");
                        }
                        else
                        {
                            s.Append("   ");
                        }
                        ImGui.Text(s.ToString());
                        ImGui.TableNextColumn();
                        s.Clear();
                        if (fetched != null)
                        {
                            byte value = romData.GetByte(curAddress);
                            s.Append(char.IsControl((char)value) ? '.' : (char)value);
                        }
                        else
                        {
                            s.Append(' ');
                        }
                        curAddress++;
                        ImGui.TextUnformatted(s.ToString());
                    }
                }
                clipper.End();
                ImGui.EndTable();
            }
            ImGui.EndChild();
        }
    }

    private void DisplayRomData()
    {
        var rom = romData.GetRomData;
        var romDataSize = rom.Length;
        var romStartOffset = romData.GetMinAddress;
        var romEndOffset = romData.GetMaxAddress;

        var regions = romData.GetRomRanges;

        // Calculate total bytes and lines needed
        var totalBytes = romEndOffset - romStartOffset;
        var bytesPerLine = 16;
        var totalLines = (totalBytes + (UInt64)bytesPerLine - 1) / (UInt64)bytesPerLine;

        // Calculate visible lines based on window height
        var lineHeight = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var visibleLines = (int)(ImGui.GetContentRegionAvail().Y / lineHeight);
        var scrollMax = Math.Max(0, (int)totalLines - visibleLines);

        // Add vertical scrollbar
        ImGui.BeginChild("RomDataTable", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
        
        // Table flags for resizable columns and borders
        ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | 
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY;


        float totalHeight = (float)totalLines * lineHeight;
        float availableHeight = ImGui.GetContentRegionAvail().Y;
        float tableHeight = Math.Min(totalHeight, availableHeight);

        if (ImGui.BeginTable("RomData", 17, tableFlags, new Vector2(0, tableHeight)))
        {
            // Set up columns
            ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
            for (int i = 0; i < 16; i++)
            {
                ImGui.TableSetupColumn($"B{i:X1}", ImGuiTableColumnFlags.WidthFixed, 30);
            }
            ImGui.TableSetupColumn("ASCII", ImGuiTableColumnFlags.WidthStretch);

            // Header row
            ImGui.TableHeadersRow();

            // Display ROM data
            for (UInt64 line = 0; line < totalLines; line++)
            {
                ImGui.TableNextRow();
                UInt64 lineU64 = (UInt64)line;
                UInt64 bytesPerLineU64 = (UInt64)bytesPerLine;
                UInt64 product = lineU64 * bytesPerLineU64;
                UInt64 lineAddress = romStartOffset + product;

                // Address column
                ImGui.TableNextColumn();
                ImGui.Text($"{lineAddress:X8}");

                // Bytes columns
                StringBuilder asciiBuilder = new StringBuilder();
                for (int i = 0; i < 16; i++)
                {
                    ImGui.TableNextColumn();
                    UInt64 currentAddress = lineAddress + (UInt64)i;
                    if (currentAddress <= romEndOffset)
                    {
                        byte value = romData.GetByte(currentAddress);
                        ImGui.Text($"{value:X2}");
                        asciiBuilder.Append(char.IsControl((char)value) ? '.' : (char)value);
                    }
                    else
                    {
                        ImGui.Text("  ");
                        asciiBuilder.Append(' ');
                    }
                }

                // ASCII column
                ImGui.TableNextColumn();
                ImGui.Text(asciiBuilder.ToString());

                if (ImGui.GetContentRegionAvail().Y < 0)
                {
                    break;
                }
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }

    private void DisplayTrace()
    {
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
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {
        if (!romLoaded)
        {
            // Read ROM data in chunks
            romData = new RomDataParser();

            romData.Parse(debugger);
            
            romLoaded = true;
        }

        // Handle trace in progress
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
                        //ParseDisassembly(line);
                        ParseLocation(line);
                    }
                }
                traceInProgress = false;
            }
        }
    }

    private void ParseLocation(string line)
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

        // Update memory map data
        // Add all addresses from this instruction to code addresses
        romData.AddCodeRange(address, address);
    }


}