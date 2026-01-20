using System.Globalization;
using RetroEditor.Plugins;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.Megadrive;

/// <summary>
/// Megadrive/Genesis 68000 trace parser
/// </summary>
internal class MegadriveTraceParser : ITraceParser
{
    public bool TryParseTraceLine(string line, out TraceEntry entry)
    {
        entry = new TraceEntry { IsValid = false };

        // Format: D0=00000000|D1=00000000|...|A7=00000000|SR=2700|ADDRESS: instruction
        if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
            return false;

        var parts = line.Split('|');
        if (parts.Length < 17) // 8 data + 8 address + SR
            return false;

        // Parse data registers (D0-D7)
        var dataRegisters = new UInt32[8];
        for (int i = 0; i < 8; i++)
        {
            if (!parts[i].StartsWith($"D{i}=") || 
                !UInt32.TryParse(parts[i].Substring(3), NumberStyles.HexNumber, null, out dataRegisters[i]))
                return false;
        }

        // Parse address registers (A0-A7)
        var addressRegisters = new UInt32[8];
        for (int i = 0; i < 8; i++)
        {
            if (!parts[i + 8].StartsWith($"A{i}=") || 
                !UInt32.TryParse(parts[i + 8].Substring(3), NumberStyles.HexNumber, null, out addressRegisters[i]))
                return false;
        }

        // Parse status register (SR)
        if (!parts[16].StartsWith("SR=") || 
            !UInt16.TryParse(parts[16].Substring(3), NumberStyles.HexNumber, null, out UInt16 sr))
            return false;

        // Parse address from the remaining part
        // Expected format after SR: "ADDRESS: instruction"
        var addressPart = parts[16].Split(':');
        if (addressPart.Length < 2)
            return false;

        var addrStr = addressPart[0].Replace("SR=", "").Trim();
        // Try to extract address from the last part
        var lastPart = parts[parts.Length - 1];
        var addressMatch = lastPart.Split(':');
        
        UInt64 address = 0;
        if (addressMatch.Length >= 2)
        {
            // Try to parse hex address before the colon
            var hexAddr = addressMatch[0].Trim();
            if (!UInt64.TryParse(hexAddr, NumberStyles.HexNumber, null, out address))
                return false;
        }
        else
        {
            return false;
        }

        // Create CPU state
        var cpuState = new Megadrive68000State
        {
            SupervisorMode = (sr & 0x2000) == 0x2000
        };

        // Create register state
        var registerState = new M68000RegisterState
        {
            DataRegisters = dataRegisters,
            AddressRegisters = addressRegisters,
            SR = sr
        };

        entry = new TraceEntry
        {
            Address = address,
            CpuState = cpuState,
            RegisterState = registerState,
            IsValid = true
        };

        return true;
    }
}
