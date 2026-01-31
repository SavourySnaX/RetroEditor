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
                        foreach (var symbol in filteredSymbols)
                        {
                            ImGui.TableNextRow();
                            
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text($"{symbol.Address:X8}");
                            
                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(symbol.Size.ToString());
                            
                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text(symbol.Name);
                            
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

                        foreach (var label in filteredLabels)
                        {
                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text($"{label.Address:X8}");

                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(label.Name);

                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text(label.RegionName);
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
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

