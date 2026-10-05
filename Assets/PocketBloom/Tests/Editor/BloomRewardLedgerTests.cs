using NUnit.Framework;

namespace PocketBloom.Tests
{
    public sealed class BloomRewardLedgerTests
    {
        [Test] public void RewardAfterCloseIsDeliveredOnce()
        {
            var ledger = new BloomRewardLedger(); int rewards = 0, failures = 0;
            Assert.That(ledger.Begin(() => rewards++, () => failures++, () => true));
            ledger.Displayed("auction-A"); Assert.That(ledger.Close("auction-A"));
            Assert.That(rewards, Is.Zero); Assert.That(failures, Is.Zero);
            Assert.That(ledger.IsShowing, Is.False);
            Assert.That(ledger.Reward("auction-A")); Assert.That(ledger.Reward("auction-A"), Is.False);
            Assert.That(rewards, Is.EqualTo(1));
        }
        [Test] public void DelayedRewardBelongsToOriginalRequestWhileAnotherAdIsShowing()
        {
            var ledger = new BloomRewardLedger(); int first = 0, second = 0;
            ledger.Begin(() => first++, null, () => true); ledger.Displayed("A"); ledger.Close("A");
            ledger.Begin(() => second++, null, () => true); ledger.Displayed("B");
            ledger.Reward("A"); Assert.That(first, Is.EqualTo(1)); Assert.That(second, Is.Zero);
            Assert.That(ledger.Close("A"), Is.False); Assert.That(ledger.IsShowing);
            ledger.Reward("B"); Assert.That(second, Is.EqualTo(1));
        }
        [Test] public void ChangedRunCannotReceiveAnOldReward()
        {
            var ledger = new BloomRewardLedger(); bool originalRunActive = true; int rewards = 0;
            ledger.Begin(() => rewards++, null, () => originalRunActive); ledger.Displayed("A"); ledger.Close("A");
            originalRunActive = false;
            Assert.That(ledger.Reward("A"), Is.False); Assert.That(rewards, Is.Zero);
        }
        [Test] public void DisplayFailureRejectsLateRewardsAndFailsOnce()
        {
            var ledger = new BloomRewardLedger(); int rewards = 0, failures = 0;
            ledger.Begin(() => rewards++, () => failures++, () => true); ledger.Displayed("A");
            Assert.That(ledger.Fail("A")); Assert.That(ledger.Fail("A"), Is.False);
            Assert.That(ledger.Reward("A"), Is.False);
            Assert.That(rewards, Is.Zero); Assert.That(failures, Is.EqualTo(1));
        }
        [Test] public void RewardBeforeCloseIsDeliveredOnceAndAllowsNextAdOnlyAfterClose()
        {
            var ledger = new BloomRewardLedger(); int rewards = 0;
            ledger.Begin(() => rewards++, null, () => true); ledger.Displayed("A"); ledger.Reward("A");
            Assert.That(ledger.Begin(() => rewards++, null, () => true), Is.False);
            ledger.Close("A"); Assert.That(ledger.Begin(() => rewards++, null, () => true)); ledger.Displayed("B");
            Assert.That(ledger.Reward("A"), Is.False); Assert.That(rewards, Is.EqualTo(1));
            Assert.That(ledger.Reward("unrecognized"), Is.False);
        }
    }
}
