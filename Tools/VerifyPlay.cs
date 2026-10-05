// unity command eval_file --file Tools/VerifyPlay.cs 로 실행한다. 저장은 종료 시 복원한다.
var game = UnityEngine.Object.FindFirstObjectByType<PocketBloom.BloomGame>();
if (game == null || !UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var original = UnityEngine.JsonUtility.ToJson(game.Profile);
var checks = new System.Collections.Generic.List<string>();
var path = "Docs/Validation/PlayFlow.json";
System.IO.Directory.CreateDirectory("Docs/Validation");
System.IO.File.WriteAllText(path, "{\"status\":\"running\"}");
int phase = 0, steps = 0, seeds = game.Profile.seeds;
double next = 0, start = UnityEditor.EditorApplication.timeSinceStartup;
System.Action<string> click = name => {
    var button = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == name && b.gameObject.activeInHierarchy);
    button.onClick.Invoke();
};
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (UnityEditor.EditorApplication.timeSinceStartup < next) return;
    next = UnityEditor.EditorApplication.timeSinceStartup + .5;
    try {
        if (UnityEditor.EditorApplication.timeSinceStartup - start > 90) throw new System.Exception("Verification timeout");
        if (phase == 0) { game.Profile.tutorialSeen = true; game.StartRun("journey", 1); phase++; return; }
        if (phase == 1) {
            if (!game.Run.finished) {
                var move = PocketBloom.BloomBoard.BestMove(game.Run);
                if (move == null) throw new System.Exception("No route");
                game.SelectPiece(move[0]);
                if (!game.PlaceSelected(move[1], move[2])) throw new System.Exception("Production placement rejected");
                steps++; return;
            }
            if (!game.Run.won || game.Profile.unlockedStage < 2 || game.Profile.seeds <= seeds) throw new System.Exception("Win/progression/reward failed");
            checks.Add("Journey 1 won through production placement and animation in " + steps + " moves");
            seeds = game.Profile.seeds; game.ShowGame(); phase++; return;
        }
        if (phase == 2) {
            if (game.Profile.seeds != seeds) throw new System.Exception("Result reward duplicated");
            checks.Add("Result revisit is idempotent");
            click("Choice_0"); phase++; return;
        }
        if (phase == 3) {
            if (game.Run.stage != 2 || game.Run.finished) throw new System.Exception("Next stage failed");
            checks.Add("Next stage starts through result button");
            var move = PocketBloom.BloomBoard.BestMove(game.Run); game.SelectPiece(move[0]); game.PlaceSelected(move[1], move[2]); phase++; return;
        }
        if (phase == 4) {
            click("Undo"); phase++; return;
        }
        if (phase == 5) {
            if (game.Run.moves != 0 || game.Run.undoLeft != 0) throw new System.Exception("Undo failed");
            click("Shuffle"); if (game.Run.shuffleLeft != 0) throw new System.Exception("Shuffle failed");
            checks.Add("Free undo and shuffle limits work");
            var loaded = PocketBloom.BloomSave.Load();
            if (UnityEngine.JsonUtility.ToJson(loaded.active) != UnityEngine.JsonUtility.ToJson(game.Run)) throw new System.Exception("Save roundtrip changed active run");
            checks.Add("Actual disk save and reload preserve active board, RNG and tools");
            game.Home(); phase++; return;
        }
        if (phase == 6) { click("Settings"); phase++; return; }
        if (phase == 7) { click("Language"); phase++; return; }
        if (phase == 8) {
            var clipped = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Where(t => t.isTextTruncated).Select(t => t.text).ToArray();
            if (clipped.Length > 0) throw new System.Exception("Settings text clipped: " + string.Join("|", clipped));
            checks.Add("Settings language toggle renders without clipped text");
            game.Home(); phase++; return;
        }
        if (phase == 9) { click("Collection"); phase++; return; }
        if (phase == 10) {
            var clipped = UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Where(t => t.isTextTruncated).Select(t => t.text).ToArray();
            if (clipped.Length > 0) throw new System.Exception("Collection text clipped: " + string.Join("|", clipped));
            checks.Add("Collection screen renders without clipped text");
            UnityEngine.JsonUtility.FromJsonOverwrite(original, game.Profile); PocketBloom.BloomSave.Write(game.Profile); game.Home();
            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new { status="passed", checks, utc=System.DateTime.UtcNow.ToString("O") }, Newtonsoft.Json.Formatting.Indented));
            UnityEditor.EditorApplication.update -= tick;
        }
    } catch (System.Exception e) {
        UnityEditor.EditorApplication.update -= tick;
        UnityEngine.JsonUtility.FromJsonOverwrite(original, game.Profile); PocketBloom.BloomSave.Write(game.Profile); game.Home();
        System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new { status="failed", error=e.ToString(), checks }, Newtonsoft.Json.Formatting.Indented));
    }
};
UnityEditor.EditorApplication.update += tick;
return "Production gameplay verification started; poll Docs/Validation/PlayFlow.json";
