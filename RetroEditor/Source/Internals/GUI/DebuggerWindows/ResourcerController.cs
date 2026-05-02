using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using RetroEditor.Source.Internals.ReverseEngineering;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.GUI.DebuggerWindows;

/// <summary>
/// Controller for Resourcer - handles all business logic and actions
/// </summary>
internal class ResourcerController : IResourcerViewCallbacks
{
    // Public properties for external access
    public IReadOnlyDictionary<MemoryRegionKey, RomDataParser> RomDataParsers => romDataParsers;
    public IMemoryInformationProvider MemoryInformationProvider => memoryInformationProvider;
    public bool IsTraceInProgress => traceInProgress;
    
    // Dependencies
    private readonly LibMameDebugger debugger;
    private readonly Dictionary<MemoryRegionKey, RomDataParser> romDataParsers;
    private readonly Dictionary<MemoryRegionKey, IMemoryInformation> memoryRegionInfoCache;
    private readonly IPlatformFactory platformFactory;
    private readonly IDisassembler disassembler;
    private readonly IMemoryMapper memoryMapper;
    private readonly ICpuStateManager cpuStateManager;
    private readonly ITraceParser traceParser;
    private readonly IMemoryInformationProvider memoryInformationProvider;
    private readonly IHardwareSymbolsProvider hardwareRegisterProvider;
    private readonly IDisassembler autoDisassembler;
    
    // State
    private readonly string systemName;
    private readonly string mediaName;
    private readonly string traceFile = "trace.txt";
    private bool traceInProgress = false;
    private bool romLoaded = false;
    private bool traceCommandInProgress = false;
    private bool traceCommandFinishStarted = false;
    private bool traceCommandFinished = false;
    private bool traceContinue = false;
    
    // Auto-disassembly state
    private bool automated = false;
    private Stack<ulong> autoStack = new();
    private Stack<ICpuState> autoState = new();
    private HashSet<ulong> stacked = new();
    
    // Navigation state
    private MemoryRegionKey regionToSwitch = new MemoryRegionKey(0);
    private bool pendingTabSwitch = false;
    
    // Scroll view state per region
    internal Dictionary<MemoryRegionKey, ScrollViewState> ScrollViewStates { get; } = new();

    public ResourcerController(
        string systemName, 
        string mediaName, 
        LibMameDebugger debugger, 
        IPlatformFactory platformFactory)
    {
        this.debugger = debugger;
        this.platformFactory = platformFactory;
        this.systemName = systemName;
        this.mediaName = mediaName;
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
    }

    internal ScrollViewState GetOrCreateScrollViewState(MemoryRegionKey regionKey)
    {
        if (!ScrollViewStates.ContainsKey(regionKey))
        {
            ScrollViewStates[regionKey] = new ScrollViewState();
        }
        return ScrollViewStates[regionKey];
    }

    internal IEnumerable<string> GetMemoryRegionNames()
    {
        foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
        {
            yield return memInfo.DisplayName;
        }
    }

    internal IEnumerable<(MemoryRegionKey RegionKey, RangeCollection<IRegionInfo> Ranges)> GetAllMemoryRegions()
    {
        foreach (var (regionKey, parser) in romDataParsers)
        {
            yield return (regionKey, parser.GetRanges());
        }
    }

    internal ulong GetMinAddress()
    {
        if (romDataParsers.Count == 0) return 0;
        return romDataParsers.Values.Min(p => p.GetMinAddress);
    }

    internal ulong GetMaxAddress()
    {
        if (romDataParsers.Count == 0) return 0;
        return romDataParsers.Values.Max(p => p.GetMaxAddress);
    }

    internal MemoryRegionKey GetRegionToSwitch() => regionToSwitch;
    internal bool IsPendingTabSwitch() => pendingTabSwitch;
    internal void ClearPendingTabSwitch() => pendingTabSwitch = false;

    internal ICpuStateManager GetCpuStateManager() => cpuStateManager;
    internal bool IsAutomated() => automated;
    internal void StopAutomation() => automated = false;

    /// <summary>
    /// Navigates to the specified address in the Resourcer view.
    /// </summary>
    public void NavigateToAddress(MemoryRegionKey regionKey, ulong address)
    {
        // Set the active region to switch to the correct tab
        regionToSwitch = regionKey;
        pendingTabSwitch = true;
        
        if (ScrollViewStates.TryGetValue(regionKey, out var vars))
        {
            vars.JumpToAddress = address;
            vars.JumpPending = true;
            // Also set the cursor position to the address for immediate visibility
            var parser = romDataParsers[regionKey];
            var ranges = parser.GetRanges();
            var line = ranges.FetchLineForAddress(address);
            vars.SelectedRows.Clear();
            vars.SelectedRows.Add(line);
            vars.CursorPosition = line;
        }
    }

    public bool Initialise()
    {
        return true;
    }

    public void Close()
    {
        // Save all regions
        foreach (var (regionName, parser) in romDataParsers)
        {
            parser.Save($"TEST_{parser.MemoryInformation.DisplayName}.JSON");
        }
    }

    public void Update(float seconds)
    {
        if (automated)
        {
            UpdateAutoDisassembly();
        }

        if (!romLoaded)
        {
            LoadRomData();
            romLoaded = true;
        }

        // Handle trace in progress
        if (traceInProgress && !traceCommandInProgress)
        {
            UpdateTraceCapture();
        }
    }

    private void UpdateAutoDisassembly()
    {
        if (autoStack.Count == 0 || autoState.Count == 0)
        {
            automated = false;
            return;
        }

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

    private void LoadRomData()
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
                romDataParsers[regionKey].AddCommentRange(
                    ["RetroEditor Resourcer Version 0.1", "", "A WIP Tool for re-sourcing ROMS", "", ""], 0);
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
            foreach (var regionKey in romDataParsers.Keys)
            {
                hardwareRegisterProvider.InitializeSymbols(romDataParsers[regionKey], regionKey);
            }
        }
    }

    private void UpdateTraceCapture()
    {
        if (debugger.IsStopped && !traceCommandFinishStarted)
        {
            traceCommandFinishStarted = true;
            traceCommandFinished = false;
            debugger.QueueCommand("traceflush ; trace off", LibMameDebugger.ActionTrigger.Default, 
                (s, id) => { traceCommandFinished = true; });
        }
        
        if (debugger.IsStopped && traceCommandFinished)
        {
            ProcessTraceFile();
            
            if (traceContinue)
            {
                RestartTraceContinuous();
            }
            else
            {
                traceInProgress = false;
            }
        }
    }

    private void ProcessTraceFile()
    {
        // Read and parse trace file
        if (File.Exists(traceFile))
        {
            var lines = File.ReadAllLines(traceFile);
            foreach (var line in lines)
            {
                if (traceParser.TryParseTraceLine(line, out var traceEntry) && traceEntry.IsValid)
                {
                    ParseTraceEntry(traceEntry);
                }
            }
        }
    }

    private void RestartTraceContinuous()
    {
        if (File.Exists("trace.log"))
        {
            File.Delete("trace.log");
        }
        traceCommandInProgress = true;
        traceCommandFinished = false;
        traceCommandFinishStarted = false;

        debugger.QueueCommand($"trace {traceFile},,,{{tracelog {cpuStateManager.GetTraceFormat()}}}", 
            LibMameDebugger.ActionTrigger.Default, (s, id) => { });
        debugger.QueueCommand("gtime 500", LibMameDebugger.ActionTrigger.TriggerOnRunning, 
            (s, id) => { traceCommandInProgress = false; });
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
            return;
        }
        
        if (i.IsBranch)
        {
            // Fetch the target address and construct a label there
            foreach (var target in i.NextAddresses)
            {
                var mappedTarget = memoryMapper.MapCpuToRegion(target, out var targetRegionKey);
                romDataParsers[targetRegionKey].AddLabel(mappedTarget, $"loc_{systemName}_{mediaName}_{mappedTarget:X8}");
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
                    romDataParsers[access.RegionKey].AddDataRange(regionAddress, endAddress, access.Size, 
                        accessRegionInfo?.HasPhysicalData ?? true);
                }
            }
        }
    }

    // ============ IResourcerViewCallbacks Implementation ============

    public void OnJumpToAddress(MemoryRegionKey regionKey, ulong address)
    {
        NavigateToAddress(regionKey, address);
    }

    public void OnRowSelected(MemoryRegionKey regionKey, ulong line, bool isShiftHeld)
    {
        var vars = GetOrCreateScrollViewState(regionKey);
        
        if (isShiftHeld)
        {
            // Range selection
            if (!vars.SelectionStart.HasValue)
            {
                vars.SelectionStart = vars.CursorPosition ?? line;
            }
            var start = Math.Min(vars.SelectionStart.Value, line);
            var end = Math.Max(vars.SelectionStart.Value, line);
            vars.SelectedRows.Clear();
            for (var j = start; j <= end; j++)
            {
                vars.SelectedRows.Add(j);
            }
        }
        else
        {
            // Toggle selection
            bool isSelected = vars.SelectedRows.Contains(line);
            vars.SelectedRows.Clear();
            if (!isSelected)
            {
                vars.SelectedRows.Add(line);
            }
            vars.CursorPosition = line;
            vars.SelectionStart = null;
        }
    }

    public void OnKeyPressed(MemoryRegionKey regionKey, MyMGui.ImGuiKey key, ulong? cursorPosition, HashSet<ulong> selectedRows)
    {
        var parser = romDataParsers[regionKey];
        var ranges = parser.GetRanges();
        
        memoryRegionInfoCache.TryGetValue(regionKey, out var regionInfo);
        
        if (selectedRows.Count > 0)
        {
            ulong minAddress = ulong.MaxValue;
            ulong maxAddress = ulong.MinValue;
            
            foreach (var srow in selectedRows)
            {
                var address = ranges.FetchAddressForLine(srow);
                var endAddress = ranges.FetchAddressForLine(srow + 1);
                if (address < minAddress)
                    minAddress = address;
                if (endAddress > 0)
                    endAddress--;
                if (endAddress > maxAddress)
                    maxAddress = endAddress;
            }
            
            ulong cursorMinAddress = 0;
            ulong cursorMaxAddress = 0;
            if (cursorPosition != null)
            {
                cursorMinAddress = ranges.FetchAddressForLine(cursorPosition.Value);
                cursorMaxAddress = ranges.FetchAddressForLine(cursorPosition.Value + 1);
                if (cursorMaxAddress > 0)
                    cursorMaxAddress--;
                cursorMaxAddress = Math.Max(cursorMinAddress, cursorMaxAddress);
            }
            
            bool clearSelection = false;
            
            switch (key)
            {
                case MyMGui.ImGuiKey.S:
                    parser.AddStringRange(minAddress, maxAddress, regionInfo?.HasPhysicalData ?? true);
                    clearSelection = true;
                    break;
                    
                case MyMGui.ImGuiKey.U:
                    parser.AddUnknownRange(minAddress, maxAddress, regionInfo?.HasPhysicalData ?? true);
                    clearSelection = true;
                    break;
                    
                case MyMGui.ImGuiKey.Semicolon:
                    if (cursorPosition != null)
                    {
                        parser.AddCommentRange(
                            ["I AM THE VERY MODEL OF A MODERN MAJOR GENERAL", 
                             "I'VE INFORMATION ANIMAL VEGETABLE AND MINERAL", "....."], 
                            cursorMinAddress);
                    }
                    break;
            }
            
            // Code and data operations only apply to regions with physical data
            if (regionInfo != null && regionInfo.HasPhysicalData)
            {
                switch (key)
                {
                    case MyMGui.ImGuiKey.C:
                        var cpuState = cpuStateManager.FetchStateFromUI();
                        disassembler.State = cpuState;
                        parser.AddCodeRange((DisassemblerBase)disassembler, minAddress, maxAddress, memoryMapper);
                        cpuStateManager.UpdateUIFromState(disassembler.State);
                        clearSelection = true;
                        break;
                        
                    case MyMGui.ImGuiKey.A when !automated:
                        cpuState = cpuStateManager.FetchStateFromUI();
                        autoDisassembler.State = cpuState;
                        var autoPC = memoryMapper.MapRomToCpu(minAddress);
                        autoStack.Clear();
                        autoState.Clear();
                        stacked.Clear();
                        autoStack.Push(autoPC);
                        autoState.Push(autoDisassembler.State);
                        automated = true;
                        break;
                        
                    case MyMGui.ImGuiKey._1 when !automated:
                        parser.AddDataRange(minAddress, minAddress + 1, 1);
                        break;
                        
                    case MyMGui.ImGuiKey._2 when !automated:
                        parser.AddDataRange(minAddress, minAddress + 1, 2);
                        break;
                        
                    case MyMGui.ImGuiKey._3 when !automated:
                        parser.AddDataRange(minAddress, minAddress + 2, 3);
                        break;
                        
                    case MyMGui.ImGuiKey._4 when !automated:
                        parser.AddDataRange(minAddress, minAddress + 3, 4);
                        break;
                }
            }
            
            if (clearSelection)
            {
                selectedRows.Clear();
            }
        }
    }

    public void OnAddLabel(MemoryRegionKey regionKey, ulong address, string name)
    {
        romDataParsers[regionKey].AddLabel(address, name);
    }

    public void OnRenameLabel(MemoryRegionKey regionKey, ulong address, string oldName, string newName)
    {
        romDataParsers[regionKey].RenameLabel(address, oldName, newName);
    }

    public void OnDeleteLabel(MemoryRegionKey regionKey, ulong address, string name)
    {
        romDataParsers[regionKey].RemoveLabel(address, name);
    }

    public void OnCaptureTrace(TraceCaptureMode mode)
    {
        traceInProgress = true;
        if (File.Exists("trace.log"))
        {
            File.Delete("trace.log");
        }
        traceCommandInProgress = true;
        traceCommandFinished = false;
        traceCommandFinishStarted = false;

        debugger.QueueCommand($"trace {traceFile},,,{{tracelog {cpuStateManager.GetTraceFormat()}}}", 
            LibMameDebugger.ActionTrigger.Default, (s, id) => { });

        switch (mode)
        {
            case TraceCaptureMode.Frame:
                debugger.QueueCommand("gvblank", LibMameDebugger.ActionTrigger.TriggerOnRunning, 
                    (s, id) => { traceCommandInProgress = false; });
                break;
                
            case TraceCaptureMode.OneSecond:
                debugger.QueueCommand("gtime 1000", LibMameDebugger.ActionTrigger.TriggerOnRunning, 
                    (s, id) => { traceCommandInProgress = false; });
                break;
                
            case TraceCaptureMode.Continuous:
                traceContinue = true;
                debugger.QueueCommand("gtime 500", LibMameDebugger.ActionTrigger.TriggerOnRunning, 
                    (s, id) => { traceCommandInProgress = false; });
                break;
        }
    }

    public void OnStopTrace()
    {
        traceContinue = false;
    }

    // ============ Line Enumeration Helpers ============

    /// <summary>
    /// Enumerates lines from a RangeCollection by line range
    /// </summary>
    internal IEnumerable<LineEnumerationItem> EnumerateLinesByRange(RangeCollection<IRegionInfo> ranges, ulong startLine, ulong count)
    {
        var currentLine = startLine;
        var fetched = ranges.GetRangeContainingLine(currentLine, out var line);
        
        if (fetched == null)
            yield break;
        
        var lineCount = fetched.Value.LineCount;
        
        for (ulong i = 0; i < count; i++)
        {
            if (lineCount == line)
            {
                currentLine = fetched.LineEnd + 1;
                fetched = ranges.GetRangeContainingLine(currentLine, out line);
                if (fetched == null)
                    yield break;
                lineCount = fetched.Value.LineCount;
            }
            
            var lineInfo = fetched.Value.GetLineInfo(line);
            
            yield return new LineEnumerationItem
            {
                AbsoluteLine = startLine + i,
                LineWithinRange = line,
                RegionInfo = fetched.Value,
                LineInfo = lineInfo
            };
            
            line++;
        }
    }

    /// <summary>
    /// Enumerates lines from a RangeCollection by address range
    /// </summary>
    internal IEnumerable<LineEnumerationItem> EnumerateLinesByAddressRange(RangeCollection<IRegionInfo> ranges, ulong startAddress, ulong endAddress)
    {
        var startLine = ranges.FetchLineForAddress(startAddress);
        var endLine = ranges.FetchLineForAddress(endAddress);
        var count = endLine >= startLine ? endLine - startLine + 1 : 0;
        
        return EnumerateLinesByRange(ranges, startLine, count);
    }

    private IEnumerable<LineEnumerationItem> EnumerateLinesBySelection(RangeCollection<IRegionInfo> ranges, IEnumerable<ulong> selectedLines)
    {
        foreach (var line in selectedLines)
        {
            var fetched = ranges.GetRangeContainingLine(line, out var lineWithinRange);
            if (fetched != null)
            {
                var lineInfo = fetched.Value.GetLineInfo(lineWithinRange);
                yield return new LineEnumerationItem
                {
                    AbsoluteLine = line,
                    LineWithinRange = lineWithinRange,
                    RegionInfo = fetched.Value,
                    LineInfo = lineInfo
                };
            }
        }
    }
}
