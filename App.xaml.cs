namespace ScriptingMaui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new ScriptingMaui.MainPage();
			//new AppShell();
	}
}

