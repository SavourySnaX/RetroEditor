using System;
using System.Collections.Generic;
using MyMGui;
using RetroEditor.Source.Internals.ReverseEngineering;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;

namespace RetroEditor.Source.Internals.GUI.DebuggerWindows;

/// <summary>
/// Actions that can be triggered from the view
/// </summary>
internal enum ResourcerAction
{
    AddLabel,
    RenameLabel,
    DeleteLabel,
    AddCodeRange,
    AddDataRange,
    AddStringRange,
    AddUnknownRange,
    AddCommentRange,
    AutoDisassemble,
    JumpToNextRegion,
    JumpToPriorRegion
}

/// <summary>
/// Callbacks from the view to the controller
/// </summary>
internal interface IResourcerViewCallbacks
{
    void OnJumpToAddress(MemoryRegionKey regionKey, ulong address);
    void OnRowSelected(MemoryRegionKey regionKey, ulong line, bool isShiftHeld);
    void OnKeyPressed(MemoryRegionKey regionKey, ImGuiKey key, ulong? cursorPosition, HashSet<ulong> selectedRows);
    void OnAddLabel(MemoryRegionKey regionKey, ulong address, string name);
    void OnRenameLabel(MemoryRegionKey regionKey, ulong address, string oldName, string newName);
    void OnDeleteLabel(MemoryRegionKey regionKey, ulong address, string name);
    void OnCaptureTrace(TraceCaptureMode mode);
    void OnStopTrace();
}
