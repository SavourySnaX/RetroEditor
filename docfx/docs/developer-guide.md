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

_to be continued_

_under construction_