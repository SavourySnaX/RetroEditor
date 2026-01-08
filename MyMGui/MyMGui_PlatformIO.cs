using System.Runtime.InteropServices;

namespace MyMGui;


// - Currently represents the Platform Window created by the application which is hosting our Dear ImGui windows.
// - With multi-viewport enabled, we extend this concept to have multiple active viewports.
// - In the future we will extend this concept further to also represent Platform Monitor and support a "no main platform window" operation mode.
// - About Main Area vs Work Area:
//   - Main Area = entire viewport.
//   - Work Area = entire viewport minus sections used by main menu bars (for platform windows), or by task bar (for platform monitor).
//   - Windows are generally trying to stay within the Work Area of their host viewport.
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 112)]
public unsafe partial struct ImGuiViewport
{
    [FieldOffset(0)] public ImGuiID              ID;                     // Unique identifier for the viewport
    [FieldOffset(4)] public ImGuiViewportFlags   Flags;                  // See ImGuiViewportFlags_
    [FieldOffset(8)] public ImVec2               Pos;                    // Main Area: Position of the viewport (Dear ImGui coordinates are the same as OS desktop/native coordinates)
    [FieldOffset(16)] public ImVec2              Size;                   // Main Area: Size of the viewport.
    [FieldOffset(24)] public ImVec2              FramebufferScale;       // Density of the viewport for Retina display (always 1,1 on Windows, may be 2,2 etc on macOS/iOS). This will affect font rasterizer density.
    [FieldOffset(32)] public ImVec2              WorkPos;                // Work Area: Position of the viewport minus task bars, menus bars, status bars (>= Pos)
    [FieldOffset(40)] public ImVec2              WorkSize;               // Work Area: Size of the viewport minus task bars, menu bars, status bars (<= Size)
    [FieldOffset(48)] public float               DpiScale;               // 1.0f = 96 DPI = No extra scale.
    [FieldOffset(52)] public ImGuiID             ParentViewportId;       // (Advanced) 0: no parent. Instruct the platform backend to setup a parent/child relationship between platform windows.
    [FieldOffset(56)] public ImGuiViewportPtr    ParentViewport;         // (Advanced) Direct shortcut to ImGui::FindViewportByID(ParentViewportId). NULL: no parent.
    [FieldOffset(64)] internal ImDrawData*         DrawData;               // The ImDrawData corresponding to this viewport. Valid after Render() and until the next call to NewFrame().

    // Platform/Backend Dependent Data
    // Our design separate the Renderer and Platform backends to facilitate combining default backends with each others.
    // When our create your own backend for a custom engine, it is possible that both Renderer and Platform will be handled
    // by the same system and you may not need to use all the UserData/Handle fields.
    // The library never uses those fields, they are merely storage to facilitate backend implementation.
    [FieldOffset(72)] public void*               RendererUserData;       // void* to hold custom data structure for the renderer (e.g. swap chain, framebuffers etc.). generally set by your Renderer_CreateWindow function.
    [FieldOffset(80)] public void*               PlatformUserData;       // void* to hold custom data structure for the OS / platform (e.g. windowing info, render context). generally set by your Platform_CreateWindow function.
    [FieldOffset(88)] public void*               PlatformHandle;         // void* to hold higher-level, platform window handle (e.g. HWND for Win32 backend, Uint32 WindowID for SDL, GLFWWindow* for GLFW), for FindViewportByPlatformHandle().
    [FieldOffset(96)] public void*               PlatformHandleRaw;      // void* to hold lower-level, platform-native window handle (always HWND on Win32 platform, unused for other platforms).
    [FieldOffset(104)] public byte               PlatformWindowCreated;  // Platform window has been created (Platform_CreateWindow() has been called). This is false during the first frame where a viewport is being created.
    [FieldOffset(105)] public byte               PlatformRequestMove;    // Platform window requested move (e.g. window was moved by the OS / host window manager, authoritative position will be OS window position)
    [FieldOffset(106)] public byte               PlatformRequestResize;  // Platform window requested resize (e.g. window was resized by the OS / host window manager, authoritative size will be OS window size)
    [FieldOffset(107)] public byte               PlatformRequestClose;   // Platform window requested closure (e.g. window was moved by the OS / host window manager, e.g. pressing ALT-F4)
}

public unsafe struct ImGuiViewportPtr
{
    private ImGuiViewport* ptr;

    public ImGuiViewportPtr(ImGuiViewport* nativePtr) => ptr = nativePtr;
/*
    public ImGuiID ID => ptr->ID;
    public ImGuiViewportFlags Flags => ptr->Flags;
    public ImVec2 Pos => ptr->Pos;
    public ImVec2 Size => ptr->Size;
    public ImVec2 FramebufferScale => ptr->FramebufferScale;
    public ImVec2 WorkPos => ptr->WorkPos;
    public ImVec2 WorkSize => ptr->WorkSize;
    public float DpiScale => ptr->DpiScale;
    public ImGuiID ParentViewportId => ptr->ParentViewportId;
    public ImGuiViewportPtr ParentViewport => new ImGuiViewportPtr(ptr->ParentViewport);
    public ImDrawDataPtr DrawData => new ImDrawDataPtr(ptr->DrawData);
*/
    // Helpers
//    public ImVec2 GetCenter() => new ImVec2(ptr->Pos.x + ptr->Size.x * 0.5f, ptr->Pos.y + ptr->Size.y * 0.5f);
//    public ImVec2 GetWorkCenter() => new ImVec2(ptr->WorkPos.x + ptr->WorkSize.x * 0.5f, ptr->WorkPos.y + ptr->WorkSize.y * 0.5f);
}

// (Optional) Support for IME (Input Method Editor) via the platform_io.Platform_SetImeDataFn() function. Handler is called during EndFrame().
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 20)]
public unsafe partial struct ImGuiPlatformImeData
{
    [FieldOffset(0)] public byte    WantVisible;            // A widget wants the IME to be visible.
    [FieldOffset(1)] public byte    WantTextInput;          // A widget wants text input, not necessarily IME to be visible. This is automatically set to the upcoming value of io.WantTextInput.
    [FieldOffset(4)] public ImVec2  InputPos;               // Position of input cursor (for IME).
    [FieldOffset(12)] public float   InputLineHeight;        // Line height (for IME).
    [FieldOffset(16)] public ImGuiID ViewportId;             // ID of platform window/viewport.
};

public unsafe struct ImGuiPlatformImeDataPtr
{
    private ImGuiPlatformImeData* ptr;

    public ImGuiPlatformImeDataPtr(ImGuiPlatformImeData* nativePtr) => ptr = nativePtr;
}

// (Optional) This is required when enabling multi-viewport. Represent the bounds of each connected monitor/display and their DPI.
// We use this information for multiple DPI support + clamping the position of popups and tooltips so they don't straddle multiple monitors.
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 48)]
public unsafe partial struct ImGuiPlatformMonitor
{
    [FieldOffset(0)] public ImVec2  MainPos;
    [FieldOffset(8)] public ImVec2  MainSize;      // Coordinates of the area displayed on this monitor (Min = upper left, Max = bottom right)
    [FieldOffset(16)] public ImVec2  WorkPos;
    [FieldOffset(24)] public ImVec2  WorkSize;      // Coordinates without task bars / side bars / menu bars. Used to avoid positioning popups/tooltips inside this region. If you don't have this info, please copy the value for MainPos/MainSize.
    [FieldOffset(32)] public float   DpiScale;               // 1.0f = 96 DPI
    [FieldOffset(40)] public void*   PlatformHandle;         // Backend dependant data (e.g. HMONITOR, GLFWmonitor*, SDL Display Index, NSScreen*)
};


[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 328)]
public unsafe partial struct ImGuiPlatformIO
{
    [FieldOffset(0)] public delegate* unmanaged[Cdecl]<ImGuiContextPtr, byte*> Platform_GetClipboardTextFn;
    [FieldOffset(8)] public delegate* unmanaged[Cdecl]<ImGuiContextPtr, byte*, void> Platform_SetClipboardTextFn;
    [FieldOffset(16)] public void* Platform_ClipboardUserData;
    [FieldOffset(24)] public delegate* unmanaged[Cdecl]<ImGuiContextPtr, byte*, bool> Platform_OpenInShellFn;
    [FieldOffset(32)] public void* Platform_OpenInShellUserData;
    [FieldOffset(40)] public delegate* unmanaged[Cdecl]<ImGuiContextPtr, ImGuiViewportPtr, ImGuiPlatformImeDataPtr, void> Platform_SetImeDataFn;
    [FieldOffset(48)] public void* Platform_ImeUserData;
    [FieldOffset(56)] public ImWchar Platform_LocaleDecimalPoint;
    [FieldOffset(60)] public int Renderer_TextureMaxWidth;
    [FieldOffset(64)] public int Renderer_TextureMaxHeight;
    [FieldOffset(72)] public void* Renderer_RenderState;
    [FieldOffset(80)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Platform_CreateWindow;
    [FieldOffset(88)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Platform_DestroyWindow;
    [FieldOffset(96)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Platform_ShowWindow;
    [FieldOffset(104)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec2, void> Platform_SetWindowPos;
    [FieldOffset(112)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec2> Platform_GetWindowPos;
    [FieldOffset(120)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec2, void> Platform_SetWindowSize;
    [FieldOffset(128)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec2> Platform_GetWindowSize;
    [FieldOffset(136)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec2> Platform_GetWindowFramebufferScale;
    [FieldOffset(144)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Platform_SetWindowFocus;
    [FieldOffset(152)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, bool> Platform_GetWindowFocus;
    [FieldOffset(160)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, bool> Platform_GetWindowMinimized;
    [FieldOffset(168)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, byte*, void> Platform_SetWindowTitle;
    [FieldOffset(176)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, float, void> Platform_SetWindowAlpha;
    [FieldOffset(184)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Platform_UpdateWindow;
    [FieldOffset(192)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void*, void> Platform_RenderWindow;
    [FieldOffset(200)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void*, void> Platform_SwapBuffers;
    [FieldOffset(208)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, float> Platform_GetWindowDpiScale;
    [FieldOffset(216)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Platform_OnChangedViewport;
    [FieldOffset(224)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec4> Platform_GetWindowWorkAreaInsets;
    [FieldOffset(232)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ulong, void*, ulong*, int> Platform_CreateVkSurface;
    [FieldOffset(240)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Renderer_CreateWindow;
    [FieldOffset(248)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void> Renderer_DestroyWindow;
    [FieldOffset(256)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, ImVec2, void> Renderer_SetWindowSize;
    [FieldOffset(264)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void*, void> Renderer_RenderWindow;
    [FieldOffset(272)] public delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void*, void> Renderer_SwapBuffers;
    [FieldOffset(280)] public ImVector<ImGuiPlatformMonitor> Monitors;
    [FieldOffset(296)] public ImVector<ImTextureDataPtr> Textures;
    [FieldOffset(312)] public ImVector<ImGuiViewportPtr> Viewports;
}

public unsafe struct ImGuiPlatformIOPtr
{
    private ImGuiPlatformIO* ptr;

    public ImGuiPlatformIOPtr(ImGuiPlatformIO* nativePtr) => ptr = nativePtr;

    public ref delegate* unmanaged[Cdecl]<ImGuiContextPtr, byte*> Platform_GetClipboardTextFn => ref ptr->Platform_GetClipboardTextFn;
    public ref delegate* unmanaged[Cdecl]<ImGuiContextPtr, byte*, void> Platform_SetClipboardTextFn => ref ptr->Platform_SetClipboardTextFn;
    public ref void* Platform_ClipboardUserData => ref ptr->Platform_ClipboardUserData;

    public ref delegate* unmanaged[Cdecl]<ImGuiViewportPtr, float> Platform_GetWindowDpiScale => ref ptr->Platform_GetWindowDpiScale;
}