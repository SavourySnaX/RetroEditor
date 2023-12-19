# Retro Editor Thingy - Very much WIP
## About
Plugin based editor, for working on creating hacks of old games.

![Screenshot](screenshot.png)

## Current state

Not much more than a proof a concept at this point. It is capable of editing the ZX Spectrum game Jet Set Willy for the 48K spectrum, (can change the room layout tiles, but nothing more yet).
There is an initial tile image viewer for Phantasy Star 2 (Sega Megadrive/Genesis) (I have the sprites for maps mostly extracted, but not integrated here yet).
There is an initial (current focus) pass on extracting the level layouts from Rollercoaster (ZX Spectrum).
There is a remote mame debugger plugin (requires [Mame fork with branch for debugger support](https://github.com/SavourySnaX/mame/tree/rdebug)) - Note its currently exepcting to be debugging Rollercoaster. It should be started as shown [here](https://github.com/SavourySnaX/RetroEditor/blob/2f65b1a9cc33011dd8fa563737ccfcc9fae75e70/Source/Plugins/DebuggerPlugins/MameRemote.cs#L3)

## TODO

Everything - I havevn't even settled on plugin formats, configs, builtins etc. 
