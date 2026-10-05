// Unity CLI eval_file: 실제 표시된 메시 중심, 텍스트 높이, 화면 경계 및 팝업을 검사한다.
var game = UnityEngine.Object.FindFirstObjectByType<PocketBloom.BloomGame>();
if (game == null || !UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var original = UnityEngine.JsonUtility.ToJson(game.Profile);
var results = new System.Collections.Generic.List<object>();
var path = System.IO.Path.GetFullPath("Docs/Validation/VisualLayout.json");
System.Action<object> write = result => {
    var temporary = path + ".tmp";
    System.IO.File.WriteAllText(temporary, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    if (System.IO.File.Exists(path)) System.IO.File.Replace(temporary, path, null); else System.IO.File.Move(temporary, path);
};
var sizes = new[] { new[] {540,960}, new[] {540,1170}, new[] {768,1024} };
var screens = new[] { "home", "Journey", "Collection", "Settings", "game", "Pause", "Tutorial", "Privacy", "Replace", "Victory", "FinalVictory", "DailyVictory" };
var bindings = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
System.Action<string> click = name => UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == name && b.gameObject.activeInHierarchy).onClick.Invoke();
System.Action<string> invoke = name => game.GetType().GetMethod(name, bindings).Invoke(game, null);
// 여행 1 승리 상태는 실제 퍼즐 규칙으로 만든다. 마지막 단계/일일 팝업은 별도 UI 상태 fixture다.
var wonRun = PocketBloom.BloomBoard.New("journey", 1, PocketBloom.BloomBoard.CampaignSeed(1), System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
for (int i=0; i<80 && !wonRun.finished; i++) {
    var move = PocketBloom.BloomBoard.BestMove(wonRun);
    if (move == null || PocketBloom.BloomBoard.Place(wonRun, move[0], move[1], move[2]) == null) throw new System.Exception("No legal victory path");
}
if (!wonRun.won) throw new System.Exception("Normal-rule journey victory required");
int index = 0; bool capture = false; double next = 0;
write(new {status="running"});
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (UnityEditor.EditorApplication.timeSinceStartup < next) return;
    next = UnityEditor.EditorApplication.timeSinceStartup + .5;
    try {
        if (index >= sizes.Length * 2 * screens.Length) {
            UnityEditor.EditorApplication.update -= tick;
            UnityEngine.JsonUtility.FromJsonOverwrite(original, game.Profile); PocketBloom.BloomSave.Write(game.Profile);
            PocketBloom.Editor.BloomProjectBuilder.SetGameViewSize(540,960); game.Home();
            write(new {status="completed", utc=System.DateTime.UtcNow.ToString("O"), scope="Editor visual layout; final/daily result cases are display-only fixtures", results}); return;
        }
        int resolution = index / (2*screens.Length), language=(index/screens.Length)%2;
        string screen=screens[index%screens.Length];
        if (!capture) {
            PocketBloom.Editor.BloomProjectBuilder.SetGameViewSize(sizes[resolution][0], sizes[resolution][1]);
            game.Profile.language=language==0 ? "ko" : "en"; game.Profile.tutorialSeen=true;
            game.Profile.reducedMotion=true; game.Profile.active=null; game.Home();
            if (new[] {"Journey","Collection","Settings"}.Contains(screen)) click(screen);
            else if (screen != "home") {
                game.StartRun("journey",1);
                if (new[] {"Victory","FinalVictory","DailyVictory"}.Contains(screen)) {
                    game.Profile.active = UnityEngine.JsonUtility.FromJson<PocketBloom.BloomRun>(UnityEngine.JsonUtility.ToJson(wonRun));
                    if (screen=="FinalVictory") game.Run.stage=36;
                    if (screen=="DailyVictory") game.Run.mode="daily";
                    game.ShowGame();
                } else if (screen=="Replace") game.GetType().GetMethod("OfferNew",bindings).Invoke(game,new object[]{"daily",1});
                else if (screen != "game") invoke(screen);
            }
            capture=true; return;
        }
        UnityEngine.Canvas.ForceUpdateCanvases();
        var errors = new System.Collections.Generic.List<string>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Where(t=>t.gameObject.activeInHierarchy)) {
            t.ForceMeshUpdate();
            if (t.isTextTruncated) errors.Add("truncated: "+t.name+": "+t.text);
            if (!string.IsNullOrEmpty(t.text) && t.GetPreferredValues(t.text,t.rectTransform.rect.width,float.PositiveInfinity).y>t.rectTransform.rect.height+1)
                errors.Add("text height: "+t.name+": "+t.text);
        }
        foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Where(b=>b.gameObject.activeInHierarchy)) {
            var corners=new UnityEngine.Vector3[4]; b.GetComponent<UnityEngine.RectTransform>().GetWorldCorners(corners);
            if(corners.Any(c=>c.x<-.5f||c.y<-.5f||c.x>UnityEngine.Screen.width+.5f||c.y>UnityEngine.Screen.height+.5f)) errors.Add("offscreen: "+b.name);
        }
        var centers=new System.Collections.Generic.List<object>();
        if(screen=="home") foreach(var image in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>().Where(i=>i.gameObject.activeInHierarchy && (i.name=="DailyArt"||i.name=="EndlessArt"))) {
            var mesh=image.canvasRenderer.GetMesh();
            var p=image.rectTransform.parent as UnityEngine.RectTransform;
            var center=p.InverseTransformPoint(image.rectTransform.TransformPoint(mesh.bounds.center));
            float delta=UnityEngine.Mathf.Abs(center.x-p.rect.center.x);
            if(delta>1)errors.Add("art center: "+image.name+": "+delta);
            centers.Add(new {image=image.name,offset=delta});
        }
        if(screen.EndsWith("Victory")) {
            var card=UnityEngine.Object.FindObjectsByType<UnityEngine.RectTransform>().Single(r=>r.name=="Card"&&r.gameObject.activeInHierarchy);
            var center=card.TransformPoint(card.rect.center);
            if(UnityEngine.Mathf.Abs(center.x-UnityEngine.Screen.width/2f)>1||UnityEngine.Mathf.Abs(center.y-UnityEngine.Screen.height/2f)>1)errors.Add("result card center");
            var score=card.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="ResultScore");
            if(score.text!=game.Run.score.ToString("N0"))errors.Add("result score does not match run");
            if(card.GetComponent<PocketBloom.BloomCelebration>().enabled)errors.Add("reduce motion did not stop animation");
        }
        results.Add(new {width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,language=game.Profile.language,screen,fixture=screen=="FinalVictory"||screen=="DailyVictory",errors,centers});
        index++;capture=false;
    } catch(System.Exception error) {
        UnityEditor.EditorApplication.update-=tick;
        UnityEngine.JsonUtility.FromJsonOverwrite(original,game.Profile);PocketBloom.BloomSave.Write(game.Profile);game.Home();
        write(new {status="failed",error=error.ToString(),results});
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Started visual layout sweep; poll Docs/Validation/VisualLayout.json";
