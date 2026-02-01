using MyMGui;
using System.Numerics;
using RetroEditor.Source.Internals.ReverseEngineering;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

internal class SymbolsWindow : IWindow
{
    public float UpdateInterval => 1 / 30.0f;
    public bool MinimumSize => false;

    private Dictionary<MemoryRegionKey, RomDataParser> romDataParsers;
    private IMemoryInformationProvider memoryInformationProvider;
    private List<ExtendedSymbol> allSymbols = new();
    private List<ExtendedSymbol> filteredSymbols = new();
    private List<ExtendedLabel> allLabels = new();
    private List<ExtendedLabel> filteredLabels = new();
    
    private string searchFilter = "";
    private int sortColumn = 0; // 0=Address, 1=Size, 2=Name, 3=Region
    private bool sortAscending = true;
    private int labelSortColumn = 0; // 0=Address, 1=Name, 2=Region
    private bool labelSortAscending = true;

    // Rename state
    private int selectedSymbolIndex = -1;
    private int selectedLabelIndex = -1;
    private bool showRenameDialog = false;
    private string renameBuffer = "";
    private bool isRenamingSymbol = false;
    private ExtendedSymbol? symbolToRename = null;
    private ExtendedLabel? labelToRename = null;

    public SymbolsWindow(Dictionary<MemoryRegionKey, RomDataParser> romDataParsers, IMemoryInformationProvider memoryInformationProvider)
    {
        this.romDataParsers = romDataParsers;
        this.memoryInformationProvider = memoryInformationProvider;
    }

    public bool Initialise()
    {
        RefreshSymbols();
        RefreshLabels();
        return true;
    }

    public void Update(float seconds)
    {
        // Could add periodic refresh if symbols change at runtime
    }

    public bool Draw()
    {
        bool shouldClose = false;

        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
        
            // Search filter
            ImGui.Text("Search:");
            ImGui.SameLine();
            if (ImGui.InputText("##SymbolSearch", ref searchFilter, 256))
            {
                ApplyFilter();
                ApplyLabelFilter();
            }

            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
            {
                RefreshSymbols();
                RefreshLabels();
                ApplyFilter();
                ApplyLabelFilter();
            }

            ImGui.Separator();

            if (ImGui.BeginTabBar("SymbolsLabelsTabs"))
            {
                if (ImGui.BeginTabItem("Symbols"))
                {
                    // Sort buttons
                    ImGui.Text("Sort by:");
                    ImGui.SameLine();
                    if (ImGui.Button("Address"))
                    {
                        if (sortColumn == 0)
                            sortAscending = !sortAscending;
                        else
                        {
                            sortColumn = 0;
                            sortAscending = true;
                        }
                        SortSymbols();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Size"))
                    {
                        if (sortColumn == 1)
                            sortAscending = !sortAscending;
                        else
                        {
                            sortColumn = 1;
                            sortAscending = true;
                        }
                        SortSymbols();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Name"))
                    {
                        if (sortColumn == 2)
                            sortAscending = !sortAscending;
                        else
                        {
                            sortColumn = 2;
                            sortAscending = true;
                        }
                        SortSymbols();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Region"))
                    {
                        if (sortColumn == 3)
                            sortAscending = !sortAscending;
                        else
                        {
                            sortColumn = 3;
                            sortAscending = true;
                        }
                        SortSymbols();
                    }

                    ImGui.Separator();

                    // Table display
                    ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersV 
                        | ImGuiTableFlags.BordersH 
                        | ImGuiTableFlags.RowBg 
                        | ImGuiTableFlags.ScrollY;

                    if (ImGui.BeginTable("SymbolsTable", 4, tableFlags))
                    {
                        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
                        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 60);
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Region", ImGuiTableColumnFlags.WidthFixed, 80);

                        // Display symbols
                        for (int i = 0; i < filteredSymbols.Count; i++)
                        {
                            var symbol = filteredSymbols[i];
                            ImGui.TableNextRow();
                            
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text($"{symbol.Address:X8}");
                            
                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(symbol.Size.ToString());
                            
                            ImGui.TableSetColumnIndex(2);
                            
                            // Make the row selectable for right-click context menu
                            ImGui.PushID(i);
                            if (ImGui.Selectable($"##{i}", selectedSymbolIndex == i, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                selectedSymbolIndex = i;
                            }
                            
                            // Right-click context menu
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            {
                                selectedSymbolIndex = i;
                                symbolToRename = symbol;
                                isRenamingSymbol = true;
                                ImGui.OpenPopup($"SymbolMenu{i}");
                            }
                            
                            if (ImGui.BeginPopup($"SymbolMenu{i}"))
                            {
                                if (ImGui.MenuItem("Rename"))
                                {
                                    symbolToRename = symbol;
                                    isRenamingSymbol = true;
                                    showRenameDialog = true;
                                    renameBuffer = symbol.Name;
                                }
                                if (ImGui.MenuItem("Delete"))
                                {
                                    if (romDataParsers.TryGetValue(symbol.RegionKey, out var parser))
                                    {
                                        parser.RemoveSymbol(symbol.Address, symbol.Size);
                                        RefreshSymbols();
                                    }
                                }
                                ImGui.EndPopup();
                            }
                            
                            // Draw the actual text on the same line
                            ImGui.SameLine(0, 0);
                            ImGui.Text(symbol.Name);
                            ImGui.PopID();
                            
                            ImGui.TableSetColumnIndex(3);
                            ImGui.Text(symbol.RegionName);
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Labels"))
                {
                    // Sort buttons
                    ImGui.Text("Sort by:");
                    ImGui.SameLine();
                    if (ImGui.Button("Address##Labels"))
                    {
                        if (labelSortColumn == 0)
                            labelSortAscending = !labelSortAscending;
                        else
                        {
                            labelSortColumn = 0;
                            labelSortAscending = true;
                        }
                        SortLabels();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Name##Labels"))
                    {
                        if (labelSortColumn == 1)
                            labelSortAscending = !labelSortAscending;
                        else
                        {
                            labelSortColumn = 1;
                            labelSortAscending = true;
                        }
                        SortLabels();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Region##Labels"))
                    {
                        if (labelSortColumn == 2)
                            labelSortAscending = !labelSortAscending;
                        else
                        {
                            labelSortColumn = 2;
                            labelSortAscending = true;
                        }
                        SortLabels();
                    }

                    ImGui.Separator();

                    // Table display
                    ImGuiTableFlags labelTableFlags = ImGuiTableFlags.BordersV 
                        | ImGuiTableFlags.BordersH 
                        | ImGuiTableFlags.RowBg 
                        | ImGuiTableFlags.ScrollY;

                    if (ImGui.BeginTable("LabelsTable", 3, labelTableFlags))
                    {
                        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Region", ImGuiTableColumnFlags.WidthFixed, 80);

                        for (int i = 0; i < filteredLabels.Count; i++)
                        {
                            var label = filteredLabels[i];
                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text($"{label.Address:X8}");

                            ImGui.TableSetColumnIndex(1);
                            
                            // Make the row selectable for right-click context menu
                            ImGui.PushID(i);
                            if (ImGui.Selectable($"##{i}", selectedLabelIndex == i, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                selectedLabelIndex = i;
                            }
                            
                            // Right-click context menu
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            {
                                selectedLabelIndex = i;
                                labelToRename = label;
                                ImGui.OpenPopup($"LabelMenu{i}");
                            }
                            
                            if (ImGui.BeginPopup($"LabelMenu{i}"))
                            {
                                if (ImGui.MenuItem("Rename"))
                                {
                                    labelToRename = label;
                                    isRenamingSymbol = false;
                                    showRenameDialog = true;
                                    renameBuffer = label.Name;
                                }
                                if (ImGui.MenuItem("Delete"))
                                {
                                    if (romDataParsers.TryGetValue(label.RegionKey, out var parser))
                                    {
                                        parser.RemoveLabel(label.Address, label.Name);
                                        RefreshLabels();
                                    }
                                }
                                ImGui.EndPopup();
                            }
                            
                            // Draw the actual text on the same line
                            ImGui.SameLine(0, 0);
                            ImGui.Text(label.Name);
                            ImGui.PopID();

                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text(label.RegionName);
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

        // Rename dialog popup
        if (showRenameDialog)
        {
            ImGui.OpenPopup("Rename");
        }

        if (ImGui.BeginPopupModal("Rename", ref showRenameDialog, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (isRenamingSymbol && symbolToRename != null)
            {
                ImGui.Text($"Rename symbol at 0x{symbolToRename.Address:X8}");
            }
            else if (!isRenamingSymbol && labelToRename != null)
            {
                ImGui.Text($"Rename label at 0x{labelToRename.Address:X8}");
            }

            ImGui.Separator();

            ImGui.Text("New name:");
            bool enterPressed = ImGui.InputText("##NewName", ref renameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue);

            ImGui.Separator();

            if (ImGui.Button("OK", new Vector2(120, 0)) || enterPressed)
            {
                // Validate the new name
                string? validationError = SymbolValidator.ValidateName(renameBuffer);
                
                if (validationError != null)
                {
                    // Show error - for now just ignore, in a real scenario we'd show a message
                    ImGui.OpenPopup("Error");
                }
                else
                {
                    // Check for conflicts
                    string? conflictError = null;
                    
                    if (isRenamingSymbol && symbolToRename != null)
                    {
                        conflictError = SymbolValidator.CheckAnyConflict(renameBuffer, romDataParsers, 
                            symbolToRename.Address, symbolToRename.Size);
                        
                        if (conflictError == null)
                        {
                            if (romDataParsers.TryGetValue(symbolToRename.RegionKey, out var parser))
                            {
                                parser.RenameSymbol(symbolToRename.Address, symbolToRename.Size, renameBuffer);
                                RefreshSymbols();
                                showRenameDialog = false;
                            }
                        }
                    }
                    else if (!isRenamingSymbol && labelToRename != null)
                    {
                        conflictError = SymbolValidator.CheckAnyConflict(renameBuffer, romDataParsers, 
                            labelToRename.Address);
                        
                        if (conflictError == null)
                        {
                            if (romDataParsers.TryGetValue(labelToRename.RegionKey, out var parser))
                            {
                                parser.RenameLabel(labelToRename.Address, labelToRename.Name, renameBuffer);
                                RefreshLabels();
                                showRenameDialog = false;
                            }
                        }
                    }
                    
                    if (conflictError != null)
                    {
                        validationError = conflictError;
                        ImGui.OpenPopup("Error");
                    }
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                showRenameDialog = false;
            }

            // Error popup
            bool errorOpen = true;
            if (ImGui.BeginPopupModal("Error", ref errorOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                string? validationError = SymbolValidator.ValidateName(renameBuffer);
                
                if (validationError == null && isRenamingSymbol && symbolToRename != null)
                {
                    validationError = SymbolValidator.CheckAnyConflict(renameBuffer, romDataParsers,
                        symbolToRename.Address, symbolToRename.Size);
                }
                else if (validationError == null && !isRenamingSymbol && labelToRename != null)
                {
                    validationError = SymbolValidator.CheckAnyConflict(renameBuffer, romDataParsers,
                        labelToRename.Address);
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

        return shouldClose;
    }

    public void Close()
    {
        // Cleanup if needed
    }

    private void RefreshSymbols()
    {
        allSymbols.Clear();

        // Collect all symbols from all regions
        foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
        {
            if (romDataParsers.TryGetValue(memInfo.RegionKey, out var parser))
            {
                var regionSymbols = parser.SymbolProvider.GetAllSymbols(memInfo.RegionKey);
                allSymbols.AddRange(regionSymbols);
            }
        }

        ApplyFilter();
    }

    private void RefreshLabels()
    {
        allLabels.Clear();

        foreach (var memInfo in memoryInformationProvider.GetMemoryRegions())
        {
            if (romDataParsers.TryGetValue(memInfo.RegionKey, out var parser))
            {
                var regionLabels = parser.LabelProvider.GetAllLabels(memInfo.RegionKey);
                allLabels.AddRange(regionLabels);
            }
        }

        ApplyLabelFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(searchFilter))
        {
            filteredSymbols = new List<ExtendedSymbol>(allSymbols);
        }
        else
        {
            var lowerFilter = searchFilter.ToLowerInvariant();
            filteredSymbols = allSymbols
                .Where(s => s.Name.ToLowerInvariant().Contains(lowerFilter) 
                    || s.Address.ToString("X8").Contains(searchFilter))
                .ToList();
        }

        SortSymbols();
    }

    private void ApplyLabelFilter()
    {
        if (string.IsNullOrWhiteSpace(searchFilter))
        {
            filteredLabels = new List<ExtendedLabel>(allLabels);
        }
        else
        {
            var lowerFilter = searchFilter.ToLowerInvariant();
            filteredLabels = allLabels
                .Where(l => l.Name.ToLowerInvariant().Contains(lowerFilter)
                    || l.Address.ToString("X8").Contains(searchFilter))
                .ToList();
        }

        SortLabels();
    }

    private void SortSymbols()
    {
        filteredSymbols = sortColumn switch
        {
            0 => sortAscending 
                ? filteredSymbols.OrderBy(s => s.Address).ToList()
                : filteredSymbols.OrderByDescending(s => s.Address).ToList(),
            1 => sortAscending 
                ? filteredSymbols.OrderBy(s => s.Size).ToList()
                : filteredSymbols.OrderByDescending(s => s.Size).ToList(),
            2 => sortAscending 
                ? filteredSymbols.OrderBy(s => s.Name).ToList()
                : filteredSymbols.OrderByDescending(s => s.Name).ToList(),
            3 => sortAscending 
                ? filteredSymbols.OrderBy(s => s.RegionName).ToList()
                : filteredSymbols.OrderByDescending(s => s.RegionName).ToList(),
            _ => filteredSymbols
        };
    }

    private void SortLabels()
    {
        filteredLabels = labelSortColumn switch
        {
            0 => labelSortAscending
                ? filteredLabels.OrderBy(l => l.Address).ToList()
                : filteredLabels.OrderByDescending(l => l.Address).ToList(),
            1 => labelSortAscending
                ? filteredLabels.OrderBy(l => l.Name).ToList()
                : filteredLabels.OrderByDescending(l => l.Name).ToList(),
            2 => labelSortAscending
                ? filteredLabels.OrderBy(l => l.RegionName).ToList()
                : filteredLabels.OrderByDescending(l => l.RegionName).ToList(),
            _ => filteredLabels
        };
    }
}

