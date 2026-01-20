using System.Globalization;

namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.SNES;

/// <summary>
/// SNES/65816 trace parser
/// </summary>
internal class SNESTraceParser : ITraceParser
{
    public bool TryParseTraceLine(string line, out TraceEntry entry)
    {
        entry = new TraceEntry { IsValid = false };

        // Format: E=00|P=00|DB=00|D=0000|X=0000|Y=0000|S=0000|BANK:OFFSET: mnemonic operands
        if (string.IsNullOrWhiteSpace(line) || !line.Contains("|"))
            return false;

        var parts = line.Split('|');
        if (parts.Length < 7)
            return false;

        // Parse emulation mode (E)
        if (!parts[0].StartsWith("E=") || !byte.TryParse(parts[0].Substring(2), NumberStyles.HexNumber, null, out byte e))
            return false;

        // Parse processor status (P)
        if (!parts[1].StartsWith("P=") || !byte.TryParse(parts[1].Substring(2), NumberStyles.HexNumber, null, out byte p))
            return false;

        // Parse data bank (DB)
        if (!parts[2].StartsWith("DB=") || !byte.TryParse(parts[2].Substring(3), NumberStyles.HexNumber, null, out byte db))
            return false;

        // Parse direct offset (D)
        if (!parts[3].StartsWith("D=") || !ushort.TryParse(parts[3].Substring(2), NumberStyles.HexNumber, null, out ushort d))
            return false;

        // Parse index register (X)
        if (!parts[4].StartsWith("X=") || !ushort.TryParse(parts[4].Substring(2), NumberStyles.HexNumber, null, out ushort x))
            return false;

        // Parse index register (Y)
        if (!parts[5].StartsWith("Y=") || !ushort.TryParse(parts[5].Substring(2), NumberStyles.HexNumber, null, out ushort y))
            return false;

        // Parse stack pointer (S)
        if (!parts[6].StartsWith("S=") || !ushort.TryParse(parts[6].Substring(2), NumberStyles.HexNumber, null, out ushort s))
            return false;

        // Parse bank and offset
        var addressPart = parts[7].Split(' ')[0];
        var addressComponents = addressPart.Split(':');
        if (addressComponents.Length != 3 ||
            !byte.TryParse(addressComponents[0], NumberStyles.HexNumber, null, out byte bank) ||
            !ushort.TryParse(addressComponents[1], NumberStyles.HexNumber, null, out ushort offset))
            return false;

        // Convert bank:offset to SNES address
        UInt64 snesAddress = (UInt64)((bank << 16) | offset);

        // Create CPU state
        var cpuState = new SNES65816State
        {
            EmulationMode = e == 1,
            Accumulator8Bit = (p & 0x20) == 0x20,
            Index8Bit = (p & 0x10) == 0x10
        };

        // Create register state
        var registerState = new SNES65816RegisterState
        {
            DBR = db,
            D = d,
            X = x,
            Y = y,
            S = s
        };

        entry = new TraceEntry
        {
            Address = snesAddress,
            CpuState = cpuState,
            RegisterState = registerState,
            IsValid = true
        };

        return true;
    }
}