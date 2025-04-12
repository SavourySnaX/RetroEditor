using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Numerics;

internal class Resourcer : IWindow
{
    public float UpdateInterval => 1 / 60.0f;
    
    LibMameDebugger debugger;
    RomDataParser romData;
    string traceFile = "trace.txt";
    bool traceInProgress = false;
    bool newTraceInProgress = false;
    int newTraceCnt=0;
    bool romLoaded = false;

    // Selection state
    private HashSet<UInt64> selectedRows = new HashSet<UInt64>();
    private UInt64? cursorPosition = null;
    private UInt64? selectionStart = null;

    // Memory map data
    private const int MEMORY_MAP_HEIGHT = 50;

    private ResourcerConfig config;

    public Resourcer(LibMameDebugger debugger)
    {
        this.debugger = debugger;

        romData = new RomDataParser();

        config = new ResourcerConfig();
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

        foreach (var region in romData.GetRomRanges)
        {
            drawList.AddRectFilled(
                new Vector2(pos.X + region.Value.AddressStart * scale, pos.Y),
                new Vector2(pos.X + region.Value.AddressEnd * scale, pos.Y + size.Y),
                config.GetColorU32(region.Value.Colour)
            );
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + MEMORY_MAP_HEIGHT);
    }

    bool traceCommandInProgress=false;
    bool traceCommandFinishStarted=false;
    bool traceCommandFinished=false;

    bool cpu_emulationMode=true, cpu_8bitAccumulator=true, cpu_8bitIndex=true;
    public bool Draw()
    {
        DrawMemoryMap();

        var jump=false;
        var traceDisable = traceInProgress||newTraceInProgress;
        if (traceDisable)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Capture Frame"))
        {
            traceInProgress = true;
            if (File.Exists("trace.log"))
            {
                File.Delete("trace.log");
            }
            traceCommandInProgress=true;
            traceCommandFinished=false;
            traceCommandFinishStarted=false;
            // Set up trace logging
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog \"E=%02X|P=%02X|\",e,p}}", (s,id)=>{});
            // Wait for vblank
            debugger.QueueCommand("gvblank", (s,id)=>{traceCommandInProgress=false;});
        }
        ImGui.SameLine();
        if (ImGui.Button("Capture 1 Second"))
        {
            traceInProgress = true;
            if (File.Exists("trace.log"))
            {
                File.Delete("trace.log");
            }
            traceCommandInProgress=true;
            traceCommandFinished=false;
            traceCommandFinishStarted=false;

            // Set up trace logging
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog \"E=%02X|P=%02X|\",e,p}}",(s,id)=>{});
            // Wait for vblank
            debugger.QueueCommand("gtime 1000",(s,id)=>{traceCommandInProgress=false;});
        }
        ImGui.SameLine();
        if (ImGui.Button("New Trace"))
        {
            var pc = romData.GetCPUState(debugger, "PC");
            var e = romData.GetCPUState(debugger, "E");
            var p = romData.GetCPUState(debugger, "P");

            SNES65816Disassembler disassembler = new SNES65816Disassembler();
            var nState = (SNES65816State)disassembler.State;
            nState.SetEmulationMode(e==1);
            nState.Accumulator8Bit=(p & 0x20) == 0x20;
            nState.Index8Bit=(p & 0x10) == 0x10;
            disassembler.State=nState;
            romData.AddCodeRange(disassembler, pc, out var _);
            jumpToAddress = romData.MapSnesCpuToLorom(pc,out var _);
            debugger.SendCommand($"step");
            newTraceCnt=100;
            newTraceInProgress=true;
        }
        if (traceDisable)
        {
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        ImGui.Checkbox("EmulationMode", ref cpu_emulationMode);
        ImGui.SameLine();
        if (cpu_emulationMode)
        {
            ImGui.BeginDisabled();
        }
        ImGui.Checkbox("8Bit Accumulator", ref cpu_8bitAccumulator);
        ImGui.SameLine();
        ImGui.Checkbox("8Bit Index", ref cpu_8bitIndex);
        if (cpu_emulationMode)
        {
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        ImGui.BeginDisabled();
        ImGui.Checkbox("Automated", ref automated);
        ImGui.EndDisabled();

//        DisplayRomData();       
        ScrollableTableView(jump);

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

    bool InputU64ScalarWrapped(string label, ref UInt64 value)
    {
        unsafe
        {
            fixed (UInt64* pValue = &value)
            {
                ImGui.InputScalar(label, ImGuiDataType.U64, (nint)pValue, 0, 0, "%X", ImGuiInputTextFlags.CharsHexadecimal);
                return ImGui.IsItemDeactivated();
            }
        }
    }

    private UInt64 jumpToAddress = 0;
    private void ScrollableTableView(bool jump)
    {
        if (InputU64ScalarWrapped("Jump to Address", ref jumpToAddress))
        {
            jump = true;
        }

        // Start Table and render header
        float[] widths = new float[4];

        ImGui.Columns(4, "MOO", true);
        ImGui.Text("Address");
        ImGui.NextColumn();
        ImGui.Text("Bytes");
        ImGui.NextColumn();
        ImGui.Text("Details");
        ImGui.NextColumn();
        ImGui.Text("Comments");

        for (int i = 0; i < 4; i++)
        {
            widths[i] = ImGui.GetColumnWidth(i);
        }

        ImGui.Columns(1);
        ImGui.Separator();

        ImGuiTableFlags tableFlags = ImGuiTableFlags.None | ImGuiTableFlags.BordersV | ImGuiTableFlags.RowBg;

        var rowHeight = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var contentSize = ImGui.GetContentRegionAvail();
        if (ImGui.BeginChild("Virtual Table", contentSize, ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            var scroll = ImGui.GetScrollY();

            bool moved = false;


            var rom = romData.GetRomData;
            var romStartOffset = romData.GetMinAddress;
            var romEndOffset = romData.GetMaxAddress;

            var regions = romData.GetRomRanges;
            var romDataSize = regions.LineCount;

            UInt64 currentLine = (UInt64)(0 + scroll / rowHeight);
            UInt64 firstLine = currentLine;
            float availableHeight = ImGui.GetContentRegionAvail().Y;
            float tableHeight = availableHeight;
            var visibleLines = (int)(tableHeight / rowHeight);

            // Handle keyboard input for navigation
            if (ImGui.IsWindowFocused())
            {

                if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                {
                    if (cursorPosition.HasValue && cursorPosition.Value > 0)
                    {
                        cursorPosition--;
                        selectedRows.Clear();
                        selectedRows.Add(cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                {
                    if (cursorPosition.HasValue && cursorPosition.Value < romData.GetRomRanges.LineCount - 1)
                    {
                        cursorPosition++;
                        selectedRows.Clear();
                        selectedRows.Add(cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.PageUp))
                {
                    if (cursorPosition.HasValue)
                    {
                        var newPosition = cursorPosition.Value >= (UInt64)visibleLines ?
                            cursorPosition.Value - (UInt64)visibleLines : 0;
                        cursorPosition = newPosition;
                        selectedRows.Clear();
                        selectedRows.Add(cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.PageDown))
                {
                    if (cursorPosition.HasValue)
                    {
                        var newPosition = (UInt64)Math.Min(romData.GetRomRanges.LineCount - 1, cursorPosition.Value + (UInt64)visibleLines);
                        cursorPosition = newPosition;
                        selectedRows.Clear();
                        selectedRows.Add(cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Home))
                {
                    cursorPosition = 0;
                    selectedRows.Clear();
                    selectedRows.Add(0);
                    moved = true;
                }
                if (ImGui.IsKeyPressed(ImGuiKey.End))
                {
                    cursorPosition = romData.GetRomRanges.LineCount - 1;
                    selectedRows.Clear();
                    selectedRows.Add(cursorPosition.Value);
                    moved = true;
                }
                if (selectedRows.Count > 0)
                {
                    UInt64 minAddress = UInt64.MaxValue;
                    UInt64 maxAddress = UInt64.MinValue;
                    foreach (var srow in selectedRows)
                    {
                        var address = regions.FetchAddressForLine(srow);
                        var lastAddress = regions.FetchAddressForLine(srow + 1);
                        if (address < minAddress)
                            minAddress = address;
                        address = (UInt64)lastAddress - 1;
                        if (address > maxAddress)
                            maxAddress = address;
                    }
                    bool clearSelection = false;
                    if (ImGui.IsKeyPressed(ImGuiKey.S))
                    {
                        romData.AddStringRange(minAddress, maxAddress);
                        clearSelection = true;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.U))
                    {
                        romData.AddUnknownRange(minAddress, maxAddress);
                        clearSelection = true;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.C))
                    {
                        // Convert to code
                        var disassembler = new SNES65816Disassembler();
                        var state = ((SNES65816State)disassembler.State);
                        state.SetEmulationMode(cpu_emulationMode);
                        state.Accumulator8Bit = cpu_8bitAccumulator;
                        state.Index8Bit = cpu_8bitIndex;
                        disassembler.State = state;
                        romData.AddCodeRange(disassembler, minAddress, maxAddress);
                        cpu_emulationMode = ((SNES65816State)disassembler.State).EmulationMode;
                        cpu_8bitAccumulator = ((SNES65816State)disassembler.State).Accumulator8Bit;
                        cpu_8bitIndex = ((SNES65816State)disassembler.State).Index8Bit;
                        clearSelection = true;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.A) && !automated)
                    {
                        // Auto disassemble starting at the first selected address
                        var state = ((SNES65816State)autoDisassembler.State);
                        state.SetEmulationMode(cpu_emulationMode);
                        state.Accumulator8Bit = cpu_8bitAccumulator;
                        state.Index8Bit = cpu_8bitIndex;
                        autoDisassembler.State = state;
                        var autoPC = romData.MapRomToCpu(minAddress);
                        autoStack.Clear();
                        autoStack.Push(autoPC);
                        autoState.Push(autoDisassembler.State);
                        automated = true;
                    }

                    if (cursorPosition.HasValue && ImGui.IsKeyPressed(ImGuiKey.Period) && ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        // Jump to next region
                        var currentRegion = regions.GetRangeContainingLine(cursorPosition.Value, out var line);
                        if (currentRegion != null)
                        {
                            var nextRegion = regions.GetRangeContainingLine(currentRegion.LineEnd + 1, out line);
                            if (nextRegion != null)
                            {
                                cursorPosition = nextRegion.LineStart;
                                selectedRows.Clear();
                                selectedRows.Add(cursorPosition.Value);
                                moved = true;
                            }
                        }
                    }
                    if (cursorPosition.HasValue && ImGui.IsKeyPressed(ImGuiKey.Comma) && ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        // Jump to prior region
                        var currentRegion = regions.GetRangeContainingLine(cursorPosition.Value, out var line);
                        if (currentRegion != null)
                        {
                            var nextRegion = regions.GetRangeContainingLine(currentRegion.LineStart - 1, out line);
                            if (nextRegion != null)
                            {
                                cursorPosition = nextRegion.LineEnd;
                                selectedRows.Clear();
                                selectedRows.Add(cursorPosition.Value);
                                moved = true;
                            }
                        }
                    }

                    if (clearSelection)
                    {
                        selectedRows.Clear();
                    }
                }
            }

            if (ImGui.BeginTable("RomDataView", 4, tableFlags))
            {
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, widths[0] - ImGui.GetStyle().ItemSpacing.X * 2);
                ImGui.TableSetupColumn("Bytes", ImGuiTableColumnFlags.WidthFixed, widths[1] - ImGui.GetStyle().ItemSpacing.X);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthFixed, widths[2] - ImGui.GetStyle().ItemSpacing.X);
                ImGui.TableSetupColumn("Comments", ImGuiTableColumnFlags.WidthFixed, widths[3] - ImGui.GetStyle().ItemSpacing.X);

                using var clipper = new ImGuiClipper((int)romDataSize, rowHeight);
                clipper.Begin();
                while (clipper.Step())
                {
                    var actualLine = currentLine;
                    var fetched = regions.GetRangeContainingLine(currentLine, out var line);
                    if (fetched == null)
                    {
                        break;
                    }

                    var lineCount = fetched.Value.LineCount;

                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        if (lineCount == line)
                        {
                            currentLine = fetched.LineEnd + 1;
                            fetched = regions.GetRangeContainingLine(currentLine, out line);
                            if (fetched == null)
                            {
                                break;
                            }
                            lineCount = fetched.Value.LineCount;
                        }

                        ImGui.PushID((int)(actualLine - firstLine));
                        ImGui.TableNextRow();
                        // Make the row interactive
                        ImGui.TableSetColumnIndex(0);

                        var lData = fetched.Value.GetLineInfo(line);

                        // Handle row selection
                        bool isSelected = selectedRows.Contains(actualLine);
                        bool isCursor = cursorPosition == actualLine;

                        bool clicked = ImGui.Selectable($"{lData.Address:X8}", isSelected, ImGuiSelectableFlags.SpanAllColumns);//, new Vector2(0, rowHeight));

                        // Set row background color based on selection state
                        if (isSelected)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.Header));
                        }
                        else if (isCursor)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.HeaderActive));
                        }
                        else if (ImGui.IsItemHovered())
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.HeaderHovered));
                        }
                        else
                        {
                            // Set Colour based on kind
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, config.GetColorU32(fetched.Value.Colour));
                        }

                        if (clicked)
                        {
                            if (ImGui.IsKeyDown(ImGuiKey.ModShift))
                            {
                                // Range selection
                                if (!selectionStart.HasValue)
                                {
                                    selectionStart = cursorPosition ?? actualLine;
                                }
                                var start = Math.Min(selectionStart.Value, actualLine);
                                var end = Math.Max(selectionStart.Value, actualLine);
                                selectedRows.Clear();
                                for (var j = start; j <= end; j++)
                                {
                                    selectedRows.Add(j);
                                }
                            }
                            else
                            {
                                selectedRows.Clear();
                                // Toggle selection
                                if (isSelected)
                                {
                                    selectedRows.Remove(actualLine);
                                }
                                else
                                {
                                    selectedRows.Add(actualLine);
                                }
                                cursorPosition = actualLine;
                                selectionStart = null;
                            }
                        }

                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(lData.Bytes);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text(lData.Details);
                        ImGui.TableSetColumnIndex(3);
                        ImGui.Text(lData.Comment);
                        ImGui.PopID();
                        line++;
                        actualLine++;
                    }
                }
                clipper.End();
                ImGui.EndTable();
            }

            if (jump)
            {
                var jumpLine = romData.GetRomRanges.FetchLineForAddress(jumpToAddress);
                ImGui.SetScrollY(jumpLine * rowHeight);
                jump = false;
            }

            if (moved && cursorPosition.HasValue)
            {
                // Set Scroll position to keep the currsor in 
                if (scroll > cursorPosition.Value * rowHeight)
                {
                    ImGui.SetScrollY(cursorPosition.Value * rowHeight);
                }
                else if (scroll + (visibleLines - 1) * rowHeight < cursorPosition.Value * rowHeight)
                {
                    ImGui.SetScrollY(cursorPosition.Value * rowHeight - (visibleLines - 1) * rowHeight);
                }
                moved = false;
            }
        }
        ImGui.EndChild();
    }

    public bool Initialise()
    {
        return true;
    }

    bool automated=false;
    SNES65816Disassembler autoDisassembler = new SNES65816Disassembler();
    Stack<UInt64> autoStack = new ();
    Stack<ICpuState> autoState = new ();
    public void Update(float seconds)
    {
        if (automated)
        {
            if (autoStack.Count==0 || autoState.Count==0)
            {
                automated = false;
            }
            else
            {
                var autoPC = autoStack.Pop();
                var state = autoState.Pop();
                autoDisassembler.State = state;

                var mappedAddress = romData.MapSnesCpuToLorom(autoPC, out var region);
                if (region==RomDataParser.SNESLoRomRegion.ROM)
                {
                    var r = romData.GetRomRanges.GetRangeContainingAddress(mappedAddress);
                    if (r!=null && r.Value.GetType() == typeof(CodeRegion))
                    {
                        // Already disassembled
                        return;
                    }
                    if (romData.AddCodeRange(autoDisassembler, autoPC, out var instruction))
                    {
                        if (instruction.Mnemonic=="XCE")
                        {
                            return;   
                        }
                        else
                        {
                            foreach (var next in instruction.NextAddresses)
                            {
                                autoStack.Push(next);
                                autoState.Push(autoDisassembler.State);
                            }
                            if (instruction.IsBasicBlockTerminator && instruction.IsBranch)
                            {
                                if (instruction.Mnemonic=="JSR" || instruction.Mnemonic=="JSL")
                                {
                                    // Allow branches to be followed both ways
                                    autoStack.Push(instruction.Address+(UInt64)instruction.Bytes.Length);
                                    autoState.Push(autoDisassembler.State);
                                }
                            }
                        }
                    }
                    else
                    {
                        automated=false;
                        autoStack.Clear();
                        autoState.Clear();
                    }
                }
            }
        }
        if (!romLoaded)
        {
            // Read ROM data in chunks
            romData.Parse(debugger);

            romData.AddStringRange(0x7FC0, 0x7FD4); // LoRom ASCII Title in header
            
            romLoaded = true;
        }

        if (newTraceInProgress)
        {
            if (debugger.IsStopped)
            {
                var pc = romData.GetCPUState(debugger, "PC");
                var e = romData.GetCPUState(debugger, "E");
                var p = romData.GetCPUState(debugger, "P");

                SNES65816Disassembler disassembler = new SNES65816Disassembler();
                var state = ((SNES65816State)disassembler.State);
                state.SetEmulationMode(e==1);
                state.Accumulator8Bit=(p & 0x20) == 0x20;
                state.Index8Bit=(p & 0x10) == 0x10;
                disassembler.State=state;
                romData.AddCodeRange(disassembler, pc, out var _);
                jumpToAddress = romData.MapSnesCpuToLorom(pc,out var _);
                newTraceCnt--;
                if (newTraceCnt==0)
                {
                    newTraceInProgress = false;
                }
                else
                {
                    debugger.SendCommand("step");
                }
            }
        }

        // Handle trace in progress
        if (traceInProgress && !traceCommandInProgress)
        {
            if (debugger.IsStopped && !traceCommandFinishStarted)
            {
                traceCommandFinishStarted=true;
                traceCommandFinished=false;
                debugger.QueueCommand("traceflush",(s,id)=>{});
                debugger.QueueCommand("trace off",(s,id)=>{traceCommandFinished=true;});
            }
            if (debugger.IsStopped && traceCommandFinished)
            {
                // Read and parse trace file
                if (File.Exists(traceFile))
                {
                    // Parse disassembly
                    var lines= File.ReadAllLines(traceFile);
                    int lOffset=0;
                    foreach (var line in lines)
                    {
                        ParseLocation(line);
                        lOffset++;
                    }
                }
                traceInProgress = false;
            }
        }
    }

    private void ParseLocation(string line)
    {
        // Format: E=XX|P=XX|BANK:OFFSET: mnemonic operands
        // Example: E=00|P=00|00:0000 LDA #$00

        // Split the line into parts
        // Ignore empty lines or lines that don't contain the expected format
        if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
            return;

        var parts = line.Split('|');
        if (parts.Length < 3)
            return;

        // Parse emulation mode (E)
        if (!parts[0].StartsWith("E=") || !byte.TryParse(parts[0].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out byte e))
            return;

        // Parse processor status (P)
        if (!parts[1].StartsWith("P=") || !byte.TryParse(parts[1].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out byte p))
            return;

        // Parse bank and offset
        var addressPart = parts[2].Split(' ')[0];
        var addressComponents = addressPart.Split(':');
        if (addressComponents.Length != 3 || 
            !byte.TryParse(addressComponents[0], System.Globalization.NumberStyles.HexNumber, null, out byte bank) ||
            !ushort.TryParse(addressComponents[1], System.Globalization.NumberStyles.HexNumber, null, out ushort offset))
            return;

        // Convert bank:offset to SNES address
        UInt64 snesAddress = (UInt64)((bank << 16) | offset);
        
        // Map SNES address to LoROM address
        // Create a disassembler with the current CPU state
        SNES65816Disassembler disassembler = new SNES65816Disassembler();
        var state = ((SNES65816State)disassembler.State);
        state.SetEmulationMode(e == 1);
        state.Accumulator8Bit = (p & 0x20) == 0x20;
        state.Index8Bit = (p & 0x10) == 0x10;
        disassembler.State = state;

        // Add this location as code
        romData.AddCodeRange(disassembler, snesAddress, out var _);
    }
}