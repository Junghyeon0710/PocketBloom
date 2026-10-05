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
            // Image.preserveAspect는 RectTransform의 pivot을 기준으로 맞춘다.
            // 좌상단 pivot을 그대로 쓰면 세로 삽화가 카드 왼쪽에 붙는다.
            if (preserve)
            {
                image.rectTransform.pivot = new Vector2(.5f, .5f);
                image.rectTransform.anchoredPosition = new Vector2(x + w / 2, -y - h / 2);
                var padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
                float scale = Mathf.Min(w / sprite.rect.width, h / sprite.rect.height);
                image.rectTransform.anchoredPosition += new Vector2(padding.z - padding.x, padding.w - padding.y) * scale / 2;
            }
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
            var title = Label(start, "Label", resume ? T("이어서 꽃 피우기", "Continue blooming") : T("정원 여행 시작", "Start your garden"), 56, 22, 520, 65, 37, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            var shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.48f, .22f, .1f, .35f); shadow.effectDistance = new Vector2(0, -2);
            Icon(start, BloomIcon.Kind.Chevron, 566, 36, 28, 40, Color.white);
            Button(page, "Journey", T("정원 여행", "Journey") + "  " + Profile.unlockedStage.ToString("00") + " / 36", 144, 874, 432, 42, Cream, Journey, 20);
            HomeCard("Daily", "DailyArt", T("오늘의 정원", "Daily garden"), 44, () => OfferNew("daily", 1));
            HomeCard("Endless", "EndlessArt", T("끝없는 정원", "Endless garden"), 369, () => OfferNew("endless", 1));
            var collection = SurfaceButton(page, "Collection", 44, 1134, 632, 104, Cream, Collection);
            Illustration(collection, "CollectionArt", 22, 5, 130, 92, true);
            Label(collection, "Label", T("나의 정원", "My garden"), 168, 25, 360, 53, 31, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Icon(collection, BloomIcon.Kind.Chevron, 561, 36, 24, 33);
        }

        void HomeCard(string name, string art, string text, float x, Action action)
        {
            var card = SurfaceButton(page, name, x, 930, 307, 185, Cream, action);
            Illustration(card, art, (307 - 238) / 2f, 8, 238, 122, true);
            // 제목은 카드 중심에, 화살표는 독립된 끝 여백에 둔다.
            Label(card, "Label", text, 32, 132, 243, 43, 27, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Icon(card, BloomIcon.Kind.Chevron, 277, 141, 13, 24);
        }

        void SunlitGame()
        {
            IconButton(page, "Back", BloomIcon.Kind.Back, 44, 30, 70, 70, Cream, Pause);
            Label(page, "GardenTitle", Run.mode == "journey" ? T("정원 여행 ", "Garden journey ") + Run.stage.ToString("00") : Run.mode == "daily" ? T("오늘의 정원", "Daily garden") : T("끝없는 정원", "Endless garden"), 128, 39, 464, 52, 30, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            IconButton(page, "Pause", BloomIcon.Kind.Pause, 606, 30, 70, 70, Cream, Pause);
            var panel = Box(page, "ScoreCard", 44, 119, 632, 131, Cream, 28).rectTransform;
            const float column = 632f / 3;
            Label(panel, "ScoreCaption", T("점수", "SCORE"), 10, 10, column - 20, 34, 21, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreText = Label(panel, "Score", "", 10, 40, column - 20, 55, 35, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(panel, "TargetCaption", Run.target > 0 ? T("목표", "GOAL") : T("최고", "BEST"), column + 10, 10, column - 20, 34, 21, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(panel, "Target", (Run.target > 0 ? Run.target : Profile.best).ToString("N0"), column + 10, 40, column - 20, 55, 35, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(panel, "MovesCaption", Run.moveLimit > 0 ? T("남은 수", "MOVES") : T("콤보", "COMBO"), column * 2 + 10, 10, column - 20, 34, 21, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            movesText = Label(panel, "Moves", "", column * 2 + 10, 40, column - 20, 55, 35, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Box(panel, "Divider", column - 1, 20, 2, 64, Hex("E5D8BB"), 0);
            Box(panel, "Divider", column * 2 - 1, 20, 2, 64, Hex("E5D8BB"), 0);
            Box(panel, "ProgressTrack", 36, 104, 560, 15, Hex("DDD1B3"), 7);
            progress = Box(panel, "Progress", 36, 104, 1, 15, Hex("83BE7A"), 7);
            detailText = Label(page, "Detail", "", 44, 258, 632, 31, 20, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            Box(page, "BoardShadow", 44, 301, 632, 632, Hex("B1BB90"), 27);
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
                slots[i] = Rect(page, "Piece_" + i, 44 + i * 217, 947, 198, 142);
                var hit = Box(slots[i], "TouchArea", 0, 0, 198, 142, Cream, 24); hit.raycastTarget = true;
                var input = hit.gameObject.AddComponent<BloomPieceInput>(); input.game = this; input.slot = i;
            }
            ToolButton("Undo", BloomIcon.Kind.Undo, T("한 수 뒤로", "Undo"), 44, Mint, Undo);
            ToolButton("Hint", BloomIcon.Kind.Hint, T("힌트", "Hint"), 261, Gold, Hint);
            ToolButton("Shuffle", BloomIcon.Kind.Shuffle, T("새 조각", "New pieces"), 478, Hex("A7D7E9"), Shuffle);
            Label(page, "TrayCaption", T("꽃 조각을 끌어 빈 칸에 놓아주세요", "Drag a flower piece into an empty space"), 44, 1243, 632, 29, 18, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        void SunlitVictory(bool final, Choice[] choices)
        {
            CloseOverlay(); ClearToast();
            overlay = Rect(root, "Dialog", 0, 0, 720, 1280);
            Box(overlay, "Shade", 0, 0, 720, 1280, new Color(.04f, .13f, .09f, .72f), 0).raycastTarget = true;
            var petals = new List<BloomGraphic>();
            if (!Profile.reducedMotion)
                for (int i = 0; i < 16; i++)
                {
                    var petal = Box(overlay, "CelebrationPetal_" + i, 18 + i * 43, 90 + (i * 71) % 190, 22 + i % 3 * 5, 22 + i % 3 * 5, Petals[i % Petals.Length], 0);
                    petal.flower = true; petals.Add(petal);
                }
            Box(overlay, "CardShadow", 50, 206, 620, 880, Hex("8D9D78"), 36);
            var card = Box(overlay, "Card", 50, 200, 620, 880, Cream, 36).rectTransform;
            card.pivot = new Vector2(.5f, .5f); card.anchoredPosition = new Vector2(360, -640);
            Illustration(card, "VictoryArt", 85, 16, 450, 212, true);
            Label(card, "Title", final ? T("36개의 정원, 모두 피웠어요!", "All 36 gardens are in bloom!") : T("오늘도 예쁘게 피었어요", "A lovely little bloom"), 32, 232, 556, 96, 32, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(card, "Subtitle", Run.mode == "journey" ? T("정원 여행 ", "Garden journey ") + Run.stage.ToString("00") : T("오늘의 정원", "Daily garden"), 32, 328, 556, 32, 20, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            int earned = Run.moves <= Run.moveLimit * .55f ? 3 : Run.moves <= Run.moveLimit * .8f ? 2 : 1;
            var awards = new List<RectTransform>();
            int count = Run.mode == "journey" ? 3 : 1;
            for (int i = 0; i < count; i++)
            {
                var bloom = Box(card, "AwardFlower_" + i, (620 - (count * 54 + (count - 1) * 28)) / 2f + i * 82, 374, 54, 54,
                    Run.mode != "journey" || i < earned ? Gold : Hex("D7DDC3"), 0);
                bloom.flower = true; awards.Add(bloom.rectTransform);
            }
            var summary = Box(card, "ScoreSummary", 32, 446, 556, 96, Hex("F2E8CE"), 24).rectTransform;
            Label(summary, "ScoreCaption", T("점수", "SCORE"), 20, 6, 516, 30, 18, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            var score = Label(summary, "ResultScore", Run.score.ToString("N0"), 20, 32, 516, 64, 42, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            var reward = Box(card, "SeedReward", 32, 560, 556, 68, Mint, 24).rectTransform;
            var seedLabel = Label(reward, "Seeds", T("씨앗 +", "Seeds +") + EarnedSeeds(), 0, 10, 1, 48, 28, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            float seedWidth = Mathf.Ceil(seedLabel.GetPreferredValues().x) + 8;
            float seedLeft = (556 - seedWidth - 46) / 2;
            seedLabel.rectTransform.sizeDelta = new Vector2(seedWidth, 48);
            seedLabel.rectTransform.anchoredPosition = new Vector2(seedLeft + 46, -10);
            Icon(reward, BloomIcon.Kind.Seed, seedLeft, 18, 34, 32);
            Label(card, "ResultDetails", T("피운 줄 ", "Lines ") + Run.lines + "    ·    " + T("사용한 수 ", "Moves ") + Run.moves, 32, 642, 556, 34, 21, Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            for (int i = 0; i < choices.Length; i++)
            {
                var choice = choices[i];
                Button(card, "Choice_" + i, choice.label, 32, i == 0 ? 700 : 792, 556, i == 0 ? 78 : 60,
                    i == 0 ? Coral : Hex("E5ECD9"), choice.action, i == 0 ? 29 : 24, i == 0 ? Color.white : Ink);
            }
            card.gameObject.AddComponent<BloomCelebration>().Setup(card, score, Run.score, awards.ToArray(), petals.ToArray(), Profile.reducedMotion);
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
