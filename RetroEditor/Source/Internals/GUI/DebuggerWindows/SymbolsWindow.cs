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
    
    private string searchFilter = "";
    private int sortColumn = 0; // 0=Address, 1=Size, 2=Name, 3=Region
    private bool sortAscending = true;

    public SymbolsWindow(Dictionary<MemoryRegionKey, RomDataParser> romDataParsers, IMemoryInformationProvider memoryInformationProvider)
    {
        this.romDataParsers = romDataParsers;
        this.memoryInformationProvider = memoryInformationProvider;
    }

    public bool Initialise()
    {
        RefreshSymbols();
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
            }

            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
            {
                RefreshSymbols();
                ApplyFilter();
            }

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
                    ImGui.Text($"0x{symbol.Address:X8}");
                    
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(symbol.Size.ToString());
                    
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(symbol.Name);
                    
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(symbol.RegionName);
                }

                ImGui.EndTable();
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
}

