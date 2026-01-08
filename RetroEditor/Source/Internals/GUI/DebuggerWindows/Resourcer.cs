using MyMGui;
using System.Numerics;

internal class Resourcer : IWindow
{
    public float UpdateInterval => 1 / 60.0f;
    public bool MinimumSize => false;
    
    LibMameDebugger debugger;
    RomDataParser romData;
    string traceFile = "trace.txt";
    bool traceInProgress = false;
    bool newTraceInProgress = false;
    int newTraceCnt=0;
    bool romLoaded = false;

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
        romData.Save("TEST.JSON");
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
        ImGui.Dummy();
    }

    bool traceCommandInProgress=false;
    bool traceCommandFinishStarted=false;
    bool traceCommandFinished=false;
    bool traceContinue = false;

    bool cpu_emulationMode=true, cpu_8bitAccumulator=true, cpu_8bitIndex=true;
    public bool Draw()
    {
        DrawMemoryMap();

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
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {TRACEREGS}}}", (s,id)=>{});
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
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {TRACEREGS}}}",(s,id)=>{});
            // Wait for vblank
            debugger.QueueCommand("gtime 1000",(s,id)=>{traceCommandInProgress=false;});
        }
        ImGui.SameLine();
        if (ImGui.Button("Capture Continuous"))
        {
            traceInProgress = true;
            if (File.Exists("trace.log"))
            {
                File.Delete("trace.log");
            }
            traceCommandInProgress=true;
            traceCommandFinished=false;
            traceCommandFinishStarted=false;
            traceContinue = true;

            // Set up trace logging
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {TRACEREGS}}}",(s,id)=>{});
            debugger.QueueCommand("gtime 500",(s,id)=>{traceCommandInProgress=false;});
        }
        ImGui.SameLine();
        if (ImGui.Button("New Trace"))
        {
            var pc = romData.GetCPUState(debugger, "PC");
            var e = romData.GetCPUState(debugger, "E");
            var p = romData.GetCPUState(debugger, "P");

            SNES65816Disassembler disassembler = new SNES65816Disassembler();
            var nState = (SNES65816State)disassembler.State;
            nState.SetEmulationMode(e == 1);
            nState.Accumulator8Bit = (p & 0x20) == 0x20;
            nState.Index8Bit = (p & 0x10) == 0x10;
            disassembler.State = nState;
            romData.AddCodeRange(disassembler, pc, out var _);
            cartridgeVars.jumpToAddress = romData.MapSnesCpuToLorom(pc, out var _);
            debugger.SendCommand($"step");
            newTraceCnt = 100;
            newTraceInProgress = true;
        }
        if (traceDisable)
        {
            ImGui.EndDisabled();
        }
        if (!traceDisable)
        {
            ImGui.BeginDisabled();
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop Trace"))
        {
            traceContinue = false;
        }
        if (!traceDisable)
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


        if (ImGui.BeginTabBar("ResourcerTabs"))
        {
            if (ImGui.BeginTabItem("Cartridge"))
            {
                ScrollableTableView(romData.GetRomRanges, RomDataParser.RangeRegion.Cartridge, cartridgeVars);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Ram"))
            {
                ScrollableTableView(romData.GetRamRanges, RomDataParser.RangeRegion.RAM, ramVars);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        return false;
    }

    bool InputU64ScalarWrapped(string label, ref UInt64 value)
    {
        ImGui.InputScalar(label,ImGuiDataType.U64, ref value, "%X", ImGuiInputTextFlags.CharsHexadecimal);
        return ImGui.IsItemDeactivated();
    }

    private class ScrollViewVars
    {
        public UInt64 jumpToAddress = 0;
        // Selection state
        public HashSet<UInt64> selectedRows = new HashSet<UInt64>();
        public UInt64? cursorPosition = null;
        public UInt64? selectionStart = null;
    }

    ScrollViewVars cartridgeVars = new();
    ScrollViewVars ramVars = new();

    private void ScrollableTableView(RangeCollection<IRegionInfo> regions, RomDataParser.RangeRegion rangeRegion, ScrollViewVars vars)
    {
        var jump=false;
        if (InputU64ScalarWrapped("Jump to Address", ref vars.jumpToAddress))
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
                    if (vars.cursorPosition.HasValue && vars.cursorPosition.Value > 0)
                    {
                        vars.cursorPosition--;
                        vars.selectedRows.Clear();
                        vars.selectedRows.Add(vars.cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                {
                    if (vars.cursorPosition.HasValue && vars.cursorPosition.Value < romData.GetRomRanges.LineCount - 1)
                    {
                        vars.cursorPosition++;
                        vars.selectedRows.Clear();
                        vars.selectedRows.Add(vars.cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.PageUp))
                {
                    if (vars.cursorPosition.HasValue)
                    {
                        var newPosition = vars.cursorPosition.Value >= (UInt64)visibleLines ?
                            vars.cursorPosition.Value - (UInt64)visibleLines : 0;
                        vars.cursorPosition = newPosition;
                        vars.selectedRows.Clear();
                        vars.selectedRows.Add(vars.cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.PageDown))
                {
                    if (vars.cursorPosition.HasValue)
                    {
                        var newPosition = (UInt64)Math.Min(romData.GetRomRanges.LineCount - 1, vars.cursorPosition.Value + (UInt64)visibleLines);
                        vars.cursorPosition = newPosition;
                        vars.selectedRows.Clear();
                        vars.selectedRows.Add(vars.cursorPosition.Value);
                        moved = true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Home))
                {
                    vars.cursorPosition = 0;
                    vars.selectedRows.Clear();
                    vars.selectedRows.Add(0);
                    moved = true;
                }
                if (ImGui.IsKeyPressed(ImGuiKey.End))
                {
                    vars.cursorPosition = romData.GetRomRanges.LineCount - 1;
                    vars.selectedRows.Clear();
                    vars.selectedRows.Add(vars.cursorPosition.Value);
                    moved = true;
                }
                if (vars.selectedRows.Count > 0)
                {
                    UInt64 minAddress = UInt64.MaxValue;
                    UInt64 maxAddress = UInt64.MinValue;
                    foreach (var srow in vars.selectedRows)
                    {
                        var address = regions.FetchAddressForLine(srow);
                        var lastAddress = regions.FetchAddressForLine(srow + 1);
                        if (address < minAddress)
                            minAddress = address;
                        if (lastAddress>0)
                            lastAddress--;
                        address = Math.Max(minAddress, lastAddress);
                        if (address > maxAddress)
                            maxAddress = address;
                    }
                    if (vars.cursorPosition != null)
                    {
                        UInt64 cursorMinAddress = regions.FetchAddressForLine(vars.cursorPosition.Value);
                        UInt64 cursorMaxAddress = regions.FetchAddressForLine(vars.cursorPosition.Value + 1);
                        if (cursorMaxAddress > 0)
                            cursorMaxAddress--;
                        cursorMaxAddress = Math.Max(cursorMinAddress, cursorMaxAddress);

                        if (ImGui.IsKeyPressed(ImGuiKey.L))
                        {
                            // Labels only apply to current line
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey.Semicolon))
                        {
                            romData.AddCommentRange(rangeRegion, ["I AM THE VERY MODEL OF A MODERN MAJOR GENERAL", "I'VE INFORMATION ANIMAL VEGETABLE AND MINERAL", "....."], cursorMinAddress);
                        }
                    }

                    bool clearSelection = false;
                    if (ImGui.IsKeyPressed(ImGuiKey.S))
                    {
                        romData.AddStringRange(rangeRegion, minAddress, maxAddress);
                        clearSelection = true;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.U))
                    {
                        romData.AddUnknownRange(rangeRegion, minAddress, maxAddress);
                        clearSelection = true;
                    }
                    if (rangeRegion==RomDataParser.RangeRegion.Cartridge)
                    {
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
                            autoState.Clear();
                            stacked.Clear();
                            autoStack.Push(autoPC);
                            autoState.Push(autoDisassembler.State);
                            automated = true;
                        }
                    }

                    if (vars.cursorPosition.HasValue && ImGui.IsKeyPressed(ImGuiKey.Period) && ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        // Jump to next region
                        var currentRegion = regions.GetRangeContainingLine(vars.cursorPosition.Value, out var line);
                        if (currentRegion != null)
                        {
                            var nextRegion = regions.GetRangeContainingLine(currentRegion.LineEnd + 1, out line);
                            if (nextRegion != null)
                            {
                                vars.cursorPosition = nextRegion.LineStart;
                                vars.selectedRows.Clear();
                                vars.selectedRows.Add(vars.cursorPosition.Value);
                                moved = true;
                            }
                        }
                    }
                    if (vars.cursorPosition.HasValue && ImGui.IsKeyPressed(ImGuiKey.Comma) && ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        // Jump to prior region
                        var currentRegion = regions.GetRangeContainingLine(vars.cursorPosition.Value, out var line);
                        if (currentRegion != null)
                        {
                            var nextRegion = regions.GetRangeContainingLine(currentRegion.LineStart - 1, out line);
                            if (nextRegion != null)
                            {
                                vars.cursorPosition = nextRegion.LineEnd;
                                vars.selectedRows.Clear();
                                vars.selectedRows.Add(vars.cursorPosition.Value);
                                moved = true;
                            }
                        }
                    }

                    if (clearSelection)
                    {
                        vars.selectedRows.Clear();
                    }
                }
            }

            var itemSpacing = ImGui.GetStyle().ItemSpacing.X;

            if (ImGui.BeginTable("RomDataView", 4, tableFlags))
            {
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, widths[0] - itemSpacing * 2);
                ImGui.TableSetupColumn("Bytes", ImGuiTableColumnFlags.WidthFixed, widths[1] - itemSpacing);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthFixed, widths[2] - itemSpacing);
                ImGui.TableSetupColumn("Comments", ImGuiTableColumnFlags.WidthFixed, widths[3] - itemSpacing);

                using var clipper = new ListClipper((int)romDataSize, rowHeight);
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
                        bool isSelected = vars.selectedRows.Contains(actualLine);
                        bool isCursor = vars.cursorPosition == actualLine;

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
                                if (!vars.selectionStart.HasValue)
                                {
                                    vars.selectionStart = vars.cursorPosition ?? actualLine;
                                }
                                var start = Math.Min(vars.selectionStart.Value, actualLine);
                                var end = Math.Max(vars.selectionStart.Value, actualLine);
                                vars.selectedRows.Clear();
                                for (var j = start; j <= end; j++)
                                {
                                    vars.selectedRows.Add(j);
                                }
                            }
                            else
                            {
                                vars.selectedRows.Clear();
                                // Toggle selection
                                if (isSelected)
                                {
                                    vars.selectedRows.Remove(actualLine);
                                }
                                else
                                {
                                    vars.selectedRows.Add(actualLine);
                                }
                                vars.cursorPosition = actualLine;
                                vars.selectionStart = null;
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
                var jumpLine = romData.GetRomRanges.FetchLineForAddress(vars.jumpToAddress);
                ImGui.SetScrollY(jumpLine * rowHeight);
                jump = false;
            }

            if (moved && vars.cursorPosition.HasValue)
            {
                // Set Scroll position to keep the currsor in 
                if (scroll > vars.cursorPosition.Value * rowHeight)
                {
                    ImGui.SetScrollY(vars.cursorPosition.Value * rowHeight);
                }
                else if (scroll + (visibleLines - 1) * rowHeight < vars.cursorPosition.Value * rowHeight)
                {
                    ImGui.SetScrollY(vars.cursorPosition.Value * rowHeight - (visibleLines - 1) * rowHeight);
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
    HashSet<UInt64> stacked = new();

    public void Update(float seconds)
    {
        if (automated)
        {
            if (autoStack.Count == 0 || autoState.Count == 0)
            {
                automated = false;
            }
            else
            {
                var autoPC = autoStack.Pop();
                var state = autoState.Pop();
                autoDisassembler.State = state;

                var mappedAddress = romData.MapSnesCpuToLorom(autoPC, out var region);
                if (region == RomDataParser.SNESLoRomRegion.ROM)
                {
                    var r = romData.GetRomRanges.GetRangeContainingAddress(mappedAddress);
                    if (r != null && r.Value.GetType() == typeof(CodeRegion))
                    {
                        // Already disassembled
                        return;
                    }
                    if (romData.AddCodeRange(autoDisassembler, autoPC, out var instruction))
                    {
                        if (instruction.Mnemonic == "XCE")
                        {
                            return;
                        }
                        else
                        {
                            foreach (var next in instruction.NextAddresses)
                            {
                                if (!stacked.Contains(next))
                                {
                                    autoStack.Push(next);
                                    autoState.Push(autoDisassembler.State);
                                    stacked.Add(next);
                                }
                            }
                        }
                    }
                    else
                    {
                        automated = false;
                        autoStack.Clear();
                        autoState.Clear();
                        stacked.Clear();
                    }
                }
            }
        }
        if (!romLoaded)
        {
            // Read ROM data in chunks
            romData.Parse(debugger);


            if (File.Exists("TEST.JSON"))
            {
                romData.Load("TEST.JSON");
            }
            else
            {
                romData.AddUnknownRange(RomDataParser.RangeRegion.Cartridge, romData.GetMinAddress, romData.GetMaxAddress);
                romData.AddUnknownRange(RomDataParser.RangeRegion.RAM, 0, 128*1024-1);

                romData.AddStringRange(RomDataParser.RangeRegion.Cartridge, 0x7FC0, 0x7FD4); // LoRom ASCII Title in header
                romData.AddCommentRange(RomDataParser.RangeRegion.Cartridge, ["RetroEditor Resourcer Version 0.1", "", "A WIP Tool for re-sourcing ROMS", "", ""], 0);
                romData.AddCommentRange(RomDataParser.RangeRegion.RAM, ["RetroEditor Resourcer Version 0.1", "", "A WIP Tool for re-sourcing ROMS", "", ""], 0);

                romData.AddSymbol(0x2100, 2, "INIDISP");
                romData.AddSymbol(0x2101, 2, "OBSEL");
                romData.AddSymbol(0x2102, 2, "OAMADDL");
                romData.AddSymbol(0x2103, 2, "OAMADDH");
                romData.AddSymbol(0x2104, 2, "OAMDATA");
                romData.AddSymbol(0x2105, 2, "BGMODE");
                romData.AddSymbol(0x2106, 2, "MOSAIC");
                romData.AddSymbol(0x2107, 2, "BG1SC");
                romData.AddSymbol(0x2108, 2, "BG2SC");
                romData.AddSymbol(0x2109, 2, "BG3SC");
                romData.AddSymbol(0x210A, 2, "BG4SC");
                romData.AddSymbol(0x210B, 2, "BG12NBA");
                romData.AddSymbol(0x210C, 2, "BG34NBA");
                romData.AddSymbol(0x210D, 2, "BG1HOFS");
                romData.AddSymbol(0x210E, 2, "BG1VOFS");
                romData.AddSymbol(0x210F, 2, "BG2HOFS");
                romData.AddSymbol(0x2110, 2, "BG2VOFS");
                romData.AddSymbol(0x2111, 2, "BG3HOFS");
                romData.AddSymbol(0x2112, 2, "BG3VOFS");
                romData.AddSymbol(0x2113, 2, "BG4HOFS");
                romData.AddSymbol(0x2114, 2, "BG4VOFS");
                romData.AddSymbol(0x2115, 2, "VMAIN");
                romData.AddSymbol(0x2116, 2, "VMADDL");
                romData.AddSymbol(0x2117, 2, "VMADDH");
                romData.AddSymbol(0x2118, 2, "VMDATAL");
                romData.AddSymbol(0x2119, 2, "VMDATAH");
                romData.AddSymbol(0x211A, 2, "M7SEL");
                romData.AddSymbol(0x211B, 2, "M7A");
                romData.AddSymbol(0x211C, 2, "M7B");
                romData.AddSymbol(0x211D, 2, "M7C");
                romData.AddSymbol(0x211E, 2, "M7D");
                romData.AddSymbol(0x211F, 2, "M7X");
                romData.AddSymbol(0x2120, 2, "M7Y");
                romData.AddSymbol(0x2121, 2, "CGADD");
                romData.AddSymbol(0x2122, 2, "CGDATA");
                romData.AddSymbol(0x2123, 2, "W12SEL");
                romData.AddSymbol(0x2124, 2, "W34SEL");
                romData.AddSymbol(0x2125, 2, "WOBJSEL");
                romData.AddSymbol(0x2126, 2, "WH0");
                romData.AddSymbol(0x2127, 2, "WH1");
                romData.AddSymbol(0x2128, 2, "WH2");
                romData.AddSymbol(0x2129, 2, "WH3");
                romData.AddSymbol(0x212A, 2, "WBGLOG");
                romData.AddSymbol(0x212B, 2, "WOBJLOG");
                romData.AddSymbol(0x212C, 2, "TM");
                romData.AddSymbol(0x212D, 2, "TD");
                romData.AddSymbol(0x212E, 2, "TMW");
                romData.AddSymbol(0x212F, 2, "TSW");
                romData.AddSymbol(0x2130, 2, "CGWSEL");
                romData.AddSymbol(0x2131, 2, "CGADSUB");
                romData.AddSymbol(0x2132, 2, "COLDATA");
                romData.AddSymbol(0x2133, 2, "SETINI");
                romData.AddSymbol(0x2134, 2, "MPYL");
                romData.AddSymbol(0x2135, 2, "MPYM");
                romData.AddSymbol(0x2136, 2, "MPYH");
                romData.AddSymbol(0x2137, 2, "SLHV");
                romData.AddSymbol(0x2138, 2, "OAMDATAREAD");
                romData.AddSymbol(0x2139, 2, "VMDATAL");
                romData.AddSymbol(0x213A, 2, "VMDATAH");
                romData.AddSymbol(0x213B, 2, "CGDATAREAD");
                romData.AddSymbol(0x213C, 2, "OPHCT");
                romData.AddSymbol(0x213D, 2, "OPVCT");
                romData.AddSymbol(0x213E, 2, "STAT77");
                romData.AddSymbol(0x213F, 2, "STAT78");
                romData.AddSymbol(0x2140, 2, "APUI00");
                romData.AddSymbol(0x2141, 2, "APUI01");
                romData.AddSymbol(0x2142, 2, "APUI02");
                romData.AddSymbol(0x2143, 2, "APUI03");

                romData.AddSymbol(0x2180, 2, "WMDATA");
                romData.AddSymbol(0x2181, 2, "WMADDL");
                romData.AddSymbol(0x2182, 2, "WMADDM");
                romData.AddSymbol(0x2183, 2, "WMADDH");

                romData.AddSymbol(0x4016, 2, "JOYA");
                romData.AddSymbol(0x4017, 2, "JOYB");

                romData.AddSymbol(0x4200, 2, "NMITIMEN");
                romData.AddSymbol(0x4201, 2, "WRIO");
                romData.AddSymbol(0x4202, 2, "WRMPYA");
                romData.AddSymbol(0x4203, 2, "WRMPYB");
                romData.AddSymbol(0x4204, 2, "WRDIVLH");
                romData.AddSymbol(0x4205, 2, "WRDIVB");
                romData.AddSymbol(0x4207, 2, "HTIMELH");
                romData.AddSymbol(0x4209, 2, "VTIMELH");
                romData.AddSymbol(0x420B, 2, "MDMAEN");
                romData.AddSymbol(0x420C, 2, "HDMAEN");
                romData.AddSymbol(0x420D, 2, "MEMSEL");

                romData.AddSymbol(0x4210, 2, "RDNMI");
                romData.AddSymbol(0x4211, 2, "TIMEUP");
                romData.AddSymbol(0x4212, 2, "RDIO");
                romData.AddSymbol(0x4213, 2, "RDDIVL");
                romData.AddSymbol(0x4214, 2, "RDDIVH");
                romData.AddSymbol(0x4215, 2, "RDMPYL");
                romData.AddSymbol(0x4216, 2, "RDMPYH");

                romData.AddSymbol(0x4218, 2, "JOY1L");
                romData.AddSymbol(0x4219, 2, "JOY1H");
                romData.AddSymbol(0x421A, 2, "JOY2L");
                romData.AddSymbol(0x421B, 2, "JOY2H");
                romData.AddSymbol(0x421C, 2, "JOY3L");
                romData.AddSymbol(0x421D, 2, "JOY3H");
                romData.AddSymbol(0x421E, 2, "JOY4L");
                romData.AddSymbol(0x421F, 2, "JOY4H");

                for (int a = 0; a < 8; a++)
                {
                    romData.AddSymbol(0x4300 + (UInt64)(0x10 * a), 2, $"DMAP{a}");
                    romData.AddSymbol(0x4301 + (UInt64)(0x10 * a), 2, $"BBAD{a}");
                    romData.AddSymbol(0x4302 + (UInt64)(0x10 * a), 2, $"A1T{a}L");
                    romData.AddSymbol(0x4303 + (UInt64)(0x10 * a), 2, $"A1T{a}H");
                    romData.AddSymbol(0x4304 + (UInt64)(0x10 * a), 2, $"A1B{a}");
                    romData.AddSymbol(0x4305 + (UInt64)(0x10 * a), 2, $"DAS{a}L");
                    romData.AddSymbol(0x4306 + (UInt64)(0x10 * a), 2, $"DAS{a}H");
                    romData.AddSymbol(0x430A + (UInt64)(0x10 * a), 2, $"NTRL{a}");
                }
            }

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
                state.SetEmulationMode(e == 1);
                state.Accumulator8Bit = (p & 0x20) == 0x20;
                state.Index8Bit = (p & 0x10) == 0x10;
                disassembler.State = state;
                romData.AddCodeRange(disassembler, pc, out var _);
                cartridgeVars.jumpToAddress = romData.MapSnesCpuToLorom(pc, out var _);
                newTraceCnt--;
                if (newTraceCnt == 0)
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
                traceCommandFinishStarted = true;
                traceCommandFinished = false;
                debugger.QueueCommand("traceflush", (s, id) => { });
                debugger.QueueCommand("trace off", (s, id) => { traceCommandFinished = true; });
            }
            if (debugger.IsStopped && traceCommandFinished)
            {
                // Read and parse trace file
                if (File.Exists(traceFile))
                {
                    // Parse disassembly
                    var lines = File.ReadAllLines(traceFile);
                    int lOffset = 0;
                    foreach (var line in lines)
                    {
                        ParseLocation(line);
                        lOffset++;
                    }
                }
                if (traceContinue)
                {
                    if (File.Exists("trace.log"))
                    {
                        File.Delete("trace.log");
                    }
                    traceCommandInProgress = true;
                    traceCommandFinished = false;
                    traceCommandFinishStarted = false;

                    // Set up trace logging
                    debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {TRACEREGS}}}", (s, id) => { });
                    debugger.QueueCommand("gtime 500", (s, id) => { traceCommandInProgress = false; });
                }
                else
                {
                    traceInProgress = false;
                    // After trace, perform a walk of any blocks that end in non terminating branches
/*
                    foreach (var b in romData.GetRomRanges)
                    {
                        if (b.Value is CodeRegion codeRegion)
                        {
                            var lastLine = codeRegion.LineCount - 1;
                            var lastInstruction = codeRegion.GetInstructionForLine(lastLine);
                            if (lastInstruction.IsBranch && !lastInstruction.IsBasicBlockTerminator)
                            {
                                // Conditional instruction
                                var next = lastInstruction.Address + (UInt64)lastInstruction.Bytes.Length;
                                if (!stacked.Contains(next))
                                {
                                    autoStack.Push(next);
                                    autoState.Push(lastInstruction.cpuState);
                                    stacked.Add(next);
                                    automated = true;
                                }
                            }
                        }
                    }*/
                }
            }
        }
    }

    readonly string TRACEREGS = "\"E=%02X|P=%02X|DB=%02X|D=%04X|X=%04X|Y=%04X|S=%04X|\",e,p,db,d,x,y,s";

    private void ParseLocation(string line)
    {
        // Format: TRACEREGS E=00|P=00|DB=00|D=0000|X=0000|Y=0000|S=0000|BANK:OFFSET: mnemonic operands

        // Split the line into parts
        // Ignore empty lines or lines that don't contain the expected format
        if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
            return;

        var parts = line.Split('|');
        if (parts.Length < 7)
            return;

        // Parse emulation mode (E)
        if (!parts[0].StartsWith("E=") || !byte.TryParse(parts[0].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out byte e))
            return;

        // Parse processor status (P)
        if (!parts[1].StartsWith("P=") || !byte.TryParse(parts[1].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out byte p))
            return;

        // Parse data bank (DB)
        if (!parts[2].StartsWith("DB=") || !byte.TryParse(parts[2].Substring(3), System.Globalization.NumberStyles.HexNumber, null, out byte db))
            return;

        // Parse direct offset (D)
        if (!parts[3].StartsWith("D=") || !ushort.TryParse(parts[3].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out ushort d))
            return;

        // Parse index register (X)
        if (!parts[4].StartsWith("X=") || !ushort.TryParse(parts[4].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out ushort x))
            return;

        // Parse index register (Y)
        if (!parts[5].StartsWith("Y=") || !ushort.TryParse(parts[5].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out ushort y))
            return;

        // Parse stack pointer (S)
        if (!parts[6].StartsWith("S=") || !ushort.TryParse(parts[6].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out ushort s))
            return;

        // Parse bank and offset
        var addressPart = parts[7].Split(' ')[0];
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
        romData.AddCodeRange(disassembler, snesAddress, out var i);
        if (i.Bytes.Length == 0)
        {
            // No bytes, so no code
            return;
        }
        if (i.IsBranch)
        {
            return;
        }

        SNES65816RegisterState registerState = new SNES65816RegisterState
        {
            DBR = db,
            D = d,
            X = x,
            Y = y,
            S = s
        };

        var mem = disassembler.FetchMemoryAccesses(i, registerState);
        foreach (var addr in mem)
        {
            var regionAddress = romData.MapSnesCpuToLorom(addr.address, out var memKind);

            if (memKind == RomDataParser.SNESLoRomRegion.ROM)
            {
                if (romData.CheckRegionUnknown(regionAddress,regionAddress+addr.size-1))
                {
                    romData.AddDataRange(RomDataParser.RangeRegion.Cartridge, regionAddress, regionAddress + addr.size-1, addr.size);
                }
                else
                {
                    Console.WriteLine($"Skipping {addr.address:X8} ({regionAddress:X8}) {addr.size} as it is not unknown");
                }
            }
            if (memKind == RomDataParser.SNESLoRomRegion.RAM)
            {
                if (romData.CheckRegionUnknown(regionAddress,regionAddress+addr.size-1))
                {
                    romData.AddDataRange(RomDataParser.RangeRegion.RAM, regionAddress, regionAddress + addr.size-1, addr.size);
                }
                else
                {
                    Console.WriteLine($"Skipping {addr.address:X8} ({regionAddress:X8}) {addr.size} as it is not unknown");
                }
            }
        }
    }
}