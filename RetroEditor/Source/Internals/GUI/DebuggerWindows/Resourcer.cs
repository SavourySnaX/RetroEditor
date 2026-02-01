using MyMGui;
using System.Numerics;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;
using RetroEditor.Source.Internals.ReverseEngineering;

internal class Resourcer : IWindow
{
    public float UpdateInterval => 1 / 60.0f;
    public bool MinimumSize => false;
    
    // Public properties for external access (e.g., SymbolsWindow)
    public IReadOnlyDictionary<MemoryRegionKey, RomDataParser> RomDataParsers => romDataParsers;
    public IMemoryInformationProvider MemoryInformationProvider => memoryInformationProvider;
    
    LibMameDebugger debugger;
    Dictionary<MemoryRegionKey, RomDataParser> romDataParsers;
    Dictionary<MemoryRegionKey, IMemoryInformation> memoryRegionInfoCache;
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
    private MemoryRegionKey regionToSwitch=new MemoryRegionKey(0);
    private bool pendingTabSwitch = false; // Flag to activate tab switch only once

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

        romDataParsers = new Dictionary<MemoryRegionKey, RomDataParser>();
        memoryRegionInfoCache = new Dictionary<MemoryRegionKey, IMemoryInformation>();
        InitializeRegionParsers();

        config = new ResourcerConfig();
    }

    private void InitializeRegionParsers()
    {
        // Create a RomDataParser for each memory region
        foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
        {
            memoryRegionInfoCache[memInfo.RegionKey] = memInfo;
            var dataProvider = memInfo.CreateDataProvider();
            var parser = new RomDataParser(memInfo, dataProvider);
            romDataParsers[memInfo.RegionKey] = parser;
        }
        // Initialize active region to the first one
    }

    private IEnumerable<string> GetMemoryRegionNames()
    {
        foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
        {
            yield return memInfo.DisplayName;
        }
    }

    private IEnumerable<(MemoryRegionKey RegionKey, RangeCollection<IRegionInfo> Ranges)> GetAllMemoryRegions()
    {
        foreach (var (regionKey, parser) in romDataParsers)
        {
            yield return (regionKey, parser.GetRanges());
        }
    }

    /// <summary>
    /// Navigates to the specified address in the Resourcer view.
    /// </summary>
    public void NavigateToAddress(MemoryRegionKey regionKey, ulong address)
    {
        // Set the active region to switch to the correct tab
        regionToSwitch = regionKey;
        pendingTabSwitch = true; // Mark that we need to switch tabs
        
        if (scrollViewVars.TryGetValue(regionKey, out var vars))
        {
            vars.jumpToAddress = address;
            vars.jumpPending = true; // Mark that we need to jump/scroll
            // Also set the cursor position to the address for immediate visibility
            var parser = romDataParsers[regionKey];
            var ranges = parser.GetRanges();
            var line = ranges.FetchLineForAddress(address);
            vars.selectedRows.Clear();
            vars.selectedRows.Add(line);
            vars.cursorPosition = line;
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
            // If we're pending a tab switch, render the active region's tab first
            var regionsToRender = memoryInformationProvider.GetMemoryRegions().ToList();
            foreach (var memInfo in regionsToRender)
            {
                var regionKey = memInfo.RegionKey;
                var displayName = memInfo.DisplayName;
                
                // Only use SetSelected flag once when navigating
                bool isActive = (regionKey.Key == regionToSwitch.Key);
                ImGuiTabItemFlags flags = (isActive && pendingTabSwitch) ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(displayName, flags))
                {
                    // User switched tabs manually or via navigation
                    if (flags == ImGuiTabItemFlags.SetSelected)
                    {
                        pendingTabSwitch = false; // Consume the flag only when correct tab is active
                    }

                    var ranges = romDataParsers[regionKey].GetRanges();
                    ScrollableTableView(ranges, regionKey, GetOrCreateScrollViewVars(regionKey));
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
        public bool jumpPending = false; // Flag to trigger navigation-based jump
        // Selection state
        public HashSet<UInt64> selectedRows = new HashSet<UInt64>();
        public UInt64? cursorPosition = null;
        public UInt64? selectionStart = null;
        
        // Rename state
        public bool showRenameDialog = false;
        public string renameBuffer = "";
        public bool isRenamingLabel = false;
        public ulong renameAddress = 0;
        public string renameLabelOldName = "";
        public int renameSymbolSize = 0;
    }

    Dictionary<MemoryRegionKey, ScrollViewVars> scrollViewVars = new();

    private ScrollViewVars GetOrCreateScrollViewVars(MemoryRegionKey regionKey)
    {
        if (!scrollViewVars.ContainsKey(regionKey))
        {
            scrollViewVars[regionKey] = new ScrollViewVars();
        }
        return scrollViewVars[regionKey];
    }

    private void ScrollableTableView(RangeCollection<IRegionInfo> regions, MemoryRegionKey regionKey, ScrollViewVars vars)
    {
        var jump = false;
        if (InputU64ScalarWrapped("Jump to Address", ref vars.jumpToAddress))
        {
            jump = true;
        }
        
        // Also trigger jump if pending from navigation
        if (vars.jumpPending)
        {
            jump = true;
            vars.jumpPending = false; // Consume the flag
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
                        var endAddress = regions.FetchAddressForLine(srow + 1);
                        if (address < minAddress)
                            minAddress = address;
                        if (endAddress > 0)
                            endAddress--;
                        if (endAddress > maxAddress)
                            maxAddress = endAddress;
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
                            romDataParsers[regionKey].AddCommentRange(["I AM THE VERY MODEL OF A MODERN MAJOR GENERAL", "I'VE INFORMATION ANIMAL VEGETABLE AND MINERAL", "....."], cursorMinAddress);
                        }
                    }

                    bool clearSelection = false;
                    
                    // Get memory region info for this region from cache
                    memoryRegionInfoCache.TryGetValue(regionKey, out var regionInfo);
                    
                    if (ImGui.IsKeyPressed(ImGuiKey.S))
                    {
                        romDataParsers[regionKey].AddStringRange(minAddress, maxAddress, regionInfo?.HasPhysicalData ?? true);
                        clearSelection = true;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.U))
                    {
                        romDataParsers[regionKey].AddUnknownRange(minAddress, maxAddress, regionInfo?.HasPhysicalData ?? true);
                        clearSelection = true;
                    }
                    
                    // Code and data operations only apply to regions with physical data (typically Cartridge/ROM)
                    if (regionInfo != null && regionInfo.HasPhysicalData)
                    {
                        if (ImGui.IsKeyPressed(ImGuiKey.C))
                        {
                            // Convert to code
                            var cpuState = cpuStateManager.FetchStateFromUI();
                            disassembler.State = cpuState;
                            romDataParsers[regionKey].AddCodeRange((DisassemblerBase)disassembler, minAddress, maxAddress, memoryMapper);
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
                            romDataParsers[regionKey].AddDataRange(minAddress, minAddress+1, 1);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._2) && !automated)
                        {
                            // Add data region of 2-byte words
                            romDataParsers[regionKey].AddDataRange(minAddress, minAddress+1, 2);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._3) && !automated)
                        {
                            // Add data region of 3-byte words
                            romDataParsers[regionKey].AddDataRange(minAddress, minAddress+2, 3);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey._4) && !automated)
                        {
                            // Add data region of 4-byte words
                            romDataParsers[regionKey].AddDataRange(minAddress, minAddress+3, 4);
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
                            var colour = lData.IsLabel ? ResourcerConfig.ConfigColour.Label : fetched.Value.Colour;
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, config.GetColorU32(colour));
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

                        // Right-click context menu
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            if (lData.IsLabel)
                            {
                                // This is a label line - extract label name from address field
                                var labelName = lData.Address.TrimEnd(':');
                                var address = fetched.Value.RegionAddressForLine(line);
                                
                                vars.isRenamingLabel = true;
                                vars.renameAddress = address;
                                vars.renameLabelOldName = labelName;
                                vars.renameBuffer = labelName;
                                ImGui.OpenPopup($"RowMenu{actualLine}");
                            }
                            else
                            {
                                var address = fetched.Value.RegionAddressForLine(line);
                                vars.renameAddress = address;
                                vars.isRenamingLabel = false;
                                ImGui.OpenPopup($"RowMenu{actualLine}");
                            }
                        }
                        
                        if (ImGui.BeginPopup($"RowMenu{actualLine}"))
                        {
                            if (lData.IsLabel)
                            {
                                // This is a label line
                                if (ImGui.MenuItem("Rename Label"))
                                {
                                    vars.showRenameDialog = true;
                                }
                                
                                if (ImGui.MenuItem("Delete Label"))
                                {
                                    var labelName = lData.Address.TrimEnd(':');
                                    var address = fetched.Value.RegionAddressForLine(line);
                                    romDataParsers[regionKey].RemoveLabel(address, labelName);
                                }
                            }
                            else
                            {
                                // Regular instruction line
                                if (ImGui.MenuItem("Add Label"))
                                {
                                    var address = fetched.Value.RegionAddressForLine(line);
                                    romDataParsers[regionKey].AddLabel(address, $"label_{address:X8}");
                                }
                            }
                            
                            ImGui.EndPopup();
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
                // Scroll to show a few lines before the target for context (similar to cursor positioning)
                var jumpVisibleLines = (uint)(contentSize.Y / rowHeight);
                var scrollOffset = Math.Min(jumpLine, jumpVisibleLines / 3); // Show target ~1/3 down the view
                ImGui.SetScrollY((jumpLine - scrollOffset) * rowHeight);
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
        
        // Rename dialog popup
        if (vars.showRenameDialog)
        {
            ImGui.OpenPopup("Rename##Resourcer");
        }

        if (ImGui.BeginPopupModal("Rename##Resourcer", ref vars.showRenameDialog, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (vars.isRenamingLabel)
            {
                ImGui.Text($"Rename label at 0x{vars.renameAddress:X8}");
            }

            ImGui.Separator();

            ImGui.Text("New name:");
            bool enterPressed = ImGui.InputText("##NewName", ref vars.renameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue);

            ImGui.Separator();

            if (ImGui.Button("OK", new Vector2(120, 0)) || enterPressed)
            {
                // Validate the new name
                string? validationError = SymbolValidator.ValidateName(vars.renameBuffer);
                
                if (validationError != null)
                {
                    ImGui.OpenPopup("Error##Resourcer");
                }
                else
                {
                    // Check for conflicts
                    string? conflictError = SymbolValidator.CheckAnyConflict(vars.renameBuffer, romDataParsers, vars.renameAddress);
                    
                    if (conflictError == null)
                    {
                        if (vars.isRenamingLabel)
                        {
                            romDataParsers[regionKey].RenameLabel(vars.renameAddress, vars.renameLabelOldName, vars.renameBuffer);
                        }
                        vars.showRenameDialog = false;
                    }
                    else
                    {
                        validationError = conflictError;
                        ImGui.OpenPopup("Error##Resourcer");
                    }
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                vars.showRenameDialog = false;
            }

            // Error popup
            bool errorOpen = true;
            if (ImGui.BeginPopupModal("Error##Resourcer", ref errorOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                string? validationError = SymbolValidator.ValidateName(vars.renameBuffer);
                
                if (validationError == null)
                {
                    validationError = SymbolValidator.CheckAnyConflict(vars.renameBuffer, romDataParsers, vars.renameAddress);
                }

                ImGui.Text(validationError ?? "Unknown error");
                ImGui.Separator();
                
                if (ImGui.Button("OK", new Vector2(120, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.EndPopup();
            }

            ImGui.EndPopup();
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
                var ranges = romDataParsers[region].GetRanges();
                var r = ranges.GetRangeContainingAddress(mappedAddress);
                if (r != null && r.Value.GetType() == typeof(CodeRegion))
                {
                    // Already disassembled
                    return;
                }
                if (romDataParsers[region].AddCodeRange((DisassemblerBase)autoDisassembler, autoPC, out var instruction, memoryMapper))
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
                foreach (var regionKey in romDataParsers.Keys)
                {
                    romDataParsers[regionKey].AddCommentRange(["RetroEditor Resourcer Version 0.1", "", "A WIP Tool for re-sourcing ROMS", "", ""], 0);
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
                //var cartridgeRegionName = GetMemoryRegionNames().FirstOrDefault();
                //if (!string.IsNullOrEmpty(cartridgeRegionName))
                foreach (var regionKey in romDataParsers.Keys)
                {
                    //TODO move hardwareRegisterProvider to memory information ?
                    hardwareRegisterProvider.InitializeSymbols(romDataParsers[regionKey], regionKey);
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

    private void ParseTraceEntry(TraceEntry entry)
    {
        // Create a disassembler with the current CPU state
        disassembler.State = entry.CpuState;

        // Add this location as code
        memoryMapper.MapCpuToRegion(entry.Address, out var regionKey);
        romDataParsers[regionKey].AddCodeRange((DisassemblerBase)disassembler, entry.Address, out var i, memoryMapper);
        if (i.Bytes.Length == 0)
        {
            // No bytes, so no code
            return;
        }
        if (i.IsBranch)
        {
            // Fetch the target address and construct a label there
            foreach (var target in i.NextAddresses)
            {
                var mappedTarget = memoryMapper.MapCpuToRegion(target, out var targetRegionKey);
                romDataParsers[targetRegionKey].AddLabel(mappedTarget,$"loc_{mappedTarget:X8}");
            }
            return;
        }

        var accesses = ((DisassemblerBase)disassembler).FetchMappedAccesses(i, entry.RegisterState, memoryMapper);
        foreach (var access in accesses)
        {
            if (access.RegionKey.Key == (uint)MemoryInformationRegion.Invalid)
            {
                continue;
            }

            if (romDataParsers.ContainsKey(access.RegionKey))
            {
                var regionAddress = access.Address;
                var endAddress = regionAddress + access.Size - 1;
                if (romDataParsers[access.RegionKey].CheckRegionUnknown(regionAddress, endAddress))
                {
                    memoryRegionInfoCache.TryGetValue(access.RegionKey, out var accessRegionInfo);
                    romDataParsers[access.RegionKey].AddDataRange(regionAddress, endAddress, access.Size, accessRegionInfo?.HasPhysicalData ?? true);
                }
            }
            else
            {
                Console.WriteLine($"No parser for region {access.RegionKey}");
            }
        }
    }
}