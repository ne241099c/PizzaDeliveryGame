from pathlib import Path
import sys,runpy,contextlib,io,json,time,pathlib
folder=sys.argv[1]
sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(Path(__file__).with_name('unity_mcp.py')))
rpc=ns['rpc']
def call(name,args={}):
 r=rpc('tools/call',dict(name=name,arguments=args));out=json.loads('\n'.join(x.get('text','') for x in r['result']['content']))
 if r['result'].get('isError') or not out.get('success') or out.get('data',{}).get('logged_error_count',0):raise RuntimeError(out)
 return out
base='var window=UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>().First(v=>v.GetType().Name=="GameView");window.Focus();var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();clock.AutomaticTick=false;var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var menu=UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>();var hub=UnityEngine.Object.FindFirstObjectByType<HubScreenUI>();'
views=[('title','menu.ReturnTitle();'),('hub','save.StartNewGame();hub.ShowTab(0);'),('applicants','hub.ShowTab(1);'),('management','hub.ShowTab(2);'),('facilities','hub.ShowTab(3);'),('training','hub.ShowTab(0);var picker=UIVisibility.Find(canvas.transform,"StaffTab").GetComponentsInChildren<TrainingPickerUI>(true).First(v=>v.gameObject.activeInHierarchy);var b=(UnityEngine.UI.Button)new SerializedObject(picker).FindProperty("toggle").objectReferenceValue;b.onClick.Invoke();'),('business','campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;var orders=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();orders.AddOrder(new Order("Goal1","Baking pizza",0,60));var ready=M7Validation.ReadyOrder("Goal2","Ready pizza",0,60);orders.AddOrder(ready);orders.SelectOnly(ready);var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();roster.SelectForDispatch(roster.People[0]);UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>().ApplySuggestion();clock.Advance(10.8f);UnityEngine.Object.FindFirstObjectByType<BusinessScreenUI>().Refresh();'),('menu','menu.ShowMenu();'),('help','menu.ShowHelp();'),('settlement','menu.CloseHelp();menu.Resume();clock.AutomaticTick=false;clock.Advance(clock.BusinessDuration+1000);UnityEngine.Object.FindFirstObjectByType<SettlementScreenUI>().ShowAll();'),('gameover','UnityEngine.Object.FindFirstObjectByType<ScreenRouter>().Show(ScreenId.GameOver);')]
root=Path(__file__).resolve().parents[2]
for name,action in views:
 call('execute_code',dict(code=base+action+'var router=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();router.Show(router.Current,true);Canvas.ForceUpdateCanvases();return "view '+name+'";',skip_refresh=True));time.sleep(.5)
 path=pathlib.Path('Docs/UI_Screens')/folder/(name+'.png');(root/path).parent.mkdir(parents=True,exist_ok=True)
 if (root/path).exists():(root/path).unlink()
 call('execute_code',dict(code='ReviewFrameCapture.Begin('+json.dumps(str(path))+');return "capture at end of frame";',skip_refresh=True))
 for _ in range(30):
  time.sleep(.1)
  if (root/path).exists():break
 else:raise RuntimeError('screenshot missing '+str(path))
 print(path,flush=True)
call('execute_code',dict(code=base+'save.StartNewGame();menu.ReturnTitle();return "capture finished";',skip_refresh=True))
