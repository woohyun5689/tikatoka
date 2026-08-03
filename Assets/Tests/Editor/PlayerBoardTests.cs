using NUnit.Framework;

namespace Tikatooka.Tests
{
    public sealed class PlayerBoardTests
    {
        [Test]
        public void PlacementStartsFromEachPlayersOwnSide()
        {
            var playerOne = new PlayerBoard(0, 3);
            var playerTwo = new PlayerBoard(1, 3);

            Assert.That(playerOne.CanPlace(0, 2), Is.True);
            Assert.That(playerOne.CanPlace(0, 1), Is.False);
            Assert.That(playerTwo.CanPlace(0, 0), Is.True);
            Assert.That(playerTwo.CanPlace(0, 1), Is.False);

            playerOne.Place(0, 2, 4, false);
            playerTwo.Place(0, 0, 4, false);

            Assert.That(playerOne.CanPlace(0, 1), Is.True);
            Assert.That(playerTwo.CanPlace(0, 1), Is.True);
        }

        [TestCase(0, 2, 1, 0)]
        [TestCase(1, 0, 1, 2)]
        public void MatchingDiceMoveTogether(int player, int first, int second, int third)
        {
            var board = new PlayerBoard(player, 3);

            board.Place(0, first, 2, false);
            board.Place(0, second, 5, false);
            var finalColumn = board.Place(0, third, 2, false);

            Assert.That(board.Cells[0, first], Is.EqualTo(2));
            Assert.That(board.Cells[0, second], Is.EqualTo(2));
            Assert.That(board.Cells[0, third], Is.EqualTo(5));
            Assert.That(finalColumn, Is.EqualTo(second));
        }

        [Test]
        public void ProtectedDieSurvivesAttackAndCompaction()
        {
            var board = new PlayerBoard(0, 3);
            board.Place(0, 2, 4, false);
            board.Place(0, 1, 4, true);
            board.Place(0, 0, 4, false);

            var removed = board.RemoveMatchingGroup(0, 2);

            Assert.That(removed, Is.EqualTo(2));
            Assert.That(board.Cells[0, 2], Is.EqualTo(4));
            Assert.That(board.AttackProtected[0, 2], Is.True);
            Assert.That(board.Cells[0, 1], Is.EqualTo(0));
            Assert.That(board.AttackProtected[0, 1], Is.False);
            Assert.That(board.FilledCount, Is.EqualTo(1));
        }

        [Test]
        public void ScoreIncludesMatchingBonus()
        {
            var board = new PlayerBoard(0, 3);
            board.Place(0, 2, 3, false);
            board.Place(0, 1, 3, false);
            board.Place(0, 0, 5, false);

            Assert.That(board.CalculateSectionScoreUnits(0), Is.EqualTo(28));
        }

        [Test]
        public void AttackPreviewRemovesOnlyTheSelectedMatchingGroup()
        {
            var board = new PlayerBoard(0, 3);
            board.Place(0, 2, 3, false);
            board.Place(0, 1, 3, false);
            board.Place(0, 0, 5, false);

            Assert.That(board.GetMatchingGroupSize(0, 2), Is.EqualTo(2));
            Assert.That(board.CalculateSectionScoreUnitsWithoutMatchingGroup(0, 2), Is.EqualTo(10));
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, false)]
        [TestCase(6, false)]
        public void OnlySmallAiBonusDiceTargetThePlayerBoard(int dieValue, bool expected)
        {
            Assert.That(
                DiceBoardGameController.ShouldAiPlaceBonusOnOpponent(dieValue, true, false),
                Is.EqualTo(expected));
        }

        [Test]
        public void AiBonusTargetRequiresThePostAttackPlacementState()
        {
            Assert.That(DiceBoardGameController.ShouldAiPlaceBonusOnOpponent(2, false, false), Is.False);
            Assert.That(DiceBoardGameController.ShouldAiPlaceBonusOnOpponent(2, true, true), Is.False);
        }
    }
}
