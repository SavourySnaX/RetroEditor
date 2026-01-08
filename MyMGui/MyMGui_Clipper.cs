using System.Runtime.InteropServices;

namespace MyMGui;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 56)]
internal unsafe partial struct ImGuiListClipper
{
    [FieldOffset(0)] public ImGuiContextPtr   Ctx;                // Parent UI context
    [FieldOffset(8)] public int             DisplayStart;       // First item to display, updated by each call to Step()
    [FieldOffset(12)] public int             DisplayEnd;         // End of items to display (exclusive)
    [FieldOffset(16)] public int             ItemsCount;         // [Internal] Number of items
    [FieldOffset(20)] public float           ItemsHeight;        // [Internal] Height of item after a first step and item submission can calculate it
    [FieldOffset(24)] public double          StartPosY;          // [Internal] Cursor position at the time of Begin() or after table frozen rows are all processed
    [FieldOffset(32)] public double          StartSeekOffsetY;   // [Internal] Account for frozen rows in a table and initial loss of precision in very large windows.
    [FieldOffset(40)] public void*           TempData;           // [Internal] Internal data
    [FieldOffset(48)] public ImGuiListClipperFlags Flags;        // [Internal] Flags, currently not yet well exposed.
};


public unsafe class ListClipper : IDisposable
{
    private ImGuiListClipper* _clipper;

    public ListClipper(int itemsCount, float itemsHeight)
    {
        _itemsCount = itemsCount;
        _itemsHeight = itemsHeight;
        _clipper = ImGui.ImGuiListClipper_ImGuiListClipper();
    }

    public void Begin()
    {
        ImGui.ImGuiListClipper_Begin(_clipper, _itemsCount, _itemsHeight);
    }

    public bool Step()
    {
        return ImGui.ImGuiListClipper_Step(_clipper) != 0;
    }

    public void End()
    {
        ImGui.ImGuiListClipper_End(_clipper);
    }

    public void Dispose()
    {
        ImGui.ImGuiListClipper_destroy(_clipper);
    }

    public int DisplayStart => _clipper->DisplayStart;
    public int DisplayEnd => _clipper->DisplayEnd;

    private int _itemsCount;
    private float _itemsHeight;
}
