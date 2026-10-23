using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Writes the last Test Runner result to Library/TestResults/last.txt, so a run started from a script or an
/// agent (Play Mode tests reload the domain) can be read afterwards. Registered again after every domain reload.</summary>
[InitializeOnLoad]
public sealed class TestResultFile : ICallbacks
{
    public static string PathName=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Library/TestResults/last.txt");
    static TestResultFile()=>ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new TestResultFile());

    /// <summary>Starts the EditMode tests (which include the Play Mode validations) as the Test Runner window would.</summary>
    [MenuItem("Tools/Pizza Delivery/Run Tests")]
    public static void RunAll()
    {
        if(File.Exists(PathName))File.Delete(PathName);
        ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,assemblyNames=new[]{"PizzaDelivery.Tests"}}));
    }
    public void RunStarted(ITestAdaptor tests){}
    public void TestStarted(ITestAdaptor test){}
    public void TestFinished(ITestResultAdaptor result){}
    public void RunFinished(ITestResultAdaptor result)
    {
        var text=new StringBuilder($"{result.ResultState} passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount} seconds={result.Duration:F1}\n");
        void Walk(ITestResultAdaptor r)
        {
            if(!r.HasChildren)
            {
                text.Append($"{r.ResultState,-12} {r.Test.FullName}{(string.IsNullOrEmpty(r.Message)?"":" — "+r.Message.Trim().Replace('\n',' '))}\n");
                return;
            }
            foreach(var c in r.Children)Walk(c);
        }
        Walk(result);
        Directory.CreateDirectory(Path.GetDirectoryName(PathName));File.WriteAllText(PathName,text.ToString());
    }
}
