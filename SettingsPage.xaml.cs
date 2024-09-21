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
    const string SpeechSet = "speechrate";
    const string SoundSet = "sound";
    const string PlaySecFormat = "{0}s";

    public static SettingsPage? Instance;

    public static string VoiceLearn { get; set; } = DefaultLearnVoice;
    public static string MyVoice { get; set; } = MyDefaultVoice;
    public static string LangCode { get; set; } = DefaultCode;

    public static double PlayRate { get; set; } = 3.0;
    //public static double SpeechRate { get; set; } = 50;
    public static bool Sound { get; set; } = true;

    PickerColumn m_col1;
    PickerColumn m_col2;

    public SettingsPage()
	{
        Instance = this;
        InitializeComponent();
        var itemInfo = new SettingsInfo();
        //this.Picker.ItemTemplate = itemInfo.customView;

        m_col1 = new PickerColumn()
        {            
            ItemsSource = itemInfo.DataSourceStudy,
            SelectedIndex = 0,
        };
        m_col2 = new PickerColumn()
        {
            ItemsSource = itemInfo.MyDataSource,
            SelectedIndex = 0,
        };

        LanguagePicker.Columns.Add(m_col1);
        LanguagePicker.Columns.Add(m_col2);
        LanguagePicker.SelectionChanged += My_SelectionChanged;

        PlaySlider.ValueChanged += PlaySlider_ValueChanged;
        //SpeechSlider.ValueChanged += SpeechSlider_ValueChanged;
        SoundCheck.CheckedChanged += SoundCheck_CheckedChanged;

        Setup();
    }

    public void Setup()
    {
        var mine = Preferences.Get(MyVoiceSet, "");
        var learn = Preferences.Get(ToLearnSet, "");
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
        m_col1.SelectedIndex = indLearn;
        m_col2.SelectedIndex = indMine;

        flagLearn.Source = Words.GetFlag(VoiceLearn);
        flagMy.Source = Words.GetFlag(MyVoice);

        //MainPage.Instance?.SetBackground(indLearn);
        LangCode = Words.Codes[indMine];

        PlayRate = Preferences.Get(PlaySet, PlayRate);
        //SpeechRate = Preferences.Get(SpeechSet, SpeechRate);
        Sound = Preferences.Get(SoundSet, true);

        PlaySlider.Value = PlayRate;
        //SpeechSlider.Value = SpeechRate;
        SoundCheck.IsChecked = Sound;
    }
    public void Localize()
    {
        toLearn.Text = AppResources.Language_to_learn_;
        myLanguage.Text = AppResources.Translate_to_;
        m_col1.HeaderText = AppResources.Language_to_learn_;
        m_col2.HeaderText = AppResources.Translate_to_;
        SoundLab.Text = AppResources.Sound_;
        //SpeechrateLab.Text = AppResources.Speech_Rate_;
        PlayrateLab.Text = AppResources.Play_Rate_;
        PlayrateSec.Text = string.Format(PlaySecFormat, PlayRate);
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

    void My_SelectionChanged(object? sender, PickerSelectionChangedEventArgs e)
    {
        if (e.NewValue < 0)
        {
            return;
        }
        if (e.ColumnIndex == 0)
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
            //await TTS.Speak("Hello lol. Como estas?", voice);
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
