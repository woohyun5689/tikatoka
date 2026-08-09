using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tikatooka.PlayModeTests
{
    public sealed class DiceCupInputTests
    {
        [UnityTest]
        public IEnumerator SectionScoreRowsAlignWithBoardRows()
        {
            var controller = Object.FindFirstObjectByType<DiceBoardGameController>();
            if (controller == null)
            {
                controller = new GameObject("Dice Board Game Score Alignment Test Controller")
                    .AddComponent<DiceBoardGameController>();
            }

            yield return null;

            var titleStartButton = GameObject.Find("Title Start Button")?.GetComponent<Button>();
            Assert.That(titleStartButton, Is.Not.Null, "The title start button was not created.");
            titleStartButton.onClick.Invoke();
            yield return null;

            var pveButton = GameObject.Find("PVE Mode Button")?.GetComponent<Button>();
            Assert.That(pveButton, Is.Not.Null, "The PVE mode button was not created.");
            pveButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var scorePanel = GameObject.Find("Section Score Comparison")?.GetComponent<RectTransform>();
            var playerPanel = GameObject.Find("Player 1 Panel")?.transform;
            var playerPanelRect = playerPanel?.GetComponent<RectTransform>();
            var grid = playerPanel?.Find("Grid With Section Labels/Grid")?.GetComponent<RectTransform>();
            Assert.That(scorePanel, Is.Not.Null, "The section score panel was not created.");
            Assert.That(playerPanelRect, Is.Not.Null, "The player panel was not created.");
            Assert.That(grid, Is.Not.Null, "The player grid was not created.");

            for (var section = 0; section < 5; section++)
            {
                var scoreRow = scorePanel.Find($"Section {section + 1}")?.GetComponent<RectTransform>();
                var boardCell = grid.Find($"Cell {section},0")?.GetComponent<RectTransform>();
                Assert.That(scoreRow, Is.Not.Null, $"Score row {section + 1} is missing.");
                Assert.That(boardCell, Is.Not.Null, $"Board row {section + 1} is missing.");

                var scoreCenter = scoreRow.TransformPoint(scoreRow.rect.center);
                var boardCenter = boardCell.TransformPoint(boardCell.rect.center);
                Assert.That(
                    Mathf.Abs(scoreCenter.y - boardCenter.y),
                    Is.LessThan(0.1f),
                    $"Score row {section + 1} does not align with the matching board row. "
                    + $"Score Y: {scoreCenter.y:F2}; board Y: {boardCenter.y:F2}.");
            }

            Object.Destroy(controller.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerDragMovesTheDiceCup()
        {
            var controller = Object.FindFirstObjectByType<DiceBoardGameController>();
            if (controller == null)
            {
                controller = new GameObject("Dice Board Game Test Controller")
                    .AddComponent<DiceBoardGameController>();
            }

            yield return null;

            var titleScreen = GameObject.Find("Title Screen Overlay");
            Assert.That(titleScreen, Is.Not.Null, "The title screen was not created.");
            Assert.That(titleScreen.activeInHierarchy, Is.True, "The title screen should be the initial front screen.");
            Assert.That(GameObject.Find("Roll Button"), Is.Null, "The board controls should stay hidden until title start.");

            var titleStartButton = GameObject.Find("Title Start Button")?.GetComponent<Button>();
            Assert.That(titleStartButton, Is.Not.Null, "The title start button was not created.");
            titleStartButton.onClick.Invoke();
            yield return null;

            var pveButton = GameObject.Find("PVE Mode Button")?.GetComponent<Button>();
            Assert.That(pveButton, Is.Not.Null, "PVE mode button was not created.");
            pveButton.onClick.Invoke();
            yield return null;

            var rollButton = GameObject.Find("Roll Button")?.GetComponent<Button>();
            Assert.That(rollButton, Is.Not.Null, "Roll button was not created.");
            Assert.That(rollButton.interactable, Is.True, "Roll button should be ready for player one.");
            rollButton.onClick.Invoke();
            yield return null;

            var inputSurface = GameObject.Find("Dice Cup Input Surface");
            var cup = GameObject.Find("Dice Cup")?.transform;
            Assert.That(inputSurface, Is.Not.Null, "The full-screen cup input surface is missing.");
            Assert.That(inputSurface.activeInHierarchy, Is.True, "The cup input surface is not active during a roll.");
            Assert.That(cup, Is.Not.Null, "The world-space dice cup is missing.");
            Assert.That(EventSystem.current, Is.Not.Null, "The UI EventSystem is missing.");

            var startPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var dragPosition = startPosition + new Vector2(Screen.width * 0.18f, Screen.height * 0.04f);
            var pointerData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = startPosition
            };
            ExecuteEvents.Execute(
                inputSurface,
                pointerData,
                ExecuteEvents.pointerDownHandler);

            var startCupPosition = cup.localPosition;
            pointerData.delta = dragPosition - startPosition;
            pointerData.position = dragPosition;
            ExecuteEvents.Execute(
                inputSurface,
                pointerData,
                ExecuteEvents.dragHandler);
            yield return null;

            Assert.That(
                Vector3.Distance(startCupPosition, cup.localPosition),
                Is.GreaterThan(0.05f),
                "Pointer drag reached the input surface but did not move the dice cup.");

            ExecuteEvents.Execute(
                inputSurface,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.cancelHandler);
            Object.Destroy(controller.gameObject);
            yield return null;
        }
    }
}
