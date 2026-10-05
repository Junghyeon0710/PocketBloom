var game=UnityEngine.Object.FindFirstObjectByType<PocketBloom.BloomGame>();
if(!UnityEditor.EditorApplication.isPlaying||game==null)throw new System.Exception("Play mode required");
var original=UnityEngine.JsonUtility.ToJson(game.Profile);
game.Profile.tutorialSeen=true;game.StartRun("journey",1);
var touch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("BloomTestTouch");
var records=new System.Collections.Generic.List<object>();
int step=0;double next=0;
UnityEngine.Vector2 origin=UnityEngine.Vector2.zero,target=UnityEngine.Vector2.zero;
System.IO.File.WriteAllText("Docs/Validation/TouchDrag.json","{\"status\":\"running\"}");
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 if(UnityEditor.EditorApplication.timeSinceStartup<next)return;
 next=UnityEditor.EditorApplication.timeSinceStartup+.35;
 try{
  if(step==0){
   var piece=UnityEngine.Object.FindObjectsByType<PocketBloom.BloomPieceInput>().Single(p=>p.slot==0).GetComponent<UnityEngine.RectTransform>();
   origin=UnityEngine.RectTransformUtility.WorldToScreenPoint(null,piece.TransformPoint(piece.rect.center));
   var board=UnityEngine.Object.FindObjectsByType<UnityEngine.RectTransform>().Single(r=>r.name=="Board");
   target=UnityEngine.RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new UnityEngine.Vector3(5*75+37.5f,-37.5f-85,0)));
  }
  if(step<4){
   var phase=step==0?UnityEngine.InputSystem.TouchPhase.Began:step==3?UnityEngine.InputSystem.TouchPhase.Ended:UnityEngine.InputSystem.TouchPhase.Moved;
   var point=step==0?origin:step==1?UnityEngine.Vector2.Lerp(origin,target,.5f):target;
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState {touchId=1,phase=phase,position=point,pressure=step==3?0:1});
   records.Add(new{step,phase=phase.ToString(),x=point.x,y=point.y,moves=game.Run.moves});step++;return;
  }
  bool passed=game.Run.moves==1&&game.Run.lines==1&&game.Run.score==95;
  System.IO.File.WriteAllText("Docs/Validation/TouchDrag.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status=passed?"passed":"failed",game.Run.moves,game.Run.lines,game.Run.score,records},Newtonsoft.Json.Formatting.Indented));
  UnityEditor.EditorApplication.update-=tick;UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
  UnityEngine.JsonUtility.FromJsonOverwrite(original,game.Profile);PocketBloom.BloomSave.Write(game.Profile);game.Home();
 }catch(System.Exception e){
  UnityEditor.EditorApplication.update-=tick;UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
  UnityEngine.JsonUtility.FromJsonOverwrite(original,game.Profile);PocketBloom.BloomSave.Write(game.Profile);game.Home();
  System.IO.File.WriteAllText("Docs/Validation/TouchDrag.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString(),records},Newtonsoft.Json.Formatting.Indented));
 }
};
UnityEditor.EditorApplication.update+=tick;
return "Touch input verification started";
