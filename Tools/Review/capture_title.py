"""Title screen screenshots (D-5a) at 1920x1080, 1280x720 and 1920x1200 (16:10) into Docs/UI_Screens/<folder>/.
States: a save ("save"), first launch ("first"), an unreadable save ("broken") and the credits. Each shot also checks the
layout (no overflow or overlapping buttons). Press Play first (the Title scene is loaded if needed). Uses temporary save files only (the player's save is
not read or written) and restores the GameView size afterwards.

usage: python3 Tools/Review/capture_title.py [--folder after-redesign]
"""
import contextlib,io,runpy,sys,json,time,pathlib,struct
folder=sys.argv[sys.argv.index('--folder')+1] if '--folder' in sys.argv else 'after-redesign'
root=pathlib.Path(__file__).resolve().parents[2];sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(root/'Tools/Review/unity_mcp.py'))
def code(source):
 r=ns['rpc']('tools/call',{'name':'execute_code','arguments':{'code':source,'skip_refresh':True}})['result'];d=json.loads('\n'.join(v.get('text','') for v in r.get('content',[])))
 if r.get('isError') or not d.get('success') or d.get('data',{}).get('logged_error_count',0):raise RuntimeError(d)
 return d.get('data',{}).get('result')
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent));import session;session.ensure_scene(code,'Title')
window='var window=UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>().First(w=>w.GetType().Name=="GameView");var view=new SerializedObject(window);'
sizes='var sizes=UnityEngine.Resources.FindObjectsOfTypeAll<ScriptableObject>().First(s=>s.GetType().Name=="GameViewSizes");var data=new SerializedObject(sizes);var custom=data.FindProperty("m_Standalone.m_Custom");'
original=int(code(window+'return view.FindProperty("m_SelectedSizes").GetArrayElementAtIndex(0).intValue;'))
start=int(code(sizes+'return custom.arraySize;'))
def add(w,h):
 return int(code(sizes+f'int i=custom.arraySize;custom.arraySize++;var item=custom.GetArrayElementAtIndex(i);item.FindPropertyRelative("m_Width").intValue={w};item.FindPropertyRelative("m_Height").intValue={h};item.FindPropertyRelative("m_SizeType").enumValueIndex=1;item.FindPropertyRelative("m_BaseText").stringValue="Title review {w}x{h}";data.ApplyModifiedPropertiesWithoutUndo();return 7+i;'))
resolutions=[(1920,1080,''),(1280,720,'-1280x720'),(1920,1200,'-1920x1200')]
presets={(w,h):add(w,h) for w,h,_ in resolutions}
states=[('title','"save"',False),('title-first','"first"',False),('title-broken','"broken"',False),('title-credits','"save"',True)]
try:
 for w,h,suffix in resolutions:
  code(window+f'view.FindProperty("m_SelectedSizes").GetArrayElementAtIndex(0).intValue={presets[(w,h)]};view.ApplyModifiedPropertiesWithoutUndo();window.Focus();window.Repaint();return "size";');time.sleep(.8)
  for name,kind,credits in states:
   issues=code(f'return TitleValidation.Stage({kind},{"true" if credits else "false"});')
   if issues:raise RuntimeError(f'{w}x{h} {name}: {issues}')
   time.sleep(.6)
   path=pathlib.Path('Docs/UI_Screens')/(folder+suffix)/(name+'.png');(root/path).parent.mkdir(parents=True,exist_ok=True)
   if (root/path).exists():(root/path).unlink()
   code('ReviewFrameCapture.Begin('+json.dumps(str(path))+');return "capture";')
   for _ in range(40):
    time.sleep(.1)
    if (root/path).exists():break
   else:raise RuntimeError('screenshot missing '+name)
   time.sleep(.2);dims=struct.unpack('>II',(root/path).read_bytes()[16:24]);assert dims==(w,h),dims
   print(path,dims,'layout ok',flush=True)
finally:
 code('TitleValidation.Restore();return "restored";')
 code(window+f'view.FindProperty("m_SelectedSizes").GetArrayElementAtIndex(0).intValue={original};view.ApplyModifiedPropertiesWithoutUndo();window.Repaint();return "preset";')
 for _ in resolutions:code(sizes+f'custom.DeleteArrayElementAtIndex({start});data.ApplyModifiedPropertiesWithoutUndo();return "removed";')
