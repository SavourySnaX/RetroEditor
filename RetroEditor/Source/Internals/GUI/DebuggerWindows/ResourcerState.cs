using System.Collections.Generic;

namespace RetroEditor.Source.Internals.GUI.DebuggerWindows;

/// <summary>
/// View state for a single memory region's scrollable table view
/// </summary>
internal class ScrollViewState
{
    public ulong JumpToAddress = 0;
    public bool JumpPending = false;
    
    // Selection state
    public HashSet<ulong> SelectedRows = new HashSet<ulong>();
    public ulong? CursorPosition = null;
    public ulong? SelectionStart = null;
    
    // Rename state (fields for ImGui ref parameters)
    public bool ShowRenameDialog = false;
    public string RenameBuffer = "";
    public bool IsRenamingLabel = false;
    public ulong RenameAddress = 0;
    public string RenameLabelOldName = "";
    public int RenameSymbolSize = 0;
}

/// <summary>
/// Holds information about a single enumerated line from a RangeCollection
/// </summary>
internal record LineEnumerationItem
{
    public ulong AbsoluteLine { get; init; }
    public ulong LineWithinRange { get; init; }
    public IRegionInfo RegionInfo { get; init; } = null!;
    public LineInfo LineInfo { get; init; }
}

/// <summary>
/// Trace capture modes
/// </summary>
internal enum TraceCaptureMode
{
    Frame,
    OneSecond,
    Continuous
}
