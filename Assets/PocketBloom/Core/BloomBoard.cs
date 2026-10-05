using System;
using System.Collections.Generic;
using System.Linq;

namespace PocketBloom
{
    [Serializable]
    public sealed class BloomRun
    {
        public int version = 1;
        public int[] cells = new int[64];
        public int[] buds = new int[64];
        public int[] hand = new int[3];
        public int[] colors = new int[3];
        public int score, lines, flowers, combo, moves, stage, target, moveLimit;
        public int undoLeft = 1, shuffleLeft = 1, revives;
        public uint randomState;
        public string mode = "journey", day = "";
        public bool finished, won, rewardClaimed, countedRun;
        public int awardedSeeds;

        public BloomRun Copy()
        {
            var copy = (BloomRun)MemberwiseClone();
            copy.cells = (int[])cells.Clone(); copy.buds = (int[])buds.Clone();
            copy.hand = (int[])hand.Clone(); copy.colors = (int[])colors.Clone();
            return copy;
        }
        public bool IsValid()
        {
            return version == 1 && cells?.Length == 64 && buds?.Length == 64 &&
                hand?.Length == 3 && colors?.Length == 3 && cells.All(c => c >= 0 && c <= 5) &&
                buds.All(c => c == 0 || c == 1) && hand.All(h => h >= -1 && h < BloomBoard.Shapes.Length) &&
                colors.All(c => c >= 1 && c <= 5) && score >= 0 && stage >= 1 && stage <= 36 &&
                (mode == "journey" || mode == "endless" || mode == "daily");
        }
    }

    public sealed class BloomMove
    {
        public int gained, lines, flowers;
        public readonly List<int> planted = new List<int>();
        public readonly List<int> cleared = new List<int>();
    }

    // 규칙은 Unity와 분리하여 저장 복원, 동시 줄 제거, 난수 재현을 검증한다.
    public static class BloomBoard
    {
        public const int Size = 8;
        static readonly uint[] CampaignSeeds = { 912,1085,1258,1431,1604,1777,1950,2123,2296,2469,2642,2815,2988,3161,3335,3507,3680,3853,4026,4199,4372,4546,4718,4891,5066,5237,5410,5586,5756,5929,6103,6275,6448,6625,6798,6968 };
        public static uint CampaignSeed(int stage) => CampaignSeeds[Math.Max(1, Math.Min(36, stage)) - 1];
        public static readonly int[][] Shapes = {
            new[]{0,0}, new[]{0,0,1,0}, new[]{0,0,0,1},
            new[]{0,0,1,0,2,0}, new[]{0,0,0,1,0,2},
            new[]{0,0,1,0,0,1,1,1}, new[]{0,0,0,1,1,1},
            new[]{0,0,1,0,1,1}, new[]{1,0,0,1,1,1},
            new[]{0,0,1,0,0,1}, new[]{0,0,1,0,2,0,3,0},
            new[]{0,0,0,1,0,2,0,3}, new[]{0,0,1,0,2,0,1,1},
            new[]{1,0,0,1,1,1,2,1}, new[]{0,0,0,1,1,1,0,2},
            new[]{1,0,0,1,1,1,1,2}, new[]{0,0,1,0,1,1,2,1},
            new[]{1,0,2,0,0,1,1,1}, new[]{0,0,0,1,0,2,1,2},
            new[]{0,0,1,0,2,0,0,1,1,1,2,1}, new[]{0,0,1,0,0,1,1,1,0,2,1,2}
        };

        public static uint Next(BloomRun run)
        {
            uint x = run.randomState == 0 ? 0x9E3779B9u : run.randomState;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return run.randomState = x;
        }
        public static BloomRun New(string mode, int stage, uint seed, string day = "")
        {
            stage = Math.Max(1, Math.Min(36, stage));
            var r = new BloomRun { mode = mode, stage = stage, randomState = seed, day = day,
                target = mode == "daily" ? 1800 : 320 + (stage - 1) * 65,
                moveLimit = mode == "daily" ? 45 : 24 + stage / 3 };
            if (mode == "endless") { r.target = 0; r.moveLimit = 0; }
            if (mode == "journey")
            {
                // 처음에는 가로 세 칸을 놓으면 바로 줄 제거를 경험한다.
                int rows = stage == 1 ? 1 : 2 + stage / 12;
                for (int y = 0; y < rows; y++)
                    for (int x = 0; x < 5; x++) r.cells[y * 8 + x] = 1 + y % 5;
                if (stage > 1) for (int y = 0; y < rows; y++) r.buds[y * 8 + 2] = 1;
            }
            Deal(r);
            if (mode == "journey") { r.hand[0] = 3; r.colors[0] = 1; }
            return r;
        }
        public static bool Fits(BloomRun r, int shape, int x, int y)
        {
            if (shape < 0 || shape >= Shapes.Length) return false;
            var cells = Shapes[shape];
            for (int i = 0; i < cells.Length; i += 2)
            {
                int px = x + cells[i], py = y + cells[i + 1];
                if (px < 0 || py < 0 || px >= 8 || py >= 8 || r.cells[py * 8 + px] != 0) return false;
            }
            return true;
        }
        public static bool HasMove(BloomRun r)
        {
            foreach (int s in r.hand)
                if (CanFit(r, s)) return true;
            return false;
        }
        static bool CanFit(BloomRun r, int shape)
        {
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                if (Fits(r, shape, x, y)) return true;
            return false;
        }
        public static void Deal(BloomRun r)
        {
            // 매 묶음의 최소 한 조각은 현재 판에 놓을 수 있다. 미래 수까지 보장하지 않는다.
            int max = r.mode == "journey" && r.stage < 4 ? 10 : Shapes.Length;
            for (int i = 0; i < 3; i++)
            {
                int s = (int)(Next(r) % (uint)max);
                if (i == 0 && !CanFit(r, s))
                {
                    var legal = new List<int>();
                    for (int k = 0; k < max; k++) if (CanFit(r, k)) legal.Add(k);
                    if (legal.Count > 0) s = legal[(int)(Next(r) % (uint)legal.Count)];
                }
                r.hand[i] = s; r.colors[i] = 1 + (int)(Next(r) % 5);
            }
        }
        public static BloomMove Place(BloomRun r, int slot, int x, int y)
        {
            if (r.finished || slot < 0 || slot > 2 || !Fits(r, r.hand[slot], x, y)) return null;
            var result = new BloomMove();
            var shape = Shapes[r.hand[slot]];
            for (int i = 0; i < shape.Length; i += 2)
            {
                int index = (y + shape[i + 1]) * 8 + x + shape[i];
                r.cells[index] = r.colors[slot]; result.planted.Add(index);
            }
            var clear = new bool[64];
            for (int row = 0; row < 8; row++)
            {
                bool fullRow = true, fullCol = true;
                for (int col = 0; col < 8; col++)
                { fullRow &= r.cells[row * 8 + col] != 0; fullCol &= r.cells[col * 8 + row] != 0; }
                if (fullRow) { result.lines++; for (int c = 0; c < 8; c++) clear[row * 8 + c] = true; }
                if (fullCol) { result.lines++; for (int c = 0; c < 8; c++) clear[c * 8 + row] = true; }
            }
            for (int i = 0; i < 64; i++) if (clear[i])
            { result.cleared.Add(i); result.flowers += r.buds[i]; r.cells[i] = r.buds[i] = 0; }
            r.combo = result.lines > 0 ? r.combo + 1 : 0;
            result.gained = result.planted.Count * 5 + result.lines * 80 * Math.Min(5, r.combo) + result.flowers * 35;
            r.score += result.gained; r.lines += result.lines; r.flowers += result.flowers; r.moves++;
            r.hand[slot] = -1;
            if (r.hand.All(h => h == -1)) Deal(r);
            if (r.target > 0 && r.score >= r.target) { r.finished = r.won = true; }
            else if ((r.moveLimit > 0 && r.moves >= r.moveLimit) || !HasMove(r)) r.finished = true;
            return result;
        }
        public static void Revive(BloomRun r)
        {
            if (!r.finished || r.won || r.revives >= 1) return;
            // 중앙 네 줄을 비워 확실한 재개 공간을 제공하고 점수는 추가하지 않는다.
            for (int y = 2; y < 6; y++) for (int x = 0; x < 8; x++) r.cells[y * 8 + x] = r.buds[y * 8 + x] = 0;
            r.revives++; r.finished = false; r.rewardClaimed = false; r.combo = 0;
            if (r.moveLimit > 0) r.moveLimit += 8;
            Deal(r);
        }
        public static int[] BestMove(BloomRun r)
        {
            int[] best = null; double value = double.MinValue;
            for (int slot = 0; slot < 3; slot++) for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                if (!Fits(r, r.hand[slot], x, y)) continue;
                var sim = r.Copy(); var move = Place(sim, slot, x, y);
                double v = move.gained * 2 - sim.cells.Count(c => c != 0) * 1.5;
                for (int a = 0; a < 8; a++)
                {
                    int row = 0, col = 0;
                    for (int b = 0; b < 8; b++) { if (sim.cells[a * 8 + b] > 0) row++; if (sim.cells[b * 8 + a] > 0) col++; }
                    v += row * row + col * col;
                }
                if (v > value) { value = v; best = new[] { slot, x, y }; }
            }
            return best;
        }
    }
}
