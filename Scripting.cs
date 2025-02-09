using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using CSCS.InterpreterManager;
using CSCSMath;
using Microsoft.Maui.Storage;
using SplitAndMerge;

namespace ScriptingMaui;

public class Scripting
{
    const string CSCSHandler = "{0}@clicked";

    protected static InterpreterManagerModule s_interpreterManager = new InterpreterManagerModule();
    public Interpreter InterpreterInstance { get; private set; }
    public Dictionary<string, IVisualTreeElement> Controls { get; private set; } = new Dictionary<string, IVisualTreeElement>();

    internal Dictionary<string, object> VarCache { get; set; } = new Dictionary<string, object>();

    public static Scripting CurrentInstance { get; private set; }
    public double lol { get; set; }
    public List<string> m_data { get; set; } = new List<string>();


    public Scripting()
    {
        CurrentInstance = this;
        s_interpreterManager.Modules = GetModuleList();
        var interpreterId = s_interpreterManager.NewInterpreter();
        s_interpreterManager.SetInterpreter(interpreterId);
        InterpreterInstance = s_interpreterManager.GetInterpreter(interpreterId);
        RegisterFunctions();
    }

    public void RegisterFunctions()
    {
        InterpreterInstance.RegisterFunction("Run", new RunScriptFunction());

        InterpreterInstance.RegisterFunction("GetValue", new GetSetValueFunction(true));
        InterpreterInstance.RegisterFunction("SetValue", new GetSetValueFunction(false));
        InterpreterInstance.RegisterFunction("Show", new ShowHideFunction(true));
        InterpreterInstance.RegisterFunction("Hide", new ShowHideFunction(false));
        InterpreterInstance.RegisterFunction("Focus", new FocusUnfocusFunction(true));
        InterpreterInstance.RegisterFunction("Unfocus", new FocusUnfocusFunction(false));
        InterpreterInstance.RegisterFunction("Schedule", new TimerFunction(true));
        InterpreterInstance.RegisterFunction("CancelSchedule", new TimerFunction(false));

        InterpreterInstance.RegisterFunction("RunOnMain", new RunOnMainFunction());
        InterpreterInstance.RegisterFunction("PrintConsole", new PrintConsoleFunction());
        InterpreterInstance.RegisterFunction("ImportFile", new ImportFileFunction());

        InterpreterInstance.RegisterFunction("Cache", new CacheFunction(true));
        InterpreterInstance.RegisterFunction("GetCache", new CacheFunction(false));
        InterpreterInstance.RegisterFunction("InitSyncfusion", new SyncfusionInitFunction());

        InterpreterInstance.RegisterFunction("time:year", new MyDateTimeFunction("yyyy"));
        InterpreterInstance.RegisterFunction("time:month", new MyDateTimeFunction("MM"));
        InterpreterInstance.RegisterFunction("time:day", new MyDateTimeFunction("dd"));
        InterpreterInstance.RegisterFunction("time:hour", new MyDateTimeFunction("HH"));
        InterpreterInstance.RegisterFunction("time:minute", new MyDateTimeFunction("mm"));
        InterpreterInstance.RegisterFunction("time:second", new MyDateTimeFunction("ss"));
        InterpreterInstance.RegisterFunction("time:millis", new MyDateTimeFunction("fff"));
        InterpreterInstance.RegisterFunction("timestamp", new MyDateTimeFunction("yyyy/MM/dd HH:mm:ss.fff"));
    }

    public void Start(string filename = "start.cscs")
    {
        Task.Run(() => StartAsync(filename)).Wait();
    }
    public async Task StartAsync(string filename = "start.cscs")
    {
        using var resourceStream = await FileSystem.OpenAppPackageFileAsync(filename);
#if IOS
        var fs = resourceStream as FileStream;
        if (fs != null)
        {
            string path = fs.Name;
            RunFile(path);
        }
#else
        using StreamReader reader = new StreamReader(resourceStream);
        var script = reader.ReadToEnd();
        CurrentInstance.Run(script);
#endif

    }

    public void Init(List<ContentPage> pages)
    {
        foreach (var page in pages)
        {
            if (!string.IsNullOrWhiteSpace(page.StyleId))
            {
                Controls[page.StyleId] = page;
            }
            var elements = (List<IVisualTreeElement>)page.GetVisualTreeDescendants();
            foreach (var elem in elements)
            {
                if (elem is Button btn && !string.IsNullOrWhiteSpace(btn.StyleId))
                {
                    Controls[btn.StyleId] = btn;
                    btn.Clicked += Clicked;
                }
                else if (elem is ImageButton ib && !string.IsNullOrWhiteSpace(ib.StyleId))
                {
                    Controls[ib.StyleId] = ib;
                    ib.Clicked += Clicked;
                }
                else if (elem is Entry entry && !string.IsNullOrWhiteSpace(entry.StyleId))
                {
                    Controls[entry.StyleId] = entry;
                    entry.TextChanged += Clicked;
                }
                else if (elem is CheckBox cb && !string.IsNullOrWhiteSpace(cb.StyleId))
                {
                    Controls[cb.StyleId] = cb;
                    cb.CheckedChanged += Clicked;
                }
                else if (elem is RadioButton rb && !string.IsNullOrWhiteSpace(rb.StyleId))
                {
                    Controls[rb.StyleId] = rb;
                    rb.CheckedChanged += Clicked;
                }
                else if (elem is Stepper stp && !string.IsNullOrWhiteSpace(stp.StyleId))
                {
                    Controls[stp.StyleId] = stp;
                    stp.ValueChanged += Clicked;
                }
                else if (elem is Picker p && !string.IsNullOrWhiteSpace(p.StyleId))
                {
                    Controls[p.StyleId] = p;
                    p.SelectedIndexChanged += Clicked;
                }
                else if (elem is Syncfusion.Maui.Picker.SfPicker sfp && !string.IsNullOrWhiteSpace(sfp.StyleId))
                {
                    Controls[sfp.StyleId] = sfp;
                    sfp.SelectionChanged += Clicked;
                }
                else if (elem is Syncfusion.Maui.ListView.SfListView sfl && !string.IsNullOrWhiteSpace(sfl.StyleId))
                {
                    Controls[sfl.StyleId] = sfl;
                    sfl.ItemTapped += Clicked;
                }
                else if (elem is Label lb && !string.IsNullOrWhiteSpace(lb.StyleId))
                {
                    Controls[lb.StyleId] = lb;
                }
                else if (elem is StackLayout sl && !string.IsNullOrWhiteSpace(sl.StyleId))
                {
                    Controls[sl.StyleId] = sl;
                }
                else if (elem is HorizontalStackLayout hsl && !string.IsNullOrWhiteSpace(hsl.StyleId))
                {
                    Controls[hsl.StyleId] = hsl;
                }
                else if (elem is VerticalStackLayout vsl && !string.IsNullOrWhiteSpace(vsl.StyleId))
                {
                    Controls[vsl.StyleId] = vsl;
                }
                else if (elem is Grid gr && !string.IsNullOrWhiteSpace(gr.StyleId))
                {
                    Controls[gr.StyleId] = gr;
                }
            }
        }

        Register(nameof(lol), this);
        Register(nameof(m_data), this);
        Register(nameof(LearnPage.Context.TransInfo), LearnPage.Context);

        Variable data = new Variable(Variable.VarType.ARRAY);
        data.Tuple.Add(new Variable("lol"));
        data.Tuple.Add(new Variable("lala"));

        UpdateValue(nameof(m_data), data);
    }

    private void Clicked(object? sender, EventArgs e)
    {
        string funcName = "";
        Variable? arg1 = null;
        Variable? arg2 = null;
        Variable? arg3 = null;
        if (sender is Button btn)
        {
            funcName = string.Format(CSCSHandler, btn.StyleId);
            arg1 = new Variable(btn.StyleId);
            arg2 = new Variable(btn.Text);
        }
        else if (sender is ImageButton ib)
        {
            funcName = string.Format(CSCSHandler, ib.StyleId);
            arg1 = new Variable(ib.StyleId);
        }
        else if (sender is Entry entry && e is TextChangedEventArgs txtArgs)
        {
            funcName = string.Format(CSCSHandler, entry.StyleId);
            arg1 = new Variable(entry.StyleId);
            arg2 = new Variable(txtArgs.NewTextValue);
            arg3 = new Variable(txtArgs.OldTextValue);
        }
        else if (sender is CheckBox cb && e is CheckedChangedEventArgs chArgs)
        {
            funcName = string.Format(CSCSHandler, cb.StyleId);
            arg1 = new Variable(cb.StyleId);
            arg2 = new Variable(chArgs.Value);
        }
        else if (sender is RadioButton rb && e is CheckedChangedEventArgs rbArgs)
        {
            funcName = string.Format(CSCSHandler, rb.StyleId);
            arg1 = new Variable(rb.StyleId);
            arg2 = new Variable(rb.GroupName);
            arg3 = new Variable(rbArgs.Value);
        }
        else if (sender is Stepper stp && e is ValueChangedEventArgs valArgs)
        {
            funcName = string.Format(CSCSHandler, stp.StyleId);
            arg1 = new Variable(stp.StyleId);
            arg2 = new Variable(valArgs.NewValue);
        }
        else if (sender is Picker p)
        {
            funcName = string.Format(CSCSHandler, p.StyleId);
            arg1 = new Variable(p.StyleId);
            arg2 = new Variable(p.SelectedIndex);
            arg3 = new Variable(p.SelectedItem);
        }
        else if (sender is Syncfusion.Maui.Picker.SfPicker sfp &&
            e is Syncfusion.Maui.Picker.PickerSelectionChangedEventArgs sfpArgs)
        {
            funcName = string.Format(CSCSHandler, sfp.StyleId);
            arg1 = new Variable(sfp.StyleId);
            arg2 = new Variable(sfpArgs.ColumnIndex);
            arg3 = new Variable(sfpArgs.NewValue);
        }
        else if (sender is Syncfusion.Maui.ListView.SfListView sfl &&
            e is Syncfusion.Maui.ListView.ItemTappedEventArgs sflArgs)
        {
            funcName = string.Format(CSCSHandler, sfl.StyleId);
            arg1 = new Variable(sfl.StyleId);
            arg2 = new Variable(sflArgs.Position);
            arg3 = new Variable(sflArgs.DataItem);
        }

        RunScript(funcName, arg1, arg2, arg3);
    }

    public static Variable RunScript(string funcName, Variable? arg1 = null, Variable? arg2 = null, Variable? arg3 = null)
    {
        try
        {
            return CurrentInstance.InterpreterInstance.Run(funcName, arg1, arg2, arg3);
        }
        catch (Exception exc)
        {
            Console.WriteLine("Exception: " + exc.Message);
            Console.WriteLine(exc.StackTrace);
        }
        return Variable.EmptyInstance;
    }


    public static Variable RunFile(string filename)
    {
        return CurrentInstance.Run(string.Empty, filename);
    }
    public static Variable RunScript(string filename)
    {
        return CurrentInstance.Run(filename);
    }

    protected static List<ICscsModule> GetModuleList()
    {
        return new List<ICscsModule>
        {
            new CscsMathModule(),
            s_interpreterManager
        };
    }
    private Variable Run(string script, string filename = "")
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            script = ImportFileFunction.GetFileContents(filename);
        }
        string data = Utils.ConvertToScript(InterpreterInstance, script, out Dictionary<int, int> char2Line, filename);
        ParsingScript toParse = new ParsingScript(InterpreterInstance, data, 0, char2Line);
        toParse.OriginalScript = script;
        toParse.Filename = filename;

        string errorMsg = string.Empty;
        Variable result = Variable.EmptyInstance;
        try
        {
            if (!string.IsNullOrWhiteSpace(filename))
            {
                result = Task.Run(() =>
                InterpreterInstance.ProcessFile(filename, true)).Result;
            }
            else
            {
                result = Task.Run(() =>
                    InterpreterInstance.Process(script, filename, true)).Result;
            }
        }
        catch (Exception exc)
        {
            errorMsg = exc.InnerException != null ? exc.InnerException.Message : exc.Message;
            InterpreterInstance.InvalidateStacksAfterLevel(0);
        }
        return result;
    }

    public static void RunOnMainThread(CustomFunction callbackFunction,
        string? arg1 = null, string? arg2 = null, string? arg3 = null)
    {
        List<Variable> args = new List<Variable>();
        if (arg1 != null)
        {
            args.Add(new Variable(arg1));
        }
        if (arg2 != null)
        {
            args.Add(new Variable(arg2));
        }
        if (arg3 != null)
        {
            args.Add(new Variable(arg3));
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            callbackFunction.Run(args);
        });
    }

    public static void RunFunctionOnMainThread(ParserFunction func, ParsingScript script)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            func.GetValue(script);
        });
    }

    Dictionary<string, CommonData> CommonValues = new Dictionary<string, CommonData>();

    public class CommonData
    {
        public object OwnerObject { get; set; }
        public Type VariableType { get; set; }
        public string VariableName { get; set; }

        public void SetValue(object value)
        {
            var t = OwnerObject.GetType();
            var prop = t.GetProperty(VariableName);
            prop.SetValue(OwnerObject, value);
        }
        public object GetValue()
        {
            var t = OwnerObject.GetType();
            var prop = t.GetProperty(VariableName);
            return prop.GetValue(OwnerObject);
        }
    }
    public bool Register(string variableName, object callingObj)
    {
        var tp = callingObj.GetType();
        var field = tp.GetField(variableName);
        var mem = tp.GetMember(variableName);
        var props = tp.GetProperties();
        var prop = tp.GetProperty(variableName);
        if (prop == null)
        {
            return false;
        }
        var t = prop.PropertyType;
        var data = new CommonData() { OwnerObject = callingObj, VariableName = variableName, VariableType = t };
        CommonValues[variableName.ToLower()] = data;
        return true;
    }
    public bool UpdateValue(string variableName, Variable newValue)
    {
        if (!CommonValues.TryGetValue(variableName.ToLower(), out CommonData data))
        {
            return false;
        }
        var currentValue = data.GetValue();
        var newt = newValue.GetType();
        var thetype = data.VariableType;
        if (thetype is int)
        {
            data.SetValue(newValue.AsInt());
        }
        else if (thetype is double)
        {
            data.SetValue(newValue.AsDouble());
        }
        else if (thetype is bool)
        {
            data.SetValue(newValue.AsBool());
        }
        else if (thetype is DateTime)
        {
            data.SetValue(newValue.AsDateTime());
        }
        else if (thetype is string)
        {
            data.SetValue(newValue.AsString());
        }
        else if (thetype.ToString().Contains("ObservableCollection"))
        {
            if (newValue.Type != Variable.VarType.ARRAY || newValue.Tuple == null)
            {
                return false;
            }
            var newdata = new ObservableCollection<object>();
            var coll = data.GetValue() as ObservableCollection<object>;
            for (int i = 0; i < newValue.Tuple.Count; i++)
            {
                //newdata.Add(newValue.Tuple[i].AsString());
                coll.Add(newValue.Tuple[i].AsString());
            }
            //data.SetValue(newdata);
        }
        else if (thetype.ToString().Contains("List"))
        {
            if (newValue.Type != Variable.VarType.ARRAY || newValue.Tuple == null)
            {
                return false;
            }
            var newdata = new List<string>();
            for (int i = 0; i < newValue.Tuple.Count; i++)
            {
                newdata.Add(newValue.Tuple[i].AsString());
            }
            data.SetValue(newdata);
        }
        else
        {
            int i = 0;
        }
        var currentValue2 = data.GetValue();
        return true;
    }

    public void Cache(string key, object obj)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            VarCache[key] = obj;
        });
    }
    public object? GetCache(string key)
    {
        object? result = null;
        var task = MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!VarCache.TryGetValue(key, out result))
                result = null;
        });
        task.Wait();
        return result;
    }
}

class ImportFileFunction : ParserFunction
{
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name, true);

        string filename = args[0].AsString();

        string fileContents = GetFileContents(filename);

        Variable result = RunScriptFunction.Execute(fileContents, filename);
        return result;
    }
    public static string GetFileContents(string filename)
    {
        string fileContents = string.Empty;
#if IOS
        fileContents = File.ReadAllText(filename);
#else
        using var rs =  FileSystem.OpenAppPackageFileAsync(filename);
        rs.Wait();
        var resourceStream = rs.Result;
        if (resourceStream != null)
        {
            using StreamReader reader = new StreamReader(resourceStream);
            fileContents = reader.ReadToEnd();
        }
#endif
        if (string.IsNullOrWhiteSpace(fileContents))
        {
            throw new ArgumentException("Couldn't read file [" + filename +
                                        "] from disk.");
        }
        return fileContents;
    }
}

public class CacheFunction : ParserFunction
{
    bool m_store;
    public CacheFunction(bool store = true)
    {
        m_store = store;
    }

    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name);

        string key = args[0].AsString();
        if (m_store)
        {
            var val = Utils.GetSafeVariable(args, 1);
            Scripting.CurrentInstance.Cache(key, val);
            return Variable.EmptyInstance;
        }

        if (!Scripting.CurrentInstance.VarCache.TryGetValue(key, out object? data))
        {
            return Variable.EmptyInstance;
        }
        if (data is Variable var)
        {
            return var;
        }
        if (data is int varint)
        {
            return new Variable(varint);
        }
        if (data is bool varbool)
        {
            return new Variable(varbool);
        }
        if (data is double vard)
        {
            return new Variable(vard);
        }

        return Variable.EmptyInstance;
    }
}

public class FocusUnfocusFunction : ParserFunction
{
    bool m_focus;
    public FocusUnfocusFunction(bool focus = true)
    {
        m_focus = focus;
    }

    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name);

        string varname = args[0].AsString();
        if (!Scripting.CurrentInstance.Controls.TryGetValue(varname, out IVisualTreeElement? control))
        {
            return Variable.EmptyInstance;
        }

        if (control is VisualElement ve)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (m_focus)
                {
                    ve.Focus();
                }
                else
                {
                    ve.Unfocus();
                }
            });
        }

        return Variable.EmptyInstance;
    }
}

public class ShowHideFunction : ParserFunction
{
    bool m_show;
    public ShowHideFunction(bool show = true)
    {
        m_show = show;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name);

        string varname = args[0].AsString();
        if (!Scripting.CurrentInstance.Controls.TryGetValue(varname, out IVisualTreeElement? control))
        {
            return Variable.EmptyInstance;
        }

        if (m_show && control is ContentPage cp)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage.Instance.CurrentPage = cp;
            });
        }
        else if (control is VisualElement ve)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ve.IsVisible = m_show;
            });
        }
        else
        {
            return Variable.EmptyInstance;
        }

        return new Variable(true);
    }
}

public class GetSetValueFunction : ParserFunction
{
    bool m_getMode;
    public GetSetValueFunction(bool getValue = true)
    {
        m_getMode = getValue;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name);

        string varname = args[0].AsString();
        if (!Scripting.CurrentInstance.Controls.TryGetValue(varname, out IVisualTreeElement? control))
        {
            return Variable.EmptyInstance;
        }

        Variable result = Variable.EmptyInstance;
        if (m_getMode)
        {
            var task = MainThread.InvokeOnMainThreadAsync(() =>
            {
                result = GetValue(control);
            });
            task.Wait();
            return result;
        }

        Variable newValue = Utils.GetSafeVariable(args, 1);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            result = SetValue(control, newValue);
        });
        return result;
    }

    Variable GetValue(IVisualTreeElement elem)
    {
        Variable result = Variable.EmptyInstance;
        if (elem is Button btn)
        {
            result = new Variable(btn.Text);
        }
        else if (elem is ImageButton ib)
        {
            result = new Variable(ib.Source);
        }
        else if (elem is Label lbl)
        {
            result = new Variable(lbl.Text);
        }
        else if (elem is Entry entry)
        {
            result = new Variable(entry.Text);
        }
        else if (elem is CheckBox cb)
        {
            result = new Variable(cb.IsChecked);
        }
        else if (elem is RadioButton rb)
        {
            result = new Variable(rb.IsChecked);
        }
        else if (elem is Stepper stp)
        {
            result = new Variable(stp.Value);
        }
        else if (elem is Picker p)
        {
            result = new Variable(Variable.VarType.ARRAY);
            result.Tuple.Add(new Variable(p.SelectedIndex));
            result.Tuple.Add(new Variable(p.SelectedItem));
        }
        else if (elem is Syncfusion.Maui.Picker.SfPicker sfp)
        {
            result = new Variable(Variable.VarType.ARRAY);
            for (int i = 0; i < sfp.Columns.Count; i++)
            {
                result.Tuple.Add(new Variable(sfp.Columns[i].SelectedIndex));
                result.Tuple.Add(new Variable(sfp.Columns[i].SelectedItem));
            }
        }
        else if (elem is Syncfusion.Maui.ListView.SfListView sfl)
        {
            result = new Variable(Variable.VarType.ARRAY);
            result.Tuple.Add(sfl.SelectedItem == null ? Variable.EmptyInstance : new Variable(sfl.SelectedItem.ToString()));
            var items = sfl.ItemsSource as ObservableCollection<object>;
            if (items == null || items.Count == 0)
            {
                return result;
            }
        }
        return result;
    }
    Variable SetValue(IVisualTreeElement elem, Variable newValue)
    {
        Variable result = Variable.EmptyInstance;
        if (elem is Button btn)
        {
            btn.Text = newValue.AsString();
        }
        else if (elem is ImageButton ib)
        {
            ib.Source = newValue.AsString();
        }
        else if (elem is Label lbl)
        {
            lbl.Text = newValue.AsString();
        }
        else if (elem is Entry entry)
        {
            entry.Text = newValue.AsString();
        }
        else if (elem is CheckBox cb)
        {
            cb.IsChecked = newValue.AsBool();
        }
        else if (elem is RadioButton rb)
        {
            rb.IsChecked = newValue.AsBool();
        }
        else if (elem is Stepper stp)
        {
            stp.Value = newValue.AsDouble();
        }
        else if (elem is Picker p)
        {
            if (newValue.Type == Variable.VarType.NUMBER)
            {
                p.SelectedIndex = newValue.AsInt();
            }
            else
            {
                p.SelectedItem = newValue.AsString();
            }
        }
        else if (elem is Syncfusion.Maui.Picker.SfPicker sfp)
        {
            if (newValue.Type == Variable.VarType.ARRAY)
            {
                for (int i = 0; i < newValue.Tuple.Count && i < sfp.Columns.Count; i++)
                {
                    if (newValue.Tuple[i].Type == Variable.VarType.NUMBER)
                    {
                        sfp.Columns[i].SelectedIndex = newValue.Tuple[i].AsInt();
                    }
                    else
                    {
                        sfp.Columns[i].SelectedItem = newValue.Tuple[i].AsString();
                    }
                }
            }
        }
        else if (elem is Syncfusion.Maui.ListView.SfListView sfl)
        {
            sfl.SelectedItem = newValue.AsString();
        }
        return result;
    }
}

class RunScriptFunction : ParserFunction
{
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name);

        string strScript = Utils.GetSafeString(args, 0);
        Variable result = Variable.EmptyInstance;

        ParserFunction.StackLevelDelta++;
        try
        {
            result = Execute(strScript);
        }
        finally
        {
            ParserFunction.StackLevelDelta--;
        }

        return result != null ? result : Variable.EmptyInstance;
    }

    public static Variable Execute(string text, string filename = "")
    {
        string[] lines = text.Split(new char[] { '\n' });

        Dictionary<int, int> char2Line;
        string includeScript = Utils.ConvertToScript(Scripting.CurrentInstance.InterpreterInstance, text, out char2Line);
        ParsingScript tempScript = new ParsingScript(Scripting.CurrentInstance.InterpreterInstance, includeScript, 0, char2Line);
        tempScript.Filename = filename;
        tempScript.OriginalScript = string.Join(Constants.END_LINE.ToString(), lines);

        Variable result = Variable.EmptyInstance;
        while (tempScript.Pointer < includeScript.Length)
        {
            result = tempScript.Execute();
            tempScript.GoToNextStatement();
        }
        return result;
    }
}

public class PrintConsoleFunction : ParserFunction
{
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        for (int i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            System.Diagnostics.Debug.Write(arg.ToString());
        }
        System.Diagnostics.Debug.WriteLine("");
        return Variable.EmptyInstance;
    }
    public static void Print(string msg)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        System.Diagnostics.Debug.WriteLine(timestamp + " " + Environment.CurrentManagedThreadId + " " + msg);
    }
}

public class RunOnMainFunction : ParserFunction
{
    protected override Variable Evaluate(ParsingScript script)
    {
        string funcName = Utils.GetToken(script, Constants.NEXT_OR_END_ARRAY);
        var initPointer = script.Pointer;

        List<Variable> args = script.GetFunctionArgs();

        string arg1 = Utils.GetSafeString(args, 0, null);
        string arg2 = Utils.GetSafeString(args, 1, null);
        string arg3 = Utils.GetSafeString(args, 2, null);

        ParserFunction func = InterpreterInstance.GetFunction(funcName);
        Utils.CheckNotNull(funcName, func, script);

        var customFunc = func as CustomFunction;
        if (customFunc != null)
        {
            Scripting.RunOnMainThread(customFunc, arg1, arg2, arg3);
            return Variable.EmptyInstance;
        }

        ParsingScript tempScript = script.GetTempScript(script.String, initPointer);
        PrintConsoleFunction.Print("RunOnMain rest=" + tempScript.Rest);
        Scripting.RunFunctionOnMainThread(func, tempScript);
        //Thread.Sleep(100);
        return Variable.EmptyInstance;
    }
}

public class TimerFunction : ParserFunction
{
    static int TimerId { get; set; }
    static Dictionary<string, System.Timers.Timer> m_timers =
       new Dictionary<string, System.Timers.Timer>();

    bool m_startTimer;

    public TimerFunction(bool startTimer)
    {
        m_startTimer = startTimer;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();

        if (!m_startTimer)
        {
            Utils.CheckArgs(args.Count, 1, m_name);
            string cancelTimerId = Utils.GetSafeString(args, 0);
            if (m_timers.TryGetValue(cancelTimerId, out System.Timers.Timer? cancelTimer))
            {
                cancelTimer.Stop();
                cancelTimer.Dispose();
                m_timers.Remove(cancelTimerId);
            }
            return Variable.EmptyInstance;
        }

        Utils.CheckArgs(args.Count, 2, m_name);
        int timeout = args[0].AsInt();
        string strAction = args[1].AsString();
        string owner = Utils.GetSafeString(args, 2);
        bool autoReset = Utils.GetSafeInt(args, 3, 0) != 0;
        TimerId++;

        System.Timers.Timer pauseTimer = new System.Timers.Timer(timeout);
        pauseTimer.Elapsed += (sender, e) =>
        {
            if (!autoReset)
            {
                pauseTimer.Stop();
                pauseTimer.Dispose();
                m_timers.Remove(TimerId.ToString());
            }
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Scripting.RunScript(strAction, new Variable(owner), new Variable(TimerId));
            });
        };
        pauseTimer.AutoReset = autoReset;
        m_timers[TimerId.ToString()] = pauseTimer;

        pauseTimer.Start();
        return new Variable(TimerId);
    }
}
public class MyDateTimeFunction : ParserFunction
{
    string m_format;
    public MyDateTimeFunction(string format)
    {
        m_format = format;
    }
    protected override Variable Evaluate(ParsingScript script)
    {
        return new Variable(DateTime.Now.ToString(m_format));
    }
}

internal class SyncfusionInitFunction : ParserFunction
{
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();

        string licenseKey = Utils.GetSafeString(args, 0, "MzQzMTk4NUAzMjM2MmUzMDJlMzBXSDViRm1qTnZ3UTUrek9SR2orMlFJVDVPUWk0LzNHRFhqNno1eXZEOTI0PQ==");
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        return Variable.EmptyInstance;
    }
}
