using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

public static class LibRetroPluginFactory
{
    public static LibRetroPlugin? Create(string path)
    {
        try 
        {
            return new LibRetroPlugin(path);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load plugin {path}: {e.Message}");
            return null;
        }
    }

}

public class LibRetroPlugin : IDisposable
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


    public LibRetroPlugin(string path)
    {
        disableVideo = false;
        frameBuffer = Array.Empty<byte>();
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
        nativeGetMemorySize = Marshal.GetDelegateForFunctionPointer<retro_get_memory_size>(NativeLibrary.GetExport(libraryHandle, "retro_get_memory_size"));
        nativeGetMemoryData = Marshal.GetDelegateForFunctionPointer<retro_get_memory_data>(NativeLibrary.GetExport(libraryHandle, "retro_get_memory_data"));
        nativeSerializeSize = Marshal.GetDelegateForFunctionPointer<retro_serialize_size>(NativeLibrary.GetExport(libraryHandle, "retro_serialize_size"));
        nativeSerialize = Marshal.GetDelegateForFunctionPointer<retro_serialize>(NativeLibrary.GetExport(libraryHandle, "retro_serialize"));
        nativeUnserialize = Marshal.GetDelegateForFunctionPointer<retro_unserialize>(NativeLibrary.GetExport(libraryHandle, "retro_unserialize"));
        nativeReset = Marshal.GetDelegateForFunctionPointer<retro_reset>(NativeLibrary.GetExport(libraryHandle, "retro_reset"));
        nativeRun = Marshal.GetDelegateForFunctionPointer<retro_run>(NativeLibrary.GetExport(libraryHandle, "retro_run"));
        nativeDeinit = Marshal.GetDelegateForFunctionPointer<retro_deinit>(NativeLibrary.GetExport(libraryHandle, "retro_deinit"));
        SetEnvironment();
    }

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
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var info = new retro_game_info
        {
            path = Marshal.StringToHGlobalAnsi(path),
            data = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0),
            size = (UIntPtr)data.Length,
            meta = IntPtr.Zero
        };
        unsafe
        {
            nativeLoadGame(&info);
        }
        dataHandle.Free();

        // Should be able to allocate our framebuffer here
        var aVInfo=GetSystemAVInfo();
        frameBuffer = new byte[aVInfo.geometry.baseWidth * aVInfo.geometry.baseHeight * 4];
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

    public byte[] GetMemory(UInt64 start, UInt64 size)
    {
        // Gets memory from memory maps
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

    public void SetMemory(UInt64 start, byte[] data)
    {
        // Sets memory in memory maps
        var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
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

    public void Reset()
    {
        nativeReset.Invoke();
    }

    public void Run()
    {
        nativeRun.Invoke();
    }

    public void AutoLoad(Func<bool> condition)
    {
        disableVideo = true;
        while (!condition())
        {
            Run();
        }
        disableVideo = false;
    }

    public byte[] GetFrameBuffer()
    {
        return frameBuffer;
    }

    public void Dispose()
    {
        NativeLibrary.Free(libraryHandle);
    }

    private bool disableVideo;

    private PixelFormat pixelFormat;
    private IntPtr libraryHandle;
    private byte[] frameBuffer;
    private MemoryMap[] memoryMaps;

    private delegate byte retro_environment_t(uint cmd, IntPtr data);
    private delegate void retro_audio_sample_t(short left, short right);
    private delegate void retro_audio_sample_batch_t(IntPtr data, UIntPtr frames);
    private delegate void retro_input_poll_t();
    private delegate short retro_input_state_t(uint port, uint device, uint index, uint id);
    private delegate void retro_video_refresh_t(IntPtr data, uint width, uint height, UIntPtr pitch);

    private retro_environment_t environmentCallback;                // Prevent collection of delegate
    private retro_audio_sample_t audioSampleCallback;               // Prevent collection of delegate
    private retro_audio_sample_batch_t audioSampleBatchCallback;    // Prevent collection of delegate
    private retro_input_poll_t inputPollCallback;                   // Prevent collection of delegate  
    private retro_input_state_t inputStateCallback;                 // Prevent collection of delegate
    private retro_video_refresh_t videoRefreshCallback;             // Prevent collection of delegate

    private delegate void Log(int level, IntPtr fmt);

    private Log logCallback;                                       // Prevent collection of delegate

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
        ENVIRONMENT_SET_PIXEL_FORMAT = 10,
        ENVIRONMENT_GET_VARIABLE_UPDATE = 17,
        ENVIRONMENT_GET_LOG_INTERFACE = 27,
        ENVIRONMENT_SET_MEMORY_MAPS = 36,
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

    private struct retro_memory_descriptor
    {
        public UInt64 flags;
        public IntPtr ptr;
        public IntPtr offset;
        public IntPtr start;
        public IntPtr select;
        public IntPtr disconnect;
        public IntPtr len;
        public IntPtr addressSpace;
    }

    private struct retro_log_callback
    {
        public Log log;
    }

    private struct retro_memory_map
    {
        public IntPtr descriptors;
        public uint num_descriptors;
    }

    private void LogCallback(int level, IntPtr fmt)
    {
        var fmtString = Marshal.PtrToStringAnsi(fmt);
        System.Console.WriteLine($"Log callback: {level}, {fmtString}");
        // TODO implement args expansion
    }

    private byte EnvironmentCallback(uint cmd, IntPtr data)
    {
        var experimental = (cmd & 0x10000) == 0x10000;
        var frontendPrivate = (cmd & 0x20000) == 0x20000;
        cmd &= 0xFFFF;
        var command = (EnvironmentCommand)cmd;
        switch (command)
        {
            case EnvironmentCommand.ENVIRONMENT_SET_PIXEL_FORMAT:
                {
                    pixelFormat = (PixelFormat)Marshal.ReadInt32(data);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_GET_VARIABLE_UPDATE:
                {
                    return 0;   // no variables updated since last run
                }
            case EnvironmentCommand.ENVIRONMENT_GET_LOG_INTERFACE:
                {
                    logCallback = LogCallback;
                    var logInterface = new retro_log_callback
                    {
                        log = logCallback
                    };
                    Marshal.StructureToPtr(logInterface, data, false);
                    return 1;
                }
            case EnvironmentCommand.ENVIRONMENT_SET_MEMORY_MAPS:
                {
                    var memoryMap = Marshal.PtrToStructure<retro_memory_map>(data);

                    memoryMaps = new MemoryMap[memoryMap.num_descriptors];
                    for (int a=0;a<memoryMap.num_descriptors;a++)
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
                            addressSpace = Marshal.PtrToStringAnsi(descriptor.addressSpace)
                        };
                    }
                    return 0;
                }
            default:
                {
                    System.Console.WriteLine($"Environment callback :  {cmd} {(experimental?"Experimental":"")} {(frontendPrivate?"Private":"")}");
                }
                return 0;
        }
    }


    private void AudioSampleCallback(short left, short right)
    {
        //System.Console.WriteLine($"Audio sample callback: {left}, {right}");
    }

    private void AudioSampleBatchCallback(IntPtr data, UIntPtr frames)
    {
        //System.Console.WriteLine($"Audio sample batch callback: {frames}");
    }

    private void InputPollCallback()
    {
        //System.Console.WriteLine($"Input poll callback");
    }

    private short InputStateCallback(uint port, uint device, uint index, uint id)
    {
        //System.Console.WriteLine($"Input state callback: {port}, {device}, {index}, {id}");
        return 0;
    }

    private void VideoRefreshCallback(IntPtr data, uint width, uint height, UIntPtr pitch)
    {
        //System.Console.WriteLine($"Video refresh callback: {width}, {height}, {pitch}");
        if (disableVideo)
        {
            return;
        }

        // Perform pixel format conversion here
        switch (pixelFormat)
        {
            case PixelFormat.RGB565:
            {
                unsafe 
                {
                    var src = (ushort*)data;
                    var frameBufferPos = 0;
                    var srcNextLine=(pitch.ToUInt32() / 2) - width;
                    for (int y=0;y<height;y++)
                    {
                        for (int x=0;x<width;x++)
                        {
                            var pixel = *src++;
                            var b = (byte)((pixel & 0xF800) >> 11);
                            var g = (byte)((pixel & 0x07E0) >> 5);
                            var r = (byte)((pixel & 0x001F) >> 0);
                            frameBuffer[frameBufferPos++] = (byte)((r << 3) | (r >> 2));
                            frameBuffer[frameBufferPos++] = (byte)((g << 2) | (g >> 4));
                            frameBuffer[frameBufferPos++] = (byte)((b << 3) | (b >> 2));
                            frameBuffer[frameBufferPos++] = 255;
                        }
                        src+=srcNextLine;
                    }
                }
                break;
            }
            default:
                throw new Exception($"TODO implement pixel format conversion {pixelFormat}");
        }
    }

    private void SetEnvironment()
    {
        // Record delegate to prevent garbage collection
        environmentCallback=EnvironmentCallback;
        audioSampleCallback=AudioSampleCallback;
        audioSampleBatchCallback=AudioSampleBatchCallback;
        inputPollCallback=InputPollCallback;
        inputStateCallback=InputStateCallback;
        videoRefreshCallback=VideoRefreshCallback;

        nativeSetEnvironment.Invoke(environmentCallback);
        nativeSetAudioSample.Invoke(audioSampleCallback);
        nativeSetAudioSampleBatch.Invoke(audioSampleBatchCallback);
        nativeSetInputPoll.Invoke(inputPollCallback);
        nativeSetInputState.Invoke(inputStateCallback);
        nativeSetVideoRefresh.Invoke(videoRefreshCallback);
    }

}