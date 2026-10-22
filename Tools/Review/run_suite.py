from pathlib import Path
import sys,runpy,contextlib,io,json,time,re,pathlib
label=sys.argv[1] if len(sys.argv)>1 else 'suite'
sys.argv=['pizza_unity_mcp.py','get_editor_state']
for attempt in range(6):
 buffer=io.StringIO()
 try:
  with contextlib.redirect_stdout(buffer):ns=runpy.run_path(str(Path(__file__).with_name('unity_mcp.py')))
  break
 except SystemExit:
  if attempt==5:print(buffer.getvalue());raise
  time.sleep(2)
rpc=ns['rpc']
def call(name,args={}):
 out=rpc('tools/call',{'name':name,'arguments':args})
 result=out.get('result',{})
 blocks=result.get('content',[])
 s='\n'.join(b.get('text','') for b in blocks)
 try:parsed=json.loads(s)
 except ValueError:raise RuntimeError(s)
 if result.get('isError') or not parsed.get('success'):raise RuntimeError(s)
 return parsed

def code(s):
 x=call('execute_code',dict(code=s,skip_refresh=True));data=x.get('data',{})
 if data.get('logged_error_count',0):raise RuntimeError(x)
 return data.get('result',x.get('message',''))
results=[code('return M6Validation.Run()+" / "+ScreenValidation.Run()+" / "+M7Validation.Run()+" / "+VisualValidation.Run()+" / "+ShopValidation.Run();')];print(label,results[-1],flush=True)
code('UIThemeDriver.RenderingSuspended=true;return "Visual frame styling suspended only during accounting simulations";')
try:
 for cls,start in [('M5SimulationValidation','Begin'),('M6SimulationValidation','Begin'),('M6SimulationValidation','BeginComparisons'),('M7SimulationValidation','Begin')]:
  code(cls+'.'+start+'(); return '+cls+'.Status;')
  last=''
  for _ in range(400):
   time.sleep(2)
   status=code('return '+cls+'.Status;')
   if status!=last:print(cls,status,flush=True);last=status
   if status.startswith('FAIL'):raise RuntimeError(status)
   if status.startswith('PASS'):results.append(status);break
  else:raise RuntimeError('timeout '+cls)
finally:
 code('UIThemeDriver.RenderingSuspended=false;return "Visual frame styling restored";')
path=Path('/private/tmp/pizza_suite_'+label+'.json');path.write_text(json.dumps(results,ensure_ascii=False,indent=2));print('ALL PASS',label,flush=True)
