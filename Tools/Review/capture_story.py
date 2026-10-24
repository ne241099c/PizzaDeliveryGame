"""Opening, payoff choice, ending and the settlement mission celebration, captured into Docs/UI_Screens/story/.
Starts new games the way the title does with saving locked for the whole Play session (the real save is never written).
Press Play first.

usage: python3 Tools/Review/capture_story.py
"""
import contextlib,io,runpy,sys,json,time,pathlib
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
def shot(name):
 path=pathlib.Path('Docs/UI_Screens/story')/(name+'.png');(root/path).parent.mkdir(parents=True,exist_ok=True)
 if (root/path).exists():(root/path).unlink()
 code('ReviewFrameCapture.Begin('+json.dumps(str(path))+');return "capture";')
 for _ in range(60):
  time.sleep(.1)
  if (root/path).exists():return path
 raise RuntimeError('screenshot missing '+name)
def until(src,seconds=30):
 end=time.time()+seconds
 while time.time()<end:
  v=code(src)
  if v and v!='no':return v
  time.sleep(.3)
 raise RuntimeError('timed out: '+src)
CAMPAIGN='var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var r=campaign.Rewards;var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();'
def scenes(prefix):
 n=int(until('return Story.Current!=null?Story.Current.SceneCount.ToString():"no";'))
 for i in range(n):
  until(f'return Story.Current!=null && Story.Current.Index=={i}?"yes":"no";')
  time.sleep(3.2);report.append(f'{prefix} {i+1}/{n} -> {shot(f"{prefix}-{i+1}")}')
  code('Story.Current.Advance();Story.Current.Advance();return "next";')
 until('return Story.Current==null?"done":"no";')
report=[]
try:
 # ---- the settlement mission celebration (targets set to 0 on a short day, so the easy ones complete) ----
 code('CampaignSaveManager.SessionSavingLocked=true;GameScenes.NewGameMode=GameMode.Normal;GameScenes.LoadGame(LaunchMode.NewGame);return "load";')
 time.sleep(3)
 until(CAMPAIGN+'return campaign!=null && clock.Phase==DayPhase.BeforeOpening?"yes":"no";')
 r0=code(CAMPAIGN+'economy.TransferInPreparation(5000);foreach(var m in r.TodayMissions)m.target=0;UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1000);'
      'return string.Join(",",r.TodayMissions.Select(m=>m.id+":"+m.completed));')
 report.append('missions: '+str(r0))
 time.sleep(.4);report.append('missions start -> '+str(shot('missions-1')))
 time.sleep(1.4);report.append('missions stamped -> '+str(shot('missions-2')))
 time.sleep(1.6);report.append('missions done -> '+str(shot('missions-3')))
 time.sleep(1.5);report.append('receipt after -> '+str(shot('missions-4')))
 # ---- opening of a new game from the title ----
 code('CampaignSaveManager.SessionSavingLocked=true;GameScenes.NewGameMode=GameMode.Normal;GameScenes.PlayOpening=true;GameScenes.LoadGame(LaunchMode.NewGame);return "load";')
 time.sleep(2)
 scenes('opening')
 report.append('after opening -> '+str(shot('opening-after')))
 # ---- repay everything on the preparation screen, then choose the ending ----
 code(CAMPAIGN+'economy.TransferInPreparation(r.Loan+1000);UnityEngine.Object.FindFirstObjectByType<GameMenu>().ShowMissions();return "cash";')
 time.sleep(.6)
 code('var b=UnityEngine.Object.FindFirstObjectByType<HubView>().GetComponentsInChildren<StickerButton>(true).First(x=>x.name=="RepayAll");b.Button.onClick.Invoke();return "repaid";')
 time.sleep(.6);report.append('payoff choice -> '+str(shot('payoff-choice')))
 code('var b=UnityEngine.Object.FindObjectsByType<StickerButton>(FindObjectsSortMode.None).First(x=>x.name=="GoToEnding");b.Button.onClick.Invoke();return "ending";')
 scenes('ending')
 time.sleep(.6);report.append('result -> '+str(shot('ending-result')))
finally:
 code('CampaignSaveManager.SessionSavingLocked=true;GameScenes.LoadTitle();return "back";')
 time.sleep(2)
 print('\n'.join(report))
