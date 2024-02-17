
using System.Runtime.InteropServices;
using ImGuiNET;

internal class LibMameDebugger
{
    public class DView
    {
        public DView(LibRetroPlugin.RetroDebugView view, int x, int y, int w, int h, string expression)
        {
            this.view = view;
            this.view.X=x;
            this.view.Y=y;
            this.view.W=w;
            this.view.H=h;
            this.view.Expression=expression;
            this.state = new byte[w * h * 2];
            for (int i = 0; i < w * h * 2; i += 2)
            {
                state[i + 1] = 0x00;
                state[i + 0] = 0x20;
            }
        }

        public LibRetroPlugin.RetroDebugView view;

        public byte[] state;
    }


    private LibRetroPlugin plugin;

    private LibRetroPlugin.DebuggerView debuggerViewCallbacks;
    private LibRetroPlugin.RemoteCommand remoteCommandCallback;


    private int stateViewCounter;
    private int disasmCounter;
    private int memoryCounter;

    public LibMameDebugger(LibRetroPlugin plugin)
    {
        this.stateViewCounter=0;
        this.disasmCounter=0;
        this.memoryCounter=0;
        this.plugin = plugin;
        this.plugin.SetDebuggerCallback(DebuggerCallback);
    }
/*
    public void OpenDebugger()
    {
        var pluginWindow = new LibRetroDebuggerWindow(retro, null, null);
        pluginWindow.Initialise();
        retro.LoadGame(result.Path);
        pluginWindow.OtherStuff();
        pluginWindow.InitWindow();
        windowManager.AddWindow(pluginWindow, "MAME RETRO");

    }*/

    public void Menus(IEditor editor)
    {
        if (ImGui.MenuItem("CPU State"))
        {
            editor.OpenWindow(new DebuggerView(this, LibRetroPlugin.debug_view_type.State, 20, 25, ""), $"CPU State {stateViewCounter++}");
        }
        if (ImGui.MenuItem("Disassembly"))
        {
            editor.OpenWindow(new DebuggerView(this, LibRetroPlugin.debug_view_type.Disassembly, 100, 25, "curpc"), $"Disassembly {disasmCounter++}");
        }
        if (ImGui.MenuItem("Memory"))
        {
            editor.OpenWindow(new DebuggerView(this, LibRetroPlugin.debug_view_type.Memory, 80, 25, "0"), $"Memory {memoryCounter++}");
        }
        if (ImGui.MenuItem("Console"))
        {
            editor.OpenWindow(new DebuggerCommand(this), "Console");
        }
    }

    public LibRetroPlugin.RetroDebugView AllocView(LibRetroPlugin.debug_view_type kind)
    {
        if (debuggerViewCallbacks.allocCb == null)
        {
            return new LibRetroPlugin.RetroDebugView();
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            var view = debuggerViewCallbacks.allocCb(_this, kind);
            return new LibRetroPlugin.RetroDebugView(view);
        }
    }

    public void FreeView(LibRetroPlugin.RetroDebugView view)
    {
        if (debuggerViewCallbacks.freeCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            debuggerViewCallbacks.freeCb(_this, view.view);
        }
    }

    public void SetExpression(ref DView view)
    {
        if (debuggerViewCallbacks.updateExpressionCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            debuggerViewCallbacks.updateExpressionCb(_this, view.view.view);
        }
    }

    public void ProcessKey(ref DView view, LibRetroPlugin.debug_key key)
    {
        if (debuggerViewCallbacks.processCharCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            debuggerViewCallbacks.processCharCb(_this, view.view.view, (int)key);
        }
    }

    public void UpdateDView(ref DView view)
    {
        int viewSize = view.view.W * view.view.H * 2;
        if (debuggerViewCallbacks.viewCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            var view_char = debuggerViewCallbacks.viewCb(_this, view.view.view);
            Marshal.Copy((nint)view_char,view.state,0,viewSize);
        }
    }

    public string SendCommand(string command)
    {
        if (remoteCommandCallback.remoteCommandCB != null)
        {
            var inputBufferMarshalled = Marshal.StringToHGlobalAnsi(command);
            string output = "";
            unsafe
            {
                var outputB = remoteCommandCallback.remoteCommandCB(remoteCommandCallback.data, (byte*)inputBufferMarshalled);
                output = Marshal.PtrToStringAnsi((nint)outputB) ?? "";
            }
            return output.Trim();
        }
        return "";
    }

    private int DebuggerCallback(int kind,IntPtr data)
    {
        switch (kind)
        {
            case 0:
                debuggerViewCallbacks = Marshal.PtrToStructure<LibRetroPlugin.DebuggerView>(data);
                return 1;
            case 1:
                remoteCommandCallback = Marshal.PtrToStructure<LibRetroPlugin.RemoteCommand>(data);
                return 1;
            default:
                return 0;
        }
    }


}