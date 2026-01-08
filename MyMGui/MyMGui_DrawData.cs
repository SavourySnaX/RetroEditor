using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;

namespace MyMGui;

public readonly record struct ImDrawIdx(ushort underlying);
public readonly record struct ImTextureID(UInt64 underlying);

[StructLayout(LayoutKind.Explicit,Pack = 1, Size = 8)]
internal unsafe partial struct ImTextureRect
{
    [FieldOffset(0)] public ushort x;
    [FieldOffset(2)] public ushort y;
    [FieldOffset(4)] public ushort w;
    [FieldOffset(6)] public ushort h;
};

// Specs and pixel storage for a texture used by Dear ImGui.
// This is only useful for (1) core library and (2) backends. End-user/applications do not need to care about this.
// Renderer Backends will create a GPU-side version of this.
// Why does we store two identifiers: TexID and BackendUserData?
// - ImTextureID    TexID           = lower-level identifier stored in ImDrawCmd. ImDrawCmd can refer to textures not created by the backend, and for which there's no ImTextureData.
// - void*          BackendUserData = higher-level opaque storage for backend own book-keeping. Some backends may have enough with TexID and not need both.
 // In columns below: who reads/writes each fields? 'r'=read, 'w'=write, 'core'=main library, 'backend'=renderer backend
[StructLayout(LayoutKind.Explicit,Pack = 1, Size = 88)]
internal unsafe partial struct ImTextureData
{
    //------------------------------------------ core / backend ---------------------------------------
    [FieldOffset(0)] public int                 UniqueID;               // w    -   // [DEBUG] Sequential index to facilitate identifying a texture when debugging/printing. Unique per atlas.
    [FieldOffset(4)] public ImTextureStatus     Status;                 // rw   rw  // ImTextureStatus_OK/_WantCreate/_WantUpdates/_WantDestroy. Always use SetStatus() to modify!
    [FieldOffset(8)] public void*               BackendUserData;        // -    rw  // Convenience storage for backend. Some backends may have enough with TexID.
    [FieldOffset(16)] public ImTextureID         TexID;                  // r    w   // Backend-specific texture identifier. Always use SetTexID() to modify! The identifier will stored in ImDrawCmd::GetTexID() and passed to backend's RenderDrawData function.
    [FieldOffset(24)] public ImTextureFormat     Format;                 // w    r   // ImTextureFormat_RGBA32 (default) or ImTextureFormat_Alpha8
    [FieldOffset(28)] public int                 Width;                  // w    r   // Texture width
    [FieldOffset(32)] public int                 Height;                 // w    r   // Texture height
    [FieldOffset(36)] public int                 BytesPerPixel;          // w    r   // 4 or 1
    [FieldOffset(40)] public byte*               Pixels;                 // w    r   // Pointer to buffer holding 'Width*Height' pixels and 'Width*Height*BytesPerPixels' bytes.
    [FieldOffset(48)] public ImTextureRect       UsedRect;               // w    r   // Bounding box encompassing all past and queued Updates[].
    [FieldOffset(56)] public ImTextureRect       UpdateRect;             // w    r   // Bounding box encompassing all queued Updates[].
    [FieldOffset(64)] public ImVector<ImTextureRect> Updates;            // w    r   // Array of individual updates.
    [FieldOffset(80)] public int                 UnusedFrames;           // w    r   // In order to facilitate handling Status==WantDestroy in some backend: this is a count successive frames where the texture was not used. Always >0 when Status==WantDestroy.
    [FieldOffset(84)] public ushort              RefCount;               // w    r   // Number of contexts using this texture. Used during backend shutdown.
    [FieldOffset(86)] public byte                UseColors;              // w    r   // Tell whether our texture data is known to use colors (rather than just white + alpha).
    [FieldOffset(87)] public byte                WantDestroyNextFrame;   // rw   -   // [Internal] Queued to set ImTextureStatus_WantDestroy next frame. May still be used in the current frame.
}

[StructLayout(LayoutKind.Explicit,Pack = 1, Size = 20)]
public unsafe partial struct ImDrawVert
{
    [FieldOffset(0)] public ImVec2      pos;        // 8    // Position
    [FieldOffset(8)] public ImVec2      uv;         // 8    // UV
    [FieldOffset(16)] public ImCol      col;        // 4    // Color
}

// ImTextureRef = higher-level identifier for a texture. Store a ImTextureID _or_ a ImTextureData*.
// The identifier is valid even before the texture has been uploaded to the GPU/graphics system.
// This is what gets passed to functions such as `ImGui::Image()`, `ImDrawList::AddImage()`.
// This is what gets stored in draw commands (`ImDrawCmd`) to identify a texture during rendering.
// - When a texture is created by user code (e.g. custom images), we directly stores the low-level ImTextureID.
//   Because of this, when displaying your own texture you are likely to ever only manage ImTextureID values on your side.
// - When a texture is created by the backend, we stores a ImTextureData* which becomes an indirection
//   to extract the ImTextureID value during rendering, after texture upload has happened.
// - To create a ImTextureRef from a ImTextureData you can use ImTextureData::GetTexRef().
//   We intentionally do not provide an ImTextureRef constructor for this: we don't expect this
//   to be frequently useful to the end-user, and it would be erroneously called by many legacy code.
// - If you want to bind the current atlas when using custom rectangle, you can use io.Fonts->TexRef.
// - Binding generators for languages such as C (which don't have constructors), should provide a helper, e.g.
//      inline ImTextureRef ImTextureRefFromID(ImTextureID tex_id) { ImTextureRef tex_ref = { ._TexData = NULL, .TexID = tex_id }; return tex_ref; }
// In 1.92 we changed most drawing functions using ImTextureID to use ImTextureRef.
// We intentionally do not provide an implicit ImTextureRef -> ImTextureID cast operator because it is technically lossy to convert ImTextureRef to ImTextureID before rendering.
[StructLayout(LayoutKind.Explicit,Pack = 1, Size = 16)]
public unsafe partial struct ImTextureRef
{
    // Members (either are set, never both!)
    [FieldOffset(0)] public ImTextureDataPtr      _TexData;           //      A texture, generally owned by a ImFontAtlas. Will convert to ImTextureID during render loop, after texture has been uploaded.
    [FieldOffset(8)] public ImTextureID         _TexID;             // _OR_ Low-level backend texture identifier, if already uploaded or created by user/app. Generally provided to e.g. ImGui::Image() calls.

    public ImTextureRef(ImTextureID texID)
    {
        _TexData = new ImTextureDataPtr(null);
        _TexID = texID;
    }

    public ImTextureID GetID()
    {
        if (_TexData.NativePtr != null)
        {
            return _TexData.TexID;
        }
        return _TexID;
    }

}


// Typically, 1 command = 1 GPU draw call (unless command is a callback)
// - VtxOffset: When 'io.BackendFlags & ImGuiBackendFlags_RendererHasVtxOffset' is enabled,
//   this fields allow us to render meshes larger than 64K vertices while keeping 16-bit indices.
//   Backends made for <1.71. will typically ignore the VtxOffset fields.
// - The ClipRect/TexRef/VtxOffset fields must be contiguous as we memcmp() them together (this is asserted for).
[StructLayout(LayoutKind.Explicit,Pack = 1,Size = 72)]
public unsafe partial struct ImDrawCmd
{
    [FieldOffset(0)] public ImVec4          ClipRect;           // 4*4  // Clipping rectangle (x1, y1, x2, y2). Subtract ImDrawData->DisplayPos to get clipping rectangle in "viewport" coordinates
    [FieldOffset(16)] public ImTextureRef    TexRef;             // 16   // Reference to a font/texture atlas (where backend called ImTextureData::SetTexID()) or to a user-provided texture ID (via e.g. ImGui::Image() calls). Both will lead to a ImTextureID value.
    [FieldOffset(32)] public uint    VtxOffset;          // 4    // Start offset in vertex buffer. ImGuiBackendFlags_RendererHasVtxOffset: always 0, otherwise may be >0 to support meshes larger than 64K vertices with 16-bit indices.
    [FieldOffset(36)] public uint    IdxOffset;          // 4    // Start offset in index buffer.
    [FieldOffset(40)] public uint    ElemCount;          // 4    // Number of indices (multiple of 3) to be rendered as triangles. Vertices are stored in the callee ImDrawList's vtx_buffer[] array, indices in idx_buffer[].
    [FieldOffset(48)] internal delegate* unmanaged[Cdecl]<ImDrawList*, ImDrawCmd*, void*> UserCallback;
    //public ImDrawCallback  UserCallback;       // 4-8  // If != NULL, call the function instead of rendering the vertices. clip_rect and texture_id will be set normally.
    [FieldOffset(56)] public void*           UserCallbackData;   // 4-8  // Callback user data (when UserCallback != NULL). If called AddCallback() with size == 0, this is a copy of the AddCallback() argument. If called AddCallback() with size > 0, this is pointing to a buffer where data is stored.
    [FieldOffset(64)] public int             UserCallbackDataSize;  // 4 // Size of callback user data when using storage, otherwise 0.
    [FieldOffset(68)] public int             UserCallbackDataOffset;// 4 // [Internal] Offset of callback user data when using storage, otherwise -1.

    public bool HasUserCallback()
    {
        return UserCallback != null;
    }

    public void CallUserCallback(ref ImDrawCmd cmd)
    {
        UserCallback(null, (ImDrawCmd*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref cmd));
    }


}

[StructLayout(LayoutKind.Explicit,Pack = 1,Size =568)]
internal unsafe partial struct ImDrawListSharedData
{
    [FieldOffset(0)] public ImVec2 TexUvWhitePixel;
    [FieldOffset(8)] public ImVec4* TexUvLines;
    [FieldOffset(16)] public ImFontAtlas_TODO* FontAtlas;
    [FieldOffset(24)] public ImFont_TODO* Font;
    [FieldOffset(32)] public float FontSize;
    [FieldOffset(36)] public float FontScale;
    [FieldOffset(40)] public float CurveTessellationTol;
    [FieldOffset(44)] public float CircleSegmentMaxError;
    [FieldOffset(48)] public float InitialFringeScale;
    [FieldOffset(52)] public ImDrawListFlags InitialFlags;
    [FieldOffset(56)] public ImVec4 ClipRectFullscreen;
    [FieldOffset(72)] public ImVector<ImVec2> TempBuffer;
    [FieldOffset(88)] public ImVector<ImDrawListPtr> DrawLists;
    [FieldOffset(104)] public ImGuiContextPtr Context;
    [FieldOffset(112)] public fixed UInt64 ArcFastVtx[48]; /* ImVec2[48] */
    [FieldOffset(496)] public float ArcFastRadiusCutoff;
    [FieldOffset(500)] public fixed byte CircleSegmentCounts[64];
}

[StructLayout(LayoutKind.Explicit,Pack = 1,Size = 40)]
internal unsafe partial struct ImDrawCmdHeader
{
    [FieldOffset(0)] public ImVec4 ClipRect;
    [FieldOffset(16)] public ImTextureRef TexRef;
    [FieldOffset(32)] public uint VtxOffset;
}

[StructLayout(LayoutKind.Explicit,Pack = 1, Size = 32)]
internal unsafe partial struct ImDrawChannel
{
    [FieldOffset(0)] public ImVector<ImDrawCmd> _CmdBuffer;
    [FieldOffset(16)] public ImVector<ImDrawIdx> _IdxBuffer;
};


[StructLayout(LayoutKind.Explicit,Pack = 1, Size = 24)]
internal unsafe partial struct ImDrawListSplitter
{
    [FieldOffset(0)] public int _Current;
    [FieldOffset(4)] public int _Count;
    [FieldOffset(8)] public ImVector<ImDrawChannel> _Channels;
};

// Draw command list
// This is the low-level list of polygons that ImGui:: functions are filling. At the end of the frame,
// all command lists are passed to your ImGuiIO::RenderDrawListFn function for rendering.
// Each dear imgui window contains its own ImDrawList. You can use ImGui::GetWindowDrawList() to
// access the current window draw list and draw custom primitives.
// You can interleave normal ImGui:: calls and adding primitives to the current draw list.
// In single viewport mode, top-left is == GetMainViewport()->Pos (generally 0,0), bottom-right is == GetMainViewport()->Pos+Size (generally io.DisplaySize).
// You are totally free to apply whatever transformation matrix you want to the data (depending on the use of the transformation you may want to apply it to ClipRect as well!)
// Important: Primitives are always added to the list and not culled (culling is done at higher-level by ImGui:: functions), if you use this API a lot consider coarse culling your drawn objects.
[StructLayout(LayoutKind.Explicit,Pack = 1,Size = 224)]
internal unsafe partial struct ImDrawList
{
    // This is what you have to render
    [FieldOffset(0)] public ImVector<ImDrawCmd>     CmdBuffer;          // Draw commands. Typically 1 command = 1 GPU draw call, unless the command is a callback.
    [FieldOffset(16)] public ImVector<ImDrawIdx>     IdxBuffer;          // Index buffer. Each command consume ImDrawCmd::ElemCount of those
    [FieldOffset(32)] public ImVector<ImDrawVert>    VtxBuffer;          // Vertex buffer.
    [FieldOffset(48)] public ImDrawListFlags         Flags;              // Flags, you may poke into these to adjust anti-aliasing settings per-primitive.

    // [Internal, used while building lists]
    [FieldOffset(52)] public uint            _VtxCurrentIdx;     // [Internal] generally == VtxBuffer.Size unless we are past 64K vertices, in which case this gets reset to 0.
    [FieldOffset(56)] public ImDrawListSharedData*   _Data;              // Pointer to shared draw data (you can use ImGui::GetDrawListSharedData() to get the one from current ImGui context)
    [FieldOffset(64)] public ImDrawVert*             _VtxWritePtr;       // [Internal] point within VtxBuffer.Data after each add command (to avoid using the ImVector<> operators too much)
    [FieldOffset(72)] public ImDrawIdx*              _IdxWritePtr;       // [Internal] point within IdxBuffer.Data after each add command (to avoid using the ImVector<> operators too much)
    [FieldOffset(80)] public ImVector<ImVec2>        _Path;              // [Internal] current path building
    [FieldOffset(96)] public ImDrawCmdHeader         _CmdHeader;         // [Internal] template of active commands. Fields should match those of CmdBuffer.back().
    [FieldOffset(136)] public ImDrawListSplitter      _Splitter;          // [Internal] for channels api (note: prefer using your own persistent instance of ImDrawListSplitter!)
    [FieldOffset(160)] public ImVector<ImVec4>        _ClipRectStack;     // [Internal]
    [FieldOffset(176)] public ImVector<ImTextureRef>  _TextureStack;      // [Internal]
    [FieldOffset(192)] public ImVector<byte>          _CallbacksDataBuf;  // [Internal]
    [FieldOffset(208)] public float                   _FringeScale;       // [Internal] anti-alias fringe is scaled by this value, this helps to keep things sharp while zooming at vertex buffer content
    [FieldOffset(216)] public byte*             _OwnerName;         // Pointer to owner window's name for debugging
}

// All draw data to render a Dear ImGui frame
// (NB: the style and the naming convention here is a little inconsistent, we currently preserve them for backward compatibility purpose,
// as this is one of the oldest structure exposed by the library! Basically, ImDrawList == CmdList)
[StructLayout(LayoutKind.Explicit,Pack = 1,Size = 72)]
internal unsafe partial struct ImDrawData
{
    [FieldOffset(0)] public byte                Valid;              // Only valid after Render() is called and before the next NewFrame() is called.
    [FieldOffset(4)] public int                 CmdListsCount;      // == CmdLists.Size. (OBSOLETE: exists for legacy reasons). Number of ImDrawList* to render.
    [FieldOffset(8)] public int                 TotalIdxCount;      // For convenience, sum of all ImDrawList's IdxBuffer.Size
    [FieldOffset(12)] public int                 TotalVtxCount;      // For convenience, sum of all ImDrawList's VtxBuffer.Size
    [FieldOffset(16)] public ImVector<ImDrawListPtr> CmdLists;         // Array of ImDrawList* to render. The ImDrawLists are owned by ImGuiContext and only pointed to from here.
    [FieldOffset(32)] public ImVec2              DisplayPos;         // Top-left position of the viewport to render (== top-left of the orthogonal projection matrix to use) (== GetMainViewport()->Pos for the main viewport, == (0.0) in most single-viewport applications)
    [FieldOffset(40)] public ImVec2              DisplaySize;        // Size of the viewport to render (== GetMainViewport()->Size for the main viewport, == io.DisplaySize in most single-viewport applications)
    [FieldOffset(48)] public ImVec2              FramebufferScale;   // Amount of pixels for each unit of DisplaySize. Copied from viewport->FramebufferScale (== io.DisplayFramebufferScale for main viewport). Generally (1,1) on normal display, (2,2) on OSX with Retina display.
    [FieldOffset(56)] public ImGuiViewportPtr      OwnerViewport;      // Viewport carrying the ImDrawData instance, might be of use to the renderer (generally not).
    [FieldOffset(64)] public ImVector<ImTextureDataPtr>* Textures;     // List of textures to update. Most of the times the list is shared by all ImDrawData, has only 1 texture and it doesn't need any update. This almost always points to ImGui::GetPlatformIO().Textures[]. May be overriden or set to NULL if you want to manually update textures.

    // Functions
    // see ImDrawDataPtr
}

public unsafe struct ImDrawDataPtr
{
    private ImDrawData* _ptr;

    internal ImDrawDataPtr(ImDrawData* ptr)
    {
        _ptr = ptr;
    }

    internal readonly ImDrawData* NativePtr => _ptr;

    public ref int CmdListsCount => ref _ptr->CmdListsCount;
    public ref ImVector<ImDrawListPtr> CmdLists => ref _ptr->CmdLists;
    public readonly bool HasTextures => _ptr->Textures != null;
    public ref ImVector<ImTextureDataPtr> Textures => ref *_ptr->Textures;

    public readonly ImVec2 DisplayPos => _ptr->DisplayPos;
    public readonly ImVec2 DisplaySize => _ptr->DisplaySize;
}

public unsafe struct ImTextureDataPtr
{
    private ImTextureData* _ptr;

    internal ImTextureDataPtr(ImTextureData* ptr)
    {
        _ptr = ptr;
    }

    internal readonly ImTextureData* NativePtr => _ptr;

    public ImTextureStatus Status => _ptr->Status;
    public ImTextureID TexID => ImGui.ImTextureData_GetTexID(_ptr);
    public int UnusedFrames => _ptr->UnusedFrames;
    public int Width => _ptr->Width;
    public int Height => _ptr->Height;
    public ImTextureFormat Format => _ptr->Format;

    public ReadOnlySpan<byte> GetPixelsAsSpan()
    {
        var size = ImGui.ImTextureData_GetSizeInBytes(_ptr);
        return new ReadOnlySpan<byte>(ImGui.ImTextureData_GetPixels(_ptr), size);
    }

    public int GetPitch()
    {
        return ImGui.ImTextureData_GetPitch(_ptr);
    }

    public void* GetPixels()
    {
        return ImGui.ImTextureData_GetPixels(_ptr);
    }

    public void SetTexID(ImTextureID id)
    {
        ImGui.ImTextureData_SetTexID(_ptr, id);
    }

    public void SetStatus(ImTextureStatus status)
    {
        ImGui.ImTextureData_SetStatus(_ptr, status);
    }
}

public unsafe struct ImDrawListPtr
{
    private ImDrawList* _ptr;

    internal ImDrawListPtr(ImDrawList* ptr)
    {
        _ptr = ptr;
    }

    internal readonly ImDrawList* NativePtr => _ptr;
    public ref ImVector<ImDrawCmd> CmdBuffer => ref _ptr->CmdBuffer;
    public ref ImVector<ImDrawIdx> IdxBuffer => ref _ptr->IdxBuffer;
    public ref ImVector<ImDrawVert> VtxBuffer => ref _ptr->VtxBuffer;
    public ref ImDrawListFlags Flags => ref _ptr->Flags;

    public void AddRect(ImVec2 p_min, ImVec2 p_max, ImCol col, float rounding = default, ImDrawFlags flags = ImDrawFlags.None, float thickness = 1.0f)
    {
        ImGui.ImDrawList_AddRect(_ptr, p_min, p_max, col, rounding, flags, thickness);
    }

    public void AddRectFilled(ImVec2 p_min, ImVec2 p_max, ImCol col, float rounding = default, ImDrawFlags flags = ImDrawFlags.None)
    {
        ImGui.ImDrawList_AddRectFilled(_ptr, p_min, p_max, col, rounding, flags);
    }

    public void AddText(ImVec2 pos, ImCol col, ReadOnlySpan<char> text)
    {
        ImGui.ImDrawList_AddText(_ptr, pos, col, text);
    }

    public void AddImage(ImTextureRef texture, ImVec2 p_min, ImVec2 p_max)
    {
        var uv_max = new ImVec2(1.0f, 1.0f);
        var col = new ImCol(0xFFFFFFFF);
        ImGui.ImDrawList_AddImage(_ptr, texture, p_min, p_max, default, uv_max, col);
    }

    public void AddImage(ImTextureRef texture, ImVec2 p_min, ImVec2 p_max, ImVec2 uv_min, ImVec2 uv_max)
    {
        var col = new ImCol(0xFFFFFFFF);
        ImGui.ImDrawList_AddImage(_ptr, texture, p_min, p_max, uv_min, uv_max, col);
    }

    public void AddImage(ImTextureRef texture, ImVec2 p_min, ImVec2 p_max, ImVec2 uv_min, ImVec2 uv_max, ImCol col)
    {
        ImGui.ImDrawList_AddImage(_ptr, texture, p_min, p_max, uv_min, uv_max, col);
    }

    public void PushClipRect(ImVec2 clip_rect_min, ImVec2 clip_rect_max, bool intersect_with_current_clip_rect)
    {
        ImGui.ImDrawList_PushClipRect(_ptr, clip_rect_min, clip_rect_max, intersect_with_current_clip_rect);
    }

    public void PushClipRect()
    {
        ImGui.ImDrawList_PushClipRectFullScreen(_ptr);
    }

    public void PopClipRect()
    {
        ImGui.ImDrawList_PopClipRect(_ptr);
    }

}