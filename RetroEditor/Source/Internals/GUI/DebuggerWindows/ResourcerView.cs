using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MyMGui;
using RetroEditor.Source.Internals.ReverseEngineering;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.GUI.DebuggerWindows;

/// <summary>
/// View for Resourcer - handles all rendering
/// </summary>
internal class ResourcerView
{
    private const int MEMORY_MAP_HEIGHT = 50;
    
    private readonly ResourcerConfig config;

    public ResourcerView(ResourcerConfig config)
    {
        this.config = config;
    }

    /// <summary>
    /// Main draw method called by Resourcer
    /// </summary>
    internal bool Draw(ResourcerController controller)
    {
        DrawMemoryMap(controller.GetAllMemoryRegions(), controller.GetMinAddress(), controller.GetMaxAddress());
        DrawTraceControls(controller);
        controller.GetCpuStateManager().RenderUI();
        
        ImGui.SameLine();
        ImGui.BeginDisabled();
        bool automated = controller.IsAutomated();
        ImGui.Checkbox("Automated", ref automated);
        ImGui.EndDisabled();

        DrawRegionTabs(controller);

        return false;
    }

    private void DrawMemoryMap(IEnumerable<(MemoryRegionKey, RangeCollection<IRegionInfo>)> regions, ulong minAddress, ulong maxAddress)
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();
        size.Y = MEMORY_MAP_HEIGHT;

        var range = maxAddress - minAddress;
        var scale = size.X / range;

        foreach (var (regionName, ranges) in regions)
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

    private void DrawTraceControls(ResourcerController controller)
    {
        var traceDisable = controller.IsTraceInProgress;
        if (traceDisable)
        {
            ImGui.BeginDisabled();
        }
        
        if (ImGui.Button("Capture Frame"))
        {
            controller.OnCaptureTrace(TraceCaptureMode.Frame);
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Capture 1 Second"))
        {
            controller.OnCaptureTrace(TraceCaptureMode.OneSecond);
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Capture Continuous"))
        {
            controller.OnCaptureTrace(TraceCaptureMode.Continuous);
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
            controller.OnStopTrace();
        }
        
        if (!traceDisable)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawRegionTabs(ResourcerController controller)
    {
        if (!ImGui.BeginTabBar("ResourcerTabs"))
            return;

        var regionsToRender = controller.MemoryInformationProvider.GetMemoryRegions().ToList();
        var regionToSwitch = controller.GetRegionToSwitch();
        var pendingTabSwitch = controller.IsPendingTabSwitch();

        foreach (var memInfo in regionsToRender)
        {
            var regionKey = memInfo.RegionKey;
            var displayName = memInfo.DisplayName;
            
            bool isActive = (regionKey.Key == regionToSwitch.Key);
            ImGuiTabItemFlags flags = (isActive && pendingTabSwitch) ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            
            if (ImGui.BeginTabItem(displayName, flags))
            {
                if (flags == ImGuiTabItemFlags.SetSelected)
                {
                    controller.ClearPendingTabSwitch();
                }

                var ranges = controller.RomDataParsers[regionKey].GetRanges();
                var vars = controller.GetOrCreateScrollViewState(regionKey);
                DrawScrollableTableView(ranges, regionKey, vars, controller);
                ImGui.EndTabItem();
            }
        }
        
        ImGui.EndTabBar();
    }

    private void DrawScrollableTableView(
        RangeCollection<IRegionInfo> regions, 
        MemoryRegionKey regionKey, 
        ScrollViewState vars,
        ResourcerController controller)
    {
        var jump = false;
        ulong jumpAddress = vars.JumpToAddress;
        
        if (InputU64ScalarWrapped("Jump to Address", ref jumpAddress))
        {
            vars.JumpToAddress = jumpAddress;
            jump = true;
        }
        
        if (vars.JumpPending)
        {
            jump = true;
            vars.JumpPending = false;
        }

        // Start header
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
        
        if (!ImGui.BeginChild("Virtual Table", contentSize, ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar))
            return;

        var scroll = ImGui.GetScrollY();
        bool moved = false;

        var romDataSize = regions.LineCount;
        ulong currentLine = (ulong)(scroll / rowHeight);
        ulong firstLine = currentLine;
        float availableHeight = ImGui.GetContentRegionAvail().Y;
        var visibleLines = (int)(availableHeight / rowHeight);

        // Handle keyboard input
        if (ImGui.IsWindowFocused())
        {
            moved = HandleKeyboardNavigation(vars, regions, visibleLines);
            HandleKeyboardActions(regionKey, vars, regions, controller);
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
                var countToDisplay = (ulong)(clipper.DisplayEnd - clipper.DisplayStart);
                var lineItems = controller.EnumerateLinesByRange(regions, currentLine, countToDisplay);

                foreach (var item in lineItems)
                {
                    DrawTableRow(item, firstLine, vars, regionKey, controller);
                }
            }
            clipper.End();
            ImGui.EndTable();
        }

        // Handle jump
        if (jump)
        {
            var jumpLine = regions.FetchLineForAddress(vars.JumpToAddress);
            var jumpVisibleLines = (uint)(contentSize.Y / rowHeight);
            var scrollOffset = Math.Min(jumpLine, jumpVisibleLines / 3);
            ImGui.SetScrollY((jumpLine - scrollOffset) * rowHeight);
        }

        // Handle scroll for keyboard movement
        if (moved && vars.CursorPosition.HasValue)
        {
            if (scroll > vars.CursorPosition.Value * rowHeight)
            {
                ImGui.SetScrollY(vars.CursorPosition.Value * rowHeight);
            }
            else if (scroll + (visibleLines - 1) * rowHeight < vars.CursorPosition.Value * rowHeight)
            {
                ImGui.SetScrollY(vars.CursorPosition.Value * rowHeight - (visibleLines - 1) * rowHeight);
            }
        }

        // Rename dialog
        DrawRenameDialog(vars, regionKey, controller);
        
        ImGui.EndChild();
    }

    private void DrawTableRow(
        LineEnumerationItem item,
        ulong firstLine, 
        ScrollViewState vars, 
        MemoryRegionKey regionKey,
        ResourcerController controller)
    {
        var actualLine = item.AbsoluteLine;
        var fetched = item.RegionInfo;
        var line = item.LineWithinRange;

        ImGui.PushID((int)(actualLine - firstLine));
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        var lData = item.LineInfo;

        bool isSelected = vars.SelectedRows.Contains(actualLine);
        bool isCursor = vars.CursorPosition == actualLine;

        bool clicked = ImGui.Selectable($"{lData.Address:X8}", isSelected, ImGuiSelectableFlags.SpanAllColumns);

        // Set row background color
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
            var colour = lData.IsLabel ? ResourcerConfig.ConfigColour.Label : fetched.Colour;
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, config.GetColorU32(colour));
        }

        if (clicked)
        {
            controller.OnRowSelected(regionKey, actualLine, ImGui.IsKeyDown(ImGuiKey.ModShift));
        }

        // Right-click context menu
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            if (lData.IsLabel)
            {
                var labelName = lData.Address.TrimEnd(':');
                var address = fetched.RegionAddressForLine(line);
                vars.IsRenamingLabel = true;
                vars.RenameAddress = address;
                vars.RenameLabelOldName = labelName;
                vars.RenameBuffer = labelName;
            }
            else
            {
                vars.IsRenamingLabel = false;
            }
            ImGui.OpenPopup($"RowMenu{actualLine}");
        }
        
        DrawContextMenu(actualLine, item, vars, regionKey, controller);

        ImGui.TableSetColumnIndex(1);
        ImGui.Text(lData.Bytes);
        ImGui.TableSetColumnIndex(2);
        ImGui.Text(lData.Details);
        ImGui.TableSetColumnIndex(3);
        ImGui.Text(lData.Comment);
        ImGui.PopID();
    }

    private void DrawContextMenu(
        ulong actualLine, 
        LineEnumerationItem item, 
        ScrollViewState vars,
        MemoryRegionKey regionKey,
        ResourcerController controller)
    {
        if (!ImGui.BeginPopup($"RowMenu{actualLine}"))
            return;

        var lData = item.LineInfo;
        var fetched = item.RegionInfo;
        var line = item.LineWithinRange;

        if (lData.IsLabel)
        {
            if (ImGui.MenuItem("Rename Label"))
            {
                vars.ShowRenameDialog = true;
            }
            
            if (ImGui.MenuItem("Delete Label"))
            {
                var labelName = lData.Address.TrimEnd(':');
                var address = fetched.RegionAddressForLine(line);
                controller.OnDeleteLabel(regionKey, address, labelName);
            }
        }
        else
        {
            if (ImGui.MenuItem("Add Label"))
            {
                var address = fetched.RegionAddressForLine(line);
                controller.OnAddLabel(regionKey, address, $"label_{address:X8}");
            }
        }
        

        
        ImGui.EndPopup();
    }

    private bool HandleKeyboardNavigation(ScrollViewState vars, RangeCollection<IRegionInfo> regions, int visibleLines)
    {
        bool moved = false;
        
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
        {
            if (vars.CursorPosition.HasValue && vars.CursorPosition.Value > 0)
            {
                vars.CursorPosition--;
                vars.SelectedRows.Clear();
                vars.SelectedRows.Add(vars.CursorPosition.Value);
                moved = true;
            }
        }
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
        {
            if (vars.CursorPosition.HasValue && vars.CursorPosition.Value < regions.LineCount - 1)
            {
                vars.CursorPosition++;
                vars.SelectedRows.Clear();
                vars.SelectedRows.Add(vars.CursorPosition.Value);
                moved = true;
            }
        }
        if (ImGui.IsKeyPressed(ImGuiKey.PageUp))
        {
            if (vars.CursorPosition.HasValue)
            {
                var newPosition = vars.CursorPosition.Value >= (ulong)visibleLines ?
                    vars.CursorPosition.Value - (ulong)visibleLines : 0;
                vars.CursorPosition = newPosition;
                vars.SelectedRows.Clear();
                vars.SelectedRows.Add(vars.CursorPosition.Value);
                moved = true;
            }
        }
        if (ImGui.IsKeyPressed(ImGuiKey.PageDown))
        {
            if (vars.CursorPosition.HasValue)
            {
                var newPosition = (ulong)Math.Min(regions.LineCount - 1, vars.CursorPosition.Value + (ulong)visibleLines);
                vars.CursorPosition = newPosition;
                vars.SelectedRows.Clear();
                vars.SelectedRows.Add(vars.CursorPosition.Value);
                moved = true;
            }
        }
        if (ImGui.IsKeyPressed(ImGuiKey.Home))
        {
            vars.CursorPosition = 0;
            vars.SelectedRows.Clear();
            vars.SelectedRows.Add(0);
            moved = true;
        }
        if (ImGui.IsKeyPressed(ImGuiKey.End))
        {
            vars.CursorPosition = regions.LineCount - 1;
            vars.SelectedRows.Clear();
            vars.SelectedRows.Add(vars.CursorPosition.Value);
            moved = true;
        }
        
        // Jump to next/prior region
        if (vars.CursorPosition.HasValue && ImGui.IsKeyPressed(ImGuiKey.Period) && ImGui.IsKeyDown(ImGuiKey.LeftShift))
        {
            var currentRegion = regions.GetRangeContainingLine(vars.CursorPosition.Value, out var line);
            if (currentRegion != null)
            {
                var nextRegion = regions.GetRangeContainingLine(currentRegion.LineEnd + 1, out line);
                if (nextRegion != null)
                {
                    vars.CursorPosition = nextRegion.LineStart;
                    vars.SelectedRows.Clear();
                    vars.SelectedRows.Add(vars.CursorPosition.Value);
                    moved = true;
                }
            }
        }
        if (vars.CursorPosition.HasValue && ImGui.IsKeyPressed(ImGuiKey.Comma) && ImGui.IsKeyDown(ImGuiKey.LeftShift))
        {
            var currentRegion = regions.GetRangeContainingLine(vars.CursorPosition.Value, out var line);
            if (currentRegion != null)
            {
                var nextRegion = regions.GetRangeContainingLine(currentRegion.LineStart - 1, out line);
                if (nextRegion != null)
                {
                    vars.CursorPosition = nextRegion.LineEnd;
                    vars.SelectedRows.Clear();
                    vars.SelectedRows.Add(vars.CursorPosition.Value);
                    moved = true;
                }
            }
        }
        
        return moved;
    }

    private void HandleKeyboardActions(
        MemoryRegionKey regionKey, 
        ScrollViewState vars, 
        RangeCollection<IRegionInfo> regions,
        ResourcerController controller)
    {
        // Check for action keys
        var actionKeys = new[] { ImGuiKey.L, ImGuiKey.Semicolon, ImGuiKey.S, ImGuiKey.U, ImGuiKey.C, 
                                ImGuiKey.A, ImGuiKey._1, ImGuiKey._2, ImGuiKey._3, ImGuiKey._4 };
        
        foreach (var key in actionKeys)
        {
            if (ImGui.IsKeyPressed(key))
            {
                controller.OnKeyPressed(regionKey, key, vars.CursorPosition, vars.SelectedRows);
                break;
            }
        }
    }

    private void DrawRenameDialog(ScrollViewState vars, MemoryRegionKey regionKey, ResourcerController controller)
    {
        if (vars.ShowRenameDialog)
        {
            ImGui.OpenPopup("Rename##Resourcer");
        }

        if (!ImGui.BeginPopupModal("Rename##Resourcer", ref vars.ShowRenameDialog, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        if (vars.IsRenamingLabel)
        {
            ImGui.Text($"Rename label at 0x{vars.RenameAddress:X8}");
        }

        ImGui.Separator();
        ImGui.Text("New name:");
        bool enterPressed = ImGui.InputText("##NewName", ref vars.RenameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.Separator();

        if (ImGui.Button("OK", new Vector2(120, 0)) || enterPressed)
        {
            string? validationError = SymbolValidator.ValidateName(vars.RenameBuffer);
            
            if (validationError != null)
            {
                ImGui.OpenPopup("Error##Resourcer");
            }
            else
            {
                string? conflictError = SymbolValidator.CheckAnyConflict(vars.RenameBuffer, 
                    controller.RomDataParsers, vars.RenameAddress);
                
                if (conflictError == null)
                {
                    if (vars.IsRenamingLabel)
                    {
                        controller.OnRenameLabel(regionKey, vars.RenameAddress, vars.RenameLabelOldName, vars.RenameBuffer);
                    }
                    vars.ShowRenameDialog = false;
                }
                else
                {
                    ImGui.OpenPopup("Error##Resourcer");
                }
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new Vector2(120, 0)))
        {
            vars.ShowRenameDialog = false;
        }

        // Error popup
        bool errorOpen = true;
        if (ImGui.BeginPopupModal("Error##Resourcer", ref errorOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            string? validationError = SymbolValidator.ValidateName(vars.RenameBuffer);
            
            if (validationError == null)
            {
                validationError = SymbolValidator.CheckAnyConflict(vars.RenameBuffer, 
                    controller.RomDataParsers, vars.RenameAddress);
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

    private bool InputU64ScalarWrapped(string label, ref ulong value)
    {
        ImGui.InputScalar(label, ImGuiDataType.U64, ref value, "%X", ImGuiInputTextFlags.CharsHexadecimal);
        return ImGui.IsItemDeactivated();
    }
}
