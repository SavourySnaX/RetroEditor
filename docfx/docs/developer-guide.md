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

From the entry point, we meet a bunch of __CALL__ instructions, but the one located at __0x01D7__ does not return. So we start again, this time we step into the call at __0x01D7__, and then continue forward, this time when reach __0x2981__, stepping over the intro runs, and then the main menu is shown, but when we press start, we find ourselves back in the debugger. So, if we were to knock out the call at __0x2981__ then in theory in our plugin, the game will start on the first level straight away. 

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
        romAccess.WriteBytes(WriteKind.TemporaryRom, 0x2981, new byte[] { 0x00, 0x00, 0x00 });s
    }
```

save the file, then go back to the editor and select __Window__->__<project_name>__->__Reload Plugin__. Now the player window should re-open and gameplay should start at level 1 without showing the intro/menu.

### Adding a level select cheat

As great as starting at level 1 is, when editing it is helpful if the editor can skip to the level they are currently working. In order to do this, we need to figure out where in the code levels are setup, fortunately this will also help us figure out where the level data is for editor.

_to be continued_

_under construction_