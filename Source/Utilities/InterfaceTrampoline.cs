
using System.Runtime.InteropServices;

public static class InterfaceTrampoline
{
    public delegate nint Initialise(nint instance,int numParams, nint method);
    public delegate nint Printf(nint method);

    private static nint GetMethod(string method)
    {
        string instanceTrampolinePath = Path.Combine(Directory.GetCurrentDirectory(), "Native", "InstanceTrampoline", "build");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            instanceTrampolinePath = Path.Combine(instanceTrampolinePath, "Debug", "InstanceTrampoline.dll");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            instanceTrampolinePath = Path.Combine(instanceTrampolinePath, "libInstanceTrampoline.so");
        }
        else
        {
            throw new Exception("Unsupported platform");
        }
        var lib = NativeLibrary.Load(instanceTrampolinePath);
        return NativeLibrary.GetExport(lib, method);
    }

    public static Initialise GetInitialise()
    {
        return Marshal.GetDelegateForFunctionPointer<Initialise>(GetMethod("allocate_trampoline"));
    }

    public static Printf GetPrintf()
    {
        return Marshal.GetDelegateForFunctionPointer<Printf>(GetMethod("allocate_printer"));
    }

}