using System.Collections.ObjectModel;
using ScriptingMaui.Resources.Strings;
using SplitAndMerge;

namespace ScriptingMaui;

public partial class SearchPage : ContentPage
{
    public static SearchContext Context
    {
        get;
        set;
    }
    const int MaxSearchResults = 7;
    Dictionary<string, List<string>> m_words = new Dictionary<string, List<string>>();

    public SearchPage()
	{
		InitializeComponent();
        Context = Context == null ? new SearchContext() : Context;

        SearchEntry.TextChanged += SearchEntry_TextChanged;
        ResultsView.ItemTapped += ResultsView_ItemTapped;
    }

    public void Localize()
    {
        Title = AppResources.Search;
        SearchEntry.Placeholder = AppResources.Enter_text;
    }

    static string GetAlternate(string voice)
    {
        var altVoice  = voice == "en-US" ? "en-GB" :
                        voice == "en-GB" ? "en-US" :
                        voice == "es-MX" ? "es-ES" :
                        voice == "es-ES" ? "es-MX" :
                        voice == "de-DE" ? "de-CH" :
                        voice == "de-CH" ? "de-DE" : voice;
        return altVoice;
    }

    private void SearchEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var current = Normalize(SearchEntry.Text);
        if (string.IsNullOrWhiteSpace(current))
        {
            Context.ResultsInfo.Clear();
            return;
        }

        string voice1 = SettingsPage.VoiceLearn;
        string voice2 = SettingsPage.MyVoice;
        var altVoice1 = GetAlternate(voice1);
        var altVoice2 = GetAlternate(voice2);

        var words1 = GetWords(voice1);
        var words2 = GetWords(voice2);
        var words3 = altVoice1 != voice1 && altVoice1 != voice2 ?
            GetWords(altVoice1) : null;
        var words4 = altVoice2 != voice1 && altVoice2 != voice2 && altVoice2 != altVoice1 ?
            GetWords(altVoice2) : null;
        List<WordHint> results1 = new List<WordHint>();
        List<WordHint> results2 = new List<WordHint>();

        Trie search1 = new Trie(words1);
        search1.Search(current, MaxSearchResults, results1);

        Trie search2 = new Trie(words2);
        search2.Search(current, MaxSearchResults, results2);

        if (results1.Count == 0 && words3 != null)
        {
            voice1 = altVoice1;
            Trie search3 = new Trie(words3);
            search3.Search(current, MaxSearchResults, results1);
        }
        if (results2.Count == 0 && words4 != null)
        {
            voice2 = altVoice2;
            Trie search4 = new Trie(words4);
            search4.Search(current, MaxSearchResults, results2);
        }

        if (results1.Count == 0 && results2.Count == 0)
        {
            ResultsView.IsVisible = false;
            return;
        }
        ResultsView.IsVisible = true;
        Context.GenerateSearchInfo(results1, voice1);
        Context.GenerateSearchInfo(results2, voice2, false);
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
        MainPage.Instance?.SetPage(LearnPage.Instance);
        //TabbedPage tp = MainPage.Instance as TabbedPage;
        //tp.SelectedItem = this;
        await LearnPage.Instance.SetWord(word);
    }

    static string Normalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return "";
        }
        return word.Replace("ó", "o").Replace("é", "e").Replace("í", "i").Replace("á", "a").Replace("ñ", "n").
            Replace("ü", "u").Replace("ä", "a").Replace("ö", "o").Replace("ß", "ss").            
            Replace("ê", "e").Replace("â", "a").Replace("è", "e").Replace("û", "u").
            Replace("ё", "е");
    }
    List<string> GetWords(string voice)
    {
        if (!m_words.TryGetValue(voice, out List<string>? words))
        {
            words = new List<string>();
            var all = Categories.DefaultCategory.CatWords;
            foreach (var word in all)
            {
                var trans = word.GetTranslation(voice);
                words.Add(Normalize(trans));
            }
            m_words[voice] = words;
        }
        return words;
    }
}

public class SearchContext
{
    private ObservableCollection<object> resultsInfo = new ObservableCollection<object>();


    public SearchContext()
    {
        SearchPage.Context = this;
    }
    public ObservableCollection<object> ResultsInfo
    {
        get { return resultsInfo; }
        set { this.resultsInfo = value; }
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
