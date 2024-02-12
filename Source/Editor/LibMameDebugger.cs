
using System.Runtime.InteropServices;
using ImGuiNET;

internal class LibMameDebugger
{
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


    private LibRetroPlugin plugin;

    private LibRetroPlugin.DebuggerView debuggerViewCallback;
    private LibRetroPlugin.RemoteCommand remoteCommandCallback;



    public LibMameDebugger(LibRetroPlugin plugin)
    {
        this.plugin = plugin;
        plugin.SetDebuggerCallback(DebuggerCallback);
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
            editor.OpenWindow(new DebuggerView(this, 1, 20, 25), "CPU State");
        }
        if (ImGui.MenuItem("Disassembly"))
        {
            editor.OpenWindow(new DebuggerView(this, 0, 100, 25), "Disassembly");
        }
        if (ImGui.MenuItem("Console"))
        {
            editor.OpenWindow(new DebuggerCommand(this), "Console");
        }
    }

    public void UpdateDView(ref DView view)
    {
        int viewSize = view.w * view.h * 2;
        if (debuggerViewCallback.viewCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallback.data;
            var view_char = debuggerViewCallback.viewCb(_this, view.x, view.y, view.w, view.h, view.viewNum);
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