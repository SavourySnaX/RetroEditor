/*******************************************************************************************
*
*   raylib-extras [ImGui] example - Simple Integration
*
*	This is a simple ImGui Integration
*	It is done using C++ but with C style code
*	It can be done in C as well if you use the C ImGui wrapper
*	https://github.com/cimgui/cimgui
*   https://github.com/raylib-extras/rlImGui-cs (slightly modified to fix font rendering when maximised)
*
*   Copyright (c) 2021 Jeffery Myers
*
********************************************************************************************/

using System.Numerics;
using System.Runtime.InteropServices;

using Raylib_cs.BleedingEdge;
using MyMGui;
using System.Runtime.CompilerServices;

namespace rlImGui_cs
{
    internal static class rlImGui
    {
        internal static ImGuiContextPtr ImGuiContext = default!;

        private static ImGuiMouseCursor CurrentMouseCursor = ImGuiMouseCursor.COUNT;
        private static Dictionary<ImGuiMouseCursor, MouseCursor> MouseCursorMap = new Dictionary<ImGuiMouseCursor, MouseCursor>();
        private static Dictionary<uint, Texture2D> imGuiTextureMap = new Dictionary<uint, Texture2D>();

        static Dictionary<KeyboardKey, ImGuiKey> RaylibKeyMap = new Dictionary<KeyboardKey, ImGuiKey>();

        internal static bool LastFrameFocused = false;

        internal static bool LastControlPressed = false;
        internal static bool LastShiftPressed = false;
        internal static bool LastAltPressed = false;
        internal static bool LastSuperPressed = false;
        internal static bool windowIsHighDPI = false;

        internal static bool rlImGuiIsControlDown() { return Raylib.IsKeyDown(KeyboardKey.RightControl) || Raylib.IsKeyDown(KeyboardKey.LeftControl); }
        internal static bool rlImGuiIsShiftDown() { return Raylib.IsKeyDown(KeyboardKey.RightShift) || Raylib.IsKeyDown(KeyboardKey.LeftShift); }
        internal static bool rlImGuiIsAltDown() { return Raylib.IsKeyDown(KeyboardKey.RightAlt) || Raylib.IsKeyDown(KeyboardKey.LeftAlt); }
        internal static bool rlImGuiIsSuperDown() { return Raylib.IsKeyDown(KeyboardKey.RightSuper) || Raylib.IsKeyDown(KeyboardKey.LeftSuper); }

        internal delegate void SetupUserFontsCallback(ImGuiIOPtr imGuiIo);
        internal static SetupUserFontsCallback? SetupUserFonts = null;

        internal static void Setup(bool darkTheme = true, bool enableDocking = false)
        {
            windowIsHighDPI = Raylib.IsWindowState(ConfigFlags.WindowHighDpi) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            BeginInitImGui();

            if (darkTheme)
                ImGui.StyleColorsDark();
            else
                ImGui.StyleColorsLight();

            if (enableDocking)
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

            ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;
            ImGui.GetIO().ConfigDpiScaleFonts = true;
            ImGui.GetIO().ConfigDpiScaleViewports = true;

            EndInitImGui();
        }

        internal static void BeginInitImGui()
        {
            MouseCursorMap = new Dictionary<ImGuiMouseCursor, MouseCursor>();

            LastFrameFocused = Raylib.IsWindowFocused();
            LastControlPressed = false;
            LastShiftPressed = false;
            LastAltPressed = false;
            LastSuperPressed = false;

            SetupKeymap();

            ImGuiContext = ImGui.CreateContext();
        }

        internal static void SetupKeymap()
        {
            if (RaylibKeyMap.Count > 0)
                return;

            // build up a map of raylib keys to ImGuiKeys
            RaylibKeyMap[KeyboardKey.Apostrophe] = ImGuiKey.Apostrophe;
            RaylibKeyMap[KeyboardKey.Comma] = ImGuiKey.Comma;
            RaylibKeyMap[KeyboardKey.Minus] = ImGuiKey.Minus;
            RaylibKeyMap[KeyboardKey.Period] = ImGuiKey.Period;
            RaylibKeyMap[KeyboardKey.Slash] = ImGuiKey.Slash;
            RaylibKeyMap[KeyboardKey.Zero] = ImGuiKey._0;
            RaylibKeyMap[KeyboardKey.One] = ImGuiKey._1;
            RaylibKeyMap[KeyboardKey.Two] = ImGuiKey._2;
            RaylibKeyMap[KeyboardKey.Three] = ImGuiKey._3;
            RaylibKeyMap[KeyboardKey.Four] = ImGuiKey._4;
            RaylibKeyMap[KeyboardKey.Five] = ImGuiKey._5;
            RaylibKeyMap[KeyboardKey.Six] = ImGuiKey._6;
            RaylibKeyMap[KeyboardKey.Seven] = ImGuiKey._7;
            RaylibKeyMap[KeyboardKey.Eight] = ImGuiKey._8;
            RaylibKeyMap[KeyboardKey.Nine] = ImGuiKey._9;
            RaylibKeyMap[KeyboardKey.Semicolon] = ImGuiKey.Semicolon;
            RaylibKeyMap[KeyboardKey.Equal] = ImGuiKey.Equal;
            RaylibKeyMap[KeyboardKey.A] = ImGuiKey.A;
            RaylibKeyMap[KeyboardKey.B] = ImGuiKey.B;
            RaylibKeyMap[KeyboardKey.C] = ImGuiKey.C;
            RaylibKeyMap[KeyboardKey.D] = ImGuiKey.D;
            RaylibKeyMap[KeyboardKey.E] = ImGuiKey.E;
            RaylibKeyMap[KeyboardKey.F] = ImGuiKey.F;
            RaylibKeyMap[KeyboardKey.G] = ImGuiKey.G;
            RaylibKeyMap[KeyboardKey.H] = ImGuiKey.H;
            RaylibKeyMap[KeyboardKey.I] = ImGuiKey.I;
            RaylibKeyMap[KeyboardKey.J] = ImGuiKey.J;
            RaylibKeyMap[KeyboardKey.K] = ImGuiKey.K;
            RaylibKeyMap[KeyboardKey.L] = ImGuiKey.L;
            RaylibKeyMap[KeyboardKey.M] = ImGuiKey.M;
            RaylibKeyMap[KeyboardKey.N] = ImGuiKey.N;
            RaylibKeyMap[KeyboardKey.O] = ImGuiKey.O;
            RaylibKeyMap[KeyboardKey.P] = ImGuiKey.P;
            RaylibKeyMap[KeyboardKey.Q] = ImGuiKey.Q;
            RaylibKeyMap[KeyboardKey.R] = ImGuiKey.R;
            RaylibKeyMap[KeyboardKey.S] = ImGuiKey.S;
            RaylibKeyMap[KeyboardKey.T] = ImGuiKey.T;
            RaylibKeyMap[KeyboardKey.U] = ImGuiKey.U;
            RaylibKeyMap[KeyboardKey.V] = ImGuiKey.V;
            RaylibKeyMap[KeyboardKey.W] = ImGuiKey.W;
            RaylibKeyMap[KeyboardKey.X] = ImGuiKey.X;
            RaylibKeyMap[KeyboardKey.Y] = ImGuiKey.Y;
            RaylibKeyMap[KeyboardKey.Z] = ImGuiKey.Z;
            RaylibKeyMap[KeyboardKey.Space] = ImGuiKey.Space;
            RaylibKeyMap[KeyboardKey.Escape] = ImGuiKey.Escape;
            RaylibKeyMap[KeyboardKey.Enter] = ImGuiKey.Enter;
            RaylibKeyMap[KeyboardKey.Tab] = ImGuiKey.Tab;
            RaylibKeyMap[KeyboardKey.Backspace] = ImGuiKey.Backspace;
            RaylibKeyMap[KeyboardKey.Insert] = ImGuiKey.Insert;
            RaylibKeyMap[KeyboardKey.Delete] = ImGuiKey.Delete;
            RaylibKeyMap[KeyboardKey.Right] = ImGuiKey.RightArrow;
            RaylibKeyMap[KeyboardKey.Left] = ImGuiKey.LeftArrow;
            RaylibKeyMap[KeyboardKey.Down] = ImGuiKey.DownArrow;
            RaylibKeyMap[KeyboardKey.Up] = ImGuiKey.UpArrow;
            RaylibKeyMap[KeyboardKey.PageUp] = ImGuiKey.PageUp;
            RaylibKeyMap[KeyboardKey.PageDown] = ImGuiKey.PageDown;
            RaylibKeyMap[KeyboardKey.Home] = ImGuiKey.Home;
            RaylibKeyMap[KeyboardKey.End] = ImGuiKey.End;
            RaylibKeyMap[KeyboardKey.CapsLock] = ImGuiKey.CapsLock;
            RaylibKeyMap[KeyboardKey.ScrollLock] = ImGuiKey.ScrollLock;
            RaylibKeyMap[KeyboardKey.NumLock] = ImGuiKey.NumLock;
            RaylibKeyMap[KeyboardKey.PrintScreen] = ImGuiKey.PrintScreen;
            RaylibKeyMap[KeyboardKey.Pause] = ImGuiKey.Pause;
            RaylibKeyMap[KeyboardKey.F1] = ImGuiKey.F1;
            RaylibKeyMap[KeyboardKey.F2] = ImGuiKey.F2;
            RaylibKeyMap[KeyboardKey.F3] = ImGuiKey.F3;
            RaylibKeyMap[KeyboardKey.F4] = ImGuiKey.F4;
            RaylibKeyMap[KeyboardKey.F5] = ImGuiKey.F5;
            RaylibKeyMap[KeyboardKey.F6] = ImGuiKey.F6;
            RaylibKeyMap[KeyboardKey.F7] = ImGuiKey.F7;
            RaylibKeyMap[KeyboardKey.F8] = ImGuiKey.F8;
            RaylibKeyMap[KeyboardKey.F9] = ImGuiKey.F9;
            RaylibKeyMap[KeyboardKey.F10] = ImGuiKey.F10;
            RaylibKeyMap[KeyboardKey.F11] = ImGuiKey.F11;
            RaylibKeyMap[KeyboardKey.F12] = ImGuiKey.F12;
            RaylibKeyMap[KeyboardKey.LeftShift] = ImGuiKey.LeftShift;
            RaylibKeyMap[KeyboardKey.LeftControl] = ImGuiKey.LeftCtrl;
            RaylibKeyMap[KeyboardKey.LeftAlt] = ImGuiKey.LeftAlt;
            RaylibKeyMap[KeyboardKey.LeftSuper] = ImGuiKey.LeftSuper;
            RaylibKeyMap[KeyboardKey.RightShift] = ImGuiKey.RightShift;
            RaylibKeyMap[KeyboardKey.RightControl] = ImGuiKey.RightCtrl;
            RaylibKeyMap[KeyboardKey.RightAlt] = ImGuiKey.RightAlt;
            RaylibKeyMap[KeyboardKey.RightSuper] = ImGuiKey.RightSuper;
            RaylibKeyMap[KeyboardKey.KbMenu] = ImGuiKey.Menu;
            RaylibKeyMap[KeyboardKey.LeftBracket] = ImGuiKey.LeftBracket;
            RaylibKeyMap[KeyboardKey.Backslash] = ImGuiKey.Backslash;
            RaylibKeyMap[KeyboardKey.RightBracket] = ImGuiKey.RightBracket;
            RaylibKeyMap[KeyboardKey.Grave] = ImGuiKey.GraveAccent;
            RaylibKeyMap[KeyboardKey.Kp0] = ImGuiKey.Keypad0;
            RaylibKeyMap[KeyboardKey.Kp1] = ImGuiKey.Keypad1;
            RaylibKeyMap[KeyboardKey.Kp2] = ImGuiKey.Keypad2;
            RaylibKeyMap[KeyboardKey.Kp3] = ImGuiKey.Keypad3;
            RaylibKeyMap[KeyboardKey.Kp4] = ImGuiKey.Keypad4;
            RaylibKeyMap[KeyboardKey.Kp5] = ImGuiKey.Keypad5;
            RaylibKeyMap[KeyboardKey.Kp6] = ImGuiKey.Keypad6;
            RaylibKeyMap[KeyboardKey.Kp7] = ImGuiKey.Keypad7;
            RaylibKeyMap[KeyboardKey.Kp8] = ImGuiKey.Keypad8;
            RaylibKeyMap[KeyboardKey.Kp9] = ImGuiKey.Keypad9;
            RaylibKeyMap[KeyboardKey.KpDecimal] = ImGuiKey.KeypadDecimal;
            RaylibKeyMap[KeyboardKey.KpDivide] = ImGuiKey.KeypadDivide;
            RaylibKeyMap[KeyboardKey.KpMultiply] = ImGuiKey.KeypadMultiply;
            RaylibKeyMap[KeyboardKey.KpSubtract] = ImGuiKey.KeypadSubtract;
            RaylibKeyMap[KeyboardKey.KpAdd] = ImGuiKey.KeypadAdd;
            RaylibKeyMap[KeyboardKey.KpEnter] = ImGuiKey.KeypadEnter;
            RaylibKeyMap[KeyboardKey.KpEqual] = ImGuiKey.KeypadEqual;
        }

        private static void SetupMouseCursors()
        {
            MouseCursorMap.Clear();
            MouseCursorMap[ImGuiMouseCursor.Arrow] = MouseCursor.Arrow;
            MouseCursorMap[ImGuiMouseCursor.TextInput] = MouseCursor.IBeam;
            MouseCursorMap[ImGuiMouseCursor.Hand] = MouseCursor.PointingHand;
            MouseCursorMap[ImGuiMouseCursor.ResizeAll] = MouseCursor.ResizeAll;
            MouseCursorMap[ImGuiMouseCursor.ResizeEW] = MouseCursor.ResizeEw;
            MouseCursorMap[ImGuiMouseCursor.ResizeNESW] = MouseCursor.ResizeNesw;
            MouseCursorMap[ImGuiMouseCursor.ResizeNS] = MouseCursor.ResizeNs;
            MouseCursorMap[ImGuiMouseCursor.ResizeNWSE] = MouseCursor.ResizeNwse;
            MouseCursorMap[ImGuiMouseCursor.NotAllowed] = MouseCursor.NotAllowed;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        unsafe internal static byte* rlImGuiGetClipText(ImGuiContextPtr userData)
        {
            return Raylib.GetClipboardText();
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        unsafe internal static void rlImGuiSetClipText(ImGuiContextPtr userData, byte* text)
        {
            Raylib.SetClipboardText(text);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        unsafe internal static float rlImGuiGetWindowDpiScale(ImGuiViewportPtr viewport)
        {
            return Raylib.GetWindowScaleDPI().X;
        }

        internal static void EndInitImGui()
        {
            SetupMouseCursors();

            ImGui.SetCurrentContext(ImGuiContext);

            ImGui.GetIO().Fonts.AddFontDefault();
#if TODO_FAWESOME
            var fonts = ImGui.GetIO().Fonts;

            // remove this part if you don't want font awesome
            unsafe
            {
                ImFontConfig* icons_config = ImGuiNative.ImFontConfig_ImFontConfig();
                icons_config->MergeMode = 1;                      // merge the glyph ranges into the default font
                icons_config->PixelSnapH = 1;                     // don't try to render on partial pixels
                icons_config->FontDataOwnedByAtlas = 0;           // the font atlas does not own this font data

                icons_config->GlyphMaxAdvanceX = float.MaxValue;
                icons_config->RasterizerMultiply = 1.0f;
                icons_config->OversampleH = 2;
                icons_config->OversampleV = 1;

                ushort[] IconRanges = new ushort[3];
                IconRanges[0] = IconFonts.FontAwesome6.IconMin;
                IconRanges[1] = IconFonts.FontAwesome6.IconMax;
                IconRanges[2] = 0;

                fixed (ushort* range = &IconRanges[0])
                {
                    // this unmanaged memory must remain allocated for the entire run of rlImgui
                    IconFonts.FontAwesome6.IconFontRanges = Marshal.AllocHGlobal(6);
                    Buffer.MemoryCopy(range, IconFonts.FontAwesome6.IconFontRanges.ToPointer(), 6, 6);
                    icons_config->GlyphRanges = (ushort*)IconFonts.FontAwesome6.IconFontRanges.ToPointer();

                    byte[] fontDataBuffer = Convert.FromBase64String(IconFonts.FontAwesome6.IconFontData);

                    fixed (byte* buffer = fontDataBuffer)
                    {
                        var fontPtr = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(new IntPtr(buffer), fontDataBuffer.Length, 11, icons_config, IconFonts.FontAwesome6.IconFontRanges);
                    }
                }

                ImGuiNative.ImFontConfig_destroy(icons_config);
            }
#endif


            ImGuiIOPtr io = ImGui.GetIO();

            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();

            if (SetupUserFonts != null)
                SetupUserFonts(io);

            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.HasGamepad |
                              ImGuiBackendFlags.RendererHasTextures;

            io.MousePos.X = 0;
            io.MousePos.Y = 0;

            unsafe
            {
                // copy/paste callbacks
                platformIO.Platform_SetClipboardTextFn = &rlImGuiSetClipText;

                platformIO.Platform_GetClipboardTextFn = &rlImGuiGetClipText;

                platformIO.Platform_ClipboardUserData = (void*)0;

                // dpi
                platformIO.Platform_GetWindowDpiScale = &rlImGuiGetWindowDpiScale;
            }
        }

        private static void SetMouseEvent(ImGuiIOPtr io, MouseButton rayMouse, ImGuiMouseButton imGuiMouse)
        {
            if (Raylib.IsMouseButtonPressed(rayMouse))
                io.AddMouseButtonEvent(imGuiMouse, true);
            else if (Raylib.IsMouseButtonReleased(rayMouse))
                io.AddMouseButtonEvent(imGuiMouse, false);
        }

        private static void NewFrame(float dt = -1)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            if (Raylib.IsWindowFullscreen())
            {
                int monitor = Raylib.GetCurrentMonitor();
                io.DisplaySize = new ImVec2(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
            }
            else
            {
                io.DisplaySize = new ImVec2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            }

            io.DisplayFramebufferScale = new ImVec2(1, 1);

            if (windowIsHighDPI)
                    io.DisplayFramebufferScale = Raylib.GetWindowScaleDPI();

            io.DeltaTime = dt >= 0 ? dt : Raylib.GetFrameTime();

            if (io.WantSetMousePos)
            {
                Raylib.SetMousePosition((int)io.MousePos.X, (int)io.MousePos.Y);
            }
            else
            {
                io.AddMousePosEvent(Raylib.GetMouseX(), Raylib.GetMouseY());
            }

            SetMouseEvent(io, MouseButton.Left, ImGuiMouseButton.Left);
            SetMouseEvent(io, MouseButton.Right, ImGuiMouseButton.Right);
            SetMouseEvent(io, MouseButton.Middle, ImGuiMouseButton.Middle);
            SetMouseEvent(io, MouseButton.Forward, ImGuiMouseButton.Middle + 1);
            SetMouseEvent(io, MouseButton.Back, ImGuiMouseButton.Middle + 2);

            var wheelMove = Raylib.GetMouseWheelMoveV();
            io.AddMouseWheelEvent(wheelMove.X, wheelMove.Y);

            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
            {
                ImGuiMouseCursor imgui_cursor = ImGui.GetMouseCursor();
                if (imgui_cursor != CurrentMouseCursor || io.MouseDrawCursor)
                {
                    CurrentMouseCursor = imgui_cursor;
                    if (io.MouseDrawCursor || imgui_cursor == ImGuiMouseCursor.None)
                    {
                        Raylib.HideCursor();
                    }
                    else
                    {
                        Raylib.ShowCursor();

                        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
                        {

                            if (!MouseCursorMap.ContainsKey(imgui_cursor))
                                Raylib.SetMouseCursor(MouseCursor.Default);
                            else
                                Raylib.SetMouseCursor(MouseCursorMap[imgui_cursor]);
                        }
                    }
                }
            }
        }

        private static void FrameEvents()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            bool focused = Raylib.IsWindowFocused();
            if (focused != LastFrameFocused)
                io.AddFocusEvent(focused);
            LastFrameFocused = focused;

            // handle the modifyer key events so that shortcuts work
            bool ctrlDown = rlImGuiIsControlDown();
            if (ctrlDown != LastControlPressed)
                io.AddKeyEvent(ImGuiKey.ModCtrl, ctrlDown);
            LastControlPressed = ctrlDown;

            bool shiftDown = rlImGuiIsShiftDown();
            if (shiftDown != LastShiftPressed)
                io.AddKeyEvent(ImGuiKey.ModShift, shiftDown);
            LastShiftPressed = shiftDown;

            bool altDown = rlImGuiIsAltDown();
            if (altDown != LastAltPressed)
                io.AddKeyEvent(ImGuiKey.ModAlt, altDown);
            LastAltPressed = altDown;

            bool superDown = rlImGuiIsSuperDown();
            if (superDown != LastSuperPressed)
                io.AddKeyEvent(ImGuiKey.ModSuper, superDown);
            LastSuperPressed = superDown;

            // get the pressed keys, they are in event order
            var keyId = Raylib.GetKeyPressed();
            while (keyId != 0)
            {
                if (RaylibKeyMap.ContainsKey(keyId))
                    io.AddKeyEvent(RaylibKeyMap[keyId], true);
                keyId = Raylib.GetKeyPressed();
            }

            // look for any keys that were down last frame and see if they were down and are released
            foreach (var keyItr in RaylibKeyMap)
	        {
                if (Raylib.IsKeyReleased(keyItr.Key))
                    io.AddKeyEvent(keyItr.Value, false);
            }

            // add the text input in order
            var pressed = Raylib.GetCharPressed();
            while (pressed != 0)
            {
                io.AddInputCharacter((uint)pressed);
                pressed = Raylib.GetCharPressed();
            }

            // gamepads
            if ((io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) != 0 && Raylib.IsGamepadAvailable(0))
            {
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceUp, ImGuiKey.GamepadDpadUp);
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceRight, ImGuiKey.GamepadDpadRight);
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceDown, ImGuiKey.GamepadDpadDown);
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceLeft, ImGuiKey.GamepadDpadLeft);

                HandleGamepadButtonEvent(io, GamepadButton.RightFaceUp, ImGuiKey.GamepadFaceUp);
                HandleGamepadButtonEvent(io, GamepadButton.RightFaceRight, ImGuiKey.GamepadFaceLeft);
                HandleGamepadButtonEvent(io, GamepadButton.RightFaceDown, ImGuiKey.GamepadFaceDown);
                HandleGamepadButtonEvent(io, GamepadButton.RightFaceLeft, ImGuiKey.GamepadFaceRight);

                HandleGamepadButtonEvent(io, GamepadButton.LeftTrigger1, ImGuiKey.GamepadL1);
                HandleGamepadButtonEvent(io, GamepadButton.LeftTrigger2, ImGuiKey.GamepadL2);
                HandleGamepadButtonEvent(io, GamepadButton.RightTrigger1, ImGuiKey.GamepadR1);
                HandleGamepadButtonEvent(io, GamepadButton.RightTrigger2, ImGuiKey.GamepadR2);
                HandleGamepadButtonEvent(io, GamepadButton.LeftThumb, ImGuiKey.GamepadL3);
                HandleGamepadButtonEvent(io, GamepadButton.RightThumb, ImGuiKey.GamepadR3);

                HandleGamepadButtonEvent(io, GamepadButton.MiddleLeft, ImGuiKey.GamepadStart);
                HandleGamepadButtonEvent(io, GamepadButton.MiddleRight, ImGuiKey.GamepadBack);

                // left stick
                HandleGamepadStickEvent(io, GamepadAxis.LeftX, ImGuiKey.GamepadLStickLeft, ImGuiKey.GamepadLStickRight);
                HandleGamepadStickEvent(io, GamepadAxis.LeftY, ImGuiKey.GamepadLStickUp, ImGuiKey.GamepadLStickDown);

                // right stick
                HandleGamepadStickEvent(io, GamepadAxis.RightX, ImGuiKey.GamepadRStickLeft, ImGuiKey.GamepadRStickRight);
                HandleGamepadStickEvent(io, GamepadAxis.RightY, ImGuiKey.GamepadRStickUp, ImGuiKey.GamepadRStickDown);
            }
        }

        private static void HandleGamepadButtonEvent(ImGuiIOPtr io, GamepadButton button, ImGuiKey key)
        {
            if (Raylib.IsGamepadButtonPressed(0, button))
                io.AddKeyEvent(key, true);
            else if (Raylib.IsGamepadButtonReleased(0, button))
                io.AddKeyEvent(key, false);
        }

        private static void HandleGamepadStickEvent(ImGuiIOPtr io, GamepadAxis axis, ImGuiKey negKey, ImGuiKey posKey)
        {
            const float deadZone = 0.20f;

            float axisValue = Raylib.GetGamepadAxisMovement(0, axis);

            io.AddKeyAnalogEvent(negKey, axisValue < -deadZone, axisValue < -deadZone ? -axisValue : 0);
            io.AddKeyAnalogEvent(posKey, axisValue > deadZone, axisValue > deadZone ? axisValue : 0);
        }

        internal static void Begin(float dt = -1)
        {
            ImGui.SetCurrentContext(ImGuiContext);

            NewFrame(dt);
            FrameEvents();
            ImGui.NewFrame();
        }

        private static void EnableScissor(float x, float y, float width, float height)
        {
#if false
            ImGuiIOPtr io = ImGui.GetIO();
            Rlgl.EnableScissorTest();

            Rlgl.Scissor(   (int)(x),
                            (int)((io.DisplaySize.Y - (int)(y + height))),
                            (int)(width),
                            (int)(height));
#endif

            Rlgl.EnableScissorTest();
            ImGuiIOPtr io = ImGui.GetIO();

            Vector2 scale = new Vector2(1.0f, 1.0f);
            if (windowIsHighDPI)
                scale = io.DisplayFramebufferScale;

            Rlgl.Scissor(   (int)(x * scale.X),
                            (int)((io.DisplaySize.Y - (int)(y + height)) * scale.Y),
                            (int)(width * scale.X),
                            (int)(height * scale.Y));
        }


        private static void TriangleVert(ImDrawVert idx_vert)
        {
            var color = ImGui.ColorConvert(idx_vert.col);

            Rlgl.Color4f(color.X, color.Y, color.Z, color.W);
            Rlgl.TexCoord2f(idx_vert.uv.X, idx_vert.uv.Y);
            Rlgl.Vertex2f(idx_vert.pos.X, idx_vert.pos.Y);
        }

        private static void RenderTriangles(uint count, uint indexStart, ref ImVector<ImDrawIdx> indexBuffer,ref ImVector<ImDrawVert> vertBuffer, ref ImTextureRef textureRef)
        {
            if (count < 3)
                return;

            //? Shouldn't this be rendering using triangleindexlist

            uint textureId = (uint)textureRef.GetID().underlying;

            Rlgl.Begin(RlglEnum.Triangles);
            Rlgl.SetTexture(textureId);

            for (int i = 0; i <= (count - 3); i += 3)
            {
                if (Rlgl.CheckRenderBatchLimit(3))
                {
                    Rlgl.Begin(RlglEnum.Triangles);
                    Rlgl.SetTexture(textureId);
                }

                ushort indexA = indexBuffer[(int)indexStart + i].underlying;
                ushort indexB = indexBuffer[(int)indexStart + i + 1].underlying;
                ushort indexC = indexBuffer[(int)indexStart + i + 2].underlying;

                ImDrawVert vertexA = vertBuffer[indexA];
                ImDrawVert vertexB = vertBuffer[indexB];
                ImDrawVert vertexC = vertBuffer[indexC];

                TriangleVert(vertexA);
                TriangleVert(vertexB);
                TriangleVert(vertexC);
            }
            Rlgl.End();
        }

        private static unsafe void UpdateTextureData(ImTextureDataPtr tex)
        {
            if (tex.Status == ImTextureStatus.WantCreate)
            {
                // Create texture based on tex->Width, tex->Height.
                // - Most backends only support tex->Format == ImTextureFormat_RGBA32.
                // - Backends for particularly memory constrainted platforms may support tex->Format == ImTextureFormat_Alpha8.

                // Upload all texture pixels
                // - Read from our CPU-side copy of the texture and copy to your graphics API.
                // - Use tex->Width, tex->Height, tex->GetPixels(), tex->GetPixelsAt(), tex->GetPitch() as needed.

                // Store your data, and acknowledge creation.

                if (tex.Format != ImTextureFormat.RGBA32)
                    throw new NotSupportedException("Only RGBA32 texture format is supported in this backend");

                if (tex.GetPitch() != tex.Width * 4)
                    throw new NotSupportedException("Only tightly packed textures are supported in this backend");

                Image image = new Image
                {
                    Data = tex.GetPixels(),
                    Width = tex.Width,
                    Height = tex.Height,
                    Mipmaps = 1,
                    Format = PixelFormat.UncompressedR8G8B8A8,
                };

                var texture2D = Raylib.LoadTextureFromImage(image);
                imGuiTextureMap.Add(texture2D.Id, texture2D);

                tex.SetTexID(new ImTextureID(texture2D.Id));
                tex.SetStatus(ImTextureStatus.OK);
            }
            if (tex.Status == ImTextureStatus.WantUpdates)
            {
                // Upload a rectangle of pixels to the existing texture
                // - We only ever write to textures regions which have never been used before!
                // - Use tex->TexID or tex->BackendUserData to retrieve your stored data.
                // - Use tex->UpdateRect.x/y, tex->UpdateRect.w/h to obtain the block position and size.
                //   - Use tex->Updates[] to obtain individual sub-regions within tex->UpdateRect. Not recommended.
                // - Read from our CPU-side copy of the texture and copy to your graphics API.
                // - Use tex->Width, tex->Height, tex->GetPixels(), tex->GetPixelsAt(), tex->GetPitch() as needed.
                if (tex.Format != ImTextureFormat.RGBA32)
                    throw new NotSupportedException("Only RGBA32 texture format is supported in this backend");

                if (tex.GetPitch() != tex.Width * 4)
                    throw new NotSupportedException("Only tightly packed textures are supported in this backend");

                var updateTexture = imGuiTextureMap[(uint)tex.TexID.underlying];
                Image image = new Image // Just update the entire texture for simplicity
                {
                    Data = tex.GetPixels(),
                    Width = tex.Width,
                    Height = tex.Height,
                    Mipmaps = 1,
                    Format = PixelFormat.UncompressedR8G8B8A8,
                };
                Raylib.UpdateTexture(updateTexture, image.Data);
                // Acknowledge update
                tex.SetStatus(ImTextureStatus.OK);
            }
            if (tex.Status == ImTextureStatus.WantDestroy && tex.UnusedFrames > 0)
            {
                // If you use staged rendering and have in-flight renders, changed tex->UnusedFrames > 0 check to higher count as needed e.g. > 2

                // Destroy texture
                // - Use tex->TexID or tex->BackendUserData to retrieve your stored data.
                // - Destroy texture in your graphics API.

                // Acknowledge destruction
                tex.SetTexID(ImGui.ImTextureID_Invalid);
                tex.SetStatus(ImTextureStatus.Destroyed);
            }
        }
        private static void RenderData()
        {
            Rlgl.DrawRenderBatchActive();
            Rlgl.DisableBackfaceCulling();

            // We also need to process textures here now i think?

            var data = ImGui.GetDrawData();
            if (data.HasTextures)
            {
                var textures = data.Textures;
                for (int t = 0; t < textures.Size; t++)
                {
                    var texture = textures[t];
                    if (texture.Status != ImTextureStatus.OK)
                    {
                        UpdateTextureData(texture);
                    }
                }
            }

            for (int l = 0; l < data.CmdListsCount; l++)
            {
                ImDrawListPtr commandList = data.CmdLists[l];
                for (int cmdIndex = 0; cmdIndex < commandList.CmdBuffer.Size; cmdIndex++)
                {
                    var cmd = commandList.CmdBuffer[cmdIndex];

                    EnableScissor(cmd.ClipRect.X - data.DisplayPos.X, cmd.ClipRect.Y - data.DisplayPos.Y, cmd.ClipRect.Z - (cmd.ClipRect.X - data.DisplayPos.X), cmd.ClipRect.W - (cmd.ClipRect.Y - data.DisplayPos.Y));
                    if (cmd.HasUserCallback())
                    {
                        cmd.CallUserCallback(ref cmd);
                        continue;
                    }

                    RenderTriangles(cmd.ElemCount, cmd.IdxOffset, ref commandList.IdxBuffer, ref commandList.VtxBuffer, ref cmd.TexRef);

                    Rlgl.DrawRenderBatchActive();
                }
            }
            Rlgl.SetTexture(0);
            Rlgl.DisableScissorTest();
            Rlgl.EnableBackfaceCulling();
        }

        internal static void End()
        {
            ImGui.SetCurrentContext(ImGuiContext);
            ImGui.Render();
            RenderData();
        }

        internal static void Shutdown()
        {
            ImGui.DestroyContext();

#if TODO_FAWESOME
            // remove this if you don't want font awesome support
            {
                if (IconFonts.FontAwesome6.IconFontRanges != IntPtr.Zero)
                    Marshal.FreeHGlobal(IconFonts.FontAwesome6.IconFontRanges);

                IconFonts.FontAwesome6.IconFontRanges = IntPtr.Zero;
            }
#endif
        }

        internal static void Image(Texture2D image)
        {
            ImGui.Image(new ImTextureRef(new ImTextureID(image.Id)), new ImVec2(image.Width, image.Height));
        }

        internal static void ImageSize(Texture2D image, int width, int height)
        {
            ImGui.Image(new ImTextureRef(new ImTextureID(image.Id)), new ImVec2(width, height));
        }

        internal static void ImageSize(Texture2D image, ImVec2 size)
        {
            ImGui.Image(new ImTextureRef(new ImTextureID(image.Id)), size);
        }

        internal static void ImageRect(Texture2D image, int destWidth, int destHeight, Rectangle sourceRect)
        {
            Vector2 uv0 = new Vector2();
            Vector2 uv1 = new Vector2();

            if (sourceRect.Width < 0)
            {
                uv0.X = -((float)sourceRect.X / image.Width);
                uv1.X = (uv0.X - (float)(Math.Abs(sourceRect.Width) / image.Width));
            }
            else
            {
                uv0.X = (float)sourceRect.X / image.Width;
                uv1.X = uv0.X + (float)(sourceRect.Width / image.Width);
            }

            if (sourceRect.Height < 0)
            {
                uv0.Y = -((float)sourceRect.Y / image.Height);
                uv1.Y = (uv0.Y - (float)(Math.Abs(sourceRect.Height) / image.Height));
            }
            else
            {
                uv0.Y = (float)sourceRect.Y / image.Height;
                uv1.Y = uv0.Y + (float)(sourceRect.Height / image.Height);
            }

            ImGui.Image(new ImTextureRef(new ImTextureID(image.Id)), new Vector2(destWidth, destHeight), uv0, uv1);
        }

        internal static void ImageRenderTexture(RenderTexture2D image)
        {
            ImageRect(image.Texture, image.Texture.Width, image.Texture.Height, new Rectangle(0, 0, image.Texture.Width, -image.Texture.Height));
        }

        internal static void ImageRenderTextureFit(RenderTexture2D image, bool center = true)
        {
#if TODO
            Vector2 area = ImGui.GetContentRegionAvail();

            float scale = area.X / image.Texture.Width;

            float y = image.Texture.Height * scale;
            if (y > area.Y)
            {
                scale = area.Y / image.Texture.Height;
            }

            int sizeX = (int)(image.Texture.Width * scale);
            int sizeY = (int)(image.Texture.Height * scale);

            if (center)
            {
                ImGui.SetCursorPosX(0);
                ImGui.SetCursorPosX(area.X / 2 - sizeX / 2);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (area.Y / 2 - sizeY / 2));
            }

            ImageRect(image.Texture, sizeX, sizeY, new Rectangle(0,0, (image.Texture.Width), -(image.Texture.Height) ));
#endif
        }

        internal static bool ImageButton(System.String name, Texture2D image)
        {
            return ImageButtonSize(name, image, new Vector2(image.Width, image.Height));
        }

        internal static bool ImageButtonSize(System.String name, Texture2D image, Vector2 size)
        {
#if TODO
            return ImGui.ImageButton(name, new IntPtr(image.Id), size);
#else
            return false;
#endif
        }

    }
}