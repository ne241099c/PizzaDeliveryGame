from pathlib import Path
import sys,json,runpy,contextlib,io,pathlib,time
name=sys.argv[1];args=json.loads(pathlib.Path(sys.argv[2][1:]).read_text()) if len(sys.argv)>2 and sys.argv[2].startswith('@') else json.loads(sys.argv[2]) if len(sys.argv)>2 else {}
sys.argv=['helper','get_editor_state']
for attempt in range(6):
 buffer=io.StringIO()
 try:
  with contextlib.redirect_stdout(buffer):ns=runpy.run_path(str(Path(__file__).with_name('unity_mcp.py')))
  break
 except SystemExit:
  if attempt==5:print(buffer.getvalue());raise
  time.sleep(2)
r=ns['rpc']('tools/call',dict(name=name,arguments=args));x=r.get('result',{});s='\n'.join(b.get('text','') for b in x.get('content',[]))
try:d=json.loads(s)
except ValueError:print(s);sys.exit(1)
print(json.dumps(d,ensure_ascii=False))
if x.get('isError') or not d.get('success') or d.get('data',{}).get('logged_error_count',0):sys.exit(1)
