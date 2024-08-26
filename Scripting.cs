using System;
using System.Data.SqlTypes;
using CSCS.InterpreterManager;
using CSCSMath;
using SplitAndMerge;

namespace ScriptingMaui;

public class Scripting
{
    protected static InterpreterManagerModule m_interpreterManager = new InterpreterManagerModule();
    public static Interpreter InterpreterInstance => m_interpreterManager.CurrentInterpreter;

    public static void RegisterFunctions()
    {
        InterpreterInstance.RegisterFunction("Run", new RunScriptFunction());

        InterpreterInstance.RegisterFunction("RunOnMain", new RunOnMainFunction());
        InterpreterInstance.RegisterFunction("PrintConsole", new PrintConsoleFunction());
        InterpreterInstance.RegisterFunction("ImportFile", new ImportFileFunction());

        InterpreterInstance.RegisterFunction("InitSyncfusion", new SyncfusionInitFunction());
    }

    public static void Start(string filename = "start.cscs")
    {
        m_interpreterManager.Modules = GetModuleList();

        var interpreterId = m_interpreterManager.NewInterpreter();
        m_interpreterManager.SetInterpreter(interpreterId);
        RegisterFunctions();
        Task.Run(() => StartAsync(filename)).Wait();
    }
    public static async Task StartAsync(string filename)
    {
        using var resourceStream = await FileSystem.OpenAppPackageFileAsync(filename);
        var fs = resourceStream as FileStream;
        if (fs != null)
        {
            string path = fs.Name;
            RunFile(path);
        }
    }

    public static Variable RunFile(string filename)
    {
        var scripting = new Scripting();
        return scripting.Run(string.Empty, filename);
    }
    public static Variable RunScript(string filename)
    {
        var scripting = new Scripting();
        return scripting.Run(filename);
    }

    protected static List<ICscsModule> GetModuleList()
    {
        return new List<ICscsModule>
        {
            new CscsMathModule(),
            m_interpreterManager
        };
    }
    private Variable Run(string script, string filename = "")
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            script = Utils.GetFileContents(filename);
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

}

class ImportFileFunction : ParserFunction
{
    protected override Variable Evaluate(ParsingScript script)
    {
        List<Variable> args = script.GetFunctionArgs();
        Utils.CheckArgs(args.Count, 1, m_name, true);

        string filename = args[0].AsString();

        string fileContents = Utils.GetFileText(filename);

        Variable result = RunScriptFunction.Execute(fileContents, filename);
        return result;
    }
}
public class RunScriptFunction : ParserFunction
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
        string includeScript = Utils.ConvertToScript(Scripting.InterpreterInstance, text, out char2Line);
        ParsingScript tempScript = new ParsingScript(Scripting.InterpreterInstance, includeScript, 0, char2Line);
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
