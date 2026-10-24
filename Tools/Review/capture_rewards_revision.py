"""Rewards revision screenshots (2026-10-24) at 1920x1080 into Docs/UI_Screens/rewards-revision/.
States: preparation (no hint row, no loan button), the ≡ menu with ミッション・ローン, the missions modal (labels only),
business (no loan button) and settlement (ローン返済・ミッション). Press Play first; saving stays locked for the session.

usage: python3 Tools/Review/capture_rewards_revision.py
"""
import contextlib,io,runpy,sys,json,time,pathlib
root=pathlib.Path(__file__).resolve().parents[2];sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(root/'Tools/Review/unity_mcp.py'))
def code(source):
 r=ns['rpc']('tools/call',{'name':'execute_code','arguments':{'code':source,'skip_refresh':True}})['result'];d=json.loads('\n'.join(v.get('text','') for v in r.get('content',[])))
 if r.get('isError') or not d.get('success') or d.get('data',{}).get('logged_error_count',0):raise RuntimeError(d)
 return d.get('data',{}).get('result')
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent));import session;session.ensure_scene(code,'Game')
F='T F<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>(UnityEngine.FindObjectsInactive.Include);'
def shot(name):
 time.sleep(.6)
 path=pathlib.Path('Docs/UI_Screens/rewards-revision')/(name+'.png');(root/path).parent.mkdir(parents=True,exist_ok=True)
 if (root/path).exists():(root/path).unlink()
 code('ReviewFrameCapture.Begin('+json.dumps(str(path))+');return "capture";')
 for _ in range(40):
  time.sleep(.1)
  if (root/path).exists():break
 else:raise RuntimeError('screenshot missing '+name)
 print(path)
def run(body):
 return code('using UnityEngine;public static class S{static '+F+'public static string Run(){'+body+'}}')
print(run('F<GameClock>().AutomaticTick=false;F<CampaignSaveManager>().StartNewGame();F<OrderSpawner>().enabled=false;F<ScreenRouter>().Show(ScreenId.Hub,true);return "hub";'))
shot('hub')
print(run('F<GameMenu>().ShowMenu();return "menu";'))
shot('menu')
print(run('F<GameMenu>().ShowMissions();var v=F<RewardsView>();return "missions open="+v.IsOpen;'))
shot('missions')
print(run('F<RewardsView>().Close();F<GameMenu>().Resume();F<CampaignManager>().StartBusiness();F<GameClock>().AutomaticTick=false;F<ScreenRouter>().Show(ScreenId.Business,true);return "business";'))
shot('business')
print(run('var c=F<GameClock>();c.Advance(c.BusinessDuration+1000);F<ScreenRouter>().Show(ScreenId.Settlement,true);return "settlement bankrupt="+F<CampaignManager>().IsBankrupt;'))
shot('settlement')
print(run('F<RewardsView>().Show();return "settlement rewards";'))
shot('settlement-rewards')
print(run('F<RewardsView>().Close();F<CampaignSaveManager>().StartNewGame();return "reset";'))
session.finish(code)
