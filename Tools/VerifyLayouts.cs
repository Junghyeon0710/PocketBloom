var game = UnityEngine.Object.FindFirstObjectByType<PocketBloom.BloomGame>();
if (game == null || !UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var original = UnityEngine.JsonUtility.ToJson(game.Profile);
var results = new System.Collections.Generic.List<object>();
var path = "Docs/Validation/Layouts.json";
var sizes = new[] { new[] {540,960}, new[] {540,1170}, new[] {768,1024} };
var screens = new[] { "home", "Journey", "Collection", "Settings", "game" };
int index=0; bool capture=false; double next=0;
System.IO.File.WriteAllText(path,"{\"status\":\"running\"}");
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
    if(UnityEditor.EditorApplication.timeSinceStartup<next)return;
    next=UnityEditor.EditorApplication.timeSinceStartup+.5;
    try {
        if(index>=30){
            UnityEditor.EditorApplication.update-=tick;
            UnityEngine.JsonUtility.FromJsonOverwrite(original,game.Profile); PocketBloom.BloomSave.Write(game.Profile);
            PocketBloom.Editor.BloomProjectBuilder.SetGameViewSize(540,960);game.Home();
            System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new {status="completed",results},Newtonsoft.Json.Formatting.Indented));return;
        }
        int res=index/10, language=(index/5)%2, screen=index%5;
        if(!capture){
            PocketBloom.Editor.BloomProjectBuilder.SetGameViewSize(sizes[res][0],sizes[res][1]);
            game.Profile.language=language==0?"ko":"en";
            game.Home();
            if(screen==4)game.ShowGame();
            else if(screen>0)UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==screens[screen]&&b.gameObject.activeInHierarchy).onClick.Invoke();
            capture=true;return;
        }
        var clipped=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Where(t=>t.gameObject.activeInHierarchy&&t.isTextTruncated).Select(t=>t.name+": "+t.text).ToArray();
        var offscreen=new System.Collections.Generic.List<string>();
        foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>()){
            var corners=new UnityEngine.Vector3[4];b.GetComponent<UnityEngine.RectTransform>().GetWorldCorners(corners);
            if(corners.Any(c=>c.x<-.5f||c.y<-.5f||c.x>UnityEngine.Screen.width+.5f||c.y>UnityEngine.Screen.height+.5f))offscreen.Add(b.name);
        }
        results.Add(new {width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,language=game.Profile.language,screen=game.CurrentScreen,clipped,offscreen});
        if(res==0&&language==0)UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Docs/Validation/"+game.CurrentScreen+"-ko.png"));
        index++;capture=false;
    }catch(System.Exception e){
        UnityEditor.EditorApplication.update-=tick;UnityEngine.JsonUtility.FromJsonOverwrite(original,game.Profile);PocketBloom.BloomSave.Write(game.Profile);game.Home();
        System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new {status="failed",error=e.ToString(),results},Newtonsoft.Json.Formatting.Indented));
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Layout sweep started; poll Docs/Validation/Layouts.json";
