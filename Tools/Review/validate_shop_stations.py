import contextlib,io,runpy,sys,json,time,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(root/'Tools/Review/unity_mcp.py'))
def code(source):
 result=ns['rpc']('tools/call',{'name':'execute_code','arguments':{'code':source,'skip_refresh':True}})['result']
 data=json.loads('\n'.join(v.get('text','') for v in result.get('content',[])))
 if result.get('isError') or not data.get('success') or data.get('data',{}).get('logged_error_count',0):raise RuntimeError(data)
 return data.get('data',{}).get('result')
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import session;session.ensure_scene(code,'Game')
base='var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();var cooking=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var business=UnityEngine.Object.FindFirstObjectByType<BusinessView>();var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();'
setup='save.StartNewGame();clock.AutomaticTick=false;var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();var funding=new SerializedObject(economy);float before=funding.FindProperty("initialCash").floatValue;try{funding.FindProperty("initialCash").floatValue=10000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();}finally{funding.FindProperty("initialCash").floatValue=before;funding.ApplyModifiedPropertiesWithoutUndo();}var facilities=UnityEngine.Object.FindFirstObjectByType<FacilityManager>();if(!facilities.Purchase(FacilityType.Workbench) || !facilities.Purchase(FacilityType.Oven))throw new Exception("station fixture funding");campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;'
probe='int benches=business.GetComponentsInChildren<TMPro.TMP_Text>(true).Count(t=>t.name.StartsWith("BenchTitle"));int ovens=business.GetComponentsInChildren<TMPro.TMP_Text>(true).Count(t=>t.name.StartsWith("OvenTitle"));int gauges=business.GetComponentsInChildren<RectTransform>(true).Count(t=>t.name.StartsWith("Gauge") && t.name.Length==6);if(benches!=cooking.BenchCount || ovens!=cooking.OvenCount || gauges!=cooking.OvenCount)throw new Exception($"stale station labels {benches}/{ovens}/{gauges}");return $"PASS: {benches} bench labels, {ovens} oven labels, {gauges} gauges";'
results=[]
try:
 print(code(base+setup+'return "upgraded";'),flush=True);time.sleep(.7)
 results.append(code(base+probe));print(results[-1],flush=True)
 code(base+'save.StartNewGame();campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;return "reset";');time.sleep(.7)
 results.append(code(base+probe));print(results[-1],flush=True)
finally:code(base+'save.StartNewGame();clock.AutomaticTick=false;return "normal save protected";')
(root/'Docs/Handoff/Shop_Station_UI.json').write_text(json.dumps(results,ensure_ascii=False,indent=2))

session.finish(code)
