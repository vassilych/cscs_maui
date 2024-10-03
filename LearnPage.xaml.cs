using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.Shapes;
using ScriptingMaui.Resources.Strings;
using SplitAndMerge;
using Syncfusion.Maui.ListView;
using Syncfusion.Maui.Picker;
//using ObjCRuntime;
//using static ObjCRuntime.Dlfcn;

namespace ScriptingMaui;

public partial class LearnPage : ContentPage
{
    const string CategorySet = "categoryLearn";
    const string WordSet = "wordLearn";
    int MaxSearchResults = 7;
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
        //SetCategory(m_category, true);
        PrevImg.Clicked += PrevImgClick;
        NextImg.Clicked += NextImgClick;
        MainImgImg.Clicked += NextImgClick;
        MainImgTxt.Clicked += NextImgClick;
        ButSpeak.Clicked += SpeakerClick;
        ButPlay.Clicked += PlayClick;

        FindBut.Clicked += FindBut_Clicked;
        SearchEntry.TextChanged += SearchEntry_TextChanged;
        SearchEntry.Completed += BackBut_Clicked;
        ButOk.Clicked += BackBut_Clicked;

        TranslationView.ItemTapped += TranslationView_ItemTapped;
        ResultsView.ItemTapped += ResultsView_ItemTapped;

        TranslationBtn.Clicked += TranslationBtn_Clicked;
        TranslationFlag.Clicked += TranslationBtn_Clicked;

        ButInfo.Clicked += InfoData_Clicked;
        SetMode();
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

    void SetMode(bool findMode = false, bool infoMode = false)
    {
        SearchPanel.IsVisible = findMode || infoMode;
        ButOk.IsVisible = findMode || infoMode;
        ResultsView.IsVisible = findMode && !infoMode;
        SearchEntry.IsEnabled = SearchEntry.IsVisible = findMode;
        WordName.IsVisible = infoMode;
        WordDetails.IsVisible = infoMode;

        TopPanel.IsVisible = !findMode && !infoMode;
        TopMainPanel.IsVisible = !findMode && !infoMode;
        MainPanel.IsVisible = !findMode && !infoMode;
        ButtonsPanel.IsVisible = !findMode && !infoMode;
        TranslationView.IsVisible = !findMode && !infoMode;
        TranslationBtn.IsVisible = !findMode && !infoMode;
        TranslationFlag.IsVisible = !findMode && !infoMode;

        InfoPanel.IsVisible = infoMode;
        VerbInfoH1.IsVisible = VerbInfoH2.IsVisible = VerbInfoH3.IsVisible = infoMode;
        VerbInfo11.IsVisible = VerbInfo21.IsVisible = VerbInfo31.IsVisible = infoMode;
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
                    return "";
                }
            }
        }
        return data;
    }

    async void InfoData_Clicked(object? sender, EventArgs e)
    {
        StopPlay();
        var prefix1 = SettingsPage.VoiceLearn.Substring(0, 2);
        var prefix2 = SettingsPage.MyVoice.Substring(0, 2);
        var words = Context.Word.GetTranslation(SettingsPage.VoiceLearn);
        var tok = words.Split(",", StringSplitOptions.TrimEntries);
        foreach (var candidate in tok)
        {
            var data = GetWordData(prefix1, candidate);
            if (string.IsNullOrWhiteSpace(data))
            {
                if (SettingsPage.VoiceLearn == "de-CH")
                {
                    words = Context.Word.GetTranslation("de-DE");
                    var t = words.Split(",", StringSplitOptions.TrimEntries);
                    data = GetWordData(prefix1, t.First());
                }
                if (string.IsNullOrWhiteSpace(data))
                {
                    var c = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var cand2 = c.First();
                    data = GetWordData(prefix1, cand2);
                }
                if (string.IsNullOrWhiteSpace(data))
                {
                    await DisplayAlert(AppResources.verbs, string.Format(AppResources.Word_with__0__not_found, candidate), "OK");
                    continue;
                }
            }
            SetMode(false, true);
            var tokens = data.Trim().Split(new char[] { ',' }, (StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            if (prefix1.StartsWith("en"))
            {
                LoadEnVerbs(tokens);
            }
            else if (prefix1.StartsWith("es"))
            {
                LoadEsVerbs(tokens);
            }
            else if (prefix1.StartsWith("de"))
            {
                LoadDeVerbs(tokens);
            }
            else if (prefix1.StartsWith("ru"))
            {
                LoadRuVerbs(tokens);
            }
            break;
        }
        SetSize(VerbInfoH1, true); SetSize(VerbInfoH2, true); SetSize(VerbInfoH3, true);
        SetSize(VerbInfoH4, true); SetSize(VerbInfoH5, true); SetSize(VerbInfoH6, true);
        SetSize(VerbInfoH7, true); SetSize(VerbInfoH8, true); SetSize(VerbInfoH9, true);

        SetSize(VerbInfo11); SetSize(VerbInfo21); SetSize(VerbInfo31);
        SetSize(VerbInfo12); SetSize(VerbInfo22); SetSize(VerbInfo32);
        SetSize(VerbInfo13); SetSize(VerbInfo23); SetSize(VerbInfo33);
        SetSize(VerbInfo41); SetSize(VerbInfo51); SetSize(VerbInfo61);
        SetSize(VerbInfo42); SetSize(VerbInfo52); SetSize(VerbInfo62);
        SetSize(VerbInfo43); SetSize(VerbInfo53); SetSize(VerbInfo63);

        SetSize(VerbInfo71); SetSize(VerbInfo81); SetSize(VerbInfo91);
        SetSize(VerbInfo72); SetSize(VerbInfo82); SetSize(VerbInfo92);
        SetSize(VerbInfo73); SetSize(VerbInfo83); SetSize(VerbInfo93);
        SetSize(VerbInfo101); SetSize(VerbInfo111); SetSize(VerbInfo121);
        SetSize(VerbInfo102); SetSize(VerbInfo112); SetSize(VerbInfo122);
        SetSize(VerbInfo103); SetSize(VerbInfo113); SetSize(VerbInfo123);

        SetSize(VerbInfo131); SetSize(VerbInfo141); SetSize(VerbInfo151);
        SetSize(VerbInfo132); SetSize(VerbInfo142); SetSize(VerbInfo152);
        SetSize(VerbInfo133); SetSize(VerbInfo143); SetSize(VerbInfo153);
        SetSize(VerbInfo161); SetSize(VerbInfo171); SetSize(VerbInfo181);
        SetSize(VerbInfo162); SetSize(VerbInfo172); SetSize(VerbInfo182);
        SetSize(VerbInfo163); SetSize(VerbInfo173); SetSize(VerbInfo183);
    }
    void LoadEnVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Particip: " + tokens[2];
        Gerund.Text = "Gerund: " + tokens[3];
        // to be, be, been, being, am, are, is, was, were, was
        VerbInfoH1.Text = "Present";
        VerbInfo11.Text = "I " + tokens[4];
        VerbInfo21.Text = "you " + tokens[5];
        VerbInfo31.Text = "she " + tokens[6];
        VerbInfo41.Text = "we " + tokens[5];
        VerbInfo51.Text = "you " + tokens[5];
        VerbInfo61.Text = "they " + tokens[5];

        VerbInfoH2.Text = "Simple Past";
        VerbInfo12.Text = "I " + tokens[7];
        VerbInfo22.Text = "you " + tokens[8];
        VerbInfo32.Text = "she " + tokens[9];
        VerbInfo42.Text = "we " + tokens[7];
        VerbInfo52.Text = "you " + tokens[7];
        VerbInfo62.Text = "they " + tokens[7];

        VerbInfoH3.Text = "Future";
        VerbInfo13.Text = "I will " + tokens[1];
        VerbInfo23.Text = "you will " + tokens[1];
        VerbInfo33.Text = "she will " + tokens[1];
        VerbInfo43.Text = "we will " + tokens[1];
        VerbInfo53.Text = "you will " + tokens[1];
        VerbInfo63.Text = "they will " + tokens[1];

        VerbInfoH4.Text = "Pres. Cont.";
        VerbInfo71.Text = "I'm " + tokens[3];
        VerbInfo81.Text = "you're " + tokens[3];
        VerbInfo91.Text = "she's " + tokens[3];
        VerbInfo101.Text = "we're " + tokens[3];
        VerbInfo111.Text = "you're " + tokens[3];
        VerbInfo121.Text = "they're " + tokens[3];

        VerbInfoH5.Text = "Pres. Perfect";
        VerbInfo72.Text = "have " + tokens[2];
        VerbInfo82.Text = "have " + tokens[2];
        VerbInfo92.Text = "has " + tokens[2];
        VerbInfo102.Text = VerbInfo112.Text = VerbInfo122.Text = "have " + tokens[2];

        VerbInfoH6.Text = "Conditional";
        VerbInfo73.Text = VerbInfo83.Text = VerbInfo93.Text =
            VerbInfo103.Text = VerbInfo113.Text = VerbInfo123.Text = "would " + tokens[1];

        ShowRow3(true);
        VerbInfoH7.Text = "Past Cont.";
        VerbInfo131.Text = "I was " + tokens[3];
        VerbInfo141.Text = "you were " + tokens[3];
        VerbInfo151.Text = "she was " + tokens[3];
        VerbInfo161.Text = "we were " + tokens[3];
        VerbInfo171.Text = "you were " + tokens[3];
        VerbInfo181.Text = "they were " + tokens[3];

        VerbInfoH8.Text = "Past Perfect";
        VerbInfo132.Text = "had " + tokens[2];
        VerbInfo142.Text = "had " + tokens[2];
        VerbInfo152.Text = "had " + tokens[2];
        VerbInfo162.Text = VerbInfo172.Text = VerbInfo182.Text = "had " + tokens[2];

        VerbInfoH9.Text = "Pres. Emphatic";
        VerbInfo133.Text = "do " + tokens[1];
        VerbInfo143.Text = "do " + tokens[1];
        VerbInfo153.Text = "does " + tokens[1];
        VerbInfo163.Text = "do " + tokens[1];
        VerbInfo173.Text = "do " + tokens[1];
        VerbInfo183.Text = "do " + tokens[1];
    }

    void LoadEsVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Participio: " + tokens[1];
        Gerund.Text = "Gerundio: " + tokens[2];
        /* hacer, hecho, haciendo, hago,haces,hace,hacemos,hacéis,hacen,  hacía,hacías,hacía,hacíamos,hacíais,hacían,
         15 hice,hiciste,hizo,hicimos,hicisteis,hicieron, haré,harás,hará,haremos,haréis,harán,
         27 hecho,haría,harías,haría,haríamos,haríais,harían */
        VerbInfoH1.Text = "Presente";
        VerbInfo11.Text = "Yo " + tokens[3];
        VerbInfo21.Text = "Tú " + tokens[4];
        VerbInfo31.Text = "Él/Ella/Ud " + tokens[5];
        VerbInfo41.Text = "Nosotros " + tokens[6];
        VerbInfo51.Text = "Vosotros " + tokens[7];
        VerbInfo61.Text = "Ellas/Ustedes " + tokens[8];

        VerbInfoH2.Text = "Pret Imperfecto";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];

        VerbInfoH3.Text = "Pret Indefinido";
        VerbInfo13.Text = tokens[15];
        VerbInfo23.Text = tokens[16];
        VerbInfo33.Text = tokens[17];
        VerbInfo43.Text = tokens[18];
        VerbInfo53.Text = tokens[19];
        VerbInfo63.Text = tokens[20];

        VerbInfoH4.Text = "Futuro";
        VerbInfo71.Text = tokens[21];
        VerbInfo81.Text = tokens[22];
        VerbInfo91.Text = tokens[23];
        VerbInfo101.Text = tokens[24];
        VerbInfo111.Text = tokens[25];
        VerbInfo121.Text = tokens[26];

        VerbInfoH5.Text = "Pret Perfecto";
        VerbInfo72.Text = "he " + tokens[1];
        VerbInfo82.Text = "has " + tokens[1];
        VerbInfo92.Text = "ha " + tokens[1];
        VerbInfo102.Text = "hemos " + tokens[1];
        VerbInfo112.Text = "habéis " + tokens[1];
        VerbInfo122.Text = "han " + tokens[1];

        VerbInfoH6.Text = "Condicional";
        VerbInfo73.Text = tokens[27];
        VerbInfo83.Text = tokens[28];
        VerbInfo93.Text = tokens[29];
        VerbInfo103.Text = tokens[30];
        VerbInfo113.Text = tokens[31];
        VerbInfo123.Text = tokens[32];

        ShowRow3(true);
        VerbInfoH7.Text = "Pluscuamperfecto";
        VerbInfo131.Text = "había " + tokens[1];
        VerbInfo141.Text = "habías " + tokens[1];
        VerbInfo151.Text = "había " + tokens[1];
        VerbInfo161.Text = "habíamos " + tokens[1];
        VerbInfo171.Text = "habíais " + tokens[1];
        VerbInfo181.Text = "habían " + tokens[1];

        VerbInfoH8.Text = "Pres continuo";
        VerbInfo132.Text = "estoy " + tokens[2];
        VerbInfo142.Text = "estás " + tokens[2];
        VerbInfo152.Text = "está " + tokens[2];
        VerbInfo162.Text = "estamos " + tokens[2];
        VerbInfo172.Text = "estáis " + tokens[2];
        VerbInfo182.Text = "están " + tokens[2];

        VerbInfoH9.Text = "Futuro próximo";
        VerbInfo133.Text = "voy a " + tokens[0];
        VerbInfo143.Text = "vas a " + tokens[0];
        VerbInfo153.Text = "va a " + tokens[0];
        VerbInfo163.Text = "vamos a " + tokens[0];
        VerbInfo173.Text = "vais a " + tokens[0];
        VerbInfo183.Text = "van a " + tokens[0];
    }
    void LoadDeVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Partizip: " + tokens[1];
        Gerund.Text = "Gerundium: " + tokens[2];
        /* sein, gewesen, seiend, Präsens bin,bist,ist,sind,seid,sind,
         * Perfekt 9 bin gewesen,bist gewesen,ist gewesen,sind gewesen,seid gewesen,sind gewesen,
         * Plusquamperfekt 15 war gewesen,warst gewesen,war gewesen,waren gewesen,wart gewesen,waren gewesen,
         * Präteritum 21 war,warst,war,waren,wart,waren,
         * Präsens I 27 sei,seist,sei,seien,seiet,seien,
         * Präteritum II 33 wäre,wärest,wäre,wären,wäret,wären,
         * Imperative 39 Sei,Seien,Seid,Seien 
         sein, gewesen, seiend, bin,bist,ist,sind,seid,sind,
        bin gewesen,bist gewesen,ist gewesen,sind gewesen,seid gewesen,sind gewesen,
        war gewesen,warst gewesen,war gewesen,waren gewesen,wart gewesen,waren gewesen,
        war,warst,war,waren,wart,waren,
        sei,seist,sei,seien,seiet,seien,
        wäre,wärest,wäre,wären,wäret,wären,
        Sei, Seien, Seid, Seien*/
        VerbInfoH1.Text = "Präsens";
        VerbInfo11.Text = "ich " + tokens[3];
        VerbInfo21.Text = "du " + tokens[4];
        VerbInfo31.Text = "er/sie/es " + tokens[5];
        VerbInfo41.Text = "wir " + tokens[6];
        VerbInfo51.Text = "ihr " + tokens[7];
        VerbInfo61.Text = "sie/Sie " + tokens[8];

        VerbInfoH2.Text = "Perfekt";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];
                
        VerbInfoH3.Text = "Präteritum";
        VerbInfo13.Text = tokens[21];
        VerbInfo23.Text = tokens[22];
        VerbInfo33.Text = tokens[23];
        VerbInfo43.Text = tokens[24];
        VerbInfo53.Text = tokens[25];
        VerbInfo63.Text = tokens[26];
                
        VerbInfoH4.Text = "Plusquamperfekt";
        VerbInfo71.Text = tokens[15];
        VerbInfo81.Text = tokens[16];
        VerbInfo91.Text = tokens[17];
        VerbInfo101.Text = tokens[18];
        VerbInfo111.Text = tokens[19];
        VerbInfo121.Text = tokens[20];

        VerbInfoH5.Text = "Futur I";
        VerbInfo72.Text = "werde " + tokens[1];
        VerbInfo82.Text = "wirst " + tokens[1];
        VerbInfo92.Text = "wird " + tokens[1];
        VerbInfo102.Text = "werden " + tokens[1];
        VerbInfo112.Text = "werdet " + tokens[1];
        VerbInfo122.Text = "werden " + tokens[1];

        VerbInfoH6.Text = "Imperativ";
        VerbInfo73.Text = "";
        VerbInfo83.Text = tokens[39].Contains(" du") || tokens[39] == "-" ? tokens[39] : tokens[39] + " (du)!";
        VerbInfo93.Text = "";
        VerbInfo103.Text = tokens[40].Contains(" wir") || tokens[40] == "-" ? tokens[40] : tokens[40] + " wir!";
        VerbInfo113.Text = tokens[41].Contains(" ihr") || tokens[41] == "-" ? tokens[41] : tokens[41] + " ihr!";
        VerbInfo123.Text = tokens[42].Contains(" Sie") || tokens[42] == "-" ? tokens[42] : tokens[42] + " Sie!";
        ShowRow3(true);
        VerbInfoH7.Text = "Konj Präsens I";
        VerbInfo131.Text = "ich " + tokens[27];
        VerbInfo141.Text = "du " + tokens[28];
        VerbInfo151.Text = "er/sie/es " + tokens[29];
        VerbInfo161.Text = "wir " + tokens[30];
        VerbInfo171.Text = "ihr " + tokens[31];
        VerbInfo181.Text = "sie/Sie " + tokens[32];

        VerbInfoH8.Text = "Konj Präter II";
        VerbInfo132.Text = tokens[33];
        VerbInfo142.Text = tokens[34];
        VerbInfo152.Text = tokens[35];
        VerbInfo162.Text = tokens[36];
        VerbInfo172.Text = tokens[37];
        VerbInfo182.Text = tokens[38];

        VerbInfoH9.Text = "Konj Futur II";
        VerbInfo133.Text = "würde " + tokens[0];
        VerbInfo143.Text = "würdest " + tokens[0];
        VerbInfo153.Text = "würde " + tokens[0];
        VerbInfo163.Text = "würden " + tokens[0];
        VerbInfo173.Text = "würdet " + tokens[0];
        VerbInfo183.Text = "würden " + tokens[0];
    }

    void LoadRuVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Причастие: " + tokens[1];
        Gerund.Text = "Деепричастие: " + tokens[2];
        /* спорить, спорящий, споря, 
         * НАСТ 3 спорю,споришь,спорит,спорим,спорите,спорят,
         * ПРОШ 9 спорил/спорила,спорил/спорила,спорил/спорила,спорили,спорили,спорили, 
         * БУД 15 буду спорить,будешь спорить,будет спорить,будем спорить,будете спорить,будут спорить, 
         * УСЛ 21 бы спорил / спорила,бы спорил / спорила,бы спорил / спорила,бы спорили,бы спорили,бы спорили,
         * ПОВ 27 спорь,спорьте
 */
        VerbInfoH1.Text = "Настоящее время";
        VerbInfo11.Text = "Я " + tokens[3];
        VerbInfo21.Text = "Ты " + tokens[4];
        VerbInfo31.Text = "Он/она " + tokens[5];
        VerbInfo41.Text = "Мы " + tokens[6];
        VerbInfo51.Text = "Вы " + tokens[7];
        VerbInfo61.Text = "Они " + tokens[8];

        VerbInfoH2.Text = "Прошедшее Муж.";
        VerbInfo12.Text = tokens[9].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo22.Text = tokens[10].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo32.Text = tokens[11].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo42.Text = tokens[12].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo52.Text = tokens[13].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo62.Text = tokens[14].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();

        VerbInfoH3.Text = "Прошедшее Жен.";
        VerbInfo13.Text = tokens[9].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo23.Text = tokens[10].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo33.Text = tokens[11].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo43.Text = tokens[12].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo53.Text = tokens[13].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo63.Text = tokens[14].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();

        VerbInfoH4.Text = "Будущее время";
        VerbInfo71.Text = "Я " + tokens[15];
        VerbInfo81.Text = "Ты " + tokens[16];
        VerbInfo91.Text = "Он/она " + tokens[17];
        VerbInfo101.Text = "Мы " + tokens[18];
        VerbInfo111.Text = "Вы " + tokens[19];
        VerbInfo121.Text = "Они " + tokens[20];

        VerbInfoH5.Text = "Сослагательное";
        VerbInfo72.Text = tokens[21];
        VerbInfo82.Text = tokens[22];
        VerbInfo92.Text = tokens[23];
        VerbInfo102.Text = tokens[24];
        VerbInfo112.Text = tokens[25];
        VerbInfo122.Text = tokens[26];

        VerbInfoH6.Text = "Повелительное";
        VerbInfo73.Text = "";
        VerbInfo83.Text = tokens[27];
        VerbInfo93.Text = "";
        VerbInfo103.Text = "";
        VerbInfo113.Text = tokens[28];
        VerbInfo123.Text = "";
        ShowRow3(false);
    }

    void ShowRow3(bool isVisible = true)
    {
        VerbInfoH7.IsVisible = VerbInfoH8.IsVisible = VerbInfoH9.IsVisible =
            Border31.IsVisible = Border32.IsVisible = Border33.IsVisible = isVisible;
        VerbInfo131.IsVisible = VerbInfo141.IsVisible = VerbInfo151.IsVisible = VerbInfo161.IsVisible = VerbInfo171.IsVisible = VerbInfo181.IsVisible = isVisible;
        VerbInfo132.IsVisible = VerbInfo142.IsVisible = VerbInfo152.IsVisible = VerbInfo162.IsVisible = VerbInfo172.IsVisible = VerbInfo182.IsVisible = isVisible;
        VerbInfo133.IsVisible = VerbInfo143.IsVisible = VerbInfo153.IsVisible = VerbInfo163.IsVisible = VerbInfo173.IsVisible = VerbInfo183.IsVisible = isVisible;
        Border131.IsVisible = Border141.IsVisible = Border151.IsVisible = Border161.IsVisible = Border171.IsVisible = Border181.IsVisible = isVisible;
        Border132.IsVisible = Border142.IsVisible = Border152.IsVisible = Border162.IsVisible = Border172.IsVisible = Border182.IsVisible = isVisible;
        Border133.IsVisible = Border143.IsVisible = Border153.IsVisible = Border163.IsVisible = Border173.IsVisible = Border183.IsVisible = isVisible;
        if (!isVisible)
        {
            VerbInfoH7.Text = VerbInfoH8.Text = VerbInfoH9.Text = "";
            VerbInfo131.Text = VerbInfo141.Text = VerbInfo151.Text = VerbInfo161.Text = VerbInfo171.Text = VerbInfo181.Text = "";
            VerbInfo132.Text = VerbInfo142.Text = VerbInfo152.Text = VerbInfo162.Text = VerbInfo172.Text = VerbInfo182.Text = "";
            VerbInfo133.Text = VerbInfo143.Text = VerbInfo153.Text = VerbInfo163.Text = VerbInfo173.Text = VerbInfo183.Text = "";
        }
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

    void SetCategory(Category cat, bool force = false)
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

    async Task SetWord(Word word, bool speak = true)
    {
        for (int i = 0; i < 1; i++)
        {
            if (!m_settingWord)
            {
                break;
            }
            await Task.Delay(800);
        }
        m_settingWord = true;
        Context.SetWord(word);
        m_category = m_category == Categories.DefaultCategory ? m_category : word.Category;
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
        if (word.Category.IsText)
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
        ButInfoBorder.IsVisible = ButInfo.IsVisible = (word.Category.Name == "verbs");

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
        /*var categ = Categories.GetCategory("verbs");
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

        m_category = Categories.SwitchCategory(chosen);
        //SetCategory(m_category, true);

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
            AppResources.flirting_phrases
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

    internal void SetWord(Word word)
    {
        GenerateTransInfo(word);
        Word = word;
        MainWord = word.GetTranslation(SettingsPage.VoiceLearn);

        /*Variable data = new Variable(Variable.VarType.ARRAY);
        data.Tuple.Add(new Variable("lol"));
        data.Tuple.Add(new Variable("lala"));

        MainPage.Instance.Scripting.UpdateValue(nameof(LearnPage.Context.TransInfo), data);*/

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
