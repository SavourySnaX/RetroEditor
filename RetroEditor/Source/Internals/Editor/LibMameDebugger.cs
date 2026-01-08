using System.Diagnostics;
using System.Runtime.InteropServices;
using RetroEditor.Plugins;
using MyMGui;

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

    public bool IsStopped { get; private set; }
    public LibMameDebugger(LibRetroPlugin plugin)
    {
        this.stateViewCounter=0;
        this.disasmCounter=0;
        this.memoryCounter=0;
        this.plugin = plugin;
        this.plugin.SetDebuggerCallback(DebuggerCallback);
        threadPump = ExecuteOperationsThreaded;
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

    public void Menus(IEditorInternal editor)
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
        if (ImGui.MenuItem("Resourcer"))
        {
            editor.OpenWindow(new Resourcer(this), "Resourcer");
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

    public void SetDataFormat(ref DView view, LibRetroPlugin.debug_format format)
    {
        if (debuggerViewCallbacks.dataFormatCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            debuggerViewCallbacks.dataFormatCb(_this, view.view.view, (int)format);
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

    public int GetSourcesCount(ref DView view)
    {
        if (debuggerViewCallbacks.dataSourcesCountCb == null)
        {
            return 0;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            return debuggerViewCallbacks.dataSourcesCountCb(_this, view.view.view);
        }
    }

    public string GetSourceName(ref DView view, int source)
    {
        if (debuggerViewCallbacks.dataSourcesNameCb == null)
        {
            return "";
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            var sourceName = debuggerViewCallbacks.dataSourcesNameCb(_this, view.view.view, source);
            return Marshal.PtrToStringAnsi((nint)sourceName) ?? "";
        }
    }

    public void SetSource(ref DView view, int source)
    {
        if (debuggerViewCallbacks.dataSourcesSetCb == null)
        {
            return;
        }
        unsafe
        {
            var _this = (void*)debuggerViewCallbacks.data;
            debuggerViewCallbacks.dataSourcesSetCb(_this, view.view.view, source);
        }
    }

    private struct ThreadSafeCommand
    {
        public string command;
        public UInt64 id;
        public Action<string,int> cb;
    }

    Queue<ThreadSafeCommand> commandQueue = new();
    private UInt64 commandId = 1;
    public int QueueCommand(string command, Action<string,int> callback)
    {
        commandQueue.Enqueue(new ThreadSafeCommand {
            command = command,
            id = commandId++,
            cb = callback
        });
        return commandQueue.Count;
    }

    public void ExecuteOperationsThreaded()
    {
        if (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            var result = SendCommand(command.command);
            command.cb(result, (int)command.id);
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

    public bool DebuggerViewReady {get; private set;} = false;
    public bool RemoteCommandReady {get; private set;} = false;

    delegate void ThreadPump();
    private ThreadPump threadPump;
    private nint threadPumpPtr;

    private nint DebuggerCallback(int kind,IntPtr data)
    {
        switch (kind)
        {
            case 0:
                debuggerViewCallbacks = Marshal.PtrToStructure<LibRetroPlugin.DebuggerView>(data);
                DebuggerViewReady = true;
                return 1;
            case 1:
                remoteCommandCallback = Marshal.PtrToStructure<LibRetroPlugin.RemoteCommand>(data);
                RemoteCommandReady = true;
                return 1;
            case 2:
                var notified = Marshal.PtrToStructure<LibRetroPlugin.RemoteNotification>(data);
                if (notified.stopped!=0)
                {
                    IsStopped = true;
                }
                else
                {
                    IsStopped = false;
                }
                return 1;
            case 3:
                // Thread Pump
                threadPumpPtr = Marshal.GetFunctionPointerForDelegate(threadPump);
                return threadPumpPtr;
            default:
                return 0;
        }
    }

    internal string[] GetSourcesList(ref DView view)
    {
        var count = GetSourcesCount(ref view);
        var sources = new string[count];
        for (int i = 0; i < count; i++)
        {
            sources[i] = GetSourceName(ref view, i);
        }
        return sources;
    }
}