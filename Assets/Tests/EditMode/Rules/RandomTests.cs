using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Random;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    public class RandomTests
    {
        [Test]
        public void SameSeedGivesSameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 200; i++) Assert.AreEqual(a.RollD8(), b.RollD8());
        }

        [Test]
        public void DifferentSeedsGiveDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            var rollsA = Enumerable.Range(0, 50).Select(_ => a.RollD8()).ToList();
            var rollsB = Enumerable.Range(0, 50).Select(_ => b.RollD8()).ToList();
            CollectionAssert.AreNotEqual(rollsA, rollsB);
        }

        [Test]
        public void D8IsInRangeAndRoughlyUniform_R008()
        {
            var random = new SeededRandom(7);
            var counts = new int[9];
            const int rolls = 80000;
            for (int i = 0; i < rolls; i++) counts[random.RollD8()]++;

            Assert.AreEqual(0, counts[0]);
            for (int face = 1; face <= 8; face++)
                Assert.That(counts[face], Is.InRange(9000, 11000), "faccia " + face);
        }

        [Test]
        public void RangeStaysInBounds()
        {
            var random = new SeededRandom(3);
            for (int i = 0; i < 2000; i++) Assert.That(random.Range(-3, 4), Is.InRange(-3, 3));
            Assert.AreEqual(5, random.Range(5, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => random.Range(4, 4));
        }

        [Test]
        public void ShuffleIsAPermutationAndDeterministic()
        {
            List<int> Shuffled(int seed)
            {
                var list = Enumerable.Range(0, 54).ToList();
                new SeededRandom(seed).Shuffle(list);
                return list;
            }

            CollectionAssert.AreEquivalent(Enumerable.Range(0, 54), Shuffled(5));
            CollectionAssert.AreEqual(Shuffled(5), Shuffled(5));
            CollectionAssert.AreNotEqual(Shuffled(5), Shuffled(6));
        }

        [Test]
        public void ScriptedSourceReturnsScriptedRollsThenFallsBack()
        {
            var scripted = new ScriptedRandomSource(new[] { 3, 8, 1 }, new SeededRandom(11));
            Assert.AreEqual(3, scripted.RollD8());
            Assert.AreEqual(8, scripted.RollD8());
            Assert.AreEqual(1, scripted.RollD8());
            Assert.AreEqual(0, scripted.RemainingScripted);

            var reference = new SeededRandom(11);
            Assert.AreEqual(reference.RollD8(), scripted.RollD8());
        }

        [Test]
        public void ScriptedSourceRejectsInvalidRolls()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScriptedRandomSource(new[] { 9 }, new SeededRandom(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScriptedRandomSource(new[] { 0 }, new SeededRandom(1)));
        }
    }
}
