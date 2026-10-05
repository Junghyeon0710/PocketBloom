using System;
using System.Linq;
using NUnit.Framework;

namespace PocketBloom.Tests
{
    public class BloomBoardTests
    {
        [Test] public void TutorialFirstPlacementClearsOneRow()
        {
            var r = BloomBoard.New("journey", 1, 912); var result = BloomBoard.Place(r, 0, 5, 0);
            Assert.That(result.lines, Is.EqualTo(1)); Assert.That(result.cleared.Count, Is.EqualTo(8)); Assert.That(r.score, Is.EqualTo(95));
        }
        [Test] public void RowAndColumnClearSimultaneouslyWithoutDoubleCounting()
        {
            var r = BloomBoard.New("endless", 1, 1); Array.Clear(r.cells, 0, 64); r.hand[0] = 0;
            for (int i = 1; i < 8; i++) { r.cells[i] = 1; r.cells[i * 8] = 2; }
            r.buds[0] = 1;
            var move = BloomBoard.Place(r, 0, 0, 0);
            Assert.That(move.lines, Is.EqualTo(2)); Assert.That(move.cleared.Count, Is.EqualTo(15));
            Assert.That(move.flowers, Is.EqualTo(1)); Assert.That(r.score, Is.EqualTo(200)); Assert.That(r.cells.All(c => c == 0));
        }
        [Test] public void InvalidPlacementDoesNotMutateRunOrRng()
        {
            var r = BloomBoard.New("journey", 2, 4); var copy = r.Copy();
            Assert.That(BloomBoard.Place(r, 0, 0, 0), Is.Null);
            Assert.That(r.cells, Is.EqualTo(copy.cells)); Assert.That(r.randomState, Is.EqualTo(copy.randomState)); Assert.That(r.moves, Is.Zero);
            Assert.That(BloomBoard.Fits(r, 3, 7, 7), Is.False); Assert.That(BloomBoard.Fits(r, -1, 0, 0), Is.False);
        }
        [Test] public void DailySeedAndCopyResumeAreDeterministic()
        {
            var a = BloomBoard.New("daily", 1, 20261004); var b = a.Copy();
            for (int i = 0; i < 30 && !a.finished; i++)
            {
                var m = BloomBoard.BestMove(a); if (m == null) break;
                BloomBoard.Place(a, m[0], m[1], m[2]); BloomBoard.Place(b, m[0], m[1], m[2]);
                Assert.That(a.cells, Is.EqualTo(b.cells)); Assert.That(a.hand, Is.EqualTo(b.hand)); Assert.That(a.randomState, Is.EqualTo(b.randomState));
            }
        }
        [Test] public void WinTakesPriorityOverMoveLimit()
        {
            var r = BloomBoard.New("journey", 1, 3); r.target = 95; r.moveLimit = 1;
            BloomBoard.Place(r, 0, 5, 0); Assert.That(r.won); Assert.That(r.finished);
        }
        [Test] public void NoLegalMoveEndsRunAndReviveIsLimitedToOne()
        {
            var r = BloomBoard.New("endless", 1, 1);
            for (int i = 0; i < 64; i++) r.cells[i] = ((i % 8 + i / 8) % 2 == 0) ? 0 : 1;
            r.hand = new[] { 0, 5, 5 }; BloomBoard.Place(r, 0, 0, 0);
            Assert.That(r.finished); BloomBoard.Revive(r); Assert.That(r.finished, Is.False); Assert.That(BloomBoard.HasMove(r));
            r.finished = true; BloomBoard.Revive(r); Assert.That(r.finished); Assert.That(r.revives, Is.EqualTo(1));
        }
        [Test] public void AllCampaignStagesHaveAWinningUnassistedRoute()
        {
            for (int stage = 1; stage <= 36; stage++)
            {
                var r = BloomBoard.New("journey", stage, BloomBoard.CampaignSeed(stage));
                for (int i = 0; i < 100 && !r.finished; i++)
                { var m = BloomBoard.BestMove(r); Assert.That(m, Is.Not.Null, "stage " + stage); BloomBoard.Place(r, m[0], m[1], m[2]); }
                Assert.That(r.won, Is.True, "stage " + stage + " score " + r.score + "/" + r.target);
            }
        }
        [Test] public void TenThousandRandomLegalMovesMaintainInvariants()
        {
            var rng = new Random(61004); int total = 0;
            for (int game = 0; game < 1000 && total < 10000; game++)
            {
                var r = BloomBoard.New("endless", 1, (uint)rng.Next());
                while (!r.finished && total < 10000)
                {
                    var options = (from s in Enumerable.Range(0, 3) from y in Enumerable.Range(0, 8) from x in Enumerable.Range(0, 8) where BloomBoard.Fits(r, r.hand[s], x, y) select new[] { s, x, y }).ToArray();
                    if (options.Length == 0) break; var m = options[rng.Next(options.Length)]; var before = r.score;
                    BloomBoard.Place(r, m[0], m[1], m[2]); total++;
                    Assert.That(r.IsValid()); Assert.That(r.score, Is.GreaterThan(before));
                    for (int row = 0; row < 8; row++)
                    { Assert.That(Enumerable.Range(0, 8).Any(c => r.cells[row * 8 + c] == 0)); Assert.That(Enumerable.Range(0, 8).Any(c => r.cells[c * 8 + row] == 0)); }
                }
            }
            Assert.That(total, Is.EqualTo(10000));
        }
    }
}
