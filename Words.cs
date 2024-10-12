using System;
using Microsoft.Maui.Controls.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;
//using static UIKit.UIGestureRecognizer;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Reflection.PortableExecutable;
using Microsoft.Maui.Storage;
using ScriptingMaui;

namespace ScriptingMaui
{
	public class Words
	{
        public static List<string> Voices { get; set; } = new List<string>();
        public static List<string> Languages { get; set; } = new List<string>();
        public static List<string> Countries { get; set; } = new List<string>();
        public static List<string> Codes { get; set; } = new List<string>();
        public static List<string> VoiceOrder { get; set; } = new List<string>();
        public static Categories Categories { get; set; } = new Categories();
        public static Dictionary<string, string> Verbs { get; set; } = new Dictionary<string, string>();

        static int s_startWords = 5;

        public static async Task<Category[]> GetCategories()
        {
            return Categories.GetCategories();
        }
        public static async Task<List<string>> GetStringCategories()
        {
            return Categories.CategoryList;
        }
        public static async Task<List<string>> GetVoices()
        {
            return Voices;
        }
        public static string GetVoice(int index)
        {
            if (index < 0 || index > Voices.Count)
            {
                return Voices[0];
            }
            return Voices[index];
        }
        public static string GetVoiceOrd(int index)
        {
            if (index < 0 || index > VoiceOrder.Count)
            {
                return VoiceOrder[0];
            }
            return VoiceOrder[index];
        }
        public static string GetFlag(string voice)
        {
            return voice.Replace('-', '_').ToLower().Trim() + ".png";
        }
        public static string[] GetCategoriesArray()
        {
            var task = Task.Run(() => GetStringCategories());
            var categories = task.Result;
            return categories.ToArray();
        }

        public static async Task<string> GetFileContents(string filename)
        {
#if ANDROID
#elif IOS
#endif
            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync(filename);
            //StreamReader reader = new StreamReader(fileStream);
            using StreamReader reader = new StreamReader(fileStream);
            return await reader.ReadToEndAsync();
            //return reader;
        }

        public static void LoadVerbs(string lang = "en", string filename = "en_verbs.txt")
        {
            var taskA = Task.Run(async () => await GetFileContents(filename));
            var fileData = taskA.Result;
            var lineNr = 0;
            var lines = fileData.Split('\n');

            foreach (var line in lines)
            {
                lineNr++;
                var tokens = line?.Trim().Split(',');
                if (tokens == null || tokens.Length < 5)
                {
                    return;
                }
                Verbs[lang + "_" + tokens[0]] = line;
            }
            Console.WriteLine("Loaded {0} verbs from {1}.", lineNr, filename);
        }
        public static void LoadData()
        {
            if (Word.Default != null)
            {
                return;
            }
            LoadWords();
            LoadVerbs("en", "en_verbs.txt");
            LoadVerbs("es", "es_verbs.txt");
            LoadVerbs("de", "de_verbs.txt");
            LoadVerbs("ru", "ru_verbs.txt");
        }
        public static void LoadWords(string filename = "dictionary.txt")
        {
            var taskA = Task.Run(async () => await GetFileContents(filename));
            var fileData = taskA.Result;

            /*var task = Task.Run(async () => await FileSystem.OpenAppPackageFileAsync(filename));
            using var resourceStream = task.Result;
            var fs = resourceStream as FileStream;
            if (fs == null)
            {
                return;
            }
            using StreamReader sr = new(resourceStream);*/
            var lineNr = 0;
            var wordCounter = 0;
            var lines = fileData.Split('\n');

            foreach(var line in lines)
            {
            /*while (sr.Peek() >= 0)
            {
                var line = sr.ReadLine();*/
                lineNr++;
                var tokens = line?.Trim().Split('\t');
                if (tokens == null || tokens.Length < 2)
                {
                    continue;
                }
                if (lineNr <= s_startWords)
                {
                    List<string> data = lineNr == 1 ? Voices :
                                        lineNr == 2 ? Languages :
                                        lineNr == 3 ? Codes :
                                        lineNr == 4 ? Countries :
                                                      VoiceOrder;
                    var tokenNr = 0;
                    foreach (var token in tokens)
                    {
                        if (++tokenNr < 2)
                        {
                            continue;
                        }
                        data.Add(token);
                    }

                    continue;
                }

                var category = Categories.AddGetCategory(tokens[0].Trim());
                var word = new Word(tokens[1].Trim(), category);
                var prev = "";
                for (int i = 1; i < tokens.Length && i < Voices.Count + 1; i++)
                {
                    var token = tokens[i].Trim();
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        token = prev;
                    }
                    var voice = Voices[i - 1];
                    word.AddTranslation(token, voice);
                    prev = token;
                }
                category.AddWord(word);
                if (category != Categories.DefaultCategory)
                {
                    Categories.DefaultCategory.AddWord(word);
                }
                wordCounter++;
            }
            Console.WriteLine("Loaded {0} words and {1} categories.", wordCounter, Categories.GetTotal());

            var randomWords = QuizPage.GetRandom(Categories.DefaultCategory.GetTotalWords(), Categories.DefaultCategory.GetTotalWords());
            Categories.DefaultCategory.SetIndices(randomWords);
        }
    }
}

public class Word
{
    static int globalId;
    public static Word? Default { get;
        set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public Category Category { get; set; }
    public Dictionary<string, string> Translation = new Dictionary<string, string>();

    public Word(string name, Category category)
    {
        Name = name;
        Category = category;
        Id = globalId++;
        if (Default == null)
        {
            Default = this;
        }
    }

    public void AddTranslation(string translation, string voice)
    {
        Translation[voice] = translation;
    }
    public string GetTranslation(string voice)
    {
        if (Translation.TryGetValue(voice, out string? res))
        {
            return res;
        }
        return Name;
    }
    public static string GetFlag(string voice)
    {
        var filename = voice.Replace("-", "_").ToLower() + ".png";
        if (filename.StartsWith("transcript_"))
        {
            filename = filename.Substring(11);
        }
        return filename;
    }
    public string GetImage()
    {
        var name = Name.ToLower();
        // special cases word files not permitted on Android:
        if (name == "class" || name == "short" || name == "turkey")
        { //"a__case.png"
            return "a__" + name + ".png";
        }
        var core = name.Replace("-", "_").Replace(" ", "_").Replace("'", "_").Replace(",", "_").Replace("ü", "u").
            Replace("(", "_").Replace(")", "_").
            Replace("é", "e");
        core = core.TrimStart('_').TrimEnd('_');
        return core + ".png";
    }
}
public class Category
{
    public int Index { get; set; }
    public string Name { get; private set; }
    public bool IsText { get; private set; }
    public List<Word> Words { get; private set; } = new List<Word>();
    Dictionary<string, int> WordMap = new Dictionary<string, int>();
    List<int>? Indices { get; set; }
    List<int>? IndicesReverse { get; set; }

    public Category(string _id)
    {
        Name = _id;
        IsText = _id.ToLower().Contains("phrases");
    }
    public void SetIndices(List<int> indices)
    {
        Indices = indices;
        IndicesReverse = Enumerable.Range(0, indices.Count).ToList();//new List<int>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            if (indices[i] == 0 && i != 0)
            { // must be the first word
                indices[i] = indices[0];
                IndicesReverse[indices[0]] = indices[i];
                indices[0] = 0;
                IndicesReverse[0] = 0;
            }
            else
            {
                IndicesReverse[indices[i]] = i;
            }
        }
    }
    public void AddWord(Word word)
    {
        if (Words.Contains(word))
        {
            return;
        }
        Words.Add(word);
        WordMap[word.Name] = Words.Count - 1;
    }
    public Word GetWord(int ind = 0)
    {
        if (ind >= Words.Count)
        {
            ind = 0;
        }
        if (ind < 0)
        {
            ind = Words.Count - 1;
        }
        var wordIndex = Indices == null ? ind : Indices[ind];
        return Words[wordIndex];
    }
    public int GetIndex(Word word)
    {
        if (!WordMap.TryGetValue(word.Name, out int index))
        {
            return -1;
        }

        return IndicesReverse == null ? index : IndicesReverse[index];
    }

    public Word GetNextWord()
    {
        Index++;
        if (Index >= Words.Count)
        {
            Index = 0;
        }
        var wordIndex = Indices == null ? Index : Indices[Index];
        return Words[wordIndex];
    }
    public Word GetPrevWord()
    {
        Index--;
        if (Index < 0)
        {
            Index = Words.Count - 1;
        }
        var wordIndex = Indices == null ? Index : Indices[Index];
        return Words[wordIndex];
    }
    public int GetTotalWords()
    {
        return Words.Count;
    }
}
public class Categories
{
    public const string DefaultCategoryName = "all";
    public static Category DefaultCategory = new Category(DefaultCategoryName);
    public static Category CurrentCategory;

    public static List<string> CategoryList = new List<string>();
    static Dictionary<string, Category> s_categMap = new Dictionary<string, Category>();
    static Dictionary<string, int> s_categIndex = new Dictionary<string, int>();

    public Categories()
    {
        s_categMap[DefaultCategoryName] = DefaultCategory;
        CategoryList.Add(DefaultCategoryName);
        CurrentCategory = DefaultCategory;
    }

    public static Category AddGetCategory(string name)
    {
        if (s_categMap.TryGetValue(name, out Category? cat))
        {
            return cat;
        }
        cat = new Category(name);
        s_categMap[name] = cat;
        CategoryList.Add(name);
        s_categIndex[name] = CategoryList.Count - 1;
        CurrentCategory = cat;
        return cat;
    }
    public static int GetCategoryIndex(string name)
    {
        if (!s_categIndex.TryGetValue(name, out int id))
        {
            return -1;
        }
        return id;
    }

    public static Category SwitchCategory(string name)
    {
        CurrentCategory = GetCategory(name);
        return CurrentCategory;
    }
    public static Category SwitchCategory(int index)
    {
        if (CategoryList.Count == 0)
        {
            Words.LoadData();
        }
        var name = index < 0 || index >= CategoryList.Count ? DefaultCategory.Name :
                   CategoryList[index];
        return SwitchCategory(name);
    }
    public static Category GetCategory(string name)
    {
        if (CategoryList.Count == 0)
        {
            Words.LoadData();
        }
        if (!s_categMap.TryGetValue(name, out Category? cat))
        {
            return DefaultCategory;
        }
        return cat;
    }
    public static Category GetCategory(int index)
    {
        if (CategoryList.Count == 0)
        {
            Words.LoadData();
        }
        if (index < 0 || index >= CategoryList.Count)
        {
            return DefaultCategory;
        }
        var name = CategoryList[index];
        return GetCategory(name);
    }
    public static int GetTotal()
    {
        return CategoryList.Count;
    }
    public static Category[] GetCategories()
    {
        return s_categMap.Values.ToArray();
    }
}
