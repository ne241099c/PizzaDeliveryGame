using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class DaySimulationWindow : EditorWindow
{
    private DaySimulationSession session;
    private string status="Run in Play Mode. Each promise starts from the initial campaign. Hiring/firing is disabled during autoplay.";
    private string outputPath;
    private static string DefaultPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs/M5_Simulations");
    [MenuItem("Tools/Pizza Delivery/Simulate Days")]
    public static void Open() => GetWindow<DaySimulationWindow>("Simulate Days");
    private void OnGUI()
    {
        EditorGUILayout.HelpBox(status,MessageType.Info);
        var settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        if(settings==null){EditorGUILayout.LabelField("Open SampleScene with DaySimulationSettings.");return;}
        EditorGUI.BeginDisabledGroup(session!=null);
        var so=new SerializedObject(settings);so.Update();var property=so.GetIterator();bool children=true;
        while(property.NextVisible(children)) {children=false;if(property.name!="m_Script")EditorGUILayout.PropertyField(property,true);}
        so.ApplyModifiedProperties();
        if(GUILayout.Button("Run comparison") && Application.isPlaying)Begin(settings);
        EditorGUI.EndDisabledGroup();
        if(session!=null && GUILayout.Button("Stop"))Stop();
        if(!string.IsNullOrEmpty(outputPath) && GUILayout.Button("Show CSV"))EditorUtility.RevealInFinder(outputPath);
        EditorGUILayout.HelpBox("Uses real order generation, suggested dispatch, accidents, scoring and settlement. Bankruptcy ends that policy early. Parameters here are also on GameManager in the Inspector.",MessageType.None);
    }
    private void Begin(DaySimulationSettings settings)
    {
        try
        {
            session=new DaySimulationSession(settings);session.DayCompleted+=r=>UnityEngine.Debug.Log("M5 simulation: "+r.Csv);
            status=session.Progress;EditorApplication.update+=Tick;
        }
        catch(Exception e){status=e.Message;UnityEngine.Debug.LogException(e);Stop();}
    }
    private void Tick()
    {
        if(session==null)return;
        try
        {
            if(!Application.isPlaying){Stop();return;}
            var watch=Stopwatch.StartNew();
            do {session.Step();} while(!session.Finished && watch.Elapsed.TotalMilliseconds<12);
            status=session.Progress;Repaint();
            if(!session.Finished)return;
            Directory.CreateDirectory(DefaultPath);outputPath=Path.Combine(DefaultPath,"comparison-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".csv");
            File.WriteAllText(outputPath,session.ToCsv());UnityEngine.Debug.Log("M5 CSV saved: "+outputPath);Stop();
        }
        catch(Exception e){status="Failed: "+e.Message;UnityEngine.Debug.LogException(e);Stop();}
    }
    private void Stop() { EditorApplication.update-=Tick;session?.Dispose();session=null;Repaint(); }
    private void OnDisable() => Stop();
}
