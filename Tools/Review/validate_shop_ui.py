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
('bake band takeout','clock.Advance(cooking.BakeSeconds);business.ClickOvenAt(0);if(!cooking.PackingTable.Any())throw new Exception("automatic packing did not start");'),
('automatic packing','clock.Advance(cooking.PackSeconds);if(!handover.RackOrders.Any() || cooking.PackedByPlayer!=1)throw new Exception("automatic packing did not finish");'),
('open dispatch','business.ShowZone(ShopZone.Dispatch,true);'),
('courier name click','var pick=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Pick_Aoi");pick.onClick.Invoke();if(roster.SelectedPerson!=roster.People[0])throw new Exception("courier selection failed");'),
('load named order','var load=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Load_1");load.onClick.Invoke();if(handover.BagOf(roster.People[0]).Count!=1)throw new Exception("order did not enter selected bag");'),
('order countdown and no handover tab','var label=business.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Countdown" && t.transform.parent.name=="Order_1");if(!label.text.Contains("目安まで"))throw new Exception("countdown missing");var tab=business.GetComponentsInChildren<StickerButton>(true).First(b=>b.name=="Zone1");if(tab.GetComponent<CanvasGroup>().alpha!=0)throw new Exception("handover tab visible");'),
('map zoom button','var cam=Camera.main;float before=cam.orthographicSize;business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="ZoomIn").onClick.Invoke();if(cam.orthographicSize>=before)throw new Exception("zoom did not change view");'),
('road width remains readable','float px=Camera.main.orthographicSize*2/Camera.main.pixelHeight;foreach(var edge in UnityEngine.Object.FindObjectsByType<EdgeView>(FindObjectsSortMode.None)){if(edge.GetComponent<LineRenderer>().startWidth>UITheme.Active.mapRoadMaxPixels*px+.001f)throw new Exception("road exceeded pixel cap");}business.FitMap();'),
('road screen picking','var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var edge=map.GetEdge("Store-N1");var a=map.Nodes.First(n=>n.Id==edge.NodeAId).Position;var b=map.Nodes.First(n=>n.Id==edge.NodeBId).Position;business.Tap(Camera.main.WorldToScreenPoint((a+b)/2));var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();if(planner.Route.EdgeIds.Count!=1 || planner.Route.EdgeIds[0]!="Store-N1")throw new Exception("screen click did not select the intended road");business.UndoRoad();'),
('manual destination route','var visit=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Visit_1");visit.onClick.Invoke();var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();if(planner.SelectedPlan==null)throw new Exception("destination route not completed");business.UndoRoad();if(planner.SelectedPlan!=null)throw new Exception("undo did not remove return road");business.ReturnHome();if(planner.SelectedPlan==null)throw new Exception("return home did not restore route");'),
('manual route departure','var depart=business.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Depart");depart.onClick.Invoke();if(roster.People[0].CurrentTrip==null)throw new Exception("courier did not depart");'),
('in-flight countdown','var countdown=business.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Countdown" && t.transform.parent.name=="Order_1");var state=countdown.transform.parent.Find("State").GetComponent<TMPro.TMP_Text>();if(!state.text.Contains("到着まで"))throw new Exception("arrival countdown missing");'),
('delivery return settlement','clock.Advance(clock.BusinessDuration+1000);if(clock.Phase!=DayPhase.Ended || UnityEngine.Object.FindFirstObjectByType<DayStats>().DeliveredCount!=1)throw new Exception("delivery or settlement did not complete");')]
steps.extend([
('late order setup','save.StartNewGame();clock.AutomaticTick=false;campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;list.AddOrder(M7Validation.ReadyOrder("Goal1","遅延テスト",0,5));business.ShowZone(ShopZone.Dispatch,true);clock.Advance(6);'),
('grace countdown visible','var label=business.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Countdown" && t.transform.parent.name=="Order_1");if(!label.text.Contains("遅れ") || !label.text.Contains("受付あと"))throw new Exception("grace countdown missing");clock.Advance(19);'),
('cancel reason stays visible','var label=business.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Countdown" && t.transform.parent.name=="Order_1");if(!label.text.Contains("キャンセル"))throw new Exception("cancelled order disappeared without explanation");')])
results=[]
try:
 for title,action in steps:
  value=code(base+action+'return "PASS: '+title+'";');results.append(value);print(value,flush=True);time.sleep(.4)
finally:
 code(base+'save.StartNewGame();clock.AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>().ReturnTitle();return "normal save protected, review finished";')
(root/'Docs/Handoff/Shop_Manual_UI.json').write_text(json.dumps(results,ensure_ascii=False,indent=2))
