using System.Runtime.InteropServices;

namespace MyMGui;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 1300)]
internal unsafe partial struct ImGuiStyle
{
    // Font scaling
    // - recap: ImGui::GetFontSize() == FontSizeBase * (FontScaleMain * FontScaleDpi * other_scaling_factors)
    [FieldOffset(0)] public float       FontSizeBase;               // Current base font size before external global factors are applied. Use PushFont(NULL, size) to modify. Use ImGui::GetFontSize() to obtain scaled value.
    [FieldOffset(4)] public float       FontScaleMain;              // Main global scale factor. May be set by application once, or exposed to end-user.
    [FieldOffset(8)] public float       FontScaleDpi;               // Additional global scale factor from viewport/monitor contents scale. When io.ConfigDpiScaleFonts is enabled, this is automatically overwritten when changing monitor DPI.

    [FieldOffset(12)] public float       Alpha;                      // Global alpha applies to everything in Dear ImGui.
    [FieldOffset(16)] public float       DisabledAlpha;              // Additional alpha multiplier applied by BeginDisabled(). Multiply over current value of Alpha.
    [FieldOffset(20)] public ImVec2      WindowPadding;              // Padding within a window.
    [FieldOffset(28)] public float       WindowRounding;             // Radius of window corners rounding. Set to 0.0f to have rectangular windows. Large values tend to lead to variety of artifacts and are not recommended.
    [FieldOffset(32)] public float       WindowBorderSize;           // Thickness of border around windows. Generally set to 0.0f or 1.0f. (Other values are not well tested and more CPU/GPU costly).
    [FieldOffset(36)] public float       WindowBorderHoverPadding;   // Hit-testing extent outside/inside resizing border. Also extend determination of hovered window. Generally meaningfully larger than WindowBorderSize to make it easy to reach borders.
    [FieldOffset(40)] public ImVec2      WindowMinSize;              // Minimum window size. This is a global setting. If you want to constrain individual windows, use SetNextWindowSizeConstraints().
    [FieldOffset(48)] public ImVec2      WindowTitleAlign;           // Alignment for title bar text. Defaults to (0.0f,0.5f) for left-aligned,vertically centered.
    [FieldOffset(56)] public ImGuiDir    WindowMenuButtonPosition;   // Side of the collapsing/docking button in the title bar (None/Left/Right). Defaults to ImGuiDir_Left.
    [FieldOffset(60)] public float       ChildRounding;              // Radius of child window corners rounding. Set to 0.0f to have rectangular windows.
    [FieldOffset(64)] public float       ChildBorderSize;            // Thickness of border around child windows. Generally set to 0.0f or 1.0f. (Other values are not well tested and more CPU/GPU costly).
    [FieldOffset(68)] public float       PopupRounding;              // Radius of popup window corners rounding. (Note that tooltip windows use WindowRounding)
    [FieldOffset(72)] public float       PopupBorderSize;            // Thickness of border around popup/tooltip windows. Generally set to 0.0f or 1.0f. (Other values are not well tested and more CPU/GPU costly).
    [FieldOffset(76)] public ImVec2      FramePadding;               // Padding within a framed rectangle (used by most widgets).
    [FieldOffset(84)] public float       FrameRounding;              // Radius of frame corners rounding. Set to 0.0f to have rectangular frame (used by most widgets).
    [FieldOffset(88)] public float       FrameBorderSize;            // Thickness of border around frames. Generally set to 0.0f or 1.0f. (Other values are not well tested and more CPU/GPU costly).
    [FieldOffset(92)] public ImVec2      ItemSpacing;                // Horizontal and vertical spacing between widgets/lines.
    [FieldOffset(100)] public ImVec2      ItemInnerSpacing;           // Horizontal and vertical spacing between within elements of a composed widget (e.g. a slider and its label).
    [FieldOffset(108)] public ImVec2      CellPadding;                // Padding within a table cell. Cellpadding.x is locked for entire table. CellPadding.y may be altered between different rows.
    [FieldOffset(116)] public ImVec2      TouchExtraPadding;          // Expand reactive bounding box for touch-based system where touch position is not accurate enough. Unfortunately we don't sort widgets so priority on overlap will always be given to the first widget. So don't grow this too much!
    [FieldOffset(124)] public float       IndentSpacing;              // Horizontal indentation when e.g. entering a tree node. Generally == (FontSize + FramePadding.x*2).
    [FieldOffset(128)] public float       ColumnsMinSpacing;          // Minimum horizontal spacing between two columns. Preferably > (FramePadding.x + 1).
    [FieldOffset(132)] public float       ScrollbarSize;              // Width of the vertical scrollbar, Height of the horizontal scrollbar.
    [FieldOffset(136)] public float       ScrollbarRounding;          // Radius of grab corners for scrollbar.
    [FieldOffset(140)] public float       ScrollbarPadding;           // Padding of scrollbar grab within its frame (same for both axises).
    [FieldOffset(144)] public float       GrabMinSize;                // Minimum width/height of a grab box for slider/scrollbar.
    [FieldOffset(148)] public float       GrabRounding;               // Radius of grabs corners rounding. Set to 0.0f to have rectangular slider grabs.
    [FieldOffset(152)] public float       LogSliderDeadzone;          // The size in pixels of the dead-zone around zero on logarithmic sliders that cross zero.
    [FieldOffset(156)] public float       ImageBorderSize;            // Thickness of border around Image() calls.
    [FieldOffset(160)] public float       TabRounding;                // Radius of upper corners of a tab. Set to 0.0f to have rectangular tabs.
    [FieldOffset(164)] public float       TabBorderSize;              // Thickness of border around tabs.
    [FieldOffset(168)] public float       TabMinWidthBase;            // Minimum tab width, to make tabs larger than their contents. TabBar buttons are not affected.
    [FieldOffset(172)] public float       TabMinWidthShrink;          // Minimum tab width after shrinking, when using ImGuiTabBarFlags_FittingPolicyMixed policy.
    [FieldOffset(176)] public float       TabCloseButtonMinWidthSelected;     // -1: always visible. 0.0f: visible when hovered. >0.0f: visible when hovered if minimum width.
    [FieldOffset(180)] public float       TabCloseButtonMinWidthUnselected;   // -1: always visible. 0.0f: visible when hovered. >0.0f: visible when hovered if minimum width. FLT_MAX: never show close button when unselected.
    [FieldOffset(184)] public float       TabBarBorderSize;           // Thickness of tab-bar separator, which takes on the tab active color to denote focus.
    [FieldOffset(188)] public float       TabBarOverlineSize;         // Thickness of tab-bar overline, which highlights the selected tab-bar.
    [FieldOffset(192)] public float       TableAngledHeadersAngle;    // Angle of angled headers (supported values range from -50.0f degrees to +50.0f degrees).
    [FieldOffset(196)] public ImVec2      TableAngledHeadersTextAlign;// Alignment of angled headers within the cell
    [FieldOffset(204)] public ImGuiTreeNodeFlags TreeLinesFlags;      // Default way to draw lines connecting TreeNode hierarchy. ImGuiTreeNodeFlags_DrawLinesNone or ImGuiTreeNodeFlags_DrawLinesFull or ImGuiTreeNodeFlags_DrawLinesToNodes.
    [FieldOffset(208)] public float       TreeLinesSize;              // Thickness of outlines when using ImGuiTreeNodeFlags_DrawLines.
    [FieldOffset(212)] public float       TreeLinesRounding;          // Radius of lines connecting child nodes to the vertical line.
    [FieldOffset(216)] public ImGuiDir    ColorButtonPosition;        // Side of the color button in the ColorEdit4 widget (left/right). Defaults to ImGuiDir_Right.
    [FieldOffset(220)] public ImVec2      ButtonTextAlign;            // Alignment of button text when button is larger than text. Defaults to (0.5f, 0.5f) (centered).
    [FieldOffset(228)] public ImVec2      SelectableTextAlign;        // Alignment of selectable text. Defaults to (0.0f, 0.0f) (top-left aligned). It's generally important to keep this left-aligned if you want to lay multiple items on a same line.
    [FieldOffset(236)] public float       SeparatorTextBorderSize;    // Thickness of border in SeparatorText()
    [FieldOffset(240)] public ImVec2      SeparatorTextAlign;         // Alignment of text within the separator. Defaults to (0.0f, 0.5f) (left aligned, center).
    [FieldOffset(248)] public ImVec2      SeparatorTextPadding;       // Horizontal offset of text from each edge of the separator + spacing on other axis. Generally small values. .y is recommended to be == FramePadding.y.
    [FieldOffset(256)] public ImVec2      DisplayWindowPadding;       // Apply to regular windows: amount which we enforce to keep visible when moving near edges of your screen.
    [FieldOffset(264)] public ImVec2      DisplaySafeAreaPadding;     // Apply to every windows, menus, popups, tooltips: amount where we avoid displaying contents. Adjust if you cannot see the edges of your screen (e.g. on a TV where scaling has not been configured).
    [FieldOffset(272)] public byte        DockingNodeHasCloseButton;  // Docking node has their own CloseButton() to close all docked windows.
    [FieldOffset(276)] public float       DockingSeparatorSize;       // Thickness of resizing border between docked windows
    [FieldOffset(280)] public float       MouseCursorScale;           // Scale software rendered mouse cursor (when io.MouseDrawCursor is enabled). We apply per-monitor DPI scaling over this scale. May be removed later.
    [FieldOffset(284)] public byte        AntiAliasedLines;           // Enable anti-aliased lines/borders. Disable if you are really tight on CPU/GPU. Latched at the beginning of the frame (copied to ImDrawList).
    [FieldOffset(285)] public byte        AntiAliasedLinesUseTex;     // Enable anti-aliased lines/borders using textures where possible. Require backend to render with bilinear filtering (NOT point/nearest filtering). Latched at the beginning of the frame (copied to ImDrawList).
    [FieldOffset(286)] public byte        AntiAliasedFill;            // Enable anti-aliased edges around filled shapes (rounded rectangles, circles, etc.). Disable if you are really tight on CPU/GPU. Latched at the beginning of the frame (copied to ImDrawList).
    [FieldOffset(288)] public float       CurveTessellationTol;       // Tessellation tolerance when using PathBezierCurveTo() without a specific number of segments. Decrease for highly tessellated curves (higher quality, more polygons), increase to reduce quality.
    [FieldOffset(292)] public float       CircleTessellationMaxError; // Maximum error (in pixels) allowed when using AddCircle()/AddCircleFilled() or drawing rounded corner rectangles with no explicit segment count specified. Decrease for higher quality but more geometry.
    // Colors
    [FieldOffset(296)] public ImVec4      Colors_Base;   //[ImGuiCol_COUNT];

    // Behaviors
    // (It is possible to modify those fields mid-frame if specific behavior need it, unlike e.g. configuration fields in ImGuiIO)
    [FieldOffset(1272)] public float             HoverStationaryDelay;     // Delay for IsItemHovered(ImGuiHoveredFlags_Stationary). Time required to consider mouse stationary.
    [FieldOffset(1276)] public float             HoverDelayShort;          // Delay for IsItemHovered(ImGuiHoveredFlags_DelayShort). Usually used along with HoverStationaryDelay.
    [FieldOffset(1280)] public float             HoverDelayNormal;         // Delay for IsItemHovered(ImGuiHoveredFlags_DelayNormal). "
    [FieldOffset(1284)] public ImGuiHoveredFlags HoverFlagsForTooltipMouse;// Default flags when using IsItemHovered(ImGuiHoveredFlags_ForTooltip) or BeginItemTooltip()/SetItemTooltip() while using mouse.
    [FieldOffset(1288)] public ImGuiHoveredFlags HoverFlagsForTooltipNav;  // Default flags when using IsItemHovered(ImGuiHoveredFlags_ForTooltip) or BeginItemTooltip()/SetItemTooltip() while using keyboard/gamepad.
    // [Internal]
    [FieldOffset(1292)] public float       _MainScale;                 // FIXME-WIP: Reference scale, as applied by ScaleAllSizes().
    [FieldOffset(1296)] public float       _NextFrameFontSizeBase;     // FIXME: Temporary hack until we finish remaining work.
};

public unsafe struct ImGuiStylePtr
{
    private ImGuiStyle* _ptr;
    internal ImGuiStylePtr(ImGuiStyle* nativePtr)
    {
        _ptr = nativePtr;
    }
    internal ImGuiStyle* NativePtr => _ptr;

    public ref ImVec2 ItemSpacing => ref _ptr->ItemSpacing;
}
