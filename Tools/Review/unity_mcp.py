import pathlib,tomllib,urllib.request,json,sys
import os
root=pathlib.Path(__file__).resolve().parents[2]
u=os.environ.get('PIZZA_MCP_URL')
if not u:
 cfg_path=root/'.codex/config.toml'
 if not cfg_path.exists():
  print('Use the native KitWright MCP tools, or configure PIZZA_MCP_URL locally. Never commit credentials.')
  sys.exit(1)
 cfg=tomllib.loads(cfg_path.read_text())
 u=cfg['mcp_servers']['kitwright']['url']
headers={'Content-Type':'application/json','Accept':'application/json, text/event-stream'}
def rpc(method,params=None,i=1):
 payload={'jsonrpc':'2.0','method':method}
 if i is not None: payload['id']=i
 if params is not None: payload['params']=params
 req=urllib.request.Request(u,data=json.dumps(payload).encode(),headers=headers)
 with urllib.request.urlopen(req,timeout=50) as r:
  sid=r.headers.get('Mcp-Session-Id')
  if sid: headers['Mcp-Session-Id']=sid
  raw=r.read().decode()
  if not raw: return {}
  if raw.lstrip().startswith('{'): return json.loads(raw)
  msgs=[json.loads(x[6:]) for x in raw.splitlines() if x.startswith('data: ')]
  return next((x for x in msgs if x.get('id')==i),msgs)
try:
 rpc('initialize',{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'codex-unity','version':'1.0'}})
 rpc('notifications/initialized',i=None)
 if len(sys.argv)==1:
  result=rpc('tools/list')
  # Do not persist connection metadata or credentials.
  print(json.dumps([{'name':x['name'],'inputSchema':x['inputSchema']} for x in result.get('result',{}).get('tools',[])],ensure_ascii=False))
 else:
  args=json.loads(pathlib.Path(sys.argv[2][1:]).read_text()) if len(sys.argv)>2 and sys.argv[2].startswith('@') else json.loads(sys.argv[2]) if len(sys.argv)>2 else {}
  print(json.dumps(rpc('tools/call',{'name':sys.argv[1],'arguments':args}),ensure_ascii=False))
except Exception as e:
 print('MCP connection failed:',type(e).__name__,'(connection details redacted)')
 sys.exit(1)
