Console.OutputEncoding = System.Text.Encoding.UTF8;

//var romPlugins = new IRomPlugin[] { new Megadrive(), new MasterSystem(), new ZXSpectrum() };
//var plugins = new IRetroPlugin[] { new PhantasyStar2(), new JetSetWilly48(), new Rollercoaster(), new Fairlight() };
var romPlugins = new IRomPlugin[] { new ZXSpectrum() };
var plugins = new IRetroPlugin[] { new JetSetWilly48() };
//var plugins = new IRetroPlugin[] {  };

var render = new Editor(plugins,romPlugins);
render.RenderRun();
