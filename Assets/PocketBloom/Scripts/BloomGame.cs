using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace PocketBloom
{
    public sealed class BloomGame : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Texture2D gardenArt;
        public BloomAds ads;
        public BloomProfile Profile { get; private set; }
        public BloomRun Run => Profile.active;
        public string CurrentScreen { get; private set; }
        RectTransform safe, root, page, overlay, toastRoot;
        BloomAudio audioFx;
        BloomRun undo;
        int selected = -1, stagePage;
        int[] suggested;
        float dragLift;
        bool busy;
        TMP_Text scoreText, detailText, toastText;
        BloomGraphic progress;
        readonly BloomGraphic[] tiles = new BloomGraphic[64];
        readonly RectTransform[] slots = new RectTransform[3];
        RectTransform board;
        float toastUntil;
        Rect lastSafe;
        Vector2 lastScreen;
        static readonly Color Ink = Hex("123F40"), Cream = Hex("FFF9E9"), Muted = Hex("6A9690"), Mint = Hex("BFEAD6"), Gold = Hex("F8D774"), Coral = Hex("F28F80");
        static readonly Color[] Petals = { Hex("F8D774"), Hex("EFA6B6"), Hex("B9ADF0"), Hex("8FD5C4"), Hex("F3AD76") };
        public string T(string ko, string en) => Profile.language == "ko" ? ko : en;
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }

        void Awake()
        {
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Profile = BloomSave.Load();
            audioFx = gameObject.AddComponent<BloomAudio>(); audioFx.Setup(); audioFx.Apply(Profile);
            if (!ads) ads = GetComponent<BloomAds>();
            var canvasGo = new GameObject("BloomCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Box((RectTransform)canvasGo.transform, "Backdrop", 0, 0, 720, 1280, Ink, 0);
            Stretch(backdrop.rectTransform);
            safe = Rect((RectTransform)canvasGo.transform, "SafeArea", 0, 0, 720, 1280);
            root = Rect(safe, "PortraitLayout", 0, 0, 720, 1280);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            if (!FindAnyObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Resize(); Home();
        }
        void Update()
        {
            Resize();
            if (toastRoot && Time.unscaledTime > toastUntil) ClearToast();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if (overlay) CloseOverlay(); else if (CurrentScreen == "game") Pause(); else Home(); }
        }
        void Resize()
        {
            if (lastSafe == Screen.safeArea && lastScreen == new Vector2(Screen.width, Screen.height)) return;
            lastSafe = Screen.safeArea; lastScreen = new Vector2(Screen.width, Screen.height);
            safe.anchorMin = new Vector2(lastSafe.x / Screen.width, lastSafe.y / Screen.height);
            safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            root.localScale = Vector3.one * Mathf.Min(lastSafe.width / 720f, lastSafe.height / 1280f);
        }
        void OnApplicationPause(bool paused) { if (paused) { Save(); if (CurrentScreen == "game" && !overlay && !busy) Pause(); } }
        void OnApplicationQuit() => Save();
        void Save() { if (!BloomSave.Write(Profile) && toastText) Toast(T("저장 공간을 확인해주세요", "Please check free storage")); }
        void ClearPage(string name)
        {
            ClearToast();
            CloseOverlay(); if (page) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Rect(root, name, 0, 0, 720, 1280); CurrentScreen = name; selected = -1;
            Box(page, "TopGlow", 26, 22, 668, 6, Mint, 3);
        }
        void Brand(string subtitle)
        {
            Label(page, "Brand", "POCKET / BLOOM", 40, 54, 490, 35, 22, Mint, FontStyles.Bold);
            Label(page, "Subheading", subtitle, 40, 96, 620, 38, 22, Muted);
        }
        public void Home()
        {
            ClearPage("home");
            Label(page, "Eyebrow", "A LITTLE SPACE TO GROW", 40, 72, 640, 30, 17, Mint, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(page, "Title", "Pocket\nBloom", 40, 108, 640, 216, 72, Cream, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(page, "Tagline", T("한 칸씩, 나만의 작은 정원", "One little piece. A garden of your own."), 40, 326, 640, 38, 26, Mint, 0, TextAlignmentOptions.Center);
            Art(page, 32, 396, 656, 332);
            Label(page, "Seeds", T("모은 씨앗  ", "SEEDS  ") + Profile.seeds.ToString("N0") + "    /    " + T("최고 기록  ", "BEST  ") + Profile.best.ToString("N0"), 50, 735, 620, 35, 21, Cream, 0, TextAlignmentOptions.Center);
            bool resume = Run != null && !Run.finished;
            Button(page, "Play", resume ? T("이어서 꽃 피우기", "Continue blooming") : T("정원 여행 시작", "Start your garden"), 44, 799, 632, 94, Gold,
                () => { if (resume) ShowGame(); else StartRun("journey", Profile.unlockedStage); }, 30);
            Button(page, "Journey", T("정원 여행", "JOURNEY") + "\n" + Profile.unlockedStage + " / 36", 44, 916, 198, 132, Hex("215457"), Journey, 23, Cream);
            Button(page, "Daily", T("오늘의 정원", "DAILY") + "\n" + DateTime.UtcNow.ToString("MM.dd"), 261, 916, 198, 132, Hex("215457"), () => OfferNew("daily", 1), 23, Cream);
            Button(page, "Endless", T("끝없는 정원", "ENDLESS") + "\n" + T("나의 최고 기록", "Beat your best"), 478, 916, 198, 132, Hex("215457"), () => OfferNew("endless", 1), 23, Cream);
            Button(page, "Collection", T("나의 정원", "My garden"), 44, 1084, 302, 76, Hex("1A4B4D"), Collection, 24, Mint);
            Button(page, "Settings", T("설정", "Settings"), 374, 1084, 302, 76, Hex("1A4B4D"), Settings, 24, Mint);
            Label(page, "Footer", T("서두르지 않아도 괜찮아요. 오늘도 한 칸.", "No rush. Just one lovely little piece."), 40, 1210, 640, 32, 18, Muted, 0, TextAlignmentOptions.Center);
        }
        void OfferNew(string mode, int stage)
        {
            if (Run != null && !Run.finished)
                Dialog(T("새 정원을 시작할까요?", "Start a new garden?"), T("진행 중인 한 판은 새 게임으로 바뀝니다.\n씨앗과 여행 기록은 유지됩니다.", "Your current run will be replaced.\nYour seeds and journey progress are kept."),
                    new Choice(T("새로 시작", "Start new"), () => StartRun(mode, stage)), new Choice(T("돌아가기", "Go back"), CloseOverlay));
            else StartRun(mode, stage);
        }
        public void StartRun(string mode, int stage)
        {
            uint seed = mode == "daily" ? (uint)int.Parse(DateTime.UtcNow.ToString("yyyyMMdd")) : mode == "journey" ? BloomBoard.CampaignSeed(stage) : (uint)DateTime.UtcNow.Ticks;
            Profile.active = BloomBoard.New(mode, stage, seed, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            undo = null; Save(); ShowGame();
            if (!Profile.tutorialSeen) Tutorial();
        }
        void Tutorial()
        {
            Dialog(T("작은 정원을 피우는 법", "Make room for something lovely"),
                T("1  아래 꽃 조각을 정원으로 끌어놓으세요.\n\n2  가로나 세로 한 줄을 채우면 꽃이 피어요.\n\n3  연속으로 줄을 채우면 콤보 점수!\n\n조각 선택 → 빈 칸 터치로도 놓을 수 있어요.", "1  Drag a flower piece onto the garden.\n\n2  Fill a row or column to make it bloom.\n\n3  Clear on consecutive turns for a combo!\n\nYou can also tap a piece, then an empty cell."),
                new Choice(T("꽃 피우러 가기", "Let's bloom"), () => { Profile.tutorialSeen = true; Save(); CloseOverlay(); }));
        }
        public void ShowGame()
        {
            if (Run == null) { Home(); return; }
            ClearPage("game");
            Brand(Run.mode == "journey" ? T("정원 여행  /  ", "JOURNEY  /  ") + Run.stage.ToString("00") : Run.mode == "daily" ? T("오늘의 정원  /  ", "DAILY  /  ") + Run.day : T("끝없는 정원", "ENDLESS GARDEN"));
            Button(page, "Pause", "II", 608, 50, 72, 72, Hex("24595B"), Pause, 26, Cream);
            var panel = Box(page, "ScoreCard", 40, 155, 640, 126, Cream, 25).rectTransform;
            Label(panel, "ScoreCaption", T("피워낸 점수", "BLOOM SCORE"), 24, 16, 400, 30, 19, Hex("64827B"));
            scoreText = Label(panel, "Score", "", 22, 42, 400, 78, 49, Ink, FontStyles.Bold);
            Label(panel, "Target", Run.target > 0 ? T("목표", "GOAL") + "\n" + Run.target : T("최고", "BEST") + "\n" + Profile.best, 427, 22, 185, 86, 26, Ink, FontStyles.Bold, TextAlignmentOptions.Right);
            Box(page, "ProgressTrack", 44, 299, 632, 10, Hex("28585A"), 5);
            progress = Box(page, "Progress", 44, 299, 1, 10, Mint, 5);
            detailText = Label(page, "Detail", "", 48, 323, 624, 34, 21, Mint);
            Box(page, "BoardShadow", 51, 376, 618, 618, Hex("0C3033"), 27);
            Box(page, "BoardFrame", 48, 367, 624, 624, Hex("376766"), 27);
            board = Rect(page, "Board", 60, 379, 600, 600);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                int index = y * 8 + x, px = x, py = y;
                tiles[index] = Box(board, "Cell_" + index, x * 75 + 2, y * 75 + 2, 71, 71, Hex("2A5454"), 13);
                var button = tiles[index].gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = tiles[index];
                tiles[index].raycastTarget = true; button.onClick.AddListener(() => PlaceSelected(px, py));
            }
            Label(page, "TrayCaption", T("조각을 골라 빈 칸에 놓아주세요", "A little piece, a little possibility"), 40, 1004, 640, 30, 19, Muted, 0, TextAlignmentOptions.Center);
            for (int i = 0; i < 3; i++)
            {
                slots[i] = Rect(page, "Piece_" + i, 46 + i * 215, 1046, 198, 112);
                var hit = Box(slots[i], "TouchArea", 0, 0, 198, 112, Hex("1D4C4D"), 19);
                hit.raycastTarget = true;
                var input = hit.gameObject.AddComponent<BloomPieceInput>(); input.game = this; input.slot = i;
            }
            Button(page, "Undo", T("한 수 뒤로", "Undo"), 44, 1190, 198, 64, Hex("265557"), Undo, 22, Cream);
            Button(page, "Hint", T("살짝 힌트", "Hint"), 261, 1190, 198, 64, Hex("265557"), Hint, 22, Cream);
            Button(page, "Shuffle", T("새 조각", "New pieces"), 478, 1190, 198, 64, Hex("265557"), Shuffle, 22, Cream);
            RefreshBoard();
            if (Run.finished) Result();
        }
        // 고정 논리 해상도는 실제 안전 영역에 맞춰 균일하게 축소한다.
        void RefreshBoard()
        {
            if (CurrentScreen != "game") return;
            scoreText.text = Run.score.ToString("N0");
            detailText.text = T("피운 줄 ", "LINES ") + Run.lines + "    ·    " + (Run.moveLimit > 0 ? T("남은 수 ", "MOVES ") + Math.Max(0, Run.moveLimit - Run.moves) : T("콤보 ", "COMBO ") + Run.combo);
            progress.rectTransform.sizeDelta = new Vector2(632 * (Run.target > 0 ? Mathf.Clamp01(Run.score / (float)Run.target) : Mathf.Clamp01(Run.score / (float)Math.Max(1000, Profile.best))), 10);
            for (int i = 0; i < 64; i++)
            {
                var tile = tiles[i]; tile.color = Run.cells[i] > 0 ? Petals[Run.cells[i] - 1] : Hex("2A5454");
                for (int j = tile.transform.childCount - 1; j >= 0; j--) { var c = tile.transform.GetChild(j).gameObject; c.SetActive(false); Destroy(c); }
                if (Run.cells[i] > 0)
                {
                    var f = Box(tile.rectTransform, "Flower", 19, 18, 33, 33, Color.Lerp(Petals[Run.cells[i] - 1], Color.white, .35f), 0); f.flower = true;
                    if (Run.buds[i] > 0) Label(tile.rectTransform, "Bud", "+", 25, 22, 22, 28, 23, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                }
            }
            for (int s = 0; s < 3; s++) RenderPiece(s);
        }
        void RenderPiece(int slot)
        {
            var holder = slots[slot];
            for (int i = holder.childCount - 1; i >= 1; i--) { holder.GetChild(i).gameObject.SetActive(false); Destroy(holder.GetChild(i).gameObject); }
            holder.GetChild(0).GetComponent<BloomGraphic>().color = selected == slot ? Hex("447C75") : Hex("1D4C4D");
            int id = Run.hand[slot]; if (id < 0) return;
            var shape = BloomBoard.Shapes[id]; int maxX = 0, maxY = 0;
            for (int i = 0; i < shape.Length; i += 2) { maxX = Math.Max(maxX, shape[i]); maxY = Math.Max(maxY, shape[i + 1]); }
            float size = Math.Min(30, 94f / (maxY + 1)), ox = (198 - (maxX + 1) * size) / 2, oy = (112 - (maxY + 1) * size) / 2;
            for (int i = 0; i < shape.Length; i += 2)
                Box(holder, "Petal", ox + shape[i] * size, oy + shape[i + 1] * size, size - 2, size - 2, Petals[Run.colors[slot] - 1], 6);
        }
        public void SelectPiece(int slot)
        {
            if (busy || overlay || Run.finished || Run.hand[slot] < 0) return;
            selected = slot; suggested = null; for (int i = 0; i < 3; i++) RenderPiece(i); audioFx.Play(0);
        }
        public void BeginPieceDrag(int slot, Vector2 point)
        {
            dragLift = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed ? 85 : 0;
            suggested = null; DragPiece(slot, point, false);
        }
        public void DragPiece(int slot, Vector2 point, bool drop)
        {
            if (busy || overlay || Run.finished || selected != slot) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(board, point, null, out var local);
            // 손가락 위에 미리 보기를 띄워 배치 위치가 가려지지 않도록 한다.
            int x = Mathf.FloorToInt(local.x / 75), y = Mathf.FloorToInt((-local.y - dragLift) / 75);
            for (int i = 0; i < 64; i++) tiles[i].color = Run.cells[i] > 0 ? Petals[Run.cells[i] - 1] : Hex("2A5454");
            bool fits = BloomBoard.Fits(Run, Run.hand[slot], x, y);
            if (fits)
            {
                var shape = BloomBoard.Shapes[Run.hand[slot]];
                for (int i = 0; i < shape.Length; i += 2) tiles[(y + shape[i + 1]) * 8 + x + shape[i]].color = Color.Lerp(Petals[Run.colors[slot] - 1], Hex("2A5454"), .35f);
            }
            if (drop) { if (fits) PlaceSelected(x, y); else RefreshBoard(); }
        }
        public bool PlaceSelected(int x, int y)
        {
            if (selected < 0 || busy || overlay || Run.finished) return false;
            if (suggested != null && suggested[0] == selected)
            {
                var hintedShape = BloomBoard.Shapes[Run.hand[selected]];
                for (int i = 0; i < hintedShape.Length; i += 2)
                    if (x == suggested[1] + hintedShape[i] && y == suggested[2] + hintedShape[i + 1])
                    { x = suggested[1]; y = suggested[2]; break; }
            }
            var before = Run.Copy(); var move = BloomBoard.Place(Run, selected, x, y);
            if (move == null) { Toast(T("빈 칸에 맞춰 놓아주세요", "Find an empty space for this piece")); return false; }
            undo = before; selected = -1; suggested = null; Save(); audioFx.Play(move.lines > 0 ? 2 : 1);
#if UNITY_ANDROID || UNITY_IOS
            if (move.lines > 0 && Profile.haptics && Application.isMobilePlatform) Handheld.Vibrate();
#endif
            StartCoroutine(AnimateMove(move)); return true;
        }
        IEnumerator AnimateMove(BloomMove move)
        {
            busy = true;
            if (!Profile.reducedMotion)
            {
                for (float t = 0; t < .24f; t += Time.unscaledDeltaTime)
                {
                    float scale = 1 + Mathf.Sin(t / .24f * Mathf.PI) * .10f;
                    foreach (int i in move.planted) tiles[i].transform.localScale = Vector3.one * scale;
                    foreach (int i in move.cleared) { tiles[i].color = Cream; tiles[i].transform.localScale = Vector3.one * scale; }
                    yield return null;
                }
            }
            foreach (var tile in tiles) if (tile) tile.transform.localScale = Vector3.one;
            busy = false; RefreshBoard();
            if (move.lines > 0) Toast((Run.combo > 1 ? "COMBO " + Run.combo + "!   " : "BLOOM!   ") + "+" + move.gained);
            if (Run.finished) Result();
        }
        void Undo()
        {
            if (busy || Run.finished) return;
            if (undo == null || Run.undoLeft <= 0) { Toast(T("한 판에 한 번, 이전 수가 있을 때 사용해요", "One free undo per run, after a move")); return; }
            int left = Run.undoLeft - 1; Profile.active = undo; Run.undoLeft = left; undo = null; selected = -1; Save(); RefreshBoard();
        }
        void Hint()
        {
            if (busy || Run.finished) return;
            var hint = BloomBoard.BestMove(Run); if (hint == null) return;
            SelectPiece(hint[0]); suggested = hint; var shape = BloomBoard.Shapes[Run.hand[selected]];
            for (int i = 0; i < shape.Length; i += 2) tiles[(hint[2] + shape[i + 1]) * 8 + hint[1] + shape[i]].color = Cream;
            Toast(T("밝게 빛나는 칸을 눌러보세요", "Tap a glowing cell"));
        }
        void Shuffle()
        {
            if (busy || Run.finished) return;
            if (Run.shuffleLeft <= 0) { Toast(T("새 조각은 한 판에 한 번 사용할 수 있어요", "New pieces are available once per run")); return; }
            Run.shuffleLeft--; BloomBoard.Deal(Run); undo = null; selected = -1; Save(); RefreshBoard();
        }
        void Pause()
        {
            if (busy) return;
            Dialog(T("잠깐 쉬어가요", "Take a little breath"), T("정원은 여기서 기다릴게요.\n진행 상황은 자동으로 저장돼요.", "Your garden will wait right here.\nYour progress is saved automatically."),
                new Choice(T("계속하기", "Continue"), CloseOverlay), new Choice(T("놀이 방법", "How to play"), Tutorial), new Choice(T("홈으로", "Home"), Home));
        }
        void Result()
        {
            if (!Run.rewardClaimed)
            {
                int seeds = EarnedSeeds(); Profile.seeds += Math.Max(0, seeds - Run.awardedSeeds); Run.awardedSeeds = seeds;
                Profile.best = Math.Max(Profile.best, Run.score);
                if (!Run.countedRun) { Profile.completedRuns++; Run.countedRun = true; }
                if (Run.mode == "journey" && Run.won)
                {
                    Profile.unlockedStage = Math.Min(36, Math.Max(Profile.unlockedStage, Run.stage + 1));
                    int stars = Run.moves <= Run.moveLimit * .55f ? 3 : Run.moves <= Run.moveLimit * .8f ? 2 : 1;
                    Profile.stars[Run.stage - 1] = Math.Max(Profile.stars[Run.stage - 1], stars);
                }
                if (Run.mode == "daily")
                {
                    if (Profile.dailyBestDay != Run.day) { Profile.dailyBestDay = Run.day; Profile.dailyBest = 0; }
                    Profile.dailyBest = Math.Max(Profile.dailyBest, Run.score);
                    if (Run.won && Profile.lastDailyReward != Run.day) { Profile.seeds += 60; Profile.lastDailyReward = Run.day; }
                }
                Run.rewardClaimed = true; Save(); audioFx.Play(Run.won ? 3 : 1);
            }
            var options = new List<Choice>();
            bool final = Run.mode == "journey" && Run.stage == 36 && Run.won;
            options.Add(new Choice(final ? T("완성한 정원 보기", "See your garden") : Run.won && Run.mode == "journey" ? T("다음 정원", "Next garden") : T("한 번 더", "Bloom again"),
                () => { string mode = Run.mode; int stage = Math.Min(36, Run.stage + (Run.won ? 1 : 0)); CloseOverlay(); if (final) Collection(); else { if (ads) ads.TryInterstitial(Profile.completedRuns); StartRun(mode, stage); } }));
            if (!Run.won && Run.revives == 0 && ads && ads.RewardedReady)
                options.Add(new Choice(T("광고 보고 이어하기", "Watch ad · keep growing"), () =>
                {
                    var rewardedRun = Run;
                    ads.ShowRewarded(() => { BloomBoard.Revive(rewardedRun); undo = null; Save(); ShowGame(); },
                        () => Toast(T("광고를 사용할 수 없어요", "Ad unavailable")),
                        () => this && ReferenceEquals(Run, rewardedRun) && rewardedRun.finished && !rewardedRun.won && rewardedRun.revives == 0);
                }));
            options.Add(new Choice(T("홈으로", "Home"), Home));
            Dialog(final ? T("36개의 정원, 모두 피웠어요!", "All 36 gardens are in bloom!") : Run.won ? T("오늘도 예쁘게 피었어요", "A lovely little bloom") : T("다음 꽃을 위한 쉼표", "A little room to try again"),
                Run.score.ToString("N0") + T(" 점", " points") + "\n\n" + T("피운 줄 ", "Lines ") + Run.lines + "   ·   " + T("씨앗 +", "Seeds +") + EarnedSeeds() + "\n\n" +
                (Run.mode == "daily" ? T("매일 UTC 00:00에 새 정원이 열려요", "A new garden opens at 00:00 UTC") : T("서두르지 않아도 괜찮아요.", "Good things grow at their own pace.")), options.ToArray());
        }
        int EarnedSeeds() => Math.Max(3, Run.score / 50) + (Run.won ? 15 : 0);
        void Journey()
        {
            ClearPage("journey"); Brand(T("36개의 작은 정원", "36 LITTLE GARDENS"));
            Label(page, "Title", T("당신의 속도로\n피어나는 여행", "A journey at\nyour own pace"), 40, 164, 640, 142, 46, Cream, FontStyles.Bold);
            for (int i = 0; i < 12; i++)
            {
                int stage = stagePage * 12 + i + 1; bool unlocked = stage <= Profile.unlockedStage;
                string text = stage.ToString("00") + "\n" + (Profile.stars[stage - 1] > 0 ? new string('*', Profile.stars[stage - 1]) : unlocked ? T("꽃 피우기", "Bloom") : T("준비 중", "Locked"));
                Button(page, "Stage_" + stage, text, 44 + (i % 3) * 216, 355 + (i / 3) * 163, 200, 146, unlocked ? (stage == Profile.unlockedStage ? Gold : Hex("265C5B")) : Hex("1B494A"),
                    () => { if (unlocked) OfferNew("journey", stage); else Toast(T("앞 정원을 먼저 피워주세요", "Bloom the earlier garden first")); }, 25, stage == Profile.unlockedStage ? Ink : unlocked ? Cream : Muted);
            }
            Button(page, "Previous", "<", 44, 1050, 120, 68, Hex("285959"), () => { stagePage = Math.Max(0, stagePage - 1); Journey(); }, 30, Cream);
            Label(page, "Page", (stagePage + 1) + " / 3", 260, 1066, 200, 40, 25, Cream, 0, TextAlignmentOptions.Center);
            Button(page, "Next", ">", 556, 1050, 120, 68, Hex("285959"), () => { stagePage = Math.Min(2, stagePage + 1); Journey(); }, 30, Cream);
            Button(page, "Back", T("홈으로", "Back home"), 44, 1160, 632, 80, Mint, Home);
        }
        void Collection()
        {
            ClearPage("collection"); Brand(T("모으고, 피우고, 쉬어가요", "COLLECT. BLOOM. BREATHE."));
            Label(page, "Title", T("나만의 작은 정원", "Your little garden"), 40, 163, 640, 72, 44, Cream, FontStyles.Bold);
            Art(page, 32, 267, 656, 334);
            string[] names = Profile.language == "ko" ? new[] { "첫 번째 새싹", "햇살 데이지", "분홍빛 오후", "라벤더 산책", "고양이의 낮잠", "영원한 봄" } : new[] { "First sprout", "Sunny daisies", "Pink afternoon", "Lavender walk", "Catnap corner", "Forever spring" };
            Label(page, "GardenName", names[Profile.garden], 44, 630, 632, 56, 38, Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(page, "Balance", T("모은 씨앗 ", "SEEDS ") + Profile.seeds, 44, 700, 632, 45, 28, Mint, 0, TextAlignmentOptions.Center);
            for (int i = 0; i < 6; i++)
            {
                var f = Box(page, "GardenFlower_" + i, 109 + i * 84, 787, 62, 62, i <= Profile.garden ? Petals[i % 5] : Hex("38625E"), 0); f.flower = true;
            }
            int cost = (Profile.garden + 1) * 100;
            Button(page, "Grow", Profile.garden >= 5 ? T("모든 정원이 활짝 피었어요", "Your garden is in full bloom") : T("정원 가꾸기  ·  씨앗 ", "Grow garden  ·  ") + cost,
                44, 916, 632, 90, Gold, () =>
                {
                    if (Profile.garden >= 5) return;
                    if (Profile.seeds < cost) { Toast(T("정원 여행에서 씨앗을 모아주세요", "Collect seeds by playing")); return; }
                    Profile.seeds -= cost; Profile.garden++; Save(); audioFx.Play(3); Collection();
                }, 27);
            Button(page, "DailyGift", T("오늘의 씨앗 받기  +20", "Daily seed gift  +20"), 44, 1028, 632, 75, Hex("285959"), () =>
            {
                string day = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (Profile.lastGift == day) { Toast(T("오늘의 씨앗을 이미 받았어요", "Today's seeds are already yours")); return; }
                Profile.lastGift = day; Profile.seeds += 20; Save(); Collection();
            }, 24, Cream);
            Button(page, "Back", T("홈으로", "Back home"), 44, 1152, 632, 80, Mint, Home);
        }
        void Settings()
        {
            ClearPage("settings"); Brand(T("내 속도, 내 취향", "MAKE YOURSELF AT HOME"));
            Label(page, "Title", T("잠깐, 편하게", "A little comfort"), 40, 164, 640, 74, 47, Cream, FontStyles.Bold);
            Toggle(T("효과음", "Sound effects"), Profile.sound, 294, () => Profile.sound = !Profile.sound);
            Toggle(T("배경 음악", "Garden music"), Profile.music, 389, () => Profile.music = !Profile.music);
            Toggle(T("진동", "Haptics"), Profile.haptics, 484, () => Profile.haptics = !Profile.haptics);
            Toggle(T("움직임 줄이기", "Reduce motion"), Profile.reducedMotion, 579, () => Profile.reducedMotion = !Profile.reducedMotion);
            Button(page, "Language", T("언어  ·  한국어", "Language  ·  English"), 44, 688, 632, 80, Hex("285959"), () => { Profile.language = Profile.language == "ko" ? "en" : "ko"; Save(); Settings(); }, 26, Cream);
            Button(page, "Help", T("놀이 방법", "How to play"), 44, 785, 632, 74, Hex("285959"), Tutorial, 25, Cream);
            Button(page, "Privacy", T("개인정보와 광고", "Privacy & ads"), 44, 879, 632, 74, Hex("285959"), Privacy, 25, Cream);
            Label(page, "Version", "POCKET BLOOM  /  1.0.0\n" + T("인앱결제 없음 · 오프라인 플레이", "No in-app purchases · Play offline"), 44, 1000, 632, 74, 21, Muted, 0, TextAlignmentOptions.Center);
            Button(page, "Back", T("홈으로", "Back home"), 44, 1152, 632, 80, Mint, Home);
        }
        void Toggle(string label, bool on, float y, Action change) => Button(page, label, label + "    " + (on ? "ON" : "OFF"), 44, y, 632, 78, Hex("285959"), () => { change(); audioFx.Apply(Profile); Save(); Settings(); }, 26, on ? Mint : Muted);
        void Privacy()
        {
            var choices = new List<Choice> { new Choice(T("확인", "Got it"), CloseOverlay) };
            if (ads && ads.config && Uri.TryCreate(ads.config.privacyPolicyUrl, UriKind.Absolute, out var policy) && policy.Scheme == Uri.UriSchemeHttps)
                choices.Add(new Choice(T("개인정보처리방침", "Privacy policy"), () => Application.OpenURL(policy.AbsoluteUri)));
            Dialog(T("개인정보와 광고", "Privacy & ads"),
                T("게임 진행은 이 기기에 저장됩니다.\n계정 가입과 인앱결제는 없습니다.\n\n보상 광고는 선택한 경우에만 재생돼요.\n광고가 없어도 퍼즐을 즐길 수 있어요.\n\n", "Progress is stored on this device.\nNo account or in-app purchases.\n\nRewarded ads are always optional.\nYou can play without watching ads.\n\n") +
                (ads && ads.Configured ? T("판 사이에 전면 광고가 나올 수 있어요.", "Ads may appear between rounds.") : T("현재 버전에서는 광고가 제공되지 않아요.", "Ads are not provided in this version.")), choices.ToArray());
        }
        void Art(RectTransform parent, float x, float y, float w, float h)
        {
            var artFrame = Box(parent, "ArtBack", x, y, w, h, Hex("4F9D93"), 30);
            artFrame.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            if (gardenArt)
            {
                var r = Rect(artFrame.rectTransform, "GardenIllustration", 0, 0, w, h);
                var raw = r.gameObject.AddComponent<UnityEngine.UI.RawImage>(); raw.texture = gardenArt; raw.raycastTarget = false;
                float source = gardenArt.width / (float)gardenArt.height, dest = w / h;
                raw.uvRect = source > dest ? new Rect((1 - dest / source) / 2, 0, dest / source, 1) : new Rect(0, (1 - source / dest) / 2, 1, source / dest);
            }
        }
        sealed class Choice { public string label; public Action action; public Choice(string l, Action a) { label = l; action = a; } }
        void Dialog(string title, string message, params Choice[] choices)
        {
            CloseOverlay(); overlay = Rect(root, "Dialog", 0, 0, 720, 1280);
            var shade = Box(overlay, "Shade", 0, 0, 720, 1280, new Color(.02f, .10f, .11f, .88f), 0); shade.raycastTarget = true;
            float h = 510 + choices.Length * 88, top = (1280 - h) / 2;
            var card = Box(overlay, "Card", 40, top, 640, h, Cream, 34).rectTransform;
            var flower = Box(card, "Bloom", 284, 27, 72, 72, Coral, 0); flower.flower = true;
            Label(card, "Title", title, 25, 109, 590, 104, 34, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(card, "Message", message, 32, 217, 576, 260, 24, Hex("537570"), 0, TextAlignmentOptions.Center);
            for (int i = 0; i < choices.Length; i++)
            { var choice = choices[i]; Button(card, "Choice_" + i, choice.label, 30, 492 + i * 88, 580, 70, i == 0 ? Ink : Hex("E0ECDD"), choice.action, 26, i == 0 ? Cream : Ink); }
        }
        void CloseOverlay() { if (overlay) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); overlay = null; } }
        void Toast(string text)
        {
            if (!toastText)
            {
                toastRoot = Box(root, "Toast", 60, 910, 600, 74, Cream, 22).rectTransform;
                toastText = Label(toastRoot, "Message", "", 14, 8, 572, 58, 23, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            }
            toastText.transform.parent.SetAsLastSibling(); toastText.gameObject.SetActive(true); toastText.text = text; toastUntil = Time.unscaledTime + 2.3f;
        }
        void ClearToast()
        {
            if (toastRoot) { toastRoot.gameObject.SetActive(false); Destroy(toastRoot.gameObject); }
            toastRoot = null; toastText = null;
        }
        static RectTransform Rect(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static BloomGraphic Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 18)
        { var r = Rect(parent, name, x, y, w, h); var g = r.gameObject.AddComponent<BloomGraphic>(); g.color = color; g.radius = radius; g.raycastTarget = false; return g; }
        TMP_Text Label(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color, FontStyles style = 0, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var r = Rect(parent, name, x, y, w, h); var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis; return t;
        }
        void Button(RectTransform parent, string name, string text, float x, float y, float w, float h, Color color, Action action, float size = 27, Color? foreground = null)
        {
            var box = Box(parent, name, x, y, w, h, color, 22); box.raycastTarget = true;
            var b = box.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = box;
            var colors = b.colors; colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f); colors.pressedColor = new Color(.83f, .90f, .86f); b.colors = colors;
            b.onClick.AddListener(() => { audioFx.Play(0); action(); });
            Label(box.rectTransform, "Label", text, 12, 6, w - 24, h - 12, size, foreground ?? Ink, FontStyles.Bold, TextAlignmentOptions.Center);
        }
    }
}
