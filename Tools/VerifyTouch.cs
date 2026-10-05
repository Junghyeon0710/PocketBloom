// 실제 플레이 프레임에서 Input System 터치 이벤트를 전달한다.
var game=UnityEngine.Object.FindAnyObjectByType<PocketBloom.BloomGame>();
if(!UnityEditor.EditorApplication.isPlaying||game==null)throw new System.Exception("Play mode required");
var original=UnityEngine.JsonUtility.ToJson(game.Profile);
var oldBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
var oldEditorInput=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
game.Profile.tutorialSeen=true;game.StartRun("journey",1);
var touch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("BloomTestTouch");
var evidencePath=System.IO.Path.GetFullPath("Docs/Validation/TouchDrag.json");
System.Action<string> writeEvidence=json=>{
 var temp=evidencePath+".tmp";System.IO.File.WriteAllText(temp,json);
 if(System.IO.File.Exists(evidencePath))System.IO.File.Replace(temp,evidencePath,null);else System.IO.File.Move(temp,evidencePath);
};
writeEvidence("{\"status\":\"running\"}");
var records=new System.Collections.Generic.List<object>();
System.Collections.IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
System.Collections.IEnumerator Verify(){
 try{
  yield return Frames(10);
  var piece=UnityEngine.Object.FindObjectsByType<PocketBloom.BloomPieceInput>().Single(p=>p.slot==0).GetComponent<UnityEngine.RectTransform>();
  var board=UnityEngine.Object.FindObjectsByType<UnityEngine.RectTransform>().Single(r=>r.name=="Board"&&r.gameObject.activeInHierarchy);
  var origin=UnityEngine.RectTransformUtility.WorldToScreenPoint(null,piece.TransformPoint(piece.rect.center));
  var target=UnityEngine.RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new UnityEngine.Vector3(5*75+37.5f,-37.5f-85,0)));
  for(int step=0;step<4;step++){
   var phase=step==0?UnityEngine.InputSystem.TouchPhase.Began:step==3?UnityEngine.InputSystem.TouchPhase.Ended:UnityEngine.InputSystem.TouchPhase.Moved;
   var point=step==0?origin:step==1?UnityEngine.Vector2.Lerp(origin,target,.5f):target;
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState {touchId=1,phase=phase,position=point,pressure=step==3?0:1});
   UnityEngine.InputSystem.InputSystem.Update();
   yield return Frames(14);
   records.Add(new {step,phase=phase.ToString(),x=point.x,y=point.y,moves=game.Run.moves});
  }
  bool passed=game.Run.moves==1&&game.Run.lines==1&&game.Run.score==95;
  writeEvidence(Newtonsoft.Json.JsonConvert.SerializeObject(new {status=passed?"passed":"failed",game.Run.moves,game.Run.lines,game.Run.score,records,scope="Runtime frames; virtual Touchscreen; Input System; real UI dispatch",utc=System.DateTime.UtcNow.ToString("O")},Newtonsoft.Json.Formatting.Indented));
 }finally{
  UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
  UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=oldBackground;
  UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
  UnityEngine.JsonUtility.FromJsonOverwrite(original,game.Profile);PocketBloom.BloomSave.Write(game.Profile);game.Home();
 }
}
game.StartCoroutine(Verify());
return "Runtime-frame touch verification started";
