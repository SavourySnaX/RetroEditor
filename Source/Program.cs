Console.OutputEncoding = System.Text.Encoding.UTF8;

var romPlugins = new IRomPlugin[] { new ZXSpectrum(), new Megadrive(), new NintendEntertainmentSystem() };
var plugins = new IRetroPlugin[] { new JetSetWilly48(), new Rollercoaster(), new Fairlight(), new PhantasyStar2(), new Metroid() };

var render = new Editor(plugins,romPlugins);
render.RenderRun();
