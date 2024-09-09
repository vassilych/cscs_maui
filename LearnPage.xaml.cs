using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui;
//using ObjCRuntime;
using ScriptingMaui.Resources.Strings;
using SplitAndMerge;
using Syncfusion.Maui.ListView;
using Syncfusion.Maui.Picker;

namespace ScriptingMaui;

public partial class LearnPage : ContentPage
{
    const string CategorySet = "categoryLearn";
    const string WordSet = "wordLearn";
    int MaxSearchResults = 6;
    Category m_category;
    bool m_settingWord;

    Dictionary<string, List<string>> m_words = new Dictionary<string, List<string>>();

    bool m_playing;
    IDispatcherTimer? m_timer;
    static public LearnPage Instance;

    public static Context Context {
        get;
        set; }

    public LearnPage()
    {
        Instance = this;
        InitializeComponent();

        Context = Context == null ? new Context() : Context;

        CategoryPicker.ItemsSource = Context.DataSourceCategories;
        int index = Preferences.Get(CategorySet, 0);
        CategoryPicker.SelectedIndex = index;
        CategoryPicker.SelectedIndexChanged += CategorySelectionChanged;

        m_category = Categories.GetCategory(index);
        PrevImg.Clicked += PrevImgClick;
        NextImg.Clicked += NextImgClick;
        MainImgImg.Clicked += NextImgClick;
        MainImgTxt.Clicked += NextImgClick;
        ButSpeak.Clicked += SpeakerClick;
        ButPlay.Clicked += PlayClick;

        FindBut.Clicked += FindBut_Clicked;
        SearchEntry.TextChanged += SearchEntry_TextChanged;
        SearchEntry.Completed += BackBut_Clicked;
        BackBut.Clicked += BackBut_Clicked;

        TranslationView.ItemTapped += TranslationView_ItemTapped;
        ResultsView.ItemTapped += ResultsView_ItemTapped;

        TranslationBtn.Clicked += TranslationBtn_Clicked;
        TranslationFlag.Clicked += TranslationBtn_Clicked;
        //this.Loaded += LearnPage_Loaded;
    }

    async void ResultsView_ItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        var item = e.DataItem as TranslationInfo;
        if (item == null)
        {
            return;
        }
        var id = item.Id;
        var word = Categories.DefaultCategory.Words[id];
        m_category = word.Category;
        SetMode(false);
        await SetWord(word);
    }

    async void TranslationView_ItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        var item = e.DataItem as TranslationInfo;
        if (item == null )
        {
            return;
        }
        var voice = item.TransVoice;
        var trans = Context.Word.GetTranslation(voice);
        await TTS.Speak(trans, voice);
    }

    public static int GetFontSize(string text)
    {
        if (text.Length < 16)
        {
            return 18;
        }
        else if (text.Length < 20)
        {
            return 17;
        }
        else if (text.Length < 24)
        {
            return 16;
        }
        else if (text.Length < 28)
        {
            return 15;
        }
        else if (text.Length < 32)
        {
            return 14;
        }
        else if (text.Length < 36)
        {
            return 13;
        }
        return 12;
    }

    async void TranslationBtn_Clicked(object? sender, EventArgs e)
    {
        var trans = TranslationBtn.Text;
        await TTS.Speak(trans, SettingsPage.MyVoice);
    }

    private void BackBut_Clicked(object? sender, EventArgs e)
    {
        SetMode(false);
    }
    private void FindBut_Clicked(object? sender, EventArgs e)
    {
        StopPlay();
        SearchEntry.Text = string.Empty;
        Context.ResultsInfo.Clear();
        SetMode(true);
        SearchEntry.Focus();
    }

    private void SearchEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var current = SearchEntry.Text;
        if (string.IsNullOrWhiteSpace(current))
        {
            Context.ResultsInfo.Clear();
            return;            
        }

        var words1 = GetWords(SettingsPage.VoiceLearn);
        var words2 = GetWords(SettingsPage.MyVoice);
        List<WordHint> results1 = new List<WordHint>();
        List<WordHint> results2 = new List<WordHint>();

        Trie search1 = new Trie(words1);
        search1.Search(current, MaxSearchResults, results1);

        Trie search2 = new Trie(words2);
        search2.Search(current, MaxSearchResults, results2);

        if (results1.Count == 0 && results2.Count == 0)
        {
            ResultsView.IsVisible = false;
            return;
        }
        ResultsView.IsVisible = true;
        Context.GenerateSearchInfo(results1, SettingsPage.VoiceLearn);
        Context.GenerateSearchInfo(results2, SettingsPage.MyVoice, false);
    }

    List<string> GetWords(string voice)
    {
        if (!m_words.TryGetValue(voice, out List<string>? words))
        {
            words = new List<string>();
            var all = Categories.DefaultCategory.Words;
            foreach (var word in all)
            {
                var trans = word.GetTranslation(voice);
                words.Add(trans);
            }
            m_words[voice] = words;
        }
        return words;
    }

    public async Task Setup()
    {
        await SetInitWord();
        SetMode();
    }
    async Task SetInitWord()
    {
        var index = Preferences.Get(WordSet, 0);
        var word = m_category.GetWord(index);
        m_category = word.Category;
        await SetWord(word, false);

        var categoryIndex = Categories.GetCategoryIndex(word.Category.Name);
        CategoryPicker.SelectedIndex = categoryIndex;
    }

    void SetMode(bool findMode = false)
    {
        SearchPanel.IsVisible = findMode;
        ResultsView.IsVisible = findMode;
        BackBut.IsVisible = findMode;
        SearchEntry.IsEnabled = findMode;

        TopPanel.IsVisible = !findMode;
        TopMainPanel.IsVisible = !findMode;
        MainPanel.IsVisible = !findMode;
        ButtonsPanel.IsVisible = !findMode;
        TranslationView.IsVisible = !findMode;
        TranslationBtn.IsVisible = !findMode;
        TranslationFlag.IsVisible = !findMode;
    }

    public void Localize()
    {
        CategoryPicker.Title = AppResources.SelectCategory;
        CategoryPicker.ItemsSource = Context.TranslatedCategories();
        WordId.Text = string.Format(AppResources.Word__0___1_, (m_category.Index + 1), m_category.GetTotalWords());
        Title = AppResources.Learn;
    }

    private async void PlayClick(object? sender, EventArgs e)
    {
        m_playing = !m_playing;
        if (m_playing)
        {
            ButPlay.Source = "stop_but.png";
            var word = m_category.GetNextWord();
            await SetWord(word);
            m_timer?.Stop();
            m_timer = Application.Current?.Dispatcher.CreateTimer();
            m_timer.Interval = TimeSpan.FromSeconds(SettingsPage.Instance.GetPlayInterval());
            m_timer.IsRepeating = true;
            m_timer.Tick += (s, e) => OnPlayTimer();
            m_timer.Start();
        }
        else
        {
            StopPlay();
        }
    }
    public void StopPlay()
    {
        m_playing = false;
        m_timer?.Stop();
        ButPlay.Source = "play_but.png";
    }
    private void OnPlayTimer()
    {
        if (!m_playing)
        {
            return;
        }
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var word = m_category.GetNextWord();
            await SetWord(word);
        });
    }

    async Task SetWord(Word word, bool speak = true)
    {
        if (m_settingWord)
        {
            return;
        }
        m_settingWord = true;
        Context.SetWord(word);
        m_category = word.Category;
        var ind = Categories.GetCategoryIndex(m_category.Name);
        if (ind < 0)
        {
            ind = 0;
        }
        var wordIndex = m_category.GetIndex(word);
        m_category.Index = wordIndex;
        Preferences.Set(WordSet, wordIndex);
        Preferences.Set(CategorySet, ind);
        CategoryPicker.SelectedIndex = ind;
        if (m_category.IsText)
        {
            MainImgTxt.Text = Context.MainWord;
            MainImgTxt.IsVisible = true;
            MainImgImg.IsVisible = false;
            MainImgTxt.FontSize = GetFontSize(Context.MainWord) + 2;
            MainWordLabel.Text = "";
        }
        else
        {
            MainImgImg.Source = word.GetImage();
            MainImgImg.IsVisible = true;
            MainImgTxt.IsVisible = false;
            MainWordLabel.Text = Context.MainWord;
        }
        WordId.Text = string.Format(AppResources.Word__0___1_, (m_category.Index+1), m_category.GetTotalWords());
        TranslationFlag.Source = Word.GetFlag(SettingsPage.MyVoice);
        TranslationBtn.Text = word.GetTranslation(SettingsPage.MyVoice);
        TranslationBtn.FontSize = GetFontSize(TranslationBtn.Text) - 1;

        if (speak)
        {
            await TTS.Speak(Context.MainWord, SettingsPage.VoiceLearn);
        }
        m_settingWord = false;
    }

    private async void PrevImgClick(object? sender, EventArgs e)
    {
        StopPlay();
        var word = m_category.GetPrevWord();
        await SetWord(word);
    }
    private async void NextImgClick(object? sender, EventArgs e)
    {
        StopPlay();
        var word = m_category.GetNextWord();
        await SetWord(word);
    }
    private async void SpeakerClick(object? sender, EventArgs e)
    {
        await TTS.Speak(Context.MainWord, SettingsPage.VoiceLearn, true);
    }

    private async void CategorySelectionChanged(object? sender, EventArgs e)
    {
        StopPlay();
        var chosen = CategoryPicker.SelectedIndex;
        if (chosen < 0)
        {
            return;
        }
        Preferences.Set(CategorySet, chosen);

        m_category = Categories.SwitchCategory(chosen);

        var word = m_category.GetWord();
        await SetWord(word);
    }
}

public class TranslationInfo : INotifyPropertyChanged
{
    private string transName = "";
    private string transFlag = "";
    private string transVoice = "";
    private double fontSize = 18;
    public int Id { get; set; }

    public string TransName
    {
        get { return transName; }
        set
        {
            transName = value;
            OnPropertyChanged("TransName");
        }
    }
    public override string ToString()
    {
        return transName + " " + transVoice;
    }

    public string TransFlag
    {
        get { return transFlag; }
        set
        {
            transFlag = value;
            OnPropertyChanged("TransFlag");
        }
    }
    public string TransVoice
    {
        get { return transVoice; }
        set { transVoice = value; }
    }
    public double FontSize
    {
        get { return fontSize; }
        set { fontSize = value; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged(string name)
    {
        if (this.PropertyChanged != null)
            this.PropertyChanged(this, new PropertyChangedEventArgs(name));
    }
}
public class Context
{
    private ObservableCollection<string> categоries = new ObservableCollection<string>();
    private ObservableCollection<object> transInfo = new ObservableCollection<object>();
    private ObservableCollection<object> resultsInfo = new ObservableCollection<object>();
    public Word Word { get; set; }

    public string MainWord { get; set; } = "";

    public ObservableCollection<string> DataSourceCategories
    {
        get
        {
            categоries = new ObservableCollection<string>(TranslatedCategories());
            return categоries;
        }
        private set
        {
            categоries = value;
        }
    }
    public static List<string> TranslatedCategories()
    {
        List<string> cat = new List<string>();
        cat.Add(AppResources.all);
        cat.Add(AppResources.animal_world);
        cat.Add(AppResources.body_parts);
        cat.Add(AppResources.numbers_and_math);
        cat.Add(AppResources.calendar__time);
        cat.Add(AppResources.transport);
        cat.Add(AppResources.sport);
        cat.Add(AppResources.hobby__music__chess);
        cat.Add(AppResources.fruits_and_vegetables);
        cat.Add(AppResources.food_and_beverage);
        cat.Add(AppResources.household);
        cat.Add(AppResources.bed_and_bath);
        cat.Add(AppResources.tools_and_appliances);
        cat.Add(AppResources.kitchen);
        cat.Add(AppResources.clothing);
        cat.Add(AppResources.school_and_office);
        cat.Add(AppResources.colors__figures);
        cat.Add(AppResources.accesories);
        cat.Add(AppResources.family_and_kids);
        cat.Add(AppResources.profession);
        cat.Add(AppResources.countries);
        cat.Add(AppResources.environment);
        cat.Add(AppResources.verbs);
        cat.Add(AppResources.adjectives);
        cat.Add(AppResources.phrases_greetings);
        cat.Add(AppResources.basic_phrases);
        cat.Add(AppResources.phrases_traveling);
        cat.Add(AppResources.phrases_in_hotel);
        cat.Add(AppResources.phrases_in_restaurant);
        cat.Add(AppResources.flirting_phrases);
        return cat;
    }

    public Context()
    {
        LearnPage.Context = this;
        Word = Word.Default;
    }
    public ObservableCollection<object> TransInfo
    {
        get { return transInfo; }
        set { this.transInfo = value; }
    }
    public ObservableCollection<object> ResultsInfo
    {
        get { return resultsInfo; }
        set { this.resultsInfo = value; }
    }
    public string GetVoice(int index)
    {
        return ((TranslationInfo)transInfo[index]).TransVoice;
    }

    internal void SetWord(Word word)
    {
        GenerateTransInfo(word);
        Word = word;
        MainWord = word.GetTranslation(SettingsPage.VoiceLearn);

        Variable data = new Variable(Variable.VarType.ARRAY);
        data.Tuple.Add(new Variable("lol"));
        data.Tuple.Add(new Variable("lala"));

        MainPage.Instance.Scripting.UpdateValue(nameof(LearnPage.Context.TransInfo), data);

    }

    internal void GenerateTransInfo(Word word)
    {
        transInfo.Clear();
        var voiceOrder = Words.VoiceOrder;

        foreach (var voice in voiceOrder)
        {
            var trans = word.Translation[voice];
            var filename = Word.GetFlag(voice);
            var item = new TranslationInfo() {
                TransName = trans, TransFlag = filename, TransVoice = voice,
                FontSize = LearnPage.GetFontSize(trans) };
            if (voice == SettingsPage.MyVoice)
            {
                //transInfo.Insert(0, item);
                continue;
            }
            else
            {
                transInfo.Add(item);
            }
        }
    }
    internal void GenerateSearchInfo(List<WordHint> results, string voice, bool clean = true)
    {
        if (clean)
        {
            resultsInfo.Clear();
        }
        var filename = voice.Replace("-", "_").ToLower() + ".png";
        if (filename.StartsWith("transcript_"))
        {
            filename = filename.Substring(11);
        }
        foreach (var word in results)
        {
            resultsInfo.Add(new TranslationInfo()
            {
                TransName = word.OriginalText,
                TransFlag = filename,
                TransVoice = voice,
                Id = word.Id,
                FontSize = LearnPage.GetFontSize(word.OriginalText)
            });
        }
    }
}
