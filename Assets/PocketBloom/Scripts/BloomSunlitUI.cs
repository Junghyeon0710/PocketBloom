using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketBloom
{
    // 선택한 Sunlit Garden 시안을 실제 입력 가능한 uGUI 요소로 구성한다.
    public sealed partial class BloomGame
    {
        static readonly Color Soil = Hex("A5B690");
        readonly Dictionary<string, Sprite> skin = new Dictionary<string, Sprite>();
        TMP_Text movesText;

        void Illustration(RectTransform parent, string asset, float x, float y, float w, float h, bool preserve = false)
        {
            if (!skin.TryGetValue(asset, out var sprite))
                skin[asset] = sprite = Resources.Load<Sprite>("Sunlit/" + asset);
            var image = Rect(parent, asset, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = preserve; image.raycastTarget = false;
        }

        void SunlitHome()
        {
            var seeds = Box(page, "SeedBadge", 42, 38, 198, 68, Cream, 32).rectTransform;
            Icon(seeds, BloomIcon.Kind.Seed, 15, 16, 42, 38);
            Label(seeds, "Seeds", Profile.seeds.ToString("N0"), 62, 10, 125, 49, 31, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            IconButton(page, "Settings", BloomIcon.Kind.Gear, 600, 38, 76, 68, Cream, Settings);
            Illustration(page, "BotanicalLogo", 120, 136, 480, 246, true);
            bool resume = Run != null && !Run.finished;
            var start = SurfaceButton(page, "Play", 44, 755, 632, 110, Coral,
                () => { if (resume) ShowGame(); else StartRun("journey", Profile.unlockedStage); });
            var title = Label(start, "Label", resume ? T("이어서 꽃 피우기", "Continue blooming") : T("정원 여행 시작", "Start your garden"), 34, 22, 520, 65, 39, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            var shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.48f, .22f, .1f, .35f); shadow.effectDistance = new Vector2(0, -2);
            Icon(start, BloomIcon.Kind.Chevron, 566, 36, 28, 40, Color.white);
            Button(page, "Journey", T("정원 여행", "Journey") + "  " + Profile.unlockedStage.ToString("00") + " / 36", 144, 874, 432, 42, Cream, Journey, 20);
            HomeCard("Daily", "DailyArt", T("오늘의 정원", "Daily garden"), 44, () => OfferNew("daily", 1));
            HomeCard("Endless", "EndlessArt", T("끝없는 정원", "Endless garden"), 369, () => OfferNew("endless", 1));
            var collection = SurfaceButton(page, "Collection", 44, 1134, 632, 104, Cream, Collection);
            Illustration(collection, "CollectionArt", 12, 3, 134, 96, true);
            Label(collection, "Label", T("나의 정원", "My garden"), 168, 27, 360, 53, 31, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Icon(collection, BloomIcon.Kind.Chevron, 561, 36, 24, 33);
        }

        void HomeCard(string name, string art, string text, float x, Action action)
        {
            var card = SurfaceButton(page, name, x, 930, 307, 185, Cream, action);
            Illustration(card, art, 43, 4, 220, 125, true);
            Label(card, "Label", text, 12, 132, 253, 43, 28, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Icon(card, BloomIcon.Kind.Chevron, 270, 140, 17, 26);
        }

        void SunlitGame()
        {
            IconButton(page, "Back", BloomIcon.Kind.Back, 40, 30, 70, 70, Cream, Pause);
            Label(page, "GardenTitle", Run.mode == "journey" ? T("정원 여행 ", "Garden journey ") + Run.stage.ToString("00") : Run.mode == "daily" ? T("오늘의 정원", "Daily garden") : T("끝없는 정원", "Endless garden"), 122, 43, 476, 52, 31, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            IconButton(page, "Pause", BloomIcon.Kind.Pause, 610, 30, 70, 70, Cream, Pause);
            var panel = Box(page, "ScoreCard", 40, 119, 640, 131, Cream, 28).rectTransform;
            Label(panel, "ScoreCaption", T("점수", "SCORE"), 14, 10, 190, 34, 21, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreText = Label(panel, "Score", "", 14, 40, 190, 55, 35, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(panel, "TargetCaption", Run.target > 0 ? T("목표", "GOAL") : T("최고", "BEST"), 224, 10, 190, 34, 21, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(panel, "Target", (Run.target > 0 ? Run.target : Profile.best).ToString("N0"), 224, 40, 190, 55, 35, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(panel, "MovesCaption", Run.moveLimit > 0 ? T("남은 수", "MOVES") : T("콤보", "COMBO"), 436, 10, 190, 34, 21, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            movesText = Label(panel, "Moves", "", 436, 40, 190, 55, 35, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Box(panel, "Divider", 213, 20, 2, 64, Hex("E5D8BB"), 0);
            Box(panel, "Divider", 425, 20, 2, 64, Hex("E5D8BB"), 0);
            Box(panel, "ProgressTrack", 36, 104, 568, 15, Hex("DDD1B3"), 7);
            progress = Box(panel, "Progress", 36, 104, 1, 15, Hex("83BE7A"), 7);
            detailText = Label(page, "Detail", "", 40, 258, 640, 31, 20, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            Box(page, "BoardShadow", 46, 301, 632, 632, Hex("B1BB90"), 27);
            Box(page, "BoardFrame", 44, 296, 632, 632, Hex("D7DDAC"), 27);
            board = Rect(page, "Board", 60, 312, 600, 600);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                int index = y * 8 + x, px = x, py = y;
                tiles[index] = Box(board, "Cell_" + index, x * 75 + 2, y * 75 + 2, 71, 71, Soil, 12);
                tiles[index].inset = true; tiles[index].raycastTarget = true;
                var button = tiles[index].gameObject.AddComponent<Button>(); button.targetGraphic = tiles[index];
                button.onClick.AddListener(() => PlaceSelected(px, py));
            }
            for (int i = 0; i < 3; i++)
            {
                slots[i] = Rect(page, "Piece_" + i, 46 + i * 215, 947, 198, 142);
                var hit = Box(slots[i], "TouchArea", 0, 0, 198, 142, Cream, 24); hit.raycastTarget = true;
                var input = hit.gameObject.AddComponent<BloomPieceInput>(); input.game = this; input.slot = i;
            }
            ToolButton("Undo", BloomIcon.Kind.Undo, T("한 수 뒤로", "Undo"), 44, Mint, Undo);
            ToolButton("Hint", BloomIcon.Kind.Hint, T("힌트", "Hint"), 261, Gold, Hint);
            ToolButton("Shuffle", BloomIcon.Kind.Shuffle, T("새 조각", "New pieces"), 478, Hex("A7D7E9"), Shuffle);
            Label(page, "TrayCaption", T("꽃 조각을 끌어 빈 칸에 놓아주세요", "Drag a flower piece into an empty space"), 40, 1243, 640, 29, 18, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        RectTransform SurfaceButton(RectTransform parent, string name, float x, float y, float w, float h, Color color, Action action)
        {
            Box(parent, name + "Shadow", x, y + 4, w, h, Color.Lerp(color, Ink, .2f), 26);
            var box = Box(parent, name, x, y, w, h, color, 26); box.raycastTarget = true;
            var b = box.gameObject.AddComponent<Button>(); b.targetGraphic = box;
            var colors = b.colors; colors.pressedColor = new Color(.85f, .9f, .85f); b.colors = colors;
            b.onClick.AddListener(() => { audioFx.Play(0); action(); }); return box.rectTransform;
        }
        void IconButton(RectTransform parent, string name, BloomIcon.Kind icon, float x, float y, float w, float h, Color color, Action action)
        {
            var button = SurfaceButton(parent, name, x, y, w, h, color, action);
            Icon(button, icon, (w - 35) / 2, (h - 35) / 2, 35, 35);
        }
        void ToolButton(string name, BloomIcon.Kind icon, string text, float x, Color color, Action action)
        {
            var button = SurfaceButton(page, name, x, 1110, 198, 118, color, action);
            Icon(button, icon, 76, 13, 46, 46);
            Label(button, "Label", text, 8, 69, 182, 38, 26, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
        }
        static void Icon(RectTransform parent, BloomIcon.Kind kind, float x, float y, float w, float h, Color? color = null)
        {
            var icon = Rect(parent, "Icon", x, y, w, h).gameObject.AddComponent<BloomIcon>();
            icon.kind = kind; icon.color = color ?? Ink; icon.raycastTarget = false;
        }
    }
}
