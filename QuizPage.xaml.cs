using Microsoft.Maui.Controls;
using ScriptingMaui.Resources.Strings;
using SplitAndMerge;
using System;
using System.Globalization;
using System.Xml.Linq;

namespace ScriptingMaui;

public partial class QuizPage : ContentPage
{
    const string CategorySet = "categoryQuiz";
    const string WordsSet = "wordsQuiz";
    const string StartBtn = "start.png";
    const string StopBtn = "stop.png";

    Dictionary<string, View> m_views = new Dictionary<string, View>();
    List<string> m_words = new List<string>();
    const int m_timeoutms = 250;
    const int m_quizChoices = 6;

    bool m_playing;
    Category m_category = Categories.DefaultCategory;
    int m_totalWords; // total category words
    int m_quizWords;  // total words in the quiz
    int m_currentQuizQuestion; // current question (0 - m_quizWords)
    int m_correctId; // correct answer (0 - 5)
    int m_correctInQuiz; // correct answers in the quiz (0 - m_currentQuizQuestion)
    Word? m_correctWord = Word.Default;
    IDispatcherTimer? m_timer = Application.Current?.Dispatcher.CreateTimer();
    DateTime m_startQuiz = DateTime.Now;
    Random m_random = new Random();

    public QuizPage()
	{
        InitializeComponent();
        Setup();
        WordsStepper.Value = Preferences.Get(WordsSet, 5);
        NbWords.Text = WordsStepper.Value.ToString();
        WordsStepper.ValueChanged += WordsStepper_ValueChanged;
        StartStopBtn.Clicked += StartStopBtn_Clicked;

        Localize();

        int index = Preferences.Get(CategorySet, 0);
        CategoryPicker.SelectedIndex = index;
        CategoryPicker.SelectedIndexChanged += CategorySelectionChanged;
    }

    private async void CategorySelectionChanged(object? sender, EventArgs e)
    {
        var chosen = CategoryPicker.SelectedIndex;
        if (chosen < 0)
        {
            return;
        }
        Preferences.Set(CategorySet, chosen);

        if (m_playing)
        {
            await StopQuiz(false);
        }

        m_category = Categories.SwitchCategory(chosen);
        SetupRecords();
    }

    public void Localize()
    {
        CategoryPicker.Title = AppResources.SelectCategory;
        CategoryPicker.ItemsSource = Context.TranslatedCategories();
        SetupRecords();

        WordsLabel.Text = AppResources.Quiz_Words_;
        var pct = m_currentQuizQuestion == 0 ? 0 :
            (int)Math.Round(100.0 * (double)m_correctInQuiz / (double)m_currentQuizQuestion);
        Result.Text = string.Format(AppResources.Correct___0_____1___2__,
            pct, m_correctInQuiz, m_currentQuizQuestion);

        Title = AppResources.Quiz;
    }
    public void Setup()
    {
        m_views[(string)ImgBtn1.BindingContext] = ImgBtn1;
        m_views[(string)ImgBtn2.BindingContext] = ImgBtn2;
        m_views[(string)ImgBtn3.BindingContext] = ImgBtn3;
        m_views[(string)ImgBtn4.BindingContext] = ImgBtn4;
        m_views[(string)ImgBtn5.BindingContext] = ImgBtn5;
        m_views[(string)ImgBtn6.BindingContext] = ImgBtn6;
        m_views[(string)TxtBtn1.BindingContext] = TxtBtn1;
        m_views[(string)TxtBtn2.BindingContext] = TxtBtn2;
        m_views[(string)TxtBtn3.BindingContext] = TxtBtn3;
        m_views[(string)TxtBtn4.BindingContext] = TxtBtn4;
        m_views[(string)TxtBtn5.BindingContext] = TxtBtn5;
        m_views[(string)TxtBtn6.BindingContext] = TxtBtn6;
        m_views["1"] = Lab1; m_views["2"] = Lab2; m_views["3"] = Lab3;
        m_views["4"] = Lab4; m_views["5"] = Lab5; m_views["6"] = Lab6;
        ImgBtn1.Clicked += Btn_Clicked;
        ImgBtn2.Clicked += Btn_Clicked;
        ImgBtn3.Clicked += Btn_Clicked;
        ImgBtn4.Clicked += Btn_Clicked;
        ImgBtn5.Clicked += Btn_Clicked;
        ImgBtn6.Clicked += Btn_Clicked;
        TxtBtn1.Clicked += Btn_Clicked;
        TxtBtn2.Clicked += Btn_Clicked;
        TxtBtn3.Clicked += Btn_Clicked;
        TxtBtn4.Clicked += Btn_Clicked;
        TxtBtn5.Clicked += Btn_Clicked;
        TxtBtn6.Clicked += Btn_Clicked;
        Reset();
    }

    private async void Btn_Clicked(object? sender, EventArgs e)
    {
        View btn = sender is Button ? (sender as Button) :
            (sender as ImageButton);
        var context = (sender is Button ? (sender as Button).BindingContext :
            (sender as ImageButton).BindingContext) as string;
        if (string.IsNullOrWhiteSpace(context))
        {
            return;
        }
        var strId = "" + context[context.Length - 1];
        if (!int.TryParse(strId, out int id) || id < 0 || id > m_quizChoices)
        {
            return;
        }
        if (!m_playing)
        {
            await TTS.Speak(m_words[id-1], SettingsPage.VoiceLearn);
            return;
        }

        bool correct = (m_correctId == (id - 1));
        m_correctInQuiz += correct ? 1 : 0;
        var pct = (int)((double)m_correctInQuiz / (double)m_currentQuizQuestion);

        Result.Text = string.Format(AppResources.Correct___0_____1___2__,
            pct, m_correctInQuiz, m_currentQuizQuestion);
        var correctLab = m_views["" + (m_correctId + 1)] as Label;
        if (!correct)
        {
            var incorrectLab = m_views[strId] as Label;
            incorrectLab.IsVisible = true;
            incorrectLab.Text = "No";
            incorrectLab.TextColor = Colors.Red;
            await Task.Delay(1000);
        }
        correctLab.IsVisible = true;
        correctLab.Text = "OK";
        correctLab.TextColor = Colors.Green;

        await Task.Delay(1000 + (correct ? 0: 1000));
        await GetNewWords();
    }

    private async void StartStopBtn_Clicked(object? sender, EventArgs e)
    {
        m_playing = !m_playing;
        StartStopBtn.Source = m_playing ? StopBtn : StartBtn;

        if (!m_playing)
        {
            return;
        }

        m_category = Categories.GetCategory(CategoryPicker.SelectedIndex);
        m_quizWords = (int)WordsStepper.Value;
        m_totalWords = m_category.GetTotalWords();
        m_currentQuizQuestion = 0;
        m_correctInQuiz = 0;

        await GetNewWords();
        m_startQuiz = DateTime.Now;

        m_timer?.Stop();
        m_timer.Interval = TimeSpan.FromMilliseconds(m_timeoutms);
        m_timer.IsRepeating = true;
        m_timer.Tick += (s, e) => OnPlayTimer();
        m_timer.Start();
    }

    void SetupRecords(double newScore = 0.0, double newTime = 0.0)
    {
        m_category = Categories.GetCategory(CategoryPicker.SelectedIndex);
        m_quizWords = (int)WordsStepper.Value;
        var keyScore = string.Format("Record_{0}_{1}_{2}",
            m_category.Name, m_quizWords, SettingsPage.VoiceLearn);
        var keyDate = keyScore + "_dt";
        var keyTime = keyScore + "_tm";

        var score = Preferences.Get(keyScore, 0.0);
        var time = Preferences.Get(keyTime, 0.0);
        var date = Preferences.Get(keyDate, "");
        var highest = Math.Max(score, newScore);
        if (newScore > score || (score == newScore && score > 0 && newTime < time))
        {
            date = DateTime.Now.ToString("yyyy/MM/dd HH:mm").Replace(".", "/");
            Preferences.Set(keyScore, highest);
            Preferences.Set(keyDate, date);
            Preferences.Set(keyTime, newTime);
            time = newTime;
        }
        score = Math.Max(score, newScore);
        this.Record.Text = score > 0 ? AppResources.Best_score_ + " " + highest + "% - " +
                time + " sec - " + date : "    ";
    }

    void Reset()
    {
        ImgBtn1.IsVisible = false; TxtBtn1.IsVisible = false; Lab1.IsVisible = false;
        ImgBtn2.IsVisible = false; TxtBtn2.IsVisible = false; Lab2.IsVisible = false;
        ImgBtn3.IsVisible = false; TxtBtn3.IsVisible = false; Lab3.IsVisible = false;
        ImgBtn4.IsVisible = false; TxtBtn4.IsVisible = false; Lab4.IsVisible = false;
        ImgBtn5.IsVisible = false; TxtBtn5.IsVisible = false; Lab5.IsVisible = false;
        ImgBtn6.IsVisible = false; TxtBtn6.IsVisible = false; Lab6.IsVisible = false;
        m_words.Clear();
    }
    async Task StopQuiz(bool showResults = true)
    {
        var finish = DateTime.Now;
        m_timer?.Stop();
        m_playing = false;
        StartStopBtn.Source = StartBtn;

        if (showResults)
        {
            var time = Math.Round((finish - m_startQuiz).TotalSeconds, 1);
            var pct = Math.Round(100.0 * (double)m_correctInQuiz / (double)m_quizWords);

            await DisplayAlert("Quiz Finished", string.Format("Correct {0}/{1}",
                m_correctInQuiz, m_quizWords) + " " + pct + "%", "OK");
            if (m_quizWords * m_correctInQuiz > 0)
            {
                SetupRecords(pct, time);
            }
        }
    }
    private void OnPlayTimer()
    {
        if (!m_playing)
        {
            m_timer?.Stop();
            return;
        }
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var span = DateTime.Now.Subtract(m_startQuiz);
            var fmt = span.ToString().Substring(3,9);
            Timer.Text = fmt;
        });
    }
    private void WordsStepper_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        NbWords.Text = e.NewValue.ToString();
        Preferences.Set(WordsSet, (int)e.NewValue);
        SetupRecords();
    }
    void TestWords()
    {
        var words = Categories.DefaultCategory.Words;
        foreach (var w in words)
        {
            SetWord(w, 0);
        }
    }
    async Task GetNewWords(bool speak = true)
    {
        //TestWords();
        if (m_currentQuizQuestion >= m_quizWords)
        {
            await StopQuiz();
            return;
        }

        Reset();
        var randomInt = GetRandom(m_quizChoices, m_totalWords);
        for (int i = 0; i < randomInt.Count; i++)
        {
            var word = m_category.GetWord(randomInt[i]);
            SetWord(word, i);
            m_words.Add(word.GetTranslation(SettingsPage.VoiceLearn));
        }
        m_correctId = GetRandom(1, m_quizChoices).First(); // 0 to 5
        m_correctWord = m_category.GetWord(randomInt[m_correctId]);

        WordId.Text = m_correctWord.GetTranslation(SettingsPage.VoiceLearn);
        m_currentQuizQuestion++;
        if (speak)
        {
            await TTS.Speak(WordId.Text, SettingsPage.VoiceLearn);
        }
    }
    void SetWord(Word word, int index)
    {
        var key = word.Category.IsText ? "txt" + (index + 1) : "img" + (index + 1);
        if (m_views.TryGetValue(key, out View? widget))
        {
            if (widget is ImageButton)
            {
                ((ImageButton)widget).IsVisible = true;
                var img = word.GetImage();
                //Console.WriteLine(index + " Getting " + img);
                ((ImageButton)widget).Source = img;
            }
            else
            {
                ((Button)widget).IsVisible = true;
                ((Button)widget).Text = word.GetTranslation(SettingsPage.VoiceLearn);
            }
        }
        else
        {
            Console.WriteLine("Not found: " + key);
        }
    }

    List<int> GetRandom(int numberRandoms, int total)
    {
        List<int> result = new List<int>();
        List<int> available = Enumerable.Range(0, total).ToList();

        for (int i = 0; i < numberRandoms && available.Count > 0; i++)
        {
            int nextRandom = m_random.Next(0, available.Count);
            result.Add(available[nextRandom]);
            available.RemoveAt(nextRandom);
        }

        return result;
    }
}
