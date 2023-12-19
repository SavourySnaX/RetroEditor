/*
var remote = new MameRemoteClient();
remote.Connect();

for (int a=0;a<32*192;a++)
{
    remote.SendMemory(16384+a, new byte[]{255});
}

remote.Disconnect();
*/
Console.OutputEncoding = System.Text.Encoding.UTF8;

var romPlugins = new IRomPlugin[] { new Megadrive(), new MasterSystem(), new ZXSpectrum() };
var plugins = new IRetroPlugin[] { new PhantasyStar2(), new JetSetWilly48(), new Rollercoaster() };

var render = new Editor(plugins,romPlugins);
render.RenderRun();


