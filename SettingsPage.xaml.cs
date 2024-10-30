using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls.Shapes;
using ScriptingMaui.Resources.Strings;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.Picker;

namespace ScriptingMaui;

public partial class SettingsPage : ContentPage
{
    const string DefaultLearnVoice = "es-MX";
    const string MyDefaultVoice = "en-US";
    const string DefaultCode = "en";

    const string MyVoiceSet = "myVoice";
    const string ToLearnSet = "toLearn";
    const string PlaySet = "playrate";
    const string DelaySet = "delayrate";
    const string DelayOrigSet = "delayorigrate";
    const string SoundSet = "sound";
    const string QuizSet = "quiz46";
    const string PlaySecFormat = "{0}s";
    readonly CultureInfo OrigCulture = CultureInfo.CurrentUICulture;

    public static SettingsPage? Instance;

    public static string VoiceLearn { get; set; } = DefaultLearnVoice;
    public static string MyVoice { get; set; } = MyDefaultVoice;
    public static string LangCode { get; set; } = DefaultCode;

    public static double PlayRate { get; set; } = 3.0;
    public static double DelayRate { get; set; } = 0.0;
    public static bool Sound { get; set; } = true;
    public static bool DelayOriginal { get; set; } = true;
    public static bool Quiz6 { get; set; } = true;

    PickerColumn m_col1;
    PickerColumn m_col2;

    public SettingsPage()
    {
        Instance = this;
        InitializeComponent();
        var itemInfo = new SettingsInfo();
        //this.Picker.ItemTemplate = itemInfo.customView;

        /*m_col1 = new PickerColumn()
        {
            ItemsSource = itemInfo.DataSourceStudy,
            SelectedIndex = 0,
        };
        m_col2 = new PickerColumn()
        {
            ItemsSource = itemInfo.MyDataSource,
            SelectedIndex = 0,
        };
        LanguagePicker.Columns.Add(m_col2);
        LanguagePicker.Columns.Add(m_col1);
        LanguagePicker.SelectionChanged += My_SelectionChanged;*/

        myLanguagePicker.ItemsSource = itemInfo.MyDataSource;
        learnLanguagePicker.ItemsSource = itemInfo.DataSourceStudy;
        myLanguagePicker.SelectedIndexChanged += MyLanguagePicker_SelectedIndexChanged;
        learnLanguagePicker.SelectedIndexChanged += LearnLanguagePicker_SelectedIndexChanged; ;

        PlaySlider.ValueChanged += PlaySlider_ValueChanged;
        DelaySlider.ValueChanged += DelaySlider_ValueChanged;
        SoundCheck.CheckedChanged += SoundCheck_CheckedChanged;
        DelayOrigCheck.CheckedChanged += DelayOrigCheck_CheckedChanged;
        QuizSwitch.Toggled += QuizSwitch_Toggled;
        AboutBtn.Clicked += AboutBtn_Clicked;

        Setup();
    }

    private void QuizSwitch_Toggled(object? sender, ToggledEventArgs e)
    {
        Quiz6 = e.Value;
        Quiz46.Text = QuizSwitch.IsToggled ? "6" : "4";
        Preferences.Set(QuizSet, Quiz6);
    }

    private void LearnLanguagePicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var chosen = learnLanguagePicker.SelectedIndex;
        if (chosen < 0)
        {
            return;
        }
        VoiceLearn = Words.GetVoice(chosen);
        MainPage.Instance?.SetBackground(chosen);
        Preferences.Set(ToLearnSet, VoiceLearn);
        flagLearn.Source = Words.GetFlag(VoiceLearn);
        toLearn.Text = Words.Languages[chosen];

        learnLanguagePicker.SelectedItem = AppResources.Language_to_learn_;
    }

    private void MyLanguagePicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var chosen = myLanguagePicker.SelectedIndex;
        if (chosen < 0)
        {
            return;
        }
        MyVoice = Words.GetVoice(chosen);
        LangCode = Words.Codes[chosen];
        MainPage.Instance?.SetLanguage(LangCode);
        Preferences.Set(MyVoiceSet, MyVoice);
        flagMy.Source = Words.GetFlag(MyVoice);
        myLanguage.Text = Words.Languages[chosen];

        myLanguagePicker.SelectedItem = AppResources.Translate_to_;
    }

    async void AboutBtn_Clicked(object? sender, EventArgs e)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        var platform = DeviceInfo.Current.Platform == DevicePlatform.iOS ? "iOS" :
            DeviceInfo.Current.Platform == DevicePlatform.Android ? "Android" : "macOS";
        var refresh = Math.Round(DeviceDisplay.Current.MainDisplayInfo.RefreshRate, 0);

        sb.AppendLine($"Version: {AppInfo.Current.VersionString}");
        sb.AppendLine("");
        sb.AppendLine($"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Name}");
#if IOS
        var model = MauiUtils.GetiPhoneModel();
        sb.AppendLine($"{model}");
#endif
        sb.AppendLine($"OS: {platform} {DeviceInfo.Current.VersionString}");
        sb.AppendLine($"Size: {DeviceDisplay.Current.MainDisplayInfo.Width} x {DeviceDisplay.Current.MainDisplayInfo.Height}");
        sb.AppendLine($"Culture: {OrigCulture.Name}");
        sb.AppendLine($"Learning: {VoiceLearn} --> {MyVoice}");
        sb.AppendLine($"Refresh Rate: {refresh}");
        sb.AppendLine("");
        sb.AppendLine("Developed by Vassili - iLanguage.ch");

        await DisplayAlert(AppInfo.Current.Name, sb.ToString(), "OK");
    }

    public void SetSize()
    {
        var width = DeviceDisplay.Current.MainDisplayInfo.Width;
        if (width <= 750)
        {
            PlaySlider.WidthRequest = 120;
        }
        else if (width <= 900)
        {
            PlaySlider.WidthRequest = 130;
        }
        else if (width <= 1000)
        {
            PlaySlider.WidthRequest = 150;
        }
        else if (width <= 1200)
        {
            PlaySlider.WidthRequest = 165;
        }
        else
        {
            PlaySlider.WidthRequest = 180;
        }
    }

    public void Setup()
    {
        var mine = Preferences.Get(MyVoiceSet, "");
        var learn = Preferences.Get(ToLearnSet, "");
        Quiz6 = Preferences.Get(QuizSet, Quiz6);
        QuizSwitch.IsToggled = Quiz6;
        Quiz46.Text = QuizSwitch.IsToggled ? "6" : "4";
        var current = CultureInfo.CurrentUICulture;
        var lang2 = current.TwoLetterISOLanguageName;
        var code = current.Name;

        if (string.IsNullOrWhiteSpace(mine) || string.IsNullOrWhiteSpace(learn))
        {
            if (Words.Voices.Exists(x => x == code))
            {
                mine = code;
            }
            else if (Words.Voices.Exists(x => x.Substring(0, 2) == lang2))
            {
                mine = Words.Voices.Find(x => x.Substring(0, 2) == lang2);
            }
            else
            {
                mine = MyDefaultVoice;
            }
            learn = mine == MyDefaultVoice ? DefaultLearnVoice : MyDefaultVoice;
            Preferences.Set(MyVoiceSet, mine);
            Preferences.Set(ToLearnSet, learn);
        }
        var indMine = Words.Voices.IndexOf(mine);
        var indLearn = Words.Voices.IndexOf(learn);
        //m_col1.SelectedIndex = indLearn;
        //m_col2.SelectedIndex = indMine;
        myLanguagePicker.SelectedIndex = indMine;
        learnLanguagePicker.SelectedIndex = indLearn;

        myLanguage.Text = Words.Languages[indMine];
        toLearn.Text = Words.Languages[indLearn];

        flagLearn.Source = Words.GetFlag(VoiceLearn);
        flagMy.Source = Words.GetFlag(MyVoice);

        //MainPage.Instance?.SetBackground(indLearn);
        LangCode = Words.Codes[indMine];

        PlayRate = Preferences.Get(PlaySet, PlayRate);
        DelayRate = Preferences.Get(DelaySet, DelayRate);
        DelayOriginal = Preferences.Get(DelayOrigSet, DelayOriginal);
        DelayOrigLab.IsVisible = DelayOrigCheck.IsVisible = DelayRate > 0;

        Sound = Preferences.Get(SoundSet, true);

        PlaySlider.Value = PlayRate;
        DelaySlider.Value = DelayRate;
        SoundCheck.IsChecked = Sound;
        DelayOrigCheck.IsChecked = DelayOriginal;

        VoiceLearn = learn;
        MyVoice = mine;

        SetSize();
    }
    public void Localize()
    {
        //toLearn.Text = AppResources.Language_to_learn_;
        //myLanguage.Text = AppResources.Translate_to_;
        //m_col1.HeaderText = AppResources.Language_to_learn_;
        //m_col2.HeaderText = AppResources.Translate_to_;
        myLanguagePicker.Title = AppResources.Translate_to_;
        learnLanguagePicker.Title = AppResources.Language_to_learn_;

        SoundLab.Text = AppResources.Sound_;
        DelayLab.Text = AppResources.Translation_Delay_Rate_;
        DelayOrigLab.Text = AppResources.Delay_New_Word_;
        PlayrateLab.Text = AppResources.Play_Rate_;
        PlayrateSec.Text = string.Format(PlaySecFormat, PlayRate);
        DelaySec.Text = string.Format(PlaySecFormat, DelayRate);
        QuizLab.Text = AppResources.Quiz_Words_;
        Title = AppResources.Settings;
    }

    /*private void SpeechSlider_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        SpeechRate = e.NewValue;
        Preferences.Set(SpeechSet, SpeechRate);
    }*/
    private void PlaySlider_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        PlayRate = Math.Round(e.NewValue, 1);
        Preferences.Set(PlaySet, PlayRate);
        PlayrateSec.Text = string.Format(PlaySecFormat, PlayRate);
    }
    private void DelaySlider_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        DelayRate = Math.Round(e.NewValue, 1);
        Preferences.Set(DelaySet, DelayRate);
        DelaySec.Text = string.Format(PlaySecFormat, DelayRate);

        DelayOrigLab.IsVisible = DelayOrigCheck.IsVisible = DelayRate > 0;
    }

    public double GetPlayInterval()
    {
        //var minmax = PlaySlider.Minimum + PlaySlider.Maximum;
        //var result = minmax - PlaySlider.Value;
        //return result;
        return PlaySlider.Value;
    }
    private void SoundCheck_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        Sound = e.Value;
        Preferences.Set(SoundSet, Sound);
    }
    private void DelayOrigCheck_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        DelayOriginal = e.Value;
        Preferences.Set(DelayOrigSet, DelayOriginal);
    }

    void My_SelectionChanged(object? sender, PickerSelectionChangedEventArgs e)
    {
        if (e.NewValue < 0)
        {
            return;
        }
        if (e.ColumnIndex == 1)
        {
            VoiceLearn = Words.GetVoice(e.NewValue);
            MainPage.Instance?.SetBackground(e.NewValue);
            Preferences.Set(ToLearnSet, VoiceLearn);
            flagLearn.Source = Words.GetFlag(VoiceLearn);
        }
        else
        {
            MyVoice = Words.GetVoice(e.NewValue);
            LangCode = Words.Codes[e.NewValue];
            MainPage.Instance?.SetLanguage(LangCode);
            Preferences.Set(MyVoiceSet, MyVoice);
            flagMy.Source = Words.GetFlag(MyVoice);
        }
    }
}

public class SettingsInfo
{
    private ObservableCollection<object> dataSource1 = new ObservableCollection<object>(Words.Languages.ToArray());
    private ObservableCollection<object> dataSource2 = new ObservableCollection<object>(Words.Languages.ToArray());
    private ObservableCollection<object> flags = new ObservableCollection<object>(Words.Codes.ToArray());

    public ObservableCollection<object> DataSourceStudy
    {
        get
        {
            return dataSource1;
        }
        set
        {
            dataSource1 = value;
        }
    }
    public ObservableCollection<object> MyDataSource
    {
        get
        {
            return dataSource2;
        }
        set
        {
            dataSource2 = value;
        }
    }
    public ObservableCollection<object> LanguageFlags
    {
        get
        {
            return flags;
        }
        set
        {
            flags = value;
        }
    }

    public DataTemplate customView = new DataTemplate(() =>
    {
        Grid grid = new Grid
        {
            Padding = new Thickness(0, 1, 0, 1),
        };

        Label label = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Colors.Black,
        };

        label.SetBinding(Label.TextProperty, new Binding("LanguageFlags"));
        grid.Children.Add(label);
        return grid;
    });
}
