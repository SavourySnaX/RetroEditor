using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// This is a workaround for Windows arm64 builds (but I assume it would mac and linux), where the interface in ImGui.NET is
// incorrect when Vector2 (or other small <16bytes structures) are passed by value
//For now, any methods we use that pass Vector2 by value will be wrapped here to pass the value in a compatible way
//if running on Arm64, otherwise we can call ImGui.NET directly

namespace RetroEditor.Source.Internals.GUI
{
    internal unsafe static class AbiSafe_ImGuiWrapper
    {
        static nint cimguiHandle = nint.Zero;
        static delegate* unmanaged[Cdecl]<byte*, UInt64, ImGuiNET.ImGuiChildFlags, ImGuiNET.ImGuiWindowFlags, byte> igBeginChild_Str;
        static delegate* unmanaged[Cdecl]<byte*, UInt64, byte> igButton;
        static delegate* unmanaged[Cdecl]<nint, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, void> igImage;
        static delegate* unmanaged[Cdecl]<byte*, byte, ImGuiSelectableFlags, UInt64, byte> igSelectable_Bool;
        static delegate* unmanaged[Cdecl]<ImGuiStyleVar, UInt64, void> igPushStyleVar_Vec2;
        static delegate* unmanaged[Cdecl]<byte*, int, ImGuiTableFlags, UInt64, float, byte> igBeginTable;
        static delegate* unmanaged[Cdecl]<UInt64,UInt64,uint> igGetColorU32_Vec4;
        static delegate* unmanaged[Cdecl]<ImDrawList*, nint, UInt64, UInt64, UInt64, UInt64, uint, void> ImDrawList_AddImage;
        static delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, uint, float, ImDrawFlags, float, void> ImDrawList_AddRect;
        static delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, uint, float, ImDrawFlags, void> ImDrawList_AddRectFilled;
        static delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, byte, void> ImDrawList_PushClipRect;
        static delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, uint, byte*, byte*, void> ImDrawList_AddText;
        static AbiSafe_ImGuiWrapper()
        {
            cimguiHandle = NativeLibrary.Load("cimgui");
            var method = NativeLibrary.GetExport(cimguiHandle, "igBeginChild_Str");
            var method2 = NativeLibrary.GetExport(cimguiHandle, "igButton");
            var method3 = NativeLibrary.GetExport(cimguiHandle, "igImage");
            var method4 = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddImage");
            var method5 = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddRect");
            var method6 = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddRectFilled");
            var method7 = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_PushClipRect");
            var method8 = NativeLibrary.GetExport(cimguiHandle, "igSelectable_Bool");
            var method9 = NativeLibrary.GetExport(cimguiHandle, "igPushStyleVar_Vec2");
            var method10 = NativeLibrary.GetExport(cimguiHandle, "ImDrawList_AddText_Vec2");
            var method11 = NativeLibrary.GetExport(cimguiHandle, "igBeginTable");
            var method12 = NativeLibrary.GetExport(cimguiHandle, "igGetColorU32_Vec4");

            // Cache the delegates
            igBeginChild_Str = (delegate* unmanaged[Cdecl]<byte*, UInt64, ImGuiNET.ImGuiChildFlags, ImGuiNET.ImGuiWindowFlags, byte>)method;
            igButton = (delegate* unmanaged[Cdecl]<byte*, UInt64, byte>)method2;
            igImage = (delegate* unmanaged[Cdecl]<nint, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, void>)method3;
            igSelectable_Bool = (delegate* unmanaged[Cdecl]<byte*, byte, ImGuiSelectableFlags, UInt64, byte>)method8;
            igPushStyleVar_Vec2 = (delegate* unmanaged[Cdecl]<ImGuiStyleVar, UInt64, void>)method9;
            igBeginTable = (delegate* unmanaged[Cdecl]<byte*, int, ImGuiTableFlags, UInt64, float, byte>)method11;
            igGetColorU32_Vec4 = (delegate* unmanaged[Cdecl]<UInt64,UInt64,uint>)method12;
            ImDrawList_AddImage = (delegate* unmanaged[Cdecl]<ImDrawList*, nint, UInt64, UInt64, UInt64, UInt64, uint, void>)method4;
            ImDrawList_AddRect = (delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, uint, float, ImDrawFlags, float, void>)method5;
            ImDrawList_AddRectFilled = (delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, uint, float, ImDrawFlags, void>)method6;
            ImDrawList_PushClipRect = (delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, UInt64, byte, void>)method7;
            ImDrawList_AddText = (delegate* unmanaged[Cdecl]<ImDrawList*, UInt64, uint, byte*, byte*, void>)method10;
        }

        public static uint GetColorU32(Vector4 col)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var tArray = stackalloc float[4];
                tArray[0] = col.X;
                tArray[1] = col.Y;
                tArray[2] = col.Z;
                tArray[3] = col.W;
                UInt64 combinedColA = *((UInt64*)Unsafe.AsPointer(ref tArray[0]));
                UInt64 combinedColB = *((UInt64*)Unsafe.AsPointer(ref tArray[2]));
                return igGetColorU32_Vec4(combinedColA, combinedColB);
            }
            else
            {
                return ImGuiNET.ImGui.GetColorU32(col);
            }
        }

        public static bool Button(string str_id, Vector2 size = default)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var len = str_id == null ? 0 : Encoding.UTF8.GetByteCount(str_id);
                var bytes = stackalloc byte[len + 1];
                if (str_id != null)
                {
                    fixed (char* strPtr = str_id)
                    {
                        Encoding.UTF8.GetBytes(strPtr, str_id.Length, bytes, len);
                    }
                }
                var tArray = stackalloc float[2];
                tArray[0] = size.X;
                tArray[1] = size.Y;
                UInt64 combinedSize = *((UInt64*)Unsafe.AsPointer(ref tArray[0]));
                return igButton(bytes, combinedSize) != 0;
            }
            else
            {
                return ImGuiNET.ImGui.Button(str_id, size);
            }
        }

        public static bool BeginChild(string str_id, System.Numerics.Vector2 size = default, ImGuiNET.ImGuiChildFlags cFlags= ImGuiNET.ImGuiChildFlags.None, ImGuiNET.ImGuiWindowFlags wFlags= ImGuiNET.ImGuiWindowFlags.None)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var len = str_id == null ? 0 : Encoding.UTF8.GetByteCount(str_id);
                var bytes = stackalloc byte[len + 1];
                if (str_id != null)
                {
                    fixed (char* strPtr = str_id)
                    {
                        Encoding.UTF8.GetBytes(strPtr, str_id.Length, bytes, len);
                    }
                }
                var tArray = stackalloc float[2];
                tArray[0] = size.X;
                tArray[1] = size.Y;
                UInt64 combinedSize = *((UInt64*)Unsafe.AsPointer(ref tArray[0]));
                return igBeginChild_Str(bytes, combinedSize, cFlags, wFlags) != 0;
            }
            else
            {
                return ImGuiNET.ImGui.BeginChild(str_id, size, cFlags, wFlags);
            }
        }

        public static bool BeginTable(string str_id, int column, ImGuiTableFlags flags = ImGuiTableFlags.None, Vector2 outer_size = default, float inner_width = 0.0f)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var len = str_id == null ? 0 : Encoding.UTF8.GetByteCount(str_id);
                var bytes = stackalloc byte[len + 1];
                if (str_id != null)
                {
                    fixed (char* strPtr = str_id)
                    {
                        Encoding.UTF8.GetBytes(strPtr, str_id.Length, bytes, len);
                    }
                }
                var tArray = stackalloc float[2];
                tArray[0] = outer_size.X;
                tArray[1] = outer_size.Y;
                UInt64 combinedSize = *((UInt64*)Unsafe.AsPointer(ref tArray[0]));

                return igBeginTable(bytes, column, flags, combinedSize, inner_width) != 0;
            }
            else
            {
                return ImGuiNET.ImGui.BeginTable(str_id, column, flags, outer_size, inner_width);
            }
        }

        public static bool Selectable(string label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 size = default)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var len = label == null ? 0 : Encoding.UTF8.GetByteCount(label);
                var bytes = stackalloc byte[len + 1];
                if (label != null)
                {
                    fixed (char* strPtr = label)
                    {
                        Encoding.UTF8.GetBytes(strPtr, label.Length, bytes, len);
                    }
                }
                var tArray = stackalloc float[2];
                tArray[0] = size.X;
                tArray[1] = size.Y;
                UInt64 combinedSize = *((UInt64*)Unsafe.AsPointer(ref tArray[0]));

                return igSelectable_Bool(bytes, (byte)(selected ? 1 : 0), flags, 0) != 0;
            }
            else
            {
                return ImGuiNET.ImGui.Selectable(label, selected, flags, size);
            }
        }

        public static void PushStyleVar(ImGuiStyleVar idx, Vector2 val)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var tArray = stackalloc float[2];
                tArray[0] = val.X;
                tArray[1] = val.Y;
                UInt64 combinedVal = *((UInt64*)Unsafe.AsPointer(ref tArray[0]));
                igPushStyleVar_Vec2(idx, combinedVal);
            }
            else
            {
                ImGuiNET.ImGui.PushStyleVar(idx, val);
            }
        }

        public static void DrawList_AddImage(ImDrawListPtr list, nint user_texture_id, Vector2 a, Vector2 b)
        {
            var uv1 = new Vector2(1.0f, 1.0f);
            DrawList_AddImage(list, user_texture_id, a, b, (Vector2)default, uv1);
        }

        public static void DrawList_AddImage(ImDrawListPtr list, nint user_texture_id, Vector2 a, Vector2 b, Vector2 uv0, Vector2 uv1)
        {
            DrawList_AddImage(list, user_texture_id, a, b, uv0, uv1, 0xFFFFFFFF);
        }

        public static void DrawList_AddImage(ImDrawListPtr list, nint user_texture_id, Vector2 a, Vector2 b, Vector2 uv0, Vector2 uv1, uint col)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var aArray = stackalloc float[2];
                aArray[0] = a.X;
                aArray[1] = a.Y;
                UInt64 combinedA = *((UInt64*)Unsafe.AsPointer(ref aArray[0]));

                var bArray = stackalloc float[2];
                bArray[0] = b.X;
                bArray[1] = b.Y;
                UInt64 combinedB = *((UInt64*)Unsafe.AsPointer(ref bArray[0]));

                var uv0Array = stackalloc float[2];
                uv0Array[0] = uv0.X;
                uv0Array[1] = uv0.Y;
                UInt64 combinedUv0 = *((UInt64*)Unsafe.AsPointer(ref uv0Array[0]));

                var uv1Array = stackalloc float[2];
                uv1Array[0] = uv1.X;
                uv1Array[1] = uv1.Y;
                UInt64 combinedUv1 = *((UInt64*)Unsafe.AsPointer(ref uv1Array[0]));

                ImDrawList_AddImage(list, user_texture_id, combinedA, combinedB, combinedUv0, combinedUv1, col);
            }
            else
            {
                list.AddImage(user_texture_id, a, b, uv0, uv1, col);
            }
        }

        public static void DrawList_AddRect(ImDrawListPtr list, Vector2 a, Vector2 b, uint col, float rounding=0.0f, ImDrawFlags flags=ImDrawFlags.None, float thickness=1.0f)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var aArray = stackalloc float[2];
                aArray[0] = a.X;
                aArray[1] = a.Y;
                UInt64 combinedA = *((UInt64*)Unsafe.AsPointer(ref aArray[0]));

                var bArray = stackalloc float[2];
                bArray[0] = b.X;
                bArray[1] = b.Y;
                UInt64 combinedB = *((UInt64*)Unsafe.AsPointer(ref bArray[0]));

                ImDrawList_AddRect(list, combinedA, combinedB, col, rounding, flags, thickness);
            }
            else
            {
                list.AddRect(a, b, col, rounding, flags, thickness);
            }
        }

        public static void DrawList_AddRectFilled(ImDrawListPtr list, Vector2 a, Vector2 b, uint col, float rounding=0.0f, ImDrawFlags flags=ImDrawFlags.None)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var aArray = stackalloc float[2];
                aArray[0] = a.X;
                aArray[1] = a.Y;
                UInt64 combinedA = *((UInt64*)Unsafe.AsPointer(ref aArray[0]));

                var bArray = stackalloc float[2];
                bArray[0] = b.X;
                bArray[1] = b.Y;
                UInt64 combinedB = *((UInt64*)Unsafe.AsPointer(ref bArray[0]));

                ImDrawList_AddRectFilled(list, combinedA, combinedB, col, rounding, flags);
            }
            else
            {
                list.AddRectFilled(a, b, col, rounding, flags);
            }
        }

        public static void DrawList_PushClipRect(ImDrawListPtr list, Vector2 a, Vector2 b, bool intersectWithCurrentClipRect)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var aArray = stackalloc float[2];
                aArray[0] = a.X;
                aArray[1] = a.Y;
                UInt64 combinedA = *((UInt64*)Unsafe.AsPointer(ref aArray[0]));

                var bArray = stackalloc float[2];
                bArray[0] = b.X;
                bArray[1] = b.Y;
                UInt64 combinedB = *((UInt64*)Unsafe.AsPointer(ref bArray[0]));

                ImDrawList_PushClipRect(list, combinedA, combinedB, (byte)(intersectWithCurrentClipRect ? 1 : 0));
            }
            else
            {
                list.PushClipRect(a, b, intersectWithCurrentClipRect);
            }
        }

        public static void DrawList_AddText(ImDrawListPtr list, Vector2 pos, uint col, string text)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var len = text == null ? 0 : Encoding.UTF8.GetByteCount(text);
                var bytes = stackalloc byte[len + 1];
                if (text != null)
                {
                    fixed (char* strPtr = text)
                    {
                        Encoding.UTF8.GetBytes(strPtr, text.Length, bytes, len);
                    }
                }

                var posArray = stackalloc float[2];
                posArray[0] = pos.X;
                posArray[1] = pos.Y;
                UInt64 combinedPos = *((UInt64*)Unsafe.AsPointer(ref posArray[0]));

                ImDrawList_AddText(list, combinedPos, col, bytes, bytes + len);
            }
            else
            {
                list.AddText(pos, col, text);
            }
        }

        public static void Image(nint user_texture_id, Vector2 size)
        {
            var uv1 = new Vector2(1.0f, 1.0f);
            Image(user_texture_id, size, (Vector2)default, uv1);
        }

        public static void Image(nint user_texture_id, Vector2 size, Vector2 uv0, Vector2 uv1)
        {
            var tint_col = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            Image(user_texture_id, size, uv0, uv1, tint_col);
        }

        public static void Image(nint user_texture_id, Vector2 size, Vector2 uv0, Vector2 uv1, Vector4 tint_col, Vector4 border_col=default)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var sizeArray = stackalloc float[2];
                sizeArray[0] = size.X;
                sizeArray[1] = size.Y;
                UInt64 combinedSize = *((UInt64*)Unsafe.AsPointer(ref sizeArray[0]));

                var uv0Array = stackalloc float[2];
                uv0Array[0] = uv0.X;
                uv0Array[1] = uv0.Y;
                UInt64 combinedUv0 = *((UInt64*)Unsafe.AsPointer(ref uv0Array[0]));

                var uv1Array = stackalloc float[2];
                uv1Array[0] = uv1.X;
                uv1Array[1] = uv1.Y;
                UInt64 combinedUv1 = *((UInt64*)Unsafe.AsPointer(ref uv1Array[0]));

                var tintArray = stackalloc float[4];
                tintArray[0] = tint_col.X;
                tintArray[1] = tint_col.Y;
                tintArray[2] = tint_col.Z;
                tintArray[3] = tint_col.W;
                UInt64 combinedTintA = *((UInt64*)Unsafe.AsPointer(ref tintArray[0]));
                UInt64 combinedTintB = *((UInt64*)Unsafe.AsPointer(ref tintArray[2]));

                var borderArray = stackalloc float[4];
                borderArray[0] = border_col.X;
                borderArray[1] = border_col.Y;
                borderArray[2] = border_col.Z;
                borderArray[3] = border_col.W;
                UInt64 combinedBorderA = *((UInt64*)Unsafe.AsPointer(ref borderArray[0]));
                UInt64 combinedBorderB = *((UInt64*)Unsafe.AsPointer(ref borderArray[2]));

                igImage(user_texture_id, combinedSize, combinedUv0, combinedUv1, combinedTintA, combinedTintB, combinedBorderA, combinedBorderB);
            }
            else
            {
                ImGuiNET.ImGui.Image(user_texture_id, size, uv0, uv1, tint_col, border_col);
            }
        }

    }
}
