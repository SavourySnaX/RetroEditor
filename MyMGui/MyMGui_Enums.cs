namespace MyMGui;

public enum ImGuiMouseCursor : int
{
    None = -1,
    Arrow = 0,
    TextInput = 1,
    ResizeAll = 2,
    ResizeNS = 3,
    ResizeEW = 4,
    ResizeNESW = 5,
    ResizeNWSE = 6,
    Hand = 7,
    NotAllowed = 8,
    COUNT
}

public enum ImGuiKey : int
{
    None = 0,
    NamedKey_BEGIN = 512,
    Tab = 512,
    LeftArrow = 513,
    RightArrow = 514,
    UpArrow = 515,
    DownArrow = 516,
    PageUp = 517,
    PageDown = 518,
    Home = 519,
    End = 520,
    Insert = 521,
    Delete = 522,
    Backspace = 523,
    Space = 524,
    Enter = 525,
    Escape = 526,
    LeftCtrl = 527,
    LeftShift = 528,
    LeftAlt = 529,
    LeftSuper = 530,
    RightCtrl = 531,
    RightShift = 532,
    RightAlt = 533,
    RightSuper = 534,
    Menu = 535,
    _0 = 536,
    _1 = 537,
    _2 = 538,
    _3 = 539,
    _4 = 540,
    _5 = 541,
    _6 = 542,
    _7 = 543,
    _8 = 544,
    _9 = 545,
    A = 546,
    B = 547,
    C = 548,
    D = 549,
    E = 550,
    F = 551,
    G = 552,
    H = 553,
    I = 554,
    J = 555,
    K = 556,
    L = 557,
    M = 558,
    N = 559,
    O = 560,
    P = 561,
    Q = 562,
    R = 563,
    S = 564,
    T = 565,
    U = 566,
    V = 567,
    W = 568,
    X = 569,
    Y = 570,
    Z = 571,
    F1 = 572,
    F2 = 573,
    F3 = 574,
    F4 = 575,
    F5 = 576,
    F6 = 577,
    F7 = 578,
    F8 = 579,
    F9 = 580,
    F10 = 581,
    F11 = 582,
    F12 = 583,
    F13 = 584,
    F14 = 585,
    F15 = 586,
    F16 = 587,
    F17 = 588,
    F18 = 589,
    F19 = 590,
    F20 = 591,
    F21 = 592,
    F22 = 593,
    F23 = 594,
    F24 = 595,
    Apostrophe = 596,
    Comma = 597,
    Minus = 598,
    Period = 599,
    Slash = 600,
    Semicolon = 601,
    Equal = 602,
    LeftBracket = 603,
    Backslash = 604,
    RightBracket = 605,
    GraveAccent = 606,
    CapsLock = 607,
    ScrollLock = 608,
    NumLock = 609,
    PrintScreen = 610,
    Pause = 611,
    Keypad0 = 612,
    Keypad1 = 613,
    Keypad2 = 614,
    Keypad3 = 615,
    Keypad4 = 616,
    Keypad5 = 617,
    Keypad6 = 618,
    Keypad7 = 619,
    Keypad8 = 620,
    Keypad9 = 621,
    KeypadDecimal = 622,
    KeypadDivide = 623,
    KeypadMultiply = 624,
    KeypadSubtract = 625,
    KeypadAdd = 626,
    KeypadEnter = 627,
    KeypadEqual = 628,
    AppBack = 629,
    AppForward = 630,
    GamepadStart = 631,
    GamepadBack = 632,
    GamepadFaceLeft = 633,
    GamepadFaceRight = 634,
    GamepadFaceUp = 635,
    GamepadFaceDown = 636,
    GamepadDpadLeft = 637,
    GamepadDpadRight = 638,
    GamepadDpadUp = 639,
    GamepadDpadDown = 640,
    GamepadL1 = 641,
    GamepadR1 = 642,
    GamepadL2 = 643,
    GamepadR2 = 644,
    GamepadL3 = 645,
    GamepadR3 = 646,
    GamepadLStickLeft = 647,
    GamepadLStickRight = 648,
    GamepadLStickUp = 649,
    GamepadLStickDown = 650,
    GamepadRStickLeft = 651,
    GamepadRStickRight = 652,
    GamepadRStickUp = 653,
    GamepadRStickDown = 654,
    MouseLeft = 655,
    MouseRight = 656,
    MouseMiddle = 657,
    MouseX1 = 658,
    MouseX2 = 659,
    MouseWheelX = 660,
    MouseWheelY = 661,
    ReservedForModCtrl = 662,
    ReservedForModShift = 663,
    ReservedForModAlt = 664,
    ReservedForModSuper = 665,
    NamedKey_END = 666,
    ModNone = 0,
    ModCtrl = 4096,
    ModShift = 8192,
    ModAlt = 16384,
    ModSuper = 32768,
    ModMask = 61440,
    NamedKey_COUNT = 154,
}

[Flags]
public enum ImGuiConfigFlags : int
{
    None                   = 0,
    NavEnableKeyboard      = 1 << 0,   // Master keyboard navigation enable flag. Enable full Tabbing + directional arrows + space/enter to activate.
    NavEnableGamepad       = 1 << 1,   // Master gamepad navigation enable flag. Backend also needs to set ImGuiBackendFlags_HasGamepad.
    NoMouse                = 1 << 4,   // Instruct dear imgui to disable mouse inputs and interactions.
    NoMouseCursorChange    = 1 << 5,   // Instruct backend to not alter mouse cursor shape and visibility. Use if the backend cursor changes are interfering with yours and you don't want to use SetMouseCursor() to change mouse cursor. You may want to honor requests from imgui by reading GetMouseCursor() yourself instead.
    NoKeyboard             = 1 << 6,   // Instruct dear imgui to disable keyboard inputs and interactions. This is done by ignoring keyboard events and clearing existing states.

    // [BETA] Docking
    DockingEnable          = 1 << 7,   // Docking enable flags.

    // [BETA] Viewports
    // When using viewports it is recommended that your default value for ImGuiCol_WindowBg is opaque (Alpha=1.0) so transition to a viewport won't be noticeable.
    ViewportsEnable        = 1 << 10,  // Viewport enable flags (require both ImGuiBackendFlags_PlatformHasViewports + ImGuiBackendFlags_RendererHasViewports set by the respective backends)

    // User storage (to allow your backend/engine to communicate to code that may be shared between multiple projects. Those flags are NOT used by core Dear ImGui)
    IsSRGB                 = 1 << 20,  // Application is SRGB-aware.
    IsTouchScreen          = 1 << 21,  // Application is using a touch screen instead of a mouse.
};

// Backend capabilities flags stored in io.BackendFlags. Set by imgui_impl_xxx or custom backend.
[Flags]
public enum ImGuiBackendFlags : int
{
    None                  = 0,
    HasGamepad            = 1 << 0,   // Backend Platform supports gamepad and currently has one connected.
    HasMouseCursors       = 1 << 1,   // Backend Platform supports honoring GetMouseCursor() value to change the OS cursor shape.
    HasSetMousePos        = 1 << 2,   // Backend Platform supports io.WantSetMousePos requests to reposition the OS mouse position (only used if io.ConfigNavMoveSetMousePos is set).
    RendererHasVtxOffset  = 1 << 3,   // Backend Renderer supports ImDrawCmd::VtxOffset. This enables output of large meshes (64K+ vertices) while still using 16-bit indices.
    RendererHasTextures   = 1 << 4,   // Backend Renderer supports ImTextureData requests to create/update/destroy textures. This enables incremental texture updates and texture reloads. See https://github.com/ocornut/imgui/blob/master/docs/BACKENDS.md for instructions on how to upgrade your custom backend.


    // [BETA] Viewports
    RendererHasViewports  = 1 << 10,  // Backend Renderer supports multiple viewports.
    PlatformHasViewports  = 1 << 11,  // Backend Platform supports multiple viewports.
    HasMouseHoveredViewport=1 << 12,  // Backend Platform supports calling io.AddMouseViewportEvent() with the viewport under the mouse. IF POSSIBLE, ignore viewports with the ImGuiViewportFlags_NoInputs flag (Win32 backend, GLFW 3.30+ backend can do this, SDL backend cannot). If this cannot be done, Dear ImGui needs to use a flawed heuristic to find the viewport under.
    HasParentViewport     = 1 << 13,  // Backend Platform supports honoring viewport->ParentViewport/ParentViewportId value, by applying the corresponding parent/child relation at the Platform level.
};

// Enumeration for AddMouseSourceEvent() actual source of Mouse Input data.
// Historically we use "Mouse" terminology everywhere to indicate pointer data, e.g. MousePos, IsMousePressed(), io.AddMousePosEvent()
// But that "Mouse" data can come from different source which occasionally may be useful for application to know about.
// You can submit a change of pointer type using io.AddMouseSourceEvent().
public enum ImGuiMouseSource : int
{
    ImGuiMouseSource_Mouse = 0,         // Input is coming from an actual mouse.
    ImGuiMouseSource_TouchScreen,       // Input is coming from a touch screen (no hovering prior to initial press, less precise initial press aiming, dual-axis wheeling possible).
    ImGuiMouseSource_Pen,               // Input is coming from a pressure/magnetic pen (often used in conjunction with high-sampling rates).
    ImGuiMouseSource_COUNT
};

public enum ImGuiMouseButton : int
{
    Left = 0,
    Right = 1,
    Middle = 2,
    COUNT = 5
}

[Flags]
public enum ImDrawListFlags : int
{
    None = 0,
    AntiAliasedLines = 1 << 0,
    AntiAliasedLinesUseTex = 1 << 1,
    AntiAliasedFill = 1 << 2,
    AllowVtxOffset = 1 << 3,
}

// Status of a texture to communicate with Renderer Backend.
public enum ImTextureStatus : int
{
    OK=0,
    Destroyed,      // Backend destroyed the texture.
    WantCreate,     // Requesting backend to create the texture. Set status OK when done.
    WantUpdates,    // Requesting backend to update specific blocks of pixels (write to texture portions which have never been used before). Set status OK when done.
    WantDestroy,    // Requesting backend to destroy the texture. Set status to Destroyed when done.
}

// We intentionally support a limited amount of texture formats to limit burden on CPU-side code and extension.
// Most standard backends only support RGBA32 but we provide a single channel option for low-resource/embedded systems.
public enum ImTextureFormat : int
{
    RGBA32=0,         // 4 components per pixel, each is unsigned 8-bit. Total size = TexWidth * TexHeight * 4
    Alpha8,         // 1 component per pixel, each is unsigned 8-bit. Total size = TexWidth * TexHeight
}

[Flags]
public enum ImGuiWindowFlags : int
{
    None                   = 0,
    NoTitleBar             = 1 << 0,   // Disable title-bar
    NoResize               = 1 << 1,   // Disable user resizing with the lower-right grip
    NoMove                 = 1 << 2,   // Disable user moving the window
    NoScrollbar            = 1 << 3,   // Disable scrollbars (window can still scroll with mouse or programmatically)
    NoScrollWithMouse      = 1 << 4,   // Disable user vertically scrolling with mouse wheel. On child window, mouse wheel will be forwarded to the parent unless NoScrollbar is also set.
    NoCollapse             = 1 << 5,   // Disable user collapsing window by double-clicking on it. Also referred to as Window Menu Button (e.g. within a docking node).
    AlwaysAutoResize       = 1 << 6,   // Resize every window to its content every frame
    NoBackground           = 1 << 7,   // Disable drawing background color (WindowBg, etc.) and outside border. Similar as using SetNextWindowBgAlpha(0.0f).
    NoSavedSettings        = 1 << 8,   // Never load/save settings in .ini file
    NoMouseInputs          = 1 << 9,   // Disable catching mouse, hovering test with pass through.
    MenuBar                = 1 << 10,  // Has a menu-bar
    HorizontalScrollbar    = 1 << 11,  // Allow horizontal scrollbar to appear (off by default). You may use SetNextWindowContentSize(ImVec2(width,0.0f)); prior to calling Begin() to specify width. Read code in imgui_demo in the "Horizontal Scrolling" section.
    NoFocusOnAppearing     = 1 << 12,  // Disable taking focus when transitioning from hidden to visible state
    NoBringToFrontOnFocus  = 1 << 13,  // Disable bringing window to front when taking focus (e.g. clicking on it or programmatically giving it focus)
    AlwaysVerticalScrollbar= 1 << 14,  // Always show vertical scrollbar (even if ContentSize.y < Size.y)
    AlwaysHorizontalScrollbar=1<< 15,  // Always show horizontal scrollbar (even if ContentSize.x < Size.x)
    NoNavInputs            = 1 << 16,  // No keyboard/gamepad navigation within the window
    NoNavFocus             = 1 << 17,  // No focusing toward this window with keyboard/gamepad navigation (e.g. skipped by CTRL+TAB)
    UnsavedDocument        = 1 << 18,  // Display a dot next to the title. When used in a tab/docking context, tab is selected when clicking the X + closure is not assumed (will wait for user to stop submitting the tab). Otherwise closure is assumed when pressing the X, so if you keep submitting the tab may reappear at end of tab bar.
    NoDocking              = 1 << 19,  // Disable docking of this window
    NoNav                  = NoNavInputs | NoNavFocus,
    NoDecoration           = NoTitleBar | NoResize | NoScrollbar | NoCollapse,
    NoInputs               = NoMouseInputs | NoNavInputs | NoNavFocus,
};

[Flags]
public enum ImGuiDockNodeFlags : int
{
    None = 0,
    KeepAliveOnly = 1 << 0,
    NoDockingOverCentralNode = 1 << 2,
    PassthruCentralNode = 1 << 3,
    NoDockingSplit = 1 << 4,
    NoResize = 1 << 5,
    AutoHideTabBar = 1 << 6,
    NoUndocking = 1 << 7,
}

[Flags]
public enum ImGuiViewportFlags : int
{
    None = 0,
    IsPlatformWindow = 1 << 0,
    IsPlatformMonitor = 1 << 1,
    OwnedByApp = 1 << 2,
    NoDecoration = 1 << 3,
    NoTaskBarIcon = 1 << 4,
    NoFocusOnAppearing = 1 << 5,
    NoFocusOnClick = 1 << 6,
    NoInputs = 1 << 7,
    NoRendererClear = 1 << 8,
    NoAutoMerge = 1 << 9,
    TopMost = 1 << 10,
    CanHostOtherWindows = 1 << 11,
    IsMinimized = 1 << 12,
    IsFocused = 1 << 13,
}

public enum ImGuiDir : int
{
    None = -1,
    Left = 0,
    Right = 1,
    Up = 2,
    Down = 3,
    COUNT = 4,
}

[Flags]
public enum ImGuiCond : int
{
    None = 0,
    Always = 1 << 0,
    Once = 1 << 1,
    FirstUseEver = 1 << 2,
    Appearing = 1 << 3,
}

[Flags]
public enum ImGuiPopupFlags : int
{
    None = 0,
    MouseButtonLeft = 0,
    MouseButtonRight = 1,
    MouseButtonMiddle = 2,
    MouseButtonMask_ = 0x1F,
    MouseButtonDefault_ = 1,
    NoReopen = 1 << 5,
    NoOpenOverExistingPopup = 1 << 7,
    NoOpenOverItems = 1 << 8,
    AnyPopupId = 1 << 10,
    AnyPopupLevel = 1 << 11,
    AnyPopup = AnyPopupId | AnyPopupLevel,
}

[Flags]
public enum ImGuiInputTextFlags : int
{
    None = 0,
    CharsDecimal = 1 << 0,
    CharsHexadecimal = 1 << 1,
    CharsScientific = 1 << 2,
    CharsUppercase = 1 << 3,
    CharsNoBlank = 1 << 4,
    AllowTabInput = 1 << 5,
    EnterReturnsTrue = 1 << 6,
    EscapeClearsAll = 1 << 7,
    CtrlEnterForNewLine = 1 << 8,
    ReadOnly = 1 << 9,
    Password = 1 << 10,
    AlwaysOverwrite = 1 << 11,
    AutoSelectAll = 1 << 12,
    ParseEmptyRefVal = 1 << 13,
    DisplayEmptyRefVal = 1 << 14,
    NoHorizontalScroll = 1 << 15,
    NoUndoRedo = 1 << 16,
    ElideLeft = 1 << 17,
    CallbackCompletion = 1 << 18,
    CallbackHistory = 1 << 19,
    CallbackAlways = 1 << 20,
    CallbackCharFilter = 1 << 21,
    CallbackResize = 1 << 22,
    CallbackEdit = 1 << 23,
    WordWrap = 1 << 24,
}

[Flags]
public enum ImGuiFocusedFlags : int
{
    None = 0,
    ChildWindows = 1 << 0,
    RootWindow = 1 << 1,
    AnyWindow = 1 << 2,
    NoPopupHierarchy = 1 << 3,
    DockHierarchy = 1 << 4,
    RootAndChildWindows = RootWindow | ChildWindows,
}

[Flags]
public enum ImGuiHoveredFlags : int
{
    None = 0,
    ChildWindows = 1 << 0,
    RootWindow = 1 << 1,
    AnyWindow = 1 << 2,
    NoPopupHierarchy = 1 << 3,
    DockHierarchy = 1 << 4,
    AllowWhenBlockedByPopup = 1 << 5,
    AllowWhenBlockedByActiveItem = 1 << 7,
    AllowWhenOverlappedByItem = 1 << 8,
    AllowWhenOverlappedByWindow = 1 << 9,
    AllowWhenDisabled = 1 << 10,
    NoNavOverride = 1 << 11,
    AllowWhenOverlapped = AllowWhenOverlappedByItem | AllowWhenOverlappedByWindow,
    RectOnly = AllowWhenBlockedByPopup | AllowWhenBlockedByActiveItem | AllowWhenOverlapped,
    RootAndChildWindows = RootWindow | ChildWindows,
    ForTooltip = 1 << 12,
    Stationary = 1 << 13,
    DelayNone = 1 << 14,
    DelayShort = 1 << 15,
    DelayNormal = 1 << 16,
    NoSharedDelay = 1 << 17,
}

[Flags]
public enum ImGuiListClipperFlags : int
{
    None = 0,
    NoSetTableRowCounters = 1 << 0,   // [Internal] Disabled modifying table row counters. Avoid assumption that 1 clipper item == 1 table row.
};

[Flags]
public enum ImGuiChildFlags : int
{
    None = 0,
    Borders = 1 << 0,
    AlwaysUseWindowPadding = 1 << 1,
    ResizeX = 1 << 2,
    ResizeY = 1 << 3,
    AutoResizeX = 1 << 4,
    AutoResizeY = 1 << 5,
    AlwaysAutoResize = 1 << 6,
    FrameStyle = 1 << 7,
    NavFlattened = 1 << 8,
}

[Flags]
public enum ImGuiSelectableFlags : int
{
    None = 0,
    NoAutoClosePopups = 1 << 0,
    SpanAllColumns = 1 << 1,
    AllowDoubleClick = 1 << 2,
    Disabled = 1 << 3,
    AllowOverlap = 1 << 4,
    Highlight = 1 << 5,
    SelectOnNav = 1 << 6,
}

[Flags]
public enum ImGuiTabItemFlags : int
{
    None = 0,
    UnsavedDocument = 1 << 0,
    SetSelected = 1 << 1,
    NoCloseWithMiddleMouseButton = 1 << 2,
    NoPushId = 1 << 3,
    NoTooltip = 1 << 4,
    NoReorder = 1 << 5,
    Leading = 1 << 6,
    Trailing = 1 << 7,
    NoAssumedClosure = 1 << 8,
}

[Flags]
public enum ImGuiTabBarFlags : int
{
    None = 0,
    Reorderable = 1 << 0,
    AutoSelectNewTabs = 1 << 1,
    TabListPopupButton = 1 << 2,
    NoCloseWithMiddleMouseButton = 1 << 3,
    NoTabListScrollingButtons = 1 << 4,
    NoTooltip = 1 << 5,
    DrawSelectedOverline = 1 << 6,
    FittingPolicyMixed = 1 << 7,
    FittingPolicyShrink = 1 << 8,
    FittingPolicyScroll = 1 << 9,
    FittingPolicyMask_ = FittingPolicyMixed | FittingPolicyShrink | FittingPolicyScroll,
    FittingPolicyDefault_ = FittingPolicyMixed,
}

public enum ImGuiCol : int
{
    Text,
    TextDisabled,
    WindowBg,
    ChildBg,
    PopupBg,
    Border,
    BorderShadow,
    FrameBg,
    FrameBgHovered,
    FrameBgActive,
    TitleBg,
    TitleBgActive,
    TitleBgCollapsed,
    MenuBarBg,
    ScrollbarBg,
    ScrollbarGrab,
    ScrollbarGrabHovered,
    ScrollbarGrabActive,
    CheckMark,
    SliderGrab,
    SliderGrabActive,
    Button,
    ButtonHovered,
    ButtonActive,
    Header,
    HeaderHovered,
    HeaderActive,
    Separator,
    SeparatorHovered,
    SeparatorActive,
    ResizeGrip,
    ResizeGripHovered,
    ResizeGripActive,
    InputTextCursor,
    TabHovered,
    Tab,
    TabSelected,
    TabSelectedOverline,
    TabDimmed,
    TabDimmedSelected,
    TabDimmedSelectedOverline,
    DockingPreview,
    DockingEmptyBg,
    PlotLines,
    PlotLinesHovered,
    PlotHistogram,
    PlotHistogramHovered,
    TableHeaderBg,
    TableBorderStrong,
    TableBorderLight,
    TableRowBg,
    TableRowBgAlt,
    TextLink,
    TextSelectedBg,
    TreeLines,
    DragDropTarget,
    UnsavedMarker,
    NavCursor,
    NavWindowingHighlight,
    NavWindowingDimBg,
    ModalWindowDimBg,
    COUNT,
}

public enum ImGuiStyleVar : int
{
    Alpha,
    DisabledAlpha,
    WindowPadding,
    WindowRounding,
    WindowBorderSize,
    WindowMinSize,
    WindowTitleAlign,
    ChildRounding,
    ChildBorderSize,
    PopupRounding,
    PopupBorderSize,
    FramePadding,
    FrameRounding,
    FrameBorderSize,
    ItemSpacing,
    ItemInnerSpacing,
    IndentSpacing,
    CellPadding,
    ScrollbarSize,
    ScrollbarRounding,
    ScrollbarPadding,
    GrabMinSize,
    GrabRounding,
    ImageBorderSize,
    TabRounding,
    TabBorderSize,
    TabMinWidthBase,
    TabMinWidthShrink,
    TabBarBorderSize,
    TabBarOverlineSize,
    TableAngledHeadersAngle,
    TableAngledHeadersTextAlign,
    TreeLinesSize,
    TreeLinesRounding,
    ButtonTextAlign,
    SelectableTextAlign,
    SeparatorTextBorderSize,
    SeparatorTextAlign,
    SeparatorTextPadding,
    DockingSeparatorSize,
    COUNT
}

[Flags]
public enum ImDrawFlags : int
{
    None = 0,
    Closed = 1 << 0,
    RoundCornersTopLeft = 1 << 4,
    RoundCornersTopRight = 1 << 5,
    RoundCornersBottomLeft = 1 << 6,
    RoundCornersBottomRight = 1 << 7,
    RoundCornersNone = 1 << 8,
    RoundCornersTop = RoundCornersTopLeft | RoundCornersTopRight,
    RoundCornersBottom = RoundCornersBottomLeft | RoundCornersBottomRight,
    RoundCornersLeft = RoundCornersBottomLeft | RoundCornersTopLeft,
    RoundCornersRight = RoundCornersBottomRight | RoundCornersTopRight,
    RoundCornersAll = RoundCornersTopLeft | RoundCornersTopRight | RoundCornersBottomLeft | RoundCornersBottomRight,
    RoundCornersDefault_ = RoundCornersAll,
    RoundCornersMask_ = RoundCornersAll | RoundCornersNone,
}

public enum ImGuiDataType : int
{
    S8,
    U8,
    S16,
    U16,
    S32,
    U32,
    S64,
    U64,
    Float,
    Double,
    Bool,
    String,
    COUNT
}

[Flags]
public enum ImGuiTableFlags : int
{
    None = 0,
    Resizable = 1 << 0,
    Reorderable = 1 << 1,
    Hideable = 1 << 2,
    Sortable = 1 << 3,
    NoSavedSettings = 1 << 4,
    ContextMenuInBody = 1 << 5,
    RowBg = 1 << 6,
    BordersInnerH = 1 << 7,
    BordersOuterH = 1 << 8,
    BordersInnerV = 1 << 9,
    BordersOuterV = 1 << 10,
    BordersH = BordersInnerH | BordersOuterH,
    BordersV = BordersInnerV | BordersOuterV,
    BordersInner = BordersInnerV | BordersInnerH,
    BordersOuter = BordersOuterV | BordersOuterH,
    Borders = BordersInner | BordersOuter,
    NoBordersInBody = 1 << 11,
    NoBordersInBodyUntilResize = 1 << 12,
    SizingFixedFit = 1 << 13,
    SizingFixedSame = 2 << 13,
    SizingStretchProp = 3 << 13,
    SizingStretchSame = 4 << 13,
    NoHostExtendX = 1 << 16,
    NoHostExtendY = 1 << 17,
    NoKeepColumnsVisible = 1 << 18,
    PreciseWidths = 1 << 19,
    NoClip = 1 << 20,
    PadOuterX = 1 << 21,
    NoPadOuterX = 1 << 22,
    NoPadInnerX = 1 << 23,
    ScrollX = 1 << 24,
    ScrollY = 1 << 25,
    SortMulti = 1 << 26,
    SortTristate = 1 << 27,
    HighlightHoveredColumn = 1 << 28,
    SizingMask_ = SizingFixedFit | SizingFixedSame | SizingStretchProp | SizingStretchSame,
}

[Flags]
public enum ImGuiTableColumnFlags : int
{
    None = 0,
    Disabled = 1 << 0,
    DefaultHide = 1 << 1,
    DefaultSort = 1 << 2,
    WidthStretch = 1 << 3,
    WidthFixed = 1 << 4,
    NoResize = 1 << 5,
    NoReorder = 1 << 6,
    NoHide = 1 << 7,
    NoClip = 1 << 8,
    NoSort = 1 << 9,
    NoSortAscending = 1 << 10,
    NoSortDescending = 1 << 11,
    NoHeaderLabel = 1 << 12,
    NoHeaderWidth = 1 << 13,
    PreferSortAscending = 1 << 14,
    PreferSortDescending = 1 << 15,
    IndentEnable = 1 << 16,
    IndentDisable = 1 << 17,
    AngledHeader = 1 << 18,
    IsEnabled = 1 << 24,
    IsVisible = 1 << 25,
    IsSorted = 1 << 26,
    IsHovered = 1 << 27,
    WidthMask_ = WidthStretch | WidthFixed,
    IndentMask_ = IndentEnable | IndentDisable,
    StatusMask_ = IsEnabled | IsVisible | IsSorted | IsHovered,
    NoDirectResize_ = 1 << 30,
}

[Flags]
public enum ImGuiTableRowFlags : int
{
    None = 0,
    Headers = 1 << 0,
}

[Flags]
public enum ImGuiTreeNodeFlags : int
{
    None = 0,
    Selected = 1 << 0,
    Framed = 1 << 1,
    AllowOverlap = 1 << 2,
    NoTreePushOnOpen = 1 << 3,
    NoAutoOpenOnLog = 1 << 4,
    DefaultOpen = 1 << 5,
    OpenOnDoubleClick = 1 << 6,
    OpenOnArrow = 1 << 7,
    Leaf = 1 << 8,
    Bullet = 1 << 9,
    FramePadding = 1 << 10,
    SpanAvailWidth = 1 << 11,
    SpanFullWidth = 1 << 12,
    SpanLabelWidth = 1 << 13,
    SpanAllColumns = 1 << 14,
    LabelSpanAllColumns = 1 << 15,
    NavLeftJumpsToParent = 1 << 17,
    CollapsingHeader = Framed | NoTreePushOnOpen | NoAutoOpenOnLog,
    DrawLinesNone = 1 << 18,
    DrawLinesFull = 1 << 19,
    DrawLinesToNodes = 1 << 20,
}

public enum ImGuiTableBgTarget : int
{
    None = 0,
    RowBg0 = 1,
    RowBg1 = 2,
    CellBg = 3,
}

[Flags]
public enum ImGuiSliderFlags : int
{
    None = 0,
    Logarithmic = 1 << 5,
    NoRoundToFormat = 1 << 6,
    NoInput = 1 << 7,
    WrapAround = 1 << 8,
    ClampOnInput = 1 << 9,
    ClampZeroRange = 1 << 10,
    NoSpeedTweaks = 1 << 11,
    AlwaysClamp = ClampOnInput | ClampZeroRange,
    InvalidMask_ = 0x7000000F,
}
