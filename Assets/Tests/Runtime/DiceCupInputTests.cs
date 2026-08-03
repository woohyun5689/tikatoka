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
        public IEnumerator PointerDragMovesTheDiceCup()
        {
            var controller = Object.FindFirstObjectByType<DiceBoardGameController>();
            if (controller == null)
            {
                controller = new GameObject("Dice Board Game Test Controller")
                    .AddComponent<DiceBoardGameController>();
            }

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
