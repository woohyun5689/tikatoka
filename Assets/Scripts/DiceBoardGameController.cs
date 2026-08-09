using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Tikatooka
{
    public sealed class DiceBoardGameController : MonoBehaviour
    {
        private const int PlayerCount = 2;
        private const int BoardSize = 5;
        private const int AiPlayerIndex = 1;
        private const int AiSmallBonusDieMaxValue = 3;
        private const string BootCoverName = "Tikatooka Boot Cover";
        private const int DiceStageLayer = 31;
        private const int DiceRenderWidth = 960;
        private const int DiceRenderHeight = 540;
        private const float BootFadeDuration = 0.23f;

        private enum MatchMode
        {
            Pvp,
            Pve
        }

        private static readonly Vector3 CupHomePosition = new Vector3(1.10f, 0.45f, 0.30f);
        private static readonly Vector3 DieInCupOffset = new Vector3(-0.06f, -0.17f, 0.02f);
        private static readonly Vector3 CupReleasePosition = new Vector3(0.88f, 0.68f, 0.2f);
        private static readonly Vector3 DieResultPosition = new Vector3(-0.42f, 0.25f, 0.05f);
        private static readonly Vector3 CupPourRotation = new Vector3(0f, -12f, 112f);
        private const float EmergencyBoundsMinX = -2.3f;
        private const float EmergencyBoundsMaxX = 1.65f;
        private const float EmergencyBoundsMinZ = -1.4f;
        private const float EmergencyBoundsMaxZ = 1.4f;
        private const float EmergencyBoundsMinY = -0.8f;
        private const float CupMaxLinearSpeed = 24f;
        private const float CupMaxAngularSpeed = 2400f;
        private const float CupDieCenterRadius = 0.285f;
        private const float CupDragMinX = -1.45f;
        private const float CupDragMaxX = 1.45f;
        private const float CupDragMinZ = -0.8f;
        private const float CupDragMaxZ = 0.8f;
        private const float DieLinearDamping = 0.06f;
        private const float DieAngularDamping = 0.04f;
        private const float PlayableDieMinX = -1.65f;
        private const float PlayableDieMaxX = 0.93f;
        private const float PlayableDieMinZ = -0.72f;
        private const float PlayableDieMaxZ = 0.76f;

        private static readonly WaitForEndOfFrame WaitForPresentedFrame = new WaitForEndOfFrame();
        private static readonly WaitForFixedUpdate WaitForPhysicsStep = new WaitForFixedUpdate();

        private static readonly Color PageColor = new Color32(236, 238, 234, 255);
        private static readonly Color PanelColor = new Color32(250, 250, 247, 255);
        private static readonly Color TextColor = new Color32(36, 40, 46, 255);
        private static readonly Color ActiveBorderColor = new Color32(23, 126, 137, 255);
        private static readonly Color MutedTextColor = new Color32(103, 108, 112, 255);
        private static readonly Color WinColor = new Color32(46, 145, 88, 255);
        private static readonly Color LoseColor = new Color32(196, 75, 65, 255);
        private static readonly Color TieColor = new Color32(112, 116, 120, 255);
        private static readonly Color ScorePanelColor = new Color32(219, 224, 220, 255);
        private static readonly Color ScoreNeutralColor = new Color32(83, 90, 96, 255);
        private static readonly Color ScoreTextColor = new Color32(244, 247, 242, 255);
        private static readonly Color EmptyCellColor = new Color32(218, 211, 195, 255);
        private static readonly Color FrontEmptyCellColor = new Color32(213, 205, 188, 255);
        private static readonly Color ActiveEmptyCellColor = new Color32(204, 238, 232, 255);
        private static readonly Color AttackTargetCellColor = new Color32(236, 92, 78, 255);
        private static readonly Color ButtonColor = new Color32(239, 127, 64, 255);
        private static readonly Color DisabledButtonColor = new Color32(192, 195, 190, 255);
        private static readonly Color ArtDecoCream = new Color32(247, 236, 206, 255);
        private static readonly Color ArtDecoMutedGold = new Color32(198, 181, 133, 255);
        private static readonly Color HudCardLabelColor = new Color32(235, 209, 150, 255);
        private static readonly Color HudInkColor = new Color32(56, 40, 29, 255);
        private static readonly Color HudScoreColor = new Color32(28, 61, 50, 255);

        private static readonly Color[] PlayerAccentColors =
        {
            new Color32(184, 100, 88, 255),
            new Color32(85, 127, 161, 255)
        };

        private static readonly Color[] PlayerLabelColors =
        {
            new Color32(151, 82, 72, 255),
            new Color32(67, 105, 135, 255)
        };

        private static readonly Color[] DieColors =
        {
            new Color32(246, 210, 83, 255),
            new Color32(105, 180, 120, 255),
            new Color32(97, 161, 210, 255),
            new Color32(232, 133, 104, 255),
            new Color32(169, 126, 201, 255),
            new Color32(73, 82, 92, 255)
        };

        private readonly PlayerBoard[] boards = new PlayerBoard[PlayerCount];
        private readonly Image[] sectionRowImages = new Image[BoardSize];
        private readonly Image[,] sectionScoreImages = new Image[PlayerCount, BoardSize];
        private readonly Text[,] sectionScoreTexts = new Text[PlayerCount, BoardSize];
        private readonly Text[] sectionResultTexts = new Text[BoardSize];

        private Font defaultFont;
        private TikatookaGameAudio gameAudio;
        private bool ownsDefaultFont;
        private Sprite scoreBadgeSprite;
        private Sprite diceFaceSprite;
        private Sprite dicePipSprite;
        private Sprite boardPatternSprite;
        private Sprite diceCeramicSprite;
        private Sprite panelParchmentSprite;
        private Sprite playerBoardPanelSprite;
        private Sprite playerOnePlaymatSprite;
        private Sprite playerTwoPlaymatSprite;
        private Sprite gameplayTableauSprite;
        private Sprite playerHeaderSprite;
        private Sprite playerStatusHeaderSprite;
        private Sprite playerProgressRailSprite;
        private Sprite scoreHeaderSprite;
        private Sprite mainBoardBackdropSprite;
        private Sprite scoreTowerSprite;
        private Sprite controlPlaqueSprite;
        private Sprite turnStatusPlaqueSprite;
        private Sprite gameplayActionTraySprite;
        private Sprite matchScoreHudSprite;
        private Sprite drawnDieHudSprite;
        private Sprite buttonPrimarySprite;
        private Sprite buttonSecondarySprite;
        private Sprite buttonPvpSprite;
        private Sprite buttonPveSprite;
        private Sprite cellPlateSprite;
        private Sprite scoreMedallionSprite;
        private Sprite diceFaceArtSprite;
        private Sprite titleBackdropSprite;
        private Sprite titleCrestSprite;
        private Texture2D boardPatternTexture;
        private Texture2D diceCeramicTexture;
        private Texture2D panelParchmentTexture;
        private Texture2D playerBoardPanelTexture;
        private Texture2D playerOnePlaymatTexture;
        private Texture2D playerTwoPlaymatTexture;
        private Texture2D gameplayTableauTexture;
        private Texture2D playerHeaderTexture;
        private Texture2D playerStatusHeaderTexture;
        private Texture2D playerProgressRailTexture;
        private Texture2D scoreHeaderTexture;
        private Texture2D mainBoardBackdropTexture;
        private Texture2D scoreTowerTexture;
        private Texture2D controlPlaqueTexture;
        private Texture2D turnStatusPlaqueTexture;
        private Texture2D gameplayActionTrayTexture;
        private Texture2D matchScoreHudTexture;
        private Texture2D drawnDieHudTexture;
        private Texture2D buttonPrimaryTexture;
        private Texture2D buttonSecondaryTexture;
        private Texture2D buttonPvpTexture;
        private Texture2D buttonPveTexture;
        private Texture2D cellPlateTexture;
        private Texture2D scoreMedallionTexture;
        private Texture2D diceFaceArtTexture;
        private Texture2D worldDieSurfaceTexture;
        private Texture2D walnutTableTexture;
        private Texture2D cupLeatherTexture;
        private Texture2D titleBackdropTexture;
        private Texture2D titleCrestTexture;
        private Image headerImage;
        private Text statusText;
        private Image matchScoreImage;
        private Text matchScoreLabel;
        private Text matchScoreText;
        private Image drawnDieImage;
        private Image drawnDieFaceImage;
        private Image drawnDieProtectionMarker;
        private Text drawnDieLabel;
        private Text drawnDieText;
        private readonly Image[] drawnDiePips = new Image[7];
        private GameObject attackAnimationLayer;
        private GameObject resultBanner;
        private Image resultCardImage;
        private Text resultTitleText;
        private Text resultDetailText;
        private GameObject modeSelectionOverlay;
        private GameObject titleScreenOverlay;
        private GameObject titleHowToPlayOverlay;
        private GameObject diceOverlay;
        private GameObject boardUiRoot;
        private RawImage diceOutputImage;
        private GameObject diceInputSurface;
        private RenderTexture diceRenderTexture;
        private Camera diceCamera;
        private Transform diceStageRoot;

        // GameObject.CreatePrimitive adds these at runtime. Keep explicit type
        // references so WebGL engine-code stripping retains the components.
#pragma warning disable CS0169
        private MeshFilter primitiveMeshFilterReference;
        private MeshRenderer primitiveMeshRendererReference;
        private BoxCollider primitiveBoxColliderReference;
        private SphereCollider primitiveSphereColliderReference;
        private CapsuleCollider primitiveCapsuleColliderReference;
#pragma warning restore CS0169

        private Transform diceCupTransform;
        private Rigidbody diceCupBody;
        private Transform worldDieTransform;
        private Rigidbody worldDieBody;
        private Collider worldDieCollider;
        private PhysicsMaterial worldDiePhysicsMaterial;
        private Mesh worldDieMesh;
        private Mesh cupOuterMesh;
        private Mesh cupInnerMesh;
        private Mesh cupRimMesh;
        private Mesh cupRimInnerInlayMesh;
        private Mesh cupRimOuterInlayMesh;
        private Mesh cupUpperBandMesh;
        private Mesh cupLowerBandMesh;
        private Material cupMaterial;
        private Material cupInnerMaterial;
        private Material dieMaterial;
        private Material pipMaterial;
        private Material tableMaterial;
        private Material feltMaterial;
        private Material trayRimMaterial;
        private Material trayHighlightMaterial;
        private Button rollButton;
        private Text rollButtonText;
        private Button resetButton;
        private Button modeButton;
        private Button pvpModeButton;
        private Button pveModeButton;
        private Button titleStartButton;
        private Button titleHowToPlayButton;
        private Button titleHowToCloseButton;
        private Button resultResetButton;
        private GameObject bootCover;
        private Coroutine bootCoverRoutine;
        private bool bootCoverInputBlocked;
        private Camera[] maskedSceneCameras;
        private int[] originalSceneCameraMasks;
        private int originalVSyncCount;
        private int originalTargetFrameRate;
        private float originalFixedDeltaTime;
        private float originalMaximumDeltaTime;
        private bool runtimeSettingsApplied;

        private int activePlayer;
        private int currentDie;
        private float shakeEnergy;
        private float shakeDistance;
        private float peakShakeDelta;
        private int shakeDirectionChanges;
        private Vector2 lastShakeDirection;
        private Vector2 cupDragStartScreenPosition;
        private Vector2 polledCupPointerPosition;
        private Vector2 lastProcessedCupPointerPosition;
        private Vector3 cupDragStartLocalPosition;
        private Vector3 cupPointerGrabOffset;
        private int lastCupDragInputFrame = -1;
        private bool hasPolledCupPointerPosition;
        private bool mouseCupDragActive;
        private Vector3 cupTargetLocalPosition = CupHomePosition;
        private Quaternion cupTargetLocalRotation = Quaternion.identity;
        private bool hasPendingDie;
        private bool pendingDieCanAttack;
        private bool pendingDieCanPlaceOnOpponent;
        private bool nextRollCanAttack = true;
        private bool nextRollCanPlaceOnOpponent;
        private bool isRolling;
        private bool isWaitingForCupShake;
        private bool isCupDragging;
        private bool cupReleaseStarted;
        private bool isAttackAnimating;
        private bool isAiThinking;
        private bool placementComplete;
        private bool gameStarted;
        private bool worldDieHasTouchedSurface;
        private bool worldDieAttachedToCup;
        private Quaternion dieInCupBaseLocalRotation = Quaternion.identity;
        private MatchMode currentMode = MatchMode.Pve;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureControllerExists()
        {
            if (FindFirstObjectByType<DiceBoardGameController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("Dice Dominion Game");
            controllerObject.AddComponent<DiceBoardGameController>();
        }

        private void Awake()
        {
            originalVSyncCount = QualitySettings.vSyncCount;
            originalTargetFrameRate = Application.targetFrameRate;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            originalMaximumDeltaTime = Time.maximumDeltaTime;
            runtimeSettingsApplied = true;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            Time.fixedDeltaTime = 1f / 60f;
            Time.maximumDeltaTime = 0.05f;
            gameAudio = GetComponent<TikatookaGameAudio>();
            if (gameAudio == null)
            {
                gameAudio = gameObject.AddComponent<TikatookaGameAudio>();
            }

            gameAudio.Initialize();
            bootCover = FindBootCoverInScene();
            PrepareBootCover();
            defaultFont = LoadInterfaceFont(out ownsDefaultFont);
            scoreBadgeSprite = CreateCircleSprite(96);
            diceFaceSprite = CreateRoundedRectSprite(96, 13);
            dicePipSprite = CreateCircleSprite(32);
            LoadGeneratedArt();
            BuildInterface();
            StartNewGame();
            ShowTitleScreen();
            Canvas.ForceUpdateCanvases();
            bootCoverRoutine = StartCoroutine(HideBootCoverRoutine());
        }

        private GameObject FindBootCoverInScene()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == BootCoverName)
                {
                    return root;
                }
            }

            return GameObject.Find(BootCoverName);
        }

        private void PrepareBootCover()
        {
            bootCoverInputBlocked = bootCover != null;
            if (bootCover == null)
            {
                return;
            }

            bootCover.SetActive(true);
            var coverRect = bootCover.GetComponent<RectTransform>();
            if (coverRect != null)
            {
                coverRect.localScale = Vector3.one;
                coverRect.localRotation = Quaternion.identity;
            }

            var coverGroup = bootCover.GetComponent<CanvasGroup>();
            if (coverGroup != null)
            {
                coverGroup.alpha = 1f;
                coverGroup.blocksRaycasts = true;
            }

            if (bootCover.GetComponent<GraphicRaycaster>() == null)
            {
                bootCover.AddComponent<GraphicRaycaster>();
            }

            var coverGraphics = bootCover.GetComponentsInChildren<Graphic>(true);
            for (var index = 0; index < coverGraphics.Length; index++)
            {
                coverGraphics[index].raycastTarget = true;
            }
        }

        private IEnumerator HideBootCoverRoutine()
        {
            if (bootCover == null)
            {
                bootCoverInputBlocked = false;
                bootCoverRoutine = null;
                yield break;
            }

            var coverGroup = bootCover.GetComponent<CanvasGroup>();
            var restoreDiceOverlay = diceOverlay != null && diceOverlay.activeSelf;
            if (diceOverlay != null)
            {
                diceOverlay.SetActive(true);
            }

            var restoreDiceStage = diceStageRoot != null && diceStageRoot.gameObject.activeSelf;
            if (diceStageRoot != null)
            {
                diceStageRoot.gameObject.SetActive(true);
            }

            if (diceCamera != null)
            {
                diceCamera.enabled = true;
            }

            yield return WaitForPresentedFrame;
            yield return WaitForPresentedFrame;

            if (diceCamera != null)
            {
                diceCamera.enabled = restoreDiceOverlay;
            }

            if (diceOverlay != null && !restoreDiceOverlay)
            {
                diceOverlay.SetActive(false);
            }

            if (diceStageRoot != null && !restoreDiceStage)
            {
                diceStageRoot.gameObject.SetActive(false);
            }

            Canvas.ForceUpdateCanvases();
            var fadeElapsed = 0f;
            while (bootCover != null && fadeElapsed < BootFadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                if (coverGroup != null)
                {
                    var progress = Mathf.Clamp01(fadeElapsed / BootFadeDuration);
                    coverGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
                }

                yield return WaitForPresentedFrame;
            }

            if (bootCover != null)
            {
                if (coverGroup != null)
                {
                    coverGroup.alpha = 0f;
                    coverGroup.blocksRaycasts = false;
                }

                bootCover.SetActive(false);
            }

            bootCoverInputBlocked = false;
            bootCoverRoutine = null;
            FocusActiveFrontScreen();
        }

        private void StopGameplayCoroutines()
        {
            gameAudio?.StopCupShake();
            var finishBootCover = bootCoverRoutine != null;
            StopAllCoroutines();
            bootCoverRoutine = null;
            if (finishBootCover)
            {
                CompleteBootCoverImmediately();
            }
        }

        private void CompleteBootCoverImmediately()
        {
            if (bootCover == null)
            {
                bootCoverInputBlocked = false;
                return;
            }

            var coverGroup = bootCover.GetComponent<CanvasGroup>();
            if (coverGroup != null)
            {
                coverGroup.alpha = 0f;
                coverGroup.blocksRaycasts = false;
            }

            bootCover.SetActive(false);
            bootCoverInputBlocked = false;
            FocusActiveFrontScreen();
        }

        private void FixedUpdate()
        {
            if (diceCupBody == null || diceStageRoot == null || !diceStageRoot.gameObject.activeInHierarchy)
            {
                return;
            }

            var targetPosition = diceStageRoot.TransformPoint(cupTargetLocalPosition);
            var targetRotation = diceStageRoot.rotation * cupTargetLocalRotation;
            var cupAtTarget = (diceCupBody.position - targetPosition).sqrMagnitude < 0.000001f
                && Quaternion.Angle(diceCupBody.rotation, targetRotation) < 0.05f;
            if (!cupAtTarget)
            {
                var nextPosition = Vector3.MoveTowards(
                    diceCupBody.position,
                    targetPosition,
                    CupMaxLinearSpeed * Time.fixedDeltaTime);
                var nextRotation = Quaternion.RotateTowards(
                    diceCupBody.rotation,
                    targetRotation,
                    CupMaxAngularSpeed * Time.fixedDeltaTime);
                diceCupBody.MovePosition(nextPosition);
                diceCupBody.MoveRotation(nextRotation);
            }

            UpdateAttachedWorldDiePose();
        }

        private void UpdateAttachedWorldDiePose()
        {
            if (!worldDieAttachedToCup
                || worldDieBody == null
                || !worldDieBody.isKinematic
                || diceCupTransform == null)
            {
                return;
            }

            var shakeAmount = isCupDragging && isWaitingForCupShake
                ? Mathf.Clamp01(shakeEnergy / 1.25f)
                : 0f;
            var phase = Time.fixedTime * 22f;
            var localOffset = DieInCupOffset + new Vector3(
                Mathf.Sin(phase) * 0.065f * shakeAmount,
                Mathf.Abs(Mathf.Sin(phase * 1.37f)) * 0.035f * shakeAmount,
                Mathf.Cos(phase * 0.83f) * 0.055f * shakeAmount);
            var localWobble = Quaternion.Euler(
                Mathf.Sin(phase * 0.91f) * 10f * shakeAmount,
                Mathf.Cos(phase * 1.17f) * 12f * shakeAmount,
                Mathf.Sin(phase * 1.31f) * 9f * shakeAmount);
            worldDieBody.MovePosition(diceCupTransform.TransformPoint(localOffset));
            worldDieBody.MoveRotation(diceCupTransform.rotation * localWobble * dieInCupBaseLocalRotation);
        }

        private void Update()
        {
            if (titleHowToPlayOverlay != null && titleHowToPlayOverlay.activeInHierarchy)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseTitleHowToPlay();
                }

                return;
            }

            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted || IsAiTurnActive())
            {
                return;
            }

            UpdateMouseCupShake();
            if (cupReleaseStarted)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    CancelCupShake();
                    return;
                }

                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    EndCupShake();
                    return;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                CancelCupShake();
            }
            else if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                EndCupShake();
            }
        }

        private void UpdateMouseCupShake()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var pointerPosition = mouse.position.ReadValue();
            var leftButtonPressed = mouse.leftButton.isPressed;
            var pointerInsideSurface = diceInputSurface != null
                && diceInputSurface.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(
                    diceInputSurface.GetComponent<RectTransform>(),
                    pointerPosition,
                    null);
            if (leftButtonPressed
                && !isCupDragging
                && pointerInsideSurface)
            {
                mouseCupDragActive = true;
                BeginCupShake(pointerPosition);
            }

            if (leftButtonPressed && isCupDragging && mouseCupDragActive)
            {
                var pointerDelta = hasPolledCupPointerPosition
                    ? pointerPosition - polledCupPointerPosition
                    : mouse.delta.ReadValue();
                if (pointerDelta.sqrMagnitude <= 0.0001f)
                {
                    pointerDelta = mouse.delta.ReadValue();
                }

                polledCupPointerPosition = pointerPosition;
                hasPolledCupPointerPosition = true;
                if (lastCupDragInputFrame != Time.frameCount
                    && pointerDelta.sqrMagnitude > 0.0001f)
                {
                    DragCupShake(pointerPosition, pointerDelta);
                }
            }

            if (mouseCupDragActive && !leftButtonPressed)
            {
                mouseCupDragActive = false;
                hasPolledCupPointerPosition = false;
                if (isCupDragging)
                {
                    EndCupShake();
                }
            }
        }

        private static Font LoadInterfaceFont(out bool ownsFont)
        {
            ownsFont = false;

            // WebGL cannot use the player's operating-system fonts. Keep the Korean
            // interface font in Resources so every platform receives the same glyphs.
            var packagedFont = Resources.Load<Font>("Fonts/NotoSansKR");
            if (packagedFont != null)
            {
                return packagedFont;
            }

            var preferredFonts = new[]
            {
                "Malgun Gothic",
                "Apple SD Gothic Neo",
                "Noto Sans CJK KR",
                "Noto Sans KR",
                "NanumGothic",
                "Segoe UI",
                "Arial"
            };

            var preferred = TryCreateHangulFont(preferredFonts);
            if (preferred != null)
            {
                ownsFont = true;
                return preferred;
            }

            var installed = TryCreateHangulFont(Font.GetOSInstalledFontNames());
            if (installed != null)
            {
                ownsFont = true;
                return installed;
            }

            var builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtInFont != null ? builtInFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Font TryCreateHangulFont(string[] fontNames)
        {
            if (fontNames == null || fontNames.Length == 0)
            {
                return null;
            }

            for (var index = 0; index < fontNames.Length; index++)
            {
                var candidate = Font.CreateDynamicFontFromOSFont(fontNames[index], 32);
                if (candidate == null)
                {
                    continue;
                }

                candidate.RequestCharactersInTexture("한글", 32);
                if (candidate.HasCharacter('한') && candidate.HasCharacter('글'))
                {
                    return candidate;
                }

                Destroy(candidate);
            }

            return null;
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvasObject = CreateUiObject("Canvas", transform);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            boardUiRoot = CreateUiObject("Root", canvasObject.transform);
            var rootRect = boardUiRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var rootImage = boardUiRoot.AddComponent<Image>();
            if (mainBoardBackdropSprite != null)
            {
                rootImage.sprite = mainBoardBackdropSprite;
                rootImage.type = Image.Type.Simple;
                rootImage.preserveAspect = false;
                rootImage.color = Color.white;
            }
            else if (boardPatternSprite != null)
            {
                rootImage.sprite = boardPatternSprite;
                rootImage.type = Image.Type.Tiled;
                rootImage.color = new Color(0.82f, 0.88f, 0.84f, 1f);
            }
            else
            {
                rootImage.color = PageColor;
            }
            rootImage.raycastTarget = false;

            var rootLayout = boardUiRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(12, 12, 12, 12);
            rootLayout.spacing = 12;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;

            CreateHeader(boardUiRoot.transform);
            CreateBoards(boardUiRoot.transform);
            CreateControls(boardUiRoot.transform);
            CreateDiceOverlay(canvasObject.transform);
            CreateAttackAnimationLayer(canvasObject.transform);
            CreateResultBanner(canvasObject.transform);
            CreateModeSelectionOverlay(canvasObject.transform);
            CreateTitleScreen(canvasObject.transform);
            Canvas.ForceUpdateCanvases();
        }

        private void CreateHeader(Transform parent)
        {
            var header = CreateUiObject("Header", parent);
            headerImage = header.AddComponent<Image>();
            if (controlPlaqueSprite != null)
            {
                headerImage.sprite = controlPlaqueSprite;
                headerImage.type = Image.Type.Sliced;
                headerImage.color = Color.white;
            }
            else
            {
                headerImage.sprite = diceFaceSprite;
                headerImage.color = new Color32(247, 249, 246, 255);
            }
            headerImage.raycastTarget = false;
            var headerShadow = header.AddComponent<Shadow>();
            headerShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            headerShadow.effectDistance = new Vector2(2f, -2f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 22;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.padding = new RectOffset(24, 24, 0, 0);
            AddLayout(header, -1, 72);

            var statusPlaque = CreateUiObject("Turn Status Plaque", header.transform);
            var statusPlaqueImage = statusPlaque.AddComponent<Image>();
            if (turnStatusPlaqueSprite != null)
            {
                statusPlaqueImage.sprite = turnStatusPlaqueSprite;
                // This artwork was generated at the compact status-label proportion.
                // Rendering it as one plaque avoids expanding the decorative end caps
                // across the entire header.
                statusPlaqueImage.type = Image.Type.Simple;
                statusPlaqueImage.preserveAspect = false;
                statusPlaqueImage.color = Color.white;
            }
            else
            {
                statusPlaqueImage.sprite = controlPlaqueSprite ?? diceFaceSprite;
                statusPlaqueImage.type = controlPlaqueSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                statusPlaqueImage.color = Color.white;
            }

            statusPlaqueImage.raycastTarget = false;
            var statusPlaqueShadow = statusPlaque.AddComponent<Shadow>();
            statusPlaqueShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            statusPlaqueShadow.effectDistance = new Vector2(1.5f, -1.5f);
            AddLayout(statusPlaque, 360, 64);

            statusText = CreateText("Status", statusPlaque.transform, "플레이어 1 차례", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            ApplyPlayerHeaderTypography(statusText, ArtDecoCream);
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(40f, 2f);
            statusRect.offsetMax = new Vector2(-40f, -2f);

            // Keep the generated status plaque compact at the left of the HUD while
            // reserving the remaining header width for the two fixed HUD cards.
            var statusSpacer = CreateUiObject("Header Status Spacer", header.transform);
            AddLayout(statusSpacer, -1, 64, flexibleWidth: 1);

            CreateMatchScoreDisplay(header.transform);
            CreateDrawnDieDisplay(header.transform);
        }

        private void CreateMatchScoreDisplay(Transform parent)
        {
            var display = CreateUiObject("Match Score Display", parent);
            matchScoreImage = display.AddComponent<Image>();
            if (matchScoreHudSprite != null)
            {
                matchScoreImage.sprite = matchScoreHudSprite;
                // This card is exported at the final HUD aspect ratio, so keeping it
                // simple preserves the generated Art Deco frame without 9-slice warping.
                matchScoreImage.type = Image.Type.Simple;
                matchScoreImage.preserveAspect = false;
                matchScoreImage.color = Color.white;
            }
            else
            {
                matchScoreImage.sprite = panelParchmentSprite != null ? panelParchmentSprite : diceFaceSprite;
                if (panelParchmentSprite != null)
                {
                    matchScoreImage.type = Image.Type.Tiled;
                }

                matchScoreImage.color = ScorePanelColor;
            }
            var scoreShadow = display.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            scoreShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var scoreOutline = display.AddComponent<Outline>();
            scoreOutline.effectColor = new Color(0f, 0f, 0f, 0.08f);
            scoreOutline.effectDistance = new Vector2(1f, -1f);
            AddLayout(display, 154, 72);

            var headerClip = CreateHudClipRegion(
                "Match Score Header Clip",
                display.transform,
                new Vector2(0.18f, 0.62f),
                new Vector2(0.82f, 0.92f));
            matchScoreLabel = CreateText("Match Score Label", headerClip.transform, "구간 승리", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            matchScoreLabel.color = matchScoreHudSprite != null ? HudCardLabelColor : HudInkColor;
            matchScoreLabel.resizeTextForBestFit = false;
            matchScoreLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            matchScoreLabel.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyTextShadow(
                matchScoreLabel,
                matchScoreHudSprite != null ? new Color(0f, 0f, 0f, 0.64f) : new Color(1f, 1f, 1f, 0.42f),
                new Vector2(1f, -1f));
            StretchHudContent(matchScoreLabel.rectTransform, 2f, 1f);

            var valueClip = CreateHudClipRegion(
                "Match Score Value Clip",
                display.transform,
                new Vector2(0.15f, 0.08f),
                new Vector2(0.85f, 0.59f));
            matchScoreText = CreateText("Match Score Value", valueClip.transform, "0 : 0", 31, FontStyle.Bold, TextAnchor.MiddleCenter);
            matchScoreText.color = matchScoreHudSprite != null ? ArtDecoCream : HudScoreColor;
            matchScoreText.resizeTextForBestFit = false;
            matchScoreText.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyTextShadow(
                matchScoreText,
                matchScoreHudSprite != null ? new Color(0f, 0f, 0f, 0.72f) : new Color(1f, 1f, 1f, 0.38f),
                new Vector2(1f, -1f));
            StretchHudContent(matchScoreText.rectTransform, 2f, 1f);
        }

        private void CreateDrawnDieDisplay(Transform parent)
        {
            var display = CreateUiObject("Drawn Die Display", parent);
            drawnDieImage = display.AddComponent<Image>();
            if (drawnDieHudSprite != null)
            {
                drawnDieImage.sprite = drawnDieHudSprite;
                // The die recess is part of the generated art and must remain square.
                drawnDieImage.type = Image.Type.Simple;
                drawnDieImage.preserveAspect = false;
                drawnDieImage.color = Color.white;
            }
            else
            {
                drawnDieImage.sprite = panelParchmentSprite != null ? panelParchmentSprite : diceFaceSprite;
                if (panelParchmentSprite != null)
                {
                    drawnDieImage.type = Image.Type.Tiled;
                }

                drawnDieImage.color = PanelColor;
            }
            var displayShadow = display.AddComponent<Shadow>();
            displayShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            displayShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var displayOutline = display.AddComponent<Outline>();
            displayOutline.effectColor = new Color(0f, 0f, 0f, 0.08f);
            displayOutline.effectDistance = new Vector2(1f, -1f);
            AddLayout(display, 150, 72);

            var headerClip = CreateHudClipRegion(
                "Drawn Die Header Clip",
                display.transform,
                new Vector2(0.18f, 0.62f),
                new Vector2(0.82f, 0.92f));
            drawnDieLabel = CreateText("Drawn Die Label", headerClip.transform, "주사위", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            drawnDieLabel.color = drawnDieHudSprite != null ? HudCardLabelColor : HudInkColor;
            drawnDieLabel.resizeTextForBestFit = false;
            drawnDieLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            drawnDieLabel.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyTextShadow(
                drawnDieLabel,
                drawnDieHudSprite != null ? new Color(0f, 0f, 0f, 0.64f) : new Color(1f, 1f, 1f, 0.42f),
                new Vector2(1f, -1f));
            StretchHudContent(drawnDieLabel.rectTransform, 2f, 1f);

            var bodyClip = CreateHudClipRegion(
                "Drawn Die Body Clip",
                display.transform,
                new Vector2(0.35f, 0.09f),
                new Vector2(0.65f, 0.59f));
            var faceObject = CreateUiObject("Drawn Die Face", bodyClip.transform);
            drawnDieFaceImage = faceObject.AddComponent<Image>();
            drawnDieFaceImage.sprite = diceFaceArtSprite != null ? diceFaceArtSprite : diceFaceSprite;
            drawnDieFaceImage.type = Image.Type.Simple;
            drawnDieFaceImage.preserveAspect = true;
            drawnDieFaceImage.color = GetDiceFaceArtTint(PanelColor);
            drawnDieFaceImage.raycastTarget = false;
            var faceShadow = faceObject.AddComponent<Shadow>();
            faceShadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
            faceShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var faceOutline = faceObject.AddComponent<Outline>();
            faceOutline.effectColor = new Color(0f, 0f, 0f, 0.14f);
            faceOutline.effectDistance = new Vector2(1f, -1f);
            var faceRect = faceObject.GetComponent<RectTransform>();
            faceRect.anchorMin = new Vector2(0.5f, 0.5f);
            faceRect.anchorMax = new Vector2(0.5f, 0.5f);
            faceRect.pivot = new Vector2(0.5f, 0.5f);
            faceRect.sizeDelta = new Vector2(30f, 30f);
            faceRect.anchoredPosition = Vector2.zero;

            if (diceFaceArtSprite == null)
            {
                CreateDiceCeramicInlay(faceObject.transform, 0.16f, 0.32f);
            }
            CreateDrawnDiePips(faceObject.transform);

            drawnDieText = CreateText("Drawn Die Value", faceObject.transform, "-", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            drawnDieText.color = HudInkColor;
            drawnDieText.resizeTextForBestFit = false;
            drawnDieText.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyTextShadow(drawnDieText, new Color(1f, 1f, 1f, 0.34f), new Vector2(1f, -1f));
            var textRect = drawnDieText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void CreateDrawnDiePips(Transform face)
        {
            var positions = new[]
            {
                Vector2.zero,
                new Vector2(-8.5f, 8.5f),
                new Vector2(8.5f, 8.5f),
                new Vector2(-8.5f, 0f),
                new Vector2(8.5f, 0f),
                new Vector2(-8.5f, -8.5f),
                new Vector2(8.5f, -8.5f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                var pipObject = CreateUiObject($"Drawn Pip {index}", face);
                var pipImage = pipObject.AddComponent<Image>();
                pipImage.sprite = dicePipSprite;
                pipImage.color = TextColor;
                pipImage.raycastTarget = false;
                var pipShadow = pipObject.AddComponent<Shadow>();
                pipShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                pipShadow.effectDistance = new Vector2(0.7f, -0.7f);

                var pipRect = pipObject.GetComponent<RectTransform>();
                pipRect.anchorMin = new Vector2(0.5f, 0.5f);
                pipRect.anchorMax = new Vector2(0.5f, 0.5f);
                pipRect.sizeDelta = new Vector2(6.5f, 6.5f);
                pipRect.anchoredPosition = positions[index];
                pipObject.SetActive(false);
                drawnDiePips[index] = pipImage;
            }

            var markerObject = CreateUiObject("Drawn Protection Marker", face);
            drawnDieProtectionMarker = markerObject.AddComponent<Image>();
            drawnDieProtectionMarker.sprite = scoreBadgeSprite;
            drawnDieProtectionMarker.color = ButtonColor;
            drawnDieProtectionMarker.raycastTarget = false;
            var markerOutline = markerObject.AddComponent<Outline>();
            markerOutline.effectColor = Color.white;
            markerOutline.effectDistance = new Vector2(1f, -1f);

            var markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(1f, 1f);
            markerRect.anchorMax = new Vector2(1f, 1f);
            markerRect.sizeDelta = new Vector2(11f, 11f);
            markerRect.anchoredPosition = new Vector2(-7f, -7f);
            var markerSymbol = CreateText("Protection Symbol", markerObject.transform, "!", 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            markerSymbol.color = Color.white;
            markerSymbol.raycastTarget = false;
            var markerSymbolRect = markerSymbol.GetComponent<RectTransform>();
            markerSymbolRect.anchorMin = Vector2.zero;
            markerSymbolRect.anchorMax = Vector2.one;
            markerSymbolRect.offsetMin = Vector2.zero;
            markerSymbolRect.offsetMax = Vector2.zero;
            markerObject.SetActive(false);
        }

        private void CreateDiceOverlay(Transform parent)
        {
            diceOverlay = CreateUiObject("Dice Roll Overlay", parent);
            var overlayRect = diceOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            diceOverlay.transform.SetAsLastSibling();

            var backdrop = diceOverlay.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.55f);
            backdrop.raycastTarget = false;

            var stageObject = CreateUiObject("Dice Cup Full Stage", diceOverlay.transform);
            var stageRect = stageObject.GetComponent<RectTransform>();
            stageRect.anchorMin = Vector2.zero;
            stageRect.anchorMax = Vector2.one;
            stageRect.offsetMin = Vector2.zero;
            stageRect.offsetMax = Vector2.zero;

            var rawImage = stageObject.AddComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            diceOutputImage = rawImage;
            CreateDiceCupStage(rawImage);
            CreateDiceShakeHint(diceOverlay.transform);

            // Keep pointer capture separate from the rendered stage. As the final
            // sibling this transparent surface cannot be hidden behind the
            // backdrop, RenderTexture image, or the visual hint panel.
            diceInputSurface = CreateUiObject("Dice Cup Input Surface", diceOverlay.transform);
            var inputRect = diceInputSurface.GetComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            var inputImage = diceInputSurface.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0f);
            inputImage.raycastTarget = true;
            var dragSurface = diceInputSurface.AddComponent<DiceCupDragSurface>();
            dragSurface.Initialize(this);

            SetDiceOverlayVisible(false);
        }

        private void CreateDiceShakeHint(Transform parent)
        {
            var hintPanel = CreateUiObject("Dice Shake Hint Panel", parent);
            var hintImage = hintPanel.AddComponent<Image>();
            if (controlPlaqueSprite != null)
            {
                hintImage.sprite = controlPlaqueSprite;
                hintImage.type = Image.Type.Sliced;
                hintImage.color = Color.white;
            }
            else
            {
                hintImage.sprite = diceFaceSprite;
                hintImage.color = new Color(0.035f, 0.055f, 0.06f, 0.86f);
            }
            hintImage.raycastTarget = false;

            var hintRect = hintPanel.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(1160f, 68f);
            hintRect.anchoredPosition = new Vector2(0f, 34f);

            var hintText = CreateText(
                "Dice Shake Hint",
                hintPanel.transform,
                "마우스로 컵을 흔든 뒤 놓으세요  •  Enter/Space: 컵 굴리기  •  Esc: 취소",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            hintText.color = controlPlaqueSprite != null ? ArtDecoCream : Color.white;
            ApplyTextShadow(hintText, new Color32(28, 10, 7, 220), new Vector2(1f, -1f));
            hintText.raycastTarget = false;
            var hintTextRect = hintText.GetComponent<RectTransform>();
            hintTextRect.anchorMin = Vector2.zero;
            hintTextRect.anchorMax = Vector2.one;
            hintTextRect.offsetMin = new Vector2(18f, 8f);
            hintTextRect.offsetMax = new Vector2(-18f, -8f);
        }

        private void CreateResultBanner(Transform parent)
        {
            resultBanner = CreateUiObject("Game Result Banner", parent);
            var bannerRect = resultBanner.GetComponent<RectTransform>();
            bannerRect.anchorMin = Vector2.zero;
            bannerRect.anchorMax = Vector2.one;
            bannerRect.offsetMin = Vector2.zero;
            bannerRect.offsetMax = Vector2.zero;
            resultBanner.transform.SetAsLastSibling();

            var backdrop = resultBanner.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.36f);
            backdrop.raycastTarget = true;

            var card = CreateUiObject("Result Card", resultBanner.transform);
            resultCardImage = card.AddComponent<Image>();
            if (controlPlaqueSprite != null)
            {
                resultCardImage.sprite = controlPlaqueSprite;
                resultCardImage.type = Image.Type.Sliced;
                resultCardImage.color = Color.white;
            }
            else
            {
                resultCardImage.sprite = diceFaceSprite;
                resultCardImage.color = PanelColor;
            }
            var cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            cardShadow.effectDistance = new Vector2(5f, -5f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0f, 0f, 0f, 0.12f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 224f);
            cardRect.anchoredPosition = Vector2.zero;

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(28, 28, 24, 24);
            cardLayout.spacing = 10;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = true;

            resultTitleText = CreateText("Result Title", card.transform, "게임 종료", 38, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (controlPlaqueSprite != null)
            {
                resultTitleText.color = ArtDecoCream;
            }
            AddLayout(resultTitleText.gameObject, -1, 54);

            resultDetailText = CreateText("Result Detail", card.transform, "구간 승수 0 : 0", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            resultDetailText.color = controlPlaqueSprite != null ? ArtDecoMutedGold : MutedTextColor;
            AddLayout(resultDetailText.gameObject, -1, 38);

            resultResetButton = CreateButton("Result Reset Button", card.transform, "새 게임", 25);
            ApplyGeneratedButtonSkin(resultResetButton, buttonSecondarySprite, new Color32(65, 73, 82, 255));
            resultResetButton.onClick.AddListener(StartNewGame);
            AddControlButtonDepth(resultResetButton.gameObject);
            var resultResetText = resultResetButton.GetComponentInChildren<Text>();
            if (resultResetText != null)
            {
                resultResetText.color = Color.white;
            }

            AddLayout(resultResetButton.gameObject, 180, 52);
            resultBanner.SetActive(false);
        }

        private void CreateTitleScreen(Transform parent)
        {
            titleScreenOverlay = CreateUiObject("Title Screen Overlay", parent);
            var overlayRect = titleScreenOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            titleScreenOverlay.transform.SetAsLastSibling();

            var backdrop = titleScreenOverlay.AddComponent<Image>();
            if (titleBackdropSprite != null)
            {
                backdrop.sprite = titleBackdropSprite;
                backdrop.type = Image.Type.Simple;
                backdrop.preserveAspect = false;
                backdrop.color = Color.white;
            }
            else if (mainBoardBackdropSprite != null)
            {
                backdrop.sprite = mainBoardBackdropSprite;
                backdrop.type = Image.Type.Simple;
                backdrop.preserveAspect = false;
                backdrop.color = Color.white;
            }
            else
            {
                backdrop.color = new Color32(13, 39, 34, 255);
            }
            backdrop.raycastTarget = true;

            var vignette = CreateUiObject("Title Center Vignette", titleScreenOverlay.transform);
            var vignetteImage = vignette.AddComponent<Image>();
            vignetteImage.color = new Color(0f, 0.035f, 0.022f, 0.2f);
            vignetteImage.raycastTarget = false;
            var vignetteRect = vignette.GetComponent<RectTransform>();
            vignetteRect.anchorMin = new Vector2(0.18f, 0.08f);
            vignetteRect.anchorMax = new Vector2(0.82f, 0.92f);
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;

            var crestObject = CreateUiObject("Title Crest", titleScreenOverlay.transform);
            var crestImage = crestObject.AddComponent<Image>();
            crestImage.sprite = titleCrestSprite ?? diceFaceArtSprite ?? diceFaceSprite;
            crestImage.type = Image.Type.Simple;
            crestImage.preserveAspect = true;
            crestImage.color = Color.white;
            crestImage.raycastTarget = false;
            var crestShadow = crestObject.AddComponent<Shadow>();
            crestShadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            crestShadow.effectDistance = new Vector2(0f, -7f);
            var crestRect = crestObject.GetComponent<RectTransform>();
            crestRect.anchorMin = new Vector2(0.5f, 0.5f);
            crestRect.anchorMax = new Vector2(0.5f, 0.5f);
            crestRect.pivot = new Vector2(0.5f, 0.5f);
            crestRect.sizeDelta = new Vector2(192f, 192f);
            crestRect.anchoredPosition = new Vector2(0f, 218f);

            var wordmark = CreateText("Title Logo Wordmark", titleScreenOverlay.transform, "DICE DOMINION", 96, FontStyle.Bold, TextAnchor.MiddleCenter);
            wordmark.color = ArtDecoCream;
            wordmark.resizeTextMinSize = 52;
            wordmark.resizeTextMaxSize = 96;
            wordmark.raycastTarget = false;
            var wordmarkShadow = wordmark.gameObject.AddComponent<Shadow>();
            wordmarkShadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
            wordmarkShadow.effectDistance = new Vector2(0f, -6f);
            var wordmarkOutline = wordmark.gameObject.AddComponent<Outline>();
            wordmarkOutline.effectColor = new Color(0.43f, 0.26f, 0.07f, 0.96f);
            wordmarkOutline.effectDistance = new Vector2(2f, -2f);
            var wordmarkRect = wordmark.GetComponent<RectTransform>();
            wordmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            wordmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            wordmarkRect.pivot = new Vector2(0.5f, 0.5f);
            wordmarkRect.sizeDelta = new Vector2(1040f, 112f);
            wordmarkRect.anchoredPosition = new Vector2(0f, 82f);

            var divider = CreateUiObject("Title Gold Divider", titleScreenOverlay.transform);
            var dividerImage = divider.AddComponent<Image>();
            dividerImage.color = ArtDecoMutedGold;
            dividerImage.raycastTarget = false;
            var dividerRect = divider.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
            dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            dividerRect.sizeDelta = new Vector2(270f, 3f);
            dividerRect.anchoredPosition = new Vector2(0f, 21f);

            var subtitle = CreateText("Title Subtitle", titleScreenOverlay.transform, "DICE BOARD GAME", 27, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitle.color = ArtDecoMutedGold;
            subtitle.resizeTextMinSize = 16;
            subtitle.raycastTarget = false;
            var subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(620f, 42f);
            subtitleRect.anchoredPosition = new Vector2(0f, -17f);

            titleStartButton = CreateButton("Title Start Button", titleScreenOverlay.transform, "게임 시작", 30);
            ApplyGeneratedButtonSkin(titleStartButton, buttonPrimarySprite, new Color32(191, 123, 39, 255));
            titleStartButton.onClick.AddListener(OpenModeSelectionFromTitle);
            AddControlButtonDepth(titleStartButton.gameObject);
            var startText = titleStartButton.GetComponentInChildren<Text>();
            if (startText != null)
            {
                startText.color = ArtDecoCream;
                ApplyTextShadow(startText, new Color32(24, 10, 8, 220), new Vector2(1f, -1f));
            }
            var startRect = titleStartButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.5f);
            startRect.anchorMax = new Vector2(0.5f, 0.5f);
            startRect.pivot = new Vector2(0.5f, 0.5f);
            startRect.sizeDelta = new Vector2(320f, 78f);
            startRect.anchoredPosition = new Vector2(0f, -138f);

            titleHowToPlayButton = CreateButton("Title How To Play Button", titleScreenOverlay.transform, "게임 방법", 28);
            ApplyGeneratedButtonSkin(titleHowToPlayButton, buttonPrimarySprite, new Color32(191, 123, 39, 255));
            titleHowToPlayButton.onClick.AddListener(OpenTitleHowToPlay);
            AddControlButtonDepth(titleHowToPlayButton.gameObject);
            var howToPlayButtonText = titleHowToPlayButton.GetComponentInChildren<Text>();
            if (howToPlayButtonText != null)
            {
                howToPlayButtonText.color = ArtDecoCream;
                ApplyTextShadow(howToPlayButtonText, new Color32(24, 10, 8, 220), new Vector2(1f, -1f));
            }

            var howToPlayButtonRect = titleHowToPlayButton.GetComponent<RectTransform>();
            howToPlayButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
            howToPlayButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
            howToPlayButtonRect.pivot = new Vector2(0.5f, 0.5f);
            howToPlayButtonRect.sizeDelta = new Vector2(320f, 70f);
            howToPlayButtonRect.anchoredPosition = new Vector2(0f, -228f);

            var startHint = CreateText("Title Start Hint", titleScreenOverlay.transform, "Enter 또는 버튼을 눌러 시작", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            startHint.color = new Color(0.88f, 0.81f, 0.62f, 0.9f);
            startHint.resizeTextMinSize = 14;
            startHint.raycastTarget = false;
            var startHintRect = startHint.GetComponent<RectTransform>();
            startHintRect.anchorMin = new Vector2(0.5f, 0.5f);
            startHintRect.anchorMax = new Vector2(0.5f, 0.5f);
            startHintRect.pivot = new Vector2(0.5f, 0.5f);
            startHintRect.sizeDelta = new Vector2(460f, 32f);
            startHintRect.anchoredPosition = new Vector2(0f, -294f);

            CreateTitleHowToPlayOverlay(titleScreenOverlay.transform);

            titleScreenOverlay.SetActive(false);
        }

        private void CreateTitleHowToPlayOverlay(Transform parent)
        {
            titleHowToPlayOverlay = CreateUiObject("Title How To Play Overlay", parent);
            var overlayRect = titleHowToPlayOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var backdrop = titleHowToPlayOverlay.AddComponent<Image>();
            backdrop.color = new Color(0f, 0.02f, 0.014f, 0.66f);
            backdrop.raycastTarget = true;

            var card = CreateUiObject("How To Play Card", titleHowToPlayOverlay.transform);
            var cardImage = card.AddComponent<Image>();
            if (controlPlaqueSprite != null)
            {
                cardImage.sprite = controlPlaqueSprite;
                cardImage.type = Image.Type.Sliced;
                cardImage.color = Color.white;
            }
            else
            {
                cardImage.sprite = diceFaceSprite;
                cardImage.color = PanelColor;
            }

            var cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            cardShadow.effectDistance = new Vector2(6f, -6f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0f, 0f, 0f, 0.16f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1180f, 600f);
            cardRect.anchoredPosition = Vector2.zero;

            var title = CreateText("How To Play Title", card.transform, "게임 방법", 38, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = ArtDecoCream;
            ApplyTextShadow(title, new Color32(26, 9, 7, 225), new Vector2(1.5f, -1.5f));
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(780f, 52f);
            titleRect.anchoredPosition = new Vector2(0f, 226f);

            var subtitle = CreateText("How To Play Subtitle", card.transform, "주사위를 굴려 5개의 구간을 장악하세요.", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitle.color = ArtDecoMutedGold;
            subtitle.raycastTarget = false;
            var subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(820f, 34f);
            subtitleRect.anchoredPosition = new Vector2(0f, 184f);

            CreateTitleHowToPlaySection(
                card.transform,
                "How To Play Roll",
                "① 컵 굴리기",
                "「컵 굴리기」를 누른 뒤 컵을 끌어 흔들고, 놓아 주사위를 굴립니다.",
                new Vector2(-248f, 72f));
            CreateTitleHowToPlaySection(
                card.transform,
                "How To Play Place",
                "② 주사위 배치",
                "나온 눈은 빛나는 빈칸에 놓습니다. 같은 눈은 자동으로 붙어 정렬됩니다.",
                new Vector2(248f, 72f));
            CreateTitleHowToPlaySection(
                card.transform,
                "How To Play Attack",
                "③ 같은 눈으로 공격",
                "상대의 같은 눈을 클릭하면 연결된 같은 눈을 제거합니다. 성공하면 한 번 더 굴립니다.",
                new Vector2(-248f, -58f));
            CreateTitleHowToPlaySection(
                card.transform,
                "How To Play Score",
                "④ 점수와 승리",
                "같은 눈은 추가 점수! 5개 구간 중 더 많이 이기면 승리합니다.",
                new Vector2(248f, -58f));

            var tip = CreateText("How To Play Tip", card.transform, "빈칸은 배치 가능 · 검은 주사위는 공격 가능 대상", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            tip.color = new Color32(225, 128, 57, 255);
            ApplyTextShadow(tip, new Color32(26, 9, 7, 220), new Vector2(1f, -1f));
            var tipRect = tip.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.sizeDelta = new Vector2(880f, 30f);
            tipRect.anchoredPosition = new Vector2(0f, -179f);

            titleHowToCloseButton = CreateButton("Title How To Play Close Button", card.transform, "닫기", 25);
            ApplyGeneratedButtonSkin(titleHowToCloseButton, buttonPrimarySprite, new Color32(191, 123, 39, 255));
            titleHowToCloseButton.onClick.AddListener(CloseTitleHowToPlay);
            AddControlButtonDepth(titleHowToCloseButton.gameObject);
            var closeText = titleHowToCloseButton.GetComponentInChildren<Text>();
            if (closeText != null)
            {
                closeText.color = ArtDecoCream;
                ApplyTextShadow(closeText, new Color32(24, 10, 8, 220), new Vector2(1f, -1f));
            }

            var closeRect = titleHowToCloseButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0.5f);
            closeRect.anchorMax = new Vector2(0.5f, 0.5f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(210f, 58f);
            closeRect.anchoredPosition = new Vector2(0f, -238f);

            titleHowToPlayOverlay.SetActive(false);
        }

        private void CreateTitleHowToPlaySection(
            Transform parent,
            string name,
            string heading,
            string detail,
            Vector2 anchoredPosition)
        {
            var section = CreateUiObject(name, parent);
            var sectionImage = section.AddComponent<Image>();
            sectionImage.sprite = diceFaceSprite;
            sectionImage.color = new Color(0.025f, 0.06f, 0.045f, 0.5f);
            sectionImage.raycastTarget = false;
            var sectionOutline = section.AddComponent<Outline>();
            sectionOutline.effectColor = new Color(0.78f, 0.57f, 0.23f, 0.42f);
            sectionOutline.effectDistance = new Vector2(1f, -1f);
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.5f, 0.5f);
            sectionRect.anchorMax = new Vector2(0.5f, 0.5f);
            sectionRect.pivot = new Vector2(0.5f, 0.5f);
            sectionRect.sizeDelta = new Vector2(470f, 108f);
            sectionRect.anchoredPosition = anchoredPosition;

            var headingText = CreateText("Heading", section.transform, heading, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            headingText.color = ArtDecoCream;
            ApplyTextShadow(headingText, new Color32(26, 9, 7, 220), new Vector2(1f, -1f));
            var headingRect = headingText.GetComponent<RectTransform>();
            headingRect.anchorMin = new Vector2(0f, 1f);
            headingRect.anchorMax = new Vector2(1f, 1f);
            headingRect.pivot = new Vector2(0.5f, 1f);
            headingRect.offsetMin = new Vector2(18f, -38f);
            headingRect.offsetMax = new Vector2(-18f, -8f);

            var detailText = CreateText("Detail", section.transform, detail, 17, FontStyle.Normal, TextAnchor.UpperLeft);
            detailText.color = new Color(0.94f, 0.88f, 0.74f, 0.96f);
            detailText.resizeTextMinSize = 15;
            detailText.raycastTarget = false;
            var detailRect = detailText.GetComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(18f, 10f);
            detailRect.offsetMax = new Vector2(-18f, -42f);
        }

        private void CreateModeSelectionOverlay(Transform parent)
        {
            modeSelectionOverlay = CreateUiObject("Mode Selection Overlay", parent);
            var overlayRect = modeSelectionOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            modeSelectionOverlay.transform.SetAsLastSibling();

            var backdrop = modeSelectionOverlay.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.42f);
            backdrop.raycastTarget = true;

            var card = CreateUiObject("Mode Selection Card", modeSelectionOverlay.transform);
            var cardImage = card.AddComponent<Image>();
            if (controlPlaqueSprite != null)
            {
                cardImage.sprite = controlPlaqueSprite;
                cardImage.type = Image.Type.Sliced;
                cardImage.color = Color.white;
            }
            else
            {
                cardImage.sprite = diceFaceSprite;
                cardImage.color = PanelColor;
            }
            var cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            cardShadow.effectDistance = new Vector2(5f, -5f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0f, 0f, 0f, 0.12f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(620f, 300f);
            cardRect.anchoredPosition = Vector2.zero;

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(34, 34, 28, 30);
            cardLayout.spacing = 14;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = true;

            var title = CreateText("Mode Selection Title", card.transform, "게임 모드 선택", 38, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = controlPlaqueSprite != null ? ArtDecoCream : TextColor;
            AddLayout(title.gameObject, -1, 56);

            var subtitle = CreateText("Mode Selection Subtitle", card.transform, "PVP는 플레이어끼리, PVE는 플레이어 1 대 AI로 진행합니다.", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitle.color = controlPlaqueSprite != null ? ArtDecoMutedGold : MutedTextColor;
            AddLayout(subtitle.gameObject, -1, 42);

            var buttons = CreateUiObject("Mode Buttons", card.transform);
            var buttonsLayout = buttons.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 22;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childForceExpandHeight = false;
            buttonsLayout.childForceExpandWidth = false;
            AddLayout(buttons, -1, 82);

            pvpModeButton = CreateButton("PVP Mode Button", buttons.transform, "PVP", 30);
            ApplyGeneratedButtonSkin(pvpModeButton, buttonPvpSprite, PlayerAccentColors[0]);
            ApplyFixedAspectButtonArtwork(pvpModeButton);
            pvpModeButton.onClick.AddListener(() => SelectMatchMode(MatchMode.Pvp));
            AddControlButtonDepth(pvpModeButton.gameObject);
            var pvpText = pvpModeButton.GetComponentInChildren<Text>();
            if (pvpText != null)
            {
                pvpText.color = Color.white;
            }

            AddLayout(pvpModeButton.gameObject, 210, 72);

            pveModeButton = CreateButton("PVE Mode Button", buttons.transform, "PVE", 30);
            ApplyGeneratedButtonSkin(pveModeButton, buttonPveSprite, PlayerAccentColors[1]);
            ApplyFixedAspectButtonArtwork(pveModeButton);
            pveModeButton.onClick.AddListener(() => SelectMatchMode(MatchMode.Pve));
            AddControlButtonDepth(pveModeButton.gameObject);
            var pveText = pveModeButton.GetComponentInChildren<Text>();
            if (pveText != null)
            {
                pveText.color = Color.white;
            }

            AddLayout(pveModeButton.gameObject, 210, 72);

            modeSelectionOverlay.SetActive(false);
        }

        private void CreateAttackAnimationLayer(Transform parent)
        {
            attackAnimationLayer = CreateUiObject("Attack Animation Layer", parent);
            var layerRect = attackAnimationLayer.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            attackAnimationLayer.SetActive(false);
        }

        private void CreateDiceCupStage(RawImage target)
        {
            diceRenderTexture = new RenderTexture(DiceRenderWidth, DiceRenderHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "Dice Cup Render Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1
            };
            diceRenderTexture.Create();
            target.texture = diceRenderTexture;

            diceStageRoot = new GameObject("Dice Cup 3D Stage").transform;
            diceStageRoot.SetParent(transform, false);
            diceStageRoot.position = Vector3.zero;

            // Keep the 3D interaction readable, but use the same warm walnut, emerald and brass
            // palette as the board UI rather than letting the default lighting wash the props out.
            cupMaterial = CreateStageMaterial("Oxblood Leather Cup Material", new Color32(188, 76, 52, 255), false);
            cupInnerMaterial = CreateStageMaterial("Cup Inner Material", new Color32(68, 14, 13, 255), false);
            dieMaterial = CreateStageMaterial("Die Material", Color.white, false);
            pipMaterial = CreateStageMaterial("Pip Material", new Color(0.08f, 0.09f, 0.1f, 1f), false);
            tableMaterial = CreateStageMaterial("Walnut Table Material", new Color32(170, 93, 51, 255), false);
            feltMaterial = CreateStageMaterial("Patterned Emerald Felt Material", new Color32(78, 130, 99, 255), false);
            trayRimMaterial = CreateStageMaterial("Antique Brass Tray Rim Material", new Color32(86, 55, 18, 255), false);
            trayHighlightMaterial = CreateStageMaterial("Polished Brass Tray Highlight Material", new Color32(235, 184, 72, 255), false);
            ApplyStageTexture(cupMaterial, cupLeatherTexture, new Vector2(1.4f, 1f), 0.28f);
            ApplyStageTexture(cupInnerMaterial, cupLeatherTexture, new Vector2(1.1f, 1f), 0.16f);
            ApplyStageTexture(dieMaterial, worldDieSurfaceTexture != null ? worldDieSurfaceTexture : diceCeramicTexture, Vector2.one, 0.56f);
            ApplyStageTexture(tableMaterial, walnutTableTexture, new Vector2(2.5f, 2f), 0.38f);
            ApplyStageTexture(feltMaterial, boardPatternTexture, new Vector2(1.6f, 1.6f), 0.2f);
            SetMaterialFinish(cupMaterial, 0f, 0.34f);
            SetMaterialFinish(cupInnerMaterial, 0f, 0.2f);
            SetMaterialFinish(dieMaterial, 0.02f, 0.56f);
            SetMaterialFinish(pipMaterial, 0.04f, 0.3f);
            SetMaterialFinish(tableMaterial, 0f, 0.3f);
            SetMaterialFinish(feltMaterial, 0f, 0.16f);
            // A mostly-metallic Standard material has no useful environment reflection in this
            // self-contained RenderTexture, so it turns muddy.  Low metallic values keep brass
            // warm and legible under the stage lights.
            SetMaterialFinish(trayRimMaterial, 0.08f, 0.4f);
            SetMaterialFinish(trayHighlightMaterial, 0.16f, 0.46f);

            CreateStageCamera();
            CreateStageLights();
            CreateStageTable();
            CreateWorldDie();
            CreateWorldCup();
            SetLayerRecursively(diceStageRoot.gameObject, DiceStageLayer);
            ExcludeDiceStageFromOtherCameras();
            ResetDiceStage();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void ExcludeDiceStageFromOtherCameras()
        {
            var diceLayerMask = 1 << DiceStageLayer;
            var sceneCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var cameraCount = 0;
            for (var index = 0; index < sceneCameras.Length; index++)
            {
                var sceneCamera = sceneCameras[index];
                if (sceneCamera != diceCamera && sceneCamera.gameObject.scene == gameObject.scene)
                {
                    cameraCount++;
                }
            }

            maskedSceneCameras = new Camera[cameraCount];
            originalSceneCameraMasks = new int[cameraCount];
            var storedIndex = 0;
            for (var index = 0; index < sceneCameras.Length; index++)
            {
                var sceneCamera = sceneCameras[index];
                if (sceneCamera != diceCamera && sceneCamera.gameObject.scene == gameObject.scene)
                {
                    maskedSceneCameras[storedIndex] = sceneCamera;
                    originalSceneCameraMasks[storedIndex] = sceneCamera.cullingMask;
                    storedIndex++;
                    sceneCamera.cullingMask &= ~diceLayerMask;
                }
            }
        }

        private void CreateBoards(Transform parent)
        {
            var boardsRow = CreateUiObject("Boards", parent);
            var boardsLayout = boardsRow.AddComponent<HorizontalLayoutGroup>();
            boardsLayout.spacing = 16;
            boardsLayout.childControlHeight = true;
            boardsLayout.childControlWidth = true;
            boardsLayout.childForceExpandHeight = false;
            boardsLayout.childForceExpandWidth = false;
            boardsLayout.childAlignment = TextAnchor.MiddleCenter;
            AddLayout(boardsRow, -1, 590, flexibleHeight: 1);

            CreateGameplayTableau(boardsRow.transform);
            boards[0] = CreatePlayerBoard(boardsRow.transform, 0);
            CreateScoreComparison(boardsRow.transform);
            boards[1] = CreatePlayerBoard(boardsRow.transform, 1);
        }

        private void CreateGameplayTableau(Transform parent)
        {
            if (gameplayTableauSprite == null)
            {
                return;
            }

            var tableau = CreateUiObject("Gameplay Tableau Background", parent);
            var tableauLayout = tableau.AddComponent<LayoutElement>();
            tableauLayout.ignoreLayout = true;
            var tableauImage = tableau.AddComponent<Image>();
            tableauImage.sprite = gameplayTableauSprite;
            tableauImage.type = Image.Type.Simple;
            tableauImage.preserveAspect = false;
            tableauImage.color = Color.white;
            tableauImage.raycastTarget = false;

            var tableauRect = tableau.GetComponent<RectTransform>();
            tableauRect.anchorMin = new Vector2(0.5f, 0.5f);
            tableauRect.anchorMax = new Vector2(0.5f, 0.5f);
            tableauRect.pivot = new Vector2(0.5f, 0.5f);
            tableauRect.sizeDelta = new Vector2(1308f, 590f);
            tableauRect.anchoredPosition = Vector2.zero;
            tableau.transform.SetAsFirstSibling();
        }

        private static void ConfigureTableauRect(GameObject target, float x, float y, float width, float height)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = true;
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static Image CreateTableauArtwork(
            string name,
            Transform parent,
            Sprite sprite,
            float x,
            float y,
            float width,
            float height)
        {
            var artwork = CreateUiObject(name, parent);
            var image = artwork.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
            ConfigureTableauRect(artwork, x, y, width, height);
            artwork.transform.SetAsFirstSibling();
            return image;
        }

        private PlayerBoard CreatePlayerBoard(Transform parent, int playerIndex)
        {
            var board = new PlayerBoard(playerIndex, BoardSize);
            var useGameplayTableau = gameplayTableauSprite != null;
            var playmatSprite = useGameplayTableau
                ? null
                : playerIndex == 0 ? playerOnePlaymatSprite : playerTwoPlaymatSprite;

            var panel = CreateUiObject($"Player {playerIndex + 1} Panel", parent);
            board.PanelImage = panel.AddComponent<Image>();
            if (useGameplayTableau)
            {
                board.PanelImage.color = Color.clear;
                board.PanelImage.raycastTarget = false;
            }
            else if (playmatSprite != null)
            {
                board.PanelImage.sprite = playmatSprite;
                board.PanelImage.type = Image.Type.Simple;
                board.PanelImage.color = Color.white;
            }
            else if (playerBoardPanelSprite != null)
            {
                board.PanelImage.sprite = playerBoardPanelSprite;
                board.PanelImage.type = Image.Type.Simple;
                board.PanelImage.color = Color.white;
            }
            else if (panelParchmentSprite != null)
            {
                board.PanelImage.sprite = panelParchmentSprite;
                board.PanelImage.type = Image.Type.Tiled;
                board.PanelImage.color = Color.white;
            }
            else
            {
                board.PanelImage.color = PanelColor;
            }
            var panelShadow = panel.AddComponent<Shadow>();
            panelShadow.effectColor = useGameplayTableau ? Color.clear : new Color(0f, 0f, 0f, 0.08f);
            panelShadow.effectDistance = new Vector2(3f, -3f);
            board.PanelOutline = panel.AddComponent<Outline>();
            board.PanelOutline.effectColor = Color.clear;
            board.PanelOutline.effectDistance = Vector2.zero;
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(12, 12, 10, 12);
            panelLayout.spacing = 8;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.enabled = !useGameplayTableau;
            AddLayout(panel, 548, 590);
            if (!useGameplayTableau && playmatSprite == null && playerBoardPanelSprite == null)
            {
                CreateBoardPatternOverlay(panel.transform, 0.12f);
            }

            // This generated panel deliberately covers the progress rail baked into the
            // original tableau.  Player labels stay live Unity text so Korean remains crisp.
            if (useGameplayTableau && playerStatusHeaderSprite != null)
            {
                CreateTableauArtwork(
                    "Player Status Header Art",
                    panel.transform,
                    playerStatusHeaderSprite,
                    0f,
                    0f,
                    548f,
                    114f);
            }

            var titleRow = CreateUiObject("Title Row", panel.transform);
            var titleLayout = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 12;
            titleLayout.childAlignment = TextAnchor.MiddleCenter;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = true;
            titleLayout.childForceExpandWidth = false;
            AddLayout(titleRow, -1, 38);
            if (useGameplayTableau)
            {
                ConfigureTableauRect(titleRow, 32f, 24f, 484f, 48f);
            }

            board.TitleText = CreateText("Title", titleRow.transform, $"플레이어 {playerIndex + 1}", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
            ApplyPlayerHeaderTypography(board.TitleText, ArtDecoCream);
            AddLayout(board.TitleText.gameObject, 280, 48, flexibleWidth: 1);

            board.ProgressText = CreateText("Progress", titleRow.transform, "0 / 25", 25, FontStyle.Bold, TextAnchor.MiddleRight);
            ApplyPlayerHeaderTypography(board.ProgressText, ArtDecoMutedGold);
            AddLayout(board.ProgressText.gameObject, 112, 48);

            CreateBoardProgressBar(panel.transform, board, playerIndex);
            if (useGameplayTableau)
            {
                ConfigureTableauRect(board.ProgressTrackImage.gameObject, 12f, 92f, 524f, 16f);
                // The player total remains in the header text.  There is intentionally no
                // separate progress rail, and the old baked rail is covered by the header art.
                board.ProgressTrackImage.gameObject.SetActive(false);
                var fillRect = board.ProgressFillImage.rectTransform;
                fillRect.offsetMin = new Vector2(10f, 4f);
                fillRect.offsetMax = new Vector2(-10f, -4f);
            }

            var gridWrap = CreateUiObject("Grid With Section Labels", panel.transform);
            var gridWrapLayout = gridWrap.AddComponent<HorizontalLayoutGroup>();
            gridWrapLayout.spacing = 6;
            gridWrapLayout.childAlignment = TextAnchor.MiddleCenter;
            gridWrapLayout.childControlHeight = true;
            gridWrapLayout.childControlWidth = true;
            gridWrapLayout.childForceExpandHeight = false;
            gridWrapLayout.childForceExpandWidth = false;
            AddLayout(gridWrap, -1, 492);
            if (useGameplayTableau)
            {
                gridWrapLayout.enabled = false;
                ConfigureTableauRect(gridWrap, 0f, 0f, 548f, 590f);
            }

            var labelStrip = CreateUiObject("Section Labels", gridWrap.transform);
            var labelLayout = labelStrip.AddComponent<VerticalLayoutGroup>();
            labelLayout.spacing = 4;
            labelLayout.childAlignment = TextAnchor.MiddleCenter;
            labelLayout.childControlHeight = true;
            labelLayout.childControlWidth = true;
            labelLayout.childForceExpandHeight = false;
            labelLayout.childForceExpandWidth = false;
            AddLayout(labelStrip, 24, 492);
            if (useGameplayTableau)
            {
                labelLayout.padding = new RectOffset(0, 0, 0, 0);
                labelLayout.spacing = 0;
                ConfigureTableauRect(
                    labelStrip,
                    playerIndex == 0 ? 474f : 49f,
                    125f,
                    24f,
                    430f);
            }

            for (var section = 0; section < BoardSize; section++)
            {
                CreateSectionLabel(labelStrip.transform, board, playerIndex, section, useGameplayTableau ? 86f : 92f);
            }

            var gridObject = CreateUiObject("Grid", gridWrap.transform);
            var gridImage = gridObject.AddComponent<Image>();
            if (useGameplayTableau)
            {
                gridImage.color = Color.clear;
                gridImage.raycastTarget = false;
            }
            else if (boardPatternSprite != null)
            {
                gridImage.sprite = boardPatternSprite;
                gridImage.type = Image.Type.Tiled;
                gridImage.color = playmatSprite != null
                    ? new Color(1f, 1f, 1f, 0.14f)
                    : new Color(1f, 1f, 1f, 0.96f);
            }
            else
            {
                gridImage.color = new Color32(216, 219, 216, 255);
            }
            var gridShadow = gridObject.AddComponent<Shadow>();
            gridShadow.effectColor = useGameplayTableau ? Color.clear : new Color(0f, 0f, 0f, 0.08f);
            gridShadow.effectDistance = new Vector2(2f, -2f);
            var gridOutline = gridObject.AddComponent<Outline>();
            gridOutline.effectColor = useGameplayTableau ? Color.clear : new Color(0f, 0f, 0f, 0.1f);
            gridOutline.effectDistance = new Vector2(1f, -1f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardSize;
            if (useGameplayTableau)
            {
                grid.cellSize = new Vector2(86f, 86f);
                grid.spacing = new Vector2(2f, 0f);
                grid.padding = new RectOffset(5, 7, 5, 5);
                ConfigureTableauRect(
                    gridObject,
                    playerIndex == 0 ? 21f : 76f,
                    120f,
                    450f,
                    440f);
            }
            else
            {
                grid.cellSize = new Vector2(92, 92);
                grid.spacing = new Vector2(4, 4);
                grid.padding = new RectOffset(8, 8, 8, 8);
                AddLayout(gridObject, 492, 492);
            }

            for (var row = 0; row < BoardSize; row++)
            {
                for (var column = 0; column < BoardSize; column++)
                {
                    var capturedRow = row;
                    var capturedColumn = column;
                    var cellButton = CreateButton($"Cell {row},{column}", gridObject.transform, string.Empty, 36);
                    if (!useGameplayTableau && cellPlateSprite != null)
                    {
                        cellButton.image.sprite = cellPlateSprite;
                        cellButton.image.type = Image.Type.Simple;
                        cellButton.image.preserveAspect = false;
                        var cellColors = cellButton.colors;
                        cellColors.disabledColor = Color.white;
                        cellButton.colors = cellColors;
                    }
                    cellButton.image.color = useGameplayTableau ? Color.clear : GetCellPlateTint(EmptyCellColor);
                    var cellOutline = cellButton.gameObject.AddComponent<Outline>();
                    cellOutline.effectColor = Color.clear;
                    cellOutline.effectDistance = Vector2.zero;
                    cellButton.onClick.AddListener(() => HandleCellClick(playerIndex, capturedRow, capturedColumn));
                    board.CellButtons[row, column] = cellButton;
                    board.CellTexts[row, column] = cellButton.GetComponentInChildren<Text>();
                    board.CellOutlines[row, column] = cellOutline;
                    CreateCellPips(cellButton.transform, board, row, column);
                }
            }

            if (playerIndex == 0)
            {
                labelStrip.transform.SetAsLastSibling();
            }

            return board;
        }

        private void CreateBoardProgressBar(Transform parent, PlayerBoard board, int playerIndex)
        {
            var track = CreateUiObject("Board Progress Track", parent);
            board.ProgressTrackImage = track.AddComponent<Image>();
            board.ProgressTrackImage.sprite = diceFaceSprite;
            board.ProgressTrackImage.color = new Color(0.88f, 0.78f, 0.56f, 0.3f);
            board.ProgressTrackImage.raycastTarget = false;
            AddLayout(track, -1, 10);

            var fillObject = CreateUiObject("Board Progress Fill", track.transform);
            board.ProgressFillImage = fillObject.AddComponent<Image>();
            board.ProgressFillImage.sprite = diceFaceSprite;
            board.ProgressFillImage.color = PlayerAccentColors[playerIndex];
            board.ProgressFillImage.type = Image.Type.Filled;
            board.ProgressFillImage.fillMethod = Image.FillMethod.Horizontal;
            board.ProgressFillImage.fillOrigin = playerIndex == 0 ? 1 : 0;
            board.ProgressFillImage.fillAmount = 0f;
            board.ProgressFillImage.raycastTarget = false;

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private void CreateSectionLabel(Transform parent, PlayerBoard board, int playerIndex, int section, float slotHeight)
        {
            var slot = CreateUiObject($"Section Label Slot {section + 1}", parent);
            AddLayout(slot, 24, slotHeight);

            var badge = CreateUiObject($"Section Label {section + 1}", slot.transform);
            var badgeImage = badge.AddComponent<Image>();
            badgeImage.sprite = scoreBadgeSprite;
            var badgeColor = PlayerAccentColors[playerIndex];
            badgeColor.a = 0.18f;
            badgeImage.color = badgeColor;
            badgeImage.raycastTarget = false;
            var badgeShadow = badge.AddComponent<Shadow>();
            badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            badgeShadow.effectDistance = new Vector2(1f, -1f);
            var badgeOutline = badge.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);
            board.SectionLabelImages[section] = badgeImage;

            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(24f, 24f);
            badgeRect.anchoredPosition = Vector2.zero;

            var label = CreateText("Label", badge.transform, (section + 1).ToString(), 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = PlayerLabelColors[playerIndex];
            board.SectionLabelTexts[section] = label;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void CreateCellPips(Transform cell, PlayerBoard board, int row, int column)
        {
            var faceObject = CreateUiObject("Die Face", cell);
            var faceImage = faceObject.AddComponent<Image>();
            faceImage.sprite = diceFaceArtSprite != null ? diceFaceArtSprite : diceFaceSprite;
            faceImage.type = Image.Type.Simple;
            faceImage.preserveAspect = false;
            faceImage.color = Color.white;
            faceImage.raycastTarget = false;
            var faceShadow = faceObject.AddComponent<Shadow>();
            faceShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            faceShadow.effectDistance = new Vector2(2f, -2f);
            var faceOutline = faceObject.AddComponent<Outline>();
            faceOutline.effectColor = new Color(0f, 0f, 0f, 0.14f);
            faceOutline.effectDistance = new Vector2(1f, -1f);
            var faceRect = faceObject.GetComponent<RectTransform>();
            faceRect.anchorMin = new Vector2(0.5f, 0.5f);
            faceRect.anchorMax = new Vector2(0.5f, 0.5f);
            faceRect.sizeDelta = new Vector2(76f, 76f);
            faceRect.anchoredPosition = Vector2.zero;
            if (diceFaceArtSprite == null)
            {
                CreateDiceCeramicInlay(faceObject.transform, 0.1f, 0.34f);
            }
            faceObject.SetActive(false);
            board.CellFaceImages[row, column] = faceImage;

            var positions = new[]
            {
                Vector2.zero,
                new Vector2(-20f, 20f),
                new Vector2(20f, 20f),
                new Vector2(-20f, 0f),
                new Vector2(20f, 0f),
                new Vector2(-20f, -20f),
                new Vector2(20f, -20f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                var pipObject = CreateUiObject($"Pip {index}", faceObject.transform);
                var pipImage = pipObject.AddComponent<Image>();
                pipImage.sprite = dicePipSprite;
                pipImage.color = TextColor;
                pipImage.raycastTarget = false;
                var pipShadow = pipObject.AddComponent<Shadow>();
                pipShadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
                pipShadow.effectDistance = new Vector2(1.1f, -1.1f);

                var pipRect = pipObject.GetComponent<RectTransform>();
                pipRect.anchorMin = new Vector2(0.5f, 0.5f);
                pipRect.anchorMax = new Vector2(0.5f, 0.5f);
                pipRect.sizeDelta = new Vector2(14f, 14f);
                pipRect.anchoredPosition = positions[index];
                pipObject.SetActive(false);
                board.CellPips[row, column, index] = pipImage;
            }

            var markerObject = CreateUiObject("Protection Marker", faceObject.transform);
            var markerImage = markerObject.AddComponent<Image>();
            markerImage.sprite = scoreBadgeSprite;
            markerImage.color = ButtonColor;
            markerImage.raycastTarget = false;
            var markerOutline = markerObject.AddComponent<Outline>();
            markerOutline.effectColor = Color.white;
            markerOutline.effectDistance = new Vector2(1f, -1f);

            var markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(1f, 1f);
            markerRect.anchorMax = new Vector2(1f, 1f);
            markerRect.sizeDelta = new Vector2(15f, 15f);
            markerRect.anchoredPosition = new Vector2(-11f, -11f);
            var markerSymbol = CreateText("Protection Symbol", markerObject.transform, "!", 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            markerSymbol.color = Color.white;
            markerSymbol.raycastTarget = false;
            var markerSymbolRect = markerSymbol.GetComponent<RectTransform>();
            markerSymbolRect.anchorMin = Vector2.zero;
            markerSymbolRect.anchorMax = Vector2.one;
            markerSymbolRect.offsetMin = Vector2.zero;
            markerSymbolRect.offsetMax = Vector2.zero;
            markerObject.SetActive(false);
            board.CellProtectionMarkers[row, column] = markerImage;
        }

        private void CreateScoreComparison(Transform parent)
        {
            var useGameplayTableau = gameplayTableauSprite != null;
            var panel = CreateUiObject("Section Score Comparison", parent);
            var panelImage = panel.AddComponent<Image>();
            if (useGameplayTableau)
            {
                panelImage.color = Color.clear;
            }
            else if (scoreTowerSprite != null)
            {
                panelImage.sprite = scoreTowerSprite;
                panelImage.type = Image.Type.Simple;
                panelImage.color = Color.white;
            }
            else
            {
                panelImage.color = ScorePanelColor;
            }
            panelImage.raycastTarget = false;
            var panelShadow = panel.AddComponent<Shadow>();
            panelShadow.effectColor = useGameplayTableau ? Color.clear : new Color(0f, 0f, 0f, 0.1f);
            panelShadow.effectDistance = new Vector2(3f, -3f);

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(6, 6, 10, 18);
            panelLayout.spacing = 4;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.enabled = !useGameplayTableau;
            AddLayout(panel, 180, 590);
            var title = CreateText("Score Comparison Title", panel.transform, "구간 점수", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = scoreTowerSprite != null ? ArtDecoCream : TextColor;
            AddLayout(title.gameObject, -1, 42);
            if (useGameplayTableau)
            {
                ConfigureTableauRect(title.gameObject, 0f, 8f, 180f, 42f);
            }

            for (var section = 0; section < BoardSize; section++)
            {
                var row = CreateUiObject($"Section {section + 1}", panel.transform);
                var rowImage = row.AddComponent<Image>();
                rowImage.sprite = diceFaceSprite;
                rowImage.color = Color.clear;
                rowImage.raycastTarget = false;
                sectionRowImages[section] = rowImage;
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 6;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlHeight = false;
                rowLayout.childControlWidth = true;
                rowLayout.childForceExpandHeight = false;
                rowLayout.childForceExpandWidth = false;
                var rowHeight = useGameplayTableau ? 86f : 92f;
                AddLayout(row, -1, rowHeight);
                if (useGameplayTableau)
                {
                    // Match grid y = 120, top padding = 5, cell height = 86.
                    ConfigureTableauRect(row, 0f, 125f + section * rowHeight, 180f, rowHeight);
                }

                CreateScoreBadge(row.transform, 0, section);

                var resultText = CreateText("Result", row.transform, "-", 21, FontStyle.Bold, TextAnchor.MiddleCenter);
                resultText.color = MutedTextColor;
                var resultShadow = resultText.gameObject.AddComponent<Shadow>();
                resultShadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
                resultShadow.effectDistance = new Vector2(1f, -1f);
                sectionResultTexts[section] = resultText;
                AddLayout(resultText.gameObject, 48, 54);

                CreateScoreBadge(row.transform, 1, section);
            }
        }

        private void CreateScoreBadge(Transform parent, int player, int section)
        {
            var badge = CreateUiObject($"Player {player + 1} Section {section + 1} Score", parent);
            var badgeImage = badge.AddComponent<Image>();
            badgeImage.sprite = scoreMedallionSprite != null ? scoreMedallionSprite : scoreBadgeSprite;
            badgeImage.type = Image.Type.Simple;
            badgeImage.preserveAspect = true;
            badgeImage.color = GetScoreMedallionTint(ScoreNeutralColor);
            var badgeShadow = badge.AddComponent<Shadow>();
            badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            badgeShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var badgeOutline = badge.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(1f, 1f, 1f, 0.2f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);
            sectionScoreImages[player, section] = badgeImage;
            AddLayout(badge, 52, 52);

            var scoreText = CreateText("Score", badge.transform, "0", 25, FontStyle.Bold, TextAnchor.MiddleCenter);
            scoreText.color = scoreMedallionSprite != null ? new Color32(25, 49, 39, 255) : ScoreTextColor;
            sectionScoreTexts[player, section] = scoreText;

            var scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = Vector2.zero;
            scoreRect.anchorMax = Vector2.one;
            // Legacy Text aligns its glyph baseline slightly below the visual centre.
            // Compensate within the fixed circular medallion without changing the badge
            // size or its row layout.
            scoreRect.offsetMin = new Vector2(0.85f, 5f);
            scoreRect.offsetMax = new Vector2(0.85f, 5f);
        }

        private void CreateStageCamera()
        {
            var cameraObject = new GameObject("Dice Cup Camera");
            cameraObject.transform.SetParent(diceStageRoot, false);
            cameraObject.transform.localPosition = new Vector3(-0.2f, 4.65f, -3.65f);
            cameraObject.transform.LookAt(diceStageRoot.position + new Vector3(-0.25f, 0.08f, 0.05f));

            diceCamera = cameraObject.AddComponent<Camera>();
            diceCamera.clearFlags = CameraClearFlags.SolidColor;
            diceCamera.backgroundColor = new Color32(5, 27, 20, 255);
            diceCamera.fieldOfView = 43f;
            diceCamera.nearClipPlane = 0.1f;
            diceCamera.farClipPlane = 30f;
            diceCamera.cullingMask = 1 << DiceStageLayer;
            diceCamera.allowHDR = false;
            diceCamera.allowMSAA = false;
            diceCamera.useOcclusionCulling = false;
            diceCamera.depthTextureMode = DepthTextureMode.None;
            diceCamera.targetTexture = diceRenderTexture;
        }

        private void CreateStageLights()
        {
            var keyLight = new GameObject("Dice Cup Key Light");
            keyLight.transform.SetParent(diceStageRoot, false);
            keyLight.transform.localPosition = new Vector3(-1.8f, 3.2f, -2.8f);
            var key = keyLight.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 0.82f;
            key.shadows = LightShadows.Hard;
            key.cullingMask = 1 << DiceStageLayer;
            keyLight.transform.rotation = Quaternion.Euler(48f, -24f, 0f);

            var fillLight = new GameObject("Dice Cup Fill Light");
            fillLight.transform.SetParent(diceStageRoot, false);
            fillLight.transform.localPosition = new Vector3(2.2f, 1.8f, -2.2f);
            var fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 0.22f;
            fill.range = 5f;
            fill.renderMode = LightRenderMode.ForceVertex;
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << DiceStageLayer;
        }

        private void CreateStageTable()
        {
            CreateStageCube("Walnut Table", new Vector3(0f, -0.14f, 0.02f), new Vector3(7.5f, 0.08f, 4.6f), tableMaterial, true);
            CreateStageCube("Emerald Felt", new Vector3(-0.36f, -0.08f, 0.02f), new Vector3(2.75f, 0.06f, 1.85f), feltMaterial, true);
            CreateStageCube("Tray Top Rail", new Vector3(-0.36f, 0.02f, 1.05f), new Vector3(3.05f, 0.22f, 0.16f), trayRimMaterial, true);
            CreateStageCube("Tray Bottom Rail", new Vector3(-0.36f, 0.02f, -1.01f), new Vector3(3.05f, 0.22f, 0.16f), trayRimMaterial, true);
            CreateStageCube("Tray Left Rail", new Vector3(-1.94f, 0.02f, 0.02f), new Vector3(0.16f, 0.22f, 2.18f), trayRimMaterial, true);
            CreateStageCube("Tray Right Rail", new Vector3(1.22f, 0.02f, 0.02f), new Vector3(0.16f, 0.22f, 2.18f), trayRimMaterial, true);
            CreateStageCube("Tray Top Highlight", new Vector3(-0.36f, 0.145f, 1.05f), new Vector3(2.78f, 0.018f, 0.026f), trayHighlightMaterial);
            CreateStageCube("Tray Bottom Highlight", new Vector3(-0.36f, 0.145f, -1.01f), new Vector3(2.78f, 0.018f, 0.026f), trayHighlightMaterial);
            CreateStageCube("Tray Left Highlight", new Vector3(-1.94f, 0.145f, 0.02f), new Vector3(0.026f, 0.018f, 1.9f), trayHighlightMaterial);
            CreateStageCube("Tray Right Highlight", new Vector3(1.22f, 0.145f, 0.02f), new Vector3(0.026f, 0.018f, 1.9f), trayHighlightMaterial);
        }

        private void CreateStageCube(string name, Vector3 position, Vector3 scale, Material material, bool keepCollider = false)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(diceStageRoot, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Destroy(cube.GetComponent<Collider>());
            }
        }

        private void CreateWorldCup()
        {
            var cup = new GameObject("Dice Cup");
            cup.transform.SetParent(diceStageRoot, false);
            diceCupTransform = cup.transform;
            diceCupBody = cup.AddComponent<Rigidbody>();
            diceCupBody.useGravity = false;
            diceCupBody.isKinematic = true;
            diceCupBody.interpolation = RigidbodyInterpolation.None;
            diceCupBody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var outerWall = new GameObject("Cup Outer Wall");
            outerWall.transform.SetParent(cup.transform, false);
            var outerFilter = outerWall.AddComponent<MeshFilter>();
            cupOuterMesh = CreateOpenCupMesh(0.62f, 0.82f, 48, false);
            outerFilter.sharedMesh = cupOuterMesh;
            outerWall.AddComponent<MeshRenderer>().sharedMaterial = cupMaterial;
            var outerCollider = outerWall.AddComponent<MeshCollider>();
            outerCollider.sharedMesh = cupOuterMesh;
            outerCollider.convex = false;
            outerCollider.material = worldDiePhysicsMaterial;

            var innerWall = new GameObject("Cup Red Interior");
            innerWall.transform.SetParent(cup.transform, false);
            innerWall.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            var innerFilter = innerWall.AddComponent<MeshFilter>();
            cupInnerMesh = CreateOpenCupMesh(0.5f, 0.72f, 48, true);
            innerFilter.sharedMesh = cupInnerMesh;
            innerWall.AddComponent<MeshRenderer>().sharedMaterial = cupInnerMaterial;
            var innerCollider = innerWall.AddComponent<MeshCollider>();
            innerCollider.sharedMesh = cupInnerMesh;
            innerCollider.convex = false;
            innerCollider.material = worldDiePhysicsMaterial;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Cup Red Floor";
            floor.transform.SetParent(cup.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.39f, 0f);
            floor.transform.localScale = new Vector3(1f, 0.025f, 1f);
            floor.GetComponent<Renderer>().sharedMaterial = cupInnerMaterial;
            Destroy(floor.GetComponent<Collider>());
            var floorCollider = floor.AddComponent<MeshCollider>();
            floorCollider.sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
            floorCollider.convex = true;
            floorCollider.material = worldDiePhysicsMaterial;

            var rim = new GameObject("Cup Thick Rim");
            rim.transform.SetParent(cup.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.41f, 0f);
            var rimFilter = rim.AddComponent<MeshFilter>();
            cupRimMesh = CreateRingMesh(0.51f, 0.65f, 64);
            rimFilter.sharedMesh = cupRimMesh;
            rim.AddComponent<MeshRenderer>().sharedMaterial = trayRimMaterial;
            var rimCollider = rim.AddComponent<MeshCollider>();
            rimCollider.sharedMesh = cupRimMesh;
            rimCollider.convex = false;
            rimCollider.material = worldDiePhysicsMaterial;

            // A broad, single-color top ring reads like a target from an elevated camera.
            // Keep the physical lip narrow and use two fine gold inlays for an Art Deco rim.
            var innerRimInlay = new GameObject("Cup Inner Rim Gold Inlay");
            innerRimInlay.transform.SetParent(cup.transform, false);
            innerRimInlay.transform.localPosition = new Vector3(0f, 0.417f, 0f);
            var innerRimInlayFilter = innerRimInlay.AddComponent<MeshFilter>();
            cupRimInnerInlayMesh = CreateRingMesh(0.512f, 0.531f, 64);
            innerRimInlayFilter.sharedMesh = cupRimInnerInlayMesh;
            innerRimInlay.AddComponent<MeshRenderer>().sharedMaterial = trayHighlightMaterial;

            var outerRimInlay = new GameObject("Cup Outer Rim Gold Inlay");
            outerRimInlay.transform.SetParent(cup.transform, false);
            outerRimInlay.transform.localPosition = new Vector3(0f, 0.418f, 0f);
            var outerRimInlayFilter = outerRimInlay.AddComponent<MeshFilter>();
            cupRimOuterInlayMesh = CreateRingMesh(0.63f, 0.648f, 64);
            outerRimInlayFilter.sharedMesh = cupRimOuterInlayMesh;
            outerRimInlay.AddComponent<MeshRenderer>().sharedMaterial = trayHighlightMaterial;

            // Decorative bands are render-only.  They give the cup a deliberate Art Deco
            // silhouette without changing its collider volume or dice-in-cup physics.
            var upperBand = new GameObject("Cup Upper Brass Band");
            upperBand.transform.SetParent(cup.transform, false);
            upperBand.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            var upperBandFilter = upperBand.AddComponent<MeshFilter>();
            cupUpperBandMesh = CreateOpenCupMesh(0.626f, 0.04f, 48, false);
            upperBandFilter.sharedMesh = cupUpperBandMesh;
            upperBand.AddComponent<MeshRenderer>().sharedMaterial = trayHighlightMaterial;

            var lowerBand = new GameObject("Cup Lower Brass Band");
            lowerBand.transform.SetParent(cup.transform, false);
            lowerBand.transform.localPosition = new Vector3(0f, -0.23f, 0f);
            var lowerBandFilter = lowerBand.AddComponent<MeshFilter>();
            cupLowerBandMesh = CreateOpenCupMesh(0.626f, 0.035f, 48, false);
            lowerBandFilter.sharedMesh = cupLowerBandMesh;
            lowerBand.AddComponent<MeshRenderer>().sharedMaterial = trayHighlightMaterial;
        }

        private void CreateWorldDie()
        {
            var die = new GameObject("World Die");
            die.transform.SetParent(diceStageRoot, false);
            die.transform.localScale = Vector3.one * 0.42f;
            worldDieMesh = CreateRoundedCubeMesh(0.5f, 0.075f, 4);
            var dieFilter = die.AddComponent<MeshFilter>();
            dieFilter.sharedMesh = worldDieMesh;
            die.AddComponent<MeshRenderer>().sharedMaterial = dieMaterial;
            var dieCollider = die.AddComponent<MeshCollider>();
            dieCollider.sharedMesh = worldDieMesh;
            dieCollider.convex = true;
            worldDieCollider = dieCollider;
            if (worldDieCollider != null)
            {
                worldDiePhysicsMaterial = new PhysicsMaterial("World Die Physics")
                {
                    dynamicFriction = 0.2f,
                    staticFriction = 0.28f,
                    bounciness = 0.2f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
                worldDieCollider.material = worldDiePhysicsMaterial;
            }

            worldDieBody = die.AddComponent<Rigidbody>();
            worldDieBody.mass = 0.18f;
            worldDieBody.useGravity = false;
            worldDieBody.isKinematic = true;
            worldDieBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            worldDieBody.interpolation = RigidbodyInterpolation.Interpolate;
            worldDieBody.linearDamping = DieLinearDamping;
            worldDieBody.angularDamping = DieAngularDamping;
            worldDieBody.maxAngularVelocity = 22f;
            worldDieBody.solverIterations = 6;
            worldDieBody.solverVelocityIterations = 2;
            worldDieTransform = die.transform;
            die.AddComponent<WorldDieCollisionReporter>().Initialize(this);

            CreateWorldDieFacePips(1, Vector3.up, Vector3.right, Vector3.forward);
            CreateWorldDieFacePips(6, Vector3.down, Vector3.right, Vector3.back);
            CreateWorldDieFacePips(2, Vector3.forward, Vector3.right, Vector3.up);
            CreateWorldDieFacePips(5, Vector3.back, Vector3.left, Vector3.up);
            CreateWorldDieFacePips(3, Vector3.right, Vector3.back, Vector3.up);
            CreateWorldDieFacePips(4, Vector3.left, Vector3.forward, Vector3.up);
        }

        private void CreateWorldDieFacePips(int value, Vector3 normal, Vector3 axisU, Vector3 axisV)
        {
            const float faceOffset = 0.515f;
            const float pipOffset = 0.155f;
            var positions = new[]
            {
                Vector2.zero,
                new Vector2(-pipOffset, pipOffset),
                new Vector2(pipOffset, pipOffset),
                new Vector2(-pipOffset, 0f),
                new Vector2(pipOffset, 0f),
                new Vector2(-pipOffset, -pipOffset),
                new Vector2(pipOffset, -pipOffset)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                if (!ShouldShowPip(value, index))
                {
                    continue;
                }

                var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pip.name = $"World Die {value} Pip {index}";
                pip.transform.SetParent(worldDieTransform, false);
                pip.transform.localPosition = normal * faceOffset + axisU * positions[index].x + axisV * positions[index].y;
                pip.transform.localScale = Vector3.one * 0.082f;
                pip.GetComponent<Renderer>().sharedMaterial = pipMaterial;
                Destroy(pip.GetComponent<Collider>());
            }
        }

        private void CreateControls(Transform parent)
        {
            var controls = CreateUiObject("Controls", parent);
            var controlsImage = controls.AddComponent<Image>();
            var usesGeneratedActionTray = gameplayActionTraySprite != null;
            if (usesGeneratedActionTray)
            {
                // The generated tray already contains the complete visual frame.  Leaving
                // this root clear prevents the old wide wooden bar from becoming a second,
                // competing border behind it.
                controlsImage.color = Color.clear;
            }
            else if (controlPlaqueSprite != null)
            {
                controlsImage.sprite = controlPlaqueSprite;
                controlsImage.type = Image.Type.Sliced;
                controlsImage.color = Color.white;
            }
            else
            {
                controlsImage.color = Color.clear;
            }
            controlsImage.raycastTarget = false;
            var controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 18;
            controlsLayout.childAlignment = TextAnchor.MiddleCenter;
            controlsLayout.childControlHeight = true;
            controlsLayout.childControlWidth = true;
            controlsLayout.childForceExpandHeight = true;
            controlsLayout.childForceExpandWidth = false;
            controlsLayout.padding = new RectOffset(18, 18, 9, 9);
            AddLayout(controls, -1, usesGeneratedActionTray ? 92 : 74);

            if (usesGeneratedActionTray)
            {
                CreateGameplayActionTray(controls.transform);
            }

            rollButton = CreateButton("Roll Button", controls.transform, "컵 굴리기", 26);
            if (usesGeneratedActionTray)
            {
                ConfigureGameplayActionTrayButton(rollButton, -200f);
            }
            else
            {
                ApplyGeneratedButtonSkin(rollButton, buttonPrimarySprite, ButtonColor);
            }
            rollButtonText = rollButton.GetComponentInChildren<Text>();
            if (rollButtonText != null)
            {
                rollButtonText.color = ArtDecoCream;
                ApplyTextShadow(rollButtonText, new Color32(24, 10, 8, 225), new Vector2(1f, -1f));
            }
            rollButton.onClick.AddListener(RollDie);
            if (!usesGeneratedActionTray)
            {
                AddControlButtonDepth(rollButton.gameObject);
                AddLayout(rollButton.gameObject, 190, 56);
            }

            resetButton = CreateButton("Reset Button", controls.transform, "새 게임", 26);
            if (usesGeneratedActionTray)
            {
                ConfigureGameplayActionTrayButton(resetButton, 0f);
            }
            else
            {
                ApplyGeneratedButtonSkin(resetButton, buttonPrimarySprite, ButtonColor);
            }
            resetButton.onClick.AddListener(StartNewGame);
            var resetButtonText = resetButton.GetComponentInChildren<Text>();
            if (resetButtonText != null)
            {
                resetButtonText.color = ArtDecoCream;
                ApplyTextShadow(resetButtonText, new Color32(24, 10, 8, 225), new Vector2(1f, -1f));
            }

            if (!usesGeneratedActionTray)
            {
                AddControlButtonDepth(resetButton.gameObject);
                AddLayout(resetButton.gameObject, 190, 56);
            }

            modeButton = CreateButton("Mode Select Button", controls.transform, "모드 선택", 24);
            if (usesGeneratedActionTray)
            {
                ConfigureGameplayActionTrayButton(modeButton, 200f);
            }
            else
            {
                ApplyGeneratedButtonSkin(modeButton, buttonPrimarySprite, ButtonColor);
            }
            modeButton.onClick.AddListener(ShowModeSelection);
            var modeButtonText = modeButton.GetComponentInChildren<Text>();
            if (modeButtonText != null)
            {
                modeButtonText.color = ArtDecoCream;
                ApplyTextShadow(modeButtonText, new Color32(24, 10, 8, 225), new Vector2(1f, -1f));
            }

            if (!usesGeneratedActionTray)
            {
                AddControlButtonDepth(modeButton.gameObject);
                AddLayout(modeButton.gameObject, 190, 56);
            }
        }

        private void CreateGameplayActionTray(Transform parent)
        {
            var tray = CreateUiObject("Gameplay Action Tray", parent);
            var trayImage = tray.AddComponent<Image>();
            trayImage.sprite = gameplayActionTraySprite;
            trayImage.type = Image.Type.Simple;
            trayImage.preserveAspect = false;
            trayImage.color = Color.white;
            trayImage.raycastTarget = false;

            var trayLayout = tray.AddComponent<LayoutElement>();
            trayLayout.ignoreLayout = true;
            var trayRect = tray.GetComponent<RectTransform>();
            trayRect.anchorMin = new Vector2(0.5f, 0.5f);
            trayRect.anchorMax = new Vector2(0.5f, 0.5f);
            trayRect.pivot = new Vector2(0.5f, 0.5f);
            trayRect.sizeDelta = new Vector2(624f, 86f);
            trayRect.anchoredPosition = Vector2.zero;
            tray.transform.SetAsFirstSibling();
        }

        private static void ConfigureGameplayActionTrayButton(Button button, float horizontalPosition)
        {
            if (button == null || button.image == null)
            {
                return;
            }

            // The tray supplies the shared frame.  These remain transparent hit targets
            // so each live Korean label stays centered inside its generated bay.
            button.image.color = Color.clear;
            button.transition = Selectable.Transition.None;
            var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(184f, 62f);
            rect.anchoredPosition = new Vector2(horizontalPosition, 0f);

            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = 22;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 19;
            label.resizeTextMaxSize = 22;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
        }

        private void StartNewGame()
        {
            StopGameplayCoroutines();
            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(false);
            }

            if (boardUiRoot != null)
            {
                boardUiRoot.SetActive(true);
            }

            gameStarted = true;
            if (modeSelectionOverlay != null)
            {
                modeSelectionOverlay.SetActive(false);
            }

            activePlayer = 0;
            currentDie = 0;
            shakeEnergy = 0f;
            ResetShakeTracking();
            hasPendingDie = false;
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            nextRollCanAttack = true;
            nextRollCanPlaceOnOpponent = false;
            isRolling = false;
            isWaitingForCupShake = false;
            isCupDragging = false;
            cupReleaseStarted = false;
            isAttackAnimating = false;
            isAiThinking = false;
            placementComplete = false;
            SetDiceOverlayVisible(false);
            ClearAttackAnimationLayer();

            foreach (var board in boards)
            {
                board.Clear();
            }

            ResetBoardFaceScales();
            RefreshView();
            FocusFirstAvailableAction();
        }

        private void ShowTitleScreen()
        {
            StopGameplayCoroutines();
            gameStarted = false;
            currentDie = 0;
            shakeEnergy = 0f;
            ResetShakeTracking();
            hasPendingDie = false;
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            isRolling = false;
            isWaitingForCupShake = false;
            isCupDragging = false;
            cupReleaseStarted = false;
            isAttackAnimating = false;
            isAiThinking = false;
            placementComplete = false;
            SetDiceOverlayVisible(false);
            ClearAttackAnimationLayer();

            if (modeSelectionOverlay != null)
            {
                modeSelectionOverlay.SetActive(false);
            }

            if (titleHowToPlayOverlay != null)
            {
                titleHowToPlayOverlay.SetActive(false);
            }

            if (resultBanner != null)
            {
                resultBanner.SetActive(false);
            }

            if (boardUiRoot != null)
            {
                boardUiRoot.SetActive(false);
            }

            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(true);
                titleScreenOverlay.transform.SetAsLastSibling();
            }

            Canvas.ForceUpdateCanvases();
            if (bootCoverInputBlocked)
            {
                EventSystem.current?.SetSelectedGameObject(null);
                return;
            }

            FocusTitleScreen();
        }

        private void OpenModeSelectionFromTitle()
        {
            if (titleHowToPlayOverlay != null)
            {
                titleHowToPlayOverlay.SetActive(false);
            }

            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(false);
            }

            if (boardUiRoot != null)
            {
                boardUiRoot.SetActive(true);
            }

            ShowModeSelection();
        }

        private void ShowModeSelection()
        {
            StopGameplayCoroutines();
            gameAudio?.PlayMenuOpen();
            if (titleHowToPlayOverlay != null)
            {
                titleHowToPlayOverlay.SetActive(false);
            }

            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(false);
            }

            if (boardUiRoot != null)
            {
                boardUiRoot.SetActive(true);
            }

            gameStarted = false;
            currentDie = 0;
            shakeEnergy = 0f;
            ResetShakeTracking();
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            nextRollCanAttack = true;
            nextRollCanPlaceOnOpponent = false;
            isRolling = false;
            isWaitingForCupShake = false;
            isCupDragging = false;
            cupReleaseStarted = false;
            isAttackAnimating = false;
            isAiThinking = false;
            hasPendingDie = false;
            placementComplete = false;
            SetDiceOverlayVisible(false);
            ClearAttackAnimationLayer();
            ResetBoardFaceScales();
            if (modeSelectionOverlay != null)
            {
                modeSelectionOverlay.SetActive(true);
                modeSelectionOverlay.transform.SetAsLastSibling();
            }

            RefreshView();
            if (bootCoverInputBlocked)
            {
                EventSystem.current?.SetSelectedGameObject(null);
                return;
            }

            FocusModeSelection();
        }

        private void FocusModeSelection()
        {
            if (EventSystem.current != null
                && modeSelectionOverlay != null
                && modeSelectionOverlay.activeInHierarchy
                && pveModeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(pveModeButton.gameObject);
            }
        }

        private void FocusTitleScreen()
        {
            if (titleHowToPlayOverlay != null && titleHowToPlayOverlay.activeInHierarchy)
            {
                FocusTitleHowToPlay();
                return;
            }

            if (EventSystem.current != null
                && titleScreenOverlay != null
                && titleScreenOverlay.activeInHierarchy
                && titleStartButton != null)
            {
                EventSystem.current.SetSelectedGameObject(titleStartButton.gameObject);
            }
        }

        private void OpenTitleHowToPlay()
        {
            if (titleHowToPlayOverlay == null)
            {
                return;
            }

            gameAudio?.PlayMenuOpen();
            titleHowToPlayOverlay.SetActive(true);
            titleHowToPlayOverlay.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();

            if (!bootCoverInputBlocked)
            {
                FocusTitleHowToPlay();
            }
        }

        private void CloseTitleHowToPlay()
        {
            if (titleHowToPlayOverlay == null || !titleHowToPlayOverlay.activeSelf)
            {
                return;
            }

            titleHowToPlayOverlay.SetActive(false);
            gameAudio?.PlayMenuClose();
            FocusTitleScreen();
        }

        private void FocusTitleHowToPlay()
        {
            if (EventSystem.current != null
                && titleHowToPlayOverlay != null
                && titleHowToPlayOverlay.activeInHierarchy
                && titleHowToCloseButton != null)
            {
                EventSystem.current.SetSelectedGameObject(titleHowToCloseButton.gameObject);
            }
        }

        private void FocusActiveFrontScreen()
        {
            if (titleHowToPlayOverlay != null && titleHowToPlayOverlay.activeInHierarchy)
            {
                FocusTitleHowToPlay();
                return;
            }

            if (titleScreenOverlay != null && titleScreenOverlay.activeInHierarchy)
            {
                FocusTitleScreen();
                return;
            }

            FocusModeSelection();
        }

        private void SelectMatchMode(MatchMode mode)
        {
            currentMode = mode;
            gameAudio?.PlayConfirm();
            StartNewGame();
        }

        private void RollDie()
        {
            if (!gameStarted || IsAiTurnActive() || placementComplete || hasPendingDie || isRolling || isAttackAnimating || boards[activePlayer].IsFull)
            {
                return;
            }

            isRolling = true;
            isWaitingForCupShake = true;
            isCupDragging = false;
            cupReleaseStarted = false;
            currentDie = 0;
            shakeEnergy = 0f;
            ResetShakeTracking();
            hasPendingDie = false;
            pendingDieCanAttack = nextRollCanAttack;
            pendingDieCanPlaceOnOpponent = nextRollCanPlaceOnOpponent;
            nextRollCanAttack = true;
            nextRollCanPlaceOnOpponent = false;
            gameAudio?.PlayCupReady();
            SetDiceOverlayVisible(true);
            ResetDiceStage();
        }

        private void BeginCupShake(Vector2 screenPosition)
        {
            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted)
            {
                return;
            }

            if (isCupDragging)
            {
                return;
            }

            isCupDragging = true;
            gameAudio?.StartCupShake();
            mouseCupDragActive |= Mouse.current != null && Mouse.current.leftButton.isPressed;
            cupDragStartScreenPosition = screenPosition;
            polledCupPointerPosition = screenPosition;
            hasPolledCupPointerPosition = true;
            cupDragStartLocalPosition = diceCupTransform != null ? cupTargetLocalPosition : CupHomePosition;
            cupPointerGrabOffset = TryGetCupPointerStagePosition(screenPosition, out var pointerPosition)
                ? cupDragStartLocalPosition - pointerPosition
                : Vector3.zero;
        }

        private void DragCupShake(Vector2 screenPosition, Vector2 delta)
        {
            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted)
            {
                return;
            }

            if (!isCupDragging)
            {
                BeginCupShake(screenPosition);
            }

            if (lastCupDragInputFrame == Time.frameCount)
            {
                return;
            }

            lastCupDragInputFrame = Time.frameCount;
            lastProcessedCupPointerPosition = screenPosition;

            RecordShakeDelta(delta);
            shakeEnergy = Mathf.Min(2.35f, shakeEnergy + Mathf.Max(0.025f, delta.magnitude * 0.009f));
            gameAudio?.UpdateCupShake(shakeEnergy);
            ApplyCupShakePose(screenPosition, delta);
        }

        private void EndCupShake()
        {
            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted)
            {
                return;
            }

            isCupDragging = false;
            hasPolledCupPointerPosition = false;
            mouseCupDragActive = false;
            isWaitingForCupShake = false;
            cupReleaseStarted = true;
            shakeEnergy = Mathf.Max(shakeEnergy, 0.18f);
            gameAudio?.StopCupShake();
            StartCoroutine(FinishPlayerCupShakeAndRoll(BuildShakeMotionSeed()));
        }

        private void CancelCupShake()
        {
            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted || IsAiTurnActive())
            {
                return;
            }

            nextRollCanAttack = pendingDieCanAttack;
            nextRollCanPlaceOnOpponent = pendingDieCanPlaceOnOpponent;
            currentDie = 0;
            hasPendingDie = false;
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            isRolling = false;
            isWaitingForCupShake = false;
            isCupDragging = false;
            cupReleaseStarted = false;
            shakeEnergy = 0f;
            gameAudio?.StopCupShake();
            gameAudio?.PlayError();
            ResetShakeTracking();
            SetDiceOverlayVisible(false);
            RefreshView();
            FocusFirstAvailableAction();
        }

        private IEnumerator FinishPlayerCupShakeAndRoll(int motionSeed)
        {
            const float finishShakeDuration = 0.14f;
            const float finishShakeFrequency = 5.5f;
            const float uprightDuration = 0.03f;
            var startPosition = cupTargetLocalPosition;
            var startRotation = cupTargetLocalRotation;
            var shake01 = Mathf.InverseLerp(0.18f, 2.35f, shakeEnergy + shakeDistance * 0.002f);
            var amplitudeX = Mathf.Lerp(0.16f, 0.19f, shake01);
            var amplitudeZ = Mathf.Lerp(0.09f, 0.12f, shake01);
            var basePosition = new Vector3(
                Mathf.Clamp(startPosition.x, CupDragMinX + amplitudeX, CupDragMaxX - amplitudeX),
                Mathf.Max(startPosition.y, CupHomePosition.y + 0.06f),
                Mathf.Clamp(startPosition.z, CupDragMinZ + amplitudeZ, CupDragMaxZ - amplitudeZ));
            var elapsed = 0f;

            while (elapsed < uprightDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / uprightDuration));
                SetCupPose(
                    Vector3.Lerp(startPosition, basePosition, progress),
                    Quaternion.Slerp(startRotation, Quaternion.identity, progress),
                    false);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < finishShakeDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / finishShakeDuration);
                var envelope = Mathf.Sin(progress * Mathf.PI);
                var phase = elapsed * Mathf.PI * 2f * finishShakeFrequency;
                var horizontalWave = Mathf.Sin(phase);
                var depthWave = Mathf.Cos(phase * 0.82f);
                var shakePosition = basePosition + new Vector3(
                    horizontalWave * amplitudeX * envelope,
                    Mathf.Abs(Mathf.Sin(phase * 1.4f)) * 0.025f * envelope,
                    depthWave * amplitudeZ * envelope);
                var shakeRotation = Quaternion.Euler(
                    depthWave * 6f * envelope,
                    horizontalWave * 5f * envelope,
                    -horizontalWave * 9.5f * envelope);
                SetCupPose(shakePosition, shakeRotation, false);
                yield return null;
            }

            SetCupPose(basePosition, Quaternion.identity, false);
            yield return WaitForPhysicsStep;
            yield return RollDieRoutine(motionSeed);
        }

        private int BuildShakeMotionSeed()
        {
            var measuredMotion = Mathf.FloorToInt(
                shakeEnergy * 31f +
                shakeDistance * 0.13f +
                peakShakeDelta * 0.71f +
                shakeDirectionChanges * 17f);
            return unchecked(measuredMotion * 397 ^ Random.Range(1, int.MaxValue));
        }

        private void ResetShakeTracking()
        {
            shakeDistance = 0f;
            peakShakeDelta = 0f;
            shakeDirectionChanges = 0;
            lastShakeDirection = Vector2.zero;
            lastCupDragInputFrame = -1;
            lastProcessedCupPointerPosition = Vector2.zero;
            hasPolledCupPointerPosition = false;
            mouseCupDragActive = false;
        }

        private void RecordShakeDelta(Vector2 delta)
        {
            var distance = delta.magnitude;
            shakeDistance += distance;
            peakShakeDelta = Mathf.Max(peakShakeDelta, distance);
            if (distance < 2f)
            {
                return;
            }

            var direction = delta / distance;
            if (lastShakeDirection.sqrMagnitude > 0.01f && Vector2.Dot(lastShakeDirection, direction) < 0.25f)
            {
                shakeDirectionChanges++;
            }

            lastShakeDirection = direction;
        }

        private void CommitRolledDie(int value)
        {
            currentDie = Mathf.Clamp(value, 1, 6);
        }

        private void ApplyCupShakePose(Vector2 screenPosition, Vector2 delta)
        {
            if (diceCupTransform == null)
            {
                return;
            }

            var screenWidth = Mathf.Max(1f, Screen.width);
            var screenHeight = Mathf.Max(1f, Screen.height);
            var dragOffset = screenPosition - cupDragStartScreenPosition;
            var pointerVelocity = delta / Mathf.Max(Time.unscaledDeltaTime, 1f / 240f);
            var lift = Mathf.Lerp(0.018f, 0.075f, Mathf.InverseLerp(80f, 900f, pointerVelocity.magnitude));
            var cupPosition = TryGetCupPointerStagePosition(screenPosition, out var pointerPosition)
                ? pointerPosition + cupPointerGrabOffset
                : cupDragStartLocalPosition + new Vector3(
                    dragOffset.x / screenWidth * 3.1f,
                    0f,
                    dragOffset.y / screenHeight * 2.25f);
            cupPosition.y = CupHomePosition.y + lift;
            cupPosition.x = Mathf.Clamp(cupPosition.x, CupDragMinX, CupDragMaxX);
            cupPosition.z = Mathf.Clamp(cupPosition.z, CupDragMinZ, CupDragMaxZ);

            var tiltX = Mathf.Clamp(pointerVelocity.y * 0.007f, -10f, 10f);
            var tiltY = Mathf.Clamp(pointerVelocity.x * 0.0035f, -7f, 7f);
            var tiltZ = Mathf.Clamp(-pointerVelocity.x * 0.0085f, -14f, 14f);
            var cupRotation = Quaternion.Euler(tiltX, tiltY, tiltZ);
            SetCupPose(cupPosition, cupRotation, true);
        }

        private bool TryGetCupPointerStagePosition(Vector2 screenPosition, out Vector3 localPosition)
        {
            localPosition = Vector3.zero;
            if (diceCamera == null || diceOutputImage == null || diceStageRoot == null)
            {
                return false;
            }

            var outputRect = diceOutputImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(outputRect, screenPosition, null, out var localPointer))
            {
                return false;
            }

            var rect = outputRect.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return false;
            }

            var viewportPoint = new Vector3(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPointer.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPointer.y),
                0f);
            var ray = diceCamera.ViewportPointToRay(viewportPoint);
            var cupPlane = new Plane(
                diceStageRoot.up,
                diceStageRoot.TransformPoint(new Vector3(0f, CupHomePosition.y, 0f)));
            if (!cupPlane.Raycast(ray, out var distance))
            {
                return false;
            }

            localPosition = diceStageRoot.InverseTransformPoint(ray.GetPoint(distance));
            return true;
        }

        private IEnumerator RollDieRoutine(int finalDie)
        {
            worldDieHasTouchedSurface = false;
            gameAudio?.StopCupShake();
            gameAudio?.PlayDiceThrow();
            var startCupPosition = diceCupTransform.localPosition;
            var startCupRotation = diceCupTransform.localRotation;
            yield return PourDieFromCupRoutine(startCupPosition, startCupRotation);
            yield return RollDieOnTableRoutine(finalDie);
            CommitRolledDie(GetWorldDieTopFaceValue());
            hasPendingDie = true;
            yield return new WaitForSeconds(0.12f);

            isRolling = false;
            SetDiceOverlayVisible(false);
            RefreshView();
            FocusFirstAvailableAction();
        }

        private IEnumerator PourDieFromCupRoutine(Vector3 startCupPosition, Quaternion startCupRotation)
        {
            const float pourDuration = 0.22f;
            var pourRotation = Quaternion.Euler(CupPourRotation);
            var elapsed = 0f;
            while (elapsed < pourDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / pourDuration);
                var eased = Mathf.SmoothStep(0f, 1f, progress);
                var cupPosition = Vector3.Lerp(startCupPosition, CupReleasePosition, eased);
                var cupRotation = Quaternion.Slerp(startCupRotation, pourRotation, eased);
                SetCupPose(cupPosition, cupRotation, false);
                yield return null;
            }

            SetCupPose(CupReleasePosition, pourRotation, false);
            yield return WaitForPhysicsStep;
        }

        private IEnumerator RollDieOnTableRoutine(int spinSeedValue)
        {
            if (worldDieTransform == null || worldDieBody == null)
            {
                yield break;
            }

            PrepareWorldDieForPhysics(true);
            var startPosition = worldDieTransform.localPosition;
            var horizontalDirection = DieResultPosition - startPosition;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude < 0.01f)
            {
                horizontalDirection = Vector3.left;
            }

            horizontalDirection.Normalize();
            var shake01 = Mathf.InverseLerp(0.18f, 2.35f, shakeEnergy + shakeDistance * 0.002f);
            var rollDirection = horizontalDirection;
            if (diceCupTransform != null && diceStageRoot != null)
            {
                var cupOpeningDirection = diceStageRoot.InverseTransformDirection(diceCupTransform.up);
                cupOpeningDirection.y = 0f;
                if (cupOpeningDirection.sqrMagnitude > 0.01f)
                {
                    rollDirection = (horizontalDirection * 0.78f + cupOpeningDirection.normalized * 0.22f).normalized;
                }
            }

            var rollWorldDirection = diceStageRoot != null
                ? diceStageRoot.TransformDirection(rollDirection).normalized
                : rollDirection.normalized;
            var lateralWorldDirection = Vector3.Cross(Vector3.up, rollWorldDirection).normalized;
            var desiredPlanarSpeed = Mathf.Lerp(1.9f, 2.45f, shake01)
                + Mathf.Min(0.2f, shakeDirectionChanges * 0.015f);
            var desiredPlanarVelocity = rollWorldDirection * desiredPlanarSpeed
                + lateralWorldDirection * Random.Range(-0.16f, 0.16f);
            desiredPlanarVelocity = Vector3.ClampMagnitude(desiredPlanarVelocity, 2.65f);
            var desiredVerticalSpeed = Mathf.Lerp(0.3f, 0.46f, shake01);
            var desiredLaunchVelocity = desiredPlanarVelocity + Vector3.up * desiredVerticalSpeed;
            worldDieBody.AddForce(
                desiredLaunchVelocity - worldDieBody.linearVelocity,
                ForceMode.VelocityChange);

            var spinAxis = Vector3.Cross(Vector3.up, rollWorldDirection).normalized;
            var compactSeed = spinSeedValue % 997;
            var spinVariance = new Vector3(
                Mathf.Sin(compactSeed * 1.73f) * 1.5f,
                Mathf.Cos(compactSeed * 2.19f) * 2f,
                Mathf.Sin(compactSeed * 2.61f) * 1.5f);
            if (diceStageRoot != null)
            {
                spinVariance = diceStageRoot.TransformDirection(spinVariance);
            }

            var desiredAngularVelocity = spinAxis * Mathf.Lerp(11f, 16f, shake01)
                + spinVariance
                + Random.insideUnitSphere * Mathf.Lerp(1.4f, 2.6f, shake01);
            worldDieBody.AddTorque(
                Vector3.ClampMagnitude(desiredAngularVelocity - worldDieBody.angularVelocity, 18f),
                ForceMode.VelocityChange);

            const float minRollDuration = 0.45f;
            const float stableDurationRequired = 0.07f;
            const float maxRollDuration = 1.1f;
            var elapsed = 0f;
            var stableDuration = 0f;
            var appliedCupExitAssist = false;
            while (elapsed < maxRollDuration && stableDuration < stableDurationRequired)
            {
                yield return WaitForPhysicsStep;
                elapsed += Time.fixedDeltaTime;
                RecoverEscapedWorldDie();

                var currentPlanarVelocity = Vector3.ProjectOnPlane(worldDieBody.linearVelocity, Vector3.up);
                var dieCupDistance = diceCupTransform != null
                    ? Vector2.Distance(
                        new Vector2(worldDieTransform.localPosition.x, worldDieTransform.localPosition.z),
                        new Vector2(diceCupTransform.localPosition.x, diceCupTransform.localPosition.z))
                    : float.MaxValue;
                var needsSlowExitAssist = !worldDieHasTouchedSurface
                    && currentPlanarVelocity.magnitude < 0.85f
                    && (IsWorldDieInsideCup() || dieCupDistance < 0.75f);
                if (!appliedCupExitAssist && elapsed >= 0.3f && needsSlowExitAssist)
                {
                    appliedCupExitAssist = true;
                    var assistPlanarVelocity = rollWorldDirection * 1.25f;
                    var assistVerticalVelocity = Mathf.Max(worldDieBody.linearVelocity.y, 0.18f);
                    worldDieBody.AddForce(
                        assistPlanarVelocity + Vector3.up * assistVerticalVelocity - worldDieBody.linearVelocity,
                        ForceMode.VelocityChange);
                    var assistAngularVelocity = spinAxis * 8f + Random.onUnitSphere * 1.2f;
                    worldDieBody.AddTorque(
                        assistAngularVelocity - worldDieBody.angularVelocity,
                        ForceMode.VelocityChange);
                }

                if (elapsed > 0.55f)
                {
                    var assist = Mathf.InverseLerp(0.55f, 0.92f, elapsed);
                    worldDieBody.linearDamping = Mathf.Lerp(0.18f, 2.1f, assist);
                    worldDieBody.angularDamping = Mathf.Lerp(0.1f, 1.9f, assist);
                }

                var isStable = elapsed >= minRollDuration
                    && worldDieHasTouchedSurface
                    && worldDieBody.linearVelocity.sqrMagnitude < 0.01f
                    && worldDieBody.angularVelocity.sqrMagnitude < 0.04f
                    && GetWorldDieTopFaceDot() > 0.9f;
                stableDuration = isStable ? stableDuration + Time.fixedDeltaTime : 0f;
                if (stableDuration >= stableDurationRequired)
                {
                    break;
                }

            }

            var assistedSettleElapsed = 0f;
            worldDieBody.linearDamping = 4f;
            worldDieBody.angularDamping = 3.6f;
            while (assistedSettleElapsed < 0.08f
                && (worldDieBody.linearVelocity.sqrMagnitude > 0.0064f
                    || worldDieBody.angularVelocity.sqrMagnitude > 0.0225f))
            {
                yield return WaitForPhysicsStep;
                assistedSettleElapsed += Time.fixedDeltaTime;
                RecoverEscapedWorldDie();
            }

            var needsRestCorrection = !worldDieHasTouchedSurface
                || IsWorldDieInsideCup()
                || !IsWorldDieInsidePlayableTray()
                || worldDieTransform.localPosition.y < 0.08f
                || worldDieTransform.localPosition.y > 0.48f
                || GetWorldDieTopFaceDot() < 0.9f
                || worldDieBody.linearVelocity.sqrMagnitude > 0.01f
                || worldDieBody.angularVelocity.sqrMagnitude > 0.04f;
            if (needsRestCorrection)
            {
                yield return StabilizeWorldDieRestPose(GetWorldDieTopFaceValue());
            }

            if (!worldDieBody.isKinematic)
            {
                worldDieBody.linearVelocity = Vector3.zero;
                worldDieBody.angularVelocity = Vector3.zero;
                worldDieBody.Sleep();
            }

            worldDieBody.useGravity = false;
            worldDieBody.isKinematic = true;
            worldDieBody.constraints = RigidbodyConstraints.None;
        }

        private IEnumerator StabilizeWorldDieRestPose(int faceValue)
        {
            if (worldDieTransform == null || worldDieBody == null)
            {
                yield break;
            }

            var startPosition = worldDieTransform.localPosition;
            var startRotation = worldDieTransform.localRotation;
            var targetPosition = new Vector3(
                Mathf.Clamp(startPosition.x, -1.45f, 0.75f),
                0.17f,
                Mathf.Clamp(startPosition.z, -0.64f, 0.68f));
            if (IsWorldDieInsideCup() || startPosition.y > 0.48f)
            {
                targetPosition = new Vector3(DieResultPosition.x, 0.17f, DieResultPosition.z);
            }

            var targetRotation = GetWorldDieRotationForFace(faceValue, startRotation.eulerAngles.y);
            if (!worldDieBody.isKinematic)
            {
                worldDieBody.linearVelocity = Vector3.zero;
                worldDieBody.angularVelocity = Vector3.zero;
            }

            worldDieBody.useGravity = false;
            worldDieBody.isKinematic = true;

            const float correctionDuration = 0.05f;
            var elapsed = 0f;
            while (elapsed < correctionDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / correctionDuration));
                worldDieTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
                worldDieTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                yield return null;
            }

            worldDieTransform.localPosition = targetPosition;
            worldDieTransform.localRotation = targetRotation;
        }

        private void HandleCellClick(int playerIndex, int row, int column)
        {
            if (IsAiTurnActive())
            {
                return;
            }

            if (CanPlaceDieOnBoard(playerIndex, row, column))
            {
                PlaceDie(playerIndex, row, column);
                return;
            }

            AttackDie(playerIndex, row, column);
        }

        private void PlaceDie(int playerIndex, int row, int column)
        {
            if (!CanPlaceDieOnBoard(playerIndex, row, column))
            {
                return;
            }

            var placedColumn = boards[playerIndex].Place(row, column, currentDie, !pendingDieCanAttack);
            gameAudio?.PlayDiePlaced();
            currentDie = 0;
            hasPendingDie = false;
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            nextRollCanAttack = true;
            nextRollCanPlaceOnOpponent = false;

            AdvanceTurn();
            RefreshView();
            StartPlacementPulse(playerIndex, row, placedColumn);
            FocusFirstAvailableAction();
        }

        private void AttackDie(int targetPlayer, int row, int column)
        {
            if (!CanAttackCell(targetPlayer, row, column))
            {
                return;
            }

            StartCoroutine(AttackDieRoutine(targetPlayer, row, column));
        }

        private IEnumerator AttackDieRoutine(int targetPlayer, int row, int column)
        {
            var targetColumns = boards[targetPlayer].GetMatchingGroupColumns(row, column);
            if (targetColumns.Length == 0)
            {
                yield break;
            }

            var attackValue = currentDie;
            isAttackAnimating = true;
            RefreshView();
            yield return PlayAttackCollisionAnimation(targetPlayer, row, targetColumns, attackValue);

            var removed = boards[targetPlayer].RemoveMatchingGroup(row, column);
            if (removed <= 0)
            {
                isAttackAnimating = false;
                ClearAttackAnimationLayer();
                RefreshView();
                FocusFirstAvailableAction();
                yield break;
            }

            currentDie = 0;
            hasPendingDie = false;
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            nextRollCanAttack = false;
            nextRollCanPlaceOnOpponent = true;
            isAttackAnimating = false;
            ClearAttackAnimationLayer();
            RefreshView();
            FocusFirstAvailableAction();
        }

        private IEnumerator PlayAttackCollisionAnimation(int targetPlayer, int row, int[] targetColumns, int attackValue)
        {
            if (attackAnimationLayer == null || targetColumns.Length == 0)
            {
                yield break;
            }

            ClearAttackAnimationLayer();
            attackAnimationLayer.SetActive(true);

            var targetBoard = boards[targetPlayer];
            var layerRect = attackAnimationLayer.GetComponent<RectTransform>();
            var firstTargetFace = targetBoard.CellFaceImages[row, targetColumns[0]];
            var targetPoint = firstTargetFace != null
                ? GetPointInLayer(firstTargetFace.rectTransform, layerRect)
                : Vector2.zero;
            var attackStart = GetAttackStartPointInLayer(row, targetPoint, layerRect);
            var direction = targetPoint - attackStart;
            if (direction.sqrMagnitude < 1f)
            {
                direction = targetPlayer == 0 ? Vector2.left : Vector2.right;
            }

            direction.Normalize();
            var perpendicular = new Vector2(-direction.y, direction.x);
            var impactPoint = targetPoint - direction * 34f;
            var attackDie = CreateFlyingDie("Attack Flying Die", attackValue, false, attackStart, new Vector2(72f, 72f));
            var attackGroup = attackDie.GetComponent<CanvasGroup>();

            var targetDice = new RectTransform[targetColumns.Length];
            var targetGroups = new CanvasGroup[targetColumns.Length];
            var targetStarts = new Vector2[targetColumns.Length];
            for (var index = 0; index < targetColumns.Length; index++)
            {
                var column = targetColumns[index];
                var face = targetBoard.CellFaceImages[row, column];
                if (face == null)
                {
                    continue;
                }

                var start = GetPointInLayer(face.rectTransform, layerRect);
                targetStarts[index] = start;
                targetDice[index] = CreateFlyingDie($"Target Flying Die {index + 1}", attackValue, false, start, face.rectTransform.rect.size);
                targetGroups[index] = targetDice[index].GetComponent<CanvasGroup>();
                face.gameObject.SetActive(false);
            }

            const float approachDuration = 0.24f;
            var elapsed = 0f;
            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / approachDuration));
                attackDie.anchoredPosition = Vector2.Lerp(attackStart, impactPoint, progress);
                attackDie.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 130f, progress));
                attackDie.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }

            gameAudio?.PlayAttackImpact();
            const float knockDuration = 0.48f;
            elapsed = 0f;
            var attackEnd = impactPoint + direction * 230f + perpendicular * 42f;
            var attackBounceStart = attackDie.anchoredPosition;
            while (elapsed < knockDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / knockDuration);
                var eased = 1f - Mathf.Pow(1f - progress, 2.4f);
                var lift = Mathf.Sin(progress * Mathf.PI) * 38f;

                attackDie.anchoredPosition = Vector2.Lerp(attackBounceStart, attackEnd, eased) + Vector2.up * lift;
                attackDie.localEulerAngles = new Vector3(0f, 0f, 130f + eased * 420f);
                if (attackGroup != null)
                {
                    attackGroup.alpha = 1f - Mathf.Clamp01((progress - 0.46f) / 0.54f);
                }

                for (var index = 0; index < targetDice.Length; index++)
                {
                    var die = targetDice[index];
                    if (die == null)
                    {
                        continue;
                    }

                    var spread = (index - (targetDice.Length - 1) * 0.5f) * 56f;
                    var end = targetStarts[index] + direction * (210f + index * 38f) + perpendicular * spread + Vector2.up * (26f + index * 14f);
                    die.anchoredPosition = Vector2.Lerp(targetStarts[index], end, eased) + Vector2.up * lift;
                    die.localEulerAngles = new Vector3(0f, 0f, eased * (360f + index * 90f));
                    die.localScale = Vector3.one * Mathf.Lerp(1.04f, 0.72f, progress);
                    if (targetGroups[index] != null)
                    {
                        targetGroups[index].alpha = 1f - Mathf.Clamp01((progress - 0.36f) / 0.64f);
                    }
                }

                yield return null;
            }

            ClearAttackAnimationLayer();
        }

        private RectTransform CreateFlyingDie(string name, int value, bool attackProtected, Vector2 anchoredPosition, Vector2 size)
        {
            var dieObject = CreateUiObject(name, attackAnimationLayer.transform);
            var dieImage = dieObject.AddComponent<Image>();
            dieImage.sprite = diceFaceArtSprite != null ? diceFaceArtSprite : diceFaceSprite;
            dieImage.type = Image.Type.Simple;
            dieImage.preserveAspect = false;
            dieImage.color = GetDiceFaceArtTint(GetCellFaceColor(value, attackProtected));
            dieImage.raycastTarget = false;
            var canvasGroup = dieObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            var shadow = dieObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(3f, -3f);
            var outline = dieObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.12f);
            outline.effectDistance = new Vector2(1f, -1f);

            var dieRect = dieObject.GetComponent<RectTransform>();
            dieRect.anchorMin = new Vector2(0.5f, 0.5f);
            dieRect.anchorMax = new Vector2(0.5f, 0.5f);
            dieRect.pivot = new Vector2(0.5f, 0.5f);
            dieRect.sizeDelta = size;
            dieRect.anchoredPosition = anchoredPosition;

            if (diceFaceArtSprite == null)
            {
                CreateDiceCeramicInlay(dieRect, 0.1f, 0.34f);
            }
            CreateFlyingDiePips(dieRect, value, attackProtected);
            return dieRect;
        }

        private void CreateFlyingDiePips(Transform die, int value, bool attackProtected)
        {
            var size = ((RectTransform)die).sizeDelta.x;
            var offset = size * 0.26f;
            var pipSize = size * 0.18f;
            var positions = new[]
            {
                Vector2.zero,
                new Vector2(-offset, offset),
                new Vector2(offset, offset),
                new Vector2(-offset, 0f),
                new Vector2(offset, 0f),
                new Vector2(-offset, -offset),
                new Vector2(offset, -offset)
            };

            var pipColor = diceFaceArtSprite != null && !attackProtected
                ? (Color)new Color32(31, 43, 39, 255)
                : attackProtected || value == 6 ? Color.white : (Color)new Color32(31, 35, 39, 255);
            for (var index = 0; index < positions.Length; index++)
            {
                if (!ShouldShowPip(value, index))
                {
                    continue;
                }

                var pipObject = CreateUiObject($"Flying Pip {index}", die);
                var pipImage = pipObject.AddComponent<Image>();
                pipImage.sprite = dicePipSprite;
                pipImage.color = pipColor;
                pipImage.raycastTarget = false;
                var pipShadow = pipObject.AddComponent<Shadow>();
                pipShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                pipShadow.effectDistance = new Vector2(0.8f, -0.8f);

                var pipRect = pipObject.GetComponent<RectTransform>();
                pipRect.anchorMin = new Vector2(0.5f, 0.5f);
                pipRect.anchorMax = new Vector2(0.5f, 0.5f);
                pipRect.sizeDelta = new Vector2(pipSize, pipSize);
                pipRect.anchoredPosition = positions[index];
            }
        }

        private Vector2 GetAttackStartPointInLayer(int row, Vector2 targetPoint, RectTransform layerRect)
        {
            var sourceBoard = boards[activePlayer];
            var panelRect = sourceBoard.PanelImage != null ? sourceBoard.PanelImage.rectTransform : null;
            if (panelRect == null)
            {
                return drawnDieFaceImage != null
                    ? GetPointInLayer(drawnDieFaceImage.rectTransform, layerRect)
                    : targetPoint;
            }

            var panelCorners = new Vector3[4];
            panelRect.GetWorldCorners(panelCorners);
            var edgeWorldPoint = activePlayer == 0
                ? Vector3.Lerp(panelCorners[2], panelCorners[3], 0.5f)
                : Vector3.Lerp(panelCorners[0], panelCorners[1], 0.5f);
            var sourcePoint = GetWorldPointInLayer(edgeWorldPoint, layerRect);
            var sourceColumn = activePlayer == 0 ? BoardSize - 1 : 0;
            var sourceButton = sourceBoard.CellButtons[row, sourceColumn];
            if (sourceButton != null)
            {
                sourcePoint.y = GetPointInLayer(sourceButton.GetComponent<RectTransform>(), layerRect).y;
            }

            var towardOpponent = activePlayer == 0 ? Vector2.right : Vector2.left;
            return sourcePoint + towardOpponent * 28f;
        }

        private Vector2 GetPointInLayer(RectTransform source, RectTransform layerRect)
        {
            return GetWorldPointInLayer(source.TransformPoint(source.rect.center), layerRect);
        }

        private Vector2 GetWorldPointInLayer(Vector3 worldPoint, RectTransform layerRect)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRect, screenPoint, null, out var localPoint);
            return localPoint;
        }

        private void ClearAttackAnimationLayer()
        {
            if (attackAnimationLayer == null)
            {
                return;
            }

            for (var index = attackAnimationLayer.transform.childCount - 1; index >= 0; index--)
            {
                Destroy(attackAnimationLayer.transform.GetChild(index).gameObject);
            }

            attackAnimationLayer.SetActive(false);
        }

        private bool CanPlaceDieOnBoard(int playerIndex, int row, int column)
        {
            if (!gameStarted || placementComplete || isRolling || isAttackAnimating || !hasPendingDie || currentDie <= 0)
            {
                return false;
            }

            if (playerIndex == activePlayer)
            {
                return boards[playerIndex].CanPlace(row, column);
            }

            return pendingDieCanPlaceOnOpponent && !pendingDieCanAttack && boards[playerIndex].CanPlace(row, column);
        }

        private bool IsAiTurnActive()
        {
            return gameStarted && currentMode == MatchMode.Pve && activePlayer == AiPlayerIndex && !placementComplete;
        }

        private void TryStartAiTurn()
        {
            if (!IsAiTurnActive() || isAiThinking || isRolling || isAttackAnimating)
            {
                return;
            }

            isAiThinking = true;
            StartCoroutine(PlayerTwoAiRoutine());
        }

        private IEnumerator PlayerTwoAiRoutine()
        {
            yield return new WaitForSeconds(0.45f);

            while (IsAiTurnActive())
            {
                if (placementComplete)
                {
                    break;
                }

                if (isRolling || isAttackAnimating)
                {
                    yield return null;
                    continue;
                }

                if (!hasPendingDie)
                {
                    if (boards[activePlayer].IsFull)
                    {
                        AdvanceTurn();
                        RefreshView();
                        break;
                    }

                    yield return AiRollDieRoutine();
                    yield return new WaitForSeconds(0.28f);
                    continue;
                }

                yield return new WaitForSeconds(0.28f);
                if (pendingDieCanAttack && TryChooseAiAttack(out var attackTargetPlayer, out var attackRow, out var attackColumn))
                {
                    AttackDie(attackTargetPlayer, attackRow, attackColumn);
                    while (isAttackAnimating)
                    {
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.35f);
                    continue;
                }

                if (TryChooseAiPlacement(out var placementPlayer, out var placementRow, out var placementColumn))
                {
                    PlaceDie(placementPlayer, placementRow, placementColumn);
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                hasPendingDie = false;
                pendingDieCanAttack = false;
                pendingDieCanPlaceOnOpponent = false;
                nextRollCanAttack = true;
                nextRollCanPlaceOnOpponent = false;
                AdvanceTurn();
                RefreshView();
                break;
            }

            isAiThinking = false;
            FocusFirstAvailableAction();
        }

        private IEnumerator AiRollDieRoutine()
        {
            isRolling = true;
            isWaitingForCupShake = false;
            isCupDragging = false;
            cupReleaseStarted = false;
            currentDie = 0;
            ResetShakeTracking();
            shakeEnergy = 0f;
            hasPendingDie = false;
            pendingDieCanAttack = nextRollCanAttack;
            pendingDieCanPlaceOnOpponent = nextRollCanPlaceOnOpponent;
            nextRollCanAttack = true;
            nextRollCanPlaceOnOpponent = false;
            gameAudio?.PlayCupReady();
            gameAudio?.StartCupShake();
            SetDiceOverlayVisible(true);
            ResetDiceStage();

            yield return new WaitForSeconds(0.12f);
            yield return AnimateAiCupShakeRoutine();
            cupReleaseStarted = true;
            yield return RollDieRoutine(BuildShakeMotionSeed());
        }

        private IEnumerator AnimateAiCupShakeRoutine()
        {
            const float duration = 0.54f;
            var intensity = Random.Range(0.78f, 1.18f);
            var elapsed = 0f;
            var previousOffset = Vector3.zero;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var envelope = Mathf.Sin(progress * Mathf.PI);
                var offset = new Vector3(
                    Mathf.Sin(progress * Mathf.PI * 7.5f) * 0.22f,
                    Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 5f)) * 0.035f,
                    Mathf.Sin(progress * Mathf.PI * 5.5f + 0.7f) * 0.15f) * intensity * envelope;
                var movement = offset - previousOffset;
                var syntheticDelta = new Vector2(movement.x, movement.z) * 260f;
                RecordShakeDelta(syntheticDelta);
                shakeEnergy = Mathf.Min(2.35f, shakeEnergy + syntheticDelta.magnitude * 0.0065f);

                var rotation = Quaternion.Euler(
                    Mathf.Sin(progress * Mathf.PI * 6f) * 7f * intensity,
                    Mathf.Cos(progress * Mathf.PI * 5f) * 8f * intensity,
                    -Mathf.Sin(progress * Mathf.PI * 8f) * 11f * intensity);
                SetCupPose(CupHomePosition + offset, rotation, false);
                previousOffset = offset;
                yield return null;
            }

            shakeEnergy = Mathf.Max(shakeEnergy, 0.8f);
        }

        private bool TryChooseAiAttack(out int targetPlayer, out int row, out int column)
        {
            targetPlayer = 0;
            row = -1;
            column = -1;
            var bestScore = -1;
            for (var testRow = 0; testRow < BoardSize; testRow++)
            {
                for (var testColumn = 0; testColumn < BoardSize; testColumn++)
                {
                    if (!CanAttackCell(targetPlayer, testRow, testColumn))
                    {
                        continue;
                    }

                    var score = EvaluateAiAttack(targetPlayer, testRow, testColumn);
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    row = testRow;
                    column = testColumn;
                }
            }

            return row >= 0;
        }

        private bool TryChooseAiPlacement(out int targetPlayer, out int row, out int column)
        {
            if (IsAiSmallBonusPlacement() && TryChooseAiPlacementOnBoard(0, out targetPlayer, out row, out column))
            {
                return true;
            }

            if (TryChooseAiPlacementOnBoard(AiPlayerIndex, out targetPlayer, out row, out column))
            {
                return true;
            }

            return TryChooseAiPlacementOnBoard(0, out targetPlayer, out row, out column);
        }

        private bool IsAiSmallBonusPlacement()
        {
            return IsAiTurnActive()
                && ShouldAiPlaceBonusOnOpponent(currentDie, pendingDieCanPlaceOnOpponent, pendingDieCanAttack);
        }

        internal static bool ShouldAiPlaceBonusOnOpponent(int dieValue, bool canPlaceOnOpponent, bool canAttack)
        {
            return canPlaceOnOpponent
                && !canAttack
                && dieValue > 0
                && dieValue <= AiSmallBonusDieMaxValue;
        }

        private bool TryChooseAiPlacementOnBoard(int boardPlayer, out int targetPlayer, out int row, out int column)
        {
            targetPlayer = boardPlayer;
            row = -1;
            column = -1;
            var bestScore = int.MinValue;
            for (var testRow = 0; testRow < BoardSize; testRow++)
            {
                for (var testColumn = 0; testColumn < BoardSize; testColumn++)
                {
                    if (!CanPlaceDieOnBoard(boardPlayer, testRow, testColumn))
                    {
                        continue;
                    }

                    var score = EvaluateAiPlacement(boardPlayer, testRow);
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    row = testRow;
                    column = testColumn;
                }
            }

            return row >= 0;
        }

        private int EvaluateAiPlacement(int boardPlayer, int row)
        {
            var sameValueCount = 0;
            for (var column = 0; column < BoardSize; column++)
            {
                if (boards[boardPlayer].Cells[row, column] == currentDie)
                {
                    sameValueCount++;
                }
            }

            var aiBefore = boards[AiPlayerIndex].CalculateSectionScoreUnits(row);
            var humanBefore = boards[0].CalculateSectionScoreUnits(row);
            var beforeOutcome = EvaluateSectionForAi(aiBefore, humanBefore);
            var beforeMatch = EvaluateProjectedMatchForAi(row, aiBefore, humanBefore);
            var aiAfter = aiBefore;
            var humanAfter = humanBefore;
            if (boardPlayer == AiPlayerIndex)
            {
                aiAfter = CalculateSectionScoreUnitsWithExtra(boards[AiPlayerIndex], row, currentDie);
            }
            else
            {
                humanAfter = CalculateSectionScoreUnitsWithExtra(boards[0], row, currentDie);
            }

            var afterOutcome = EvaluateSectionForAi(aiAfter, humanAfter);
            var afterMatch = EvaluateProjectedMatchForAi(row, aiAfter, humanAfter);
            var scoreGain = boardPlayer == AiPlayerIndex ? aiAfter - aiBefore : humanBefore - humanAfter;
            var ownershipBias = boardPlayer == AiPlayerIndex ? 190 : -240;
            var duplicateBonus = boardPlayer == AiPlayerIndex
                ? sameValueCount * sameValueCount * 72
                : -sameValueCount * sameValueCount * 96;
            var swingBonus = afterOutcome - beforeOutcome;
            var matchSwingBonus = afterMatch - beforeMatch;
            var fillPressure = boardPlayer == AiPlayerIndex
                ? boards[boardPlayer].CountSectionFilled(row) * 10
                : -boards[boardPlayer].CountSectionFilled(row) * 12;
            var almostFullBonus = boards[boardPlayer].CountSectionFilled(row) == BoardSize - 1 && boardPlayer == AiPlayerIndex ? 120 : 0;
            var attackRiskPenalty = boardPlayer == AiPlayerIndex && pendingDieCanAttack
                ? EstimateAiAttackRisk(row, currentDie) * 34
                : 0;
            return ownershipBias + duplicateBonus + swingBonus + matchSwingBonus + scoreGain * 4 + currentDie * 12 + fillPressure + almostFullBonus - attackRiskPenalty + Random.Range(0, 18);
        }

        private int EvaluateAiAttack(int targetPlayer, int row, int column)
        {
            var targetGroupSize = boards[targetPlayer].GetMatchingGroupSize(row, column);
            if (targetGroupSize == 0)
            {
                return int.MinValue;
            }

            var aiScore = boards[AiPlayerIndex].CalculateSectionScoreUnits(row);
            var humanBefore = boards[0].CalculateSectionScoreUnits(row);
            var humanAfter = boards[0].CalculateSectionScoreUnitsWithoutMatchingGroup(row, column);
            var beforeOutcome = EvaluateSectionForAi(aiScore, humanBefore);
            var afterOutcome = EvaluateSectionForAi(aiScore, humanAfter);
            var beforeMatch = EvaluateProjectedMatchForAi(row, aiScore, humanBefore);
            var afterMatch = EvaluateProjectedMatchForAi(row, aiScore, humanAfter);
            var removedScore = humanBefore - humanAfter;
            var rowPressure = boards[AiPlayerIndex].CountSectionFilled(row) * 22;
            return targetGroupSize * 320 + removedScore * 6 + (afterOutcome - beforeOutcome) + (afterMatch - beforeMatch) + rowPressure + currentDie * 20 + Random.Range(0, 22);
        }

        private int EvaluateProjectedMatchForAi(int overrideRow, int aiScoreOverride, int humanScoreOverride)
        {
            var aiWins = 0;
            var humanWins = 0;
            var marginTotal = 0;
            for (var row = 0; row < BoardSize; row++)
            {
                var aiScore = row == overrideRow ? aiScoreOverride : boards[AiPlayerIndex].CalculateSectionScoreUnits(row);
                var humanScore = row == overrideRow ? humanScoreOverride : boards[0].CalculateSectionScoreUnits(row);
                marginTotal += Mathf.Clamp(aiScore - humanScore, -80, 80);
                if (aiScore > humanScore)
                {
                    aiWins++;
                }
                else if (humanScore > aiScore)
                {
                    humanWins++;
                }
            }

            return (aiWins - humanWins) * 850 + marginTotal * 3;
        }

        private int EstimateAiAttackRisk(int row, int value)
        {
            if (boards[0].IsSectionFull(row))
            {
                return 0;
            }

            var matching = 1;
            for (var column = 0; column < BoardSize; column++)
            {
                if (boards[AiPlayerIndex].Cells[row, column] == value && !boards[AiPlayerIndex].AttackProtected[row, column])
                {
                    matching++;
                }
            }

            return matching * matching;
        }

        private int EvaluateSectionForAi(int aiScoreUnits, int humanScoreUnits)
        {
            var margin = aiScoreUnits - humanScoreUnits;
            if (margin > 0)
            {
                return 700 + Mathf.Min(240, margin * 3);
            }

            if (margin < 0)
            {
                return -700 + Mathf.Max(-240, margin * 3);
            }

            return 0;
        }

        private int CalculateSectionScoreUnitsWithExtra(PlayerBoard board, int row, int extraValue)
        {
            var scoreUnits = 0;
            for (var value = 1; value <= 6; value++)
            {
                var count = extraValue == value ? 1 : 0;
                for (var column = 0; column < BoardSize; column++)
                {
                    if (board.Cells[row, column] == value)
                    {
                        count++;
                    }
                }

                if (count <= 0)
                {
                    continue;
                }

                var baseScore = value * count;
                var matchingBonus = value * (count - 1);
                scoreUnits += (baseScore + matchingBonus) * 2;
            }

            return scoreUnits;
        }

        private void StartPlacementPulse(int playerIndex, int row, int column)
        {
            if (playerIndex < 0 || playerIndex >= boards.Length || row < 0 || row >= BoardSize || column < 0 || column >= BoardSize)
            {
                return;
            }

            var face = boards[playerIndex].CellFaceImages[row, column];
            if (face == null)
            {
                return;
            }

            face.rectTransform.localScale = Vector3.one;
            StartCoroutine(PulsePlacedDieRoutine(face.rectTransform));
        }

        private IEnumerator PulsePlacedDieRoutine(RectTransform faceRect)
        {
            const float duration = 0.18f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                var scale = Mathf.Lerp(1.16f, 1f, progress);
                faceRect.localScale = Vector3.one * scale;
                yield return null;
            }

            faceRect.localScale = Vector3.one;
        }

        private void ResetBoardFaceScales()
        {
            foreach (var board in boards)
            {
                if (board == null)
                {
                    continue;
                }

                for (var row = 0; row < BoardSize; row++)
                {
                    for (var column = 0; column < BoardSize; column++)
                    {
                        var face = board.CellFaceImages[row, column];
                        if (face != null)
                        {
                            face.rectTransform.localScale = Vector3.one;
                        }
                    }
                }
            }
        }

        private bool CanAttackCell(int targetPlayer, int row, int column)
        {
            if (!gameStarted || placementComplete || isRolling || isAttackAnimating || !hasPendingDie || !pendingDieCanAttack || targetPlayer == activePlayer || currentDie <= 0)
            {
                return false;
            }

            if (boards[activePlayer].IsSectionFull(row))
            {
                return false;
            }

            return boards[targetPlayer].Cells[row, column] == currentDie && !boards[targetPlayer].AttackProtected[row, column];
        }

        private void AdvanceTurn()
        {
            if (boards[0].IsFull && boards[1].IsFull)
            {
                placementComplete = true;
                return;
            }

            activePlayer = 1 - activePlayer;
            if (boards[activePlayer].IsFull)
            {
                activePlayer = 1 - activePlayer;
            }
        }

        private void RefreshView()
        {
            for (var player = 0; player < PlayerCount; player++)
            {
                var board = boards[player];
                board.TitleText.text = GetPlayerDisplayName(player);
                board.ProgressText.text = $"{board.FilledCount} / {board.Capacity}";
                RefreshBoardProgressBar(board, player);
                var isActiveBoard = player == activePlayer && !placementComplete;
                board.PanelImage.color = gameplayTableauSprite != null
                    ? Color.clear
                    : playerOnePlaymatSprite != null || playerTwoPlaymatSprite != null || playerBoardPanelSprite != null || panelParchmentSprite != null
                        ? Color.white
                        : isActiveBoard ? new Color32(240, 250, 247, 255) : PanelColor;
                if (board.PanelOutline != null)
                {
                    board.PanelOutline.effectColor = isActiveBoard
                        ? new Color(ArtDecoMutedGold.r, ArtDecoMutedGold.g, ArtDecoMutedGold.b, 0.62f)
                        : Color.clear;
                    board.PanelOutline.effectDistance = isActiveBoard ? new Vector2(1f, -1f) : Vector2.zero;
                }

                for (var row = 0; row < BoardSize; row++)
                {
                    RefreshSectionLabel(board, player, row);
                    for (var column = 0; column < BoardSize; column++)
                    {
                        var value = board.Cells[row, column];
                        var button = board.CellButtons[row, column];
                        var text = board.CellTexts[row, column];
                        var isAttackProtected = board.AttackProtected[row, column];
                        var canPlace = CanPlaceDieOnBoard(player, row, column);
                        var canAttack = CanAttackCell(player, row, column);

                        button.interactable = !IsAiTurnActive() && (canPlace || canAttack);
                        text.text = string.Empty;
                        text.color = GetCellTextColor(value, canPlace, canAttack, isAttackProtected);
                        button.image.color = gameplayTableauSprite != null
                            ? Color.clear
                            : GetCellPlateTint(GetCellColor(player, column, value, canPlace, canAttack));
                        RefreshCellPips(board, row, column, value, canPlace, canAttack, isAttackProtected);
                        RefreshCellOutline(board, row, column, canPlace, canAttack);
                    }
                }
            }

            RefreshScoreComparison();

            rollButton.interactable = gameStarted && !IsAiTurnActive() && !placementComplete && !hasPendingDie && !isRolling && !isAttackAnimating && !boards[activePlayer].IsFull;
            rollButton.image.color = gameplayActionTraySprite != null
                ? Color.clear
                : buttonPrimarySprite != null
                    ? rollButton.interactable ? Color.white : new Color(0.58f, 0.58f, 0.58f, 0.86f)
                    : rollButton.interactable ? ButtonColor : DisabledButtonColor;
            if (rollButtonText != null)
            {
                rollButtonText.color = buttonPrimarySprite != null
                    ? rollButton.interactable ? ArtDecoCream : new Color(ArtDecoMutedGold.r, ArtDecoMutedGold.g, ArtDecoMutedGold.b, 0.68f)
                    : TextColor;
                if (isRolling)
                {
                    rollButtonText.text = "흔드는 중";
                }
                else if (!gameStarted)
                {
                    rollButtonText.text = "모드 선택";
                }
                else if (isAttackAnimating)
                {
                    rollButtonText.text = "공격 중";
                }
                else if (IsAiTurnActive())
                {
                    rollButtonText.text = "AI 진행";
                }
                else if (hasPendingDie)
                {
                    rollButtonText.text = "칸을 선택하세요";
                }
                else
                {
                    rollButtonText.text = nextRollCanAttack ? "컵 굴리기" : "보너스 굴리기";
                }
            }
            resetButton.interactable = gameStarted;
            modeButton.interactable = gameStarted;
            RefreshDrawnDieDisplay();
            RefreshMatchScoreDisplay();

            if (!isRolling)
            {
                if (hasPendingDie)
                {
                    ShowRolledDie(currentDie);
                }
                else
                {
                    ResetDiceStage();
                }
            }

            statusText.text = BuildStatusText();
            statusText.color = turnStatusPlaqueSprite != null || controlPlaqueSprite != null
                ? placementComplete
                    ? ArtDecoCream
                    : Color.Lerp(ArtDecoCream, PlayerAccentColors[activePlayer], 0.18f)
                : placementComplete ? TextColor : PlayerAccentColors[activePlayer];
            RefreshHeaderTint();
            RefreshResultBanner();
            TryStartAiTurn();
        }

        private void RefreshHeaderTint()
        {
            if (headerImage == null)
            {
                return;
            }

            if (controlPlaqueSprite != null)
            {
                headerImage.color = Color.white;
                return;
            }

            var baseColor = new Color32(247, 249, 246, 255);
            headerImage.color = placementComplete
                ? baseColor
                : gameStarted ? Color.Lerp(baseColor, PlayerAccentColors[activePlayer], 0.08f) : baseColor;
        }

        private void RefreshBoardProgressBar(PlayerBoard board, int player)
        {
            if (board.ProgressTrackImage != null)
            {
                board.ProgressTrackImage.color = gameplayTableauSprite != null
                    ? Color.clear
                    : board.IsFull
                        ? new Color(0.95f, 0.84f, 0.6f, 0.42f)
                        : new Color(0.88f, 0.78f, 0.56f, 0.3f);
            }

            if (board.ProgressFillImage == null)
            {
                return;
            }

            board.ProgressFillImage.fillAmount = board.Capacity > 0 ? board.FilledCount / (float)board.Capacity : 0f;
            board.ProgressFillImage.color = board.IsFull ? WinColor : PlayerAccentColors[player];
        }

        private void RefreshSectionLabel(PlayerBoard board, int player, int section)
        {
            var labelImage = board.SectionLabelImages[section];
            var labelText = board.SectionLabelTexts[section];
            var filled = board.CountSectionFilled(section);
            var progress = filled / (float)BoardSize;

            if (labelImage != null)
            {
                var winner = GetSectionWinner(section);
                var color = PlayerAccentColors[player];
                color.a = Mathf.Lerp(0.16f, 0.58f, progress);
                if (winner == player)
                {
                    color = WinColor;
                    color.a = Mathf.Lerp(0.4f, 0.86f, progress);
                }
                else if (winner >= 0)
                {
                    color = LoseColor;
                    color.a = Mathf.Lerp(0.22f, 0.52f, progress);
                }
                else if (filled == BoardSize)
                {
                    color.a = 0.88f;
                }

                labelImage.color = color;
            }

            if (labelText != null)
            {
                var winner = GetSectionWinner(section);
                if (winner == player)
                {
                    labelText.color = Color.white;
                }
                else if (winner >= 0)
                {
                    labelText.color = LoseColor;
                }
                else
                {
                    labelText.color = filled == BoardSize ? Color.white : PlayerAccentColors[player];
                }
            }
        }

        private void RefreshCellOutline(PlayerBoard board, int row, int column, bool canPlace, bool canAttack)
        {
            var outline = board.CellOutlines[row, column];
            if (outline == null)
            {
                return;
            }

            if (canAttack)
            {
                outline.effectColor = new Color32(116, 24, 20, 255);
                outline.effectDistance = new Vector2(4f, -4f);
                return;
            }

            if (canPlace)
            {
                outline.effectColor = PlayerAccentColors[activePlayer];
                outline.effectDistance = new Vector2(3f, -3f);
                return;
            }

            outline.effectColor = Color.clear;
            outline.effectDistance = Vector2.zero;
        }

        private void RefreshCellPips(PlayerBoard board, int row, int column, int value, bool canPlace, bool canAttack, bool isAttackProtected)
        {
            var isPreview = value == 0 && canPlace && hasPendingDie && currentDie > 0;
            var displayValue = isPreview ? currentDie : value;
            var displayProtected = isPreview ? !pendingDieCanAttack : isAttackProtected;
            var face = board.CellFaceImages[row, column];
            if (face != null)
            {
                face.gameObject.SetActive(displayValue > 0);
                face.color = GetDiceFaceArtTint(isPreview
                    ? GetPreviewCellFaceColor(displayValue, displayProtected)
                    : GetCellFaceColor(displayValue, displayProtected));
            }

            var pipColor = diceFaceArtSprite != null && !displayProtected
                ? (Color)new Color32(31, 43, 39, 255)
                : displayProtected || displayValue == 6 ? Color.white : (Color)new Color32(31, 35, 39, 255);
            if (isPreview)
            {
                pipColor.a = displayProtected || displayValue == 6 ? 0.58f : 0.42f;
            }

            for (var index = 0; index < 7; index++)
            {
                var pip = board.CellPips[row, column, index];
                if (pip == null)
                {
                    continue;
                }

                var visible = displayValue > 0 && ShouldShowPip(displayValue, index);
                pip.gameObject.SetActive(visible);
                pip.color = pipColor;
            }

            var marker = board.CellProtectionMarkers[row, column];
            if (marker != null)
            {
                var showMarker = displayValue > 0 && displayProtected;
                marker.gameObject.SetActive(showMarker);
                var markerColor = ButtonColor;
                markerColor.a = isPreview ? 0.44f : 1f;
                marker.color = markerColor;
            }
        }

        private void RefreshDrawnDieDisplay()
        {
            if (drawnDieImage == null || drawnDieFaceImage == null || drawnDieLabel == null || drawnDieText == null)
            {
                return;
            }

            if (isAttackAnimating)
            {
                drawnDieImage.color = drawnDieHudSprite != null ? Color.white : PanelColor;
                drawnDieLabel.text = "공격";
                drawnDieLabel.color = drawnDieHudSprite != null ? HudCardLabelColor : HudInkColor;
                drawnDieFaceImage.color = GetDiceFaceArtTint(PanelColor);
                drawnDieText.text = string.Empty;
                RefreshDrawnDiePips(0, Color.clear);
                SetDrawnDieProtectionMarker(false, 0f);
                return;
            }

            if (hasPendingDie && currentDie > 0)
            {
                drawnDieImage.color = drawnDieHudSprite != null
                    ? Color.white
                    : pendingDieCanAttack ? DieColors[Mathf.Clamp(currentDie - 1, 0, DieColors.Length - 1)] : Color.black;
                var textColor = !pendingDieCanAttack || currentDie == 6 ? Color.white : TextColor;
                drawnDieLabel.text = pendingDieCanAttack ? "주사위" : "공격 불가";
                drawnDieLabel.color = drawnDieHudSprite != null ? HudCardLabelColor : textColor;
                drawnDieFaceImage.color = GetDiceFaceArtTint(pendingDieCanAttack ? DieColors[Mathf.Clamp(currentDie - 1, 0, DieColors.Length - 1)] : Color.black);
                drawnDieText.text = string.Empty;
                var pipColor = diceFaceArtSprite != null && pendingDieCanAttack
                    ? (Color)new Color32(31, 43, 39, 255)
                    : textColor;
                RefreshDrawnDiePips(currentDie, pipColor);
                SetDrawnDieProtectionMarker(!pendingDieCanAttack, 1f);
                return;
            }

            if (isRolling)
            {
                drawnDieImage.color = drawnDieHudSprite != null
                    ? Color.white
                    : pendingDieCanAttack ? new Color32(255, 246, 226, 255) : Color.black;
                drawnDieLabel.text = pendingDieCanAttack ? "주사위" : "공격 불가";
                drawnDieLabel.color = drawnDieHudSprite != null
                    ? HudCardLabelColor
                    : pendingDieCanAttack ? MutedTextColor : Color.white;
                drawnDieFaceImage.color = GetDiceFaceArtTint(pendingDieCanAttack ? PanelColor : Color.black);
                drawnDieText.color = pendingDieCanAttack ? TextColor : Color.white;
                drawnDieText.text = "?";
                RefreshDrawnDiePips(0, Color.clear);
                SetDrawnDieProtectionMarker(!pendingDieCanAttack, 0.7f);
                return;
            }

            drawnDieImage.color = drawnDieHudSprite != null ? Color.white : PanelColor;
            drawnDieLabel.text = "주사위";
            drawnDieLabel.color = drawnDieHudSprite != null ? HudCardLabelColor : HudInkColor;
            drawnDieFaceImage.color = GetDiceFaceArtTint(PanelColor);
            drawnDieText.color = HudInkColor;
            drawnDieText.text = "-";
            RefreshDrawnDiePips(0, Color.clear);
            SetDrawnDieProtectionMarker(false, 0f);
        }

        private void RefreshMatchScoreDisplay()
        {
            if (matchScoreImage == null || matchScoreLabel == null || matchScoreText == null)
            {
                return;
            }

            var playerOneWins = CountSectionWins(0);
            var playerTwoWins = CountSectionWins(1);
            matchScoreLabel.text = placementComplete ? "최종 승수" : "구간 승리";
            matchScoreText.text = $"{playerOneWins} : {playerTwoWins}";

            if (playerOneWins == playerTwoWins)
            {
                matchScoreImage.color = matchScoreHudSprite != null ? Color.white : ScorePanelColor;
                matchScoreText.color = matchScoreHudSprite != null ? ArtDecoCream : HudScoreColor;
                matchScoreLabel.color = matchScoreHudSprite != null ? HudCardLabelColor : HudInkColor;
                return;
            }

            var winner = playerOneWins > playerTwoWins ? 0 : 1;
            var background = PlayerAccentColors[winner];
            background.a = 0.18f;
            matchScoreImage.color = matchScoreHudSprite != null ? Color.white : background;
            matchScoreText.color = matchScoreHudSprite != null
                ? Color.Lerp(ArtDecoCream, PlayerAccentColors[winner], 0.4f)
                : PlayerAccentColors[winner];
            matchScoreLabel.color = matchScoreHudSprite != null ? HudCardLabelColor : PlayerAccentColors[winner];
        }

        private void RefreshResultBanner()
        {
            if (resultBanner == null)
            {
                return;
            }

            var resultJustOpened = placementComplete && !resultBanner.activeSelf;
            resultBanner.SetActive(placementComplete);
            if (!placementComplete)
            {
                return;
            }

            if (resultJustOpened && EventSystem.current != null && resultResetButton != null)
            {
                EventSystem.current.SetSelectedGameObject(resultResetButton.gameObject);
            }

            var playerOneWins = CountSectionWins(0);
            var playerTwoWins = CountSectionWins(1);
            if (resultJustOpened)
            {
                if (playerOneWins == playerTwoWins)
                {
                    gameAudio?.PlayMatchDraw();
                }
                else if (currentMode == MatchMode.Pve && playerTwoWins > playerOneWins)
                {
                    gameAudio?.PlayMatchDefeat();
                }
                else
                {
                    gameAudio?.PlayMatchVictory();
                }
            }

            if (resultDetailText != null)
            {
                resultDetailText.text = $"구간 승수 {playerOneWins} : {playerTwoWins}";
            }

            if (playerOneWins == playerTwoWins)
            {
                if (resultTitleText != null)
                {
                    resultTitleText.text = "무승부";
                    resultTitleText.color = controlPlaqueSprite != null ? ArtDecoCream : TieColor;
                }

                if (resultCardImage != null)
                {
                    resultCardImage.color = controlPlaqueSprite != null ? Color.white : PanelColor;
                }

                return;
            }

            var winner = playerOneWins > playerTwoWins ? 0 : 1;
            if (resultTitleText != null)
            {
                resultTitleText.text = $"{GetPlayerDisplayName(winner)} 승리";
                resultTitleText.color = controlPlaqueSprite != null
                    ? Color.Lerp(ArtDecoCream, PlayerAccentColors[winner], 0.5f)
                    : PlayerAccentColors[winner];
            }

            if (resultCardImage != null)
            {
                resultCardImage.color = controlPlaqueSprite != null
                    ? Color.white
                    : Color.Lerp(PanelColor, PlayerAccentColors[winner], 0.12f);
            }
        }

        private void SetDrawnDieProtectionMarker(bool visible, float alpha)
        {
            if (drawnDieProtectionMarker == null)
            {
                return;
            }

            drawnDieProtectionMarker.gameObject.SetActive(visible);
            var color = ButtonColor;
            color.a = alpha;
            drawnDieProtectionMarker.color = color;
        }

        private void RefreshDrawnDiePips(int value, Color color)
        {
            for (var index = 0; index < drawnDiePips.Length; index++)
            {
                var pip = drawnDiePips[index];
                if (pip == null)
                {
                    continue;
                }

                var visible = value > 0 && ShouldShowPip(value, index);
                pip.gameObject.SetActive(visible);
                pip.color = color;
            }
        }

        private string GetPlayerDisplayName(int playerIndex)
        {
            if (currentMode == MatchMode.Pve && playerIndex == AiPlayerIndex)
            {
                return "플레이어 2 AI";
            }

            return $"플레이어 {playerIndex + 1}";
        }

        private string BuildStatusText()
        {
            if (!gameStarted)
            {
                return "게임 모드 선택";
            }

            if (placementComplete)
            {
                var playerOneWins = CountSectionWins(0);
                var playerTwoWins = CountSectionWins(1);

                if (playerOneWins == playerTwoWins)
                {
                    return $"게임 종료 - 무승부 ({playerOneWins}:{playerTwoWins})";
                }

                var winner = playerOneWins > playerTwoWins ? 0 : 1;
                return $"게임 종료 - {GetPlayerDisplayName(winner)} 승리 ({playerOneWins}:{playerTwoWins})";
            }

            if (isRolling)
            {
                if (IsAiTurnActive())
                {
                    return "플레이어 2 AI: 주사위 굴리는 중";
                }

                return cupReleaseStarted
                    ? $"플레이어 {activePlayer + 1}: 주사위 확인 중"
                    : $"플레이어 {activePlayer + 1}: 컵 흔드는 중";
            }

            if (isAttackAnimating)
            {
                return IsAiTurnActive()
                    ? "플레이어 2 AI: 공격 중"
                    : $"플레이어 {activePlayer + 1}: 공격 중";
            }

            if (hasPendingDie)
            {
                if (IsAiTurnActive())
                {
                    return pendingDieCanAttack
                        ? $"플레이어 2 AI: {currentDie} 공격/배치 판단 중"
                        : $"플레이어 2 AI: 공격 불가 {currentDie} 배치 중";
                }

                return pendingDieCanAttack
                    ? $"플레이어 {activePlayer + 1}: {currentDie} 배치 또는 공격 선택"
                    : $"플레이어 {activePlayer + 1}: 공격 불가 {currentDie} 배치 선택";
            }

            if (IsAiTurnActive())
            {
                return "플레이어 2 AI 차례";
            }

            return $"플레이어 {activePlayer + 1} 차례";
        }

        private void ResetDiceStage()
        {
            if (diceCupTransform == null || worldDieTransform == null)
            {
                return;
            }

            shakeEnergy = 0f;
            isCupDragging = false;
            cupReleaseStarted = false;
            SetCupPose(CupHomePosition, Quaternion.identity, true);
            PrepareWorldDieForCupPhysics();
        }

        private void ShowRolledDie(int value)
        {
            if (diceCupTransform == null || worldDieTransform == null)
            {
                return;
            }

            PrepareWorldDieForManualPose();
            SetCupPose(CupReleasePosition, Quaternion.Euler(CupPourRotation), true);
            worldDieTransform.localPosition = DieResultPosition;
            worldDieTransform.localRotation = Quaternion.identity;
            SetWorldDiceFace(value);
        }

        private static Vector3 GetDiePositionInCup(Vector3 cupPosition, Quaternion cupRotation, Vector3 localOffset)
        {
            return cupPosition + cupRotation * localOffset;
        }

        private void SetCupPose(Vector3 localPosition, Quaternion localRotation, bool immediate)
        {
            cupTargetLocalPosition = localPosition;
            cupTargetLocalRotation = localRotation;
            if (diceCupTransform == null)
            {
                return;
            }

            if (!immediate || diceCupBody == null || diceStageRoot == null)
            {
                if (diceCupBody == null)
                {
                    diceCupTransform.localPosition = localPosition;
                    diceCupTransform.localRotation = localRotation;
                }

                return;
            }

            // Keep the cup visually locked to the pointer instead of waiting for
            // the next physics interpolation sample.
            diceCupTransform.localPosition = localPosition;
            diceCupTransform.localRotation = localRotation;
            diceCupBody.position = diceStageRoot.TransformPoint(localPosition);
            diceCupBody.rotation = diceStageRoot.rotation * localRotation;
        }

        private void PrepareWorldDieForCupPhysics()
        {
            worldDieHasTouchedSurface = false;
            if (worldDieBody == null || diceStageRoot == null)
            {
                return;
            }

            worldDieAttachedToCup = true;
            dieInCupBaseLocalRotation = Random.rotationUniform;
            worldDieBody.isKinematic = true;
            worldDieBody.useGravity = false;
            worldDieBody.constraints = RigidbodyConstraints.None;
            worldDieBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            worldDieBody.position = diceStageRoot.TransformPoint(GetDiePositionInCup(CupHomePosition, Quaternion.identity, DieInCupOffset));
            worldDieBody.rotation = diceStageRoot.rotation * dieInCupBaseLocalRotation;
            worldDieBody.linearDamping = DieLinearDamping;
            worldDieBody.angularDamping = DieAngularDamping;
            if (worldDieCollider != null)
            {
                worldDieCollider.enabled = false;
            }

        }

        private void PrepareWorldDieForManualPose()
        {
            worldDieHasTouchedSurface = false;
            worldDieAttachedToCup = false;
            if (worldDieBody != null)
            {
                if (!worldDieBody.isKinematic)
                {
                    worldDieBody.linearVelocity = Vector3.zero;
                    worldDieBody.angularVelocity = Vector3.zero;
                }

                worldDieBody.useGravity = false;
                worldDieBody.isKinematic = true;
                worldDieBody.constraints = RigidbodyConstraints.None;
                worldDieBody.linearDamping = 0.45f;
                worldDieBody.angularDamping = 0.28f;
            }

            if (worldDieCollider != null)
            {
                worldDieCollider.enabled = false;
            }
        }

        private void PrepareWorldDieForPhysics(bool preserveVelocity)
        {
            worldDieHasTouchedSurface = false;
            worldDieAttachedToCup = false;
            if (worldDieCollider != null)
            {
                worldDieCollider.enabled = true;
            }

            if (worldDieBody == null)
            {
                return;
            }

            var wasKinematic = worldDieBody.isKinematic;
            worldDieBody.isKinematic = false;
            worldDieBody.useGravity = true;
            worldDieBody.constraints = RigidbodyConstraints.None;
            worldDieBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            worldDieBody.linearDamping = DieLinearDamping;
            worldDieBody.angularDamping = DieAngularDamping;
            if (!preserveVelocity || wasKinematic)
            {
                worldDieBody.linearVelocity = Vector3.zero;
                worldDieBody.angularVelocity = Vector3.zero;
            }

            worldDieBody.WakeUp();
        }

        private void RecoverEscapedWorldDie()
        {
            if (worldDieTransform == null || worldDieBody == null || worldDieBody.isKinematic || diceStageRoot == null)
            {
                return;
            }

            var position = worldDieTransform.localPosition;
            var escaped = position.x < EmergencyBoundsMinX
                || position.x > EmergencyBoundsMaxX
                || position.y < EmergencyBoundsMinY
                || position.z < EmergencyBoundsMinZ
                || position.z > EmergencyBoundsMaxZ;
            if (!escaped)
            {
                return;
            }

            worldDieHasTouchedSurface = false;
            worldDieBody.position = diceStageRoot.TransformPoint(new Vector3(DieResultPosition.x, 0.82f, DieResultPosition.z));
            worldDieBody.rotation = diceStageRoot.rotation * Random.rotationUniform;
            worldDieBody.linearVelocity = new Vector3(0f, -0.15f, 0f);
            worldDieBody.angularVelocity = Random.insideUnitSphere * 4f;
            worldDieBody.WakeUp();
        }

        private bool IsWorldDieInsideCup()
        {
            if (diceCupTransform == null || worldDieTransform == null)
            {
                return false;
            }

            var cupLocalPosition = diceCupTransform.InverseTransformPoint(worldDieTransform.position);
            var radialDistanceSquared = new Vector2(cupLocalPosition.x, cupLocalPosition.z).sqrMagnitude;
            return cupLocalPosition.y > -0.62f
                && cupLocalPosition.y < 0.56f
                && radialDistanceSquared < CupDieCenterRadius * CupDieCenterRadius;
        }

        private bool IsWorldDieInsidePlayableTray()
        {
            if (worldDieTransform == null)
            {
                return false;
            }

            var position = worldDieTransform.localPosition;
            return position.x >= PlayableDieMinX
                && position.x <= PlayableDieMaxX
                && position.z >= PlayableDieMinZ
                && position.z <= PlayableDieMaxZ;
        }

        private bool IsDiceCupCollider(Collider other)
        {
            return other != null
                && diceCupTransform != null
                && (other.transform == diceCupTransform || other.transform.IsChildOf(diceCupTransform));
        }

        private void NotifyWorldDieSurfaceContact()
        {
            if (!isRolling || worldDieHasTouchedSurface)
            {
                return;
            }

            worldDieHasTouchedSurface = true;
            gameAudio?.PlayDiceTableImpact();
        }

        private void SetDiceOverlayVisible(bool visible)
        {
            if (visible)
            {
                if (diceStageRoot != null)
                {
                    diceStageRoot.gameObject.SetActive(true);
                }

                if (boardUiRoot != null)
                {
                    boardUiRoot.SetActive(false);
                }

                if (diceOverlay != null)
                {
                    diceOverlay.SetActive(true);
                }

                if (diceCamera != null)
                {
                    diceCamera.enabled = true;
                }

                if (isWaitingForCupShake && diceInputSurface != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(diceInputSurface);
                }

                return;
            }

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == diceInputSurface)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (diceCamera != null)
            {
                diceCamera.enabled = false;
            }

            if (diceOverlay != null)
            {
                diceOverlay.SetActive(false);
            }

            if (diceStageRoot != null)
            {
                diceStageRoot.gameObject.SetActive(false);
            }

            if (boardUiRoot != null)
            {
                boardUiRoot.SetActive(true);
            }
        }

        private void FocusFirstAvailableAction()
        {
            if (EventSystem.current == null || !gameStarted || IsAiTurnActive() || placementComplete)
            {
                return;
            }

            for (var player = 0; player < PlayerCount; player++)
            {
                for (var row = 0; row < BoardSize; row++)
                {
                    for (var column = 0; column < BoardSize; column++)
                    {
                        var button = boards[player]?.CellButtons[row, column];
                        if (button != null && button.interactable)
                        {
                            EventSystem.current.SetSelectedGameObject(button.gameObject);
                            return;
                        }
                    }
                }
            }

            if (rollButton != null && rollButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(rollButton.gameObject);
            }
        }

        private void SetWorldDiceFace(int value)
        {
            if (worldDieTransform == null)
            {
                return;
            }

            worldDieTransform.localRotation = GetWorldDieRotationForFace(value);
        }

        private Quaternion GetWorldDieRotationForFace(int value)
        {
            var faceNormal = GetWorldDieFaceNormal(Mathf.Clamp(value, 1, 6));
            var targetUp = worldDieTransform.parent == null
                ? Vector3.up
                : worldDieTransform.parent.InverseTransformDirection(Vector3.up);
            return Quaternion.FromToRotation(faceNormal, targetUp);
        }

        private Quaternion GetWorldDieRotationForFace(int value, float yawDegrees)
        {
            return Quaternion.AngleAxis(yawDegrees, Vector3.up) * GetWorldDieRotationForFace(value);
        }

        private int GetWorldDieTopFaceValue()
        {
            if (worldDieTransform == null)
            {
                return Mathf.Clamp(currentDie, 1, 6);
            }

            var topValue = 1;
            var topDot = float.NegativeInfinity;
            for (var value = 1; value <= 6; value++)
            {
                var worldNormal = worldDieTransform.TransformDirection(GetWorldDieFaceNormal(value));
                var dot = Vector3.Dot(worldNormal, Vector3.up);
                if (dot > topDot)
                {
                    topDot = dot;
                    topValue = value;
                }
            }

            return topValue;
        }

        private float GetWorldDieTopFaceDot()
        {
            if (worldDieTransform == null)
            {
                return 1f;
            }

            var topDot = float.NegativeInfinity;
            for (var value = 1; value <= 6; value++)
            {
                var worldNormal = worldDieTransform.TransformDirection(GetWorldDieFaceNormal(value));
                topDot = Mathf.Max(topDot, Vector3.Dot(worldNormal, Vector3.up));
            }

            return topDot;
        }

        private static Vector3 GetWorldDieFaceNormal(int value)
        {
            switch (value)
            {
                case 1:
                    return Vector3.up;
                case 2:
                    return Vector3.forward;
                case 3:
                    return Vector3.right;
                case 4:
                    return Vector3.left;
                case 5:
                    return Vector3.back;
                case 6:
                    return Vector3.down;
                default:
                    return Vector3.up;
            }
        }

        private static bool ShouldShowPip(int value, int index)
        {
            switch (value)
            {
                case 1:
                    return index == 0;
                case 2:
                    return index == 1 || index == 6;
                case 3:
                    return index == 1 || index == 0 || index == 6;
                case 4:
                    return index == 1 || index == 2 || index == 5 || index == 6;
                case 5:
                    return index == 1 || index == 2 || index == 0 || index == 5 || index == 6;
                case 6:
                    return index == 1 || index == 2 || index == 3 || index == 4 || index == 5 || index == 6;
                default:
                    return false;
            }
        }

        private void RefreshScoreComparison()
        {
            for (var section = 0; section < BoardSize; section++)
            {
                var playerOneScoreUnits = boards[0].CalculateSectionScoreUnits(section);
                var playerTwoScoreUnits = boards[1].CalculateSectionScoreUnits(section);

                sectionScoreTexts[0, section].text = FormatScore(playerOneScoreUnits);
                sectionScoreTexts[1, section].text = FormatScore(playerTwoScoreUnits);

                var winner = GetSectionWinner(section);
                if (winner < 0)
                {
                    var isEmptyTie = playerOneScoreUnits == 0 && playerTwoScoreUnits == 0;
                    sectionResultTexts[section].text = isEmptyTie ? "-" : "무";
                    sectionResultTexts[section].color = isEmptyTie
                        ? scoreTowerSprite != null ? ArtDecoMutedGold : MutedTextColor
                        : scoreTowerSprite != null ? ArtDecoCream : TieColor;
                    var tieColor = isEmptyTie ? ScoreNeutralColor : TieColor;
                    sectionScoreImages[0, section].color = GetScoreMedallionTint(tieColor);
                    sectionScoreImages[1, section].color = GetScoreMedallionTint(tieColor);
                    SetSectionRowColor(section, isEmptyTie ? Color.clear : TieColor, isEmptyTie ? 0f : 0.1f);
                    continue;
                }

                sectionResultTexts[section].text = winner == 0
                    ? "1P승"
                    : currentMode == MatchMode.Pve ? "AI승" : "2P승";
                sectionResultTexts[section].color = PlayerAccentColors[winner];
                sectionScoreImages[0, section].color = GetScoreMedallionTint(winner == 0 ? WinColor : LoseColor);
                sectionScoreImages[1, section].color = GetScoreMedallionTint(winner == 1 ? WinColor : LoseColor);
                SetSectionRowColor(section, PlayerAccentColors[winner], 0.12f);
            }
        }

        private void SetSectionRowColor(int section, Color color, float alpha)
        {
            var rowImage = sectionRowImages[section];
            if (rowImage == null)
            {
                return;
            }

            color.a = alpha;
            rowImage.color = color;
        }

        private static string FormatScore(int scoreUnits)
        {
            return scoreUnits % 2 == 0
                ? (scoreUnits / 2).ToString()
                : $"{scoreUnits / 2}.5";
        }

        private int CountSectionWins(int player)
        {
            var wins = 0;
            for (var section = 0; section < BoardSize; section++)
            {
                if (GetSectionWinner(section) == player)
                {
                    wins++;
                }
            }

            return wins;
        }

        private int GetSectionWinner(int section)
        {
            var playerOneScoreUnits = boards[0].CalculateSectionScoreUnits(section);
            var playerTwoScoreUnits = boards[1].CalculateSectionScoreUnits(section);
            if (playerOneScoreUnits == playerTwoScoreUnits)
            {
                return -1;
            }

            return playerOneScoreUnits > playerTwoScoreUnits ? 0 : 1;
        }

        private static Color GetCellColor(int player, int column, int value, bool canPlace, bool canAttack)
        {
            if (canAttack)
            {
                return AttackTargetCellColor;
            }

            if (value <= 0)
            {
                if (canPlace)
                {
                    return ActiveEmptyCellColor;
                }

                return IsFrontColumn(player, column) ? FrontEmptyCellColor : EmptyCellColor;
            }

            return new Color32(218, 222, 218, 255);
        }

        private static bool IsFrontColumn(int player, int column)
        {
            return player == 0 ? column == BoardSize - 1 : column == 0;
        }

        private static Color GetCellFaceColor(int value, bool isAttackProtected)
        {
            if (value <= 0)
            {
                return Color.clear;
            }

            return isAttackProtected ? Color.black : DieColors[Mathf.Clamp(value - 1, 0, DieColors.Length - 1)];
        }

        private static Color GetPreviewCellFaceColor(int value, bool isAttackProtected)
        {
            if (value <= 0)
            {
                return Color.clear;
            }

            var color = isAttackProtected ? Color.black : (Color)DieColors[Mathf.Clamp(value - 1, 0, DieColors.Length - 1)];
            color.a = isAttackProtected ? 0.46f : 0.32f;
            return color;
        }

        private Color GetCellPlateTint(Color stateColor)
        {
            if (cellPlateSprite == null)
            {
                return stateColor;
            }

            var alpha = stateColor.a;
            stateColor.a = 1f;
            var tint = Color.Lerp(Color.white, stateColor, 0.5f);
            tint.a = alpha;
            return tint;
        }

        private Color GetScoreMedallionTint(Color stateColor)
        {
            if (scoreMedallionSprite == null)
            {
                return stateColor;
            }

            var alpha = stateColor.a;
            stateColor.a = 1f;
            var tint = Color.Lerp(Color.white, stateColor, 0.18f);
            tint.a = alpha;
            return tint;
        }

        private Color GetDiceFaceArtTint(Color stateColor)
        {
            if (diceFaceArtSprite == null)
            {
                return stateColor;
            }

            var alpha = stateColor.a;
            stateColor.a = 1f;
            var tintStrength = stateColor.grayscale < 0.08f ? 0.62f : 0.14f;
            var tint = Color.Lerp(Color.white, stateColor, tintStrength);
            tint.a = alpha;
            return tint;
        }

        private static Color GetCellTextColor(int value, bool canPlace, bool canAttack, bool isAttackProtected)
        {
            if (canAttack)
            {
                return Color.white;
            }

            if (isAttackProtected && value > 0)
            {
                return Color.white;
            }

            if (canPlace)
            {
                return ActiveBorderColor;
            }

            return value == 6 ? Color.white : TextColor;
        }

        private Button CreateButton(string name, Transform parent, string label, int fontSize)
        {
            var buttonObject = CreateUiObject(name, parent);
            var image = buttonObject.AddComponent<Image>();
            image.sprite = diceFaceSprite;
            image.color = ButtonColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(255, 246, 226, 255);
            colors.pressedColor = new Color32(211, 184, 126, 255);
            colors.selectedColor = new Color32(255, 231, 174, 255);
            colors.disabledColor = new Color32(214, 216, 213, 255);
            button.colors = colors;

            var text = CreateText("Label", buttonObject.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = TextColor;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            if (!name.StartsWith("Cell "))
            {
                button.onClick.AddListener(PlayButtonClickSound);
            }

            return button;
        }

        private void PlayButtonClickSound()
        {
            gameAudio?.PlayButtonClick();
        }

        private static void ApplyGeneratedButtonSkin(Button button, Sprite generatedSprite, Color fallbackColor)
        {
            if (button == null || button.image == null)
            {
                return;
            }

            if (generatedSprite != null)
            {
                button.image.sprite = generatedSprite;
                button.image.type = Image.Type.Sliced;
                button.image.preserveAspect = false;
                button.image.color = Color.white;
                return;
            }

            button.image.color = fallbackColor;
        }

        private static void ApplyFixedAspectButtonArtwork(Button button)
        {
            if (button == null || button.image == null)
            {
                return;
            }

            // Mode-choice artwork is exported at the final 210:72 proportion.  Slicing this
            // compact plaque would compress its top and bottom frame, so render it as one image.
            button.image.type = Image.Type.Simple;
            button.image.preserveAspect = false;
        }

        private static void ApplyTextShadow(Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            var shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void ApplyPlayerHeaderTypography(Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            ApplyTextShadow(text, new Color32(25, 12, 7, 230), new Vector2(1.5f, -1.5f));

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color32(59, 33, 17, 235);
            outline.effectDistance = new Vector2(0.75f, -0.75f);
        }

        private static void AddControlButtonDepth(GameObject buttonObject)
        {
            var shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
            shadow.effectDistance = new Vector2(2f, -2f);
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
        {
            var textObject = CreateUiObject(name, parent);
            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = defaultFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = TextColor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(16, size);
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateHudClipRegion(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var region = CreateUiObject(name, parent);
            var regionRect = region.GetComponent<RectTransform>();
            regionRect.anchorMin = anchorMin;
            regionRect.anchorMax = anchorMax;
            regionRect.offsetMin = Vector2.zero;
            regionRect.offsetMax = Vector2.zero;
            region.AddComponent<RectMask2D>();
            return region;
        }

        private static void StretchHudContent(RectTransform contentRect, float horizontalInset, float verticalInset)
        {
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(horizontalInset, verticalInset);
            contentRect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        }

        private static Material CreateStageMaterial(string name, Color color, bool transparent)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                Debug.LogError($"Tikatooka: no compatible runtime shader was found for {name}.");
                return null;
            }

            var material = new Material(shader)
            {
                name = name,
                color = color,
                enableInstancing = true
            };

            if (transparent)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            return material;
        }

        private static void ApplyStageTexture(Material material, Texture2D texture, Vector2 tiling, float smoothness)
        {
            if (material == null || texture == null)
            {
                return;
            }

            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
        }

        private static void SetMaterialFinish(Material material, float metallic, float smoothness)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
        }

        private static Mesh CreateRoundedCubeMesh(float halfExtent, float edgeRadius, int subdivisions)
        {
            subdivisions = Mathf.Max(1, subdivisions);
            edgeRadius = Mathf.Clamp(edgeRadius, 0.001f, halfExtent - 0.001f);
            var innerExtent = halfExtent - edgeRadius;
            var faceNormals = new[]
            {
                Vector3.up,
                Vector3.down,
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.left
            };
            var faceAxesU = new[]
            {
                Vector3.right,
                Vector3.right,
                Vector3.right,
                Vector3.left,
                Vector3.back,
                Vector3.forward
            };
            var faceAxesV = new[]
            {
                Vector3.back,
                Vector3.forward,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            var verticesPerFace = (subdivisions + 1) * (subdivisions + 1);
            var vertices = new Vector3[verticesPerFace * faceNormals.Length];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[subdivisions * subdivisions * 6 * faceNormals.Length];
            var vertex = 0;
            var triangle = 0;

            for (var face = 0; face < faceNormals.Length; face++)
            {
                var faceStart = vertex;
                for (var y = 0; y <= subdivisions; y++)
                {
                    var v = Mathf.Lerp(-halfExtent, halfExtent, y / (float)subdivisions);
                    for (var x = 0; x <= subdivisions; x++)
                    {
                        var u = Mathf.Lerp(-halfExtent, halfExtent, x / (float)subdivisions);
                        var cubePoint = faceNormals[face] * halfExtent + faceAxesU[face] * u + faceAxesV[face] * v;
                        var innerPoint = new Vector3(
                            Mathf.Clamp(cubePoint.x, -innerExtent, innerExtent),
                            Mathf.Clamp(cubePoint.y, -innerExtent, innerExtent),
                            Mathf.Clamp(cubePoint.z, -innerExtent, innerExtent));
                        var edgeDirection = (cubePoint - innerPoint).normalized;
                        vertices[vertex] = innerPoint + edgeDirection * edgeRadius;
                        normals[vertex] = edgeDirection;
                        uvs[vertex] = new Vector2(x / (float)subdivisions, y / (float)subdivisions);
                        vertex++;
                    }
                }

                for (var y = 0; y < subdivisions; y++)
                {
                    for (var x = 0; x < subdivisions; x++)
                    {
                        var a = faceStart + y * (subdivisions + 1) + x;
                        var b = a + 1;
                        var c = a + subdivisions + 1;
                        var d = c + 1;
                        triangles[triangle++] = a;
                        triangles[triangle++] = b;
                        triangles[triangle++] = c;
                        triangles[triangle++] = c;
                        triangles[triangle++] = b;
                        triangles[triangle++] = d;
                    }
                }
            }

            var mesh = new Mesh
            {
                name = "Rounded World Die Mesh",
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOpenCupMesh(float radius, float height, int segments, bool inwardFacing)
        {
            var mesh = new Mesh
            {
                name = "Open Dice Cup Mesh"
            };

            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[index * 2] = new Vector3(x, -height * 0.5f, z);
                vertices[index * 2 + 1] = new Vector3(x, height * 0.5f, z);
                var u = index / (float)segments;
                uvs[index * 2] = new Vector2(u, 0f);
                uvs[index * 2 + 1] = new Vector2(u, 1f);
            }

            for (var index = 0; index < segments; index++)
            {
                var vertex = index * 2;
                var tri = index * 6;
                if (inwardFacing)
                {
                    triangles[tri] = vertex;
                    triangles[tri + 1] = vertex + 2;
                    triangles[tri + 2] = vertex + 1;
                    triangles[tri + 3] = vertex + 1;
                    triangles[tri + 4] = vertex + 2;
                    triangles[tri + 5] = vertex + 3;
                }
                else
                {
                    triangles[tri] = vertex;
                    triangles[tri + 1] = vertex + 1;
                    triangles[tri + 2] = vertex + 2;
                    triangles[tri + 3] = vertex + 1;
                    triangles[tri + 4] = vertex + 3;
                    triangles[tri + 5] = vertex + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateRingMesh(float innerRadius, float outerRadius, int segments)
        {
            var mesh = new Mesh
            {
                name = "Cup Rim Ring Mesh"
            };

            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var outer = new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius);
                var inner = new Vector3(Mathf.Cos(angle) * innerRadius, 0f, Mathf.Sin(angle) * innerRadius);
                vertices[index * 2] = outer;
                vertices[index * 2 + 1] = inner;
                uvs[index * 2] = new Vector2(0.5f + outer.x / (outerRadius * 2f), 0.5f + outer.z / (outerRadius * 2f));
                uvs[index * 2 + 1] = new Vector2(0.5f + inner.x / (outerRadius * 2f), 0.5f + inner.z / (outerRadius * 2f));
            }

            for (var index = 0; index < segments; index++)
            {
                var vertex = index * 2;
                var tri = index * 6;
                triangles[tri] = vertex;
                triangles[tri + 1] = vertex + 1;
                triangles[tri + 2] = vertex + 2;
                triangles[tri + 3] = vertex + 2;
                triangles[tri + 4] = vertex + 1;
                triangles[tri + 5] = vertex + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            RestoreRuntimeSettings();
            RestoreSceneCameraMasks();

            if (diceRenderTexture != null)
            {
                if (diceCamera != null)
                {
                    diceCamera.targetTexture = null;
                }

                if (diceOutputImage != null)
                {
                    diceOutputImage.texture = null;
                }

                diceRenderTexture.Release();
                Destroy(diceRenderTexture);
                diceRenderTexture = null;
            }

            DestroyStageMaterial(cupMaterial);
            DestroyStageMaterial(cupInnerMaterial);
            DestroyStageMaterial(dieMaterial);
            DestroyStageMaterial(pipMaterial);
            DestroyStageMaterial(tableMaterial);
            DestroyStageMaterial(feltMaterial);
            DestroyStageMaterial(trayRimMaterial);
            DestroyStageMaterial(trayHighlightMaterial);
            DestroyRuntimeObject(worldDiePhysicsMaterial);
            DestroyRuntimeObject(worldDieMesh);
            DestroyRuntimeObject(cupOuterMesh);
            DestroyRuntimeObject(cupInnerMesh);
            DestroyRuntimeObject(cupRimMesh);
            DestroyRuntimeObject(cupRimInnerInlayMesh);
            DestroyRuntimeObject(cupRimOuterInlayMesh);
            DestroyRuntimeObject(cupUpperBandMesh);
            DestroyRuntimeObject(cupLowerBandMesh);
            DestroyRuntimeSprite(scoreBadgeSprite);
            DestroyRuntimeSprite(diceFaceSprite);
            DestroyRuntimeSprite(dicePipSprite);
            DestroyRuntimeObject(boardPatternSprite);
            DestroyRuntimeObject(diceCeramicSprite);
            DestroyRuntimeObject(panelParchmentSprite);
            DestroyRuntimeObject(playerBoardPanelSprite);
            DestroyRuntimeObject(playerOnePlaymatSprite);
            DestroyRuntimeObject(playerTwoPlaymatSprite);
            DestroyRuntimeObject(gameplayTableauSprite);
            DestroyRuntimeObject(playerHeaderSprite);
            DestroyRuntimeObject(playerStatusHeaderSprite);
            DestroyRuntimeObject(playerProgressRailSprite);
            DestroyRuntimeObject(scoreHeaderSprite);
            DestroyRuntimeObject(mainBoardBackdropSprite);
            DestroyRuntimeObject(scoreTowerSprite);
            DestroyRuntimeObject(controlPlaqueSprite);
            DestroyRuntimeObject(turnStatusPlaqueSprite);
            DestroyRuntimeObject(gameplayActionTraySprite);
            DestroyRuntimeObject(matchScoreHudSprite);
            DestroyRuntimeObject(drawnDieHudSprite);
            DestroyRuntimeObject(buttonPrimarySprite);
            DestroyRuntimeObject(buttonSecondarySprite);
            DestroyRuntimeObject(buttonPvpSprite);
            DestroyRuntimeObject(buttonPveSprite);
            DestroyRuntimeObject(cellPlateSprite);
            DestroyRuntimeObject(scoreMedallionSprite);
            DestroyRuntimeObject(diceFaceArtSprite);
            DestroyRuntimeObject(titleBackdropSprite);
            DestroyRuntimeObject(titleCrestSprite);
            boardPatternSprite = null;
            diceCeramicSprite = null;
            panelParchmentSprite = null;
            playerBoardPanelSprite = null;
            playerOnePlaymatSprite = null;
            playerTwoPlaymatSprite = null;
            gameplayTableauSprite = null;
            playerHeaderSprite = null;
            playerStatusHeaderSprite = null;
            playerProgressRailSprite = null;
            scoreHeaderSprite = null;
            mainBoardBackdropSprite = null;
            scoreTowerSprite = null;
            controlPlaqueSprite = null;
            turnStatusPlaqueSprite = null;
            gameplayActionTraySprite = null;
            matchScoreHudSprite = null;
            drawnDieHudSprite = null;
            buttonPrimarySprite = null;
            buttonSecondarySprite = null;
            buttonPvpSprite = null;
            buttonPveSprite = null;
            cellPlateSprite = null;
            scoreMedallionSprite = null;
            diceFaceArtSprite = null;
            titleBackdropSprite = null;
            titleCrestSprite = null;
            boardPatternTexture = null;
            diceCeramicTexture = null;
            panelParchmentTexture = null;
            playerBoardPanelTexture = null;
            playerOnePlaymatTexture = null;
            playerTwoPlaymatTexture = null;
            gameplayTableauTexture = null;
            playerHeaderTexture = null;
            playerStatusHeaderTexture = null;
            playerProgressRailTexture = null;
            scoreHeaderTexture = null;
            mainBoardBackdropTexture = null;
            scoreTowerTexture = null;
            controlPlaqueTexture = null;
            turnStatusPlaqueTexture = null;
            gameplayActionTrayTexture = null;
            matchScoreHudTexture = null;
            drawnDieHudTexture = null;
            buttonPrimaryTexture = null;
            buttonSecondaryTexture = null;
            buttonPvpTexture = null;
            buttonPveTexture = null;
            cellPlateTexture = null;
            scoreMedallionTexture = null;
            diceFaceArtTexture = null;
            worldDieSurfaceTexture = null;
            walnutTableTexture = null;
            cupLeatherTexture = null;
            titleBackdropTexture = null;
            titleCrestTexture = null;
            if (ownsDefaultFont)
            {
                DestroyRuntimeObject(defaultFont);
            }
        }

        private void RestoreRuntimeSettings()
        {
            if (!runtimeSettingsApplied)
            {
                return;
            }

            QualitySettings.vSyncCount = originalVSyncCount;
            Application.targetFrameRate = originalTargetFrameRate;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            Time.maximumDeltaTime = originalMaximumDeltaTime;
            runtimeSettingsApplied = false;
        }

        private void RestoreSceneCameraMasks()
        {
            if (maskedSceneCameras == null || originalSceneCameraMasks == null)
            {
                return;
            }

            var count = Mathf.Min(maskedSceneCameras.Length, originalSceneCameraMasks.Length);
            var diceLayerMask = 1 << DiceStageLayer;
            for (var index = 0; index < count; index++)
            {
                if (maskedSceneCameras[index] != null)
                {
                    var currentMask = maskedSceneCameras[index].cullingMask;
                    var originalDiceLayerBit = originalSceneCameraMasks[index] & diceLayerMask;
                    maskedSceneCameras[index].cullingMask = (currentMask & ~diceLayerMask) | originalDiceLayerBit;
                }
            }

            maskedSceneCameras = null;
            originalSceneCameraMasks = null;
        }

        private static void DestroyStageMaterial(Material material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private static void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            DestroyRuntimeObject(sprite.texture);
            DestroyRuntimeObject(sprite);
        }

        private static void DestroyRuntimeObject(Object runtimeObject)
        {
            if (runtimeObject != null)
            {
                Destroy(runtimeObject);
            }
        }

        private static Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = (size - 1) * 0.5f;
            var radius = center - 1f;
            var colors = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = Mathf.Clamp01(radius - distance + 1f);
                    colors[index] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void LoadGeneratedArt()
        {
            boardPatternTexture = Resources.Load<Texture2D>("Art/TikatookaFeltV2") ?? Resources.Load<Texture2D>("Art/TikatookaBoardPattern");
            diceCeramicTexture = Resources.Load<Texture2D>("Art/TikatookaDiceCeramicV2") ?? Resources.Load<Texture2D>("Art/TikatookaDiceCeramic");
            panelParchmentTexture = Resources.Load<Texture2D>("Art/TikatookaPanelParchment");
            playerBoardPanelTexture = Resources.Load<Texture2D>("Art/TikatookaBoardPanelV2");
            playerOnePlaymatTexture = Resources.Load<Texture2D>("Art/TikatookaPlayerPlaymatP1V1");
            playerTwoPlaymatTexture = Resources.Load<Texture2D>("Art/TikatookaPlayerPlaymatP2V1");
            gameplayTableauTexture = Resources.Load<Texture2D>("Art/TikatookaGameplayTableauV2");
            playerHeaderTexture = Resources.Load<Texture2D>("Art/TikatookaPlayerHeaderV1");
            playerStatusHeaderTexture = Resources.Load<Texture2D>("Art/TikatookaPlayerStatusHeaderV1");
            playerProgressRailTexture = Resources.Load<Texture2D>("Art/TikatookaPlayerProgressRailV1");
            scoreHeaderTexture = Resources.Load<Texture2D>("Art/TikatookaScoreHeaderV1");
            mainBoardBackdropTexture = Resources.Load<Texture2D>("Art/TikatookaMainBoardBackdropV2");
            scoreTowerTexture = Resources.Load<Texture2D>("Art/TikatookaScoreTowerV1");
            controlPlaqueTexture = Resources.Load<Texture2D>("Art/TikatookaControlPlaqueV1");
            turnStatusPlaqueTexture = Resources.Load<Texture2D>("Art/TikatookaTurnStatusPlaqueV1");
            gameplayActionTrayTexture = Resources.Load<Texture2D>("Art/TikatookaGameplayActionTrayV3");
            matchScoreHudTexture = Resources.Load<Texture2D>("Art/TikatookaMatchScoreHudV2")
                ?? Resources.Load<Texture2D>("Art/TikatookaMatchScoreHudV1");
            drawnDieHudTexture = Resources.Load<Texture2D>("Art/TikatookaDrawnDieHudV2")
                ?? Resources.Load<Texture2D>("Art/TikatookaDrawnDieHudV1");
            buttonPrimaryTexture = Resources.Load<Texture2D>("Art/TikatookaButtonPrimaryV4")
                ?? Resources.Load<Texture2D>("Art/TikatookaButtonPrimaryV3")
                ?? Resources.Load<Texture2D>("Art/TikatookaButtonPrimaryV2")
                ?? Resources.Load<Texture2D>("Art/TikatookaButtonPrimaryV1");
            buttonSecondaryTexture = Resources.Load<Texture2D>("Art/TikatookaButtonSecondaryV1");
            // Use the shared stepped Art Deco plaque silhouette for all actions; only the
            // PVE centre material changes to blue so the mode choice remains immediately clear.
            buttonPvpTexture = Resources.Load<Texture2D>("Art/TikatookaButtonPrimaryV4")
                ?? Resources.Load<Texture2D>("Art/TikatookaButtonPvpV1");
            buttonPveTexture = Resources.Load<Texture2D>("Art/TikatookaButtonPveV3")
                ?? Resources.Load<Texture2D>("Art/TikatookaButtonPveV1");
            cellPlateTexture = Resources.Load<Texture2D>("Art/TikatookaCellPlateV1");
            scoreMedallionTexture = Resources.Load<Texture2D>("Art/TikatookaScoreMedallionV1");
            diceFaceArtTexture = Resources.Load<Texture2D>("Art/TikatookaDiceFaceV3");
            worldDieSurfaceTexture = Resources.Load<Texture2D>("Art/TikatookaDicePorcelainSurfaceV3");
            walnutTableTexture = Resources.Load<Texture2D>("Art/TikatookaWalnutTable");
            cupLeatherTexture = Resources.Load<Texture2D>("Art/TikatookaCupLeather");
            titleBackdropTexture = Resources.Load<Texture2D>("Art/TikatookaTitleBackdropV1");
            titleCrestTexture = Resources.Load<Texture2D>("Art/TikatookaTitleCrestV1");

            if (boardPatternTexture != null)
            {
                boardPatternSprite = CreateSpriteFromTexture(boardPatternTexture, "Tikatooka Felt Sprite", 256f);
            }

            if (diceCeramicTexture != null)
            {
                diceCeramicSprite = CreateSpriteFromTexture(diceCeramicTexture, "Tikatooka Dice Ceramic V2 Sprite", 256f);
            }

            if (panelParchmentTexture != null)
            {
                panelParchmentSprite = CreateSpriteFromTexture(panelParchmentTexture, "Tikatooka Panel Parchment Sprite", 256f);
            }

            if (playerBoardPanelTexture != null)
            {
                playerBoardPanelSprite = CreateSpriteFromTexture(playerBoardPanelTexture, "Tikatooka Board Panel V2 Sprite", 256f);
            }

            if (playerOnePlaymatTexture != null)
            {
                playerOnePlaymatSprite = CreateSpriteFromTexture(playerOnePlaymatTexture, "Tikatooka Player One Playmat V1 Sprite", 256f);
            }

            if (playerTwoPlaymatTexture != null)
            {
                playerTwoPlaymatSprite = CreateSpriteFromTexture(playerTwoPlaymatTexture, "Tikatooka Player Two Playmat V1 Sprite", 256f);
            }

            if (gameplayTableauTexture != null)
            {
                gameplayTableauSprite = CreateSpriteFromTexture(gameplayTableauTexture, "Tikatooka Gameplay Tableau V2 Sprite", 100f);
            }

            if (playerHeaderTexture != null)
            {
                playerHeaderSprite = CreateSpriteFromTexture(playerHeaderTexture, "Tikatooka Player Header V1 Sprite", 100f);
            }

            if (playerStatusHeaderTexture != null)
            {
                playerStatusHeaderSprite = CreateSpriteFromTexture(
                    playerStatusHeaderTexture,
                    "Tikatooka Player Status Header V1 Sprite",
                    100f);
            }

            if (playerProgressRailTexture != null)
            {
                playerProgressRailSprite = CreateSpriteFromTexture(playerProgressRailTexture, "Tikatooka Player Progress Rail V1 Sprite", 100f);
            }

            if (scoreHeaderTexture != null)
            {
                scoreHeaderSprite = CreateSpriteFromTexture(scoreHeaderTexture, "Tikatooka Score Header V1 Sprite", 100f);
            }

            if (mainBoardBackdropTexture != null)
            {
                mainBoardBackdropSprite = CreateSpriteFromTexture(mainBoardBackdropTexture, "Tikatooka Main Board Backdrop V2 Sprite", 100f);
            }

            if (scoreTowerTexture != null)
            {
                scoreTowerSprite = CreateSpriteFromTexture(scoreTowerTexture, "Tikatooka Score Tower V1 Sprite", 100f);
            }

            if (controlPlaqueTexture != null)
            {
                controlPlaqueSprite = CreateSlicedSpriteFromTexture(
                    controlPlaqueTexture,
                    "Tikatooka Control Plaque V1 Sprite",
                    100f,
                    0.16f,
                    0.28f);
            }

            if (turnStatusPlaqueTexture != null)
            {
                turnStatusPlaqueSprite = CreateSlicedSpriteFromTexture(
                    turnStatusPlaqueTexture,
                    "Tikatooka Turn Status Plaque V1 Sprite",
                    100f,
                    0.08f,
                    0.20f);
            }

            if (gameplayActionTrayTexture != null)
            {
                gameplayActionTraySprite = CreateSpriteFromTexture(
                    gameplayActionTrayTexture,
                    "Tikatooka Gameplay Action Tray V3 Sprite",
                    100f);
            }

            if (matchScoreHudTexture != null)
            {
                matchScoreHudSprite = CreateSpriteFromTexture(
                    matchScoreHudTexture,
                    "Tikatooka Match Score HUD V2 Sprite",
                    100f);
            }

            if (drawnDieHudTexture != null)
            {
                drawnDieHudSprite = CreateSpriteFromTexture(
                    drawnDieHudTexture,
                    "Tikatooka Drawn Die HUD V2 Sprite",
                    100f);
            }

            if (buttonPrimaryTexture != null)
            {
                buttonPrimarySprite = CreateSlicedSpriteFromTexture(
                    buttonPrimaryTexture,
                    "Tikatooka Primary Button V4 Sprite",
                    100f,
                    0.16f,
                    0.24f);
            }

            if (buttonSecondaryTexture != null)
            {
                buttonSecondarySprite = CreateSlicedSpriteFromTexture(
                    buttonSecondaryTexture,
                    "Tikatooka Secondary Button V1 Sprite",
                    100f,
                    0.16f,
                    0.24f);
            }

            if (buttonPvpTexture != null)
            {
                buttonPvpSprite = CreateSlicedSpriteFromTexture(
                    buttonPvpTexture,
                    "Tikatooka PVP Button V4 Sprite",
                    100f,
                    0.16f,
                    0.24f);
            }

            if (buttonPveTexture != null)
            {
                buttonPveSprite = CreateSlicedSpriteFromTexture(
                    buttonPveTexture,
                    "Tikatooka PVE Button V3 Sprite",
                    100f,
                    0.16f,
                    0.24f);
            }

            if (cellPlateTexture != null)
            {
                cellPlateSprite = CreateSpriteFromTexture(
                    cellPlateTexture,
                    "Tikatooka Cell Plate V1 Sprite",
                    100f,
                    new Rect(0.122f, 0.128f, 0.754f, 0.762f));
            }

            if (scoreMedallionTexture != null)
            {
                scoreMedallionSprite = CreateSpriteFromTexture(
                    scoreMedallionTexture,
                    "Tikatooka Score Medallion V1 Sprite",
                    100f,
                    new Rect(0.164f, 0.186f, 0.671f, 0.679f));
            }

            if (diceFaceArtTexture != null)
            {
                diceFaceArtSprite = CreateSpriteFromTexture(
                    diceFaceArtTexture,
                    "Tikatooka Dice Face V3 Sprite",
                    100f,
                    new Rect(0.079f, 0.076f, 0.84f, 0.847f));
            }

            if (titleBackdropTexture != null)
            {
                titleBackdropSprite = CreateSpriteFromTexture(
                    titleBackdropTexture,
                    "Tikatooka Title Backdrop V1 Sprite",
                    100f);
            }

            if (titleCrestTexture != null)
            {
                titleCrestSprite = CreateSpriteFromTexture(
                    titleCrestTexture,
                    "Tikatooka Title Crest V1 Sprite",
                    100f);
            }
        }

        private static Sprite CreateSpriteFromTexture(Texture2D texture, string spriteName, float pixelsPerUnit)
        {
            return CreateSpriteFromTexture(texture, spriteName, pixelsPerUnit, new Rect(0f, 0f, 1f, 1f));
        }

        private static Sprite CreateSpriteFromTexture(Texture2D texture, string spriteName, float pixelsPerUnit, Rect normalizedRect)
        {
            if (texture == null)
            {
                return null;
            }

            var textureRect = new Rect(
                texture.width * normalizedRect.x,
                texture.height * normalizedRect.y,
                texture.width * normalizedRect.width,
                texture.height * normalizedRect.height);

            var sprite = Sprite.Create(
                texture,
                textureRect,
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private static Sprite CreateSlicedSpriteFromTexture(
            Texture2D texture,
            string spriteName,
            float pixelsPerUnit,
            float horizontalBorderRatio,
            float verticalBorderRatio)
        {
            if (texture == null)
            {
                return null;
            }

            var textureRect = new Rect(0f, 0f, texture.width, texture.height);
            var border = new Vector4(
                texture.width * horizontalBorderRatio,
                texture.height * verticalBorderRatio,
                texture.width * horizontalBorderRatio,
                texture.height * verticalBorderRatio);
            var sprite = Sprite.Create(
                texture,
                textureRect,
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = spriteName;
            return sprite;
        }

        private void CreateBoardPatternOverlay(Transform parent, float alpha)
        {
            var overlaySprite = panelParchmentSprite != null ? panelParchmentSprite : boardPatternSprite;
            if (overlaySprite == null)
            {
                return;
            }

            var overlay = CreateUiObject("Generated Parchment Overlay", parent);
            var overlayLayout = overlay.AddComponent<LayoutElement>();
            overlayLayout.ignoreLayout = true;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.sprite = overlaySprite;
            overlayImage.type = Image.Type.Tiled;
            overlayImage.color = new Color(1f, 1f, 1f, alpha);
            overlayImage.raycastTarget = false;

            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.transform.SetAsFirstSibling();
        }

        private void CreateDiceCeramicInlay(Transform parent, float inset, float alpha)
        {
            if (diceCeramicSprite == null)
            {
                return;
            }

            var inlay = CreateUiObject("Generated Ceramic Inlay", parent);
            var inlayImage = inlay.AddComponent<Image>();
            inlayImage.sprite = diceCeramicSprite;
            inlayImage.color = new Color(1f, 1f, 1f, alpha);
            inlayImage.raycastTarget = false;

            var inlayRect = inlay.GetComponent<RectTransform>();
            inlayRect.anchorMin = new Vector2(inset, inset);
            inlayRect.anchorMax = new Vector2(1f - inset, 1f - inset);
            inlayRect.offsetMin = Vector2.zero;
            inlayRect.offsetMax = Vector2.zero;
            inlay.transform.SetAsFirstSibling();
        }

        private static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color32[size * size];
            var min = radius;
            var max = size - radius;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = x + 0.5f;
                    var py = y + 0.5f;
                    var nearestX = Mathf.Clamp(px, min, max);
                    var nearestY = Mathf.Clamp(py, min, max);
                    var distance = Vector2.Distance(new Vector2(px, py), new Vector2(nearestX, nearestY));
                    var alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    colors[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void AddLayout(GameObject target, float preferredWidth, float preferredHeight, int flexibleWidth = 0, int flexibleHeight = 0)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            if (preferredWidth > 0)
            {
                layout.preferredWidth = preferredWidth;
            }

            if (preferredHeight > 0)
            {
                layout.preferredHeight = preferredHeight;
            }

            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = flexibleHeight;
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = null;
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < eventSystems.Length; index++)
            {
                var candidate = eventSystems[index];
                if (candidate != null && candidate.gameObject.scene == gameObject.scene)
                {
                    eventSystem = candidate;
                    break;
                }
            }

            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.transform.SetParent(transform, false);
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Destroy(legacyModule);
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
        }

        private sealed class WorldDieCollisionReporter : MonoBehaviour
        {
            private DiceBoardGameController controller;

            public void Initialize(DiceBoardGameController owner)
            {
                controller = owner;
            }

            private void OnCollisionEnter(Collision collision)
            {
                NotifyIfFloorContact(collision);
            }

            private void OnCollisionStay(Collision collision)
            {
                NotifyIfFloorContact(collision);
            }

            private void NotifyIfFloorContact(Collision collision)
            {
                if (controller == null
                    || controller.worldDieHasTouchedSurface
                    || controller.IsDiceCupCollider(collision.collider))
                {
                    return;
                }

                for (var index = 0; index < collision.contactCount; index++)
                {
                    if (collision.GetContact(index).normal.y > 0.45f)
                    {
                        controller?.NotifyWorldDieSurfaceContact();
                        return;
                    }
                }
            }
        }

        private sealed class DiceCupDragSurface : MonoBehaviour, IInitializePotentialDragHandler, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerUpHandler, ISubmitHandler, ICancelHandler
        {
            private DiceBoardGameController controller;

            public void Initialize(DiceBoardGameController owner)
            {
                controller = owner;
            }

            public void OnInitializePotentialDrag(PointerEventData eventData)
            {
                eventData.useDragThreshold = false;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                controller?.BeginCupShake(eventData.position);
            }

            public void OnDrag(PointerEventData eventData)
            {
                controller?.DragCupShake(eventData.position, eventData.delta);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                controller?.EndCupShake();
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                controller?.EndCupShake();
            }

            public void OnSubmit(BaseEventData eventData)
            {
                controller?.EndCupShake();
            }

            public void OnCancel(BaseEventData eventData)
            {
                controller?.CancelCupShake();
            }
        }

    }
}
