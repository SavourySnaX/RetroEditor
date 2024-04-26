//
// Needs custom build of MAME with my remote debugger plugin - see SavourySnax/MAME (branch: rdebug) (b3b16b87c9196865962b79dad2a8cd5ab9cba12a)
// launch with : mame spectrum -dump "rollercoaster.z80" -uimodekey DEL -debugger remote -debug -sound none -video none
// only tested with above command line - limitted to 64k transfer at once
using System.Net.Sockets;

internal class MameRemoteClient
{
    TcpClient gdb;
    BinaryReader? reader;
    BinaryWriter? writer;

    public MameRemoteClient()
    {
        gdb = new TcpClient();
    }

    public bool Connect()
    {
        gdb.Connect("localhost", 23946);
        var stream = gdb.GetStream();
        reader = new BinaryReader(stream);
        writer = new BinaryWriter(stream);
        return true;
    }

    public bool IsRunning()
    {
        Send($"?");
        return Recieve_Binary()[0]=='Y';
    }

    public string[] SendCommand(string command)
    {
        Send($"x{command}");
        return Recieve();
    }

    public byte[] RequestMemory(int address, int size)
    {
        Send($"m{address:X},{size:X}");
        return Recieve_Binary();
    }

    public byte[] RequestState(int x, int y, int w, int h)
    {
        Send($"vs{x},{y},{w},{h}");
        return Recieve_Binary();
    }
    
    public byte[] RequestDisasm(int x, int y, int w, int h)
    {
        Send($"vd{x},{y},{w},{h}");
        return Recieve_Binary();
    }

    public byte[] SendMemory(int address, byte[] data)
    {
        Send($"p{address:X},{data.Length:X}");
        var checkSize=RecieveSize();
        if (checkSize==data.Length)
        {
            writer?.Write(data);
            return Recieve_Binary();
        }
        return Array.Empty<byte>();
    }


    public void Disconnect()
    {
        Send("");   // disconnect

        reader?.Close();
        writer?.Close();
        gdb.Close();
    }

    private void Send(string command)
    {
        if (reader == null || writer == null)
            return;
        byte b = (byte)((command.Length >> 8) & 0xFF);
        writer.Write(b);
        b = (byte)(command.Length & 0xFF);
        writer.Write(b);
        var bytes= System.Text.Encoding.ASCII.GetBytes(command);
        writer.Write(bytes);
    }

    private int RecieveSize()
    {
        if (reader == null)
            return 0;
        var b = reader.ReadBytes(2);
        return (b[0] << 8) | b[1];
    }

    private string RecieveString()
    {
        if (reader==null)
            return "";
        var size = RecieveSize();
        if (size == 0)
            return "";
        var bytes = reader.ReadBytes(size);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }

    private string[] Recieve()
    {
        if (reader == null)
            return Array.Empty<string>();
        var size = RecieveSize();
        if (size == 0)
            return Array.Empty<string>();
        var s = new String[size];
        for (int a=0;a<size;a++)
        {
            s[a] = RecieveString();
        }
        return s;
    }

    private byte[] Recieve_Binary()
    {
        if (reader == null)
            return Array.Empty<byte>();
        var size = RecieveSize();
        if (size == 0)
            return Array.Empty<byte>();
        var bytes = reader.ReadBytes(size);
        return bytes;
    }
}
