using MyMGui;
using System.Numerics;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class Resourcer : IWindow
{
    public float UpdateInterval => 1 / 60.0f;
    public bool MinimumSize => false;
    
    LibMameDebugger debugger;
    Dictionary<string, RomDataParser> romDataParsers;
    IPlatformFactory platformFactory;
    IDisassembler disassembler;
    IMemoryMapper memoryMapper;
    ICpuStateManager cpuStateManager;
    ITraceParser traceParser;
    IMemoryInformationProvider memoryInformationProvider;
    IHardwareSymbolsProvider hardwareRegisterProvider;
    string traceFile = "trace.txt";
    bool traceInProgress = false;
    bool romLoaded = false;

    // Memory map data
    private const int MEMORY_MAP_HEIGHT = 50;

    private ResourcerConfig config;

    public Resourcer(LibMameDebugger debugger, IPlatformFactory platformFactory)
    {
        this.debugger = debugger;
        this.platformFactory = platformFactory;
        this.disassembler = this.platformFactory.CreateDisassembler();
        this.memoryMapper = this.platformFactory.CreateMemoryMapper();
        this.cpuStateManager = this.platformFactory.CreateCpuStateManager();
        this.traceParser = this.platformFactory.CreateTraceParser();
        this.hardwareRegisterProvider = this.platformFactory.CreateHardwareSymbolsProvider();
        this.memoryInformationProvider = this.platformFactory.CreateMemoryInformationProvider();
        this.autoDisassembler = this.platformFactory.CreateDisassembler();

        romDataParsers = new Dictionary<string, RomDataParser>();
        InitializeRegionParsers();

        config = new ResourcerConfig();
    }

    private void InitializeRegionParsers()
    {
        // Create a RomDataParser for each memory region
        foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
        {
            var dataProvider = memInfo.MameViewName switch
            {
                "Cart" => new DebuggerDataProvider(memInfo.MameViewName, memInfo.MameViewName) as IMemoryRegionDataProvider,
                "Work RAM" => new VirtualDataProvider(memInfo.MameViewName, memInfo.AddressRange.End - memInfo.AddressRange.Start) as IMemoryRegionDataProvider,
                _ => new DebuggerDataProvider(memInfo.MameViewName, memInfo.MameViewName) as IMemoryRegionDataProvider
            };
            
            var parser = new RomDataParser(memInfo.MameViewName, memInfo, dataProvider);
            romDataParsers[memInfo.MameViewName] = parser;
        }
    }

    private IEnumerable<string> GetMemoryRegionNames() => romDataParsers.Keys;

    private IEnumerable<(string RegionName, RangeCollection<IRegionInfo> Ranges)> GetAllMemoryRegions()
    {
        foreach (var (regionName, parser) in romDataParsers)
        {
            yield return (regionName, parser.GetRanges());
        }
    }

    private UInt64 GetMinAddress
    {
        get
        {
            if (romDataParsers.Count == 0) return 0;
            return romDataParsers.Values.Min(p => p.GetMinAddress);
        }
    }

    private UInt64 GetMaxAddress
    {
        get
        {
            if (romDataParsers.Count == 0) return 0;
            return romDataParsers.Values.Max(p => p.GetMaxAddress);
        }
    }

    public void Close()
    {
        // Save all regions
        foreach (var (regionName, parser) in romDataParsers)
        {
            parser.Save($"TEST_{parser.MemoryInformation.DisplayName}.JSON");
        }
    }

    private void DrawMemoryMap()
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();
        size.Y = MEMORY_MAP_HEIGHT;

        // Compute the size of the memory map
        var minAddress = GetMinAddress;
        var maxAddress = GetMaxAddress;
        var range = maxAddress - minAddress;
        var scale = size.X / range;

        // Draw all regions from all memory regions
        foreach (var (regionName, ranges) in GetAllMemoryRegions())
        {
            foreach (var region in ranges)
            {
                drawList.AddRectFilled(
                    new Vector2(pos.X + region.Value.AddressStart * scale, pos.Y),
                    new Vector2(pos.X + region.Value.AddressEnd * scale, pos.Y + size.Y),
                    config.GetColorU32(region.Value.Colour)
                );
            }
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + MEMORY_MAP_HEIGHT);
        ImGui.Dummy();
    }

    bool traceCommandInProgress=false;
    bool traceCommandFinishStarted=false;
    bool traceCommandFinished=false;
    bool traceContinue = false;

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
            if (File.Exists("trace.log"))
            {
                File.Delete("trace.log");
            }
            traceCommandInProgress=true;
            traceCommandFinished=false;
            traceCommandFinishStarted=false;
            // Set up trace logging
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {cpuStateManager.GetTraceFormat()}}}", (s,id)=>{});
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
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {cpuStateManager.GetTraceFormat()}}}",(s,id)=>{});
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
            debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {cpuStateManager.GetTraceFormat()}}}",(s,id)=>{});
            debugger.QueueCommand("gtime 500",(s,id)=>{traceCommandInProgress=false;});
        }
        ImGui.SameLine();
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
        cpuStateManager.RenderUI();
        ImGui.SameLine();
        ImGui.BeginDisabled();
        ImGui.Checkbox("Automated", ref automated);
        ImGui.EndDisabled();

        if (ImGui.BeginTabBar("ResourcerTabs"))
        {
            foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
            {
                var regionName = memInfo.MameViewName;
                var displayName = memInfo.DisplayName;
                
                if (ImGui.BeginTabItem(displayName))
                {
                    var ranges = romDataParsers[regionName].GetRanges();
                    ScrollableTableView(ranges, regionName, GetOrCreateScrollViewVars(regionName));
                    ImGui.EndTabItem();
                }
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

    Dictionary<string, ScrollViewVars> scrollViewVars = new();

    private ScrollViewVars GetOrCreateScrollViewVars(string regionName)
    {
        if (!scrollViewVars.ContainsKey(regionName))
        {
            scrollViewVars[regionName] = new ScrollViewVars();
        }
        return scrollViewVars[regionName];
    }

    private void ScrollableTableView(RangeCollection<IRegionInfo> regions, string regionName, ScrollViewVars vars)
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
                    if (vars.cursorPosition.HasValue && vars.cursorPosition.Value < regions.LineCount - 1)
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
                        var newPosition = (UInt64)Math.Min(regions.LineCount - 1, vars.cursorPosition.Value + (UInt64)visibleLines);
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
                    vars.cursorPosition = regions.LineCount - 1;
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
                            romDataParsers[regionName].AddCommentRange(["I AM THE VERY MODEL OF A MODERN MAJOR GENERAL", "I'VE INFORMATION ANIMAL VEGETABLE AND MINERAL", "....."], cursorMinAddress);
                        }
                    }

                    bool clearSelection = false;
                    if (ImGui.IsKeyPressed(ImGuiKey.S))
                    {
                        romDataParsers[regionName].AddStringRange(minAddress, maxAddress);
                        clearSelection = true;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.U))
                    {
                        romDataParsers[regionName].AddUnknownRange(minAddress, maxAddress);
                        clearSelection = true;
                    }
                    
                    // Get memory region info for this region to check if code operations are allowed
                    var regionInfo = memoryInformationProvider.GetMemoryRegions().FirstOrDefault(r => r.MameViewName == regionName);
                    
                    // Code and data operations only apply to regions with physical data (typically Cartridge/ROM)
                    if (regionInfo != null && regionInfo.HasPhysicalData)
                    {
                        if (ImGui.IsKeyPressed(ImGuiKey.C))
                        {
                            // Convert to code
                            var cpuState = cpuStateManager.FetchStateFromUI();
                            disassembler.State = cpuState;
                            romDataParsers[regionName].AddCodeRange((DisassemblerBase)disassembler, minAddress, maxAddress, memoryMapper);
                            // Update CPU flags from resulting state
                            cpuStateManager.UpdateUIFromState(disassembler.State);
                            clearSelection = true;
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey.A) && !automated)
                        {
                            // Auto disassemble starting at the first selected address
                            var cpuState = cpuStateManager.FetchStateFromUI();
                            autoDisassembler.State = cpuState;
                            var autoPC = memoryMapper.MapRomToCpu(minAddress);
                            autoStack.Clear();
                            autoState.Clear();
                            stacked.Clear();
                            autoStack.Push(autoPC);
                            autoState.Push(autoDisassembler.State);
                            automated = true;
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._1) && !automated)
                        {
                            // Add data region of 1-byte 
                            romDataParsers[regionName].AddDataRange(minAddress, minAddress+1, 1);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._2) && !automated)
                        {
                            // Add data region of 2-byte words
                            romDataParsers[regionName].AddDataRange(minAddress, minAddress+1, 2);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._3) && !automated)
                        {
                            // Add data region of 3-byte words
                            romDataParsers[regionName].AddDataRange(minAddress, minAddress+2, 3);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._4) && !automated)
                        {
                            // Add data region of 4-byte words
                            romDataParsers[regionName].AddDataRange(minAddress, minAddress+3, 4);
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
                var jumpLine = regions.FetchLineForAddress(vars.jumpToAddress);
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
    IDisassembler autoDisassembler;
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

                var mappedAddress = memoryMapper.MapCpuToRegion(autoPC, out var region);
                //if (region == RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryRegion.ROM)
                {
                    var codeRegionName = GetRegionNameForMemoryType(RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryInformationRegion.ROM);
                    if (!string.IsNullOrEmpty(codeRegionName))
                    {
                        var ranges = romDataParsers[codeRegionName].GetRanges();
                        var r = ranges.GetRangeContainingAddress(mappedAddress);
                        if (r != null && r.Value.GetType() == typeof(CodeRegion))
                        {
                            // Already disassembled
                            return;
                        }
                        if (romDataParsers[codeRegionName].AddCodeRange((DisassemblerBase)autoDisassembler, autoPC, out var instruction, memoryMapper))
                        {
                            if (cpuStateManager.InstructionTerminatesAutoDisassembly(instruction))
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
            foreach (var (regionName, parser) in romDataParsers)
            {
                parser.Parse(debugger);
            }

            if (File.Exists("TEST.JSON"))
            {
                foreach (var (regionName, parser) in romDataParsers)
                {
                    parser.Load($"TEST_{parser.MemoryInformation.DisplayName}.JSON");
                }
            }
            else
            {
                var regionNames = GetMemoryRegionNames();
                foreach (var regionName in regionNames)
                {
                    romDataParsers[regionName].AddCommentRange(["RetroEditor Resourcer Version 0.1", "", "A WIP Tool for re-sourcing ROMS", "", ""], 0);
                }
                foreach (var region in romDataParsers.Values)
                {
                    if (region.MemoryInformation.HasPhysicalData)
                    {
                        // For ROM regions, add unknown range for entire region
                        var minAddr = region.GetMinAddress;
                        var maxAddr = region.GetMaxAddress;
                        region.AddUnknownRange(minAddr, maxAddr);
                    }
                    else
                    {
                        // For non-physical regions (RAM), add unknown range for first 128KB
                        region.AddUnknownRange(0, 128 * 1024 - 1, false); // TODO platform specific
                    }
                }

                // Initialize platform-specific hardware registers
                // Pass the first parser (typically ROM/Cartridge) which contains hardware register symbols
                var cartridgeRegionName = GetMemoryRegionNames().FirstOrDefault();
                if (!string.IsNullOrEmpty(cartridgeRegionName))
                {
                    hardwareRegisterProvider.InitializeSymbols(romDataParsers[cartridgeRegionName]);
                }
            }

            romLoaded = true;
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
                        if (traceParser.TryParseTraceLine(line, out var traceEntry) && traceEntry.IsValid)
                        {
                            ParseTraceEntry(traceEntry);
                        }
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
                    debugger.QueueCommand($"trace {traceFile},,noloop,{{tracelog {cpuStateManager.GetTraceFormat()}}}", (s, id) => { });
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

    private string GetRegionNameForMemoryType(RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryInformationRegion memoryRegion)
    {
        // Map platform MemoryRegion enum to actual region names using property-based lookup
        var regions = memoryInformationProvider.GetMemoryRegions().ToList();
        
        switch (memoryRegion)
        {
            case RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryInformationRegion.ROM:
                // Find first region with physical data (typically ROM/Cartridge)
                return regions.FirstOrDefault(r => r.HasPhysicalData)?.MameViewName ?? regions.FirstOrDefault()?.MameViewName ?? string.Empty;
            case RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryInformationRegion.RAM:
                // Find first region without physical data (typically RAM/WRAM)
                return regions.FirstOrDefault(r => !r.HasPhysicalData)?.MameViewName ?? string.Empty;
            default:
                return string.Empty;
        }
    }

    private void ParseTraceEntry(TraceEntry entry)
    {
        // Create a disassembler with the current CPU state
        disassembler.State = entry.CpuState;

        // Add this location as code
        var codeRegionName = GetRegionNameForMemoryType(RetroEditor.Source.Internals.ReverseEngineering.Platform.MemoryInformationRegion.ROM);
        if (!string.IsNullOrEmpty(codeRegionName))
        {
            romDataParsers[codeRegionName].AddCodeRange((DisassemblerBase)disassembler, entry.Address, out var i, memoryMapper);
            if (i.Bytes.Length == 0)
            {
                // No bytes, so no code
                return;
            }
            if (i.IsBranch)
            {
                return;
            }

            var mem = ((DisassemblerBase)disassembler).FetchMemoryAccesses(i, entry.RegisterState);
            foreach (var addr in mem)
            {
                var regionAddress = memoryMapper.MapCpuToRegion(addr.address, out var memKind);
                var regionName = GetRegionNameForMemoryType(memKind);
                
                if (!string.IsNullOrEmpty(regionName))
                {
                    if (romDataParsers[regionName].CheckRegionUnknown(regionAddress, regionAddress + addr.size - 1))
                    {
                        romDataParsers[regionName].AddDataRange(regionAddress, regionAddress + addr.size - 1, addr.size);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {addr.address:X8} ({regionAddress:X8}) {addr.size} as it is not unknown");
                    }
                }
            }
        }
    }
}