using UnityEngine.UI;

namespace Tikatooka
{
    internal sealed class PlayerBoard
    {
        public readonly int[,] Cells;
        public readonly bool[,] AttackProtected;
        public readonly Button[,] CellButtons;
        public readonly Text[,] CellTexts;
        public readonly Image[,] CellFaceImages;
        public readonly Image[,] CellProtectionMarkers;
        public readonly Image[,,] CellPips;
        public readonly Outline[,] CellOutlines;
        public readonly Image[] SectionLabelImages;
        public readonly Text[] SectionLabelTexts;
        public Image PanelImage;
        public Outline PanelOutline;
        public Text TitleText;
        public Text ProgressText;
        public Image ProgressTrackImage;
        public Image ProgressFillImage;

        private readonly int size;
        private int filledCount;

        public PlayerBoard(int playerIndex, int size)
        {
            PlayerIndex = playerIndex;
            this.size = size;
            Cells = new int[size, size];
            AttackProtected = new bool[size, size];
            CellButtons = new Button[size, size];
            CellTexts = new Text[size, size];
            CellFaceImages = new Image[size, size];
            CellProtectionMarkers = new Image[size, size];
            CellPips = new Image[size, size, 7];
            CellOutlines = new Outline[size, size];
            SectionLabelImages = new Image[size];
            SectionLabelTexts = new Text[size];
        }

        public int PlayerIndex { get; }

        public int Capacity => size * size;

        public int FilledCount => filledCount;

        public bool IsFull => filledCount >= size * size;

        public bool IsSectionFull(int row)
        {
            for (var column = 0; column < size; column++)
            {
                if (Cells[row, column] == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public int CountSectionFilled(int row)
        {
            var count = 0;
            for (var column = 0; column < size; column++)
            {
                if (Cells[row, column] != 0)
                {
                    count++;
                }
            }

            return count;
        }

        public void Clear()
        {
            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    Cells[row, column] = 0;
                    AttackProtected[row, column] = false;
                }
            }

            filledCount = 0;
        }

        public bool CanPlace(int row, int column)
        {
            if (Cells[row, column] != 0)
            {
                return false;
            }

            if (PlayerIndex == 0)
            {
                return column == size - 1 || Cells[row, column + 1] != 0;
            }

            return column == 0 || Cells[row, column - 1] != 0;
        }

        public int Place(int row, int column, int value, bool attackProtected)
        {
            if (Cells[row, column] != 0)
            {
                return -1;
            }

            Cells[row, column] = value;
            AttackProtected[row, column] = attackProtected;
            filledCount++;
            var placedIndex = GroupMatchingDice(row, value);
            return placedIndex >= 0 ? GetColumnForSequenceIndex(placedIndex) : column;
        }

        public int RemoveMatchingGroup(int row, int column)
        {
            var value = Cells[row, column];
            if (value == 0 || AttackProtected[row, column])
            {
                return 0;
            }

            var selectedIndex = GetSequenceIndexForColumn(column);
            var startIndex = selectedIndex;
            while (startIndex > 0)
            {
                var previousColumn = GetColumnForSequenceIndex(startIndex - 1);
                if (Cells[row, previousColumn] != value || AttackProtected[row, previousColumn])
                {
                    break;
                }

                startIndex--;
            }

            var endIndex = selectedIndex;
            while (endIndex < size - 1)
            {
                var nextColumn = GetColumnForSequenceIndex(endIndex + 1);
                if (Cells[row, nextColumn] != value || AttackProtected[row, nextColumn])
                {
                    break;
                }

                endIndex++;
            }

            var removed = 0;
            for (var index = startIndex; index <= endIndex; index++)
            {
                var targetColumn = GetColumnForSequenceIndex(index);
                if (Cells[row, targetColumn] == value)
                {
                    Cells[row, targetColumn] = 0;
                    AttackProtected[row, targetColumn] = false;
                    removed++;
                }
            }

            filledCount -= removed;
            CompactRow(row);
            return removed;
        }

        public int[] GetMatchingGroupColumns(int row, int column)
        {
            var value = Cells[row, column];
            if (value == 0 || AttackProtected[row, column])
            {
                return new int[0];
            }

            var selectedIndex = GetSequenceIndexForColumn(column);
            var startIndex = selectedIndex;
            while (startIndex > 0)
            {
                var previousColumn = GetColumnForSequenceIndex(startIndex - 1);
                if (Cells[row, previousColumn] != value || AttackProtected[row, previousColumn])
                {
                    break;
                }

                startIndex--;
            }

            var endIndex = selectedIndex;
            while (endIndex < size - 1)
            {
                var nextColumn = GetColumnForSequenceIndex(endIndex + 1);
                if (Cells[row, nextColumn] != value || AttackProtected[row, nextColumn])
                {
                    break;
                }

                endIndex++;
            }

            var columns = new int[endIndex - startIndex + 1];
            for (var index = startIndex; index <= endIndex; index++)
            {
                columns[index - startIndex] = GetColumnForSequenceIndex(index);
            }

            return columns;
        }

        public int CalculateSectionScoreUnits(int section)
        {
            var scoreUnits = 0;
            for (var value = 1; value <= 6; value++)
            {
                var count = 0;
                for (var column = 0; column < size; column++)
                {
                    if (Cells[section, column] == value)
                    {
                        count++;
                    }
                }

                if (count > 0)
                {
                    var baseScore = value * count;
                    var matchingBonus = value * (count - 1);
                    scoreUnits += (baseScore + matchingBonus) * 2;
                }
            }

            return scoreUnits;
        }

        public int GetMatchingGroupSize(int row, int column)
        {
            return TryGetMatchingGroupRange(row, column, out var startIndex, out var endIndex)
                ? endIndex - startIndex + 1
                : 0;
        }

        public int CalculateSectionScoreUnitsWithoutMatchingGroup(int row, int column)
        {
            if (!TryGetMatchingGroupRange(row, column, out var startIndex, out var endIndex))
            {
                return CalculateSectionScoreUnits(row);
            }

            var scoreUnits = 0;
            for (var value = 1; value <= 6; value++)
            {
                var count = 0;
                for (var sequenceIndex = 0; sequenceIndex < size; sequenceIndex++)
                {
                    if (sequenceIndex >= startIndex && sequenceIndex <= endIndex)
                    {
                        continue;
                    }

                    var targetColumn = GetColumnForSequenceIndex(sequenceIndex);
                    if (Cells[row, targetColumn] == value)
                    {
                        count++;
                    }
                }

                if (count > 0)
                {
                    var baseScore = value * count;
                    var matchingBonus = value * (count - 1);
                    scoreUnits += (baseScore + matchingBonus) * 2;
                }
            }

            return scoreUnits;
        }

        private bool TryGetMatchingGroupRange(int row, int column, out int startIndex, out int endIndex)
        {
            var value = Cells[row, column];
            if (value == 0 || AttackProtected[row, column])
            {
                startIndex = -1;
                endIndex = -1;
                return false;
            }

            startIndex = GetSequenceIndexForColumn(column);
            while (startIndex > 0)
            {
                var previousColumn = GetColumnForSequenceIndex(startIndex - 1);
                if (Cells[row, previousColumn] != value || AttackProtected[row, previousColumn])
                {
                    break;
                }

                startIndex--;
            }

            endIndex = GetSequenceIndexForColumn(column);
            while (endIndex < size - 1)
            {
                var nextColumn = GetColumnForSequenceIndex(endIndex + 1);
                if (Cells[row, nextColumn] != value || AttackProtected[row, nextColumn])
                {
                    break;
                }

                endIndex++;
            }

            return true;
        }

        private int GroupMatchingDice(int row, int placedValue)
        {
            var values = new int[size];
            var protectedValues = new bool[size];
            var count = 0;

            for (var index = 0; index < size; index++)
            {
                var column = GetColumnForSequenceIndex(index);
                var value = Cells[row, column];
                if (value != 0)
                {
                    values[count] = value;
                    protectedValues[count] = AttackProtected[row, column];
                    count++;
                }
            }

            var placedIndex = -1;
            for (var index = count - 1; index >= 0; index--)
            {
                if (values[index] == placedValue)
                {
                    placedIndex = index;
                    break;
                }
            }

            var finalPlacedIndex = placedIndex;
            if (placedIndex > 0)
            {
                var insertIndex = 0;
                while (insertIndex < placedIndex && values[insertIndex] != placedValue)
                {
                    insertIndex++;
                }

                if (insertIndex < placedIndex)
                {
                    var placed = values[placedIndex];
                    var placedProtected = protectedValues[placedIndex];
                    for (var index = placedIndex; index > insertIndex + 1; index--)
                    {
                        values[index] = values[index - 1];
                        protectedValues[index] = protectedValues[index - 1];
                    }

                    values[insertIndex + 1] = placed;
                    protectedValues[insertIndex + 1] = placedProtected;
                    finalPlacedIndex = insertIndex + 1;
                }
            }

            for (var index = 0; index < size; index++)
            {
                var column = GetColumnForSequenceIndex(index);
                Cells[row, column] = index < count ? values[index] : 0;
                AttackProtected[row, column] = index < count && protectedValues[index];
            }

            return finalPlacedIndex;
        }

        private void CompactRow(int row)
        {
            var values = new int[size];
            var protectedValues = new bool[size];
            var count = 0;

            for (var index = 0; index < size; index++)
            {
                var column = GetColumnForSequenceIndex(index);
                var value = Cells[row, column];
                if (value != 0)
                {
                    values[count] = value;
                    protectedValues[count] = AttackProtected[row, column];
                    count++;
                }
            }

            for (var index = 0; index < size; index++)
            {
                var column = GetColumnForSequenceIndex(index);
                Cells[row, column] = index < count ? values[index] : 0;
                AttackProtected[row, column] = index < count && protectedValues[index];
            }
        }

        private int GetColumnForSequenceIndex(int index)
        {
            return PlayerIndex == 0 ? size - 1 - index : index;
        }

        private int GetSequenceIndexForColumn(int column)
        {
            return PlayerIndex == 0 ? size - 1 - column : column;
        }
    }
}
