import contextlib,io,runpy,sys,json,time,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
sys.argv=['helper','get_editor_state']
with contextlib.redirect_stdout(io.StringIO()):ns=runpy.run_path(str(root/'Tools/Review/unity_mcp.py'))
def code(source):
 result=ns['rpc']('tools/call',{'name':'execute_code','arguments':{'code':source,'skip_refresh':True}})['result']
 data=json.loads('\n'.join(v.get('text','') for v in result.get('content',[])))
 if result.get('isError') or not data.get('success') or data.get('data',{}).get('logged_error_count',0):raise RuntimeError(data)
 return data.get('data',{}).get('result')
base='var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var cooking=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var business=UnityEngine.Object.FindFirstObjectByType<BusinessView>();var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();'
steps=[
('manual setup','save.StartNewGame();clock.AutomaticTick=false;var recruitment=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();var crew=UnityEngine.Object.FindFirstObjectByType<KitchenCrew>();if(!recruitment.Fire(crew.People[0]))throw new Exception("could not arrange manual kitchen");campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;cooking.ManualControlEnabled=true;list.AddOrder(new Order("Goal1","マルゲリータ",clock.ElapsedSeconds,120));'),
('ticket click','var ticket=business.GetComponentsInChildren<DragSource>(true).First(s=>s.name=="Ticket_1");ticket.Clicked();if(cooking.BenchJob(0)?.Stage!=PizzaStage.Ticket)throw new Exception("ticket did not reach bench");'),
('dough and sauce','business.ClickTrayFor(DragKind.Dough,Topping.Basil);float coverage=0;for(int y=0;y<16;y++)for(int x=0;x<16;x++)coverage=business.TraceSauce(0,new Vector2((x+.5f)/16,(y+.5f)/16));if(coverage<.7f || !business.CommitSauce(0))throw new Exception("sauce did not reach 70%");'),
('cheese topping oven','business.ClickTrayFor(DragKind.Cheese,Topping.Basil);business.ClickTrayFor(DragKind.Topping,Topping.Basil);business.ClickPizzaOn(0);if(cooking.OvenJob(0)?.Stage!=PizzaStage.Baking)throw new Exception("pizza did not enter oven");'),
('bake band takeout','clock.Advance(cooking.BakeSeconds);business.ClickOvenAt(0);if(!cooking.Shelf.Any())throw new Exception("pizza was not taken out");'),
('packing click','business.ShowZone(ShopZone.Handover,true);var shelf=business.GetComponentsInChildren<DragSource>(true).First(s=>s.name=="ShelfPizza_1");shelf.Clicked();if(!cooking.PackingTable.Any())throw new Exception("pizza did not enter box");'),
('close box','var close=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Close");close.onClick.Invoke();if(!handover.RackOrders.Any() || cooking.PackedByPlayer!=1)throw new Exception("box did not close");'),
('bag box','var box=business.GetComponentsInChildren<DragSource>(true).First(s=>s.name=="Box_1");box.Clicked();if(handover.BagOf(roster.People[0]).Count!=1)throw new Exception("box did not enter bag");'),
('courier click','var courier=business.GetComponentsInChildren<DragSource>(true).First(s=>s.name=="Courier_Aoi");courier.Clicked();if(business.Zone!=ShopZone.Dispatch)throw new Exception("courier did not open board");'),
('suggestion and departure','var suggest=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Suggest");suggest.onClick.Invoke();var depart=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Depart");depart.onClick.Invoke();if(roster.People[0].CurrentTrip==null)throw new Exception("courier did not depart");'),
('delivery return settlement','clock.Advance(clock.BusinessDuration+1000);if(clock.Phase!=DayPhase.Ended || UnityEngine.Object.FindFirstObjectByType<DayStats>().DeliveredCount!=1)throw new Exception("delivery or settlement did not complete");')]
results=[]
try:
 for title,action in steps:
  value=code(base+action+'return "PASS: '+title+'";');results.append(value);print(value,flush=True);time.sleep(.4)
finally:
 code(base+'save.StartNewGame();clock.AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>().ReturnTitle();return "normal save protected, review finished";')
(root/'Docs/Handoff/Shop_Manual_UI.json').write_text(json.dumps(results,ensure_ascii=False,indent=2))
