/*

 The basis for roms that can be played....

*/

class PlayableRom
{
    private LibRetroPlugin plugin;

    public PlayableRom(LibRetroPlugin plugin)
    {
        this.plugin = plugin;
    }

    public bool Load(string filename)
    {

        return false;
    }
}