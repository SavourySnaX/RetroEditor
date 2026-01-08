/*
 I need more recent version of imgui to deal with some dpi issues.. 
 so since ImGui.NET is not being actively maintained, and I've already started 
 replacing calls for Arm64 support. I'm just going to get this going
*/
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MyMGui;

public readonly record struct ImGuiContextPtr(nint underlying);
public readonly record struct ImWchar16(ushort underlying);
public readonly record struct ImWchar(ImWchar16 underlying);
public readonly record struct ImGuiID(uint underlying);
public readonly record struct ImCol(uint underlying);

public unsafe static class ImGui
{
    public static ImCol InvertColor(this ImCol col) => new ImCol(0xFFFFFF00 ^ col.underlying);
    public static readonly ImTextureID ImTextureID_Invalid = new ImTextureID(0);

    static IntPtr cimguiHandle = IntPtr.Zero;
    static bool cimgui_warm64_workaround = false;

    static delegate* unmanaged[Cdecl]<ImFontAtlas_TODO*, ImGuiContextPtr> igCreateContext;
    static delegate* unmanaged[Cdecl]<ImGuiContextPtr, void> igDestryContext;
    static delegate* unmanaged[Cdecl]<ImGuiContextPtr, void> igSetCurrentContext;

    static delegate* unmanaged[Cdecl]<ImGuiIO*> igGetIO;
    static delegate* unmanaged[Cdecl]<ImGuiPlatformIOPtr> igGetPlatformIO;
    static delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<nuint, void*, void*>, delegate* unmanaged[Cdecl]<void*, void*, void>, void*, void> igSetAllocatorFunctions;
    static delegate* unmanaged[Cdecl]<ImDrawData*> igGetDrawData;
    static delegate* unmanaged[Cdecl]<ImDrawList*> igGetWindowDrawList;
    static delegate* unmanaged[Cdecl]<ImGuiStylePtr> igGetStyle;

    static delegate* unmanaged[Cdecl]<ImGuiStylePtr, void> igStyleColorsDark;
    static delegate* unmanaged[Cdecl]<ImGuiStylePtr, void> igStyleColorsLight;
    static delegate* unmanaged[Cdecl]<ImGuiStylePtr, void> igStyleColorsClassic;
    static delegate* unmanaged[Cdecl]<ImGuiID, void*, ImGuiDockNodeFlags, void*, ImGuiID> igDockSpaceOverViewport;

    static delegate* unmanaged[Cdecl]<ImVec4, ImCol> igColorConvertFloat4ToU32_X64;
    static delegate* unmanaged[Cdecl]<UInt64, UInt64, ImCol> igColorConvertFloat4ToU32_WArm64;
    static delegate* unmanaged[Cdecl]<ImVec4*, ImCol, void> igColorConvertU32ToFloat4;
    static delegate* unmanaged[Cdecl]<ImGuiCol, float, ImCol> igColorU32_Col;

    static delegate* unmanaged[Cdecl]<ImGuiMouseCursor> igGetMouseCursor;
    static delegate* unmanaged[Cdecl]<ImGuiMouseCursor, void> igSetMouseCursor;

    static delegate* unmanaged[Cdecl]<void> igNewFrame;
    static delegate* unmanaged[Cdecl]<void> igRender;
    static delegate* unmanaged[Cdecl]<float> igGetTextLineHeight;
    static delegate* unmanaged[Cdecl]<float> igGetTextLineHeightWithSpacing;
    static delegate* unmanaged[Cdecl]<float> igGetFrameHeight;
    static delegate* unmanaged[Cdecl]<float> igGetFrameHeightWithSpacing;

    static delegate* unmanaged[Cdecl]<byte*,byte*, ImGuiWindowFlags, byte> igBegin;
    static delegate* unmanaged[Cdecl]<void> igEnd;
    static delegate* unmanaged[Cdecl]<byte*, ImVec2, ImGuiChildFlags, ImGuiWindowFlags, byte> igBeginChild_X64;
    static delegate* unmanaged[Cdecl]<byte*, UInt64, ImGuiChildFlags, ImGuiWindowFlags, byte> igBeginChild_WArm64;
    static delegate* unmanaged[Cdecl]<void> igEndChild;
    static delegate* unmanaged[Cdecl]<byte> igBeginTooltip;
    static delegate* unmanaged[Cdecl]<void> igEndTooltip;


    static delegate* unmanaged[Cdecl]<byte*, byte*, void> igPushID_StrStr;
    static delegate* unmanaged[Cdecl]<void*, void> igPushID_Ptr;
    static delegate* unmanaged[Cdecl]<int, void> igPushID_Int;
    static delegate* unmanaged[Cdecl]<void> igPopID;

    // menu bars
    static delegate* unmanaged[Cdecl]<byte> igBeginMenuBar;
    static delegate* unmanaged[Cdecl]<void> igEndMenuBar;
    static delegate* unmanaged[Cdecl]<byte> igBeginMainMenuBar;
    static delegate* unmanaged[Cdecl]<void> igEndMainMenuBar;
    static delegate* unmanaged[Cdecl]<ImVec2, ImGuiCond, void> igSetNextWindowSize_X64;
    static delegate* unmanaged[Cdecl]<UInt64, ImGuiCond, void> igSetNextWindowSize_WArm64;

    // tab bars
    static delegate* unmanaged[Cdecl]<byte*, ImGuiTabBarFlags, byte> igBeginTabBar;
    static delegate* unmanaged[Cdecl]<void> igEndTabBar;
    static delegate* unmanaged[Cdecl]<byte*, byte*, ImGuiTabItemFlags, byte> igBeginTabItem;
    static delegate* unmanaged[Cdecl]<void> igEndTabItem;
    static delegate* unmanaged[Cdecl]<byte*, ImGuiTabItemFlags, byte> igTabItemButton;
    static delegate* unmanaged[Cdecl]<byte*, void> igSetTabItemClosed;

    // widgets
    static delegate* unmanaged[Cdecl]<byte*, ImVec2, byte> igButton_X64;
    static delegate* unmanaged[Cdecl]<byte*, UInt64, byte> igButton_WArm64;
    static delegate* unmanaged[Cdecl]<byte*, byte*, nint,  ImGuiInputTextFlags, nint, void*, byte> igInputText;
    static delegate* unmanaged[Cdecl]<byte*, byte*, byte*, nint, ImGuiInputTextFlags, nint, void*, byte> igInputTextWithHint;
    static delegate* unmanaged[Cdecl]<byte*, byte*, byte> igCheckbox;
    static delegate* unmanaged[Cdecl]<byte*, byte, ImGuiSelectableFlags, ImVec2, byte> igSelectable_X64;
    static delegate* unmanaged[Cdecl]<byte*, byte, ImGuiSelectableFlags, UInt64, byte> igSelectable_WArm64;
    static delegate* unmanaged[Cdecl]<byte*, int*, byte**, int, int, byte> igComboStrArr;
    static delegate* unmanaged[Cdecl]<byte*, ImGuiDataType, void*, void*, void*, byte*, ImGuiInputTextFlags, byte> igInputScalar;
    static delegate* unmanaged[Cdecl]<byte*, int*, int, int, byte*, ImGuiSliderFlags, byte> igSliderInt;

    // popups
    static delegate* unmanaged[Cdecl]<byte*, ImGuiPopupFlags, void> igOpenPopup;
    static delegate* unmanaged[Cdecl]<byte*, ImGuiWindowFlags, byte> igBeginPopup;
    static delegate* unmanaged[Cdecl]<byte*, byte*, ImGuiWindowFlags, byte> igBeginPopupModal;
    static delegate* unmanaged[Cdecl]<void> igEndPopup;
    static delegate* unmanaged[Cdecl]<void> igCloseCurrentPopup;

    // style
    static delegate* unmanaged[Cdecl]<byte, void> igBeginDisabled;
    static delegate* unmanaged[Cdecl]<void> igEndDisabled;
    static delegate* unmanaged[Cdecl]<ImGuiCol, ImCol, void> igPushStyleColor_U32;
    static delegate* unmanaged[Cdecl]<ImGuiCol, ImVec4, void> igPushStyleColor_Vec4_X64;
    static delegate* unmanaged[Cdecl]<ImGuiCol, UInt64, UInt64, void> igPushStyleColor_Vec4_WArm64;
    static delegate* unmanaged[Cdecl]<int, void> igPopStyleColor;
    static delegate* unmanaged[Cdecl]<ImGuiStyleVar, float, void> igPushStyleVar_Float;
    static delegate* unmanaged[Cdecl]<ImGuiStyleVar, ImVec2, void> igPushStyleVar_Vec2_X64;
    static delegate* unmanaged[Cdecl]<ImGuiStyleVar, UInt64, void> igPushStyleVar_Vec2_WArm64;
    static delegate* unmanaged[Cdecl]<ImGuiStyleVar, float, void> igPushStyleVarX;
    static delegate* unmanaged[Cdecl]<ImGuiStyleVar, float, void> igPushStyleVarY;
    static delegate* unmanaged[Cdecl]<int, void> igPopStyleVar;

    // menu
    static delegate* unmanaged[Cdecl]<byte*, byte, byte> igBeginMenu;
    static delegate* unmanaged[Cdecl]<void> igEndMenu;
    static delegate* unmanaged[Cdecl]<byte*, byte*, byte, byte, byte> igMenuItem_Bool;

    // layout helpers
    static delegate* unmanaged[Cdecl]<void> igSeparator;
    static delegate* unmanaged[Cdecl]<float, float, void> igSameLine;
    static delegate* unmanaged[Cdecl]<void> igNewLine;
    static delegate* unmanaged[Cdecl]<void> igSpacing;
    static delegate* unmanaged[Cdecl]<ImVec2, void> igDummy_X64;
    static delegate* unmanaged[Cdecl]<UInt64, void> igDummy_WArm64;

    // text
    static delegate* unmanaged[Cdecl]<byte*, byte*, void> igTextUnformatted;
    static delegate* unmanaged[Cdecl]<byte*, byte*, void> igLabelText;
    static delegate* unmanaged[Cdecl]<ImVec2*, byte*,byte*,byte,float, void> igCalcTextSize;

    // image
    static delegate* unmanaged[Cdecl]<ImTextureRef, ImVec2, ImVec2, ImVec2, void> igImage_X64;
    static delegate* unmanaged[Cdecl]<UInt64, UInt64, UInt64, UInt64, UInt64, void> igImage_WArm64;

    //tables
    static delegate* unmanaged[Cdecl]<int, byte*, byte, void> igColumns;
    static delegate* unmanaged[Cdecl]<void> igNextColumn;
    static delegate* unmanaged[Cdecl]<int> igGetColumnIndex;
    static delegate* unmanaged[Cdecl]<int, float> igGetColumnWidth;
    static delegate* unmanaged[Cdecl]<int, float, void> igSetColumnWidth;
    static delegate* unmanaged[Cdecl]<int, float> igGetColumnOffset;
    static delegate* unmanaged[Cdecl]<int, float, void> igSetColumnOffset;
    static delegate* unmanaged[Cdecl]<int> igGetColumnsCount;
    static delegate* unmanaged[Cdecl]<byte*, int, ImGuiTableFlags, ImVec2, float, byte> igBeginTable_X64;
    static delegate* unmanaged[Cdecl]<byte*, int, ImGuiTableFlags, UInt64, float, byte> igBeginTable_WArm64;
    static delegate* unmanaged[Cdecl]<void> igEndTable;
    static delegate* unmanaged[Cdecl]<byte*, ImGuiTableColumnFlags,float,ImGuiID, void> igTableSetupColumn;
    static delegate* unmanaged[Cdecl]<ImGuiTableRowFlags, float, void> igTableNextRow;
    static delegate* unmanaged[Cdecl]<byte> igTableNextColumn;
    static delegate* unmanaged[Cdecl]<int, byte> igTableSetColumnIndex;
    static delegate* unmanaged[Cdecl]<ImGuiTableBgTarget,ImCol,int, void> igTableSetBgColor;

    // status
    static delegate* unmanaged[Cdecl]<byte> igIsWindowAppearing;
    static delegate* unmanaged[Cdecl]<byte> igIsWindowCollapsed;
    static delegate* unmanaged[Cdecl]<ImGuiFocusedFlags, byte> igIsWindowFocused;
    static delegate* unmanaged[Cdecl]<ImGuiHoveredFlags, byte> igIsWindowHovered;
    static delegate* unmanaged[Cdecl]<ImGuiKey, byte> igIsKeyDown;
    static delegate* unmanaged[Cdecl]<ImGuiKey, byte, byte> igIsKeyPressed;
    static delegate* unmanaged[Cdecl]<ImGuiKey, byte> igIsKeyReleased;
    static delegate* unmanaged[Cdecl]<ImGuiHoveredFlags, byte> igIsItemHovered;
    static delegate* unmanaged[Cdecl]<byte> igIsItemActive;
    static delegate* unmanaged[Cdecl]<byte> igIsItemFocused;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte> igIsItemClicked;
    static delegate* unmanaged[Cdecl]<byte> igIsItemVisible;
    static delegate* unmanaged[Cdecl]<byte> igIsItemEdited;
    static delegate* unmanaged[Cdecl]<byte> igIsItemActivated;
    static delegate* unmanaged[Cdecl]<byte> igIsItemDeactivated;
    static delegate* unmanaged[Cdecl]<byte> igIsItemDeactivatedAfterEdit;
    static delegate* unmanaged[Cdecl]<byte> igIsItemToggledOpen;
    static delegate* unmanaged[Cdecl]<byte> igIsAnyItemHovered;
    static delegate* unmanaged[Cdecl]<byte> igIsAnyItemActive;
    static delegate* unmanaged[Cdecl]<byte> igIsAnyItemFocused;
    static delegate* unmanaged[Cdecl]<ImVec2*, void> igGetCursorPos;
    static delegate* unmanaged[Cdecl]<float> igGetCursorPosX;
    static delegate* unmanaged[Cdecl]<float> igGetCursorPosY;
    static delegate* unmanaged[Cdecl]<ImVec2, void> igSetCursorPos_X64;
    static delegate* unmanaged[Cdecl]<UInt64, void> igSetCursorPos_WArm64;
    static delegate* unmanaged[Cdecl]<float, void> igSetCursorPosX;
    static delegate* unmanaged[Cdecl]<float, void> igSetCursorPosY;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte> igIsMouseDown;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte, byte> igIsMouseClicked;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte> igIsMouseReleased;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte> igIsMouseDoubleClicked;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, float, byte> igIsMouseReleasedWithDelay;
    static delegate* unmanaged[Cdecl]<ImGuiMouseButton, int> igGetMouseClickedCount;
    static delegate* unmanaged[Cdecl]<ImVec2, ImVec2, byte, byte> igIsMouseHoveringRect_X64;
    static delegate* unmanaged[Cdecl]<UInt64, UInt64, byte, byte> igIsMouseHoveringRect_WArm64;
    static delegate* unmanaged[Cdecl]<ImVec2*, byte> igIsMousePosValid;
    static delegate* unmanaged[Cdecl]<byte> igIsAnyMouseDown;
    static delegate* unmanaged[Cdecl]<ImVec2*, void> igGetMousePos;

 
    static delegate* unmanaged[Cdecl]<ImVec2*,void> igGetWindowPos;
    static delegate* unmanaged[Cdecl]<ImVec2*,void> igGetWindowSize;

    // scroll
    static delegate* unmanaged[Cdecl]<float> igGetScrollX;
    static delegate* unmanaged[Cdecl]<float> igGetScrollY;
    static delegate* unmanaged[Cdecl]<float, void> igSetScrollX_Float;
    static delegate* unmanaged[Cdecl]<float, void> igSetScrollY_Float;
    static delegate* unmanaged[Cdecl]<float> igGetScrollMaxX;
    static delegate* unmanaged[Cdecl]<float> igGetScrollMaxY;
    static delegate* unmanaged[Cdecl]<float, void> igSetScrollHereX;
    static delegate* unmanaged[Cdecl]<float, void> igSetScrollHereY;
    static delegate* unmanaged[Cdecl]<float, float, void> igSetScrollFromPosX_Float;
    static delegate* unmanaged[Cdecl]<float, float, void> igSetScrollFromPosY_Float;

    // cursor
    static delegate* unmanaged[Cdecl]<ImVec2*, void> igGetCursorScreenPos;
    static delegate* unmanaged[Cdecl]<ImVec2, void> igSetCursorScreenPos_X64;
    static delegate* unmanaged[Cdecl]<UInt64, void> igSetCursorScreenPos_WArm64;
    static delegate* unmanaged[Cdecl]<ImVec2*, void> igGetContentRegionAvail;


    // fonts
    static delegate* unmanaged[Cdecl]<ImFontAtlas_TODO*, ImFontConfig_TODO*, ImFont_TODO*> igImFontAtlas_AddFontDefault; 
    static delegate* unmanaged[Cdecl]<ImGuiViewportPtr> igGetMainViewport;

    // IO functions
    static delegate* unmanaged[Cdecl]<ImGuiIO*, float, float, void> igImGuiIO_AddMousePosEvent;
    static delegate* unmanaged[Cdecl]<ImGuiIO*, ImGuiMouseButton, byte, void> igImGuiIO_AddMouseButtonEvent;
    static delegate* unmanaged[Cdecl]<ImGuiIO*, float, float, void> igImGuiIO_AddMouseWheelEvent;
    static delegate* unmanaged[Cdecl]<ImGuiIO*, byte, void> igImGuiIO_AddFocusEvent;
    static delegate* unmanaged[Cdecl]<ImGuiIO*, ImGuiKey, byte, void> igImGuiIO_AddKeyEvent;
    static delegate* unmanaged[Cdecl]<ImGuiIO*, uint, void> igImGuiIO_AddInputCharacter;
    static delegate* unmanaged[Cdecl]<ImGuiIO*, ImGuiKey, byte, float, void> igImGuiIO_AddKeyAnalogEvent;


    // TextureData functions
    static delegate* unmanaged[Cdecl]<ImTextureData*, void*> igImTextureData_GetPixels;
    static delegate* unmanaged[Cdecl]<ImTextureData*, int, int, void*> igImTextureData_GetPixelsAt;
    static delegate* unmanaged[Cdecl]<ImTextureData*, int> igImTextureData_GetSizeInBytes;
    static delegate* unmanaged[Cdecl]<ImTextureData*, int> igImTextureData_GetPitch;
    static delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureRef*, void> igImTextureData_GetTexRef;
    static delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureID> igImTextureData_GetTexID;
    static delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureID, void> igImTextureData_SetTexID; 
    static delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureStatus, void> igImTextureData_SetStatus;

    // DrawList functions
    static delegate* unmanaged[Cdecl]<ImDrawList*,ImVec2 ,ImVec2 ,ImCol ,float ,ImDrawFlags, void> ImDrawList_AddRectFilled_X64;
    static delegate* unmanaged[Cdecl]<ImDrawList*,UInt64 ,UInt64 ,ImCol ,float ,ImDrawFlags, void> ImDrawList_AddRectFilled_WArm64;
    static delegate* unmanaged[Cdecl]<ImDrawList*, ImVec2, ImCol, byte*, byte*, void> ImDrawList_AddText_X64;
    static delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, ImCol, byte*, byte*, void> ImDrawList_AddText_WArm64;
    static delegate* unmanaged[Cdecl]<ImDrawList*,ImVec2 ,ImVec2 ,ImCol ,float ,ImDrawFlags, float, void> ImDrawList_AddRect_X64;
    static delegate* unmanaged[Cdecl]<ImDrawList*,UInt64 ,UInt64 ,ImCol ,float ,ImDrawFlags, float, void> ImDrawList_AddRect_WArm64;
    static delegate* unmanaged[Cdecl]<ImDrawList*,ImTextureRef ,ImVec2 ,ImVec2 ,ImVec2 ,ImVec2 ,ImCol ,void> ImDrawList_AddImage_X64;
    static delegate* unmanaged[Cdecl]<ImDrawList*, ImTextureRef, UInt64, UInt64, UInt64, UInt64, ImCol, void> ImDrawList_AddImage_WArm64;
    static delegate* unmanaged[Cdecl]<ImDrawList*, ImVec2, ImVec2, byte, void> ImDrawList_PushClipRect_X64;
    static delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, byte, void> ImDrawList_PushClipRect_WArm64;
    static delegate* unmanaged[Cdecl]<ImDrawList*,void> _ImDrawList_PushClipRectFullScreen;
    static delegate* unmanaged[Cdecl]<ImDrawList*,void> _ImDrawList_PopClipRect;

    // listclipper
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*> igImGuiListClipper_ImGuiListClipper;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, void> igImGuiListClipper_destroy;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, float, void> igImGuiListClipper_Begin;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, void> igImGuiListClipper_End;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, byte> igImGuiListClipper_Step;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, void> igImGuiListClipper_IncludeItemByIndex;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, int, void> igImGuiListClipper_IncludeItemsByIndex;
    static delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, void> igImGuiListClipper_SeekCursorForItem;

    // Internals
    static delegate* unmanaged[Cdecl]<byte*, ImGuiViewportPtr, ImGuiDir, float, ImGuiWindowFlags, byte> igBeginViewportSideBar;


    static ImGui()
    {
        var version = GetVersion();
        if (version!="1.92.4")
        {
            throw new InvalidOperationException($"CImgui version mismatch. Expected 1.92.4 but got {version}");
        }
        if (Environment.GetEnvironmentVariable("OVERRIDE_CIMGUI_PATH") is string overridePath && !string.IsNullOrEmpty(overridePath))
        {
            cimguiHandle = NativeLibrary.Load(overridePath);
        }
        else
        {
            cimguiHandle = NativeLibrary.Load("cimgui", typeof(ImGui).Assembly, DllImportSearchPath.AssemblyDirectory);
        }

        var method = NativeLibrary.GetExport(cimguiHandle, "igCreateContext");
        igCreateContext = (delegate* unmanaged[Cdecl]<ImFontAtlas_TODO*, ImGuiContextPtr>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igDestroyContext");
        igDestryContext = (delegate* unmanaged[Cdecl]<ImGuiContextPtr, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetCurrentContext");
        igSetCurrentContext = (delegate* unmanaged[Cdecl]<ImGuiContextPtr, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetIO_Nil");
        igGetIO = (delegate* unmanaged[Cdecl]<ImGuiIO*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetPlatformIO_Nil");
        igGetPlatformIO = (delegate* unmanaged[Cdecl]<ImGuiPlatformIOPtr>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetAllocatorFunctions");
        igSetAllocatorFunctions = (delegate* unmanaged[Cdecl]<void*,void*,void*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetDrawData");
        igGetDrawData = (delegate* unmanaged[Cdecl]<ImDrawData*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetWindowDrawList");
        igGetWindowDrawList = (delegate* unmanaged[Cdecl]<ImDrawList*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetStyle");
        igGetStyle = (delegate* unmanaged[Cdecl]<ImGuiStylePtr>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igStyleColorsDark");
        igStyleColorsDark = (delegate* unmanaged[Cdecl]<ImGuiStylePtr, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igStyleColorsLight");
        igStyleColorsLight = (delegate* unmanaged[Cdecl]<ImGuiStylePtr, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igStyleColorsClassic");
        igStyleColorsClassic = (delegate* unmanaged[Cdecl]<ImGuiStylePtr, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igDockSpaceOverViewport");
        igDockSpaceOverViewport = (delegate* unmanaged[Cdecl]<ImGuiID, void*, ImGuiDockNodeFlags, void*, ImGuiID>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igColorConvertFloat4ToU32");
        igColorConvertFloat4ToU32_X64 = (delegate* unmanaged[Cdecl]<ImVec4,ImCol>)method;
        igColorConvertFloat4ToU32_WArm64 = (delegate* unmanaged[Cdecl]<ulong,ulong,ImCol>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igColorConvertU32ToFloat4");
        igColorConvertU32ToFloat4 = (delegate* unmanaged[Cdecl]<ImVec4*,ImCol,void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetColorU32_Col");
        igColorU32_Col = (delegate* unmanaged[Cdecl]<ImGuiCol,float,ImCol>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igGetMouseCursor");
        igGetMouseCursor = (delegate* unmanaged[Cdecl]<ImGuiMouseCursor>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetMouseCursor");
        igSetMouseCursor = (delegate* unmanaged[Cdecl]<ImGuiMouseCursor, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igNewFrame");
        igNewFrame = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igRender");
        igRender = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetTextLineHeight");
        igGetTextLineHeight = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetTextLineHeightWithSpacing");
        igGetTextLineHeightWithSpacing = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetFrameHeight");
        igGetFrameHeight = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetFrameHeightWithSpacing");
        igGetFrameHeightWithSpacing = (delegate* unmanaged[Cdecl]<float>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igBegin");
        igBegin = (delegate* unmanaged[Cdecl]<byte*,byte*, ImGuiWindowFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEnd");
        igEnd = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginChild_Str");
        igBeginChild_X64 = (delegate* unmanaged[Cdecl]<byte*, ImVec2, ImGuiChildFlags, ImGuiWindowFlags, byte>)method;
        igBeginChild_WArm64 = (delegate* unmanaged[Cdecl]<byte*, UInt64, ImGuiChildFlags, ImGuiWindowFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndChild");
        igEndChild = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginTooltip");;
        igBeginTooltip = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndTooltip");
        igEndTooltip = (delegate* unmanaged[Cdecl]<void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igPushID_StrStr");
        igPushID_StrStr = (delegate* unmanaged[Cdecl]<byte*, byte*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushID_Ptr");
        igPushID_Ptr = (delegate* unmanaged[Cdecl]<void*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushID_Int");
        igPushID_Int = (delegate* unmanaged[Cdecl]<int, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPopID");
        igPopID = (delegate* unmanaged[Cdecl]<void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igBeginMenuBar");
        igBeginMenuBar = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndMenuBar");
        igEndMenuBar = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginMainMenuBar");
        igBeginMainMenuBar = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndMainMenuBar");
        igEndMainMenuBar = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetNextWindowSize");
        igSetNextWindowSize_X64 = (delegate* unmanaged[Cdecl]<ImVec2, ImGuiCond, void>)method;
        igSetNextWindowSize_WArm64 = (delegate* unmanaged[Cdecl]<UInt64, ImGuiCond, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igBeginTabBar");
        igBeginTabBar = (delegate* unmanaged[Cdecl]<byte*, ImGuiTabBarFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndTabBar");
        igEndTabBar = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginTabItem");
        igBeginTabItem = (delegate* unmanaged[Cdecl]<byte*, byte*, ImGuiTabItemFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndTabItem");
        igEndTabItem = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igTabItemButton");
        igTabItemButton = (delegate* unmanaged[Cdecl]<byte*, ImGuiTabItemFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetTabItemClosed");
        igSetTabItemClosed = (delegate* unmanaged[Cdecl]<byte*, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igButton");
        igButton_X64 = (delegate* unmanaged[Cdecl]<byte*, ImVec2, byte>)method;
        igButton_WArm64 = (delegate* unmanaged[Cdecl]<byte*, UInt64, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igInputText");
        igInputText = (delegate* unmanaged[Cdecl]<byte*, byte*, nint, ImGuiInputTextFlags, nint, void*, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igInputTextWithHint");
        igInputTextWithHint = (delegate* unmanaged[Cdecl]<byte*, byte*, byte*, nint, ImGuiInputTextFlags, nint, void*, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igCheckbox");
        igCheckbox = (delegate* unmanaged[Cdecl]<byte*, byte*, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSelectable_Bool");
        igSelectable_X64 = (delegate* unmanaged[Cdecl]<byte*, byte, ImGuiSelectableFlags, ImVec2, byte>)method;
        igSelectable_WArm64 = (delegate* unmanaged[Cdecl]<byte*, byte, ImGuiSelectableFlags, UInt64, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igCombo_Str_arr");
        igComboStrArr = (delegate* unmanaged[Cdecl]<byte*, int*, byte**, int, int, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igInputScalar");
        igInputScalar = (delegate* unmanaged[Cdecl]<byte*, ImGuiDataType, void*, void*, void*, byte*, ImGuiInputTextFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSliderInt");
        igSliderInt = (delegate* unmanaged[Cdecl]<byte*, int*, int, int, byte*, ImGuiSliderFlags, byte>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igOpenPopup_Str");
        igOpenPopup = (delegate* unmanaged[Cdecl]<byte*, ImGuiPopupFlags, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginPopup");
        igBeginPopup = (delegate* unmanaged[Cdecl]<byte*, ImGuiWindowFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginPopupModal");
        igBeginPopupModal = (delegate* unmanaged[Cdecl]<byte*, byte*, ImGuiWindowFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndPopup");
        igEndPopup = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igCloseCurrentPopup");
        igCloseCurrentPopup = (delegate* unmanaged[Cdecl]<void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igBeginDisabled");
        igBeginDisabled = (delegate* unmanaged[Cdecl]<byte, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndDisabled");
        igEndDisabled = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushStyleColor_U32");
        igPushStyleColor_U32 = (delegate* unmanaged[Cdecl]<ImGuiCol, ImCol, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushStyleColor_Vec4");
        igPushStyleColor_Vec4_X64 = (delegate* unmanaged[Cdecl]<ImGuiCol, ImVec4, void>)method;
        igPushStyleColor_Vec4_WArm64 = (delegate* unmanaged[Cdecl]<ImGuiCol, UInt64, UInt64, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPopStyleColor");
        igPopStyleColor = (delegate* unmanaged[Cdecl]<int, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushStyleVar_Float");
        igPushStyleVar_Float = (delegate* unmanaged[Cdecl]<ImGuiStyleVar, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushStyleVar_Vec2");
        igPushStyleVar_Vec2_X64 = (delegate* unmanaged[Cdecl]<ImGuiStyleVar, ImVec2, void>)method;
        igPushStyleVar_Vec2_WArm64 = (delegate* unmanaged[Cdecl]<ImGuiStyleVar, UInt64, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushStyleVarX");
        igPushStyleVarX = (delegate* unmanaged[Cdecl]<ImGuiStyleVar, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPushStyleVarY");
        igPushStyleVarY = (delegate* unmanaged[Cdecl]<ImGuiStyleVar, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igPopStyleVar");
        igPopStyleVar = (delegate* unmanaged[Cdecl]<int, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igBeginMenu");
        igBeginMenu = (delegate* unmanaged[Cdecl]<byte*, byte, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndMenu");
        igEndMenu = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igMenuItem_Bool");
        igMenuItem_Bool = (delegate* unmanaged[Cdecl]<byte*, byte*, byte, byte, byte>)method;


        method = NativeLibrary.GetExport(cimguiHandle, "igSeparator");
        igSeparator = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSameLine");
        igSameLine = (delegate* unmanaged[Cdecl]<float, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igNewLine");
        igNewLine = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSpacing");
        igSpacing = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igDummy");
        igDummy_X64 = (delegate* unmanaged[Cdecl]<ImVec2, void>)method;
        igDummy_WArm64 = (delegate* unmanaged[Cdecl]<UInt64, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igTextUnformatted");
        igTextUnformatted = (delegate* unmanaged[Cdecl]<byte*, byte*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igLabelText");
        igLabelText = (delegate* unmanaged[Cdecl]<byte*, byte*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igCalcTextSize");
        igCalcTextSize = (delegate* unmanaged[Cdecl]<ImVec2*, byte*,byte*,byte,float, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igImage");
        igImage_X64 = (delegate* unmanaged[Cdecl]<ImTextureRef, ImVec2, ImVec2, ImVec2, void>)method;
        igImage_WArm64 = (delegate* unmanaged[Cdecl]<UInt64, UInt64, UInt64, UInt64, UInt64, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igColumns");
        igColumns = (delegate* unmanaged[Cdecl]<int, byte*, byte, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igNextColumn");
        igNextColumn = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetColumnIndex");
        igGetColumnIndex = (delegate* unmanaged[Cdecl]<int>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetColumnWidth");
        igGetColumnWidth = (delegate* unmanaged[Cdecl]<int, float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetColumnWidth");
        igSetColumnWidth = (delegate* unmanaged[Cdecl]<int, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetColumnOffset");
        igGetColumnOffset = (delegate* unmanaged[Cdecl]<int, float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetColumnOffset");
        igSetColumnOffset = (delegate* unmanaged[Cdecl]<int, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetColumnsCount");
        igGetColumnsCount = (delegate* unmanaged[Cdecl]<int>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igBeginTable");
        igBeginTable_X64 = (delegate* unmanaged[Cdecl]<byte*, int, ImGuiTableFlags, ImVec2, float, byte>)method;
        igBeginTable_WArm64 = (delegate* unmanaged[Cdecl]<byte*, int, ImGuiTableFlags, UInt64, float, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igEndTable");
        igEndTable = (delegate* unmanaged[Cdecl]<void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igTableSetupColumn");;
        igTableSetupColumn = (delegate* unmanaged[Cdecl]<byte*, ImGuiTableColumnFlags,float,ImGuiID, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igTableNextRow");;
        igTableNextRow = (delegate* unmanaged[Cdecl]<ImGuiTableRowFlags, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igTableNextColumn");
        igTableNextColumn = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igTableSetColumnIndex");
        igTableSetColumnIndex = (delegate* unmanaged[Cdecl]<int, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igTableSetBgColor");
        igTableSetBgColor = (delegate* unmanaged[Cdecl]<ImGuiTableBgTarget,ImCol,int, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igIsWindowAppearing");
        igIsWindowAppearing = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsWindowCollapsed");
        igIsWindowCollapsed = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsWindowFocused");
        igIsWindowFocused = (delegate* unmanaged[Cdecl]<ImGuiFocusedFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsWindowHovered");
        igIsWindowHovered = (delegate* unmanaged[Cdecl]<ImGuiHoveredFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsKeyDown_Nil");
        igIsKeyDown = (delegate* unmanaged[Cdecl]<ImGuiKey, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsKeyPressed_Bool");
        igIsKeyPressed = (delegate* unmanaged[Cdecl]<ImGuiKey, byte, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsKeyReleased_Nil");
        igIsKeyReleased = (delegate* unmanaged[Cdecl]<ImGuiKey, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemHovered");
        igIsItemHovered = (delegate* unmanaged[Cdecl]<ImGuiHoveredFlags, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemActive");
        igIsItemActive = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemFocused");
        igIsItemFocused = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemClicked");
        igIsItemClicked = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemVisible");
        igIsItemVisible = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemEdited");
        igIsItemEdited = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemActivated");
        igIsItemActivated = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemDeactivated");
        igIsItemDeactivated = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemDeactivatedAfterEdit");
        igIsItemDeactivatedAfterEdit = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsItemToggledOpen");
        igIsItemToggledOpen = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsAnyItemHovered");
        igIsAnyItemHovered = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsAnyItemActive");
        igIsAnyItemActive = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsAnyItemFocused");
        igIsAnyItemFocused = (delegate* unmanaged[Cdecl]<byte>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igGetCursorPos");
        igGetCursorPos = (delegate* unmanaged[Cdecl]<ImVec2*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetCursorPosX");
        igGetCursorPosX = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetCursorPosY");
        igGetCursorPosY = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetCursorPos");
        igSetCursorPos_X64 = (delegate* unmanaged[Cdecl]<ImVec2, void>)method;
        igSetCursorPos_WArm64 = (delegate* unmanaged[Cdecl]<UInt64, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetCursorPosX");
        igSetCursorPosX = (delegate* unmanaged[Cdecl]<float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetCursorPosY");
        igSetCursorPosY = (delegate* unmanaged[Cdecl]<float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMouseDown_Nil");
        igIsMouseDown = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMouseClicked_Bool");
        igIsMouseClicked = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMouseReleased_Nil");
        igIsMouseReleased = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMouseDoubleClicked_Nil");
        igIsMouseDoubleClicked = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMouseReleasedWithDelay");
        igIsMouseReleasedWithDelay = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, float, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetMouseClickedCount");
        igGetMouseClickedCount = (delegate* unmanaged[Cdecl]<ImGuiMouseButton, int>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMouseHoveringRect");
        igIsMouseHoveringRect_X64 = (delegate* unmanaged[Cdecl]<ImVec2, ImVec2, byte, byte>)method;
        igIsMouseHoveringRect_WArm64 = (delegate* unmanaged[Cdecl]<UInt64, UInt64, byte, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsMousePosValid");
        igIsMousePosValid = (delegate* unmanaged[Cdecl]<ImVec2*, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igIsAnyMouseDown");
        igIsAnyMouseDown = (delegate* unmanaged[Cdecl]<byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetMousePos");
        igGetMousePos = (delegate* unmanaged[Cdecl]<ImVec2*, void>)method;


        method = NativeLibrary.GetExport(cimguiHandle, "igGetWindowPos");
        igGetWindowPos = (delegate* unmanaged[Cdecl]<ImVec2*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetWindowSize");
        igGetWindowSize = (delegate* unmanaged[Cdecl]<ImVec2*, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igGetScrollX");
        igGetScrollX = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetScrollY");
        igGetScrollY = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetScrollX_Float");
        igSetScrollX_Float = (delegate* unmanaged[Cdecl]<float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetScrollY_Float");
        igSetScrollY_Float = (delegate* unmanaged[Cdecl]<float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetScrollMaxX");
        igGetScrollMaxX = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetScrollMaxY");
        igGetScrollMaxY = (delegate* unmanaged[Cdecl]<float>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetScrollHereX");
        igSetScrollHereX = (delegate* unmanaged[Cdecl]<float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetScrollHereY");
        igSetScrollHereY = (delegate* unmanaged[Cdecl]<float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetScrollFromPosX_Float");
        igSetScrollFromPosX_Float = (delegate* unmanaged[Cdecl]<float, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetScrollFromPosY_Float");
        igSetScrollFromPosY_Float = (delegate* unmanaged[Cdecl]<float, float, void>)method;
    
        method = NativeLibrary.GetExport(cimguiHandle, "igGetCursorScreenPos");
        igGetCursorScreenPos = (delegate* unmanaged[Cdecl]<ImVec2*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igSetCursorScreenPos");
        igSetCursorScreenPos_X64 = (delegate* unmanaged[Cdecl]<ImVec2, void>)method;
        igSetCursorScreenPos_WArm64 = (delegate* unmanaged[Cdecl]<UInt64, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetContentRegionAvail");
        igGetContentRegionAvail = (delegate* unmanaged[Cdecl]<ImVec2*, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "ImFontAtlas_AddFontDefault");
        igImFontAtlas_AddFontDefault = (delegate* unmanaged[Cdecl]<ImFontAtlas_TODO*, ImFontConfig_TODO*, ImFont_TODO*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "igGetMainViewport");
        igGetMainViewport = (delegate* unmanaged[Cdecl]<ImGuiViewportPtr>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddMousePosEvent");
        igImGuiIO_AddMousePosEvent = (delegate* unmanaged[Cdecl]<ImGuiIO*, float, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddMouseButtonEvent");
        igImGuiIO_AddMouseButtonEvent = (delegate* unmanaged[Cdecl]<ImGuiIO*, ImGuiMouseButton, byte, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddMouseWheelEvent");
        igImGuiIO_AddMouseWheelEvent = (delegate* unmanaged[Cdecl]<ImGuiIO*, float, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddFocusEvent");
        igImGuiIO_AddFocusEvent = (delegate* unmanaged[Cdecl]<ImGuiIO*, byte, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddKeyEvent");
        igImGuiIO_AddKeyEvent = (delegate* unmanaged[Cdecl]<ImGuiIO*, ImGuiKey, byte, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddInputCharacter");
        igImGuiIO_AddInputCharacter = (delegate* unmanaged[Cdecl]<ImGuiIO*, uint, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiIO_AddKeyAnalogEvent");
        igImGuiIO_AddKeyAnalogEvent = (delegate* unmanaged[Cdecl]<ImGuiIO*, ImGuiKey, byte, float, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_GetPixels");
        igImTextureData_GetPixels = (delegate* unmanaged[Cdecl]<ImTextureData*, void*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_GetPixelsAt");
        igImTextureData_GetPixelsAt = (delegate* unmanaged[Cdecl]<ImTextureData*, int, int, void*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_GetSizeInBytes");
        igImTextureData_GetSizeInBytes = (delegate* unmanaged[Cdecl]<ImTextureData*, int>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_GetPitch");
        igImTextureData_GetPitch = (delegate* unmanaged[Cdecl]<ImTextureData*, int>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_GetTexRef");
        igImTextureData_GetTexRef = (delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureRef*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_GetTexID");
        igImTextureData_GetTexID = (delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureID>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_SetTexID");
        igImTextureData_SetTexID = (delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureID, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImTextureData_SetStatus");
        igImTextureData_SetStatus = (delegate* unmanaged[Cdecl]<ImTextureData*, ImTextureStatus, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddRectFilled");
        ImDrawList_AddRectFilled_X64 = (delegate* unmanaged[Cdecl]<ImDrawList*,ImVec2 ,ImVec2 ,ImCol ,float ,ImDrawFlags, void>)method;
        ImDrawList_AddRectFilled_WArm64 = (delegate* unmanaged[Cdecl]<ImDrawList*,UInt64 ,UInt64 ,ImCol ,float ,ImDrawFlags, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddText_Vec2");;
        ImDrawList_AddText_X64 = (delegate* unmanaged[Cdecl]<ImDrawList*, ImVec2, ImCol, byte*, byte*, void>)method;
        ImDrawList_AddText_WArm64 = (delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, ImCol, byte*, byte*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddRect");
        ImDrawList_AddRect_X64 = (delegate* unmanaged[Cdecl]<ImDrawList*,ImVec2 ,ImVec2 ,ImCol ,float ,ImDrawFlags, float, void>)method;
        ImDrawList_AddRect_WArm64 = (delegate* unmanaged[Cdecl]<ImDrawList*,UInt64 ,UInt64 ,ImCol ,float ,ImDrawFlags, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddImage");
        ImDrawList_AddImage_X64 = (delegate* unmanaged[Cdecl]<ImDrawList*, ImTextureRef, ImVec2, ImVec2, ImVec2, ImVec2, ImCol, void>)method;
        ImDrawList_AddImage_WArm64 = (delegate* unmanaged[Cdecl]<ImDrawList*, ImTextureRef, UInt64, UInt64, UInt64, UInt64, ImCol, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_PushClipRect");
        ImDrawList_PushClipRect_X64 = (delegate* unmanaged[Cdecl]<ImDrawList*, ImVec2, ImVec2, byte, void>)method;
        ImDrawList_PushClipRect_WArm64 = (delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, byte, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_PushClipRectFullScreen");
        _ImDrawList_PushClipRectFullScreen = (delegate* unmanaged[Cdecl]<ImDrawList*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_PopClipRect");
        _ImDrawList_PopClipRect = (delegate* unmanaged[Cdecl]<ImDrawList*, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_ImGuiListClipper");
        igImGuiListClipper_ImGuiListClipper = (delegate* unmanaged[Cdecl]<ImGuiListClipper*>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_destroy");
        igImGuiListClipper_destroy = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_Begin");
        igImGuiListClipper_Begin = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, float, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_End");
        igImGuiListClipper_End = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_Step");
        igImGuiListClipper_Step = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, byte>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_IncludeItemByIndex");
        igImGuiListClipper_IncludeItemByIndex = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_IncludeItemsByIndex");
        igImGuiListClipper_IncludeItemsByIndex = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, int, void>)method;
        method = NativeLibrary.GetExport(cimguiHandle, "ImGuiListClipper_SeekCursorForItem");
        igImGuiListClipper_SeekCursorForItem = (delegate* unmanaged[Cdecl]<ImGuiListClipper*, int, void>)method;

        method = NativeLibrary.GetExport(cimguiHandle, "igBeginViewportSideBar");
        igBeginViewportSideBar = (delegate* unmanaged[Cdecl]<byte*, ImGuiViewportPtr, ImGuiDir, float, ImGuiWindowFlags, byte>)method;

        // (fixed in cimgui sort3 branch - but will wait for official merge)
        cimgui_warm64_workaround = (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        igSetAllocatorFunctions(&MallocFunction, &FreeFunction, null);
    } 

    public static string GetVersion()
    {
        var versionPtr = CImguiNative.CImgui.igGetVersion();
        return Marshal.PtrToStringUTF8(versionPtr) ?? string.Empty;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static void* MallocFunction(nuint size, void* user_data)
    {
        return Marshal.AllocHGlobal((IntPtr)size).ToPointer();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static void FreeFunction(void* ptr, void* user_data)
    {
        Marshal.FreeHGlobal((IntPtr)ptr);
    }

    public static ImGuiContextPtr CreateContext(ImFontAtlasPtr fontAtlas = default)
    {
        return igCreateContext(fontAtlas.NativePtr);
    }

    public static void DestroyContext(ImGuiContextPtr ctx = default!)
    {
        igDestryContext(ctx);
    }

    public static void SetCurrentContext(ImGuiContextPtr ctx)
    {
        igSetCurrentContext(ctx);
    }

    public static ImDrawDataPtr GetDrawData()
    {
        return new ImDrawDataPtr(igGetDrawData());
    }

    public static ImDrawListPtr GetWindowDrawList()
    {
        return new ImDrawListPtr(igGetWindowDrawList());
    }

    public static ImGuiStylePtr GetStyle()
    {
        return igGetStyle();
    }

    public static void StyleColorsDark()
    {
        igStyleColorsDark(default);
    }

    public static void StyleColorsLight()
    {
        igStyleColorsLight(default);
    }

    public static void StyleColorsClassic()
    {
        igStyleColorsClassic(default);
    }

    public static ImCol ColorConvert(ImVec4 col)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedColA = *((ulong*)Unsafe.AsPointer(ref col.X));
            ulong combinedColB = *((ulong*)Unsafe.AsPointer(ref col.Z));
            return igColorConvertFloat4ToU32_WArm64(combinedColA, combinedColB);
        }
        return igColorConvertFloat4ToU32_X64(col);
    }

    public static ImVec4 ColorConvert(ImCol col)
    {
        ImVec4 nativeCol;
        igColorConvertU32ToFloat4(&nativeCol, col);
        return nativeCol;
    }

    public static ImCol GetColorU32(ImGuiCol idx, float alphaMul = 1.0f)
    {
        return igColorU32_Col(idx, alphaMul);
    }

    public static ImGuiID DockSpaceOverViewport()
    {
        return igDockSpaceOverViewport(new ImGuiID(0), null, ImGuiDockNodeFlags.None, null);
    }

    public static ImGuiIOPtr GetIO()
    {
        return new ImGuiIOPtr(igGetIO());
    }

    public static ImGuiPlatformIOPtr GetPlatformIO()
    {
        return igGetPlatformIO();
    }

    public static void NewFrame()
    {
        igNewFrame();
    }

    public static void Render()
    {
        igRender();
    }

    public static float GetTextLineHeight()
    {
        return igGetTextLineHeight();
    }
    public static float GetTextLineHeightWithSpacing()
    {
        return igGetTextLineHeightWithSpacing();
    }
    public static float GetFrameHeight()
    {
        return igGetFrameHeight();
    }
    public static float GetFrameHeightWithSpacing()
    {
        return igGetFrameHeightWithSpacing();
    }

    public static bool Begin(ReadOnlySpan<char> name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(name);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = name)
        {
            Encoding.UTF8.GetBytes(strPtr, name.Length, bytes, len);
        }

        byte* pOpen = null;
        byte openByte = open ? (byte)1 : (byte)0;
        pOpen = &openByte;

        var result = igBegin(bytes, pOpen, flags);
        open = openByte != 0;
        return result != 0;
    }

    public static void End()
    {
        igEnd();
    }

    public static bool BeginChild(ReadOnlySpan<char> strId, ImVec2 sizeArg = default, ImGuiChildFlags flags = ImGuiChildFlags.None, ImGuiWindowFlags extraFlags = ImGuiWindowFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(strId);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = strId)
        {
            Encoding.UTF8.GetBytes(strPtr, strId.Length, bytes, len);
        }
        if (cimgui_warm64_workaround)
        {
            ulong combinedSize = *((ulong*)Unsafe.AsPointer(ref sizeArg.X));
            return igBeginChild_WArm64(bytes, combinedSize, flags, extraFlags) != 0;
        }
        return igBeginChild_X64(bytes, sizeArg, flags, extraFlags) != 0;
    }

    public static void EndChild()
    {
        igEndChild();
    }

    public static bool BeginTooltip()
    {
        return igBeginTooltip() != 0;
    }

    public static void EndTooltip()
    {
        igEndTooltip();
    }

    public static void PushID(ReadOnlySpan<char> strId)
    {
        var len = Encoding.UTF8.GetByteCount(strId);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = strId)
        {
            Encoding.UTF8.GetBytes(strPtr, strId.Length, bytes, len);
        }
        igPushID_StrStr(bytes, bytes + len);
    }

    public static void PushID(void* ptrId)
    {
        igPushID_Ptr(ptrId);
    }

    public static void PushID(int intId)
    {
        igPushID_Int(intId);
    }

    public static void PopID()
    {
        igPopID();
    }

    public static bool BeginMenuBar()
    {
        return igBeginMenuBar() != 0;
    }

    public static void EndMenuBar()
    {
        igEndMenuBar();
    }

    public static bool BeginMainMenuBar()
    {
        return igBeginMainMenuBar() != 0;
    }

    public static void EndMainMenuBar()
    {
        igEndMainMenuBar();
    }

    public static void SetNextWindowSize(ImVec2 size, ImGuiCond cond)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedSize = *((ulong*)Unsafe.AsPointer(ref size.X));
            igSetNextWindowSize_WArm64(combinedSize, cond);
        }
        else
        {
            igSetNextWindowSize_X64(size, cond);
        }
    }

    public static bool BeginTabBar(ReadOnlySpan<char> strId, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(strId);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = strId)
        {
            Encoding.UTF8.GetBytes(strPtr, strId.Length, bytes, len);
        }
        return igBeginTabBar(bytes, flags) != 0;
    }

    public static void EndTabBar()
    {
        igEndTabBar();
    }

    public static bool BeginTabItem(ReadOnlySpan<char> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }

        return igBeginTabItem(bytes, null, flags) != 0;
    }

    public static bool BeginTabItem(ReadOnlySpan<char> label, ref bool open, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }

        byte openByte = open ? (byte)1 : (byte)0;
        var pOpen = &openByte;

        var result = igBeginTabItem(bytes, pOpen, flags);
        open = openByte != 0;
        return result != 0;
    }

    public static void EndTabItem()
    {
        igEndTabItem();
    }

    public static bool TabItemButton(ReadOnlySpan<char> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        return igTabItemButton(bytes, flags) != 0;
    }

    public static void SetTabItemClosed(ReadOnlySpan<char> label)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        igSetTabItemClosed(bytes);
    }

    public static bool Button(ReadOnlySpan<char> label, ImVec2 sizeArg = default)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        if (cimgui_warm64_workaround)
        {
            ulong combinedSize = *((ulong*)Unsafe.AsPointer(ref sizeArg.X));
            return igButton_WArm64(bytes, combinedSize) != 0;
        }
        return igButton_X64(bytes, sizeArg) != 0;
    }

    public static bool InputText(ReadOnlySpan<char> label, ref string buf, int length, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None/*, nint callback = default, void* userData = null*/)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        var bufChars = stackalloc byte[length];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        fixed (char* bufPtr = buf)
        {
            Encoding.UTF8.GetBytes(bufPtr, buf.Length, bufChars, length);
        }
        var result = igInputText(bytes, bufChars, length, flags, default, default) != 0;
        buf = Marshal.PtrToStringUTF8((IntPtr)bufChars) ?? string.Empty;
        return result;
    }

    public static bool InputTextWithHint(ReadOnlySpan<char> label, ReadOnlySpan<char> hint, ref string buf, int length, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None/*, nint callback = default, void* userData = null*/)
    {
        var lenLabel = Encoding.UTF8.GetByteCount(label);
        var bytesLabel = stackalloc byte[lenLabel + 1];
        var lenHint = Encoding.UTF8.GetByteCount(hint);
        var bytesHint = stackalloc byte[lenHint + 1];
        var bufChars = stackalloc byte[length];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytesLabel, lenLabel);
        }
        fixed (char* hintPtr = hint)
        {
            Encoding.UTF8.GetBytes(hintPtr, hint.Length, bytesHint, lenHint);
        }
        fixed (char* bufPtr = buf)
        {
            Encoding.UTF8.GetBytes(bufPtr, buf.Length, bufChars, length);
        }
        var result = igInputTextWithHint(bytesLabel, bytesHint, bufChars, length, flags, default, default) != 0;
        buf = Marshal.PtrToStringUTF8((IntPtr)bufChars) ?? string.Empty;
        return result;
    }

    public static bool Checkbox(ReadOnlySpan<char> label, ref bool v)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        byte vByte = v ? (byte)1 : (byte)0;
        var result = igCheckbox(bytes, &vByte) != 0;
        v = vByte != 0;
        return result;
    }

    public static bool Selectable(ReadOnlySpan<char> label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, ImVec2 sizeArg = default)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        if (len == 0)
        {
            bytes[0]=0;
        }
        else
        {
            fixed (char* strPtr = label)
            {
                Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
            }
        }
        byte selectedByte = selected ? (byte)1 : (byte)0;
        if (cimgui_warm64_workaround)
        {
            ulong combinedSize = *((ulong*)Unsafe.AsPointer(ref sizeArg.X));
            return igSelectable_WArm64(bytes, selectedByte, flags, combinedSize) != 0;
        }
        return igSelectable_X64(bytes, selectedByte, flags, sizeArg) != 0;
    }

    public static bool Combo(ReadOnlySpan<char> label, ref int currentItem, string[] items, int popupMaxHeightInItems = -1)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }

        int itemsCount = items.Length;
        var itemPtrs = stackalloc byte*[itemsCount];
        var itemLengths = new int[itemsCount];
        var totalCount=0;
        for (int i = 0; i < itemsCount; i++)
        {
            itemLengths[i] = Encoding.UTF8.GetByteCount(items[i]);
            totalCount += itemLengths[i];
        }
        var itemData = stackalloc byte[totalCount + itemsCount]; // +itemsCount for null terminators
        var currentPtr = itemData;
        for (int i = 0; i < itemsCount; i++)
        {
            int itemLen = itemLengths[i];
            var itemBytes = currentPtr;
            fixed (char* itemStrPtr = items[i])
            {
                Encoding.UTF8.GetBytes(itemStrPtr, items[i].Length, itemBytes, itemLen);
            }
            itemBytes[itemLen] = 0; // null terminator
            currentPtr += itemLen + 1;
            itemPtrs[i] = itemBytes;
        }
        fixed (int* currentItemPtr = &currentItem)
        {
            return igComboStrArr(bytes, currentItemPtr, itemPtrs, itemsCount, popupMaxHeightInItems) != 0;
        }
    }

    public static bool InputScalar<T>(ReadOnlySpan<char> label, ImGuiDataType dataType, ref T data, ReadOnlySpan<char> format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged
    {
        fixed (T* pData = &data)
        {
            var result = InputScalar(label, dataType, pData, default, default, format, flags);
            data = *pData;
            return result;
        }
    }
    public static bool InputScalar<T>(ReadOnlySpan<char> label, ImGuiDataType dataType, ref T data, T step = default, T stepFast = default, ReadOnlySpan<char> format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged
    {
        fixed (T* pData = &data)
        {
            void* pStep = Unsafe.AsPointer(ref step);
            void* pStepFast = Unsafe.AsPointer(ref stepFast);
            var result = InputScalar(label, dataType, pData, pStep, pStepFast, format, flags);
            data = *pData;
            return result;
        }
    }

    internal static bool InputScalar(ReadOnlySpan<char> label, ImGuiDataType dataType, void* pData, void* pStep = null, void* pStepFast = null, ReadOnlySpan<char> format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        var lenFormat = Encoding.UTF8.GetByteCount(format);
        var bytesFormat = stackalloc byte[lenFormat + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        if (lenFormat > 0)
        {
            fixed (char* formatPtr = format)
            {
                Encoding.UTF8.GetBytes(formatPtr, format.Length, bytesFormat, lenFormat);
            }
        }
        else
        {
            bytesFormat = null;
        }
        return igInputScalar(bytes, dataType, pData, pStep, pStepFast, bytesFormat, flags) != 0;
    }

    public static bool Slider(ReadOnlySpan<char> label, ref int v, int vMin, int vMax, ReadOnlySpan<char> format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        var lenFormat = Encoding.UTF8.GetByteCount(format);
        var bytesFormat = stackalloc byte[lenFormat + 1];
        fixed (char* strPtr = label)
        {
            Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
        }
        if (lenFormat > 0)
        {
            fixed (char* formatPtr = format)
            {
                Encoding.UTF8.GetBytes(formatPtr, format.Length, bytesFormat, lenFormat);
            }
        }
        else
        {
            bytesFormat = null;
        }
        fixed (int* vPtr = &v)
        {
            return igSliderInt(bytes, vPtr, vMin, vMax, bytesFormat, flags) != 0;
        }
    }

    public static void OpenPopup(ReadOnlySpan<char> strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(strId);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = strId)
        {
            Encoding.UTF8.GetBytes(strPtr, strId.Length, bytes, len);
        }
        igOpenPopup(bytes, popupFlags);
    }

    public static bool BeginPopup(ReadOnlySpan<char> strId, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(strId);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = strId)
        {
            Encoding.UTF8.GetBytes(strPtr, strId.Length, bytes, len);
        }
        return igBeginPopup(bytes, flags) != 0;
    }

    public static bool BeginPopupModal(ReadOnlySpan<char> name, ref bool pOpen, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        var len = Encoding.UTF8.GetByteCount(name);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = name)
        {
            Encoding.UTF8.GetBytes(strPtr, name.Length, bytes, len);
        }

        byte openByte = pOpen ? (byte)1 : (byte)0;
        var result = igBeginPopupModal(bytes, &openByte, flags);
        pOpen = openByte != 0;
        return result != 0;
    }

    public static void EndPopup()
    {
        igEndPopup();
    }

    public static void CloseCurrentPopup()
    {
        igCloseCurrentPopup();
    }

    public static void BeginDisabled(bool disabled = true)
    {
        byte disabledByte = disabled ? (byte)1 : (byte)0;
        igBeginDisabled(disabledByte);
    }
    public static void EndDisabled()
    {
        igEndDisabled();
    }

    public static void PushStyleColor(ImGuiCol idx, ImCol col)
    {
        igPushStyleColor_U32(idx, col);
    }

    public static void PushStyleColor(ImGuiCol idx, ImVec4 col)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedColA = *((ulong*)Unsafe.AsPointer(ref col.X));
            ulong combinedColB = *((ulong*)Unsafe.AsPointer(ref col.Z));
            igPushStyleColor_Vec4_WArm64(idx, combinedColA, combinedColB);
        }
        else
        {
            igPushStyleColor_Vec4_X64(idx, col);
        }
    }

    public static void PopStyleColor(int count = 1)
    {
        igPopStyleColor(count);
    }

    public static void PushStyleVar(ImGuiStyleVar idx, float val)
    {
        igPushStyleVar_Float(idx, val);
    }

    public static void PushStyleVar(ImGuiStyleVar idx, ImVec2 val)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedVal = *((ulong*)Unsafe.AsPointer(ref val.X));
            igPushStyleVar_Vec2_WArm64(idx, combinedVal);
        }
        else
        {
            igPushStyleVar_Vec2_X64(idx, val);
        }
    }

    public static void PushStyleVarX(ImGuiStyleVar idx, float val)
    {
        igPushStyleVarX(idx, val);
    }

    public static void PushStyleVarY(ImGuiStyleVar idx, float val)
    {
        igPushStyleVarY(idx, val);
    }

    public static void PopStyleVar(int count = 1)
    {
        igPopStyleVar(count);
    }

    public static bool BeginMenu(ReadOnlySpan<char> name, bool enabled = true)
    {
        var len = Encoding.UTF8.GetByteCount(name);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = name)
        {
            Encoding.UTF8.GetBytes(strPtr, name.Length, bytes, len);
        }
        byte enabledByte = enabled ? (byte)1 : (byte)0;
        return igBeginMenu(bytes, enabledByte) != 0;
    }

    public static void EndMenu()
    {
        igEndMenu();
    }

    public static bool MenuItem(ReadOnlySpan<char> name, ReadOnlySpan<char> shortcut = default, bool selected = false, bool enabled = true)
    {
        var len = Encoding.UTF8.GetByteCount(name);
        var lenShortcut = Encoding.UTF8.GetByteCount(shortcut);
        var bytes = stackalloc byte[len + 1];
        var bytesShortcut = stackalloc byte[lenShortcut + 1];
       if (lenShortcut == 0)
        {
            bytesShortcut = null;
        }
        else
        {
            lenShortcut = Encoding.UTF8.GetByteCount(shortcut);
            fixed (char* strPtr = shortcut)
            {
                Encoding.UTF8.GetBytes(strPtr, shortcut.Length, bytesShortcut, lenShortcut);
            }
        }
        fixed (char* strPtr = name)
        {
            Encoding.UTF8.GetBytes(strPtr, name.Length, bytes, len);
        }
        byte enabledByte = enabled ? (byte)1 : (byte)0;
        return igMenuItem_Bool(bytes, bytesShortcut, selected ? (byte)1 : (byte)0, enabledByte) != 0;
    }

    public static ImGuiMouseCursor GetMouseCursor()
    {
        return igGetMouseCursor();
    }

    public static void SetMouseCursor(ImGuiMouseCursor cursorType)
    {
        igSetMouseCursor(cursorType);
    }

    public static void Separator()
    {
        igSeparator();
    }

    public static void SameLine(float offsetFromStartX = 0.0f, float spacingW = -1.0f)
    {
        igSameLine(offsetFromStartX, spacingW);
    }

    public static void NewLine()
    {
        igNewLine();
    }

    public static void Spacing()
    {
        igSpacing();
    }

    public static void Dummy(ImVec2 size=default)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedSize = *((ulong*)Unsafe.AsPointer(ref size.X));
            igDummy_WArm64(combinedSize);
        }
        else
        {
            igDummy_X64(size);
        }
    }

    public static void Text(ReadOnlySpan<char> text)
    {
        var len = Encoding.UTF8.GetByteCount(text);
        if (len == 0)
        {
            igTextUnformatted(null, null);
            return;
        }
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = text)
        {
            Encoding.UTF8.GetBytes(strPtr, text.Length, bytes, len);
        }
        igTextUnformatted(bytes, bytes+len);
    }

    public static void LabelText(ReadOnlySpan<char> label, ReadOnlySpan<char> text)
    {
        var lenLabel = Encoding.UTF8.GetByteCount(label);
        var lenText = Encoding.UTF8.GetByteCount(text);
        var bytesLabel = stackalloc byte[lenLabel + 1];
        var bytesText = stackalloc byte[lenText + 1];
        if (lenLabel == 0)
        {
            bytesLabel[0] = 0;
        }
        else
        {
            fixed (char* strPtr = label)
            {
                Encoding.UTF8.GetBytes(strPtr, label.Length, bytesLabel, lenLabel);
            }
        }
        if (lenText == 0)
        {
            bytesText[0] = 0;
        }
        else
        {
            fixed (char* strPtr = text)
            {
                Encoding.UTF8.GetBytes(strPtr, text.Length, bytesText, lenText);
            }
        }
        igLabelText(bytesLabel, bytesText);
    }

    public static ImVec2 CalcTextSize(ReadOnlySpan<char> text, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f)
    {
        var len = Encoding.UTF8.GetByteCount(text);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = text)
        {
            Encoding.UTF8.GetBytes(strPtr, text.Length, bytes, len);
        }
        ImVec2 size;
        igCalcTextSize(&size, bytes, bytes + len, hideTextAfterDoubleHash ? (byte)1 : (byte)0, wrapWidth);
        return size;
    }

    public static void Image(ImTextureRef texture, ImVec2 size)
    {
        Image(texture, size, new ImVec2(0.0f, 0.0f), new ImVec2(1.0f, 1.0f));
    }

    public static void Image(ImTextureRef texture, ImVec2 size, ImVec2 uv0, ImVec2 uv1)
    {
        if (cimgui_warm64_workaround)
        {
            ulong texID0 = *((ulong*)Unsafe.AsPointer(ref texture._TexData));
            ulong texID1 = *((ulong*)Unsafe.AsPointer(ref texture._TexID));
            ulong sizeCombined = *((ulong*)Unsafe.AsPointer(ref size.X));
            ulong uv0Combined = *((ulong*)Unsafe.AsPointer(ref uv0.X));
            ulong uv1Combined = *((ulong*)Unsafe.AsPointer(ref uv1.X));
            igImage_WArm64(texID0, texID1, sizeCombined, uv0Combined, uv1Combined);
        }
        else
        {
            igImage_X64(texture, size, uv0, uv1);
        }
    }

    public static void Columns(int count, ReadOnlySpan<char> id=default, bool border = true)
    {
        var len = Encoding.UTF8.GetByteCount(id);
        var bytes = stackalloc byte[len + 1];
        if (len == 0)
        {
            bytes = null;
        }
        else
        {
            fixed (char* strPtr = id)
            {
                Encoding.UTF8.GetBytes(strPtr, id.Length, bytes, len);
            }
        }
        igColumns(count, bytes, border ? (byte)1 : (byte)0);
    }

    public static void NextColumn()
    {
        igNextColumn();
    }

    public static int GetColumnIndex()
    {
        return igGetColumnIndex();
    }

    public static float GetColumnWidth(int columnIndex)
    {
        return igGetColumnWidth(columnIndex);
    }

    public static void SetColumnWidth(int columnIndex, float width)
    {
        igSetColumnWidth(columnIndex, width);
    }

    public static float GetColumnOffset(int columnIndex)
    {
        return igGetColumnOffset(columnIndex);
    }

    public static void SetColumnOffset(int columnIndex, float offsetX)
    {
        igSetColumnOffset(columnIndex, offsetX);
    }

    public static int GetColumnsCount()
    {
        return igGetColumnsCount();
    }

    public static bool BeginTable(ReadOnlySpan<char> strId, int column, ImGuiTableFlags flags = ImGuiTableFlags.None, ImVec2 outerSize = default, float innerWidth = 0.0f)
    {
        var len = Encoding.UTF8.GetByteCount(strId);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = strId)
        {
            Encoding.UTF8.GetBytes(strPtr, strId.Length, bytes, len);
        }
        if (cimgui_warm64_workaround)
        {
            ulong combinedOuterSize = *((ulong*)Unsafe.AsPointer(ref outerSize.X));
            return igBeginTable_WArm64(bytes, column, flags, combinedOuterSize, innerWidth) != 0;
        }
        return igBeginTable_X64(bytes, column, flags, outerSize, innerWidth) != 0;
    }

    public static void EndTable()
    {
        igEndTable();
    }

    public static void TableSetupColumn(ReadOnlySpan<char> label = default, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, float initWidthOrWeight = 0.0f, ImGuiID userID = default)
    {
        var len = Encoding.UTF8.GetByteCount(label);
        var bytes = stackalloc byte[len + 1];
        if (len == 0)
        {
            bytes = null;
        }
        else
        {
            fixed (char* strPtr = label)
            {
                Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
            }
        }
        igTableSetupColumn(bytes, flags, initWidthOrWeight, userID);
    }

    public static void TableNextRow(ImGuiTableRowFlags rowFlags = ImGuiTableRowFlags.None, float minRowHeight = 0.0f)
    {
        igTableNextRow(rowFlags, minRowHeight);
    }

    public static bool TableNextColumn()
    {
        return igTableNextColumn() != 0;
    }

    public static bool TableSetColumnIndex(int columnN)
    {
        return igTableSetColumnIndex(columnN) != 0;
    }

    public static void TableSetBgColor(ImGuiTableBgTarget target, ImCol color, int columnN = -1)
    {
        igTableSetBgColor(target, color, columnN);
    }

    public static bool IsWindowAppearing()
    {
        return igIsWindowAppearing() != 0;
    }

    public static bool IsWindowCollapsed()
    {
        return igIsWindowCollapsed() != 0;
    }

    public static bool IsWindowFocused(ImGuiFocusedFlags flags = ImGuiFocusedFlags.None)
    {
        return igIsWindowFocused(flags) != 0;
    }

    public static bool IsWindowHovered(ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        return igIsWindowHovered(flags) != 0;
    }

    public static bool IsKeyDown(ImGuiKey key)
    {
        return igIsKeyDown(key) != 0;
    }

    public static bool IsKeyPressed(ImGuiKey key, bool repeat = true)
    {
        return igIsKeyPressed(key, repeat ? (byte)1 : (byte)0) != 0;
    }

    public static bool IsKeyReleased(ImGuiKey key)
    {
        return igIsKeyReleased(key) != 0;
    }

    public static bool IsItemHovered(ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        return igIsItemHovered(flags) != 0;
    }

    public static bool IsItemActive()
    {
        return igIsItemActive() != 0;
    }

    public static bool IsItemFocused()
    {
        return igIsItemFocused() != 0;
    }

    public static bool IsItemClicked(ImGuiMouseButton button = ImGuiMouseButton.Left)
    {
        return igIsItemClicked(button) != 0;
    }

    public static bool IsItemVisible()
    {
        return igIsItemVisible() != 0;
    }

    public static bool IsItemEdited()
    {
        return igIsItemEdited() != 0;
    }

    public static bool IsItemActivated()
    {
        return igIsItemActivated() != 0;
    }

    public static bool IsItemDeactivated()
    {
        return igIsItemDeactivated() != 0;
    }

    public static bool IsItemDeactivatedAfterEdit()
    {
        return igIsItemDeactivatedAfterEdit() != 0;
    }

    public static bool IsItemToggledOpen()
    {
        return igIsItemToggledOpen() != 0;
    }

    public static bool IsAnyItemHovered()
    {
        return igIsAnyItemHovered() != 0;
    }

    public static bool IsAnyItemActive()
    {
        return igIsAnyItemActive() != 0;
    }

    public static bool IsAnyItemFocused()
    {
        return igIsAnyItemFocused() != 0;
    }

    public static ImVec2 GetCursorPos()
    {
        ImVec2 pos;
        igGetCursorPos(&pos);
        return pos;
    }

    public static float GetCursorPosX()
    {
        return igGetCursorPosX();
    }

    public static float GetCursorPosY()
    {
        return igGetCursorPosY();
    }

    public static void SetCursorPos(ImVec2 pos)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedPos = *((ulong*)Unsafe.AsPointer(ref pos.X));
            igSetCursorPos_WArm64(combinedPos);
            return;
        }
        igSetCursorPos_X64(pos);
    }

    public static void SetCursorPosX(float x)
    {
        igSetCursorPosX(x);
    }

    public static void SetCursorPosY(float y)
    {
        igSetCursorPosY(y);
    }

    public static bool IsMouseDown(ImGuiMouseButton button)
    {
        return igIsMouseDown(button) != 0;
    }

    public static bool IsMouseClicked(ImGuiMouseButton button, bool repeat = false)
    {
        return igIsMouseClicked(button, repeat ? (byte)1 : (byte)0) != 0;
    }

    public static bool IsMouseReleased(ImGuiMouseButton button)
    {
        return igIsMouseReleased(button) != 0;
    }

    public static bool IsMouseDoubleClicked(ImGuiMouseButton button)
    {
        return igIsMouseDoubleClicked(button) != 0;
    }

    public static bool IsMouseReleasedWithDelay(ImGuiMouseButton button, float delay)
    {
        return igIsMouseReleasedWithDelay(button, delay) != 0;
    }

    public static int GetMouseClickedCount(ImGuiMouseButton button)
    {
        return igGetMouseClickedCount(button);
    }

    public static bool IsMouseHoveringRect(ImVec2 rMin, ImVec2 rMax, bool clip = true)
    {
        if (cimgui_warm64_workaround)
        {
            ulong rMinCombined = *((ulong*)Unsafe.AsPointer(ref rMin.X));
            ulong rMaxCombined = *((ulong*)Unsafe.AsPointer(ref rMax.X));
            return igIsMouseHoveringRect_WArm64(rMinCombined, rMaxCombined, clip ? (byte)1 : (byte)0) != 0;
        }
        return igIsMouseHoveringRect_X64(rMin, rMax, clip ? (byte)1 : (byte)0) != 0;
    }

    public static bool IsMousePosValid(ref ImVec2 mousePos)
    {
        ImVec2 localMousePos = mousePos;
        var result = igIsMousePosValid(&localMousePos) != 0;
        mousePos = localMousePos;
        return result;
    }

    public static bool IsAnyMouseDown()
    {
        return igIsAnyMouseDown() != 0;
    }

    public static ImVec2 GetMousePos()
    {
        ImVec2 pos;
        igGetMousePos(&pos);
        return pos;
    }

    public static ImVec2 GetWindowPos()
    {
        ImVec2 pos;
        igGetWindowPos(&pos);
        return pos;
    }

    public static ImVec2 GetWindowSize()
    {
        ImVec2 size;
        igGetWindowSize(&size);
        return size;
    }

    public static float GetScrollX()
    {
        return igGetScrollX();
    }

    public static float GetScrollY()
    {
        return igGetScrollY();
    }

    public static void SetScrollX(float scrollX)
    {
        igSetScrollX_Float(scrollX);
    }

    public static void SetScrollY(float scrollY)
    {
        igSetScrollY_Float(scrollY);
    }

    public static float GetScrollMaxX()
    {
        return igGetScrollMaxX();
    }

    public static float GetScrollMaxY()
    {
        return igGetScrollMaxY();
    }

    public static void SetScrollHereX(float centerXRatio = 0.5f)
    {
        igSetScrollHereX(centerXRatio);
    }

    public static void SetScrollHereY(float centerYRatio = 0.5f)
    {
        igSetScrollHereY(centerYRatio);
    }

    public static void SetScrollFromPosX(float localX, float centerXRatio = 0.5f)
    {
        igSetScrollFromPosX_Float(localX, centerXRatio);
    }

    public static void SetScrollFromPosY(float localY, float centerYRatio = 0.5f)
    {
        igSetScrollFromPosY_Float(localY, centerYRatio);
    }

    public static ImVec2 GetCursorScreenPos()
    {
        ImVec2 pos;
        igGetCursorScreenPos(&pos);
        return pos;
    }

    public static void SetCursorScreenPos(ImVec2 pos)
    {
        if (cimgui_warm64_workaround)
        {
            ulong combinedPos = *((ulong*)Unsafe.AsPointer(ref pos.X));
            igSetCursorScreenPos_WArm64(combinedPos);
            return;
        }
        igSetCursorScreenPos_X64(pos);
    }

    public static ImVec2 GetContentRegionAvail()
    {
        ImVec2 size;
        igGetContentRegionAvail(&size);
        return size;
    }

    internal static ImFont_TODO* ImFontAtlas_AddFontDefault(ImFontAtlas_TODO* atlas, ImFontConfig_TODO* font_cfg)
    {
        return igImFontAtlas_AddFontDefault(atlas, font_cfg);
    }

    public static ImGuiViewportPtr GetMainViewport()
    {
        return igGetMainViewport();
    }

    internal static void ImGuiIO_AddMousePosEvent(ImGuiIO* io, float x, float y)
    {
        igImGuiIO_AddMousePosEvent(io, x, y);
    }

    internal static void ImGuiIO_AddMouseButtonEvent(ImGuiIO* io, ImGuiMouseButton button, bool down)
    {
        igImGuiIO_AddMouseButtonEvent(io, button, (byte)(down ? 1 : 0));
    }

    internal static void ImGuiIO_AddMouseWheelEvent(ImGuiIO* io, float wheelX, float wheelY)
    {
        igImGuiIO_AddMouseWheelEvent(io, wheelX, wheelY);
    }

    internal static void ImGuiIO_AddFocusEvent(ImGuiIO* io, bool focused)
    {
        igImGuiIO_AddFocusEvent(io, (byte)(focused ? 1 : 0));
    }

    internal static void ImGuiIO_AddKeyEvent(ImGuiIO* io, ImGuiKey key,  bool down)
    {
        igImGuiIO_AddKeyEvent(io, key, (byte)(down ? 1 : 0));
    }

    internal static void ImGuiIO_AddInputCharacter(ImGuiIO* io, uint c)
    {
        igImGuiIO_AddInputCharacter(io, c);
    }

    internal static void ImGuiIO_AddKeyAnalogEvent(ImGuiIO* io, ImGuiKey key, bool down, float analogValue)
    {
        igImGuiIO_AddKeyAnalogEvent(io, key, (byte)(down ? 1 : 0), analogValue);
    }

    internal static void* ImTextureData_GetPixels(ImTextureData* tex)
    {
        return igImTextureData_GetPixels(tex);
    }

    internal static void* ImTextureData_GetPixelsAt(ImTextureData* tex, int x, int y)
    {
        return igImTextureData_GetPixelsAt(tex, x, y);
    }

    internal static int ImTextureData_GetSizeInBytes(ImTextureData* tex)
    {
        return igImTextureData_GetSizeInBytes(tex);
    }

    internal static int ImTextureData_GetPitch(ImTextureData* tex)
    {
        return igImTextureData_GetPitch(tex);
    }

    internal static void ImTextureData_GetTexRef(ImTextureData* tex, ImTextureRef* texRef)
    {
        igImTextureData_GetTexRef(tex, texRef);
    }

    internal static ImTextureID ImTextureData_GetTexID(ImTextureData* tex)
    {
        return igImTextureData_GetTexID(tex);
    }

    internal static void ImTextureData_SetTexID(ImTextureData* tex, ImTextureID texID)
    {
        igImTextureData_SetTexID(tex, texID);
    }

    internal static void ImTextureData_SetStatus(ImTextureData* tex, ImTextureStatus status)
    {
        igImTextureData_SetStatus(tex, status);
    }

    internal static void ImDrawList_AddRect(ImDrawList* list, ImVec2 min, ImVec2 max, ImCol col, float rounding, ImDrawFlags flags, float thickness)
    {
        if (cimgui_warm64_workaround)
        {
            ulong minCombined = *((ulong*)Unsafe.AsPointer(ref min.X));
            ulong maxCombined = *((ulong*)Unsafe.AsPointer(ref max.X));
            ImDrawList_AddRect_WArm64(list, minCombined, maxCombined, col, rounding, flags, thickness);
            return;
        }
        ImDrawList_AddRect_X64(list, min, max, col, rounding, flags, thickness);
    }

    internal static void ImDrawList_AddRectFilled(ImDrawList* list, ImVec2 min, ImVec2 max, ImCol col, float rounding, ImDrawFlags flags)
    {
        if (cimgui_warm64_workaround)
        {
            ulong minCombined = *((ulong*)Unsafe.AsPointer(ref min.X));
            ulong maxCombined = *((ulong*)Unsafe.AsPointer(ref max.X));
            ImDrawList_AddRectFilled_WArm64(list, minCombined, maxCombined, col, rounding, flags);
            return;
        }
        ImDrawList_AddRectFilled_X64(list, min, max, col, rounding, flags);
    }

    internal static void ImDrawList_AddText(ImDrawList* list, ImVec2 pos, ImCol col, ReadOnlySpan<char> text)
    {
        var len = Encoding.UTF8.GetByteCount(text);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = text)
        {
            Encoding.UTF8.GetBytes(strPtr, text.Length, bytes, len);
        }
        if (cimgui_warm64_workaround)
        {
            ulong posCombined = *((ulong*)Unsafe.AsPointer(ref pos.X));
            ImDrawList_AddText_WArm64(list, posCombined, col, bytes, bytes + len);
            return;
        }
        ImDrawList_AddText_X64(list, pos, col, bytes, bytes + len);
    }

    internal static void ImDrawList_AddImage(ImDrawList* list, ImTextureRef user_texture_id, ImVec2 a, ImVec2 b, ImVec2 uv_a, ImVec2 uv_b, ImCol col)
    {
        if (cimgui_warm64_workaround)
        {
            ulong aCombined = *((ulong*)Unsafe.AsPointer(ref a.X));
            ulong bCombined = *((ulong*)Unsafe.AsPointer(ref b.X));
            ulong uvACombined = *((ulong*)Unsafe.AsPointer(ref uv_a.X));
            ulong uvBCombined = *((ulong*)Unsafe.AsPointer(ref uv_b.X));
            ImDrawList_AddImage_WArm64(list, user_texture_id, aCombined, bCombined, uvACombined, uvBCombined, col);
            return;
        }
        ImDrawList_AddImage_X64(list, user_texture_id, a, b, uv_a, uv_b, col);
    }

    internal static void ImDrawList_PushClipRect(ImDrawList* list, ImVec2 clip_rect_min, ImVec2 clip_rect_max, bool intersect_with_current_clip_rect)
    {
        if (cimgui_warm64_workaround)
        {
            ulong minCombined = *((ulong*)Unsafe.AsPointer(ref clip_rect_min.X));
            ulong maxCombined = *((ulong*)Unsafe.AsPointer(ref clip_rect_max.X));
            ImDrawList_PushClipRect_WArm64(list, minCombined, maxCombined, intersect_with_current_clip_rect ? (byte)1 : (byte)0);
            return;
        }
        ImDrawList_PushClipRect_X64(list, clip_rect_min, clip_rect_max, intersect_with_current_clip_rect ? (byte)1 : (byte)0);
    }

    internal static void ImDrawList_PushClipRectFullScreen(ImDrawList* list)
    {
        _ImDrawList_PushClipRectFullScreen(list);
    }

    internal static void ImDrawList_PopClipRect(ImDrawList* list)
    {
        _ImDrawList_PopClipRect(list);
    }

    internal static ImGuiListClipper* ImGuiListClipper_ImGuiListClipper()
    {
        return igImGuiListClipper_ImGuiListClipper();
    }

    internal static void ImGuiListClipper_destroy(ImGuiListClipper* clipper)
    {
        igImGuiListClipper_destroy(clipper);
    }

    internal static void ImGuiListClipper_Begin(ImGuiListClipper* clipper, int items_count, float items_height)
    {
        igImGuiListClipper_Begin(clipper, items_count, items_height);
    }

    internal static int ImGuiListClipper_Step(ImGuiListClipper* clipper)
    {
        return igImGuiListClipper_Step(clipper);
    }

    internal static void ImGuiListClipper_End(ImGuiListClipper* clipper)
    {
        igImGuiListClipper_End(clipper);
    }

    internal static void ImGuiListClipper_IncludeItemByIndex(ImGuiListClipper* clipper, int index)
    {
        igImGuiListClipper_IncludeItemByIndex(clipper, index);
    }

    internal static void ImGuiListClipper_IncludeItemsByIndex(ImGuiListClipper* clipper, int index_start, int index_end)
    {
        igImGuiListClipper_IncludeItemsByIndex(clipper, index_start, index_end);
    }

    internal static void ImGuiListClipper_SeekCursorForItem(ImGuiListClipper* clipper, int index)
    {
        igImGuiListClipper_SeekCursorForItem(clipper, index);
    }

    // Internals (but useful for editor)
    public static bool BeginViewportSideBar(ReadOnlySpan<char> name, ImGuiViewportPtr viewport, ImGuiDir dir, float size, ImGuiWindowFlags flags)
    {
        // Not emulated, because this function is not exposed in .net bindings
        var len = Encoding.UTF8.GetByteCount(name);
        var bytes = stackalloc byte[len + 1];
        fixed (char* strPtr = name)
        {
            Encoding.UTF8.GetBytes(strPtr, name.Length, bytes, len);
        }
        return igBeginViewportSideBar(bytes, viewport, dir, size, flags) != 0;
    }
}