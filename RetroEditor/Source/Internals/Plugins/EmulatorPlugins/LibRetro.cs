using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs.BleedingEdge;
using RetroEditor.Plugins;

internal class LibRetroPlugin : IDisposable
{
    public enum MemoryKind
    {
        SaveRam = 0,
        RTC = 1,
        SystemRam = 2,
        VideoRam = 3

    }
    public enum PixelFormat
    {
        RGB1555 = 0,
        XRGB8888 = 1,
        RGB565 = 2
    }

    public struct RetroSystemInfo
    {
        public string libraryName;
        public string libraryVersion;
        public string validExtensions;
        public bool needFullPath;
        public bool blockExtract;
    }


    public struct RetroSystemAVInfo
    {
        public struct RetroSystemTiming
        {
            public double fps;
            public double sampleRate;
        }
        public struct RetroGameGeometry
        {
            public uint baseWidth;
            public uint baseHeight;
            public uint maxWidth;
            public uint maxHeight;
            public float aspectRatio;
        }

        public RetroGameGeometry geometry;
        public RetroSystemTiming timing;
        public PixelFormat pixelFormat;
    }
    
    public struct MemoryMap
    {
        public UInt64 flags;
        public IntPtr ptr;
        public UInt64 offset;
        public UInt64 start;
        public UInt64 select;
        public UInt64 disconnect;
        public UInt64 len;
        public string addressSpace;
    }

    public enum RetroKey
    {
        RETROK_UNKNOWN = 0,
        RETROK_FIRST = 0,
        RETROK_BACKSPACE = 8,
        RETROK_TAB = 9,
        RETROK_CLEAR = 12,
        RETROK_RETURN = 13,
        RETROK_PAUSE = 19,
        RETROK_ESCAPE = 27,
        RETROK_SPACE = 32,
        RETROK_EXCLAIM = 33,
        RETROK_QUOTEDBL = 34,
        RETROK_HASH = 35,
        RETROK_DOLLAR = 36,
        RETROK_AMPERSAND = 38,
        RETROK_QUOTE = 39,
        RETROK_LEFTPAREN = 40,
        RETROK_RIGHTPAREN = 41,
        RETROK_ASTERISK = 42,
        RETROK_PLUS = 43,
        RETROK_COMMA = 44,
        RETROK_MINUS = 45,
        RETROK_PERIOD = 46,
        RETROK_SLASH = 47,
        RETROK_0 = 48,
        RETROK_1 = 49,
        RETROK_2 = 50,
        RETROK_3 = 51,
        RETROK_4 = 52,
        RETROK_5 = 53,
        RETROK_6 = 54,
        RETROK_7 = 55,
        RETROK_8 = 56,
        RETROK_9 = 57,
        RETROK_COLON = 58,
        RETROK_SEMICOLON = 59,
        RETROK_LESS = 60,
        RETROK_EQUALS = 61,
        RETROK_GREATER = 62,
        RETROK_QUESTION = 63,
        RETROK_AT = 64,
        RETROK_LEFTBRACKET = 91,
        RETROK_BACKSLASH = 92,
        RETROK_RIGHTBRACKET = 93,
        RETROK_CARET = 94,
        RETROK_UNDERSCORE = 95,
        RETROK_BACKQUOTE = 96,
        RETROK_a = 97,
        RETROK_b = 98,
        RETROK_c = 99,
        RETROK_d = 100,
        RETROK_e = 101,
        RETROK_f = 102,
        RETROK_g = 103,
        RETROK_h = 104,
        RETROK_i = 105,
        RETROK_j = 106,
        RETROK_k = 107,
        RETROK_l = 108,
        RETROK_m = 109,
        RETROK_n = 110,
        RETROK_o = 111,
        RETROK_p = 112,
        RETROK_q = 113,
        RETROK_r = 114,
        RETROK_s = 115,
        RETROK_t = 116,
        RETROK_u = 117,
        RETROK_v = 118,
        RETROK_w = 119,
        RETROK_x = 120,
        RETROK_y = 121,
        RETROK_z = 122,
        RETROK_LEFTBRACE = 123,
        RETROK_BAR = 124,
        RETROK_RIGHTBRACE = 125,
        RETROK_TILDE = 126,
        RETROK_DELETE = 127,

        RETROK_KP0 = 256,
        RETROK_KP1 = 257,
        RETROK_KP2 = 258,
        RETROK_KP3 = 259,
        RETROK_KP4 = 260,
        RETROK_KP5 = 261,
        RETROK_KP6 = 262,
        RETROK_KP7 = 263,
        RETROK_KP8 = 264,
        RETROK_KP9 = 265,
        RETROK_KP_PERIOD = 266,
        RETROK_KP_DIVIDE = 267,
        RETROK_KP_MULTIPLY = 268,
        RETROK_KP_MINUS = 269,
        RETROK_KP_PLUS = 270,
        RETROK_KP_ENTER = 271,
        RETROK_KP_EQUALS = 272,

        RETROK_UP = 273,
        RETROK_DOWN = 274,
        RETROK_RIGHT = 275,
        RETROK_LEFT = 276,
        RETROK_INSERT = 277,
        RETROK_HOME = 278,
        RETROK_END = 279,
        RETROK_PAGEUP = 280,
        RETROK_PAGEDOWN = 281,

        RETROK_F1 = 282,
        RETROK_F2 = 283,
        RETROK_F3 = 284,
        RETROK_F4 = 285,
        RETROK_F5 = 286,
        RETROK_F6 = 287,
        RETROK_F7 = 288,
        RETROK_F8 = 289,
        RETROK_F9 = 290,
        RETROK_F10 = 291,
        RETROK_F11 = 292,
        RETROK_F12 = 293,
        RETROK_F13 = 294,
        RETROK_F14 = 295,
        RETROK_F15 = 296,

        RETROK_NUMLOCK = 300,
        RETROK_CAPSLOCK = 301,
        RETROK_SCROLLOCK = 302,
        RETROK_RSHIFT = 303,
        RETROK_LSHIFT = 304,
        RETROK_RCTRL = 305,
        RETROK_LCTRL = 306,
        RETROK_RALT = 307,
        RETROK_LALT = 308,
        RETROK_RMETA = 309,
        RETROK_LMETA = 310,
        RETROK_LSUPER = 311,
        RETROK_RSUPER = 312,
        RETROK_MODE = 313,
        RETROK_COMPOSE = 314,

        RETROK_HELP = 315,
        RETROK_PRINT = 316,
        RETROK_SYSREQ = 317,
        RETROK_BREAK = 318,
        RETROK_MENU = 319,
        RETROK_POWER = 320,
        RETROK_EURO = 321,
        RETROK_UNDO = 322,
        RETROK_OEM_102 = 323,

        RETROK_LAST,
    }

    public const int RetroKeyArrayCount = 512;
    private IEditorInternal _editor;
    private GCHandle _pinnedEditor;
    internal string DllName { get; private set; }

    public LibRetroPlugin(IEditorInternal editor, string path)
    {
        _editor = editor;
        _pinnedEditor = GCHandle.Alloc(_editor);
        var pathOverride = Environment.GetEnvironmentVariable("RETROEDITOR_OVERRIDE_LIBRETRO_PATH");
        if (pathOverride != null)
        {
            path = pathOverride;
        }
        DllName = Path.GetFileNameWithoutExtension(path);
        nativeGameInfo = Marshal.AllocHGlobal(Marshal.SizeOf<retro_game_info_ext>());   // we need to own this memory, dispose will free
        keyArray = new bool[RetroKeyArrayCount];
        keyMap = new int[RetroKeyArrayCount];
        InitKeyboardToRetroKeyMap();
        disableVideo = false;
        frameBuffer = Array.Empty<byte>();
        frameBufferWidth = 0;
        frameBufferHeight = 0;
        pixelFormat = PixelFormat.RGB1555;
        libraryHandle = NativeLibrary.Load(path);
        nativeVersion = Marshal.GetDelegateForFunctionPointer<retro_api_version>(NativeLibrary.GetExport(libraryHandle, "retro_api_version"));
        nativeGetSystemInfo = Marshal.GetDelegateForFunctionPointer<retro_get_system_info>(NativeLibrary.GetExport(libraryHandle, "retro_get_system_info"));
        nativeSetEnvironment = Marshal.GetDelegateForFunctionPointer<retro_set_environment>(NativeLibrary.GetExport(libraryHandle, "retro_set_environment"));
        nativeGetSystemAVInfo = Marshal.GetDelegateForFunctionPointer<retro_get_system_av_info>(NativeLibrary.GetExport(libraryHandle, "retro_get_system_av_info"));
        nativeInit = Marshal.GetDelegateForFunctionPointer<retro_init>(NativeLibrary.GetExport(libraryHandle, "retro_init"));
        nativeSetAudioSample = Marshal.GetDelegateForFunctionPointer<retro_set_audio_sample>(NativeLibrary.GetExport(libraryHandle, "retro_set_audio_sample"));
        nativeSetAudioSampleBatch = Marshal.GetDelegateForFunctionPointer<retro_set_audio_sample_batch>(NativeLibrary.GetExport(libraryHandle, "retro_set_audio_sample_batch"));
        nativeSetInputPoll = Marshal.GetDelegateForFunctionPointer<retro_set_input_poll>(NativeLibrary.GetExport(libraryHandle, "retro_set_input_poll"));
        nativeSetInputState = Marshal.GetDelegateForFunctionPointer<retro_set_input_state>(NativeLibrary.GetExport(libraryHandle, "retro_set_input_state"));
        nativeSetVideoRefresh = Marshal.GetDelegateForFunctionPointer<retro_set_video_refresh>(NativeLibrary.GetExport(libraryHandle, "retro_set_video_refresh"));
        nativeLoadGame = Marshal.GetDelegateForFunctionPointer<retro_load_game>(NativeLibrary.GetExport(libraryHandle, "retro_load_game"));
        nativeUnloadGame = Marshal.GetDelegateForFunctionPointer<retro_unload_game>(NativeLibrary.GetExport(libraryHandle, "retro_unload_game"));
        nativeGetMemorySize = Marshal.GetDelegateForFunctionPointer<retro_get_memory_size>(NativeLibrary.GetExport(libraryHandle, "retro_get_memory_size"));
        nativeGetMemoryData = Marshal.GetDelegateForFunctionPointer<retro_get_memory_data>(NativeLibrary.GetExport(libraryHandle, "retro_get_memory_data"));
        nativeSerializeSize = Marshal.GetDelegateForFunctionPointer<retro_serialize_size>(NativeLibrary.GetExport(libraryHandle, "retro_serialize_size"));
        nativeSerialize = Marshal.GetDelegateForFunctionPointer<retro_serialize>(NativeLibrary.GetExport(libraryHandle, "retro_serialize"));
        nativeUnserialize = Marshal.GetDelegateForFunctionPointer<retro_unserialize>(NativeLibrary.GetExport(libraryHandle, "retro_unserialize"));
        nativeReset = Marshal.GetDelegateForFunctionPointer<retro_reset>(NativeLibrary.GetExport(libraryHandle, "retro_reset"));
        nativeRun = Marshal.GetDelegateForFunctionPointer<retro_run>(NativeLibrary.GetExport(libraryHandle, "retro_run"));
        nativeDeinit = Marshal.GetDelegateForFunctionPointer<retro_deinit>(NativeLibrary.GetExport(libraryHandle, "retro_deinit"));

        environmentCallback = EnvironmentCallback;
        audioSampleCallback = AudioSampleCallback;
        audioSampleBatchCallback = AudioSampleBatchCallback;
        inputPollCallback = InputPollCallback;
        inputStateCallback = InputStateCallback;
        videoRefreshCallback = VideoRefreshCallback;

        audioHelper = new RayLibAudioHelper();
        temporaryPath = editor.Settings.MameDebuggerDataFolder;
        debuggerTrampoline = IntPtr.Zero;
        debuggerCallback = null;
        loadedPath = "";
        memoryMaps = Array.Empty<MemoryMap>();

        core_options = new();

        nativeSetEnvironment.Invoke(environmentCallback);
        nativeSetAudioSample.Invoke(audioSampleCallback);
        nativeSetAudioSampleBatch.Invoke(audioSampleBatchCallback);
        nativeSetInputPoll.Invoke(inputPollCallback);
        nativeSetInputState.Invoke(inputStateCallback);
        nativeSetVideoRefresh.Invoke(videoRefreshCallback);
    }

    public void SetDebuggerCallback(DebuggerCallbackDelegate callback)
    {
        debuggerCallback = callback;
        debuggerTrampoline = Marshal.GetFunctionPointerForDelegate(callback);
    }

    private string temporaryPath;
    private RayLibAudioHelper audioHelper;

    public uint Version()
    {
        return nativeVersion();
    }

    public RetroSystemInfo GetSystemInfo()
    {
        var nativeInfo = new retro_system_info();
        unsafe
        {
            nativeGetSystemInfo(&nativeInfo);
        }
        var info = new RetroSystemInfo
        {
            libraryName = Marshal.PtrToStringAnsi(nativeInfo.library_name) ?? "",
            libraryVersion = Marshal.PtrToStringAnsi(nativeInfo.library_version) ?? "",
            validExtensions = Marshal.PtrToStringAnsi(nativeInfo.valid_extensions) ?? "",
            needFullPath = nativeInfo.need_fullpath != 0,
            blockExtract = nativeInfo.block_extract != 0
        };
        return info;
    }

    public void Init()
    {
        nativeInit.Invoke();
    }

    public void Deinit()
    {
        nativeDeinit.Invoke();
    }

    public RetroSystemAVInfo GetSystemAVInfo()
    {
        var nativeInfo = new retro_system_av_info();
        unsafe
        {
            nativeGetSystemAVInfo(&nativeInfo);
        }
        var info = new RetroSystemAVInfo
        {
            geometry = new RetroSystemAVInfo.RetroGameGeometry
            {
                baseWidth = nativeInfo.geometry.base_width,
                baseHeight = nativeInfo.geometry.base_height,
                maxWidth = nativeInfo.geometry.max_width,
                maxHeight = nativeInfo.geometry.max_height,
                aspectRatio = nativeInfo.geometry.aspect_ratio
            },
            timing = new RetroSystemAVInfo.RetroSystemTiming
            {
                fps = nativeInfo.timing.fps,
                sampleRate = nativeInfo.timing.sample_rate
            },
            pixelFormat = pixelFormat
        };
        return info;
    }

    public void LoadGame(string path)
    {
        var data= File.ReadAllBytes(path);
        InternalLoad(path, data);
    }

    public void LoadGame(string path, byte[] data)
    {
        InternalLoad(path, data);
    }

    public void UnloadGame()
    {
        nativeUnloadGame.Invoke();
    }

    private retro_game_info last_loaded_game;

    private void InternalLoad(string path, byte[] data)
    {
        loadedPath = path;
        loadedRom = Marshal.AllocHGlobal(data.Length);
        loadedRomSize=(UIntPtr)data.Length;
        Marshal.Copy(data, 0, loadedRom, data.Length);

        last_loaded_game = new retro_game_info
        {
            path = Marshal.StringToHGlobalAnsi(path),
            data = loadedRom,
            size = (UIntPtr)data.Length,
            meta = Marshal.StringToHGlobalAnsi("")
        };

        NativeLoadLastGame();

        // Should be able to allocate our framebuffer here
        var aVInfo=GetSystemAVInfo();
        frameBufferWidth=width=aVInfo.geometry.maxWidth;
        frameBufferHeight=height=aVInfo.geometry.maxHeight;
        frameBuffer = new byte[width * height * 4];

    }

    private void NativeLoadLastGame()
    {
        if (last_loaded_game.size == 0)
        {
            unsafe
            {
                nativeLoadGame(null);
            }
        }
        unsafe
        {
            fixed (retro_game_info* ptr = &last_loaded_game)
            {
                nativeLoadGame(ptr);
            }
        }
    }

    public UInt64 GetMemorySize(MemoryKind mem)
    {
        var size = nativeGetMemorySize((uint)mem);
        return (UInt64)size;
    }

    public byte[] GetMemoryData(MemoryKind mem, int start, int size)
    {
        var data = new byte[size];
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        unsafe
        {
            nint ptr = (nint)nativeGetMemoryData((uint)mem);
            Marshal.Copy(ptr, data, start, size);
        }
        dataHandle.Free();
        return data;
    }

    public void SetMemoryData(MemoryKind mem, int start, byte[] data)
    {
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        unsafe
        {
            nint ptr = (nint)nativeGetMemoryData((uint)mem);
            Marshal.Copy(data, start, ptr, data.Length);
        }
        dataHandle.Free();
    }

    public ReadOnlySpan<byte> GetMemory(UInt64 start, UInt64 size)
    {
        // Gets memory from memory maps - TODO Better?
        var data = new byte[size];
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        unsafe
        {

            foreach (var m in memoryMaps)
            {
                // check if the region in this map overlaps our request
                if ((m.start+m.len-1)<start || m.start>(start+size-1))
                {
                    continue;
                }

                // Calculate the amount of memory to pull from this map
                if (m.start<=start)
                {
                    var offsetIntoArray = start - m.start;
                    var offsetPtr = m.ptr + (nint)offsetIntoArray;
                    var ptr = (nint)offsetPtr;
                    var len = m.len - offsetIntoArray;
                    if (len>size)
                    {
                        len = size;
                    }
                    Marshal.Copy(ptr, data, 0, (int)len);
                }
                else
                {
                    var offsetIntoArray = m.start - start;
                    var ptr = (nint)m.ptr;
                    var len = m.len;
                    if (len>(size-offsetIntoArray))
                    {
                        len = size - offsetIntoArray;
                    }
                    Marshal.Copy(ptr, data, (int)offsetIntoArray, (int)len);
                }
            }
        }
        dataHandle.Free();
        return data;
    }

    public void SetMemory(UInt64 start, ReadOnlySpan<byte> toWrite)
    {
        // Sets memory in memory maps
        var data = toWrite.ToArray();   // TODO better?
        var dataHandle = GCHandle.Alloc(data.Length, GCHandleType.Pinned);
        unsafe
        {
            foreach (var m in memoryMaps)
            {
                // check if the region in this map overlaps our request
                if ((m.start + m.len - 1) < start || m.start > (start + (UInt64)(data.Length - 1)))
                {
                    continue;
                }

                // Calculate the amount of memory to poke in this map
                if (m.start<=start)
                {
                    var offsetIntoArray = start - m.start;
                    var offsetPtr = m.ptr + (nint)offsetIntoArray;
                    var ptr = (nint)offsetPtr;
                    var len = m.len - offsetIntoArray;
                    if (len>(UInt64)data.Length)
                    {
                        len = (UInt64)data.Length;
                    }
                    Marshal.Copy(data, 0, ptr, (int)len);
                }
                else
                {
                    var offsetIntoArray = m.start - start;
                    var ptr = (nint)m.ptr;
                    var len = m.len;
                    if (len>((UInt64)data.Length-offsetIntoArray))
                    {
                        len = (UInt64)data.Length - offsetIntoArray;
                    }
                    Marshal.Copy(data, (int)offsetIntoArray, ptr, (int)len);
                }
            }
        }
        dataHandle.Free();
    }

    public UInt64 RomLength()
    {
        return loadedRomSize;
    }

    public ReadOnlySpan<byte> FetchRom(uint address, uint length)
    {
        unsafe
        {
            return new ReadOnlySpan<byte>((void*)((nint)loadedRom + address), (int)length);
        }
    }

    public void WriteRom(uint address,ReadOnlySpan<byte> data)
    {
        unsafe
        {
            Marshal.Copy(data.ToArray(), 0, (nint)loadedRom + (nint)address, data.Length);
        }
    }

    public UInt64 GetSaveStateSize()
    {
        return nativeSerializeSize();
    }

    public bool SaveState(byte[] data)
    {
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var result = nativeSerialize(dataHandle.AddrOfPinnedObject(), (UIntPtr)data.Length);
        dataHandle.Free();
        return result != 0;
    }

    public bool RestoreState(byte[] data)
    {
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var result = nativeUnserialize(dataHandle.AddrOfPinnedObject(), (UIntPtr)data.Length);
        dataHandle.Free();
        return result != 0;
    }


    public void UpdateKey(KeyboardKey key, bool pressed)
    {
        if ((int)key < RetroKeyArrayCount)
        {
            keyArray[keyMap[(int)key]] = pressed;
        }
    }

    public void Reload()
    {
        NativeLoadLastGame();
    }

    public void Reset()
    {
        nativeReset.Invoke();
    }

    public void Run()
    {
        nativeRun.Invoke();
    }

    public void AutoLoad(IMemoryAccess romAccess, Func<IMemoryAccess,bool> condition)
    {
        disableVideo = true;
        while (!condition(romAccess))
        {
            Run();
        }
        disableVideo = false;
    }

    public byte[] GetFrameBuffer(out uint width, out uint height)
    {
        width = this.width;
        height = this.height;
        return frameBuffer;
    }

    public void Close()
    {
        UnloadGame();
        Deinit();
    }

    public void Dispose()
    {
        NativeLibrary.Free(libraryHandle);
        Marshal.FreeHGlobal(loadedRom);
        _pinnedEditor.Free();
    }

    private bool disableVideo;
    private string loadedPath;

    private nint nativeGameInfo;
    private nint loadedRom;
    private UIntPtr loadedRomSize;

    private PixelFormat pixelFormat;
    private IntPtr libraryHandle;
    private byte[] frameBuffer;
    private uint frameBufferWidth, frameBufferHeight;
    private uint width, height; // Last rendered width/height
    private MemoryMap[] memoryMaps;
    private bool[] keyArray;
    private int[] keyMap;

    private Dictionary<string, string[]> core_options;


    private delegate byte retro_environment_t(uint cmd, IntPtr data);
    private delegate void retro_audio_sample_t(short left, short right);
    private delegate void retro_audio_sample_batch_t(IntPtr data, UIntPtr frames);
    private delegate void retro_input_poll_t();
    private delegate short retro_input_state_t(uint port, uint device, uint index, uint id);
    private delegate void retro_video_refresh_t(IntPtr data, uint width, uint height, UIntPtr pitch);

    public delegate nint DebuggerCallbackDelegate(int kind, IntPtr data);

    private retro_environment_t environmentCallback;                // Prevent collection of delegate
    private retro_audio_sample_t audioSampleCallback;               // Prevent collection of delegate
    private retro_audio_sample_batch_t audioSampleBatchCallback;    // Prevent collection of delegate
    private retro_input_poll_t inputPollCallback;                   // Prevent collection of delegate  
    private retro_input_state_t inputStateCallback;                 // Prevent collection of delegate
    private retro_video_refresh_t videoRefreshCallback;             // Prevent collection of delegate
    private DebuggerCallbackDelegate? debuggerCallback;             // Prevent collection of delegate

    private unsafe delegate* unmanaged[Cdecl]<UInt64, void*, void> logCallbackDelegate;
    private unsafe delegate* unmanaged[Cdecl]<void*, UInt64, void*, void> logCallbackInstanceDelegate;
    private nint logCallbackTrampoline;
    private nint debuggerTrampoline;


    private delegate uint retro_api_version();
    private unsafe delegate void retro_get_system_info(retro_system_info* info);
    private delegate void retro_set_environment(retro_environment_t cb);
    private unsafe delegate void retro_get_system_av_info(retro_system_av_info* info);
    private delegate void retro_init();
    private delegate void retro_set_audio_sample(retro_audio_sample_t cb);
    private delegate void retro_set_audio_sample_batch(retro_audio_sample_batch_t cb);
    private delegate void retro_set_input_poll(retro_input_poll_t cb);
    private delegate void retro_set_input_state(retro_input_state_t cb);
    private delegate void retro_set_video_refresh(retro_video_refresh_t cb);
    private unsafe delegate void retro_load_game(retro_game_info* info);
    private delegate void retro_unload_game();
    private delegate UIntPtr retro_get_memory_size(uint id);
    private unsafe delegate void* retro_get_memory_data(uint id);
    private delegate UIntPtr retro_serialize_size();
    private delegate byte retro_serialize(IntPtr data, UIntPtr size);
    private delegate byte retro_unserialize(IntPtr data, UIntPtr size);
    private delegate void retro_reset();
    private delegate void retro_run();
    private delegate void retro_deinit();
    
    private retro_api_version nativeVersion;
    private retro_get_system_info nativeGetSystemInfo;
    private retro_set_environment nativeSetEnvironment;
    private retro_get_system_av_info nativeGetSystemAVInfo;
    private retro_init nativeInit;
    private retro_set_audio_sample nativeSetAudioSample;
    private retro_set_audio_sample_batch nativeSetAudioSampleBatch;
    private retro_set_input_poll nativeSetInputPoll;
    private retro_set_input_state nativeSetInputState;
    private retro_set_video_refresh nativeSetVideoRefresh;
    private retro_load_game nativeLoadGame;
    private retro_unload_game nativeUnloadGame;
    private retro_get_memory_size nativeGetMemorySize;
    private retro_get_memory_data nativeGetMemoryData;
    private retro_serialize_size nativeSerializeSize;
    private retro_serialize nativeSerialize;
    private retro_unserialize nativeUnserialize;
    private retro_reset nativeReset;
    private retro_run nativeRun;
    private retro_deinit nativeDeinit;


    private enum EnvironmentCommand
    {
        ENVIRONMENT_SET_ROTATION = 1,
        ENVIRONMENT_GET_CAN_DUPE = 3,
        ENVIRONMENT_GET_SYSTEM_DIRECTORY = 9,
        ENVIRONMENT_SET_PIXEL_FORMAT = 10,
        ENVIRONMENT_SET_INPUT_DESCRIPTORS = 11,
        ENVIRONMENT_SET_KEYBOARD_CALLBACK = 12,
        ENVIRONMENT_GET_VARIABLE = 15,
        ENVIRONMENT_SET_VARIABLES = 16,
        ENVIRONMENT_GET_VARIABLE_UPDATE = 17,
        ENVIRONMENT_SET_SUPPORT_NO_GAME = 18,
        ENVIRONMENT_GET_LOG_INTERFACE = 27,
        ENVIRONMENT_GET_CORE_ASSETS_DIRECTORY = 30,
        ENVIRONMENT_GET_SAVE_DIRECTORY = 31,
        ENVIRONMENT_SET_CONTROLLER_INFO = 35,
        ENVIRONMENT_SET_MEMORY_MAPS = 36,
        ENVIRONMENT_SET_GEOMETRY = 37,
        ENVIRONMENT_GET_LED_INTERFACE = 46,
        ENVIRONMENT_GET_AUDIO_VIDEO_ENABLE = 47,
        ENVIRONMENT_GET_INPUT_BITMASKS = 51,
        ENVIRONMENT_GET_CORE_OPTIONS_VERSION = 52,
        ENVIRONMENT_GET_MESSAGE_INTERFACE_VERSION = 59,
        ENVIRONMENT_SET_RETRO_FAST_FORWARDING_OVERRIDE = 64,
        ENVIRONMENT_GET_GAME_INFO_EXT = 66,

        // Custom (for now)
        ENVIRONMENT_GET_DEBUGGER_INTERFACE = 999,
    }

    private struct retro_system_info
    {
        public IntPtr library_name;
        public IntPtr library_version;
        public IntPtr valid_extensions;
        public byte need_fullpath;
        public byte block_extract;
    }
    
    private struct retro_game_geometry
    {
        public uint base_width;
        public uint base_height;
        public uint max_width;
        public uint max_height;
        public float aspect_ratio;
    }

    private struct retro_system_timing
    {
        public double fps;
        public double sample_rate;
    }

    private struct retro_system_av_info
    {
        public retro_game_geometry geometry;
        public retro_system_timing timing;
    }

    private struct retro_game_info
    {
        public IntPtr path;
        public IntPtr data;
        public UIntPtr size;
        public IntPtr meta;
    }

    private readonly struct retro_memory_descriptor
    {
        public retro_memory_descriptor()
        {
            flags = 0;
            ptr = IntPtr.Zero;
            offset = IntPtr.Zero;
            start = IntPtr.Zero;
            select = IntPtr.Zero;
            disconnect = IntPtr.Zero;
            len = IntPtr.Zero;
            addressSpace = IntPtr.Zero;
        }
        public readonly UInt64 flags;
        public readonly IntPtr ptr;
        public readonly IntPtr offset;
        public readonly IntPtr start;
        public readonly IntPtr select;
        public readonly IntPtr disconnect;
        public readonly IntPtr len;
        public readonly IntPtr addressSpace;
    }

    private struct retro_log_callback
    {
        public nint log;
    }

    private readonly struct retro_memory_map
    {
        public retro_memory_map()
        {
            descriptors = IntPtr.Zero;
            num_descriptors = 0;
        }
        public readonly IntPtr descriptors;
        public readonly uint num_descriptors;
    }

    private struct retro_variable
    {
        public retro_variable()
        {
            key = IntPtr.Zero;
        }
        public readonly IntPtr key;
        public IntPtr value;
    }

    private readonly struct retro_input_descriptor
    {
        public retro_input_descriptor()
        {
            port = 0;
            device = 0;
            index = 0;
            id = 0;
            description = IntPtr.Zero;
        }
        public readonly uint port;
        public readonly uint device;
        public readonly uint index;
        public readonly uint id;
        public readonly IntPtr description;
    }
    
    private readonly struct retro_controller_description
    {
        public retro_controller_description()
        {
            id = 0;
            description = IntPtr.Zero;
        }
        public readonly IntPtr description;
        public readonly uint id;
    }

    private readonly struct retro_controller_info
    {
        public retro_controller_info()
        {
            types = IntPtr.Zero;
            num_types = 0;
        }
        public readonly IntPtr types;
        public readonly uint num_types;
    }

    private struct retro_game_info_ext
    {
        public IntPtr full_path;
        public IntPtr archive_path;
        public IntPtr archive_file;
        public IntPtr dir;
        public IntPtr name;
        public IntPtr ext;
        public IntPtr meta;
        public IntPtr data;
        public UInt64 size;
        public byte file_in_archive;
        public byte persistent_data;
    }

    struct retro_keyboard_callback
    {
        public retro_keyboard_event_t callback;
    }

    private delegate void retro_keyboard_event_t(byte down, uint keycode, uint character, ushort key_modifiers);

    private void InitKeyboardToRetroKeyMap()
    {
        for (int a=(int)KeyboardKey.A; a<=(int)KeyboardKey.Z;a++)
        {
            keyMap[a] = (a - (int)KeyboardKey.A) + (int)RetroKey.RETROK_a;
        }
        for (int a=(int)KeyboardKey.Zero;a<=(int)KeyboardKey.Nine;a++)
        {
            keyMap[a] = (a - (int)KeyboardKey.Zero) + (int)RetroKey.RETROK_0;
        }
        keyMap[(int)KeyboardKey.Space] = (int)RetroKey.RETROK_SPACE;
        keyMap[(int)KeyboardKey.Up] = (int)RetroKey.RETROK_UP;
        keyMap[(int)KeyboardKey.Down] = (int)RetroKey.RETROK_DOWN;
        keyMap[(int)KeyboardKey.Left] = (int)RetroKey.RETROK_LEFT;
        keyMap[(int)KeyboardKey.Right] = (int)RetroKey.RETROK_RIGHT;
        keyMap[(int)KeyboardKey.Enter] = (int)RetroKey.RETROK_RETURN;
        keyMap[(int)KeyboardKey.LeftShift] = (int)RetroKey.RETROK_LSHIFT;
        keyMap[(int)KeyboardKey.RightShift] = (int)RetroKey.RETROK_RSHIFT;
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private unsafe static void LogCallback(void* instance, UInt64 level, void* stringPtr)
    {
        var editor = GCHandle.FromIntPtr(new IntPtr(instance)).Target as IEditorInternal;
        if (editor == null)
        {
            return;
        }
        var fmtString = Marshal.PtrToStringAnsi(new IntPtr(stringPtr));
        if (fmtString != null)
        {
            editor.Log(LogType.Info, "LibRetro", fmtString);
        }
    }

    private byte EnvironmentCallback(uint cmd, IntPtr data)
    {
        var experimental = (cmd & 0x10000) == 0x10000;
        var frontendPrivate = (cmd & 0x20000) == 0x20000;
        cmd &= 0xFFFF;
        var command = (EnvironmentCommand)cmd;
        switch (command)
        {
            case EnvironmentCommand.ENVIRONMENT_SET_ROTATION:
                {
                    Marshal.WriteInt32(data, 0);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_CAN_DUPE:
                {
                    Marshal.WriteByte(data, 1);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_SYSTEM_DIRECTORY:
                {
                    Marshal.WriteIntPtr(data, Marshal.StringToHGlobalAnsi(temporaryPath));
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_PIXEL_FORMAT:
                {
                    pixelFormat = (PixelFormat)Marshal.ReadInt32(data);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_INPUT_DESCRIPTORS:
                {
                    var descSize = Marshal.SizeOf<retro_input_descriptor>();
                    while (true)
                    {
                        var descriptor = Marshal.PtrToStructure<retro_input_descriptor>(data);
                        if (descriptor.description == IntPtr.Zero)
                        {
                            break;
                        }
                        var port = descriptor.port;
                        var device = descriptor.device;
                        var index = descriptor.index;
                        var id = descriptor.id;
                        var description = Marshal.PtrToStringAnsi(descriptor.description);
                        _editor.Log(LogType.Debug, "LibRetro", $"INPUT DESCRIPTOR : {port}, {device}, {index}, {id}, {description}");
                        data += descSize;
                    }
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_KEYBOARD_CALLBACK:
                {
                    Marshal.StructureToPtr(new retro_keyboard_callback { callback = KeyboardCallback }, data, false);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_VARIABLE:
                {
                    var variable = Marshal.PtrToStructure<retro_variable>(data);
                    var key = Marshal.PtrToStringAnsi(variable.key);
                    if (key!=null && core_options.ContainsKey(key))
                    {
                        var value = core_options[key][1];
                        if (key == "mame_media_type")   // hack for mame and consoles, need to make configurable, or autodetect
                        {
                            value = "cart";
                        }
                        else if (key == "mame_softlists_enable")
                        {
                            // Disable softlists, as they override media type
                            value = "disabled";
                        }
                        variable.value = Marshal.StringToHGlobalAnsi(value);
                        _editor.Log(LogType.Debug, "LibRetro", $"Get variable: {key} {value}");
                        Marshal.StructureToPtr(variable, data, true);
                        return 1;
                    }
                    _editor.Log(LogType.Debug, "LibRetro", $"Get variable (UNKNOWN): {key}");
                    return 0;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_VARIABLES:
                {
                    var varSize = Marshal.SizeOf<retro_variable>();
                    while (true)
                    {
                        var variable = Marshal.PtrToStructure<retro_variable>(data);
                        if (variable.key == IntPtr.Zero && variable.value == IntPtr.Zero)
                        {
                            break;
                        }
                        var key = Marshal.PtrToStringAnsi(variable.key);
                        var value = Marshal.PtrToStringAnsi(variable.value);
                        var valueS = value?.Split(";");
                        if (key!=null && valueS != null && valueS.Length == 2)
                        {
                            _editor.Log(LogType.Debug, "LibRetro", $"Set variable: {key}, {value}");

                            var valueDisplayName = valueS[0].Trim();    // Stored in 0th slot
                            var valueOptions = valueS[1].Trim().Split("|");
                            if (valueOptions != null)
                            {
                                var storeData = new string[valueOptions.Length + 1];
                                Array.Copy(valueOptions, 0, storeData, 1, valueOptions.Length);
                                storeData[0] = valueDisplayName;

                                if (core_options.ContainsKey(key))
                                {
                                    core_options[key] = storeData;
                                }
                                else
                                {
                                    core_options.Add(key, storeData);
                                }
                            }
                            else
                            {
                                _editor.Log(LogType.Warning, " LibRetro", $"Skipping Variable {key} as {value} not supported (options empty)");
                            }
                        }
                        else
                        {
                            _editor.Log(LogType.Warning, " LibRetro", $"Skipping Variable {key} as {value} not supported (display name and/or options empty)");
                        }
                        data += varSize;
                    }
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_VARIABLE_UPDATE:
                {
                    return 0;   // no variables updated since last run
                }
            case EnvironmentCommand.ENVIRONMENT_SET_SUPPORT_NO_GAME:
                {
                    _editor.Log(LogType.Debug, "LibRetro", $"Supports No Game : {Marshal.ReadByte(data)}");
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_LOG_INTERFACE:
                {
                    unsafe
                    {
                        logCallbackInstanceDelegate = &LogCallback;
                        logCallbackDelegate = (delegate* unmanaged[Cdecl]<UInt64, void*, void>)InstanceTrampoline.InterfaceTrampoline.AllocateTrampoline(GCHandle.ToIntPtr(_pinnedEditor), 2, (nint)logCallbackInstanceDelegate);
                        logCallbackTrampoline = InstanceTrampoline.InterfaceTrampoline.AllocatePrinter((nint)logCallbackDelegate);
                    }

                    var logInterface = new retro_log_callback
                    {
                        log = logCallbackTrampoline
                    };
                    Marshal.StructureToPtr(logInterface, data, false);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_CORE_ASSETS_DIRECTORY:
                {
                    Marshal.WriteIntPtr(data, Marshal.StringToHGlobalAnsi(temporaryPath));
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_SAVE_DIRECTORY:
                {
                    Marshal.WriteIntPtr(data, Marshal.StringToHGlobalAnsi(temporaryPath));
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_CONTROLLER_INFO:
                {
                    var controllerSize = Marshal.SizeOf<retro_controller_info>();
                    while (true)
                    {
                        var controller = Marshal.PtrToStructure<retro_controller_info>(data);
                        if (controller.types == IntPtr.Zero && controller.num_types == 0)
                        {
                            break;
                        }
                        var descSize = Marshal.SizeOf<retro_controller_description>();
                        for (int a = 0; a < controller.num_types; a++)
                        {
                            var controllerDesc = Marshal.PtrToStructure<retro_controller_description>(controller.types + (a * descSize));
                            var description = Marshal.PtrToStringAnsi(controllerDesc.description);
                            var id = controllerDesc.id;
                            _editor.Log(LogType.Debug, "LibRetro", $"CONTROLLER INFO : {description}, {id}");
                        }
                        data += controllerSize;
                    }

                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_MEMORY_MAPS:
                {
                    var memoryMap = Marshal.PtrToStructure<retro_memory_map>(data);

                    memoryMaps = new MemoryMap[memoryMap.num_descriptors];
                    for (int a = 0; a < memoryMap.num_descriptors; a++)
                    {
                        var ptr = memoryMap.descriptors + (a * Marshal.SizeOf<retro_memory_descriptor>());
                        var descriptor = Marshal.PtrToStructure<retro_memory_descriptor>(ptr);
                        memoryMaps[a] = new MemoryMap
                        {
                            flags = (UInt64)descriptor.flags,
                            ptr = descriptor.ptr,
                            offset = (UInt64)descriptor.offset,
                            start = (UInt64)descriptor.start,
                            select = (UInt64)descriptor.select,
                            disconnect = (UInt64)descriptor.disconnect,
                            len = (UInt64)descriptor.len,
                            addressSpace = Marshal.PtrToStringAnsi(descriptor.addressSpace) ?? ""
                        };
                        _editor.Log(LogType.Debug, "LibRetro", $"MEMORY MAP : {memoryMaps[a].flags}, {memoryMaps[a].ptr}, {memoryMaps[a].offset}, {memoryMaps[a].start}, {memoryMaps[a].select}, {memoryMaps[a].disconnect}, {memoryMaps[a].len}, {memoryMaps[a].addressSpace}");
                    }
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_GEOMETRY:
                {
                    var geometry = Marshal.PtrToStructure<retro_game_geometry>(data);
                    _editor.Log(LogType.Debug, "LibRetro", $"GEOMETRY : {geometry.base_width}, {geometry.base_height}, {geometry.max_width}, {geometry.max_height}, {geometry.aspect_ratio}");
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_LED_INTERFACE:
                {
                    return 0;   // No LED interface
                }
            case EnvironmentCommand.ENVIRONMENT_GET_AUDIO_VIDEO_ENABLE:
                {
                    // Just want video for now
                    Marshal.WriteInt32(data, 3);    // bits 3-0 HardAudioDisable|FastSave|AudioDisable|VideoDisable
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_INPUT_BITMASKS:
                {
                    //var ptr = Marshal.ReadIntPtr(data);
                    //Marshal.WriteByte(ptr, 0);    // No input bitmasks
                    return 0;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_CORE_OPTIONS_VERSION:
                {
                    return 0;       //version 0 for now
                }
            case EnvironmentCommand.ENVIRONMENT_GET_MESSAGE_INTERFACE_VERSION:
                {
                    Marshal.WriteInt32(data, 1);    // We support version 1

                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_RETRO_FAST_FORWARDING_OVERRIDE:
                {
                    return 0;   // No fast forwarding
                }
            case EnvironmentCommand.ENVIRONMENT_GET_GAME_INFO_EXT:
                {
                    var gameInfo = new retro_game_info_ext
                    {
                        full_path = Marshal.StringToHGlobalAnsi(""),
                        archive_path = Marshal.StringToHGlobalAnsi(loadedPath),
                        archive_file = Marshal.StringToHGlobalAnsi(Path.GetFileName(loadedPath)),
                        dir = Marshal.StringToHGlobalAnsi(Path.GetDirectoryName(loadedPath)),
                        name = Marshal.StringToHGlobalAnsi(Path.GetFileNameWithoutExtension(loadedPath)),
                        ext = Marshal.StringToHGlobalAnsi(Path.GetExtension(loadedPath).ToLower().TrimStart('.')),
                        meta = Marshal.StringToHGlobalAnsi(""),
                        data = loadedRom,
                        size = loadedRomSize,
                        file_in_archive = 1,
                        persistent_data = 1
                    };
                    Marshal.StructureToPtr(gameInfo, nativeGameInfo, false);
                    Marshal.WriteIntPtr(data, nativeGameInfo);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_DEBUGGER_INTERFACE:
                {
                    Marshal.WriteIntPtr(data, debuggerTrampoline);
                    return 1;
                }

            default:
                {
                    _editor.Log(LogType.Warning, "LibRetro", $"Unhandled Environment callback :  {cmd} {(experimental ? "Experimental" : "")} {(frontendPrivate ? "Private" : "")}");
                }
                return 0;
        }
    }


    private void InputPollCallback()
    {
    }

    private enum RetroJoyPad
    {
        RETRO_DEVICE_ID_JOYPAD_B = 0,
        RETRO_DEVICE_ID_JOYPAD_Y = 1,
        RETRO_DEVICE_ID_JOYPAD_SELECT = 2,
        RETRO_DEVICE_ID_JOYPAD_START = 3,
        RETRO_DEVICE_ID_JOYPAD_UP = 4,
        RETRO_DEVICE_ID_JOYPAD_DOWN = 5,
        RETRO_DEVICE_ID_JOYPAD_LEFT = 6,
        RETRO_DEVICE_ID_JOYPAD_RIGHT = 7,
        RETRO_DEVICE_ID_JOYPAD_A = 8,
        RETRO_DEVICE_ID_JOYPAD_X = 9,
        RETRO_DEVICE_ID_JOYPAD_L = 10,
        RETRO_DEVICE_ID_JOYPAD_R = 11,
        RETRO_DEVICE_ID_JOYPAD_L2 = 12,
        RETRO_DEVICE_ID_JOYPAD_R2 = 13,
        RETRO_DEVICE_ID_JOYPAD_L3 = 14,
        RETRO_DEVICE_ID_JOYPAD_R3 = 15,
    }
    private short InputStateCallback(uint port, uint device, uint index, uint id)
    {
        if (device == 1)
        {
            // Joypad
            switch (id)
            {
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_B:
                    return (short)(keyArray[(int)RetroKey.RETROK_x] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_Y:
                    return (short)(keyArray[(int)RetroKey.RETROK_s] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_A:
                    return (short)(keyArray[(int)RetroKey.RETROK_z] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_X:
                    return (short)(keyArray[(int)RetroKey.RETROK_a] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_UP:
                    return (short)(keyArray[(int)RetroKey.RETROK_UP] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_DOWN:
                    return (short)(keyArray[(int)RetroKey.RETROK_DOWN] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_LEFT:
                    return (short)(keyArray[(int)RetroKey.RETROK_LEFT] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_RIGHT:
                    return (short)(keyArray[(int)RetroKey.RETROK_RIGHT] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_L:
                    return (short)(keyArray[(int)RetroKey.RETROK_q] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_R:
                    return (short)(keyArray[(int)RetroKey.RETROK_w] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_SELECT:
                    return (short)(keyArray[(int)RetroKey.RETROK_n] ? 1 : 0);
                case (int)RetroJoyPad.RETRO_DEVICE_ID_JOYPAD_START:
                    return (short)(keyArray[(int)RetroKey.RETROK_m] ? 1 : 0);
            }
        }

        // return results for inputs requested here
        if (device == 3 && id < RetroKeyArrayCount)
        {
            return (short)(keyArray[id] == true ? 1 : 0);
        }
        return 0;
    }

    private void VideoRefreshCallback(IntPtr data, uint width, uint height, UIntPtr pitch)
    {
        if (disableVideo)
        {
            return;
        }

        if (data == IntPtr.Zero)
        {
            return;// No data to render
        }

        this.width = width;
        this.height = height;
        var frameBufferPitch = (frameBufferWidth-width)*4;
        // Perform pixel format conversion here - todo avoid need to do this, make texture correct format in rendering and share it with libretro
        switch (pixelFormat)
        {
            case PixelFormat.RGB565:
            {
                unsafe 
                {
                    var src = (ushort*)data;
                    uint frameBufferPos = 0;
                    var srcNextLine=(pitch.ToUInt32() / 2) - width;
                    for (int y=0;y<height;y++)
                    {
                        for (int x=0;x<width;x++)
                        {
                            var pixel = *src++;
                            var r = (byte)((pixel & 0xF800) >> 11);
                            var g = (byte)((pixel & 0x07E0) >> 5);
                            var b = (byte)((pixel & 0x001F) >> 0);
                            frameBuffer[frameBufferPos++] = (byte)((r << 3) | (r >> 2));
                            frameBuffer[frameBufferPos++] = (byte)((g << 2) | (g >> 4));
                            frameBuffer[frameBufferPos++] = (byte)((b << 3) | (b >> 2));
                            frameBuffer[frameBufferPos++] = 255;
                        }
                        src += srcNextLine;
                        frameBufferPos += frameBufferPitch;
                    }
                }
                break;
            }
            case PixelFormat.XRGB8888:
            {
                unsafe 
                {
                    var src = (uint*)data;
                    uint frameBufferPos = 0;
                    var srcNextLine=(pitch.ToUInt32() / 4) - width;
                    for (int y=0;y<height;y++)
                    {
                        for (int x=0;x<width;x++)
                        {
                            var pixel = *src++;
                            frameBuffer[frameBufferPos++] = (byte)((pixel & 0x00FF0000) >> 16);
                            frameBuffer[frameBufferPos++] = (byte)((pixel & 0x0000FF00) >> 8);
                            frameBuffer[frameBufferPos++] = (byte)((pixel & 0x000000FF) >> 0);
                            frameBuffer[frameBufferPos++] = 255;
                        }
                        src += srcNextLine;
                        frameBufferPos += frameBufferPitch;
                    }
                }
                break;
            }
            default:
                throw new Exception($"TODO implement pixel format conversion {pixelFormat}");
        }
    }

    private void KeyboardCallback(byte down, uint keycode, uint character, ushort key_modifiers)
    {
        _editor.Log(LogType.Debug, "LibRetro", $"Keyboard callback: {down}, {keycode}, {character}, {key_modifiers}");
        if (keycode < RetroKeyArrayCount)
        {
            keyArray[keycode] = down != 0;
        }
    }


    public void SwitchAudio(bool enable)
    {
        var avinfo = GetSystemAVInfo();
        audioHelper.SwitchAudio((uint)Math.Floor(avinfo.timing.sampleRate), enable);
    }


    private void AudioSampleCallback(short left, short right)
    {
        _editor.Log(LogType.Debug, "LibRetro", $"Unhandled Audio sample callback: {left}, {right}");
    }

    // * One frame is defined as a sample of left and right channels, interleaved.
    // * I.e. int16_t buf[4] = { l, r, l, r }; would be 2 frames.
    private void AudioSampleBatchCallback(IntPtr data, UIntPtr frames)
    {
        audioHelper.AudioSampleIn(data, frames);
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