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
using System.Text;

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
                    break;
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
            LoadVerbs("it", "it_verbs.txt");
            LoadVerbs("fr", "fr_verbs.txt");
            LoadVerbs("pt", "pt_verbs.txt");
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
            Categories.DefaultCategory = Categories.AddGetCategory(Categories.DefaultCategoryName);
            Categories.CustomCategory = Categories.AddGetCategory(Categories.CustomCategoryName);

            var lineNr = 0;
            var wordCounter = 0;
            var lines = fileData.Split('\n');

            foreach(var line in lines)
            {
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
            randomWords.Remove(0); // let's have first element aways at position 0:
            randomWords.Insert(0, 0);
            Categories.DefaultCategory.SetIndices(randomWords);

            Categories.TrashCategory = Categories.AddGetCategory(Categories.TrashCategoryName);
        }
    }
}

public class Word
{
    static int globalId;
    public static Word? Default { get;
        set; }
    public int Id { get; set; }
    public int OriginalId { get; set; }
    public string Name { get; set; }
    public bool IsTrash { get; set; }
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
    public int Index { get;
        set; }
    public string Name { get; private set; }
    public bool IsText { get; private set; }
    public List<Word> CatWords { get; private set; } = new List<Word>();
    Dictionary<string, int> WordMap = new Dictionary<string, int>();

    public Category(string _id)
    {
        Name = _id;
        IsText = _id.ToLower().Contains("phrases");
    }
    public void SetIndices(List<int> indices)
    {
        Word[] words = new Word[CatWords.Count];
        CatWords.CopyTo(words);
        CatWords.Clear();
        for (int i = 0; i < indices.Count; i++)
        {
            var word = i < words.Length ? words[indices[i]] : null;
            AddWord(word);
        }
    }
    public bool AddWord(Word? word)
    {
        if (word == null || CatWords.Contains(word))
        {
            return false;
        }
        CatWords.Add(word);
        WordMap[word.Name] = CatWords.Count - 1;
        return true;
    }

    public bool RemoveWord(Word word)
    {
        var ind = CatWords.IndexOf(word);
        if (ind < 0)
        {
            return false;
        }
        bool removed = CatWords.Remove(word);
        WordMap.Remove(word.Name);
        for (int i = ind; i < CatWords.Count; i++)
        {
            WordMap[CatWords[i].Name] = i;
        }
        return removed;
    }
    public string GetAllWords()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var word in CatWords)
        {
            sb.Append(word.Name + "\t");
        }
        return sb.ToString();
    }
    public int InitFromWords(string words)
    {
        Words.LoadData();
        CatWords.Clear();
        WordMap.Clear();
        var tokens = words.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        int counter = 0;
        foreach (var wordStr in tokens)
        {
            var word = Categories.DefaultCategory.GetWord(wordStr);
            counter += AddWord(word) ? 1 : 0;
            if (this == Categories.TrashCategory)
            { // also delete from the word's category
                word?.Category.AddToTrash(word);
            }
        }
        return counter;
    }
    public void AddToTrash(Word word)
    {
        word.IsTrash = true;
        RemoveWord(word);
    }
    public void AddFromTrash(Word word)
    {
        word.IsTrash = false;
        if (CatWords.Contains(word))
        {
            return;
        }
        bool inserted = false; ;
        for (int i = 0; i < CatWords.Count; i++)
        {
            var current = CatWords[i];
            if (word.Id < current.Id)
            {
                CatWords.Insert(i, word);
                for (int j = i; j < CatWords.Count; j++)
                {
                    WordMap[CatWords[j].Name] = j;
                }
                inserted = true;
                break;
            }
        }
        if (!inserted)
        {
            AddWord(word);
        }
    }
    public Word? GetWord(int ind = 0)
    {
        if (ind >= CatWords.Count)
        {
            ind = 0;
        }
        if (ind < 0)
        {
            ind = CatWords.Count - 1;
        }
        return ind >= CatWords.Count || ind < 0 ? null : CatWords[ind];
    }
    public int GetIndex(Word word)
    {
        if (!WordMap.TryGetValue(word.Name, out int index))
        {
            return -1;
        }
        return index;
    }
    public Word? GetWord(string name)
    {
        if (!WordMap.TryGetValue(name, out int index) || index < 0 || index >= CatWords.Count)
        {
            return null;
        }

        return CatWords[index];
    }

    public bool Exists(string name)
    {
        return GetWord(name) != null;
    }

    public Word? GetNextWord()
    {
        Index++;
        if (Index >= CatWords.Count)
        {
            Index = 0;
        }
        return GetWord(Index);
    }
    public Word? GetPrevWord()
    {
        Index--;
        if (Index < 0)
        {
            Index = CatWords.Count - 1;
        }
        return GetWord(Index);
    }
    public int GetTotalWords()
    {
        return CatWords.Count;
    }
}
public class Categories
{
    public const string DefaultCategoryName = "all";
    public const string CustomCategoryName = "custom";
    public const string TrashCategoryName = "trash";
    public const string VerbCategoryName = "verbs";
    public static Category DefaultCategory;
    public static Category CustomCategory;
    public static Category TrashCategory;
    public static Category CurrentCategory;

    public static List<string> CategoryList = new List<string>();
    static Dictionary<string, Category> s_categMap = new Dictionary<string, Category>();
    static Dictionary<string, int> s_categIndex = new Dictionary<string, int>();

    public Categories()
    {
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
