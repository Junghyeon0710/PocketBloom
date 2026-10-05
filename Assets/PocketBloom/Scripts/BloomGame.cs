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
    public sealed partial class BloomGame : MonoBehaviour
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
        static readonly Color Ink = Hex("1B4A3E"), Cream = Hex("FFF7E6"), Muted = Hex("66846C"), Mint = Hex("C4DFBC"), Gold = Hex("F6D57B"), Coral = Hex("F58F73");
        static readonly Color[] Petals = { Hex("F7D572"), Hex("F49587"), Hex("B69AE3"), Hex("9FD6AC"), Hex("F2BC70") };
        public string T(string ko, string en) => Profile.language == "ko" ? ko : en;
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }

        void Awake()
        {
            font = Resources.Load<TMP_FontAsset>("SunlitFont") ?? font;
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Profile = BloomSave.Load();
            audioFx = gameObject.AddComponent<BloomAudio>(); audioFx.Setup(); audioFx.Apply(Profile);
            if (!ads) ads = GetComponent<BloomAds>();
            var canvasGo = new GameObject("BloomCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            var backdrop = Box((RectTransform)canvasGo.transform, "Backdrop", 0, 0, 720, 1280, Cream, 0);
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
            Illustration(page, name == "home" ? "HomeBackdrop" : "PlayBackdrop", 0, 0, 720, 1280);
        }
        void Brand(string subtitle)
        {
            Label(page, "Brand", "POCKET / BLOOM", 40, 54, 490, 35, 22, Ink, FontStyles.Bold);
            Label(page, "Subheading", subtitle, 40, 96, 620, 38, 22, Muted);
        }
        public void Home()
        {
            ClearPage("home"); SunlitHome();
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
            ClearPage("game"); SunlitGame(); RefreshBoard();
            if (Run.finished) Result();
        }
        // 고정 논리 해상도는 실제 안전 영역에 맞춰 균일하게 축소한다.
        void RefreshBoard()
        {
            if (CurrentScreen != "game") return;
            scoreText.text = Run.score.ToString("N0");
            movesText.text = (Run.moveLimit > 0 ? Math.Max(0, Run.moveLimit - Run.moves) : Run.combo).ToString();
            detailText.text = T("피운 줄 ", "LINES ") + Run.lines + "    ·    " + T("콤보 ", "COMBO ") + Run.combo;
            progress.rectTransform.sizeDelta = new Vector2(568 * (Run.target > 0 ? Mathf.Clamp01(Run.score / (float)Run.target) : Mathf.Clamp01(Run.score / (float)Math.Max(1000, Profile.best))), 15);
            for (int i = 0; i < 64; i++)
            {
                var tile = tiles[i]; tile.inset = Run.cells[i] == 0; tile.SetVerticesDirty(); tile.color = Run.cells[i] > 0 ? Petals[Run.cells[i] - 1] : Soil;
                for (int j = tile.transform.childCount - 1; j >= 0; j--) { var c = tile.transform.GetChild(j).gameObject; c.SetActive(false); Destroy(c); }
                if (Run.cells[i] > 0)
                {
                    var f = Box(tile.rectTransform, "Flower", 19, 18, 33, 33, Color.Lerp(Petals[Run.cells[i] - 1], Color.white, .7f), 0); f.flower = true;
                    if (Run.buds[i] > 0) Label(tile.rectTransform, "Bud", "+", 25, 22, 22, 28, 23, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                }
            }
            for (int s = 0; s < 3; s++) RenderPiece(s);
        }
        void RenderPiece(int slot)
        {
            var holder = slots[slot];
            for (int i = holder.childCount - 1; i >= 1; i--) { holder.GetChild(i).gameObject.SetActive(false); Destroy(holder.GetChild(i).gameObject); }
            holder.GetChild(0).GetComponent<BloomGraphic>().color = selected == slot ? Hex("D7EDC7") : Cream;
            int id = Run.hand[slot]; if (id < 0) return;
            var shape = BloomBoard.Shapes[id]; int maxX = 0, maxY = 0;
            for (int i = 0; i < shape.Length; i += 2) { maxX = Math.Max(maxX, shape[i]); maxY = Math.Max(maxY, shape[i + 1]); }
            float size = Math.Min(47, 120f / (maxY + 1)), ox = (198 - (maxX + 1) * size) / 2, oy = (142 - (maxY + 1) * size) / 2;
            for (int i = 0; i < shape.Length; i += 2)
            {
                var petal = Box(holder, "Petal", ox + shape[i] * size, oy + shape[i + 1] * size, size - 3, size - 3, Petals[Run.colors[slot] - 1], 8);
                float fSize = size * .46f, offset = (size - 3 - fSize) / 2;
                var flower = Box(petal.rectTransform, "Flower", offset, offset, fSize, fSize, Color.Lerp(Petals[Run.colors[slot] - 1], Color.white, .7f), 0); flower.flower = true;
            }
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
            for (int i = 0; i < 64; i++) tiles[i].color = Run.cells[i] > 0 ? Petals[Run.cells[i] - 1] : Soil;
            bool fits = BloomBoard.Fits(Run, Run.hand[slot], x, y);
            if (fits)
            {
                var shape = BloomBoard.Shapes[Run.hand[slot]];
                for (int i = 0; i < shape.Length; i += 2) tiles[(y + shape[i + 1]) * 8 + x + shape[i]].color = Color.Lerp(Petals[Run.colors[slot] - 1], Soil, .35f);
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
            Label(page, "Title", T("당신의 속도로\n피어나는 여행", "A journey at\nyour own pace"), 40, 164, 640, 142, 46, Ink, FontStyles.Bold);
            for (int i = 0; i < 12; i++)
            {
                int stage = stagePage * 12 + i + 1; bool unlocked = stage <= Profile.unlockedStage;
                string text = stage.ToString("00") + "\n" + (Profile.stars[stage - 1] > 0 ? new string('*', Profile.stars[stage - 1]) : unlocked ? T("꽃 피우기", "Bloom") : T("준비 중", "Locked"));
                Button(page, "Stage_" + stage, text, 44 + (i % 3) * 216, 355 + (i / 3) * 163, 200, 146, unlocked ? (stage == Profile.unlockedStage ? Gold : Cream) : Hex("E5EBD0"),
                    () => { if (unlocked) OfferNew("journey", stage); else Toast(T("앞 정원을 먼저 피워주세요", "Bloom the earlier garden first")); }, 25, stage == Profile.unlockedStage ? Ink : unlocked ? Ink : Muted);
            }
            Button(page, "Previous", "<", 44, 1050, 120, 68, Cream, () => { stagePage = Math.Max(0, stagePage - 1); Journey(); }, 30, Ink);
            Label(page, "Page", (stagePage + 1) + " / 3", 260, 1066, 200, 40, 25, Ink, 0, TextAlignmentOptions.Center);
            Button(page, "Next", ">", 556, 1050, 120, 68, Cream, () => { stagePage = Math.Min(2, stagePage + 1); Journey(); }, 30, Ink);
            Button(page, "Back", T("홈으로", "Back home"), 44, 1160, 632, 80, Mint, Home);
        }
        void Collection()
        {
            ClearPage("collection"); Brand(T("모으고, 피우고, 쉬어가요", "COLLECT. BLOOM. BREATHE."));
            Label(page, "Title", T("나만의 작은 정원", "Your little garden"), 40, 163, 640, 72, 44, Ink, FontStyles.Bold);
            Art(page, 32, 267, 656, 334);
            string[] names = Profile.language == "ko" ? new[] { "첫 번째 새싹", "햇살 데이지", "분홍빛 오후", "라벤더 산책", "고양이의 낮잠", "영원한 봄" } : new[] { "First sprout", "Sunny daisies", "Pink afternoon", "Lavender walk", "Catnap corner", "Forever spring" };
            Label(page, "GardenName", names[Profile.garden], 44, 630, 632, 56, 38, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(page, "Balance", T("모은 씨앗 ", "SEEDS ") + Profile.seeds, 44, 700, 632, 45, 28, Ink, 0, TextAlignmentOptions.Center);
            for (int i = 0; i < 6; i++)
            {
                var f = Box(page, "GardenFlower_" + i, 109 + i * 84, 787, 62, 62, i <= Profile.garden ? Petals[i % 5] : Cream, 0); f.flower = true;
            }
            int cost = (Profile.garden + 1) * 100;
            Button(page, "Grow", Profile.garden >= 5 ? T("모든 정원이 활짝 피었어요", "Your garden is in full bloom") : T("정원 가꾸기  ·  씨앗 ", "Grow garden  ·  ") + cost,
                44, 916, 632, 90, Gold, () =>
                {
                    if (Profile.garden >= 5) return;
                    if (Profile.seeds < cost) { Toast(T("정원 여행에서 씨앗을 모아주세요", "Collect seeds by playing")); return; }
                    Profile.seeds -= cost; Profile.garden++; Save(); audioFx.Play(3); Collection();
                }, 27);
            Button(page, "DailyGift", T("오늘의 씨앗 받기  +20", "Daily seed gift  +20"), 44, 1028, 632, 75, Cream, () =>
            {
                string day = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (Profile.lastGift == day) { Toast(T("오늘의 씨앗을 이미 받았어요", "Today's seeds are already yours")); return; }
                Profile.lastGift = day; Profile.seeds += 20; Save(); Collection();
            }, 24, Ink);
            Button(page, "Back", T("홈으로", "Back home"), 44, 1152, 632, 80, Mint, Home);
        }
        void Settings()
        {
            ClearPage("settings"); Brand(T("내 속도, 내 취향", "MAKE YOURSELF AT HOME"));
            Label(page, "Title", T("잠깐, 편하게", "A little comfort"), 40, 164, 640, 74, 47, Ink, FontStyles.Bold);
            Toggle(T("효과음", "Sound effects"), Profile.sound, 294, () => Profile.sound = !Profile.sound);
            Toggle(T("배경 음악", "Garden music"), Profile.music, 389, () => Profile.music = !Profile.music);
            Toggle(T("진동", "Haptics"), Profile.haptics, 484, () => Profile.haptics = !Profile.haptics);
            Toggle(T("움직임 줄이기", "Reduce motion"), Profile.reducedMotion, 579, () => Profile.reducedMotion = !Profile.reducedMotion);
            Button(page, "Language", T("언어  ·  한국어", "Language  ·  English"), 44, 688, 632, 80, Cream, () => { Profile.language = Profile.language == "ko" ? "en" : "ko"; Save(); Settings(); }, 26, Ink);
            Button(page, "Help", T("놀이 방법", "How to play"), 44, 785, 632, 74, Cream, Tutorial, 25, Ink);
            Button(page, "Privacy", T("개인정보와 광고", "Privacy & ads"), 44, 879, 632, 74, Cream, Privacy, 25, Ink);
            Label(page, "Version", "POCKET BLOOM  /  SUNLIT GARDEN\n" + T("인앱결제 없음 · 오프라인 플레이", "No in-app purchases · Play offline"), 44, 1000, 632, 74, 21, Muted, 0, TextAlignmentOptions.Center);
            Button(page, "Back", T("홈으로", "Back home"), 44, 1152, 632, 80, Mint, Home);
        }
        void Toggle(string label, bool on, float y, Action change) => Button(page, label, label + "    " + (on ? "ON" : "OFF"), 44, y, 632, 78, Cream, () => { change(); audioFx.Apply(Profile); Save(); Settings(); }, 26, on ? Ink : Muted);
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
            var artFrame = Box(parent, "ArtBack", x, y, w, h, Cream, 30);
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
                toastRoot = Box(root, "Toast", 60, 1224, 600, 48, Cream, 22).rectTransform;
                toastText = Label(toastRoot, "Message", "", 14, 5, 572, 38, 20, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
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
