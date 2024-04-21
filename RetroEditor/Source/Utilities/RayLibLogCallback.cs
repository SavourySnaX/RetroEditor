// Adapted mfkl and jeremyVignelles work on libvlcsharp for marshalling va_list arguments
// For more information see the dotnet issue: https://github.com/dotnet/runtime/issues/9316
// Example of va_list interop: https://github.com/jeremyVignelles/va-list-interop-demo
using System;
using System.Runtime.InteropServices;

namespace Raylib_cs;

internal readonly struct Native
{
    internal const string Msvcrt = "msvcrt";
    internal const string Libc = "libc";
    internal const string LibSystem = "libSystem";

    [DllImport(LibSystem, EntryPoint = "vasprintf", CallingConvention = CallingConvention.Cdecl)]
    public static extern int VasPrintfApple(ref IntPtr buffer, IntPtr format, IntPtr args);

    [DllImport(Libc, EntryPoint = "vsprintf", CallingConvention = CallingConvention.Cdecl)]
    public static extern int VsPrintfLinux(IntPtr buffer, IntPtr format, IntPtr args);

    [DllImport(Msvcrt, EntryPoint = "vsprintf", CallingConvention = CallingConvention.Cdecl)]
    public static extern int VsPrintfWindows(IntPtr buffer, IntPtr format, IntPtr args);

    [DllImport(Libc, EntryPoint = "vsnprintf", CallingConvention = CallingConvention.Cdecl)]
    public static extern int VsnPrintfLinux(IntPtr buffer, UIntPtr size, IntPtr format, IntPtr args);

    [DllImport(Msvcrt, EntryPoint = "vsnprintf", CallingConvention = CallingConvention.Cdecl)]
    public static extern int VsnPrintfWindows(IntPtr buffer, UIntPtr size, IntPtr format, IntPtr args);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct VaListLinuxX64
{
    uint _gpOffset;
    uint _fpOffset;
    IntPtr _overflowArgArea;
    IntPtr _regSaveArea;
}

/// <summary>
/// Logging workaround for formatting strings from native code
/// </summary>
public static unsafe class RayLibLoggingWrapper
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe void Log(int msgType, sbyte* text, sbyte* args)
    {
        var message = GetLogMessage(new IntPtr(text), new IntPtr(args));
        switch (msgType)
        {
            case 0:
            case 1:
            case 2:
                Editor.Log(LogType.Debug, "RayLib", message);
                break;
            case 3:
                Editor.Log(LogType.Info, "RayLib", message);
                break;
            case 4:
                Editor.Log(LogType.Warning, "RayLib", message);
                break;
            default:
                Editor.Log(LogType.Error, "RayLib", message);
                break;
        }
    }

    public static string GetLogMessage(IntPtr format, IntPtr args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return AppleLogCallback(format, args);
        }

        // Special marshalling is needed on Linux desktop 64 bits.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && IntPtr.Size == 8)
        {
            return LinuxX64LogCallback(format, args);
        }

        var byteLength = VsnPrintf(IntPtr.Zero, UIntPtr.Zero, format, args) + 1;
        if (byteLength <= 1)
        {
            return string.Empty;
        }

        var buffer = Marshal.AllocHGlobal(byteLength);
        VsPrintf(buffer, format, args);

        string result = Marshal.PtrToStringUTF8(buffer) ?? "";
        Marshal.FreeHGlobal(buffer);

        return result;
    }

    static string AppleLogCallback(IntPtr format, IntPtr args)
    {
        IntPtr buffer = IntPtr.Zero;
        try
        {
            var count = Native.VasPrintfApple(ref buffer, format, args);
            if (count == -1)
            {
                return string.Empty;
            }
            return Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    static string LinuxX64LogCallback(IntPtr format, IntPtr args)
    {
        // The args pointer cannot be reused between two calls. We need to make a copy of the underlying structure.
        var listStructure = Marshal.PtrToStructure<VaListLinuxX64>(args);
        IntPtr listPointer = IntPtr.Zero;
        int byteLength = 0;
        string result = "";

        // Get length of args
        listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(listStructure));
        Marshal.StructureToPtr(listStructure, listPointer, false);
        byteLength = Native.VsnPrintfLinux(IntPtr.Zero, UIntPtr.Zero, format, listPointer) + 1;

        // Allocate buffer for result
        Marshal.StructureToPtr(listStructure, listPointer, false);

        IntPtr utf8Buffer = IntPtr.Zero;
        utf8Buffer = Marshal.AllocHGlobal(byteLength);

        // Print result into buffer
        Native.VsPrintfLinux(utf8Buffer, format, listPointer);
        result = Marshal.PtrToStringUTF8(utf8Buffer) ?? "";

        Marshal.FreeHGlobal(listPointer);
        Marshal.FreeHGlobal(utf8Buffer);

        return result;
    }

    // https://github.com/dotnet/runtime/issues/51052
    static int VsnPrintf(IntPtr buffer, UIntPtr size, IntPtr format, IntPtr args)
    {
        var os = Environment.OSVersion;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Native.VsnPrintfWindows(buffer, size, format, args);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Native.VsnPrintfLinux(buffer, size, format, args);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
        {
            return Native.VsPrintfLinux(buffer, format, args);
        }
        return -1;
    }

    // https://github.com/dotnet/runtime/issues/51052
    static int VsPrintf(IntPtr buffer, IntPtr format, IntPtr args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Native.VsPrintfWindows(buffer, format, args);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Native.VsPrintfLinux(buffer, format, args);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
        {
            return Native.VsPrintfLinux(buffer, format, args);
        }
        return -1;
    }
}