using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MyMGui;

//-----------------------------------------------------------------------------
// [SECTION] ImGuiIO
//-----------------------------------------------------------------------------
// Communicate most settings and inputs/outputs to Dear ImGui using this structure.
// Access via ImGui::GetIO(). Read 'Programmer guide' section in .cpp file for general usage.
// It is generally expected that:
// - initialization: backends and user code writes to ImGuiIO.
// - main loop: backends writes to ImGuiIO, user code and imgui code reads from ImGuiIO.
//-----------------------------------------------------------------------------
// Also see ImGui::GetPlatformIO() and ImGuiPlatformIO struct for OS/platform related functions: clipboard, IME etc.
//-----------------------------------------------------------------------------

// [Internal] Storage used by IsKeyDown(), IsKeyPressed() etc functions.
// If prior to 1.87 you used io.KeysDownDuration[] (which was marked as internal), you should use GetKeyData(key)->DownDuration and *NOT* io.KeysData[key]->DownDuration.
[StructLayout(LayoutKind.Explicit,Pack = 1)]
public unsafe partial struct ImGuiKeyData
{
    [FieldOffset(0)] public byte        Down;               // True for if key is down
    [FieldOffset(4)] public float       DownDuration;       // Duration the key has been down (<0.0f: not pressed, 0.0f: just pressed, >0.0f: time held)
    [FieldOffset(8)] public float       DownDurationPrev;   // Last frame duration the key has been down
    [FieldOffset(12)] public float       AnalogValue;        // 0.0f..1.0f for gamepad values
};

[StructLayout(LayoutKind.Sequential,Pack = 1)]  // cant make explicit load
public unsafe partial struct ImVector<T> where T : unmanaged
{
    public int        Size;
    public int        Capacity;
    public T*         Data;

    public ref T this[int index] => ref *(Data + index);
}

[StructLayout(LayoutKind.Explicit,Pack = 1)]
public unsafe partial struct ImVec2
{
    public ImVec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator ImVec2(System.Numerics.Vector2 v) => new ImVec2(v.X, v.Y);
    public static implicit operator System.Numerics.Vector2(ImVec2 v) => new System.Numerics.Vector2(v.X, v.Y);

    [FieldOffset(0)] public float X;
    [FieldOffset(4)] public float Y;

    public static ImVec2 operator -(ImVec2 a, ImVec2 b) => new ImVec2(a.X - b.X, a.Y - b.Y);
    public static ImVec2 operator +(ImVec2 a, ImVec2 b) => new ImVec2(a.X + b.X, a.Y + b.Y);
}

[StructLayout(LayoutKind.Explicit,Pack = 1)]
public unsafe partial struct ImVec4
{
    public ImVec4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static implicit operator ImVec4(System.Numerics.Vector4 v) => new ImVec4(v.X, v.Y, v.Z, v.W);
    public static implicit operator System.Numerics.Vector4(ImVec4 v) => new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);

    [FieldOffset(0)] public float X;
    [FieldOffset(4)] public float Y;
    [FieldOffset(8)] public float Z;
    [FieldOffset(12)] public float W;
}

[StructLayout(LayoutKind.Sequential,Pack = 1)]
public unsafe partial struct ImFontAtlas_TODO
{
    public int Dummy;
}

[StructLayout(LayoutKind.Sequential,Pack = 1)]
public unsafe partial struct ImFont_TODO
{
    public int Dummy;
}

[StructLayout(LayoutKind.Sequential,Pack = 1)]
public unsafe partial struct ImFontConfig_TODO
{
    public int Dummy;
}

[StructLayout(LayoutKind.Explicit,Pack = 1)]
public unsafe partial struct ImGuiIO
{
    //------------------------------------------------------------------
    // Configuration                            // Default value
    //------------------------------------------------------------------

    [FieldOffset(0)] public ImGuiConfigFlags   ConfigFlags;             // = 0              // See ImGuiConfigFlags_ enum. Set by user/application. Keyboard/Gamepad navigation options, etc.
    [FieldOffset(4)] public ImGuiBackendFlags  BackendFlags;            // = 0              // See ImGuiBackendFlags_ enum. Set by backend (imgui_impl_xxx files or custom backend) to communicate features supported by the backend.
    [FieldOffset(8)] public ImVec2      DisplaySize;                    // <unset>          // Main display size, in pixels (generally == GetMainViewport()->Size). May change every frame.
    [FieldOffset(16)] public ImVec2      DisplayFramebufferScale;        // = (1, 1)         // For retina display or other situations where window coordinates are different from framebuffer coordinates. This generally ends up in ImDrawData::FramebufferScale.
    [FieldOffset(24)] public float       DeltaTime;                      // = 1.0f/60.0f     // Time elapsed since last frame, in seconds. May change every frame.
    [FieldOffset(28)] public float       IniSavingRate;                  // = 5.0f           // Minimum time between saving positions/sizes to .ini file, in seconds.
    [FieldOffset(32)] public byte* IniFilename;                    // = "imgui.ini"    // Path to .ini file (important: default "imgui.ini" is relative to current working dir!). Set NULL to disable automatic .ini loading/saving or if you want to manually call LoadIniSettingsXXX() / SaveIniSettingsXXX() functions.
    [FieldOffset(40)] public byte* LogFilename;                    // = "imgui_log.txt"// Path to .log file (default parameter to ImGui::LogToFile when no file is specified).
    [FieldOffset(48)] public void*       UserData;                       // = NULL           // Store your own data.
    // Font system
    [FieldOffset(56)] public ImFontAtlas_TODO* Fonts;                          // <auto>           // Font atlas: load, rasterize and pack one or more fonts into a single texture.
    [FieldOffset(64)] public ImFont_TODO* FontDefault;                    // = NULL           // Font to use on NewFrame(). Use NULL to uses Fonts->Fonts[0].
    [FieldOffset(72)] public byte        FontAllowUserScaling;           // = false          // [OBSOLETE] Allow user scaling text of individual window with CTRL+Wheel.

    // Keyboard/Gamepad Navigation options
    [FieldOffset(73)] public byte        ConfigNavSwapGamepadButtons;    // = false          // Swap Activate<>Cancel (A<>B) buttons, matching typical "Nintendo/Japanese style" gamepad layout.
    [FieldOffset(74)] public byte        ConfigNavMoveSetMousePos;       // = false          // Directional/tabbing navigation teleports the mouse cursor. May be useful on TV/console systems where moving a virtual mouse is difficult. Will update io.MousePos and set io.WantSetMousePos=true.
    [FieldOffset(75)] public byte        ConfigNavCaptureKeyboard;       // = true           // Sets io.WantCaptureKeyboard when io.NavActive is set.
    [FieldOffset(76)] public byte        ConfigNavEscapeClearFocusItem;  // = true           // Pressing Escape can clear focused item + navigation id/highlight. Set to false if you want to always keep highlight on.
    [FieldOffset(77)] public byte        ConfigNavEscapeClearFocusWindow;// = false          // Pressing Escape can clear focused window as well (super set of io.ConfigNavEscapeClearFocusItem).
    [FieldOffset(78)] public byte        ConfigNavCursorVisibleAuto;     // = true           // Using directional navigation key makes the cursor visible. Mouse click hides the cursor.
    [FieldOffset(79)] public byte        ConfigNavCursorVisibleAlways;   // = false          // Navigation cursor is always visible.

    // Docking options (when ImGuiConfigFlags_DockingEnable is set)
    [FieldOffset(80)] public byte        ConfigDockingNoSplit;           // = false          // Simplified docking mode: disable window splitting, so docking is limited to merging multiple windows together into tab-bars.
    [FieldOffset(81)] public byte        ConfigDockingWithShift;         // = false          // Enable docking with holding Shift key (reduce visual noise, allows dropping in wider space)
    [FieldOffset(82)] public byte        ConfigDockingAlwaysTabBar;      // = false          // [BETA] [FIXME: This currently creates regression with auto-sizing and general overhead] Make every single floating window display within a docking node.
    [FieldOffset(83)] public byte        ConfigDockingTransparentPayload;// = false          // [BETA] Make window or viewport transparent when docking and only display docking boxes on the target viewport. Useful if rendering of multiple viewport cannot be synced. Best used with ConfigViewportsNoAutoMerge.

    // Viewport options (when ImGuiConfigFlags_ViewportsEnable is set)
    [FieldOffset(84)] public byte        ConfigViewportsNoAutoMerge;     // = false;         // Set to make all floating imgui windows always create their own viewport. Otherwise, they are merged into the main host viewports when overlapping it. May also set ImGuiViewportFlags_NoAutoMerge on individual viewport.
    [FieldOffset(85)] public byte        ConfigViewportsNoTaskBarIcon;   // = false          // Disable default OS task bar icon flag for secondary viewports. When a viewport doesn't want a task bar icon, ImGuiViewportFlags_NoTaskBarIcon will be set on it.
    [FieldOffset(86)] public byte        ConfigViewportsNoDecoration;    // = true           // Disable default OS window decoration flag for secondary viewports. When a viewport doesn't want window decorations, ImGuiViewportFlags_NoDecoration will be set on it. Enabling decoration can create subsequent issues at OS levels (e.g. minimum window size).
    [FieldOffset(87)] public byte        ConfigViewportsNoDefaultParent; // = false          // Disable default OS parenting to main viewport for secondary viewports. By default, viewports are marked with ParentViewportId = <main_viewport>, expecting the platform backend to setup a parent/child relationship between the OS windows (some backend may ignore this). Set to true if you want the default to be 0, then all viewports will be top-level OS windows.
    [FieldOffset(88)] public byte        ConfigViewportsPlatformFocusSetsImGuiFocus;//= true // When a platform window is focused (e.g. using Alt+Tab, clicking Platform Title Bar), apply corresponding focus on imgui windows (may clear focus/active id from imgui windows location in other platform windows). In principle this is better enabled but we provide an opt-out, because some Linux window managers tend to eagerly focus windows (e.g. on mouse hover, or even a simple window pos/size change).
    // DPI/Scaling options
    // This may keep evolving during 1.92.x releases. Expect some turbulence.
    [FieldOffset(89)] public byte        ConfigDpiScaleFonts;            // = false          // [EXPERIMENTAL] Automatically overwrite style.FontScaleDpi when Monitor DPI changes. This will scale fonts but _NOT_ scale sizes/padding for now.
    [FieldOffset(90)] public byte        ConfigDpiScaleViewports;        // = false          // [EXPERIMENTAL] Scale Dear ImGui and Platform Windows when Monitor DPI changes.


    // Miscellaneous options
    // (you can visualize and interact with all options in 'Demo->Configuration')
    [FieldOffset(91)] public byte        MouseDrawCursor;                // = false          // Request ImGui to draw a mouse cursor for you (if you are on a platform without a mouse cursor). Cannot be easily renamed to 'io.ConfigXXX' because this is frequently used by backend implementations.
    [FieldOffset(92)] public byte        ConfigMacOSXBehaviors;          // = defined(__APPLE__) // Swap Cmd<>Ctrl keys + OS X style text editing cursor movement using Alt instead of Ctrl, Shortcuts using Cmd/Super instead of Ctrl, Line/Text Start and End using Cmd+Arrows instead of Home/End, Double click selects by word instead of selecting whole text, Multi-selection in lists uses Cmd/Super instead of Ctrl.
    [FieldOffset(93)] public byte        ConfigInputTrickleEventQueue;   // = true           // Enable input queue trickling: some types of events submitted during the same frame (e.g. button down + up) will be spread over multiple frames, improving interactions with low framerates.
    [FieldOffset(94)] public byte        ConfigInputTextCursorBlink;     // = true           // Enable blinking cursor (optional as some users consider it to be distracting).
    [FieldOffset(95)] public byte        ConfigInputTextEnterKeepActive; // = false          // [BETA] Pressing Enter will keep item active and select contents (single-line only).
    [FieldOffset(96)] public byte        ConfigDragClickToInputText;     // = false          // [BETA] Enable turning DragXXX widgets into text input with a simple mouse click-release (without moving). Not desirable on devices without a keyboard.
    [FieldOffset(97)] public byte        ConfigWindowsResizeFromEdges;   // = true           // Enable resizing of windows from their edges and from the lower-left corner. This requires ImGuiBackendFlags_HasMouseCursors for better mouse cursor feedback. (This used to be a per-window ImGuiWindowFlags_ResizeFromAnySide flag)
    [FieldOffset(98)] public byte        ConfigWindowsMoveFromTitleBarOnly;  // = false      // Enable allowing to move windows only when clicking on their title bar. Does not apply to windows without a title bar.
    [FieldOffset(99)] public byte        ConfigWindowsCopyContentsWithCtrlC; // = false      // [EXPERIMENTAL] CTRL+C copy the contents of focused window into the clipboard. Experimental because: (1) has known issues with nested Begin/End pairs (2) text output quality varies (3) text output is in submission order rather than spatial order.
    [FieldOffset(100)] public byte        ConfigScrollbarScrollByPage;    // = true           // Enable scrolling page by page when clicking outside the scrollbar grab. When disabled, always scroll to clicked location. When enabled, Shift+Click scrolls to clicked location.
    //pad3
    [FieldOffset(104)] public float       ConfigMemoryCompactTimer;       // = 60.0f          // Timer (in seconds) to free transient windows/tables memory buffers when unused. Set to -1.0f to disable.
    // Inputs Behaviors
    // (other variables, ones which are expected to be tweaked within UI code, are exposed in ImGuiStyle)
    [FieldOffset(108)] public float       MouseDoubleClickTime;           // = 0.30f          // Time for a double-click, in seconds.
    [FieldOffset(112)] public float       MouseDoubleClickMaxDist;        // = 6.0f           // Distance threshold to stay in to validate a double-click, in pixels.
    [FieldOffset(116)] public float       MouseDragThreshold;             // = 6.0f           // Distance threshold before considering we are dragging.
    [FieldOffset(120)] public float       KeyRepeatDelay;                 // = 0.275f         // When holding a key/button, time before it starts repeating, in seconds (for buttons in Repeat mode, etc.).
    [FieldOffset(124)] public float       KeyRepeatRate;                  // = 0.050f         // When holding a key/button, rate at which it repeats, in seconds.

    //------------------------------------------------------------------
    // Debug options
    //------------------------------------------------------------------

    // Options to configure Error Handling and how we handle recoverable errors [EXPERIMENTAL]
    // - Error recovery is provided as a way to facilitate:
    //    - Recovery after a programming error (native code or scripting language - the later tends to facilitate iterating on code while running).
    //    - Recovery after running an exception handler or any error processing which may skip code after an error has been detected.
    // - Error recovery is not perfect nor guaranteed! It is a feature to ease development.
    //   You not are not supposed to rely on it in the course of a normal application run.
    // - Functions that support error recovery are using IM_ASSERT_USER_ERROR() instead of IM_ASSERT().
    // - By design, we do NOT allow error recovery to be 100% silent. One of the three options needs to be checked!
    // - Always ensure that on programmers seats you have at minimum Asserts or Tooltips enabled when making direct imgui API calls!
    //   Otherwise it would severely hinder your ability to catch and correct mistakes!
    // Read https://github.com/ocornut/imgui/wiki/Error-Handling for details.
    // - Programmer seats: keep asserts (default), or disable asserts and keep error tooltips (new and nice!)
    // - Non-programmer seats: maybe disable asserts, but make sure errors are resurfaced (tooltips, visible log entries, use callback etc.)
    // - Recovery after error/exception: record stack sizes with ErrorRecoveryStoreState(), disable assert, set log callback (to e.g. trigger high-level breakpoint), recover with ErrorRecoveryTryToRecoverState(), restore settings.
    [FieldOffset(128)] public byte        ConfigErrorRecovery;                // = true       // Enable error recovery support. Some errors won't be detected and lead to direct crashes if recovery is disabled.
    [FieldOffset(129)] public byte        ConfigErrorRecoveryEnableAssert;    // = true       // Enable asserts on recoverable error. By default call IM_ASSERT() when returning from a failing IM_ASSERT_USER_ERROR()
    [FieldOffset(130)] public byte        ConfigErrorRecoveryEnableDebugLog;  // = true       // Enable debug log output on recoverable errors.
    [FieldOffset(131)] public byte        ConfigErrorRecoveryEnableTooltip;   // = true       // Enable tooltip on recoverable errors. The tooltip include a way to enable asserts if they were disabled.

    // Option to enable various debug tools showing buttons that will call the IM_DEBUG_BREAK() macro.
    // - The Item Picker tool will be available regardless of this being enabled, in order to maximize its discoverability.
    // - Requires a debugger being attached, otherwise IM_DEBUG_BREAK() options will appear to crash your application.
    //   e.g. io.ConfigDebugIsDebuggerPresent = ::IsDebuggerPresent() on Win32, or refer to ImOsIsDebuggerPresent() imgui_test_engine/imgui_te_utils.cpp for a Unix compatible version).
    [FieldOffset(132)] public byte        ConfigDebugIsDebuggerPresent;   // = false          // Enable various tools calling IM_DEBUG_BREAK().

    // Tools to detect code submitting items with conflicting/duplicate IDs
    // - Code should use PushID()/PopID() in loops, or append "##xx" to same-label identifiers.
    // - Empty label e.g. Button("") == same ID as parent widget/node. Use Button("##xx") instead!
    // - See FAQ https://github.com/ocornut/imgui/blob/master/docs/FAQ.md#q-about-the-id-stack-system
    [FieldOffset(133)] public byte        ConfigDebugHighlightIdConflicts;// = true           // Highlight and show an error message when multiple items have conflicting identifiers.
    [FieldOffset(134)] public byte        ConfigDebugHighlightIdConflictsShowItemPicker;//=true // Show "Item Picker" button in aforementioned popup.

    // Tools to test correct Begin/End and BeginChild/EndChild behaviors.
    // - Presently Begin()/End() and BeginChild()/EndChild() needs to ALWAYS be called in tandem, regardless of return value of BeginXXX()
    // - This is inconsistent with other BeginXXX functions and create confusion for many users.
    // - We expect to update the API eventually. In the meanwhile we provide tools to facilitate checking user-code behavior.
    [FieldOffset(135)] public byte        ConfigDebugBeginReturnValueOnce;// = false          // First-time calls to Begin()/BeginChild() will return false. NEEDS TO BE SET AT APPLICATION BOOT TIME if you don't want to miss windows.
    [FieldOffset(136)] public byte        ConfigDebugBeginReturnValueLoop;// = false          // Some calls to Begin()/BeginChild() will return false. Will cycle through window depths then repeat. Suggested use: add "io.ConfigDebugBeginReturnValue = io.KeyShift" in your main loop then occasionally press SHIFT. Windows should be flickering while running.

    // Option to deactivate io.AddFocusEvent(false) handling.
    // - May facilitate interactions with a debugger when focus loss leads to clearing inputs data.
    // - Backends may have other side-effects on focus loss, so this will reduce side-effects but not necessary remove all of them.
    [FieldOffset(137)] public byte        ConfigDebugIgnoreFocusLoss;     // = false          // Ignore io.AddFocusEvent(false), consequently not calling io.ClearInputKeys()/io.ClearInputMouse() in input processing.

    // Option to audit .ini data
    [FieldOffset(138)] public byte        ConfigDebugIniSettings;         // = false          // Save .ini data with extra comments (particularly helpful for Docking, but makes saving slower)

    //------------------------------------------------------------------
    // Platform Identifiers
    // (the imgui_impl_xxxx backend files are setting those up for you)
    //------------------------------------------------------------------

    // Nowadays those would be stored in ImGuiPlatformIO but we are leaving them here for legacy reasons.
    // Optional: Platform/Renderer backend name (informational only! will be displayed in About Window) + User data for backend/wrappers to store their own stuff.
    [FieldOffset(144)] public byte* BackendPlatformName;            // = NULL
    [FieldOffset(152)] public byte* BackendRendererName;            // = NULL
    [FieldOffset(160)] public void* BackendPlatformUserData;        // = NULL           // User data for platform backend
    [FieldOffset(168)] public void* BackendRendererUserData;        // = NULL           // User data for renderer backend
    [FieldOffset(176)] public void* BackendLanguageUserData;        // = NULL           // User data for non C++ programming language backend
    //------------------------------------------------------------------
    // Input - Call before calling NewFrame()
    //------------------------------------------------------------------

    // Input Functions - Declared elsewhere
    
    //------------------------------------------------------------------
    // Output - Updated by NewFrame() or EndFrame()/Render()
    // (when reading from the io.WantCaptureMouse, io.WantCaptureKeyboard flags to dispatch your inputs, it is
    //  generally easier and more correct to use their state BEFORE calling NewFrame(). See FAQ for details!)
    //------------------------------------------------------------------

    [FieldOffset(184)] public byte        WantCaptureMouse;                   // Set when Dear ImGui will use mouse inputs, in this case do not dispatch them to your main game/application (either way, always pass on mouse inputs to imgui). (e.g. unclicked mouse is hovering over an imgui window, widget is active, mouse was clicked over an imgui window, etc.). - Declared elsewhere - Declared elsewhere
    [FieldOffset(185)] public byte        WantCaptureKeyboard;                // Set when Dear ImGui will use keyboard inputs, in this case do not dispatch them to your main game/application (either way, always pass keyboard inputs to imgui). (e.g. InputText active, or an imgui window is focused and navigation is enabled, etc.).
    [FieldOffset(186)] public byte        WantTextInput;                      // Mobile/console: when set, you may display an on-screen keyboard. This is set by Dear ImGui when it wants textual keyboard input to happen (e.g. when a InputText widget is active).
    [FieldOffset(187)] public byte        WantSetMousePos;                    // MousePos has been altered, backend should reposition mouse on next frame. Rarely used! Set only when io.ConfigNavMoveSetMousePos is enabled.
    [FieldOffset(188)] public byte        WantSaveIniSettings;                // When manual .ini load/save is active (io.IniFilename == NULL), this will be set to notify your application that you can call SaveIniSettingsToMemory() and save yourself. Important: clear io.WantSaveIniSettings yourself after saving!
    [FieldOffset(189)] public byte        NavActive;                          // Keyboard/Gamepad navigation is currently allowed (will handle ImGuiKey_NavXXX events) = a window is focused and it doesn't use the ImGuiWindowFlags_NoNavInputs flag.
    [FieldOffset(190)] public byte        NavVisible;                         // Keyboard/Gamepad navigation highlight is visible and allowed (will handle ImGuiKey_NavXXX events).
    [FieldOffset(192)] public float       Framerate;                          // Estimate of application framerate (rolling average over 60 frames, based on io.DeltaTime), in frame per second. Solely for convenience. Slow applications may not want to use a moving average or may want to reset underlying buffers occasionally.
    [FieldOffset(196)] public int         MetricsRenderVertices;              // Vertices output during last call to Render()
    [FieldOffset(200)] public int         MetricsRenderIndices;               // Indices output during last call to Render() = number of triangles * 3
    [FieldOffset(204)] public int         MetricsRenderWindows;               // Number of visible windows
    [FieldOffset(208)] public int         MetricsActiveWindows;               // Number of active windows
    [FieldOffset(212)] public ImVec2      MouseDelta;                         // Mouse delta. Note that this is zero if either current or previous position are invalid (-FLT_MAX,-FLT_MAX), so a disappearing/reappearing mouse won't have a huge delta.
    //------------------------------------------------------------------
    // [Internal] Dear ImGui will maintain those fields. Forward compatibility not guaranteed!
    //------------------------------------------------------------------

    [FieldOffset(224)] public ImGuiContextPtr Ctx;                                     // Parent UI context (needs to be set explicitly by parent).

    // Main Input State
    // (this block used to be written by backend, since 1.87 it is best to NOT write to those directly, call the AddXXX functions above instead)
    // (reading from those variables is fair game, as they are extremely unlikely to be moving anywhere)
    [FieldOffset(232)] public ImVec2      MousePos;                           // Mouse position, in pixels. Set to ImVec2(-FLT_MAX, -FLT_MAX) if mouse is unavailable (on another screen, etc.)
    [FieldOffset(240)] public fixed byte        MouseDown[5];                       // Mouse buttons: 0=left, 1=right, 2=middle + extras (ImGuiMouseButton_COUNT == 5). Dear ImGui mostly uses left and right buttons. Other buttons allow us to track if the mouse is being used by your application + available to user as a convenience via IsMouse** API.
    [FieldOffset(248)] public float       MouseWheel;                         // Mouse wheel Vertical: 1 unit scrolls about 5 lines text. >0 scrolls Up, <0 scrolls Down. Hold SHIFT to turn vertical scroll into horizontal scroll.
    [FieldOffset(252)] public float       MouseWheelH;                        // Mouse wheel Horizontal. >0 scrolls Left, <0 scrolls Right. Most users don't have a mouse with a horizontal wheel, may not be filled by all backends.
    [FieldOffset(256)] public ImGuiMouseSource MouseSource;                   // Mouse actual input peripheral (Mouse/TouchScreen/Pen).
    [FieldOffset(260)] public uint     MouseHoveredViewport;               // (Optional) Modify using io.AddMouseViewportEvent(). With multi-viewports: viewport the OS mouse is hovering. If possible _IGNORING_ viewports with the ImGuiViewportFlags_NoInputs flag is much better (few backends can handle that). Set io.BackendFlags |= ImGuiBackendFlags_HasMouseHoveredViewport if you can provide this info. If you don't imgui will infer the value using the rectangles and last focused time of the viewports it knows about (ignoring other OS windows).
    [FieldOffset(264)] public byte        KeyCtrl;                            // Keyboard modifier down: Control
    [FieldOffset(265)] public byte        KeyShift;                           // Keyboard modifier down: Shift
    [FieldOffset(266)] public byte        KeyAlt;                             // Keyboard modifier down: Alt
    [FieldOffset(267)] public byte        KeySuper;                           // Keyboard modifier down: Cmd/Super/Windows

    // Other state maintained from data above + IO function calls
    [FieldOffset(268)] public ImGuiKey    KeyMods;                          // Key mods flags (any of ImGuiMod_Ctrl/ImGuiMod_Shift/ImGuiMod_Alt/ImGuiMod_Super flags, same as io.KeyCtrl/KeyShift/KeyAlt/KeySuper but merged into flags. Read-only, updated by NewFrame()
    //fixed ImGuiKeyData  KeysData[ImGuiKey_NamedKey_COUNT];// Key state for all known keys. Use IsKeyXXX() functions to access this.
    [FieldOffset(272)] public ImGuiKeyData KeysData_0;
    [FieldOffset(288)] public ImGuiKeyData KeysData_1;
    [FieldOffset(304)] public ImGuiKeyData KeysData_2;
    [FieldOffset(320)] public ImGuiKeyData KeysData_3;
    [FieldOffset(336)] public ImGuiKeyData KeysData_4;
    [FieldOffset(352)] public ImGuiKeyData KeysData_5;
    [FieldOffset(368)] public ImGuiKeyData KeysData_6;
    [FieldOffset(384)] public ImGuiKeyData KeysData_7;
    [FieldOffset(400)] public ImGuiKeyData KeysData_8;
    [FieldOffset(416)] public ImGuiKeyData KeysData_9;
    [FieldOffset(432)] public ImGuiKeyData KeysData_10;
    [FieldOffset(448)] public ImGuiKeyData KeysData_11;
    [FieldOffset(464)] public ImGuiKeyData KeysData_12;
    [FieldOffset(480)] public ImGuiKeyData KeysData_13;
    [FieldOffset(496)] public ImGuiKeyData KeysData_14;
    [FieldOffset(512)] public ImGuiKeyData KeysData_15;
    [FieldOffset(528)] public ImGuiKeyData KeysData_16;
    [FieldOffset(544)] public ImGuiKeyData KeysData_17;
    [FieldOffset(560)] public ImGuiKeyData KeysData_18;
    [FieldOffset(576)] public ImGuiKeyData KeysData_19;
    [FieldOffset(592)] public ImGuiKeyData KeysData_20;
    [FieldOffset(608)] public ImGuiKeyData KeysData_21;
    [FieldOffset(624)] public ImGuiKeyData KeysData_22;
    [FieldOffset(640)] public ImGuiKeyData KeysData_23;
    [FieldOffset(656)] public ImGuiKeyData KeysData_24;
    [FieldOffset(672)] public ImGuiKeyData KeysData_25;
    [FieldOffset(688)] public ImGuiKeyData KeysData_26;
    [FieldOffset(704)] public ImGuiKeyData KeysData_27;
    [FieldOffset(720)] public ImGuiKeyData KeysData_28;
    [FieldOffset(736)] public ImGuiKeyData KeysData_29;
    [FieldOffset(752)] public ImGuiKeyData KeysData_30;
    [FieldOffset(768)] public ImGuiKeyData KeysData_31;
    [FieldOffset(784)] public ImGuiKeyData KeysData_32;
    [FieldOffset(800)] public ImGuiKeyData KeysData_33;
    [FieldOffset(816)] public ImGuiKeyData KeysData_34;
    [FieldOffset(832)] public ImGuiKeyData KeysData_35;
    [FieldOffset(848)] public ImGuiKeyData KeysData_36;
    [FieldOffset(864)] public ImGuiKeyData KeysData_37;
    [FieldOffset(880)] public ImGuiKeyData KeysData_38;
    [FieldOffset(896)] public ImGuiKeyData KeysData_39;
    [FieldOffset(912)] public ImGuiKeyData KeysData_40;
    [FieldOffset(928)] public ImGuiKeyData KeysData_41;
    [FieldOffset(944)] public ImGuiKeyData KeysData_42;
    [FieldOffset(960)] public ImGuiKeyData KeysData_43;
    [FieldOffset(976)] public ImGuiKeyData KeysData_44;
    [FieldOffset(992)] public ImGuiKeyData KeysData_45;
    [FieldOffset(1008)] public ImGuiKeyData KeysData_46;
    [FieldOffset(1024)] public ImGuiKeyData KeysData_47;
    [FieldOffset(1040)] public ImGuiKeyData KeysData_48;
    [FieldOffset(1056)] public ImGuiKeyData KeysData_49;
    [FieldOffset(1072)] public ImGuiKeyData KeysData_50;
    [FieldOffset(1088)] public ImGuiKeyData KeysData_51;
    [FieldOffset(1104)] public ImGuiKeyData KeysData_52;
    [FieldOffset(1120)] public ImGuiKeyData KeysData_53;
    [FieldOffset(1136)] public ImGuiKeyData KeysData_54;
    [FieldOffset(1152)] public ImGuiKeyData KeysData_55;
    [FieldOffset(1168)] public ImGuiKeyData KeysData_56;
    [FieldOffset(1184)] public ImGuiKeyData KeysData_57;
    [FieldOffset(1200)] public ImGuiKeyData KeysData_58;
    [FieldOffset(1216)] public ImGuiKeyData KeysData_59;
    [FieldOffset(1232)] public ImGuiKeyData KeysData_60;
    [FieldOffset(1248)] public ImGuiKeyData KeysData_61;
    [FieldOffset(1264)] public ImGuiKeyData KeysData_62;
    [FieldOffset(1280)] public ImGuiKeyData KeysData_63;
    [FieldOffset(1296)] public ImGuiKeyData KeysData_64;
    [FieldOffset(1312)] public ImGuiKeyData KeysData_65;
    [FieldOffset(1328)] public ImGuiKeyData KeysData_66;
    [FieldOffset(1344)] public ImGuiKeyData KeysData_67;
    [FieldOffset(1360)] public ImGuiKeyData KeysData_68;
    [FieldOffset(1376)] public ImGuiKeyData KeysData_69;
    [FieldOffset(1392)] public ImGuiKeyData KeysData_70;
    [FieldOffset(1408)] public ImGuiKeyData KeysData_71;
    [FieldOffset(1424)] public ImGuiKeyData KeysData_72;
    [FieldOffset(1440)] public ImGuiKeyData KeysData_73;
    [FieldOffset(1456)] public ImGuiKeyData KeysData_74;
    [FieldOffset(1472)] public ImGuiKeyData KeysData_75;
    [FieldOffset(1488)] public ImGuiKeyData KeysData_76;
    [FieldOffset(1504)] public ImGuiKeyData KeysData_77;
    [FieldOffset(1520)] public ImGuiKeyData KeysData_78;
    [FieldOffset(1536)] public ImGuiKeyData KeysData_79;
    [FieldOffset(1552)] public ImGuiKeyData KeysData_80;
    [FieldOffset(1568)] public ImGuiKeyData KeysData_81;
    [FieldOffset(1584)] public ImGuiKeyData KeysData_82;
    [FieldOffset(1600)] public ImGuiKeyData KeysData_83;
    [FieldOffset(1616)] public ImGuiKeyData KeysData_84;
    [FieldOffset(1632)] public ImGuiKeyData KeysData_85;
    [FieldOffset(1648)] public ImGuiKeyData KeysData_86;
    [FieldOffset(1664)] public ImGuiKeyData KeysData_87;
    [FieldOffset(1680)] public ImGuiKeyData KeysData_88;
    [FieldOffset(1696)] public ImGuiKeyData KeysData_89;
    [FieldOffset(1712)] public ImGuiKeyData KeysData_90;
    [FieldOffset(1728)] public ImGuiKeyData KeysData_91;
    [FieldOffset(1744)] public ImGuiKeyData KeysData_92;
    [FieldOffset(1760)] public ImGuiKeyData KeysData_93;
    [FieldOffset(1776)] public ImGuiKeyData KeysData_94;
    [FieldOffset(1792)] public ImGuiKeyData KeysData_95;
    [FieldOffset(1808)] public ImGuiKeyData KeysData_96;
    [FieldOffset(1824)] public ImGuiKeyData KeysData_97;
    [FieldOffset(1840)] public ImGuiKeyData KeysData_98;
    [FieldOffset(1856)] public ImGuiKeyData KeysData_99;
    [FieldOffset(1872)] public ImGuiKeyData KeysData_100;
    [FieldOffset(1888)] public ImGuiKeyData KeysData_101;
    [FieldOffset(1904)] public ImGuiKeyData KeysData_102;
    [FieldOffset(1920)] public ImGuiKeyData KeysData_103;
    [FieldOffset(1936)] public ImGuiKeyData KeysData_104;
    [FieldOffset(1952)] public ImGuiKeyData KeysData_105;
    [FieldOffset(1968)] public ImGuiKeyData KeysData_106;
    [FieldOffset(1984)] public ImGuiKeyData KeysData_107;
    [FieldOffset(2000)] public ImGuiKeyData KeysData_108;
    [FieldOffset(2016)] public ImGuiKeyData KeysData_109;
    [FieldOffset(2032)] public ImGuiKeyData KeysData_110;
    [FieldOffset(2048)] public ImGuiKeyData KeysData_111;
    [FieldOffset(2064)] public ImGuiKeyData KeysData_112;
    [FieldOffset(2080)] public ImGuiKeyData KeysData_113;
    [FieldOffset(2096)] public ImGuiKeyData KeysData_114;
    [FieldOffset(2112)] public ImGuiKeyData KeysData_115;
    [FieldOffset(2128)] public ImGuiKeyData KeysData_116;
    [FieldOffset(2144)] public ImGuiKeyData KeysData_117;
    [FieldOffset(2160)] public ImGuiKeyData KeysData_118;
    [FieldOffset(2176)] public ImGuiKeyData KeysData_119;
    [FieldOffset(2192)] public ImGuiKeyData KeysData_120;
    [FieldOffset(2208)] public ImGuiKeyData KeysData_121;
    [FieldOffset(2224)] public ImGuiKeyData KeysData_122;
    [FieldOffset(2240)] public ImGuiKeyData KeysData_123;
    [FieldOffset(2256)] public ImGuiKeyData KeysData_124;
    [FieldOffset(2272)] public ImGuiKeyData KeysData_125;
    [FieldOffset(2288)] public ImGuiKeyData KeysData_126;
    [FieldOffset(2304)] public ImGuiKeyData KeysData_127;
    [FieldOffset(2320)] public ImGuiKeyData KeysData_128;
    [FieldOffset(2336)] public ImGuiKeyData KeysData_129;
    [FieldOffset(2352)] public ImGuiKeyData KeysData_130;
    [FieldOffset(2368)] public ImGuiKeyData KeysData_131;
    [FieldOffset(2384)] public ImGuiKeyData KeysData_132;
    [FieldOffset(2400)] public ImGuiKeyData KeysData_133;
    [FieldOffset(2416)] public ImGuiKeyData KeysData_134;
    [FieldOffset(2432)] public ImGuiKeyData KeysData_135;
    [FieldOffset(2448)] public ImGuiKeyData KeysData_136;
    [FieldOffset(2464)] public ImGuiKeyData KeysData_137;
    [FieldOffset(2480)] public ImGuiKeyData KeysData_138;
    [FieldOffset(2496)] public ImGuiKeyData KeysData_139;
    [FieldOffset(2512)] public ImGuiKeyData KeysData_140;
    [FieldOffset(2528)] public ImGuiKeyData KeysData_141;
    [FieldOffset(2544)] public ImGuiKeyData KeysData_142;
    [FieldOffset(2560)] public ImGuiKeyData KeysData_143;
    [FieldOffset(2576)] public ImGuiKeyData KeysData_144;
    [FieldOffset(2592)] public ImGuiKeyData KeysData_145;
    [FieldOffset(2608)] public ImGuiKeyData KeysData_146;
    [FieldOffset(2624)] public ImGuiKeyData KeysData_147;
    [FieldOffset(2640)] public ImGuiKeyData KeysData_148;
    [FieldOffset(2656)] public ImGuiKeyData KeysData_149;
    [FieldOffset(2672)] public ImGuiKeyData KeysData_150;
    [FieldOffset(2688)] public ImGuiKeyData KeysData_151;
    [FieldOffset(2704)] public ImGuiKeyData KeysData_152;
    [FieldOffset(2720)] public ImGuiKeyData KeysData_153;
    [FieldOffset(2736)] public ImGuiKeyData KeysData_154;
    [FieldOffset(2752)] public byte        WantCaptureMouseUnlessPopupClose;   // Alternative to WantCaptureMouse: (WantCaptureMouse == true && WantCaptureMouseUnlessPopupClose == false) when a click over void is expected to close a popup.
    [FieldOffset(2756)] public ImVec2      MousePosPrev;                       // Previous mouse position (note that MouseDelta is not necessary == MousePos-MousePosPrev, in case either position is invalid)
    //ImVec2      MouseClickedPos[5];                 // Position at time of clicking
    [FieldOffset(2764)] public ImVec2 MouseClickedPos_0;
    [FieldOffset(2772)] public ImVec2 MouseClickedPos_1;
    [FieldOffset(2780)] public ImVec2 MouseClickedPos_2;
    [FieldOffset(2788)] public ImVec2 MouseClickedPos_3;
    [FieldOffset(2796)] public ImVec2 MouseClickedPos_4;
    [FieldOffset(2808)] public fixed double MouseClickedTime[5];                // Time of last click (used to figure out double-click)
    [FieldOffset(2848)] public fixed byte   MouseClicked[5];                    // Mouse button went from !Down to Down (same as MouseClickedCount[x] != 0)
    [FieldOffset(2853)] public fixed byte   MouseDoubleClicked[5];              // Has mouse button been double-clicked? (same as MouseClickedCount[x] == 2)
    [FieldOffset(2858)] public fixed ushort MouseClickedCount[5];               // == 0 (not clicked), == 1 (same as MouseClicked[]), == 2 (double-clicked), == 3 (triple-clicked) etc. when going from !Down to Down
    [FieldOffset(2868)] public fixed ushort MouseClickedLastCount[5];           // Count successive number of clicks. Stays valid after mouse release. Reset after another click is done.
    [FieldOffset(2878)] public fixed byte   MouseReleased[5];                   // Mouse button went from Down to !Down
    [FieldOffset(2888)] public fixed double MouseReleasedTime[5];               // Time of last released (rarely used! but useful to handle delayed single-click when trying to disambiguate them from double-click).
    [FieldOffset(2928)] public fixed byte   MouseDownOwned[5];                  // Track if button was clicked inside a dear imgui window or over void blocked by a popup. We don't request mouse capture from the application if click started outside ImGui bounds.
    [FieldOffset(2933)] public fixed byte   MouseDownOwnedUnlessPopupClose[5];  // Track if button was clicked inside a dear imgui window.
    [FieldOffset(2938)] public byte MouseWheelRequestAxisSwap;          // On a non-Mac system, holding SHIFT requests WheelY to perform the equivalent of a WheelX event. On a Mac system this is already enforced by the system.
    [FieldOffset(2939)] public byte MouseCtrlLeftAsRightClick;          // (OSX) Set to true when the current click was a ctrl-click that spawned a simulated right click
    [FieldOffset(2940)] public fixed float MouseDownDuration[5];               // Duration the mouse button has been down (0.0f == just clicked)
    [FieldOffset(2960)] public fixed float MouseDownDurationPrev[5];           // Previous time the mouse button has been down
    //public ImVec2 MouseDragMaxDistanceAbs[5];         // Maximum distance, absolute, on each axis, of how much mouse has traveled from the clicking point
    [FieldOffset(2980)] public ImVec2 MouseDragMaxDistanceAbs_0;
    [FieldOffset(2988)] public ImVec2 MouseDragMaxDistanceAbs_1;
    [FieldOffset(2996)] public ImVec2 MouseDragMaxDistanceAbs_2;
    [FieldOffset(3004)] public ImVec2 MouseDragMaxDistanceAbs_3;
    [FieldOffset(3012)] public ImVec2 MouseDragMaxDistanceAbs_4; 
    [FieldOffset(3020)] public fixed float MouseDragMaxDistanceSqr[5];         // Squared maximum distance of how much mouse has traveled from the clicking point (used for moving thresholds)
    [FieldOffset(3040)] public float PenPressure;                        // Touch/Pen pressure (0.0f to 1.0f, should be >0.0f only when MouseDown[0] == true). Helper storage currently unused by Dear ImGui.
    [FieldOffset(3044)] public byte  AppFocusLost;                       // Only modify via AddFocusEvent()
    [FieldOffset(3045)] public byte  AppAcceptingEvents;                 // Only modify via SetAppAcceptingEvents()
    [FieldOffset(3046)] public ImWchar16 InputQueueSurrogate;                // For AddInputCharacterUTF16()
    [FieldOffset(3048)] public ImVector<ImWchar> InputQueueCharacters;         // Queue of _characters_ input (obtained by platform backend). Fill using AddInputCharacter() helper.
}

public unsafe struct ImGuiIOPtr
{
    private readonly ImGuiIO* ptr;

    public ImGuiIOPtr(ImGuiIO* nativePtr) { ptr = nativePtr; }

    public ref ImGuiConfigFlags ConfigFlags => ref Unsafe.AsRef<ImGuiConfigFlags>(&ptr->ConfigFlags);
    public ref ImGuiBackendFlags BackendFlags => ref Unsafe.AsRef<ImGuiBackendFlags>(&ptr->BackendFlags);
    public ref float DeltaTime => ref Unsafe.AsRef<float>(&ptr->DeltaTime);
    public ref ImVec2 DisplaySize => ref Unsafe.AsRef<ImVec2>(&ptr->DisplaySize);
    public ref ImVec2 DisplayFramebufferScale => ref Unsafe.AsRef<ImVec2>(&ptr->DisplayFramebufferScale);

    public ref ImVec2 MousePos => ref Unsafe.AsRef<ImVec2>(&ptr->MousePos);
    public ref bool MouseDrawCursor => ref Unsafe.AsRef<bool>(&ptr->MouseDrawCursor);

    public ref bool WantSetMousePos => ref Unsafe.AsRef<bool>(&ptr->WantSetMousePos);

    public ref bool ConfigWindowsMoveFromTitleBarOnly => ref Unsafe.AsRef<bool>(&ptr->ConfigWindowsMoveFromTitleBarOnly);
    public ref bool ConfigDpiScaleFonts => ref Unsafe.AsRef<bool>(&ptr->ConfigDpiScaleFonts);
    public ref bool ConfigDpiScaleViewports => ref Unsafe.AsRef<bool>(&ptr->ConfigDpiScaleViewports);

    public ImFontAtlasPtr Fonts => new ImFontAtlasPtr(ptr->Fonts);


    public void AddMousePosEvent(float x, float y)
    {
        ImGui.ImGuiIO_AddMousePosEvent(ptr, x, y);
    }

    public void AddMouseButtonEvent(ImGuiMouseButton button, bool down)
    {
        ImGui.ImGuiIO_AddMouseButtonEvent(ptr, button, down);
    }

    public void AddMouseWheelEvent(float wheelX, float wheelY)
    {
        ImGui.ImGuiIO_AddMouseWheelEvent(ptr, wheelX, wheelY);
    }

    public void AddFocusEvent(bool focused)
    {
        ImGui.ImGuiIO_AddFocusEvent(ptr, focused);
    }

    public void AddKeyEvent(ImGuiKey key, bool down)
    {
        ImGui.ImGuiIO_AddKeyEvent(ptr, key, down);
    }

    public void AddInputCharacter(uint c)
    {
        ImGui.ImGuiIO_AddInputCharacter(ptr, c);
    }

    public void AddKeyAnalogEvent(ImGuiKey key, bool down, float v)
    {
        ImGui.ImGuiIO_AddKeyAnalogEvent(ptr, key, down, v);
    }
}

public unsafe struct ImFontAtlasPtr
{
    private readonly ImFontAtlas_TODO* ptr;

    public ImFontAtlasPtr(ImFontAtlas_TODO* nativePtr) { ptr = nativePtr; }

    public ImFontAtlas_TODO* NativePtr => ptr;

    public void AddFontDefault()
    {
        ImGui.ImFontAtlas_AddFontDefault(ptr, null);
    }
}