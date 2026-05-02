using System.Runtime.InteropServices;
using RetroEditor.Plugins;
using RetroEditor.Source.Internals.ReverseEngineering.Platform;
using RetroEditor.Source.Internals.ReverseEngineering.Platform.Megadrive;
using RetroEditor.Source.Internals.ReverseEngineering.Platform.SNES;
using RetroEditor.Source.Internals.ReverseEngineering.Platform.ZXSpectrum;

internal class LibMameDebuggerRetroPlugin : LibRetroPlugin
{
    public LibMameDebuggerRetroPlugin(IEditorInternal editor, string path) : base(editor, path)
    {
        debuggerTrampoline = IntPtr.Zero;
    }

    public void SetDebuggerCallback(DebuggerCallbackDelegate callback)
    {
        debuggerCallback = callback;
        debuggerTrampoline = Marshal.GetFunctionPointerForDelegate(callback);
    }

    private string mediaType = "cart";
    private string systemName = "";
    private string mediaName = "";

    public string SystemName => systemName;
    public string MediaName => mediaName;

    public delegate nint DebuggerCallbackDelegate(int kind, IntPtr data);

    private DebuggerCallbackDelegate? debuggerCallback;             // Prevent collection of delegate
    private nint debuggerTrampoline;

    protected override void InternalLoad(string path, byte[] data)
    {
        mediaName = Path.GetFileName(path);
        var justPath = Path.GetDirectoryName(path);
        if (justPath == null)
        {
            justPath = path;
        }
        systemName = Path.GetFileName(justPath);

        // Unfortunately, since different cores accept different file types,
        //we will need to figure out a solution, however for now I'll just
        //assume if the folder contains spectrum, its a snapshot (.sna/.z80)
        if (path.Contains("spec", StringComparison.InvariantCultureIgnoreCase))
        {
            mediaType = "snapshot";
        }
        else
        {
            mediaType = "cart";
        }
        base.InternalLoad(path, data);
    }

    public bool HasResourcerSupport
    {
        get
        {
            return (systemName == "snes" || 
                systemName == "genesis" || 
                systemName == "megadriv" ||
                systemName == "spectrum");
        }
    }

    public IPlatformFactory GetResourcer()
    {
        if (systemName == "snes")
        {
            return new SNESPlatformFactory();
        }
        else if (systemName == "genesis" || systemName == "megadriv")
        {
            return new MegadrivePlatformFactory();
        }
        else if (systemName == "spectrum")
        {
            return new ZXSpectrumPlatformFactory();
        }
        throw new NotSupportedException("Resourcer not supported for this system");
    }

    protected override bool EnvironmentCallbackInternal(EnvironmentCommand command, bool experimental, bool frontendPrivate, IntPtr data)
    {

        switch (command)
        {
            case EnvironmentCommand.ENVIRONMENT_GET_DEBUGGER_INTERFACE:
                {
                    Marshal.WriteIntPtr(data, debuggerTrampoline);
                    return true;
                }
            default:
                break;
        }
        return base.EnvironmentCallbackInternal(command, experimental, frontendPrivate, data);
    }

    protected override string OverrideVariableValue(string key, string currentValue)
    {
        if (key == "mame_media_type")   // hack for mame and consoles, need to make configurable, or autodetect
        {
            return mediaType; ;
        }
        else if (key == "mame_softlists_enable")
        {
            // Disable softlists, as they override media type
            return "disabled";
        }
        return currentValue;
    }

    // Custom debugger extensions, NOT part of the libretro API
    public enum debug_view_type
    {
        None = 0,
        Console = 1,
        State = 2,
        Disassembly = 3,
        Memory = 4,
        Log = 5,
        BreakPoints = 6,
        RegisterPoints = 7
    } 


    public enum debug_key
    {
        DCH_UP = 1,        // up arrow
        DCH_DOWN = 2,        // down arrow
        DCH_LEFT = 3,        // left arrow
        DCH_RIGHT = 4,        // right arrow
        DCH_PUP = 5,        // page up
        DCH_PDOWN = 6,        // page down
        DCH_HOME = 7,        // home
        DCH_CTRLHOME = 8,        // ctrl+home
        DCH_END = 9,        // end
        DCH_CTRLEND = 10,       // ctrl+end
        DCH_CTRLRIGHT = 11,       // ctrl+right
        DCH_CTRLLEFT = 12       // ctrl+left
    }

    public enum debug_format
    {
        AsmRightColumnNone = 0x0000,
        AsmRightColumnRawOpcodes = 0x0001,
        AsmRightColumnEncyptedOpcodes = 0x0002,
        AsmRightColumnComments = 0x0003,
        DataFormat1ByteHex = 0x1000,
        DataFormat2ByteHex = 0x1001,
        DataFormat4ByteHex = 0x1002,
        DataFormat8ByteHex = 0x1003,
        DataFormat1ByteOctal = 0x1004,
        DataFormat2ByteOctal = 0x1005,
        DataFormat4ByteOctal = 0x1006,
        DataFormat8ByteOctal = 0x1007,
        DataFormat32BitFloat = 0x1008,
        DataFormat64BitFloat = 0x1009,
        DataFormat80BitFloat = 0x100A,
        HexAddress = 0x2000,
        DecAddress = 0x2001,
        OctAddress = 0x2002,
        LogicalAddress = 0x3000,
        PhysicalAddress = 0x3001,
    }

    public struct retro_debug_view_t
    {
        public nint data;
        public nint expression;
        public nint view;
        public debug_view_type kind;
        public int x,y;
        public int w,h;
    }

    public unsafe struct RetroDebugView
    {
        public RetroDebugView(retro_debug_view_t* view)
        {
            this.view = view;
            Expression = "";
        }
        internal retro_debug_view_t* view;
        public string Expression
        {
            set
            {
                view->expression = Marshal.StringToHGlobalAnsi(value);
            }
        }
        public debug_view_type Kind
        {
            get
            {
                return view->kind;
            }
        }
        public int X
        {
            get
            {
                return view->x;
            }
            set
            {
                view->x = value;
            }
        }
        public int Y
        {
            get
            {
                return view->y;
            }
            set
            {
                view->y = value;
            }
        }
        public int W
        {
            get
            {
                return view->w;
            }
            set
            {
                view->w = value;
            }
        }
        public int H
        {
            get
            {
                return view->h;
            }
            set
            {
                view->h = value;
            }
        }
    }


    public unsafe delegate retro_debug_view_t* AllocDebugView(void* data,debug_view_type view);
    public unsafe delegate void FreeDebugView(void* data,retro_debug_view_t* view);
    public unsafe delegate byte* UpdateDebugView(void* data, retro_debug_view_t* view);
    public unsafe delegate void ProcessChar(void* data, retro_debug_view_t* view, int c);
    public unsafe delegate void UpdateExpression(void* data, retro_debug_view_t* view);
    public unsafe delegate void DataFormat(void* data, retro_debug_view_t* view, int format);
    public unsafe delegate int DataSourcesCount(void* data, retro_debug_view_t* view);
    public unsafe delegate void* DataSourcesName(void* data, retro_debug_view_t* view, int index);
    public unsafe delegate void DataSourcesSet(void* data, retro_debug_view_t* view, int index);
    public unsafe delegate byte* RemoteCommandCB(IntPtr data, byte* command);

    public struct DebuggerView
    {
        public AllocDebugView allocCb;
        public FreeDebugView freeCb;
        public UpdateDebugView viewCb;
        public ProcessChar processCharCb;
        public UpdateExpression updateExpressionCb;
        public DataFormat dataFormatCb;
        public DataSourcesCount dataSourcesCountCb;
        public DataSourcesName dataSourcesNameCb;
        public DataSourcesSet dataSourcesSetCb;
        public IntPtr data;
    }

    public struct RemoteCommand
    {
        public RemoteCommandCB remoteCommandCB;
        public IntPtr data;
    }

    public struct RemoteNotification
    {
        public Int32 stopped;
    }

}
