# Developer Guide

_under construction_

The [API](xref:RetroEditor.Plugins.IRetroPlugin) Documentation has some information.

## Creating a plugin from scratch (WIP)

### Overview

For this tutorial, we will take [Manic Miner For Gameboy Colour](https://hh.gbdev.io/game/manic-miner) and attempt to make an editor plugin for the rom. This is a homebrew title, and at the time of writing, is on a platform that the editor does not yet have a system plugin or a game plugin for. 

### Initial Setup / Pre-requisites
First of all, since we are going to be doing some reverse engineering, we will need to enable developer mode. If the editor has not been run before,
start it, and then close it again. This will save a default __settings.json__, which we can the edit.

Open the __settings.json__ and locate the line :

```
  "DeveloperMode": false
```

and replace the __false__ with __true__, which will enable some extra menu options. 

For reverse engineering we will use the builtin libretro_Mame debugger, alongside [Ghidra](https://ghidra-sre.org/) for static analysis and recording discoveries, so please make sure to install it.

The editor will also have created a folder named __Temp__, libretro_Mame requires __gbc_boot.1__ and __gbc_boot.2__ which should both be placed into __Temp/mame/roms/gbcolor/__ folder. I won't link to them here, but they should be easy enough to find online.

Download the [Manic Miner]() rom and place it into a folder named __gbcolor__ (its location doesn't matter, but the folder name is very important).

### Creating a new System (Gameboy Colour)

At present, the editor does not allow hot reloading of systems, so make sure the editor is closed before we begin.

Open the Plugins/Plugins.csproj in your C# Editor of choice, I used Visual Studio Code for this tutorial. Create a new file inside __Source__/__RomPlugins__ named __GameBoyColour.cs__. 

Copy and paste the following code block into the file :

```cs
using RetroEditor.plugins;

class GameBoyColour : ISystemPlugin
{
    // Name used to identify the system by plugins
    public static Name => "GameBoyColour";

    // libretro plugin name responsible for running gameboy colour roms
    public string LibRetroPluginName => "gambatte_libretro";

    // Memory endian of the system
    public MemoryEndian Endian => MemoryEndian.Little;

    // This is a rom based system, so requires reload when changing
    //things
    public bool RequiresReload => true;
}
```

This should be simple enough to understand and at this point, we can save and run the editor. You should now see a __GameBoyColour__ option under the __Developer__->__Plugin Player__-> menu, clicking it and then selecting the unzipped Manic Miner rom should cause a player window to open with the rom running inside it. If not check the logs for compilation errors or other libretro errors.

### Starting to make our plugin

Before we even need to think about reverse engineering, we can start to knock up our initial plugin.

The interface we need to implement is [IRetroPlugin](xref:RetroEditor.Plugins.IRetroPlugin). Before we do this though, we should calculate the checksum of the rom image (this is done, so we the editor can identify which plugin handles which rom). If you have access to __md5sum__ then you can simply use it to calculate the checksum of the unzipped __Manic Miner__ rom. In case you don't, I have done this for you, and it is __b13061a4a1a84ef2edb4c9d47f794093__.

So we can now implement the minimum interface we need to have the plugin startup when you create a project using the above game. So if you haven't done so, close the editor and switch back to your development IDE. Create a new folder in __Source__/__GamePlugins__/ called __ManicMinerGBC__. Then create a new file inside that new folder called __ManicMinerGBC.cs__ and paste the below code into it :


```cs
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

class ManicMinerGBC : IRetroPlugin
{
    // This is the name of the plugin that will be displayed in the UI
    public static string Name => "Manic Miner GameBoy Colour";

    // This is the name of the system (that we created above) used to load this game
    public string RomPluginName => "GameBoyColour";

    // Cartridge based games don't typically require an auto-loader (unlike disc/tape based games)
    public bool RequiresAutoLoad => false;

    // We can stub this function, it's not required for this plugin
    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        return false;
    }

    // This function is used to determine if the plugin can handle the given file
    // So we check if the MD5 hash matches the one for Manic Miner (b13061a4a1a84ef2edb4c9d47f794093)
    public bool CanHandle(string path)
    {
        var manicMinerMD5 = new byte[] { 0xb1, 0x30, 0x61, 0xa4, 0xa1, 0xa8, 0x4e, 0xf2, 0xed, 0xb4, 0xc9, 0xd4, 0x7f, 0x79, 0x40, 0x93 };
        
        if (!File.Exists(path))
        {
            return false;
        }
        var md5 = MD5.Create().ComputeHash(File.ReadAllBytes(path));

        if (manicMinerMD5.SequenceEqual(md5))
        {
            return true;
        }
        return false;
    }

    // We dont yet support exporting saves for Manic Miner GBC, so just throw
    public ISave Export(IMemoryAccess romAcess)
    {
        throw new System.NotImplementedException("Manic Miner GBC does not support exporting saves, yet");
    }

    // We can leave this empty for now
    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
    }
}
```

At this point, we can restart the editor, and __File__->__Create New Project__ selecting the manic miner rom. The Plugin should automatically be selected in the combo box above, and you can click __Create Project__.

You will now see a player window with the game starting up. In addition, the __Window__ menu will now have an item for our newly created project, but the only items available are __Open Player__ and __Reload Plugin__. At this point we are in a position where we should be able to iterate on the plugin, without needing to restart the Editor.

### Ghidra Setup

Ghidra is a great tool for doing reverse engineering to figure out data formats etc, there is a [plugin (GhidraBoy)](https://github.com/Gekkio/GhidraBoy/releases/download/20231227_ghidra_11.0/ghidra_11.0_PUBLIC_20231227_GhidraBoy.zip) available for [Ghidra 11](https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_11.0.3_build/ghidra_11.0.3_PUBLIC_20240410.zip) so grab both.

Unzip Ghidra (but not the GhidraBoy extension), and then launch Ghidra (This is not a Ghidra tutorial, but I will try to cover aspects as needed). Once open, __File__->__Install Extension__ , click the + add locate the ghidraboy zip. After installation, restart Ghidra.

Create a new project, and then press __I__ to import, selecting the manicminer image. GhidraBoy should configure everything correctly, so click __OK__, and __OK__ once again on the summary dialog.

Now double click on the imported file, clicking __YES__ to the Analyze? dialog and then click __Analyze__. This will attempt to automatically disassemble the rom, giving us an initial starting point.

### Initial Goal

Our initial goal is to try to skip any logos, intros, start menus, so that our player is on the first level when ever we open the plugin. This will give us a good basis for exploring patching, and also lead into figuring out where the level data is stored.

Open the retro editor if it isn't already. And open __Developer__->__LibMame Debugger__->__Launch__ selecting the manic miner rom, if it fails, its probably because you haven't put the required gb roms in the right place. _Note when the libMame debugger is launched, the execution starts paused_. You should open the __Developer__->__LibMame Debugger__->__Cpu State__, __Disassembly__, __Memory__ and __Console__ views as we will need them going forward.

So at this point, we should have Ghidra and the Retro Editor open, with Manic Miner paused in the debugger. What we are now going to do, is to try to figure out how to skip the initial screen presented before the menu. Since we don't know anything about this rom, we use a combination of static (Ghidra) and dynamic (The debugger windows in Retro Editor) in order to figure out what we need. You probably will want to make notes too, but for this tutorial I will mostly guiding you explicitly (again, this isn't a Ghidra or reverse engineering tutorial per se).

Another handy resource would be documentation about the Game Boy system itself, the [Pan Docs](https://gbdev.io/pandocs/Specifications.html) will suffice for this.

Ghidra should have opened with the entry() function at address 0x100 within view, if not, you can jump to the entry() via the Symbol Tree window, expand Functions and entry should be at the top. If you are observant you will notice that the debugger (Retro Editor) seems to be showing us stopped at address 0x0000. This is becaause the debugger stops at the first executable point, which in the case of the GameBoy happens to be inside the bios. We don't care about the bios, so using the console add a breakpoint at address 100 (__bp 100__) and then resume execution (__go__) until we stop at the same address.

```
bp 100
go
```

The bios should flash up the colour Gameboy logo and then the debugger should stop at the entry function we could see in Ghidra.

I usually begin by stepping forward (__F7__ when the disassembly view is focused), and when i reach a __CALL__ instruction, I step over it (__F8__). If the debugger pauses, then i continue, if the game (or intros or main menu) starts running. Then I reset and start again, but step into the call this time. This attempts to discover if the games intro/start menu are seperated from the level logic (e.g. handled in a custom function). In the case of Manic Miner, we get lucky.

From the entry point, we meet a bunch of __CALL__ instructions, but the one located at __0x01D7__ does not return. So we start again, this time we step  (__F7__) into the call at __0x01D7__, and then continue forward, this time when reach __0x2981__ and step over (__F8__), the intro runs, and then the main menu is shown, but when we press start, we find ourselves back in the debugger. So, if we were to knock out the call at __0x2981__ then in theory in our plugin, the game will start on the first level straight away. 

Load your saved Manic Miner project into the editor. Now in your editor modify :

```cs
    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
    }
```

to make it look like this :

```cs
    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        // Patch out the main menu/intro ()
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x2981, new byte[] { 0x00, 0x00, 0x00 });
    }
```

save the file, then go back to the editor and select __Window__->__<project_name>__->__Reload Plugin__. Now the player window should re-open and gameplay should start at level 1 without showing the intro/menu.

### Adding a level select cheat

As great as starting at level 1 is, when editing it is helpful if the editor can skip to the level that is being worked on. In order to do this, we need to figure out where in the code the levels are setup, fortunately this will also help us figure out where the level data is for an editor later.

According to the [Pan Docs Memory Map](https://gbdev.io/pandocs/Memory_Map.html) video RAM can be directly accessed via 0x8000 to 0x9FFF, there is also DMA, but for this particular game, we don't need to worry about that. Now the GameBoy Colour splits it video memory into [Tile Data](https://gbdev.io/pandocs/Tile_Data.html) (0x8000-0x97FF) and [Tile Maps](https://gbdev.io/pandocs/Tile_Maps.html) (0x9800-0x9FFF). 

In order to figure out where the game data is stored, we could try staring at the Ghidra Code, or maybe looking at the raw rom data, but a smarter solution would be to allow the debugger to find it for us. The libretro_MAME debugger, allows us to stop execution when memory addresses are accessed. Since we know that the tile maps are written to address 0x9800-0x9FFF, we should be able to use this knowledge to isolate the location of the levels.

To begin, lets reset the rom we are debugging, and setup an initial watchpoint (a breakpoint that stops on memory access) on the first tile address 0x9800 (length 1), setting it to stop on write (w). In the debugger console execute the following :

```
softreset
wpset 9800,1,w
```

Now from the disassembly view, press __F5__ to resume. The debugger will stop on the first write to the tilemap memory, there will be a number of these, firstly by the bios, then by initialisation in the rom, then by the main menu, and finally by the level setup. Keep resuming, until the main menu is reached, at the main menu press __M__ on the MAME RETRO player window (to start the game). The next time the debugger stops, in theory it is setting up the level. If you wish to see what the tilemap looks like, you can use the memory view and select __CGB PPU/:ppu/0/m_vram__ in the source view and __1800__ in the expression. Since we have stopped at the first tile being written, address 1800 will contain a __02__ in this case. If we resume, then we can see the whole tilemap in the memory view, if you do this, just repeat the steps from softreset (but don't set another watchpoint) in order to continue with the tutorial below. Assuming you did resume, lets try to see if the tilemap matches the level we are looking at in the player. The Tilemap is 32 bytes wide, each byte representing a tile number to use for that 8x8 block of the image. So we can see 1800 holds a __02__, and on the screen the top left tile is a brick pattern. We then see a number of __00__'s, which seem to represent blank space, then an __08__ which seems to be a key, and then a blank followed by a __06__ a stalagtite like formation. This all seems to line up with the screen, feel free to try lining up the other items, then softreset etc, and resume the tutorial below.

Ok, at this point, we have stopped just after writing the first tile. We can see in the disassembly view that the next instruction is at __716C__, and the previous instruction __ld (de),a__ must have written the memory. In the CPU State view, register __A__ contains __02__ and registers __D__&__E__ contain the values __98__&__00__ respectively. Interestingly, looking up, we can see that a __ld a,$02__ was used to place the value __02__ into A, which is odd (if the rom data was just copied to the screen, this wouldn't be the pattern). This means we cannot directly see where the rom data is being read, fortunately, all is not lost. Another handy trick we can use, is to generate a trace of the execution of the processor and record the values in the registers all to a file. We are looking for a pattern or sequence of memory accessing, that would betray the rom location of the tile data. 

Lets trace writes to the tilemap for the first line, in order to do that, we need to add another watchpoint to stop at the 32nd write, I clear the old watchpoint here, just to avoid stopping again before we hit the added watchpoint, the reason for the second stop, is the CGB has an alternate bank of memory at the same location, which holds the attribute info for a tile (palette colours etc) :

```
wpclear
wpset 9820,1,w
```

Now setup the trace, to log to a file named tilemap.txt :

```
trace tilemap.txt,,,{tracelog "A=%02X BC=%02X%02X DE=%02X%02X HL=%02X%02X  ",a,b,c,d,e,h,l}
```

The above command will pair up the BC,DE,HL registers (because we are looking for an address, and on the GameBoy register pairs are used for that kind of access). Press __F5__, and once the debugger stops again, enter the below to stop the trace :

```
trace off
```

Now, lets open the file we just created in a text editor (you can use notepad if you don't have a favourite), the file should be in the root directory of the Editor application. Search for the address we saw writing to memory __716B__, there will only be two (this is because there are only 2 bricks drawn between the first and 32nd - one at the end of the first line, and the other other at the start of the second). If you look up from one of these you should be able to find a __jp (hl)__ at __70F2__. This implies there is some sort of jump table going on when drawing out the screen. If we search for __70F2__ you will discover there are 32 of these. Which lines up with drawing 1 tile to each location between __9801__ and __9820__ inclusive. So now we need to look at how the __hl__ value is derived, since presumably __hl__ controls what is drawn for each tile in the screen. 

Looking up a little from __70F2__, there is a __ld hl,$7DF3__ at __70E9__, so lets look at the code between these two locations :

```asm
ld hl,$7DF3             ; address of 2 byte wide table.. (jump table)
add hl,de               ; de must contain the offset into the table
ld d,h                  ; save the calculated jump table address to de
ld e,l
ld a,(hl+)              ; put the value of the entry in the jump table into hl
ld h,(hl)
ld l,a
jp (hl)                 ; jump to the location in hl
```

Address __7DF3__ contains a table of addresses, we can use Ghidra to examine this table, and the code reachable from this table. There is however a snag, the gameboy memory is banked, addresses from __4000__ to __7FFF__ are banked rom, meaning the 16k of memory in that area can be pointed to any of the 256k of rom data in the cartridge (in the case of manicminer). There is a special hardware register inside a chip (memory controller) inside the cartridge that controls which 16k section of the cartridge is visible to the cpu in addresses __4000__ to __7FFF__. We need to know which 16k block is visible at the point of this routine, in order to locate the correct place in Ghidra. You can use the memory view and change the source to __Game Boy MBC1 Cartridge/:cartslot:rom_mbc1/0/m_bank_sel_rom__ in order to figure out which bank is active, the first four hexdigits are for the addresses __0000__ to __3FFF__, the second four are what we are interested in, these are used for the switchable bank. You should see the value __0004__ here. This means it currently points to the 5th 16k bank (0,1,2,3,4), in Ghidra (thanks to the Gameboy plugin), the banks are already setup, so if you press __g__ (goto address) and choose the entry from the pop up in rom4:7df3, it should take you to the correct address. At present, Ghidra does not know what this data is, so we need to tell it. While the cursor is placed on the rom4:7df3 line (on the ??'s), press __p__ (pointer). This will convert the data into a pointer (which is what our jump table contains), looking at the data, I can see what looks like 9 entries, (the addresses seem to be related), so in addition press __[__ and enter __9__, to  create an array of 9 entries. You can press __enter__ on one of the entries to jump to the address, and __alt+left cursor__ to go back to the table. If Ghidra hasn't realised there is supposed to be code at a location, pressing __c__ will get Ghidra to disassemble the code.

For now, we don't really need to use Ghidra, we can go back to the text file, and try to figure out where the __de__ value is computed. Looking above the code we examined last time, there is 

```asm
A=00 BC=D812 DE=DA00 HL=0000  708D: ld   hl,sp+$44
A=00 BC=D812 DE=DA00 HL=DFE3  708F: ld   a,(hl+)
A=13 BC=D812 DE=DA00 HL=DFE4  7090: ld   b,(hl)
A=13 BC=D812 DE=DA00 HL=DFE4  7091: ld   c,a
A=13 BC=D813 DE=DA00 HL=DFE4  7092: ld   a,(bc)
A=00 BC=D813 DE=DA00 HL=DFE4  7093: ld   c,a
A=00 BC=D800 DE=DA00 HL=DFE4  7094: cp   e
A=00 BC=D800 DE=DA00 HL=DFE4  7095: jp   nz,$709B
A=00 BC=D800 DE=DA00 HL=DFE4  7098: jp   $70C9
A=00 BC=D800 DE=DA00 HL=DFE4  70C9: ld   hl,sp+$48
A=00 BC=D800 DE=DA00 HL=DFE7  70CB: ld   a,(hl+)
A=00 BC=D800 DE=DA00 HL=DFE8  70CC: ld   d,(hl)
A=00 BC=D800 DE=0000 HL=DFE8  70CD: ld   e,a
A=00 BC=D800 DE=0000 HL=DFE8  70CE: ld   a,($7E26)
A=00 BC=D800 DE=0000 HL=DFE8  70D1: ld   b,a
A=00 BC=0000 DE=0000 HL=DFE8  70D2: ld   a,($7E25)
A=08 BC=0000 DE=0000 HL=DFE8  70D5: ld   c,a
A=08 BC=0008 DE=0000 HL=DFE8  70D6: ld   a,d
A=00 BC=0008 DE=0000 HL=DFE8  70D7: add  a,$80
A=80 BC=0008 DE=0000 HL=DFE8  70D9: ld   l,a
A=80 BC=0008 DE=0000 HL=DF80  70DA: ld   a,b
A=00 BC=0008 DE=0000 HL=DF80  70DB: add  a,$80
A=80 BC=0008 DE=0000 HL=DF80  70DD: cp   l
A=80 BC=0008 DE=0000 HL=DF80  70DE: jr   nz,$70E2
A=80 BC=0008 DE=0000 HL=DF80  70E0: ld   a,c
A=08 BC=0008 DE=0000 HL=DF80  70E1: cp   e
A=08 BC=0008 DE=0000 HL=DF80  70E2: jp   c,$722B
A=08 BC=0008 DE=0000 HL=DF80  70E5: sla  e
A=08 BC=0008 DE=0000 HL=DF80  70E7: rl   d
```

This is a big block, however if we compare this block (specifically the register values, with a few other times in the file e.g. search for __708D__) :

the top 6 lines seem to show BC= moving past consecutive address for example here are 2 sets (side by side) :

```asm
A=00 BC=D812 DE=DA00 HL=0000  708D: ld   hl,sp+$44    A=00 BC=D812 DE=DA00 HL=0000  708D: ld   hl,sp+$44
A=00 BC=D812 DE=DA00 HL=DFE3  708F: ld   a,(hl+)      A=00 BC=D812 DE=DA00 HL=DFE3  708F: ld   a,(hl+)
A=1B BC=D812 DE=DA00 HL=DFE4  7090: ld   b,(hl)       A=1C BC=D812 DE=DA00 HL=DFE4  7090: ld   b,(hl)
A=1B BC=D812 DE=DA00 HL=DFE4  7091: ld   c,a          A=1C BC=D812 DE=DA00 HL=DFE4  7091: ld   c,a
A=1B BC=D81B DE=DA00 HL=DFE4  7092: ld   a,(bc)       A=1C BC=D81C DE=DA00 HL=DFE4  7092: ld   a,(bc)
A=00 BC=D81B DE=DA00 HL=DFE4  7093: ld   c,a          A=00 BC=D81C DE=DA00 HL=DFE4  7093: ld   c,a
```

Looking at the bottom line here, __A=00 BC=D81B__ && __A=00 BC=D81C__ I'm reasonably confident that A is the tile number and BC is the address it is fetched from. __D81B__ is according to [Pan Docs Memory Map](https://gbdev.io/pandocs/Memory_Map.html), within the work ram (the bank switchable 4k block),
this unfortunately means that some other code is responsible for getting the data to that address from the cartridge. Before we try to locate that, lets make sure the memory around __D81B__ looks like our tilemap. Set the memory view source to __Sharp LR35902 ':mainpu' program space memory__ and the address to __D81B__. This certainly looks like it could be the tilemap (the tile numbers are different, but the layout seems possible), if you can't see it, try changing the address to __D812__ and you should see a 16 which would seem to be the wall tile this time. 

We can use the same tactic we used to find the writes to video ram, to find the code that is performing the copy :

```
softreset
wpclear
wpset D812,1,w
```

Set the game running, resume past the first several times the debugger stops (memory being used for bios then clearing) until you get to the main menu. Once you start the game (__m__), a breakpoint should fire and we will now be in the code responsible for putting the tilemap into ram. This code, looks like a standard copy loop :

```asm
2D0B ld a,(bc)          ; get byte from source pointer (BC)
2D0C ld (hl+),a         ; save byte into dest pointer and increment it (HL)
2D0D inc bc             ; increment source pointer
2D0E dec de             ; decrement count (DE)
2D0F ld a,b
2D10 or e               ; or D and E together
2D11 jr nz,$2D0B        ; if the combined result is non zero, we need to loop around and copy more
```

Now, if you look at the value of __B__ & __C__ in the CPU State view, you should see __40__ & __00__ respectively, so this register pair is pointing into the cartridge, again into the bankable rom slot. So using the memory view (Note _you can open multiple memory views_), set source to __Game Boy MBC1 Cartridge/:cartslot:rom_mbc1/0/m_bank_sel_rom__ and taking a look at the second set of four digits, we see __0006__. Lets goto that address in Ghidra (__g__ enter 0x4000 and then select rom6), and you should see a familiar string of data - We've found the tilemap for the first level.

However, we are not finished here, our task for this part of the tutorial was to figure out how to add a level select widget to our plugin. So far we have located the tilemap (which will be useful in a later section, when we turn to making an editor), but we don't yet know how the levels are chosen between.

The first thing to do is to find the code that performed the memory copy, we could use ghidra to find the xref's to the memory copy routine and then examine each one by hand, but I suspect quite a few places perform memory copies. So instead, we return the Editor, where we should still be stopped at the point we copied the first byte. Now we use the __history__ command to list the code addresses that were executed leading up to the breakpoint. 

Enter the following in the console view :

```
history
```

This will list the last 256 addresses and instructions the cpu on the Game Boy executed, they are listed in oldest to newest order. Reading from the bottom upwards, we can see a __2D23: call $2D09__, which will be the parent function calling the copy, so lets look at that code in Ghidra (__g__ enter __0x2D23__). This appears to be a wrapper function, if you look at the right hand side (the Decompile window) of Ghidra, it appears to be a function that just directly calls the copy function. So we can ignore this one, go back to the history and find the next oldest call instruction, in this case __17F4: call $2D14__. If we look at that function in Ghidra again looking at the Decompile view, this looks more promising :

```c
void FUN_178f(byte param_5)
{
  byte bVar1;
  
  DAT_c17c = 0x12;
  DAT_c17d = 0xd8;
  DAT_c118 = param_5;
  if (param_5 < 0x10)
  {
    FUN_0317();
    CopyMemory(CONCAT11(DAT_c17d,DAT_c17c),
               (uint)(byte)((((((((byte)(param_5 << 2) >> 7) << 1 | (byte)(param_5 << 3) >> 7) << 1
                               | (byte)(param_5 << 4) >> 7) << 1 | (byte)(param_5 << 5) >> 7) << 1 |
                             (byte)(param_5 << 6) >> 7) << 1 | param_5 & 1) << 2) * 0x100 + 0x4000,
               0x400);
    FUN_034f();
  }
  else
  {
    FUN_0317();
    bVar1 = param_5 - 0x10;
    CopyMemory(CONCAT11(DAT_c17d,DAT_c17c),
               (uint)(byte)((((((((byte)(bVar1 * '\x04') >> 7) << 1 | (byte)(bVar1 * '\b') >> 7) <<
                                1 | (byte)(param_5 * '\x10') >> 7) << 1 | (byte)(param_5 * ' ') >> 7
                              ) << 1 | (byte)(param_5 * '@') >> 7) << 1 | bVar1 & 1) << 2) * 0x100 +
               0x6ddc,0x400);
    FUN_034f();
  }
  bVar1 = LCDC;
  LCDC = bVar1 & 0x7f;
  FUN_0317();
  func_0x6f3b();
  FUN_034f();
  return;
}
```

__param_5__ in the above code seems important, there is a comparison against __0x10__ which seems to be used to modify the calculation for the memory copy, perhaps the first 16 levels are stored in a different location to the last few. __DAT_c118__ seems to have param_5 copied into it, perhaps __DAT_c118__ is used to store the current level number. In the Decompile view in Ghidra, place the cursor on the __DAT_c118__ and press __enter__, which will cause the Listing view to show the location of that variable. It will be ?? because Ghidra is only a static analysis tool, it can't have knowledge of writes to ram. We could look at address __C118__ in the memory view in the debugger, but for now, lets continue with Ghidra. In the listing view, you should see something like : 

```
                             DAT_c118                                        XREF[8]:     FUN_146f:16d6(R), 
                                                                                          FUN_178f:179d(W), 
                                                                                          FUN_186e:187c(W), 
                                                                                          FUN_1cff:1faa(R), 
                                                                                          FUN_1cff:2268(R), 
                                                                                          FUN_1cff:231d(R), 
                                                                                          FUN_1cff:2323(W), 
                                                                                          FUN_1cff:2473(R)  
            c118                 undefined1 ??
```

The XREF's show reads and writes to the address, the write at __FUN_178f:179d__ is the one we already saw, that leaves two other writes. We are looking for something that reads and increments the value and puts it back. We can double click on the references to follow them, allowing us to look at the code in place. The write at __FUN_186e:187c__ appears to be a similar (but simpler) version of the first function. However the write at __FUN_1cff:2323__ shows the following code.

```
            231d fa 18 c1        LD         A,(DAT_c118)                                     = ??
            2320 5f              LD         E,A
            2321 1c              INC        E
            2322 7b              LD         A,E
            2323 ea 18 c1        LD         (DAT_c118),A                                     = ??
```

This certainly looks like something incrementing the level counter, this code lives inside a huge function at __1CFF__, in order to save time in this tutorial, I can tell you it is the function that handles the gameplay (winning levels, level logic, death etc). Scrolling up a little from our current location you should find a label __LAB_22f7__, I think this is probably the code responsible for setting up the next level after you complete a level. It turns out that there is code specifically for setting up the first level near the top of this function, but I've spent long enough on getting us to this point, and we haven't actually got to the point of adding some code to our plugin. 

So taking a small shortcut, lets update the SetupGameTemporaryPatches method and I will explain it below :

```cs
    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        // Patch out the main menu/intro ()
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x2981, new byte[] { 0x00, 0x00, 0x00 });

        // Try forcing level 4  (4 - 2 == 2nd byte)
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x1d7d, new byte[] { 
            0x3E, 0x02,                     // LD A, 2
            0xEA, 0x18 , 0xC1,              // LD (C118), A
            0xC3, 0xF7, 0x22 });            // JP 22F7
    }
```

This patches the start of the game level code function (__1CFF__) to store a value into the level counter, and then jump to the code responsible for setting up the next level after completing it. We use 2 even though we want level 4, because the code that sets up a new level (__22F7__) increments the level variable, we need to subtract 1. In addtion, the level variable is 0 based, ie __00__ is level 1, so we need to subtract another 1.

Reload the plugin with this code in place, and after a few seconds (at some point we should try and remove this stall), the game should start on __Abandoned Uranium Workings__. It seems to work (although if all lives are lost, things do go wrong), so lets try adding a control to choose which level is currently running. In order to add a control to the player window (which is built in), we need to implement [IPlayerWindowExtension](xref:RetroEditor.Plugins.IPlayerWindowExtension), so add the interface to our plugin :

```cs
class ManicMinerGBC : IRetroPlugin, IPlayerWindowExtension
```

This extension requires a new method to be implemented : 

```cs
    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls);
```

So lets add the following code (which will add a slider widget to the player screen) into the ManicMinerGBC class :

```cs
    IWidgetRanged levelValue;

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        levelValue = widget.AddSlider("Level", 1, 1, 20, () => playerControls.Reset());
    }
```

And reload the plugin. You will need to make the player view a little bigger vertically, in order to see the slider (window sizes are automatically computed, however when adding controls to windows that have existed, they retain their position and size from the previous time). Moving the slider, will reset the game, but it will always be on level 4 (because we haven't yet used the value in the slider), lets fix that :

```cs
    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        // Patch out the main menu/intro ()
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x2981, new byte[] { 0x00, 0x00, 0x00 });

        // Set level to match slider value
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x1d7d, new byte[] { 
            0x3E, (byte)(levelValue.Value-2),   // LD A, (slider value - 2)
            0xEA, 0x18 , 0xC1,                  // LD (C118), A
            0xC3, 0xF7, 0x22 });                // JP 22F7
    }
```

Now, when you reload the plugin, the slider will change the level as we would expect.

### Adding a tilemap editor

The function at __178F__ seems to indicate that levels 1-16 are located at address __4000__ - __7FFF__ in rom6, and levels 17-20 are located at address __6DDC__ - __7DDB__  in rom7. Each block of level data is __400__ (1k) in size. We know the tilemap is at the start of the data block, but not much else.

The Game Boy screen is 160x144 pixels, there are 2 tile rows used for the levelname and status row, so that would leave 16 tiles high (144/8 - 2). 16*32 = 512, so the first 512 bytes are the tile map. We still need to figure out where the tile data is, we could use the same technique of putting a watch for a write to memory, this time a write to the tile data area __8000__ to __97FF__ and then try to track it back to where the data is coming from in the rom cartridge.  

However, manicminer.gbc appears to use the same level data as the original (zx spectrum) game, which means we can use someone elses work to figure out how to build a tilemap editor. [Manic Miner Room-Format](https://www.icemark.com/dataformats/manic/mmformat.htm) has a lot of details about the original, I've reproduced a little here in case the website goes down.

If we look at the first level (__18000__ - __183FF__ in absolute offset from start of rom), it breaks down roughly as follows :

| Offset In Rom | Offset from start of level data | Size | Meaning      |
|---|---|---|---|
| 0x18000       | 0                               | 512  | Level Layout |
| 0x18200       | 512                             | 32   | Level name   |
| 0x18220       | 544                             | 72   | Block Graphics |

This should be enough to make a start on our tilemap editor. 

The first thing we need to do, is add a menu in order to bring up our editing window. For now, lets keep things simple and just work on the first level. We can add a menu by implementing the [IMenuProvider](xref:RetroEditor.Plugins.IMenuProvider), so add the interface to our plugin :

```cs
class ManicMinerGBC : IRetroPlugin, IPlayerWindowExtension, IMenuProvider
```

We are required to implement :

```cs
    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
```

This method lets us add custom menus to our plugin, they will appear under __Window__->__<plugin name>__-> and you can create submenus as you see fit.
For now, we just need a window in order to experiment. Add the following code :

```cs
    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Edit Level 1",
                (editorInterface,menuItem) => {
                    //editorInterface.OpenUserWindow($"Edit Level 1", new ManicMinerTileEditor(rom));
                });
    }
```

If you reload the plugin, you will now see a new menu item, although since we commented out the open window call, it won't do anything just yet.

Next up, we need to add a class that implements the IUserWindow interface, this will allow us to comment out the line above.

```cs
class ManicMinerTileEditor : IUserWindow
{
    public float UpdateInterval => 1 / 30.0f;

    public ManicMinerTileEditor(IMemoryAccess rom)
    {
        // Do nothing for now
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        // Do nothing for now
    }

    public void OnClose()
    {
        // Do nothing for now
    }
}
```

Now, if you uncomment the line in ConfigureMenu, and reload the plugin, you can use the menu to open a window, although we haven't yet done anything useful with the window. The window will be tiny, because we haven't given it any widgets, lets deal with that next. At present when reloading a plugin, all windows apart from the player window are closed, at some point I hope to change that, but for now, you will have to re-open the edit level window when you reload the plugin.

Since we are trying to make an editor to edit the layout of the room, we will need two widgets, a TilePaletteWidget and a TileMapWidget. The first is used to represent the tiles that can be used, and the second is the layout of the map. Lets begin with the palette, we will need a couple of helper classes adding, lets start with the ManicMinerTile :

```cs
public class ManicMinerTile : ITile
{
    Pixel[] imageData;
    string name;

    public ManicMinerTile(IMemoryAccess rom, uint offset, string name)
    {
        this.imageData = Array.Empty<Pixel>();
        this.name = name;
    }

    public uint Width => 8;

    public uint Height => 8;

    public string Name => name;

    public void Update(Pixel[] imageData)
    {
        this.imageData = imageData;
    }

    public Pixel[] GetImageData()
    {
        return imageData;
    }
}
```

This class will hold our tile representations, it implements the [ITile](xref:RetroEditor.Plugins.ITile) interface. The next class will hold our palette of tiles, implementing the [ITilePalette](xref:RetroEditor.Plugins.ITilePalette) interface.

```cs
class ManicMinerTilePalette : ITilePalette
{
    public uint MaxTiles => 8;

    public int SelectedTile { get; set; }

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public uint TilesPerRow => 4;

    public TilePaletteStore tilePaletteStore;

    ManicMinerTile[] tiles;
    public ManicMinerTilePalette(IMemoryAccess rom)
    {
        tiles = Array.Empty<ManicMinerTile>();
        tilePaletteStore = new TilePaletteStore(this);
    }

    public void Update(float seconds)
    {
        // Do nothing for now
    }

    public ReadOnlySpan<ITile> FetchTiles()
    {
        return tiles;
    }
}
```

Finally update the ManicMinerTileEditor as follows :

```cs
class ManicMinerTileEditor : IUserWindow
{
    public float UpdateInterval => 1 / 30.0f;

    private ManicMinerTilePalette tilePalette;

    public ManicMinerTileEditor(IMemoryAccess rom)
    {
        tilePalette = new ManicMinerTilePalette(rom);
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddLabel("Palette");
        widget.AddTilePaletteWidget(tilePalette.tilePaletteStore);
    }

    public void OnClose()
    {
        // Do nothing for now
    }
}
```

You should be able to reload the plugin at this point, although the only change to the editor window will be the word Palette.

However don't fret, we can try and extract the graphics we will need for the palette. According to [Block Graphics Section of the Manic Miner Data Format](http://icemark.com/dataformats/manic/mmformat.htm#block_graphics) the block graphics are stored in the offsets 544-615, each of which is 9 bytes in size.

The first byte is the colour attribute, the next 8 bytes are a bitmap representing the 8x8 tile. We need the tilemap to be in non palettised format in order for the editor to render it. So lets modify the constructor of the tile to perform this conversion :

```cs
    public ManicMinerTile(IMemoryAccess rom, uint offset, string name)
    {
        this.imageData = new Pixel[8 * 8];
        var tileData = rom.ReadBytes(ReadKind.Rom, offset, 9);
        this.name = name;

        // See appendix A in the manicminer format (or a zx spectrum colour attribute document)
        var attr = tileData[0];
        var inkColour = attr & 0x07;                                // The lower 3 bits are the ink colour (RGB)
        var paperColour = (attr >> 3) & 0x07;                       // The next 3 bits are the paper colour (RGB)
        var bright = (attr & 0x40) != 0 ? 63 : 0;                   // The 7th bit is the bright flag
        var inkBright = (inkColour != 0) ? bright : 0;              // bright adds 63 to the colour value if not 0
        var paperBright = (paperColour != 0) ? bright : 0;          // bright adds 63 to the colour value if not 0

        // combine attributes to form a colour for ink and paper (R = 0 or 192, G = 0 or 192, B = 0 or 192) + bright
        var ink = new Pixel((byte)((inkColour & 2) * 96 + inkBright),
                            (byte)((inkColour & 4) * 48 + inkBright),
                            (byte)((inkColour & 1) * 192 + inkBright));
        var paper = new Pixel((byte)((paperColour & 2) * 96 + paperBright),
                              (byte)((paperColour & 4) * 48 + paperBright),
                              (byte)((paperColour & 1) * 192 + paperBright));
        for (int y = 0; y < 8; y++)
        {
            var row = tileData[y + 1];
            for (int x = 0; x < 8; x++)
            {
                var pixel = (row & (1 << (7 - x))) != 0 ? ink : paper;
                imageData[y * 8 + x] = pixel;
            }
        }
    }
```

Now we can initialise our 8 tiles from the offsets we are given in the [Block Graphics Section of the Manic Miner Data Format](http://icemark.com/dataformats/manic/mmformat.htm#block_graphics). So update the constructor of the tile palette as follows :

```cs
    public ManicMinerTilePalette(IMemoryAccess rom)
    {
        tiles = new ManicMinerTile[8];
        tiles[0] = new ManicMinerTile(rom, 0x4000 * 6 + 544, "Background");
        tiles[1] = new ManicMinerTile(rom, 0x4000 * 6 + 553, "Floor");
        tiles[2] = new ManicMinerTile(rom, 0x4000 * 6 + 562, "Crumbling Floor");
        tiles[3] = new ManicMinerTile(rom, 0x4000 * 6 + 571, "Wall");
        tiles[4] = new ManicMinerTile(rom, 0x4000 * 6 + 580, "Conveyor");
        tiles[5] = new ManicMinerTile(rom, 0x4000 * 6 + 589, "Nasty 1");
        tiles[6] = new ManicMinerTile(rom, 0x4000 * 6 + 598, "Nasty 2");
        tiles[7] = new ManicMinerTile(rom, 0x4000 * 6 + 607, "Spare");
        tilePaletteStore = new TilePaletteStore(this);
    }
```

Now if you reload, your window should have 8 tiles you can select between. 

In order to be able to make modifications to the level, we will need another widget. The TileMapWidget also needs a few interfaces implementing, first up, we need a layer [ILayer](xref:RetroEditor.Plugins.ILayer).

```cs
class ManicMinerTileMapLayer : ILayer
{
    public uint Width => 32;

    public uint Height => 16;

    uint[] mapData;
    public ManicMinerTileMapLayer(IMemoryAccess rom, uint offset, ManicMinerTilePalette tilePalette)
    {
        mapData = new uint[Width * Height];
    }

    public ReadOnlySpan<uint> GetMapData()
    {
        return mapData;
    }

    public void SetTile(uint x, uint y, uint tile)
    {
        mapData[y * Width + x] = tile;
    }
}
```

The width and height for the layer are expressed in tiles, and we temporarily construct an empty map, which we will populate from the games level data a little later.

Next up, we need an implementation of [ITileMap](xref:RetroEditor.Plugins.ITileMap) this defines the maximum pixel size of the map, along with accessors for the layers and palette in use by the map.

```cs
class ManicMinerTileMap : ITileMap
{
    public uint Width => 32 * 8;

    public uint Height => 16 * 8;

    public uint NumLayers => 1;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    private TilePaletteStore _tilePaletteStore;
    private ManicMinerTileMapLayer _layer;

    public ManicMinerTileMap(IMemoryAccess rom, uint offset, ManicMinerTilePalette tilePalette)
    {
        _tilePaletteStore = tilePalette.tilePaletteStore;
        _layer = new ManicMinerTileMapLayer(rom, offset, tilePalette);
    }

    public ILayer FetchLayer(uint layer)
    {
        return _layer;
    }

    public TilePaletteStore FetchPalette(uint layer)
    {
        return _tilePaletteStore;
    }
}
```

Finally, we update the ManicMinerTileEditor class as follows (adding the tilemap widget) :

```cs
class ManicMinerTileEditor : IUserWindow
{
    public float UpdateInterval => 1 / 30.0f;

    private ManicMinerTilePalette tilePalette;
    private ManicMinerTileMap tileMap;

    public ManicMinerTileEditor(IMemoryAccess rom)
    {
        tilePalette = new ManicMinerTilePalette(rom);
        tileMap = new ManicMinerTileMap(rom, 0x4000 * 6 + 0, tilePalette);
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddLabel("Palette");
        widget.AddTilePaletteWidget(tilePalette.tilePaletteStore);
        widget.AddLabel("TileMap");
        widget.AddTileMapWidget(tileMap);
    }

    public void OnClose()
    {
        // Do nothing for now
    }
}
```

Note in the above, we are passing the address of the data in the cartridge that contains the data we will need to edit the actual level (__4000__*6+0), but we are not using it yet.

At this point, you can reload the plugin. and now you can paint tiles onto the map, but the map does not contain the data from the game, and we can't affect the game either. Looking at the [Manic Miner Screen Layout](https://www.icemark.com/dataformats/manic/mmformat.htm#screen_layout), it turns out the data is stored in a slighty odd fashion. The 512 (32*16) bytes of the screen layout map 1 to 1 with tiles in our tilemap, however instead of the byte containing a tile number, it contains the attribute used to colour the particular tile. So we need to map tile attributes to our tile indices.

First up, adjust the __ManicMinerTile__ class so we can access the attribute for a tile, here is the full code, but essentially I've just added an __Attr__ property to return the attribute value from the first byte in the tiledata :

```cs
public class ManicMinerTile : ITile
{
    Pixel[] imageData;
    string name;
    byte _attr;

    // Assume offset points to the start of the tile
    public ManicMinerTile(IMemoryAccess rom, uint offset, string name)
    {
        this.imageData = new Pixel[8 * 8];
        var tileData = rom.ReadBytes(ReadKind.Rom, offset, 9);
        this.name = name;

        // See appendix A in the manicminer format (or a zx spectrum colour attribute document)
        _attr = tileData[0];
        var inkColour = _attr & 0x07;                                // The lower 3 bits are the ink colour (RGB)
        var paperColour = (_attr >> 3) & 0x07;                       // The next 3 bits are the paper colour (RGB)
        var bright = (_attr & 0x40) != 0 ? 63 : 0;                   // The 7th bit is the bright flag
        var inkBright = (inkColour != 0) ? bright : 0;     // bright adds 63 to the colour value if not 0
        var paperBright = (paperColour != 0) ? bright : 0; // bright adds 63 to the colour value if not 0

        // combine attributes to form a colour for ink and paper (R = 0 or 192, G = 0 or 192, B = 0 or 192) + bright
        var ink = new Pixel((byte)((inkColour & 2) * 96 + inkBright),
                            (byte)((inkColour & 4) * 48 + inkBright),
                            (byte)((inkColour & 1) * 192 + inkBright));
        var paper = new Pixel((byte)((paperColour & 2) * 96 + paperBright),
                              (byte)((paperColour & 4) * 48 + paperBright),
                              (byte)((paperColour & 1) * 192 + paperBright));
        for (int y = 0; y < 8; y++)
        {
            var row = tileData[y + 1];
            for (int x = 0; x < 8; x++)
            {
                var pixel = (row & (1 << (7 - x))) != 0 ? ink : paper;
                imageData[y * 8 + x] = pixel;
            }
        }
    }

    public uint Width => 8;

    public uint Height => 8;

    public string Name => name;

    public void Update(Pixel[] imageData)
    {
        this.imageData = imageData;
    }

    public Pixel[] GetImageData()
    {
        return imageData;
    }

    public byte Attr => _attr;
}
```

Now we can access the attribute value from a tile, we can create a dictionary to map between 
Add the following to the __ManicMinerTilePalette__ class :

```cs
    Dictionary<byte,uint> attrToIndex;
    Dictionary<uint,byte> indexToAttr;
    internal uint AttrToIndex(byte attr)
    {
        return attrToIndex[attr];
    }
    internal byte IndexToAttr(uint index)
    {
        return indexToAttr[index];
    }
```

also modify the constructor to initialise __attrToIndex__ :

```cs
    public ManicMinerTilePalette(IMemoryAccess rom)
    {
        tiles = new ManicMinerTile[8];
        tiles[0] = new ManicMinerTile(rom, 0x4000 * 6 + 544, "Background");
        tiles[1] = new ManicMinerTile(rom, 0x4000 * 6 + 553, "Floor");
        tiles[2] = new ManicMinerTile(rom, 0x4000 * 6 + 562, "Crumbling Floor");
        tiles[3] = new ManicMinerTile(rom, 0x4000 * 6 + 571, "Wall");
        tiles[4] = new ManicMinerTile(rom, 0x4000 * 6 + 580, "Conveyor");
        tiles[5] = new ManicMinerTile(rom, 0x4000 * 6 + 589, "Nasty 1");
        tiles[6] = new ManicMinerTile(rom, 0x4000 * 6 + 598, "Nasty 2");
        tiles[7] = new ManicMinerTile(rom, 0x4000 * 6 + 607, "Spare");
        tilePaletteStore = new TilePaletteStore(this);
        attrToIndex = new Dictionary<byte, uint>();
        indexToAttr = new Dictionary<uint, byte>();
        for (int i = 0; i < 8; i++)
        {
            attrToIndex[tiles[i].Attr] = (uint)i;
            indexToAttr[(uint)i] = tiles[i].Attr;
        }
    }
```

We now have a way to get from an attribute value to the tile, so we should be able to convert the game representation of the screen data into our tilemap format. To do this, we need to update the __ManicMinerTileMapLayer__ constructor as follows :

```cs
    IMemoryAccess _rom;
    ManicMinerTilePalette _tilePalette;
    public ManicMinerTileMapLayer(IMemoryAccess rom, uint offset, ManicMinerTilePalette tilePalette)
    {
        _rom = rom;                 // We record these because they will be useful when we modify the rom
        _tilePalette = tilePalette;
        mapData = new uint[Width * Height];
        var tileData = rom.ReadBytes(ReadKind.Rom, offset, Width * Height);
        for (uint y = 0; y < Height; y++)
        {
            for (uint x = 0; x < Width; x++)
            {
                mapData[y * Width + x] = tilePalette.AttrToIndex(tileData[(int)(y * Width + x)]);
            }
        }
    }
```

Reload the plugin, and you should now see the first level in the tilemap editor. There are some things missing :
- Enemies
- Items
- Exit

Before we worry about those things, we should make sure changes are applied back to the game. The easiest way to do this, is to update the __SetTile__ method in __ManicMinerTileMapLayer__. 

```cs
    public void SetTile(uint x, uint y, uint tile)
    {
        mapData[y * Width + x] = tile;
        _rom.WriteBytes(WriteKind.SerialisedRom, 0x4000 * 6 + 0 + y * Width + x, new byte[] { _tilePalette.IndexToAttr(tile) });
    }
```

At this point, reload the plugin, and you can modify the level, and if you move the slider on the player window away and back to level 1, your changes should be playable. 

### Adding pickups

According to [Manic Miner Items](https://www.icemark.com/dataformats/manic/mmformat.htm#items) each level can have between 0 and 5 items. This means we need to limit the number of times an item can be placed. Items can also overlay ontop of background items (as their positions are stored seperately - although according to the page they should not be placed ontop of other tiles).  

The graphic for the item is stored at offset 692-699, just the bitmap this time, the colours are stored in the item table.

The item table is 5 bytes per item with 5 items per room occupying offsets 629-653, an items 5 bytes break down as :

| Offset | Use |
|---|---|
| 0      | Colour attribute of item. Or 0 for not used or 255 to terminate the item list |
| 1      | YYYXXXXX where X is the tile column and Y is the tile row (the most significant bit of Y is held in offset 2/3) |
| 2      | 0101110Y the most significant bit of the tile row (same as offset 3) |
| 3      | 0110Y000 the most significant bit of the tile row (same as offset 2) |
| 4      | 11111111 always 255 |

For simplicity we will extend our current tilemap and palette, adding the items as another tile, and some code to control number of items placed etc.

For this, I will just post the full completed code included the changes.

```cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RetroEditor.Plugins;

class ManicMinerGBC : IRetroPlugin, IPlayerWindowExtension, IMenuProvider
{
    // This is the name of the plugin that will be displayed in the UI
    public static string Name => "Manic Miner GameBoy Colour";

    // This is the name of the system (that we created above) used to load this game
    public string RomPluginName => "GameBoyColour";

    // Cartridge based games don't typically require an auto-loader (unlike disc/tape based games)
    public bool RequiresAutoLoad => false;

    // We can stub this function, it's not required for this plugin
    public bool AutoLoadCondition(IMemoryAccess romAccess)
    {
        return false;
    }

    // This function is used to determine if the plugin can handle the given file
    // So we check if the MD5 hash matches the one for Manic Miner (b13061a4a1a84ef2edb4c9d47f794093)
    public bool CanHandle(string path)
    {
        var manicMinerMD5 = new byte[] { 0xb1, 0x30, 0x61, 0xa4, 0xa1, 0xa8, 0x4e, 0xf2, 0xed, 0xb4, 0xc9, 0xd4, 0x7f, 0x79, 0x40, 0x93 };
        
        if (!File.Exists(path))
        {
            return false;
        }
        var md5 = MD5.Create().ComputeHash(File.ReadAllBytes(path));

        if (manicMinerMD5.SequenceEqual(md5))
        {
            return true;
        }
        return false;
    }

    // We dont yet support exporting saves for Manic Miner GBC, so just throw
    public ISave Export(IMemoryAccess romAcess)
    {
        //`File.WriteAllBytes("modified.gbc", romAcess.ReadBytes(ReadKind.Rom, 0, (uint)romAcess.RomSize).ToArray());
        throw new System.NotImplementedException("Manic Miner GBC does not support exporting saves, yet");
    }

    public void SetupGameTemporaryPatches(IMemoryAccess romAccess)
    {
        // Patch out the main menu/intro ()
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x2981, new byte[] { 0x00, 0x00, 0x00 });
        // Disable bung logo
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x296C, new byte[] { 0x00, 0x00, 0x00 });

        // Set level to match slider value
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x1d7d, new byte[] { 
            0x3E, (byte)(levelValue.Value-2),   // LD A, (slider value - 2)
            0xEA, 0x18 , 0xC1,                  // LD (C118), A
            0xC3, 0xF7, 0x22 });                // JP 22F7
    }

    IWidgetRanged levelValue;

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        levelValue = widget.AddSlider("Level", 1, 1, 20, () => playerControls.Reset());
    }

    public void ConfigureMenu(IMemoryAccess rom, IMenu menu)
    {
        menu.AddItem("Edit Level 1",
                (editorInterface,menuItem) => {
                    editorInterface.OpenUserWindow($"Edit Level 1", new ManicMinerTileEditor(rom));
                });
    }
}

public class ManicMinerTile : ITile
{
    Pixel[] imageData;
    string name;
    byte _attr;

    // Assume offset points to the start of the tile
    public ManicMinerTile(IMemoryAccess rom, uint offset, string name, bool hasAttributes = true, byte overrideAttr = 0)
    {
        this.imageData = new Pixel[8 * 8];
        ReadOnlySpan<byte> tileData = hasAttributes ? rom.ReadBytes(ReadKind.Rom, offset, 9) : rom.ReadBytes(ReadKind.Rom, offset, 8);
        this.name = name;

        // See appendix A in the manicminer format (or a zx spectrum colour attribute document)
        SetupAttribute(hasAttributes ? tileData[0] : overrideAttr, out Pixel ink, out Pixel paper);
        if (hasAttributes)
        {
            tileData = tileData[1..];
        }
        for (int y = 0; y < 8; y++)
        {
            var row = tileData[y];
            for (int x = 0; x < 8; x++)
            {
                var pixel = (row & (1 << (7 - x))) != 0 ? ink : paper;
                imageData[y * 8 + x] = pixel;
            }
        }
    }

    public void SetupAttribute(byte attr, out Pixel ink, out Pixel paper)
    {
        _attr = attr;
        var inkColour = _attr & 0x07;                                // The lower 3 bits are the ink colour (RGB)
        var paperColour = (_attr >> 3) & 0x07;                       // The next 3 bits are the paper colour (RGB)
        var bright = (_attr & 0x40) != 0 ? 63 : 0;                   // The 7th bit is the bright flag
        var inkBright = (inkColour != 0) ? bright : 0;     // bright adds 63 to the colour value if not 0
        var paperBright = (paperColour != 0) ? bright : 0; // bright adds 63 to the colour value if not 0

        // combine attributes to form a colour for ink and paper (R = 0 or 192, G = 0 or 192, B = 0 or 192) + bright
        ink = new Pixel((byte)((inkColour & 2) * 96 + inkBright),
                            (byte)((inkColour & 4) * 48 + inkBright),
                            (byte)((inkColour & 1) * 192 + inkBright));
        paper = new Pixel((byte)((paperColour & 2) * 96 + paperBright),
                              (byte)((paperColour & 4) * 48 + paperBright),
                              (byte)((paperColour & 1) * 192 + paperBright));
    }

    public uint Width => 8;

    public uint Height => 8;

    public string Name => name;

    public void Update(Pixel[] imageData)
    {
        this.imageData = imageData;
    }

    public Pixel[] GetImageData()
    {
        return imageData;
    }

    public byte Attr => _attr;
}

class ManicMinerTilePalette : ITilePalette
{
    public uint MaxTiles => 9;

    public int SelectedTile { get; set; }

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    public uint TilesPerRow => 4;

    public TilePaletteStore tilePaletteStore;

    ManicMinerTile[] tiles;
    public ManicMinerTilePalette(IMemoryAccess rom)
    {
        tiles = new ManicMinerTile[9];
        tiles[0] = new ManicMinerTile(rom, 0x4000 * 6 + 544, "Background");
        tiles[1] = new ManicMinerTile(rom, 0x4000 * 6 + 553, "Floor");
        tiles[2] = new ManicMinerTile(rom, 0x4000 * 6 + 562, "Crumbling Floor");
        tiles[3] = new ManicMinerTile(rom, 0x4000 * 6 + 571, "Wall");
        tiles[4] = new ManicMinerTile(rom, 0x4000 * 6 + 580, "Conveyor");
        tiles[5] = new ManicMinerTile(rom, 0x4000 * 6 + 589, "Nasty 1");
        tiles[6] = new ManicMinerTile(rom, 0x4000 * 6 + 598, "Nasty 2");
        tiles[7] = new ManicMinerTile(rom, 0x4000 * 6 + 607, "Spare");
        tiles[8] = new ManicMinerTile(rom, 0x4000 * 6 + 692, "Pickup", false, 0x07);
        tilePaletteStore = new TilePaletteStore(this);
        attrToIndex = new Dictionary<byte, uint>();
        indexToAttr = new Dictionary<uint, byte>();
        for (int i = 0; i < 8; i++)
        {
            attrToIndex[tiles[i].Attr] = (uint)i;
            indexToAttr[(uint)i] = tiles[i].Attr;
        }
    }

    public void Update(float seconds)
    {
        // Do nothing for now
    }

    public ReadOnlySpan<ITile> FetchTiles()
    {
        return tiles;
    }

    Dictionary<byte,uint> attrToIndex;
    Dictionary<uint,byte> indexToAttr;
    internal uint AttrToIndex(byte attr)
    {
        return attrToIndex[attr];
    }
    internal byte IndexToAttr(uint index)
    {
        return indexToAttr[index];
    }
}

class ManicMinerTileMapLayer : ILayer
{
    public uint Width => 32;

    public uint Height => 16;

    uint[] mapData;
    IMemoryAccess _rom;
    ManicMinerTilePalette _tilePalette;
    public ManicMinerTileMapLayer(IMemoryAccess rom, uint offset, ManicMinerTilePalette tilePalette)
    {
        _rom = rom;                 // We record these because they will be useful when we modify the rom
        _tilePalette = tilePalette;
        mapData = new uint[Width * Height];
        var tileData = rom.ReadBytes(ReadKind.Rom, offset, Width * Height);
        for (uint y = 0; y < Height; y++)
        {
            for (uint x = 0; x < Width; x++)
            {
                mapData[y * Width + x] = tilePalette.AttrToIndex(tileData[(int)(y * Width + x)]);
            }
        }
        // Add pickup locations
        _pickupOffset = offset + 629;
        _pickups = new List<Pickup>();
        GetPickups();
        foreach (var pickup in _pickups)
        {
            mapData[pickup.y * Width + pickup.x] = 8;
        }
    }
    private uint _pickupOffset;
    private List<Pickup> _pickups;

    struct Pickup
    {
        public byte x;
        public byte y;
        public byte attr;
    }

    // Convert in memory format to our simple list format
    public void GetPickups()
    {
        _pickups.Clear();
        var pickups = _rom.ReadBytes(ReadKind.Rom, _pickupOffset, 5 * 5);
        for (int i=0;i<5;i++)
        {
            if (pickups[i * 5 + 0] == 255)
            {
                // no more pickups
                break;
            }
            if (pickups[i * 5 + 0] == 0)
            {
                // ignore this pickup
                continue;
            }
            // Get coordinates :
            var yyyxxxxx = pickups[i * 5 + 1];
            var nnnnnnny = pickups[i * 5 + 2];
            var x= yyyxxxxx & 0x1f;
            var y= (yyyxxxxx >> 5) | ((nnnnnnny & 0x1) << 3);
            _pickups.Add(new Pickup { x = (byte)x, y = (byte)y, attr = pickups[i * 5 + 0] });
        }
        UpdateItemCount();
    }

    // Convert our simple list format to in memory format
    public void StorePickups()
    {
        byte[] pickups = new byte[5 * 5];
        int pickupOffset = 0;
        foreach (var pickup in _pickups)
        {
            pickups[pickupOffset++] = pickup.attr;                                      // Attribute
            pickups[pickupOffset++] = (byte)((pickup.y << 5) | (pickup.x & 0x1f));      // YYYXXXXX
            var topYBit = (pickup.y >> 3) & 1;
            pickups[pickupOffset++] = (byte)(topYBit | 0b01011100);                     // 0101110Y
            pickups[pickupOffset++] = (byte)((topYBit<<3) | 0b01100000);                // 0110Y000
            pickups[pickupOffset++] = 255;                                              // Always 255
        }
        if (pickupOffset<5*5)
        {
            // Mark end of list
            pickups[pickupOffset++] = 255;
        }
        for (int i=0;i<5;i++)
        {
            if (i < _pickups.Count)
            {
                var pickup = _pickups[i];
                pickups[i * 5 + 0] = pickup.attr;
                pickups[i * 5 + 1] = (byte)((pickup.y << 5) | (pickup.x & 0x1f));
                pickups[i * 5 + 2] = (byte)(pickup.y >> 3);
            }
            else
            {
                pickups[i * 5 + 0] = 0;
            }
        }
        _rom.WriteBytes(WriteKind.SerialisedRom, _pickupOffset, pickups);
    }
    
    private IWidgetLabel _itemCounter;

    public void SetItemCounterWidget(IWidgetLabel widget)
    {
        _itemCounter = widget;
        UpdateItemCount();
    }

    void DeletePickup(byte x, byte y)
    {
        for (int i=0;i<_pickups.Count;i++)
        {
            if (_pickups[i].x == x && _pickups[i].y == y)
            {
                _pickups.RemoveAt(i);
                UpdateItemCount();
                return;
            }
        }
    }

    // Valid colours are magenta=3, green=4, cyan=5, yellow=6 - for now just cycle it based on count
    private static readonly byte[] _pickupAttributes = new byte[] { 3, 4, 5, 6 };

    bool AddPickup(byte x, byte y)
    {
        if (_pickups.Count >= 5)
        {
            return false;
        }
        var pickupAttr = _pickupAttributes[_pickups.Count & 3];
        _pickups.Add(new Pickup { x = x, y = y, attr = pickupAttr });
        UpdateItemCount();
        return true;
    }

    void UpdateItemCount()
    {
        if (_itemCounter != null)
        {
            _itemCounter.Name = $"Items: {_pickups.Count} / 5";
        }
    }

    public ReadOnlySpan<uint> GetMapData()
    {
        return mapData;
    }

    public void SetTile(uint x, uint y, uint tile)
    {
        // If we overwrite a pickup, we need to update the pickup data
        if (mapData[y*Width+x]==8)
        {
            // Remove pickup from pickup data
            DeletePickup((byte)x, (byte)y);
        }
        // If we are adding a pickup, we need to update the pickup data
        if (tile == 8)
        {
            // Add pickup to pickup data
            if (!AddPickup((byte)x, (byte)y))
            {
                // Failed to add pickup - too many pickups in use
                return;
            }
        }
        else
        {
            // Regular tile, just update the map data
            _rom.WriteBytes(WriteKind.SerialisedRom, 0x4000 * 6 + 0 + y * Width + x, new byte[] { _tilePalette.IndexToAttr(tile) });
        }
        StorePickups(); // Update the pickup data
        mapData[y * Width + x] = tile;
    }
}

class ManicMinerTileMap : ITileMap
{
    public uint Width => 32 * 8;

    public uint Height => 16 * 8;

    public uint NumLayers => 1;

    public float ScaleX => 2.0f;

    public float ScaleY => 2.0f;

    private TilePaletteStore _tilePaletteStore;
    private ManicMinerTileMapLayer _layer;

    public ManicMinerTileMap(IMemoryAccess rom, uint offset, ManicMinerTilePalette tilePalette)
    {
        _tilePaletteStore = tilePalette.tilePaletteStore;
        _layer = new ManicMinerTileMapLayer(rom, offset, tilePalette);
    }

    public void SetItemCounterWidget(IWidgetLabel widget)
    {
        _layer.SetItemCounterWidget(widget);
    }

    public ILayer FetchLayer(uint layer)
    {
        return _layer;
    }

    public TilePaletteStore FetchPalette(uint layer)
    {
        return _tilePaletteStore;
    }
}

class ManicMinerTileEditor : IUserWindow
{
    public float UpdateInterval => 1 / 30.0f;

    private ManicMinerTilePalette tilePalette;
    private ManicMinerTileMap tileMap;

    public ManicMinerTileEditor(IMemoryAccess rom)
    {
        tilePalette = new ManicMinerTilePalette(rom);
        tileMap = new ManicMinerTileMap(rom, 0x4000 * 6 + 0, tilePalette);
    }

    public void ConfigureWidgets(IMemoryAccess rom, IWidget widget, IPlayerControls playerControls)
    {
        widget.AddLabel("Palette");
        widget.AddTilePaletteWidget(tilePalette.tilePaletteStore);
        tileMap.SetItemCounterWidget(widget.AddLabel("Items: 0 / 5"));
        widget.AddLabel("TileMap");
        widget.AddTileMapWidget(tileMap);
    }

    public void OnClose()
    {
        // Do nothing for now
    }
}
```

The way this works; is we detect if a pickup is added or deleted on the tile map, and adjust the list of pickups each time (see `ManicMinerTileMapLayer::SetTile`). For convenience, we convert the tilemap data from the rom format into a local array (see `ManicMinerTileMapLayer::GetPickups()`) and back to rom format as required.

A label widget is used to show a counter of how many items can be placed.

At this point, I have a challenge - Can you update the code so that you can edit any of the 20 levels?

_to be continued_

_under construction_