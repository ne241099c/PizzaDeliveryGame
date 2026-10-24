"""Tutorial screenshots (S2-T) at the current GameView size into Docs/UI_Screens/tutorial/.
Starts each chapter the way the title does (GameScenes.LoadTutorial: a new game that never saves), captures every step,
checks the frames found their parts on screen, then does the step's action through the same API the UI uses.
Press Play first. Saving stays locked for the whole Play session.

usage: python3 Tools/Review/capture_tutorial.py [chapter ...]
"""
import contextlib,io,runpy,sys,json,time,pathlib
chapters=[int(a) for a in sys.argv[1:]] or [1,2,3]
root=pathlib.Path(__file__).resolve().parents[2];sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(root/'Tools/Review/unity_mcp.py'))
def code(source):
 for attempt in range(20):
  try:
   r=ns['rpc']('tools/call',{'name':'execute_code','arguments':{'code':source,'skip_refresh':True}})['result']
  except Exception:
   time.sleep(1);continue
  d=json.loads('\n'.join(v.get('text','') for v in r.get('content',[])))
  if d.get('code')=='DOMAIN_RELOAD_INTERRUPTED' or 'interrupted by Unity domain reload' in str(d.get('error','')):time.sleep(1);continue
  if r.get('isError') or not d.get('success'):raise RuntimeError(d)
  return d.get('data',{}).get('result')
 raise RuntimeError('Unity did not answer')
PRE='var d=TutorialDirector.Active;var cooking=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var orders=UnityEngine.Object.FindFirstObjectByType<OrderBook>();var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();'
def state():
 return json.loads(code(PRE+'if(d==null)return "{}";return "{\\"i\\":"+d.StepIndex+",\\"n\\":"+d.StepCount+",\\"t\\":"+d.TargetCount+",\\"done\\":"+(d.Finished?"true":"false")+"}";'))
def shot(name):
 path=pathlib.Path('Docs/UI_Screens/tutorial')/(name+'.png');(root/path).parent.mkdir(parents=True,exist_ok=True)
 if (root/path).exists():(root/path).unlink()
 code('ReviewFrameCapture.Begin('+json.dumps(str(path))+');return "capture";')
 for _ in range(60):
  time.sleep(.1)
  if (root/path).exists():return path
 raise RuntimeError('screenshot missing '+name)
def wait(cond,seconds=120):
 end=time.time()+seconds
 while time.time()<end:
  s=state()
  if cond(s):return s
  time.sleep(.3)
 raise RuntimeError('timed out at '+json.dumps(state()))
NEXT='d.Next();return "next";'
# Presses the red route line and drags it onto a nearby road that is not on the route (the map must not pan).
ROUTE_DRAG=('var bv=UnityEngine.Object.FindFirstObjectByType<BusinessView>();var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var cam=Camera.main;'
 'var p=roster.People.OrderByDescending(x=>x.Capacity).First();var path=RoadPathfinder.Suggest(map,handover.BagOf(p),p).EdgeIds.ToList();'
 'var pins=handover.BagOf(p).Select(o=>(Vector2)cam.WorldToScreenPoint(map.HomeAt(o.DestinationNodeId))).Append((Vector2)cam.WorldToScreenPoint(map.HomeAt(map.StoreNodeId))).ToList();'
 'Vector2 a=path.Select(id=>{var x=map.Edges.First(y=>y.Id==id);var q=RoadGeometry.Points(map,x);return (Vector2)cam.WorldToScreenPoint(q[q.Length/2]);}).OrderByDescending(q=>pins.Min(pp=>(pp-q).magnitude)).First();'
 'var nodes=path.SelectMany(id=>{var x=map.Edges.First(y=>y.Id==id);return new[]{x.NodeAId,x.NodeBId};}).ToHashSet();'
 'var other=map.Edges.Where(x=>!path.Contains(x.Id) && (nodes.Contains(x.NodeAId)||nodes.Contains(x.NodeBId))).OrderBy(x=>((Vector2)cam.WorldToScreenPoint(RoadGeometry.Points(map,x)[RoadGeometry.Points(map,x).Length/2])-a).sqrMagnitude).First();'
 'var op=RoadGeometry.Points(map,other);Vector2 b=cam.WorldToScreenPoint(op[op.Length/2]);var before=cam.transform.position;'
 'bv.PointerDown(a);string t1=bv.InputTrace;bv.PointerMove(a+new Vector2(12,0));bv.PointerMove(b);bv.PointerUp(b);'
 'return t1+" || "+bv.InputTrace+" || camera moved "+(cam.transform.position-before).magnitude.ToString("F3");')
ACTIONS={
 1:[NEXT,
    'var o=orders.Orders.First();return cooking.Claim(o,0).ToString();',
    'return cooking.AddDough(0).ToString();',
    'return cooking.AddSauce(0,1).ToString();',
    'return cooking.AddCheese(0).ToString();',
    'var o=orders.Orders.First();return cooking.AddTopping(0,o.RequiredTopping).ToString();',
    'return cooking.PutInOven(0,0).ToString();',
    ('poll','var o=orders.Orders.First();var j=cooking.JobFor(o);if(j!=null && j.Oven>=0 && cooking.OvenElapsed(j.Oven)>=(cooking.BandStartFor(o)+cooking.BandEndFor(o))/2)return cooking.TakeOut(j.Oven).ToString();return "wait";'),
    ('poll','var o=orders.Orders.First();var j=cooking.JobFor(o);if(j!=null && j.NeedsDecision)return cooking.ServeAsIs(j).ToString();return o.IsReady?"ready":"wait";'),
    NEXT],
 2:[NEXT,
    'var p=roster.People.OrderByDescending(x=>x.Capacity).First();return handover.Bag(orders.Orders.First(o=>o.IsReady && !handover.IsBagged(o)),p).ToString();',
    'var p=roster.People.OrderByDescending(x=>x.Capacity).First();return handover.Bag(orders.Orders.First(o=>o.IsReady && !handover.IsBagged(o)),p).ToString();',
    ('drag',ROUTE_DRAG),
    'var p=roster.People.OrderByDescending(x=>x.Capacity).First();var b=UnityEngine.Object.FindFirstObjectByType<BusinessView>().GetComponentsInChildren<StickerButton>(true).First(x=>x.name=="Depart_"+p.Name);b.Button.onClick.Invoke();return "depart";',
    ('wait',None),
    NEXT],
 3:[NEXT,('wait',None),NEXT,('wait',None),
    'var j=cooking.Jobs.First(x=>x.NeedsDecision);return cooking.Discard(j).ToString();',
    ('wait',None),
    NEXT],
}
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent));import session
session.ensure_scene(code,'Game')
report=[]
try:
 for ch in chapters:
  code(f'CampaignSaveManager.SessionSavingLocked=true;GameScenes.LoadTutorial({ch});return "load";')
  time.sleep(2)
  s=wait(lambda s:s.get('n',0)>0,60)
  for k,action in enumerate(ACTIONS[ch]):
   s=wait(lambda s,k=k:s['i']==k,240)
   time.sleep(.8);s=state()
   p=shot(f'ch{ch}-step{k+1}')
   report.append(f'ch{ch} step {k+1}/{s["n"]}: frames {s["t"]} -> {p}')
   if isinstance(action,tuple):
    kind,src=action
    if kind=='drag':
     report.append('   route drag: '+str(code(PRE+src)));time.sleep(.8);report.append('   after drag -> '+str(shot(f'ch{ch}-step{k+1}-dragged')));code(PRE+NEXT)
    if kind=='poll':
     for _ in range(400):
      if code(PRE+src)!='wait':break
      time.sleep(.25)
   else:
    code(PRE+action)
  s=wait(lambda s:s.get('done'),60);time.sleep(.6)
  report.append(f'ch{ch} finished -> {shot(f"ch{ch}-done")}')
finally:
 code('CampaignSaveManager.SessionSavingLocked=true;GameScenes.LoadGame(LaunchMode.Direct);return "back";')
 time.sleep(3)
 print('\n'.join(report))
