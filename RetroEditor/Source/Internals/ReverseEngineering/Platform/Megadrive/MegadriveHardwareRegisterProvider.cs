namespace RetroEditor.Source.Internals.ReverseEngineering.Platform.Megadrive;

/// <summary>
/// Megadrive/Genesis hardware register provider
/// </summary>
internal class MegadriveHardwareRegisterProvider : IHardwareSymbolsProvider
{
    public void InitializeSymbols(IRomDataParser romData)
    {
        // VDP (Video Display Processor) Registers - Data Port
        romData.AddSymbol(0xC00000, 2, "VDP_DATA");
        
        // VDP Control Port
        romData.AddSymbol(0xC00004, 2, "VDP_CTRL");
        
        // VDP HV Counter
        romData.AddSymbol(0xC00008, 2, "VDP_HV_COUNTER");
        
        // PSG (Programmable Sound Generator)
        romData.AddSymbol(0xC00011, 1, "PSG");
        
        // Z80 Bus Request
        romData.AddSymbol(0xA11100, 2, "Z80_BUS_REQ");
        
        // Z80 Reset
        romData.AddSymbol(0xA11200, 2, "Z80_RESET");
        
        // Controller Port 1 Data
        romData.AddSymbol(0xA10003, 1, "IO_DATA_1");
        
        // Controller Port 1 Control
        romData.AddSymbol(0xA10009, 1, "IO_CTRL_1");
        
        // Controller Port 2 Data
        romData.AddSymbol(0xA10005, 1, "IO_DATA_2");
        
        // Controller Port 2 Control
        romData.AddSymbol(0xA1000B, 1, "IO_CTRL_2");
        
        // Controller Port 3 Data (Expansion Port)
        romData.AddSymbol(0xA10007, 1, "IO_DATA_3");
        
        // Controller Port 3 Control (Expansion Port)
        romData.AddSymbol(0xA1000D, 1, "IO_CTRL_3");
        
        // Version Register
        romData.AddSymbol(0xA10001, 1, "VERSION");
        
        // YM2612 (FM Sound) Registers
        romData.AddSymbol(0xA04000, 1, "YM2612_A0");
        romData.AddSymbol(0xA04001, 1, "YM2612_D0");
        romData.AddSymbol(0xA04002, 1, "YM2612_A1");
        romData.AddSymbol(0xA04003, 1, "YM2612_D1");
        
        // TMSS (Trademark Security System)
        romData.AddSymbol(0xA14000, 4, "TMSS");
        
        // Z80 RAM (accessed from 68000)
        // Range: 0xA00000 - 0xA01FFF
        
        // 68000 RAM
        // Range: 0xFF0000 - 0xFFFFFF (64KB)
    }
}
