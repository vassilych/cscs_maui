using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.Shapes;
using ScriptingMaui.Resources.Strings;
using SplitAndMerge;
using Syncfusion.Maui.ListView;
using Syncfusion.Maui.ListView.Helpers;
using Syncfusion.Maui.Picker;
using System.Threading;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Configuration;
using CommunityToolkit.Maui.Media;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Reflection;

namespace ScriptingMaui;

public partial class LearnPage : ContentPage
{
    const string CategorySet = "categoryLearn";
    const string WordSet = "wordLearn";
    const string CategoryWord = "word_";
    const string CustomWords = "customWords";
    const string TrashWords = "trashWords";
    const string LearnedWords = "learnedhWords";

    const string AddWordIcon = "add_word.png";
    const string DeleteWordIcon = "delete_word.png";

    int MaxSearchResults = 7;
    double m_trasnlationViewY = -1;
    Category m_category;
    bool m_settingWord;

    List<string> m_conjVerbs = new List<string>();

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
        int catIndex = Preferences.Get(CategorySet, 0);
        m_category = Categories.GetCategory(catIndex);

        CategoryPicker.SelectedIndex = catIndex;
        CategoryPicker.SelectedIndexChanged += CategorySelectionChanged;

        WordId.Clicked += WordId_Clicked;

        PrevImg.Clicked += PrevImgClick;
        NextImg.Clicked += NextImgClick;
        MainImgImg.Clicked += NextImgClick;
        MainImgTxt.Clicked += NextImgClick;
        //ButSpeak.Clicked += SpeakerClick;
        MainWordText.Clicked += SpeakerClick;
        ButPlay.Clicked += PlayClick;

        ButOk.Clicked += BackBut_Clicked;
        ButAdd.Clicked += ButAdd_Clicked;
        ButDel.Clicked += ButDel_Clicked;
        ButLearned.Clicked += ButLearned_Clicked;

        TranslationView.ItemTapped += TranslationView_ItemTapped;

        TranslationBtn.Clicked += TranslationBtn_Clicked;
        TranslationFlag.Clicked += TranslationBtn_Clicked;

        ButInfo.Clicked += InfoData_Clicked;
        SetMode();
        //this.Loaded += LearnPage_Loaded;
    }

    async void WordId_Clicked(object? sender, EventArgs e)
    {
        if (m_category == null || m_category.CatWords.Count == 0)
        {
            m_category = Categories.DefaultCategory;
        }
        var word = m_category.GetWord(0);
        await SetWord(word);
    }

    async void ButLearned_Clicked(object? sender, EventArgs e)
    {
        Categories.LearnedCategory.AddWord(Context.Word);
        Context.Word.Category.MoveToSpecialCategory(Context.Word);
        Preferences.Set(LearnedWords, Categories.LearnedCategory.GetAllWords());

        ButLearnedBorder.IsVisible = false;
        await SetSomeWord(true);
        await Toast.Make(string.Format(AppResources.Word_AddedTo__0_, AppResources.learned_words), ToastDuration.Long).Show();
    }

    async void ButDel_Clicked(object? sender, EventArgs e)
    {
        string toast = "";
        var lastCat = Context.Word.Category;
        if (m_category == Categories.CustomCategory)
        {
            m_category.RemoveWord(Context.Word);
            toast = AppResources.word_deleted;
            Preferences.Set(CustomWords, m_category.GetAllWords());
        }
        else if (m_category == Categories.TrashCategory || m_category == Categories.LearnedCategory)
        {
            m_category.RemoveWord(Context.Word);
            Context.Word.Category.AddFromSpecialCategory(Context.Word);
            toast = string.Format(AppResources.Word_AddedTo__0_, Context.TranslateCategory(Context.Word.Category));
            var pref = m_category == Categories.TrashCategory ? TrashWords : LearnedWords;
            Preferences.Set(pref, m_category.GetAllWords());
        }
        else
        {
            Categories.TrashCategory.AddWord(Context.Word);
            Context.Word.Category.MoveToSpecialCategory(Context.Word);
            toast = string.Format(AppResources.Word_AddedTo__0_, AppResources.trash);
            Preferences.Set(TrashWords, Categories.TrashCategory.GetAllWords());
        }
        ButDelBorder.IsVisible = false;

        await SetSomeWord(true);
        await Toast.Make(toast, ToastDuration.Long).Show();
    }

    async Task SetSomeWord(bool moveNext = false)
    {
        var word = moveNext? m_category.GetNextWord() : m_category.GetWord(m_category.Index);
        if (word != null)
        {
            await SetWord(word);
        }
        else if (Context?.Word != null)
        {
            m_category = Context.Word.Category;
            await SetWord(Context.Word);
        }
        else
        {
            await SetInitWord();
        }
    }

    async void ButAdd_Clicked(object? sender, EventArgs e)
    {
        if (m_category == Categories.CustomCategory || m_category == Categories.TrashCategory ||
            m_category == Categories.LearnedCategory)
        {
            var ok = await DisplayAlert(AppInfo.Current.Name, AppResources.Put_back_all_words_, "OK", "Cancel");
            if (!ok)
            {
                return;
            }
            var words = m_category.CatWords.ToArray();
            foreach (var word in words)
            {
                m_category.RemoveWord(word);
                if (m_category == Categories.TrashCategory || m_category == Categories.LearnedCategory)
                {
                    word.Category.AddFromSpecialCategory(word);
                }
            }
            var pref = m_category == Categories.TrashCategory ? TrashWords :
                       m_category == Categories.CustomCategory ? CustomWords : LearnedWords;
            Preferences.Set(pref, m_category.GetAllWords());
            await SetSomeWord(true);
        }
        else if (!Categories.CustomCategory.Exists(Context.Word.Name))
        {
            Categories.CustomCategory.AddWord(Context.Word);
            ButAddBorder.IsVisible = false;
            await Toast.Make(AppResources.word_added, ToastDuration.Short).Show();
            Preferences.Set(CustomWords, Categories.CustomCategory.GetAllWords());
        }
    }

    public void SetTranslation(string text)
    {
        TranslationBtn.Text = text;
        TranslationBtn.FontSize = GetFontSize(TranslationBtn.Text) + 1;
    }
    public void SetTranslationView()
    {
        try
        {
            if (m_trasnlationViewY > 0)
            {
                TranslationView.ScrollTo(m_trasnlationViewY);
            }
        }
        catch (Exception) { }
    }
    public void SetTranslationWait()
    {
        TranslationBtn.Text += ".";
    }
    public void SetTranslationOrigWait()
    {
        MainWordText.Text += ".";
    }
    public void SetOrigTranslation(Word word, string text)
    {
        if (word.Category.IsText)
        {
            MainImgTxt.Text = text;
            MainImgTxt.FontSize = GetFontSize(text) + 2;
            MainWordText.Text = "";
        }
        else
        {
            MainWordText.Text = text;
        }
    }

    async void ResultsView_ItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        var item = e.DataItem as TranslationInfo;
        if (item == null)
        {
            return;
        }
        var id = item.Id;
        var word = Categories.DefaultCategory.CatWords[id];
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

    public static void SetSize(Label l, bool isHeader = false)
    {
        //var curr = GetFontSize(l.Text);
        //l.FontSize = curr + delta;
        l.FontSize = isHeader && l.Text.Length >= 15 ? 13 : 14;
       // l.LineHeight = l.Text.Length >= 15 ? 2 : 1;

        var width = DeviceDisplay.Current.MainDisplayInfo.Width;
        if (width <= 650)
        {
            l.WidthRequest = 96;
        }
        else if (width <= 750)
        {
            l.WidthRequest = 110;
        }
        else if (width <= 900)
        {
            l.WidthRequest = 112;
        }
        else if (width <= 1000)
        {
            l.WidthRequest = 114;
        }
        else if (width <= 1200)
        {
            l.WidthRequest = 116;
        }
        else
        {
            l.WidthRequest = 125;
        }
    }
    public static void SetSize(Button btn, double delta = 0)
    {
        var curr = GetFontSize(btn.Text);
        btn.FontSize = curr + delta;
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
        if(!string.IsNullOrWhiteSpace(trans) && !trans.StartsWith("."))
        {
            await TTS.Speak(trans, SettingsPage.MyVoice);
            //await TestSpeech();
        }
    }

    async void BackBut_Clicked(object? sender, EventArgs e)
    {
        var isDone = await Conjugate();
        if (isDone)
        {
            SetMode(false);
        }
    }

    public async Task Setup()
    {
        var customWords = Preferences.Get(CustomWords, "");
        Categories.CustomCategory.InitFromWords(customWords);

        var trashWords = Preferences.Get(TrashWords, "");
        Categories.TrashCategory.InitFromWords(trashWords);

        var learnedWords = Preferences.Get(LearnedWords, "");
        Categories.LearnedCategory.InitFromWords(learnedWords);

        await SetInitWord();
        SetMode();
    }
    async Task SetInitWord()
    {
        int catIndex = Preferences.Get(CategorySet, 0);
        m_category = Categories.GetCategory(catIndex);

        var index = Preferences.Get(WordSet, 0);
        var word = m_category.GetWord(index);
        if (word == null)
        {
            m_category = Categories.DefaultCategory;
            word = m_category.GetWord(0);
        }
        await SetWord(word, false);

        //var categoryIndex = Categories.GetCategoryIndex(word.Category.Name);
        //CategoryPicker.SelectedIndex = categoryIndex;
    }

    public async Task<string> _TestSpeech(string voice = "en-US")
    {
        var isGranted = await SpeechToText.Default.RequestPermissions();
        if (!isGranted)
        {
            await Toast.Make("Permission not granted").Show(CancellationToken.None);
            return "";
        }
        string RecognitionText = "";
        SpeechToTextResult recognitionResult = null;
        string error = "";
        try
        {
            recognitionResult = await SpeechToText.Default.ListenAsync(
                                        CultureInfo.GetCultureInfo(voice),
                                        new Progress<string>(partialText =>
                                        {
                                            RecognitionText += partialText + " ";
                                        }));
            recognitionResult.EnsureSuccess();
        }
        catch(Exception exc)
        {
            error += exc.Message;
        }
        if (recognitionResult == null || !recognitionResult.IsSuccessful || !string.IsNullOrWhiteSpace(error))
        {
            await Toast.Make("Unable to recognize speech: " + error, ToastDuration.Long).Show();
            return "";
        }

        RecognitionText = recognitionResult.Text;
        await Toast.Make("Recognized: " + RecognitionText, ToastDuration.Long).Show();
        return RecognitionText;
    }

    void SetMode(bool findMode = false, bool infoMode = false)
    {
        SearchPanel.IsVisible = findMode || infoMode;
        ButOk.IsVisible = findMode || infoMode;
        WordName.IsVisible = infoMode;
        WordDetails.IsVisible = infoMode;

        TopPanel.IsVisible = TopMainPanel.IsVisible = !findMode && !infoMode;
        TopMainPanel.IsVisible = !findMode && !infoMode;
        MainPanel.IsVisible = !findMode && !infoMode;
        ButtonsPanel.IsVisible = !findMode && !infoMode;
        TranslationView.IsVisible = TranslationFlag.IsVisible = TranslationBtn.IsVisible =
            TranslationBtnBorder.IsVisible = !findMode && !infoMode;
        //ButSpeak.IsVisible = !findMode && !infoMode;
        MainWordBorder.IsVisible = !findMode && !infoMode;

        InfoPanelScroll.IsVisible = InfoPanel.IsVisible = infoMode;
        //VerbInfoH1.IsVisible = VerbInfoH2.IsVisible = VerbInfoH3.IsVisible = infoMode;
        //VerbInfo11.IsVisible = VerbInfo21.IsVisible = VerbInfo31.IsVisible = infoMode;
    }

    string GetWordData(string langPrefix, string candidate)
    {
        var ww = candidate.Split(new char[] { ' ' }, (StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        string key = //prefix1.StartsWith("en") ? "en_" + ww[0] + " " + ww[1] :
                     langPrefix.StartsWith("es") ? "es_" + ww[0] :
                     langPrefix + "_" + candidate;
        if (!Words.Verbs.TryGetValue(key, out string? data) || string.IsNullOrWhiteSpace(data))
        {
            var newKey = langPrefix + "_" + ww[ww.Length - 1];
            if (!Words.Verbs.TryGetValue(newKey, out data) || string.IsNullOrWhiteSpace(data))
            {
                var newKey2 = langPrefix + "_" + ww[0].Substring(0, ww[0].Length - 2);
                if (!Words.Verbs.TryGetValue(newKey2, out data) || string.IsNullOrWhiteSpace(data))
                {
                    var newKey3 = langPrefix + "_" + ww[0];
                    if (!Words.Verbs.TryGetValue(newKey3, out data) || string.IsNullOrWhiteSpace(data) &&
                        ww.Length > 1)
                    {
                        var newKey4 = langPrefix + "_" + ww[0] + " " + ww[1];
                        if (!Words.Verbs.TryGetValue(newKey4, out data) || string.IsNullOrWhiteSpace(data))
                        {
                            return "";
                        }
                    }
                }
            }
        }
        return data;
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
            ResetTimer();
        }
        else
        {
            StopPlay();
        }
    }
    void ResetTimer()
    {
        m_timer?.Stop();
        m_timer = Application.Current?.Dispatcher.CreateTimer();
        m_timer.Interval = TimeSpan.FromSeconds(SettingsPage.Instance.GetPlayInterval());
        //m_timer.IsRepeating = true;
        m_timer.Tick += (s, e) => OnPlayTimer();
        m_timer.Start();
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
            ResetTimer();
        });
    }

    void _SetCategory(Category cat, bool force = false)
    {
        if (!force && (m_category == Categories.DefaultCategory || cat == m_category))
        {
            return;
        }
        m_category = cat;
        if (cat == Categories.DefaultCategory)
        {
            var rnd = QuizPage.GetRandom(100, 100);
            var i = 0;
        }
    }

    public async Task SetWord(Word word, bool speak = true, bool forceChangeCategory = false)
    {
        if (word == null)
        {
            return;
        }
        for (int i = 0; i < 1; i++)
        {
            if (!m_settingWord)
            {
                break;
            }
            await Task.Delay(800);
        }
        Context.CancelTimers();

        var v = TranslationView.GetScrollView();
        m_trasnlationViewY = v.ScrollY < 0 ? m_trasnlationViewY : v.ScrollY;

        m_settingWord = true;
        m_category = forceChangeCategory ? word.Category :
            m_category == Categories.DefaultCategory || m_category == Categories.CustomCategory ||
            m_category == Categories.TrashCategory || m_category == Categories.LearnedCategory ?
            m_category : word.Category;
        var ind = Categories.GetCategoryIndex(m_category.Name);
        if (ind < 0)
        {
            ind = 0;
        }
        var wordIndex = m_category.GetIndex(word);
        m_category.Index = wordIndex;
        Preferences.Set(WordSet, wordIndex);
        Preferences.Set(CategorySet, ind);
        Preferences.Set(CategoryWord + m_category.Name, wordIndex);

        CategoryPicker.SelectedIndex = ind;

        ButInfoBorder.IsVisible = ButInfo.IsVisible = (word.Category.Name == Categories.VerbCategoryName);
        ButAddBorder.IsVisible = m_category == Categories.TrashCategory || m_category == Categories.CustomCategory ||
                    m_category == Categories.LearnedCategory || !Categories.CustomCategory.Exists(word.Name);
        //ButDelBorder.IsVisible = m_category != Categories.DefaultCategory;
        ButDelBorder.IsVisible = m_category != Categories.DefaultCategory || !Categories.TrashCategory.Exists(word.Name);
        ButLearnedBorder.IsVisible = //m_category != Categories.DefaultCategory && m_category != Categories.LearnedCategory &&
            m_category != Categories.TrashCategory && !Categories.LearnedCategory.Exists(word.Name);// && m_category != Categories.CustomCategory;
        ButAdd.Source = m_category == Categories.CustomCategory || m_category == Categories.TrashCategory ||
            m_category == Categories.LearnedCategory ?
            "empty_folder.png" : "add_word.png";

        MainImgTxt.IsVisible = MainImgImg.IsVisible = false;
        MainWordBorder.IsVisible = !word.Category.IsText;
        MainWordText.Text = MainImgTxt.Text = " ";
        if (word.Category.IsText)
        {
            MainImgTxt.IsVisible = true;
            MainImgImg.IsVisible = false;
        }
        else
        {
            MainImgImg.Source = word.GetImage();
            MainImgImg.IsVisible = true;
            MainImgTxt.IsVisible = false;
        }

        WordId.Text = string.Format(AppResources.Word__0___1_, (m_category.Index + 1), m_category.GetTotalWords());
        bool setOriginal = SettingsPage.DelayRate <= 0 || !SettingsPage.DelayOriginal;
        bool setTransl = SettingsPage.DelayRate == 0 || SettingsPage.DelayOriginal;
        if (!setTransl)
        { // Remove transation
            SetTranslation(" ");
            Context.ClearTranslations();
        }
        else
        {
            Context.SetTranslations(word);
        }

        if (setOriginal)
        {
            string current = word.GetTranslation(SettingsPage.VoiceLearn);
            SetOrigTranslation(word, current);
            if (speak)
            {
                await TTS.Speak(current, SettingsPage.VoiceLearn);
            }
        }

        Context.SetWord(word, speak);

        TranslationFlag.Source = Word.GetFlag(SettingsPage.MyVoice);
        SetTranslationView();

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
        /*var categ = Categories.GetCategory(Categories.VerbCategoryName);
        var words = categ.Words;
        foreach (var word in words)
        {
            Context.SetWord(word);
            InfoData_Clicked(null, EventArgs.Empty);
        }
        int i = 0;*/
    }

    private async void CategorySelectionChanged(object? sender, EventArgs e)
    {
        StopPlay();
        var chosen = CategoryPicker.SelectedIndex;
        if (chosen < 0 || m_settingWord)
        {
            return;
        }
        Preferences.Set(CategorySet, chosen);

        var prevCategory = m_category;
        m_category = Categories.SwitchCategory(chosen);
        if (m_category.CatWords.Count == 0)
        {
            if (m_category == Categories.CustomCategory)
            {
                await DisplayAlert(AppInfo.Current.Name, AppResources.select_words, "OK");
            }
            else
            {

                await DisplayAlert(AppInfo.Current.Name, string.Format(AppResources.Folder__0__IsEmpty,
                    Context.TranslateCategory(m_category)), "OK");
            }
            m_category = prevCategory;
            var ind = Categories.GetCategoryIndex(prevCategory.Name);
            if (ind == chosen)
            {
                ind = 0;
            }
            CategoryPicker.SelectedIndex = ind;
            return;
        }

        var index = Preferences.Get(CategoryWord + m_category.Name, 0);
        var word = m_category.GetWord(index);

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

    IDispatcherTimer? m_timer;
    IDispatcherTimer? m_timer2;
    DateTime m_timerStart = DateTime.Now;

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
    public static string TranslateCategory(Category cat)
    {
        var index = Categories.GetCategoryIndex(cat.Name);
        return TranslatedCategories()[index];
    }

    public static List<string> TranslatedCategories()
    {
        List<string> cat = new List<string>
        {
            AppResources.all,
            AppResources.animal_world,
            AppResources.body_parts,
            AppResources.numbers_and_math,
            AppResources.calendar__time,
            AppResources.transport,
            AppResources.sport,
            AppResources.hobby__music__chess,
            AppResources.fruits_and_vegetables,
            AppResources.food_and_beverage,
            AppResources.household,
            AppResources.bed_and_bath,
            AppResources.tools_and_appliances,
            AppResources.kitchen,
            AppResources.clothing,
            AppResources.school_and_office,
            AppResources.colors__figures,
            AppResources.accesories,
            AppResources.family_and_kids,
            AppResources.profession,
            AppResources.countries,
            AppResources.environment,
            AppResources.verbs,
            AppResources.prepositions,
            AppResources.adjectives,
            AppResources.phrases_greetings,
            AppResources.basic_phrases,
            AppResources.phrases_traveling,
            AppResources.phrases_in_hotel,
            AppResources.phrases_in_restaurant,
            AppResources.flirting_phrases,
            AppResources.custom_category,
            AppResources.learned_words,
            AppResources.trash
        };
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

    internal void CancelTimers()
    {
        m_timer?.Stop();
        m_timer2?.Stop();
    }
    internal void ClearTranslations()
    {
        transInfo.Clear();
    }

    internal void SetWord(Word word, bool speak)
    {
        CancelTimers();

        Word = word;
        MainWord = Word.GetTranslation(SettingsPage.VoiceLearn);

        if (SettingsPage.DelayRate <= 0)
        {
            SetTranslations(Word);
            LearnPage.Instance.SetTranslationView();
            LearnPage.Instance.SetOrigTranslation(Word, MainWord);
            return;
        }

        LearnPage.Instance.SetTranslation(".");

        m_timer = Application.Current?.Dispatcher.CreateTimer();
        m_timer.Interval = TimeSpan.FromSeconds(0.5);
        if (SettingsPage.DelayOriginal)
        {
            SetTranslations(Word);
            LearnPage.Instance.SetTranslationView();
            m_timer.Tick += (s, e) => OnDelayOrigTimer(speak);
        }
        else
        {
            LearnPage.Instance.SetOrigTranslation(Word, MainWord);
            m_timer.Tick += (s, e) => OnDelayTimer();
        }
        m_timer.IsRepeating = true;
        m_timerStart = DateTime.Now;
        m_timer.Start();
    }

    private void OnDelayTimer()
    {
        var diff = DateTime.Now - m_timerStart;
        if (diff.TotalSeconds >= SettingsPage.DelayRate)
        {
            m_timer?.Stop();
            SetTranslations(Word);
            m_timer2 = Application.Current?.Dispatcher.CreateTimer();
            m_timer2.Interval = TimeSpan.FromSeconds(0.1);
            m_timer2.Tick += (s, e) => OnDelayTimer2();
            m_timer2.IsRepeating = false;
            m_timer2.Start();
        }
        else
        {
            LearnPage.Instance.SetTranslationWait();
        }
    }
    private void OnDelayTimer2()
    {
        m_timer2?.Stop();
        LearnPage.Instance.SetTranslationView();
    }
    async void OnDelayOrigTimer(bool speak)
    {
        var diff = DateTime.Now - m_timerStart;
        if (diff.TotalSeconds >= SettingsPage.DelayRate)
        {
            m_timer?.Stop();
            LearnPage.Instance.SetOrigTranslation(this.Word, MainWord);
            if (speak)
            {
                await TTS.Speak(MainWord, SettingsPage.VoiceLearn);
            }
        }
        else
        {
            LearnPage.Instance.SetTranslationOrigWait();
        }
    }

    internal void SetTranslations(Word word)
    {
        GenerateTransInfo(word);
        LearnPage.Instance.SetTranslation(word.GetTranslation(SettingsPage.MyVoice));
    }

    internal void GenerateTransInfo(Word word)
    {
        var voiceOrder = Words.VoiceOrder;
        transInfo.Clear();

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
