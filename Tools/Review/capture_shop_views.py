import contextlib,io,runpy,sys,json,time,pathlib,struct
small='--720' in sys.argv
folder=sys.argv[sys.argv.index('--folder')+1] if '--folder' in sys.argv else 'shop-resume'
root=pathlib.Path(__file__).resolve().parents[2];sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(root/'Tools/Review/unity_mcp.py'))
def code(source):
 r=ns['rpc']('tools/call',{'name':'execute_code','arguments':{'code':source,'skip_refresh':True}})['result'];d=json.loads('\n'.join(v.get('text','') for v in r.get('content',[])))
 if r.get('isError') or not d.get('success') or d.get('data',{}).get('logged_error_count',0):raise RuntimeError(d)
 return d.get('data',{}).get('result')
window='var window=UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>().First(w=>w.GetType().Name=="GameView");var view=new SerializedObject(window);'
original=int(code(window+'return view.FindProperty("m_SelectedSizes").GetArrayElementAtIndex(0).intValue;'))
custom_start=None
preset=3
if small:
 sizes='var sizes=UnityEngine.Resources.FindObjectsOfTypeAll<ScriptableObject>().First(s=>s.GetType().Name=="GameViewSizes");var data=new SerializedObject(sizes);var custom=data.FindProperty("m_Standalone.m_Custom");'
 custom_start=int(code(sizes+'return custom.arraySize;'))
 preset=int(code(sizes+'int i=custom.arraySize;custom.arraySize++;var item=custom.GetArrayElementAtIndex(i);item.FindPropertyRelative("m_Width").intValue=1280;item.FindPropertyRelative("m_Height").intValue=720;item.FindPropertyRelative("m_SizeType").enumValueIndex=1;item.FindPropertyRelative("m_BaseText").stringValue="Shop review 1280x720";data.ApplyModifiedPropertiesWithoutUndo();return 7+i;'))
code(window+'view.FindProperty("m_SelectedSizes").GetArrayElementAtIndex(0).intValue='+str(preset)+';view.ApplyModifiedPropertiesWithoutUndo();window.Focus();window.Repaint();return "review resolution";')
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import session;session.ensure_scene(code,'Game')
base='var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();clock.AutomaticTick=false;var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();var cooking=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var business=UnityEngine.Object.FindFirstObjectByType<BusinessView>();var hub=UnityEngine.Object.FindFirstObjectByType<HubView>();var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();var router=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();'
views=[('hub','save.StartNewGame();hub.ShowTab(0);'),('kitchen','campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;list.AddOrder(new Order("Goal1","マルゲリータ",0,120));list.AddOrder(new Order("Goal2","マルゲリータ",0,120));clock.Advance(2);business.ShowZone(ShopZone.Kitchen,true);'),('dispatch','var a=M7Validation.ReadyOrder("Goal1","マルゲリータ",clock.ElapsedSeconds,120);var b=M7Validation.ReadyOrder("Goal2","マルゲリータ",clock.ElapsedSeconds,120);list.AddOrder(a);list.AddOrder(b);handover.Bag(a,roster.People[0]);handover.Bag(b,roster.People[0]);business.ShowZone(ShopZone.Dispatch,true);UnityEngine.Object.FindFirstObjectByType<DeliveryPlanner>().ApplySuggestion();'),('settlement','UnityEngine.Object.FindFirstObjectByType<DeliveryPlanner>().Dispatch();clock.Advance(clock.BusinessDuration+1000);UnityEngine.Object.FindFirstObjectByType<SettlementView>().RevealAll();')]
try:
 for name,action in views:
  code(base+action+'router.Show(router.Current,true);Canvas.ForceUpdateCanvases();return "view";');time.sleep(1.1)
  if name=='dispatch':
   result=code(base+'var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlanner>();var time=business.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Time" && t.transform.parent.name.StartsWith("Lane_") && t.gameObject.activeInHierarchy);if(!time.text.Contains("秒"))throw new Exception("lane trip time not displayed: "+time.text);return time.text;');print(result,flush=True)
  path=(pathlib.Path('Docs/UI_Screens')/(folder+('-1280x720' if small else '')))/(name+'.png');(root/path).parent.mkdir(parents=True,exist_ok=True)
  if (root/path).exists():(root/path).unlink()
  code('ReviewFrameCapture.Begin('+json.dumps(str(path))+');return "capture";')
  for _ in range(40):
   time.sleep(.1)
   if (root/path).exists():break
  else:raise RuntimeError('screenshot missing '+name)
  dimensions=struct.unpack('>II',(root/path).read_bytes()[16:24]);assert dimensions==((1280,720) if small else (1920,1080)),dimensions
  print(str(path)+' '+str(dimensions),flush=True)
finally:
 code(base+'save.StartNewGame();return "normal save preserved";')
 code(window+'view.FindProperty("m_SelectedSizes").GetArrayElementAtIndex(0).intValue='+str(original)+';view.ApplyModifiedPropertiesWithoutUndo();window.Repaint();return "original GameView preset restored";')

 if custom_start is not None:
  code(sizes+'custom.DeleteArrayElementAtIndex('+str(custom_start)+');data.ApplyModifiedPropertiesWithoutUndo();return "temporary review size removed";')

session.finish(code)
