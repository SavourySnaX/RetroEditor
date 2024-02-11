using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

public class LibRetroDebuggerWindow : IWindow
{
    Texture2D bitmap;

    LibRetroPlugin plugin;
    LibRetroPlugin.RetroSystemAVInfo aVInfo;
    float scale = 1.0f;

    uint frameWidth, frameHeight;
    IPlayerWindowExtension extension;
    IPlayerControls controls;
    Thread mameThread;
    DView state;
    DView disasm;
    bool notDone;

    public LibRetroDebuggerWindow(LibRetroPlugin plugin, IPlayerControls controls, IPlayerWindowExtension extension)
    {
        this.plugin = plugin;
        this.extension = extension;
        this.controls = controls;
        this.state = new DView(1, 0, 0, 20, 25);
        this.disasm = new DView(0, 0, 0, 100, 25);
        this.notDone = true;
        plugin.SetDebuggerCallback(DebuggerCallback);
        inputBuffer = "";
        log = "";
    }

    public bool Initialise()
    {
        if (plugin.Version() != 1)
        {
            return false;
        }
        plugin.Init();

        // Spwan a thread to handle mame, since in mame the debugger blocks retro_run when stopped on a breakpoint etc
        // This is a workaround to keep the UI responsive
        mameThread = new Thread(() =>
        {
            while (notDone)
            {
                Thread.Sleep((int)Math.Floor(UpdateInterval*1000));
                plugin.Run();
            }
        });
        mameThread.Name = "MameRunnerThread";

        return true;
    }

    public bool OtherStuff()
    {
        // We should save snapshot, so we don't need to load from tape again...
        var saveSize = plugin.GetSaveStateSize();
        var state = new byte[saveSize];
        plugin.SaveState(state);

        return true;
    }

    public void InitWindow()
    {
        aVInfo = plugin.GetSystemAVInfo();
        frameHeight= aVInfo.geometry.maxHeight;
        frameWidth = aVInfo.geometry.maxWidth;
        var image = new Image
        {
            Width = (int)aVInfo.geometry.maxWidth,
            Height = (int)aVInfo.geometry.maxHeight,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };

        bitmap = Raylib.LoadTextureFromImage(image);
        mameThread.Start();
    }

    public void Update(float seconds)
    {
        UpdateDebuggerView(state);
        UpdateDebuggerView(disasm);
        extension?.Update(seconds);
        Raylib.UpdateTexture(bitmap, plugin.GetFrameBuffer(out frameWidth, out frameHeight));
    }

    public float UpdateInterval => (float)(1.0 / aVInfo.timing.fps);

    private bool audioEnabled = false;
    string inputBuffer;
    string log;

    public bool Draw()
    {
        if (ImGui.Checkbox("Audio", ref audioEnabled))
        {
            plugin.SwitchAudio(audioEnabled);
        }

        rlImGui.ImageRect(bitmap, (int)(aVInfo.geometry.baseWidth * scale), (int)(aVInfo.geometry.baseHeight * scale), new Rectangle(0,0,frameWidth,frameHeight));
        ImGui.SameLine();
        DrawDView(state,"State");
        ImGui.SameLine();
        DrawDView(disasm,"Disasm");

        if (ImGui.InputText("Command",ref inputBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            inputBuffer.Trim();
            if (remoteCommandCallback.remoteCommandCB != null)
            {
                var inputBufferMarshalled=Marshal.StringToHGlobalAnsi(inputBuffer);
                string output = "";
                unsafe
                {
                    var outputB = remoteCommandCallback.remoteCommandCB(remoteCommandCallback.data, (byte*)inputBufferMarshalled);
                    output = Marshal.PtrToStringAnsi((nint)outputB) ?? "";
                }
                output=output.Trim();
                if (output.Length > 0)
                {
                    log += output + "\n";
                }
            }
        }

        ImGui.BeginChild("Log", new Vector2(0, 0), 0, ImGuiWindowFlags.HorizontalScrollbar|ImGuiWindowFlags.AlwaysVerticalScrollbar);
        ImGui.Text(log);
        ImGui.EndChild();


        if (ImGui.IsWindowFocused())
        {
            // JSW keys
            plugin.UpdateKey(KeyboardKey.KEY_SPACE, ImGui.IsKeyDown(ImGuiKey.Space));
            plugin.UpdateKey(KeyboardKey.KEY_O, ImGui.IsKeyDown(ImGuiKey.O));
            plugin.UpdateKey(KeyboardKey.KEY_P, ImGui.IsKeyDown(ImGuiKey.P));

            // Rollercoaster keys
            plugin.UpdateKey(KeyboardKey.KEY_ENTER, ImGui.IsKeyDown(ImGuiKey.Enter));
            plugin.UpdateKey(KeyboardKey.KEY_M, ImGui.IsKeyDown(ImGuiKey.M));
            plugin.UpdateKey(KeyboardKey.KEY_LEFT_SHIFT, ImGui.IsKeyDown(ImGuiKey.LeftShift));
            plugin.UpdateKey(KeyboardKey.KEY_RIGHT_SHIFT, ImGui.IsKeyDown(ImGuiKey.RightShift));

            // JOYPAD emulation 
            plugin.UpdateKey(KeyboardKey.KEY_UP, ImGui.IsKeyDown(ImGuiKey.UpArrow));
            plugin.UpdateKey(KeyboardKey.KEY_DOWN, ImGui.IsKeyDown(ImGuiKey.DownArrow));
            plugin.UpdateKey(KeyboardKey.KEY_LEFT, ImGui.IsKeyDown(ImGuiKey.LeftArrow));
            plugin.UpdateKey(KeyboardKey.KEY_RIGHT, ImGui.IsKeyDown(ImGuiKey.RightArrow));
            plugin.UpdateKey(KeyboardKey.KEY_Z, ImGui.IsKeyDown(ImGuiKey.Z));
            plugin.UpdateKey(KeyboardKey.KEY_X, ImGui.IsKeyDown(ImGuiKey.X));
            plugin.UpdateKey(KeyboardKey.KEY_A, ImGui.IsKeyDown(ImGuiKey.A));
            plugin.UpdateKey(KeyboardKey.KEY_S, ImGui.IsKeyDown(ImGuiKey.S));
            plugin.UpdateKey(KeyboardKey.KEY_M, ImGui.IsKeyDown(ImGuiKey.M));
            plugin.UpdateKey(KeyboardKey.KEY_N, ImGui.IsKeyDown(ImGuiKey.N));
        }

        extension?.Render(controls);
        return false;
    }

    public void Close()
    {
        // probably need to send something to unblock the mame thread though
        notDone = false;
    }

    private void FetchColourForStyle(byte attr,out Vector4 fg,out Vector4 bg)
    {
        bg = new Vector4(1.0f, 1.0f, 1.0f, .9f);
        fg = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        if ((attr & 0x01)==0x01)
        {
            fg = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        }
        if ((attr & 0x02)==0x02)
        {
            bg = new Vector4(1.0f, 0.5f, .5f, 0.8f);
        }
        if ((attr & 0x04)==0x04)
        {
            fg = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        }
        if ((attr & 0x08)==0x08)
        {
            fg = new Vector4(fg.X * 0.5f, fg.Y * 0.5f, fg.Z * 0.5f, 1.0f);
        }
        if ((attr & 0x10)==0x10)
        {
            bg = new Vector4(0.7f, 0.7f, 0.7f, .9f);
        }
        if ((attr & 0x20)==0x20)
        {
            bg = new Vector4(1.0f, 1.0f, 0.0f, .8f);
        }
        if ((attr & 0x40)==0x40)
        {
            fg = new Vector4(0.0f, .5f, 0.0f, 1.0f);
        }
        if ((attr & 0x80)==0x80)
        {
            bg = new Vector4(0.0f, 1.0f, 1.0f, 0.8f);
        }
    }

    private void DrawDView(DView view, string title)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0,0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
    
        var sizeOfMonoText=ImGui.CalcTextSize("A");
        ImGui.BeginChild($"State - {title}", new Vector2(sizeOfMonoText.X*(view.w+2),sizeOfMonoText.Y*(view.h+2)), 0, 0);

        var drawList = ImGui.GetWindowDrawList();
        var convCode = new byte[] { 0, 0 };
        Vector2 pos = ImGui.GetCursorScreenPos();
        for (int yy=0;yy<view.h;yy++)
        {
            for (int xx=0;xx<view.w;xx++)
            {
                var attr=view.state[(yy*view.w+xx)*2+1];
                FetchColourForStyle(attr,out var fg,out var bg);
                convCode[0]=view.state[(yy*view.w+xx)*2];
                drawList.AddRectFilled(pos, new Vector2(pos.X + sizeOfMonoText.X, pos.Y + sizeOfMonoText.Y), ImGui.GetColorU32(bg));
                drawList.AddText(pos, ImGui.GetColorU32(fg), System.Text.Encoding.ASCII.GetString(convCode));
                pos.X += sizeOfMonoText.X;
            }
            pos.X = ImGui.GetCursorScreenPos().X;
            pos.Y += sizeOfMonoText.Y;
        }
        ImGui.EndChild();
        ImGui.PopStyleVar(2);
    }

    public class DView
    {
        public DView(int viewNum, int x, int y, int w, int h)
        {
            this.viewNum = viewNum;
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.state = new byte[w * h * 2];
            for (int i = 0; i < w * h * 2; i += 2)
            {
                state[i + 1] = 0x00;
                state[i + 0] = 0x20;
            }
        }

        public int viewNum;
        public int x, y, w, h;
        public byte[] state;
    }

    private LibRetroPlugin.DebuggerView debuggerViewCallback;
    private LibRetroPlugin.RemoteCommand remoteCommandCallback;

    private void UpdateDebuggerView(DView view)
    {
        var viewSize = view.w * view.h * 2;
        if (view.state.Length != viewSize)
        {
            view.state = new byte[viewSize];
        }
        if (debuggerViewCallback.viewCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallback.data;
            var view_char = debuggerViewCallback.viewCb(_this, view.x, view.y, view.w, view.h, view.viewNum);
            Marshal.Copy((nint)view_char,view.state,0,(int)viewSize);
        }
    }

    private int DebuggerCallback(int kind,IntPtr data)
    {
        switch (kind)
        {
            case 0:
                debuggerViewCallback = Marshal.PtrToStructure<LibRetroPlugin.DebuggerView>(data);
                return 1;
            case 1:
                remoteCommandCallback = Marshal.PtrToStructure<LibRetroPlugin.RemoteCommand>(data);
                return 1;
            default:
                return 0;
        }
    }


}