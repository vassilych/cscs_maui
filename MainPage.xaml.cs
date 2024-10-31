
using System.Globalization;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using ScriptingMaui.Resources.Strings;

namespace ScriptingMaui;

public partial class MainPage : TabbedPage
{
	public static MainPage? Instance;
	List<ContentPage> m_pages = new List<ContentPage>();
    public Scripting Scripting { get; private set; } = new Scripting();

    public MainPage()
	{
        Instance = this;
        InitializeComponent();

        foreach (var page in Children)
		{
			var contentPage = page as ContentPage;
			if (contentPage != null)
			{
                m_pages.Add(contentPage);
            }
        }

        SetLanguage(SettingsPage.LangCode);
        Scripting.Init(m_pages);

        var firstTime = Preferences.Get("firstTime", true);
        if (firstTime)
        {
            Preferences.Set("firstTime", false);
            CurrentPage = Children.Last();
        }

        this.PropertyChanged += MainPage_PropertyChanged;

        this.Loaded += MainPage_Loaded;
    }

    private void MainPage_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName) &&
            e.PropertyName.Equals("CurrentPage", StringComparison.OrdinalIgnoreCase))
        {
            LearnPage.Instance.StopPlay();
        }
    }

    private async void MainPage_Loaded(object? sender, EventArgs e)
    {
        var pageLearn = this.Children[0] as LearnPage;
        await pageLearn.Setup();

        var indLearn = Words.Voices.IndexOf(SettingsPage.VoiceLearn);
        MainPage.Instance?.SetBackground(indLearn);

        await Scripting.StartAsync();
    }

    public void SetPage(Page page)
    {
        CurrentPage = page;
    }

    public void SetupLanguage()
    {
        var current = CultureInfo.CurrentUICulture;
        var lang2 = current.TwoLetterISOLanguageName;
        var code = current.Name;

        if (Words.Voices.Exists(x => x == code))
        {
            SetLanguage(code);
        }
        else if (Words.Voices.Exists(x => x.Substring(0, 2) == lang2))
        {
            SetLanguage(lang2);
        }
        else
        {
            SetLanguage("en-US");
        }
    }

    public void SetLanguage(string languageCode)
    {
        CultureInfo newCulture = new CultureInfo(languageCode);
        CultureInfo.DefaultThreadCurrentCulture = newCulture;
        CultureInfo.DefaultThreadCurrentUICulture = newCulture;
        CultureInfo.CurrentUICulture = newCulture;

        Localize();
    }

    public void SetBackground(int index)
	{
        var countryImg = Words.Countries[index] + "_bg.png";
        foreach (var page in m_pages)
        {
			page.BackgroundImageSource = countryImg;
        }
    }
    public void Localize()
    {
        foreach (var page in m_pages)
        {
            if (page is LearnPage)
            {
                (page as LearnPage).Localize();
            }
            else if (page is QuizPage)
            {
                (page as QuizPage).Localize();
            }
            else if (page is SearchPage)
            {
                (page as SearchPage).Localize();
            }
            else if (page is SettingsPage)
            {
                (page as SettingsPage).Localize();
            }
        }
    }
}
