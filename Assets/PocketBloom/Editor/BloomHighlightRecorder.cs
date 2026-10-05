using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace PocketBloom.Editor
{
    // 점수/보드를 연출용으로 바꾸지 않고 실제 입력·게임 규칙을 녹화한다.
    // 촬영용 새 프로필은 종료 후 메모리와 원래 저장 파일 모두 복구한다.
    public static class BloomHighlightRecorder
    {
        const int Fps = 30;
        const string StatusPath = "Temp/PocketBloomMedia/capture-status.json";
        static BloomGame game;
        static RecorderController controller;
        static Touchscreen touch;
        static string originalProfile;
        static readonly Dictionary<string, byte[]> saveFiles = new Dictionary<string, byte[]>();
        static readonly List<object> moments = new List<object>();
        static int firstFrame;
        static bool active, oldRunInBackground;

        [MenuItem("Pocket Bloom/Media/Record Gameplay Showcase (Play Mode)")]
        public static void Begin()
        {
            if (!EditorApplication.isPlaying || active)
                throw new InvalidOperationException("Enter Play Mode with the PocketBloom scene; only one capture can run.");
            game = UnityEngine.Object.FindAnyObjectByType<BloomGame>();
            if (!game) throw new InvalidOperationException("PocketBloom game not found.");
            Directory.CreateDirectory("Recordings/PocketBloom");
            Directory.CreateDirectory("Docs/Media/Screenshots");
            Directory.CreateDirectory("Temp/PocketBloomMedia");
            originalProfile = JsonUtility.ToJson(game.Profile);
            saveFiles.Clear();
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
            {
                string path = BloomSave.SavePath + suffix;
                saveFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            oldRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            moments.Clear();
            active = true;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new BloomProfile { language = "ko", tutorialSeen = true }), game.Profile);
            BloomProjectBuilder.SetGameViewSize(1080, 1920);
            game.Home();
            touch = InputSystem.AddDevice<Touchscreen>("BloomShowcaseTouch");
            WriteStatus("preparing", null);
            game.StartCoroutine(GuardedCapture());
        }

        static IEnumerator GuardedCapture()
        {
            // 중첩된 코루틴의 예외도 복구 경로로 모은다.
            var stack = new Stack<IEnumerator>();
            stack.Push(Capture());
            while (stack.Count > 0 && active)
            {
                object yielded = null;
                try
                {
                    if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                    yielded = stack.Peek().Current;
                    if (yielded is IEnumerator nested) { stack.Push(nested); continue; }
                }
                catch (Exception error) { Finish(error); yield break; }
                yield return yielded;
            }
            if (active) Finish(null);
        }

        static IEnumerator Capture()
        {
            yield return Frames(20);
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "Pocket Bloom Gameplay";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1080, OutputHeight = 1920 };
            movie.CaptureAudio = true;
            movie.CaptureAlpha = false;
            movie.OutputFile = Path.GetFullPath("Recordings/PocketBloom/gameplay-raw");
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = Fps;
            settings.CapFrameRate = true;
            controller = new RecorderController(settings);
            controller.PrepareRecording();
            if (!controller.StartRecording()) throw new InvalidOperationException("Unity Recorder did not start.");
            firstFrame = Time.frameCount;
            Mark("home");
            yield return Frames(30); Shot("home"); yield return Frames(45);
            Click("Journey"); Mark("journey");
            yield return Frames(25); Shot("journey"); yield return Frames(35);
            game.StartRun("journey", 1); Mark("stage-1");
            yield return Frames(30);
            int moves = 0;
            while (!game.Run.finished && moves < 35)
            {
                yield return DragBestMove(moves < 3 ? 22 : 14);
                moves++;
                Mark("stage-1-move");
                if (moves == 5) Shot("gameplay");
                yield return Frames(moves < 3 ? 20 : 13);
            }
            if (!game.Run.won) throw new InvalidOperationException("Showcase did not complete stage 1.");
            Mark("stage-1-win");
            yield return Frames(35); Shot("victory"); yield return Frames(40);
            Click("Choice_0"); Mark("stage-2");
            yield return Frames(25);
            for (int i = 0; i < 4 && !game.Run.finished; i++)
            {
                yield return DragBestMove(17);
                Mark("stage-2-move");
                yield return Frames(17);
            }
            yield return Frames(25);
            game.Home(); Click("Collection"); Mark("collection");
            yield return Frames(25); Shot("collection"); yield return Frames(50);
            game.Home(); Click("Settings"); Mark("settings");
            yield return Frames(40);
            Click("Language"); Mark("english");
            yield return Frames(30); Shot("settings-en"); yield return Frames(30);
            game.Home(); Mark("end"); yield return Frames(65);
        }

        static IEnumerator DragBestMove(int dragFrames)
        {
            var move = BloomBoard.BestMove(game.Run);
            if (move == null) throw new InvalidOperationException("No legal move available.");
            int before = game.Run.moves;
            var piece = UnityEngine.Object.FindObjectsByType<BloomPieceInput>(FindObjectsSortMode.None)
                .Single(p => p.slot == move[0] && p.gameObject.activeInHierarchy).GetComponent<RectTransform>();
            var board = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .Single(r => r.name == "Board" && r.gameObject.activeInHierarchy);
            Vector2 origin = RectTransformUtility.WorldToScreenPoint(null, piece.TransformPoint(piece.rect.center));
            Vector2 target = RectTransformUtility.WorldToScreenPoint(null,
                board.TransformPoint(new Vector3(move[1] * 75 + 37.5f, -move[2] * 75 - 37.5f - 85, 0)));
            QueueTouch(UnityEngine.InputSystem.TouchPhase.Began, origin);
            yield return Frames(3);
            for (int i = 1; i <= dragFrames; i++)
            {
                float t = Mathf.SmoothStep(0, 1, i / (float)dragFrames);
                QueueTouch(UnityEngine.InputSystem.TouchPhase.Moved, Vector2.Lerp(origin, target, t));
                yield return null;
            }
            yield return Frames(5);
            QueueTouch(UnityEngine.InputSystem.TouchPhase.Ended, target);
            yield return Frames(12);
            if (game.Run.moves != before + 1) throw new InvalidOperationException("Touch placement was rejected.");
        }

        static void QueueTouch(UnityEngine.InputSystem.TouchPhase phase, Vector2 position) =>
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = phase, position = position,
                pressure = phase == UnityEngine.InputSystem.TouchPhase.Ended ? 0 : 1 });
        static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
        static void Click(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Single(b => b.name == name && b.gameObject.activeInHierarchy).onClick.Invoke();
        static void Shot(string name) => ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Media/Screenshots/" + name + ".png"));
        static void Mark(string label)
        {
            moments.Add(new { label, seconds = (Time.frameCount - firstFrame) / (float)Fps,
                screen = game.CurrentScreen, stage = game.Run?.stage, moves = game.Run?.moves,
                score = game.Run?.score, lines = game.Run?.lines, combo = game.Run?.combo });
            WriteStatus("recording", null);
        }
        static void WriteStatus(string status, string error) => File.WriteAllText(StatusPath,
            Newtonsoft.Json.JsonConvert.SerializeObject(new { status, error, fps = Fps, moments,
                scope = "Unity Editor gameplay recorded with production Input System touch events; no synthetic scores or board edits",
                utc = DateTime.UtcNow.ToString("O") }, Newtonsoft.Json.Formatting.Indented));
        static void Finish(Exception error)
        {
            active = false;
            try { controller?.StopRecording(); }
            finally
            {
                controller = null;
                if (touch != null) { InputSystem.RemoveDevice(touch); touch = null; }
                JsonUtility.FromJsonOverwrite(originalProfile, game.Profile);
                game.Home();
                Application.runInBackground = oldRunInBackground;
                RestoreSaveFiles();
                WriteStatus(error == null ? "completed" : "failed", error?.ToString());
            }
        }
        static void RestoreSaveFiles()
        {
            foreach (var entry in saveFiles)
                if (entry.Value == null) { if (File.Exists(entry.Key)) File.Delete(entry.Key); }
                else File.WriteAllBytes(entry.Key, entry.Value);
        }
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && active)
                Finish(new OperationCanceledException("Capture interrupted by leaving Play Mode."));
            if (state != PlayModeStateChange.EnteredEditMode) return;
            RestoreSaveFiles();
            saveFiles.Clear();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
