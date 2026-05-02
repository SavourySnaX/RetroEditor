using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RetroEditor.Source.Internals.ReverseEngineering;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.GUI.DebuggerWindows;

/// <summary>
/// Resourcer - Facade that coordinates between ResourcerController and ResourcerView
/// </summary>
internal class Resourcer : IWindow
{
    public float UpdateInterval => 1 / 60.0f;
    public bool MinimumSize => false;
    
    // Public properties for external access (e.g., SymbolsWindow)
    public IReadOnlyDictionary<MemoryRegionKey, RomDataParser> RomDataParsers => controller.RomDataParsers;
    public IMemoryInformationProvider MemoryInformationProvider => controller.MemoryInformationProvider;
    
    internal readonly ResourcerController controller;
    internal readonly ResourcerView view;
    internal readonly ResourcerConfig config;

    public Resourcer(string systemName, string mediaName, LibMameDebugger debugger, IPlatformFactory platformFactory)
    {
        config = new ResourcerConfig();
        controller = new ResourcerController(systemName, mediaName, debugger, platformFactory);
        view = new ResourcerView(config);
    }

    /// <summary>
    /// Navigates to the specified address in the Resourcer view.
    /// </summary>
    public void NavigateToAddress(MemoryRegionKey regionKey, ulong address)
    {
        controller.NavigateToAddress(regionKey, address);
    }

    public void Close()
    {
        controller.Close();
    }

    public bool Draw()
    {
        return view.Draw(controller);
    }

    public bool Initialise()
    {
        return controller.Initialise();
    }

    public void Update(float seconds)
    {
        controller.Update(seconds);
    }
}
