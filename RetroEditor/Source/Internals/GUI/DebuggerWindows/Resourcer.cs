using ImGuiNET;
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

    // Selection state
    private HashSet<UInt64> selectedRows = new HashSet<UInt64>();
    private UInt64? cursorPosition = null;
    private UInt64? selectionStart = null;

    // Memory map data
    private const int MEMORY_MAP_HEIGHT = 50;

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
//        drawList.AddRectFilled(pos, new Vector2(pos.X + size.X, pos.Y + size.Y), ImGui.GetColorU32(unknownColor));

        foreach (var region in romData.GetRomRanges)
        {
            drawList.AddRectFilled(
                new Vector2(pos.X + region.Value.AddressStart * scale, pos.Y),
                new Vector2(pos.X + region.Value.AddressEnd * scale, pos.Y + size.Y),
                region.Value.Colour
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
    private void ScrollableTableView()
    {
        bool jump=false;
        if (InputU64ScalarWrapped("Jump to Address", ref jumpToAddress))
        {
            jump=true;
        }

        // Start Table and render header
        float [] widths = new float[4];

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

        ImGuiTableFlags tableFlags = ImGuiTableFlags.None|ImGuiTableFlags.BordersV|ImGuiTableFlags.RowBg;

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

            UInt64 currentLine = (UInt64)(0 + scroll/rowHeight);
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
                        moved=true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                {
                    if (cursorPosition.HasValue && cursorPosition.Value < romData.GetRomRanges.LineCount - 1)
                    {
                        cursorPosition++;
                        selectedRows.Clear();
                        selectedRows.Add(cursorPosition.Value);
                        moved=true;
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
                        moved=true;
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
                        moved=true;
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Home))
                {
                    cursorPosition = 0;
                    selectedRows.Clear();
                    selectedRows.Add(0);
                    moved=true;
                }
                if (ImGui.IsKeyPressed(ImGuiKey.End))
                {
                    cursorPosition = romData.GetRomRanges.LineCount - 1;
                    selectedRows.Clear();
                    selectedRows.Add(cursorPosition.Value);
                    moved=true;
                }
                if (selectedRows.Count > 0)
                {
                    UInt64 minAddress = UInt64.MaxValue;
                    UInt64 maxAddress = UInt64.MinValue;
                    foreach (var srow in selectedRows)
                    {
                        var address = regions.FetchAddressForLine(srow);
                        if (address < minAddress)
                            minAddress = address;
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

                    if (clearSelection)
                    {
                        selectedRows.Clear();
                    }
                }
            }

            if (ImGui.BeginTable("RomDataView", 4, tableFlags))
            {
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, widths[0] - ImGui.GetStyle().ItemSpacing.X*2);
                ImGui.TableSetupColumn("Bytes", ImGuiTableColumnFlags.WidthFixed, widths[1] - ImGui.GetStyle().ItemSpacing.X);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthFixed, widths[2] - ImGui.GetStyle().ItemSpacing.X);
                ImGui.TableSetupColumn("Comments", ImGuiTableColumnFlags.WidthFixed, widths[3] - ImGui.GetStyle().ItemSpacing.X);

                using var clipper = new ImGuiClipper((int)romDataSize, rowHeight);
                clipper.Begin();
                while (clipper.Step())
                {
                    var actualLine = currentLine;
                    var fetched = regions.GetRangeContainingLine(currentLine, out var line);
                    if (fetched==null)
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
                            if (fetched==null)
                            {
                                break;
                            }
                            lineCount = fetched.Value.LineCount;
                        }
                        
                        ImGui.PushID((int)(actualLine-firstLine));
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
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, fetched.Value.Colour);
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
            }

            if (moved)
            {
                // Set Scroll position to keep the currsor in 
                if (scroll > cursorPosition.Value * rowHeight)
                {
                    ImGui.SetScrollY(cursorPosition.Value * rowHeight);
                }
                else if (scroll+(visibleLines-1)*rowHeight < cursorPosition.Value * rowHeight)
                {
                    ImGui.SetScrollY(cursorPosition.Value * rowHeight - (visibleLines-1)*rowHeight);
                }
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

    public bool Initialise()
    {
        return true;
    }

    public enum SNESLoRomRegion
    {
        ROM,
        IO,
        SRAM,
        RAM,
    }

    public UInt64 MapSnesCpuToLorom(UInt64 address, out SNESLoRomRegion region)
    {
        region = SNESLoRomRegion.ROM;
        var bank = address >> 16;
        var offset = address & 0xFFFF;

        if (bank==0x7E || bank==0x7F)
        {
            region = SNESLoRomRegion.RAM;
            return ((bank - 0x7E) << 16) | offset;
        }
        else if (bank==0xFE || bank==0xFF)
        {
            if (offset<0x8000)
            {
                // SRAM
                region = SNESLoRomRegion.SRAM;
                return ((bank - 0xF0) << 15) | offset;
            }
            else
            {
                // ROM
                return 0x3F0000 | ((bank-0xFE)<<15) | (offset&0x7FFF);           
            }
        }
        bank&=0x7F;
        if (bank<0x40)
        {
            if (offset<0x2000)
            {
                // Low RAM
                region = SNESLoRomRegion.RAM;
                return offset;
            }
            else if (offset<0x8000)
            {
                // IO
                region = SNESLoRomRegion.IO;
                return offset-0x2000;
            }
            else
            {
                // ROM
                return (bank << 15) | (offset & 0x7FFF);
            }
        }
        else if (bank<0x70)
        {
            // ROM
            return (bank << 15) | (offset & 0x7FFF);
        }
        else
        {
            // 70-7D
            if (offset<0x8000)
            {
                region = SNESLoRomRegion.SRAM;
                return ((bank - 0x70) << 15) | offset;
            }
            else
            {
                //ROM
                return (bank<<15) | (offset&0x7FFF);
            }
        }
    }

    public void Update(float seconds)
    {
        if (!romLoaded)
        {
            // Read ROM data in chunks
            romData = new RomDataParser();

            romData.Parse(debugger);

//            romData.AddStringRange(0x7FC0, 0x7FD4); // LoRom ASCII Title in header
            var disassembler = new SNES65816Disassembler();
            UInt64 PC = 0x8000;
            var disassemble = new[] { romData.GetByte(MapSnesCpuToLorom(PC, out var _)) };
            disassembler.DecodeNext(disassemble,0x8000);
            
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