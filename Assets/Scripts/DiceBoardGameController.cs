using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private enum MatchMode
        {
            Pvp,
            Pve
        }

        private static readonly Vector3 CupHomePosition = new Vector3(0.72f, 0.45f, 0.12f);
        private static readonly Vector3 DieInCupOffset = new Vector3(-0.06f, -0.17f, 0.02f);
        private static readonly Vector3 CupReleasePosition = new Vector3(0.88f, 0.68f, 0.2f);
        private static readonly Vector3 DieResultPosition = new Vector3(-0.42f, 0.25f, 0.05f);
        private static readonly Vector3 CupPourRotation = new Vector3(0f, -12f, 112f);
        private const float EmergencyBoundsMinX = -2.3f;
        private const float EmergencyBoundsMaxX = 1.65f;
        private const float EmergencyBoundsMinZ = -1.4f;
        private const float EmergencyBoundsMaxZ = 1.4f;
        private const float EmergencyBoundsMinY = -0.8f;

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
        private static readonly Color EmptyCellColor = new Color32(230, 233, 232, 255);
        private static readonly Color FrontEmptyCellColor = new Color32(222, 229, 232, 255);
        private static readonly Color ActiveEmptyCellColor = new Color32(204, 238, 232, 255);
        private static readonly Color AttackTargetCellColor = new Color32(236, 92, 78, 255);
        private static readonly Color ButtonColor = new Color32(239, 127, 64, 255);
        private static readonly Color DisabledButtonColor = new Color32(192, 195, 190, 255);

        private static readonly Color[] PlayerAccentColors =
        {
            new Color32(205, 74, 68, 255),
            new Color32(60, 126, 199, 255)
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
        private bool ownsDefaultFont;
        private Sprite scoreBadgeSprite;
        private Sprite diceFaceSprite;
        private Sprite dicePipSprite;
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
        private GameObject diceOverlay;
        private RenderTexture diceRenderTexture;
        private Camera diceCamera;
        private Transform diceStageRoot;
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
        private Material cupMaterial;
        private Material cupInnerMaterial;
        private Material dieMaterial;
        private Material pipMaterial;
        private Material tableMaterial;
        private Material woodGrainMaterial;
        private Material feltMaterial;
        private Material trayRimMaterial;
        private Material trayHighlightMaterial;
        private Button rollButton;
        private Text rollButtonText;
        private Button resetButton;
        private Button modeButton;

        private int activePlayer;
        private int currentDie;
        private float shakeEnergy;
        private float shakeDistance;
        private float peakShakeDelta;
        private int shakeDirectionChanges;
        private Vector2 lastShakeDirection;
        private Vector2 cupDragStartScreenPosition;
        private Vector3 cupDragStartLocalPosition;
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
        private MatchMode currentMode = MatchMode.Pve;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureControllerExists()
        {
            if (FindFirstObjectByType<DiceBoardGameController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("Dice Board Game");
            controllerObject.AddComponent<DiceBoardGameController>();
        }

        private void Awake()
        {
            defaultFont = LoadInterfaceFont(out ownsDefaultFont);
            scoreBadgeSprite = CreateCircleSprite(96);
            diceFaceSprite = CreateRoundedRectSprite(96, 13);
            dicePipSprite = CreateCircleSprite(32);
            BuildInterface();
            StartNewGame();
            ShowModeSelection();
        }

        private void FixedUpdate()
        {
            if (diceCupBody == null || diceStageRoot == null)
            {
                return;
            }

            diceCupBody.MovePosition(diceStageRoot.TransformPoint(cupTargetLocalPosition));
            diceCupBody.MoveRotation(diceStageRoot.rotation * cupTargetLocalRotation);
        }

        private static Font LoadInterfaceFont(out bool ownsFont)
        {
            ownsFont = false;
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
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateUiObject("Root", canvasObject.transform);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var rootImage = root.AddComponent<Image>();
            rootImage.color = PageColor;

            var rootLayout = root.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(12, 12, 12, 12);
            rootLayout.spacing = 12;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;

            CreateHeader(root.transform);
            CreateBoards(root.transform);
            CreateControls(root.transform);
            CreateDiceOverlay(canvasObject.transform);
            CreateAttackAnimationLayer(canvasObject.transform);
            CreateResultBanner(canvasObject.transform);
            CreateModeSelectionOverlay(canvasObject.transform);
        }

        private void CreateHeader(Transform parent)
        {
            var header = CreateUiObject("Header", parent);
            headerImage = header.AddComponent<Image>();
            headerImage.sprite = diceFaceSprite;
            headerImage.color = new Color32(247, 249, 246, 255);
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
            AddLayout(header, -1, 72);

            statusText = CreateText("Status", header.transform, "플레이어 1 차례", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            statusText.color = TextColor;
            AddLayout(statusText.gameObject, -1, 72, flexibleWidth: 1);

            CreateMatchScoreDisplay(header.transform);
            CreateDrawnDieDisplay(header.transform);
        }

        private void CreateMatchScoreDisplay(Transform parent)
        {
            var display = CreateUiObject("Match Score Display", parent);
            matchScoreImage = display.AddComponent<Image>();
            matchScoreImage.sprite = diceFaceSprite;
            matchScoreImage.color = ScorePanelColor;
            var scoreShadow = display.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            scoreShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var scoreOutline = display.AddComponent<Outline>();
            scoreOutline.effectColor = new Color(0f, 0f, 0f, 0.08f);
            scoreOutline.effectDistance = new Vector2(1f, -1f);
            AddLayout(display, 154, 72);

            var displayLayout = display.AddComponent<VerticalLayoutGroup>();
            displayLayout.padding = new RectOffset(8, 8, 6, 6);
            displayLayout.spacing = 0;
            displayLayout.childAlignment = TextAnchor.MiddleCenter;
            displayLayout.childControlHeight = true;
            displayLayout.childControlWidth = true;
            displayLayout.childForceExpandHeight = false;
            displayLayout.childForceExpandWidth = true;

            matchScoreLabel = CreateText("Match Score Label", display.transform, "구간 승리", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            matchScoreLabel.color = MutedTextColor;
            AddLayout(matchScoreLabel.gameObject, -1, 22);

            matchScoreText = CreateText("Match Score Value", display.transform, "0 : 0", 31, FontStyle.Bold, TextAnchor.MiddleCenter);
            matchScoreText.color = TextColor;
            AddLayout(matchScoreText.gameObject, -1, 38);
        }

        private void CreateDrawnDieDisplay(Transform parent)
        {
            var display = CreateUiObject("Drawn Die Display", parent);
            drawnDieImage = display.AddComponent<Image>();
            drawnDieImage.sprite = diceFaceSprite;
            drawnDieImage.color = PanelColor;
            var displayShadow = display.AddComponent<Shadow>();
            displayShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            displayShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var displayOutline = display.AddComponent<Outline>();
            displayOutline.effectColor = new Color(0f, 0f, 0f, 0.08f);
            displayOutline.effectDistance = new Vector2(1f, -1f);
            AddLayout(display, 150, 72);

            var displayLayout = display.AddComponent<VerticalLayoutGroup>();
            displayLayout.padding = new RectOffset(8, 8, 5, 6);
            displayLayout.spacing = 0;
            displayLayout.childAlignment = TextAnchor.MiddleCenter;
            displayLayout.childControlHeight = true;
            displayLayout.childControlWidth = true;
            displayLayout.childForceExpandHeight = false;
            displayLayout.childForceExpandWidth = true;

            drawnDieLabel = CreateText("Drawn Die Label", display.transform, "주사위", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            drawnDieLabel.color = MutedTextColor;
            AddLayout(drawnDieLabel.gameObject, -1, 22);

            var faceObject = CreateUiObject("Drawn Die Face", display.transform);
            drawnDieFaceImage = faceObject.AddComponent<Image>();
            drawnDieFaceImage.sprite = diceFaceSprite;
            drawnDieFaceImage.color = PanelColor;
            drawnDieFaceImage.raycastTarget = false;
            var faceShadow = faceObject.AddComponent<Shadow>();
            faceShadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
            faceShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var faceOutline = faceObject.AddComponent<Outline>();
            faceOutline.effectColor = new Color(0f, 0f, 0f, 0.14f);
            faceOutline.effectDistance = new Vector2(1f, -1f);
            AddLayout(faceObject, 46, 42);

            CreateDrawnDiePips(faceObject.transform);

            drawnDieText = CreateText("Drawn Die Value", faceObject.transform, "-", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            drawnDieText.color = MutedTextColor;
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
                new Vector2(-11f, 11f),
                new Vector2(11f, 11f),
                new Vector2(-11f, 0f),
                new Vector2(11f, 0f),
                new Vector2(-11f, -11f),
                new Vector2(11f, -11f)
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
                pipRect.sizeDelta = new Vector2(7.5f, 7.5f);
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
            backdrop.raycastTarget = true;

            var stageObject = CreateUiObject("Dice Cup Full Stage", diceOverlay.transform);
            var stageRect = stageObject.GetComponent<RectTransform>();
            stageRect.anchorMin = Vector2.zero;
            stageRect.anchorMax = Vector2.one;
            stageRect.offsetMin = Vector2.zero;
            stageRect.offsetMax = Vector2.zero;

            var rawImage = stageObject.AddComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = true;
            var dragSurface = stageObject.AddComponent<DiceCupDragSurface>();
            dragSurface.Initialize(this);
            CreateDiceCupStage(rawImage);

            SetDiceOverlayVisible(false);
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
            resultCardImage.sprite = diceFaceSprite;
            resultCardImage.color = PanelColor;
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
            AddLayout(resultTitleText.gameObject, -1, 54);

            resultDetailText = CreateText("Result Detail", card.transform, "구간 승수 0 : 0", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            resultDetailText.color = MutedTextColor;
            AddLayout(resultDetailText.gameObject, -1, 38);

            var resultResetButton = CreateButton("Result Reset Button", card.transform, "새 게임", 25);
            resultResetButton.image.color = new Color32(65, 73, 82, 255);
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
            cardImage.sprite = diceFaceSprite;
            cardImage.color = PanelColor;
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
            title.color = TextColor;
            AddLayout(title.gameObject, -1, 56);

            var subtitle = CreateText("Mode Selection Subtitle", card.transform, "PVP는 플레이어끼리, PVE는 플레이어 1 대 AI로 진행합니다.", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitle.color = MutedTextColor;
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

            var pvpButton = CreateButton("PVP Mode Button", buttons.transform, "PVP", 30);
            pvpButton.image.color = PlayerAccentColors[0];
            pvpButton.onClick.AddListener(() => SelectMatchMode(MatchMode.Pvp));
            AddControlButtonDepth(pvpButton.gameObject);
            var pvpText = pvpButton.GetComponentInChildren<Text>();
            if (pvpText != null)
            {
                pvpText.color = Color.white;
            }

            AddLayout(pvpButton.gameObject, 210, 72);

            var pveButton = CreateButton("PVE Mode Button", buttons.transform, "PVE", 30);
            pveButton.image.color = PlayerAccentColors[1];
            pveButton.onClick.AddListener(() => SelectMatchMode(MatchMode.Pve));
            AddControlButtonDepth(pveButton.gameObject);
            var pveText = pveButton.GetComponentInChildren<Text>();
            if (pveText != null)
            {
                pveText.color = Color.white;
            }

            AddLayout(pveButton.gameObject, 210, 72);

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
            diceRenderTexture = new RenderTexture(1600, 900, 16, RenderTextureFormat.ARGB32)
            {
                name = "Dice Cup Render Texture"
            };
            diceRenderTexture.Create();
            target.texture = diceRenderTexture;

            diceStageRoot = new GameObject("Dice Cup 3D Stage").transform;
            diceStageRoot.SetParent(transform, false);
            diceStageRoot.position = Vector3.zero;

            cupMaterial = CreateStageMaterial("Cup Outer Material", new Color(0.05f, 0.07f, 0.06f, 1f), false);
            cupInnerMaterial = CreateStageMaterial("Cup Inner Material", new Color(0.58f, 0.04f, 0.03f, 1f), false);
            dieMaterial = CreateStageMaterial("Die Material", new Color(0.96f, 0.95f, 0.9f, 1f), false);
            pipMaterial = CreateStageMaterial("Pip Material", new Color(0.08f, 0.09f, 0.1f, 1f), false);
            tableMaterial = CreateStageMaterial("Wood Table Material", new Color(0.62f, 0.34f, 0.14f, 1f), false);
            woodGrainMaterial = CreateStageMaterial("Wood Grain Material", new Color(0.38f, 0.2f, 0.07f, 1f), false);
            feltMaterial = CreateStageMaterial("Red Felt Material", new Color(0.52f, 0.12f, 0.1f, 1f), false);
            trayRimMaterial = CreateStageMaterial("Tray Rim Material", new Color(0.28f, 0.32f, 0.31f, 1f), false);
            trayHighlightMaterial = CreateStageMaterial("Tray Highlight Material", new Color(0.54f, 0.58f, 0.54f, 1f), false);

            CreateStageCamera();
            CreateStageLights();
            CreateStageTable();
            CreateWorldDie();
            CreateWorldCup();
            ResetDiceStage();
        }

        private void CreateBoards(Transform parent)
        {
            var boardsRow = CreateUiObject("Boards", parent);
            var boardsLayout = boardsRow.AddComponent<HorizontalLayoutGroup>();
            boardsLayout.spacing = 16;
            boardsLayout.childControlHeight = true;
            boardsLayout.childControlWidth = true;
            boardsLayout.childForceExpandHeight = true;
            boardsLayout.childForceExpandWidth = false;
            boardsLayout.childAlignment = TextAnchor.MiddleCenter;
            AddLayout(boardsRow, -1, 590, flexibleHeight: 1);

            boards[0] = CreatePlayerBoard(boardsRow.transform, 0);
            CreateScoreComparison(boardsRow.transform);
            boards[1] = CreatePlayerBoard(boardsRow.transform, 1);
        }

        private PlayerBoard CreatePlayerBoard(Transform parent, int playerIndex)
        {
            var board = new PlayerBoard(playerIndex, BoardSize);

            var panel = CreateUiObject($"Player {playerIndex + 1} Panel", parent);
            board.PanelImage = panel.AddComponent<Image>();
            board.PanelImage.color = PanelColor;
            var panelShadow = panel.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
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
            AddLayout(panel, 548, 590);

            var titleRow = CreateUiObject("Title Row", panel.transform);
            var titleLayout = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 12;
            titleLayout.childAlignment = TextAnchor.MiddleCenter;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = true;
            titleLayout.childForceExpandWidth = false;
            AddLayout(titleRow, -1, 38);

            board.TitleText = CreateText("Title", titleRow.transform, $"플레이어 {playerIndex + 1}", 23, FontStyle.Bold, TextAnchor.MiddleLeft);
            board.TitleText.color = PlayerAccentColors[playerIndex];
            AddLayout(board.TitleText.gameObject, 260, 38, flexibleWidth: 1);

            board.ProgressText = CreateText("Progress", titleRow.transform, "0/25", 23, FontStyle.Bold, TextAnchor.MiddleRight);
            board.ProgressText.color = PlayerAccentColors[playerIndex];
            AddLayout(board.ProgressText.gameObject, 88, 38);

            CreateBoardProgressBar(panel.transform, board, playerIndex);

            var gridWrap = CreateUiObject("Grid With Section Labels", panel.transform);
            var gridWrapLayout = gridWrap.AddComponent<HorizontalLayoutGroup>();
            gridWrapLayout.spacing = 6;
            gridWrapLayout.childAlignment = TextAnchor.MiddleCenter;
            gridWrapLayout.childControlHeight = true;
            gridWrapLayout.childControlWidth = true;
            gridWrapLayout.childForceExpandHeight = false;
            gridWrapLayout.childForceExpandWidth = false;
            AddLayout(gridWrap, -1, 492);

            var labelStrip = CreateUiObject("Section Labels", gridWrap.transform);
            var labelLayout = labelStrip.AddComponent<VerticalLayoutGroup>();
            labelLayout.spacing = 4;
            labelLayout.childAlignment = TextAnchor.MiddleCenter;
            labelLayout.childControlHeight = true;
            labelLayout.childControlWidth = true;
            labelLayout.childForceExpandHeight = false;
            labelLayout.childForceExpandWidth = false;
            AddLayout(labelStrip, 24, 492);

            for (var section = 0; section < BoardSize; section++)
            {
                CreateSectionLabel(labelStrip.transform, board, playerIndex, section);
            }

            var gridObject = CreateUiObject("Grid", gridWrap.transform);
            var gridImage = gridObject.AddComponent<Image>();
            gridImage.color = new Color32(216, 219, 216, 255);
            var gridShadow = gridObject.AddComponent<Shadow>();
            gridShadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            gridShadow.effectDistance = new Vector2(2f, -2f);
            var gridOutline = gridObject.AddComponent<Outline>();
            gridOutline.effectColor = new Color(0f, 0f, 0f, 0.1f);
            gridOutline.effectDistance = new Vector2(1f, -1f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardSize;
            grid.cellSize = new Vector2(92, 92);
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(8, 8, 8, 8);
            AddLayout(gridObject, 492, 492);

            for (var row = 0; row < BoardSize; row++)
            {
                for (var column = 0; column < BoardSize; column++)
                {
                    var capturedRow = row;
                    var capturedColumn = column;
                    var cellButton = CreateButton($"Cell {row},{column}", gridObject.transform, string.Empty, 36);
                    cellButton.image.color = EmptyCellColor;
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

            CreateFrontEdgeMarker(gridObject.transform, playerIndex);
            if (playerIndex == 0)
            {
                labelStrip.transform.SetAsLastSibling();
            }

            return board;
        }

        private void CreateFrontEdgeMarker(Transform gridTransform, int playerIndex)
        {
            var marker = CreateUiObject("Front Edge Marker", gridTransform);
            var layoutElement = marker.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            var markerImage = marker.AddComponent<Image>();
            markerImage.sprite = diceFaceSprite;
            var markerColor = PlayerAccentColors[playerIndex];
            markerColor.a = 0.72f;
            markerImage.color = markerColor;
            markerImage.raycastTarget = false;

            var markerRect = marker.GetComponent<RectTransform>();
            var isRightEdge = playerIndex == 0;
            markerRect.anchorMin = new Vector2(isRightEdge ? 1f : 0f, 0f);
            markerRect.anchorMax = new Vector2(isRightEdge ? 1f : 0f, 1f);
            markerRect.pivot = new Vector2(isRightEdge ? 1f : 0f, 0.5f);
            markerRect.sizeDelta = new Vector2(7f, -16f);
            markerRect.anchoredPosition = new Vector2(isRightEdge ? -5f : 5f, 0f);
            marker.transform.SetAsLastSibling();
        }

        private void CreateBoardProgressBar(Transform parent, PlayerBoard board, int playerIndex)
        {
            var track = CreateUiObject("Board Progress Track", parent);
            board.ProgressTrackImage = track.AddComponent<Image>();
            board.ProgressTrackImage.sprite = diceFaceSprite;
            board.ProgressTrackImage.color = new Color32(220, 225, 221, 255);
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

        private void CreateSectionLabel(Transform parent, PlayerBoard board, int playerIndex, int section)
        {
            var slot = CreateUiObject($"Section Label Slot {section + 1}", parent);
            AddLayout(slot, 24, 92);

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
            label.color = PlayerAccentColors[playerIndex];
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
            faceImage.sprite = diceFaceSprite;
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
            var panel = CreateUiObject("Section Score Comparison", parent);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = ScorePanelColor;
            var panelShadow = panel.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.1f);
            panelShadow.effectDistance = new Vector2(3f, -3f);

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(6, 6, 10, 18);
            panelLayout.spacing = 4;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;
            AddLayout(panel, 180, 590);

            var title = CreateText("Score Comparison Title", panel.transform, "구간 점수", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = TextColor;
            AddLayout(title.gameObject, -1, 42);

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
                AddLayout(row, -1, 92);

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
            badgeImage.sprite = scoreBadgeSprite;
            badgeImage.color = ScoreNeutralColor;
            var badgeShadow = badge.AddComponent<Shadow>();
            badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            badgeShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var badgeOutline = badge.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(1f, 1f, 1f, 0.2f);
            badgeOutline.effectDistance = new Vector2(1f, -1f);
            sectionScoreImages[player, section] = badgeImage;
            AddLayout(badge, 52, 52);

            var scoreText = CreateText("Score", badge.transform, "0", 25, FontStyle.Bold, TextAnchor.MiddleCenter);
            scoreText.color = ScoreTextColor;
            sectionScoreTexts[player, section] = scoreText;

            var scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = Vector2.zero;
            scoreRect.anchorMax = Vector2.one;
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;
        }

        private void CreateStageCamera()
        {
            var cameraObject = new GameObject("Dice Cup Camera");
            cameraObject.transform.SetParent(diceStageRoot, false);
            cameraObject.transform.localPosition = new Vector3(0f, 5.8f, -0.85f);
            cameraObject.transform.LookAt(diceStageRoot.position + new Vector3(0f, 0.05f, 0.05f));

            diceCamera = cameraObject.AddComponent<Camera>();
            diceCamera.clearFlags = CameraClearFlags.SolidColor;
            diceCamera.backgroundColor = new Color32(177, 110, 48, 255);
            diceCamera.fieldOfView = 38f;
            diceCamera.nearClipPlane = 0.1f;
            diceCamera.farClipPlane = 30f;
            diceCamera.targetTexture = diceRenderTexture;
        }

        private void CreateStageLights()
        {
            var keyLight = new GameObject("Dice Cup Key Light");
            keyLight.transform.SetParent(diceStageRoot, false);
            keyLight.transform.localPosition = new Vector3(-1.8f, 3.2f, -2.8f);
            var key = keyLight.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.15f;
            keyLight.transform.rotation = Quaternion.Euler(48f, -24f, 0f);

            var fillLight = new GameObject("Dice Cup Fill Light");
            fillLight.transform.SetParent(diceStageRoot, false);
            fillLight.transform.localPosition = new Vector3(2.2f, 1.8f, -2.2f);
            var fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 1.2f;
            fill.range = 5f;
        }

        private void CreateStageTable()
        {
            CreateStageCube("Wood Table", new Vector3(0f, -0.14f, 0.02f), new Vector3(4.8f, 0.08f, 3.15f), tableMaterial, true);
            for (var index = 0; index < 8; index++)
            {
                var z = -1.34f + index * 0.38f;
                CreateStageCube($"Wood Grain {index + 1}", new Vector3(0f, -0.095f, z), new Vector3(4.65f, 0.012f, 0.018f), woodGrainMaterial);
            }

            CreateStageCube("Red Felt", new Vector3(-0.36f, -0.08f, 0.02f), new Vector3(2.75f, 0.06f, 1.85f), feltMaterial, true);
            CreateStageCube("Tray Top Rail", new Vector3(-0.36f, 0.02f, 1.05f), new Vector3(3.05f, 0.22f, 0.16f), trayRimMaterial, true);
            CreateStageCube("Tray Bottom Rail", new Vector3(-0.36f, 0.02f, -1.01f), new Vector3(3.05f, 0.22f, 0.16f), trayRimMaterial, true);
            CreateStageCube("Tray Left Rail", new Vector3(-1.94f, 0.02f, 0.02f), new Vector3(0.16f, 0.22f, 2.18f), trayRimMaterial, true);
            CreateStageCube("Tray Right Rail", new Vector3(1.22f, 0.02f, 0.02f), new Vector3(0.16f, 0.22f, 2.18f), trayRimMaterial, true);
            CreateStageCube("Tray Top Highlight", new Vector3(-0.36f, 0.145f, 1.05f), new Vector3(2.78f, 0.018f, 0.026f), trayHighlightMaterial);
            CreateStageCube("Tray Bottom Highlight", new Vector3(-0.36f, 0.145f, -1.01f), new Vector3(2.78f, 0.018f, 0.026f), trayHighlightMaterial);
            CreateStageCube("Tray Left Highlight", new Vector3(-1.94f, 0.145f, 0.02f), new Vector3(0.026f, 0.018f, 1.9f), trayHighlightMaterial);
            CreateStageCube("Tray Right Highlight", new Vector3(1.22f, 0.145f, 0.02f), new Vector3(0.026f, 0.018f, 1.9f), trayHighlightMaterial);
            CreateInvisibleStageCollider("Dice Safety Top Wall", new Vector3(-0.36f, 0.36f, 0.84f), new Vector3(2.95f, 0.9f, 0.08f));
            CreateInvisibleStageCollider("Dice Safety Bottom Wall", new Vector3(-0.36f, 0.36f, -0.8f), new Vector3(2.95f, 0.9f, 0.08f));
            CreateInvisibleStageCollider("Dice Safety Left Wall", new Vector3(-1.66f, 0.36f, 0.02f), new Vector3(0.08f, 0.9f, 1.72f));
            CreateInvisibleStageCollider("Dice Safety Right Wall", new Vector3(0.98f, 0.36f, 0.02f), new Vector3(0.08f, 0.9f, 1.72f));
        }

        private void CreateStageCube(string name, Vector3 position, Vector3 scale, Material material, bool keepCollider = false)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(diceStageRoot, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            if (!keepCollider)
            {
                Destroy(cube.GetComponent<Collider>());
            }
        }

        private void CreateInvisibleStageCollider(string name, Vector3 position, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(diceStageRoot, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
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
            diceCupBody.interpolation = RigidbodyInterpolation.Interpolate;
            diceCupBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var outerWall = new GameObject("Cup Outer Wall");
            outerWall.transform.SetParent(cup.transform, false);
            var outerFilter = outerWall.AddComponent<MeshFilter>();
            cupOuterMesh = CreateOpenCupMesh(0.62f, 0.82f, 48, false);
            outerFilter.sharedMesh = cupOuterMesh;
            outerWall.AddComponent<MeshRenderer>().material = cupMaterial;

            var innerWall = new GameObject("Cup Red Interior");
            innerWall.transform.SetParent(cup.transform, false);
            innerWall.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            var innerFilter = innerWall.AddComponent<MeshFilter>();
            cupInnerMesh = CreateOpenCupMesh(0.5f, 0.72f, 48, true);
            innerFilter.sharedMesh = cupInnerMesh;
            innerWall.AddComponent<MeshRenderer>().material = cupInnerMaterial;
            var innerCollider = innerWall.AddComponent<MeshCollider>();
            innerCollider.sharedMesh = cupInnerMesh;
            innerCollider.convex = false;
            innerCollider.material = worldDiePhysicsMaterial;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Cup Red Floor";
            floor.transform.SetParent(cup.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            floor.transform.localScale = new Vector3(1f, 0.025f, 1f);
            floor.GetComponent<Renderer>().material = cupInnerMaterial;
            Destroy(floor.GetComponent<Collider>());
            var floorCollider = floor.AddComponent<BoxCollider>();
            floorCollider.size = new Vector3(0.94f, 2f, 0.94f);
            floorCollider.material = worldDiePhysicsMaterial;

            var rim = new GameObject("Cup Thick Rim");
            rim.transform.SetParent(cup.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.41f, 0f);
            var rimFilter = rim.AddComponent<MeshFilter>();
            cupRimMesh = CreateRingMesh(0.49f, 0.69f, 48);
            rimFilter.sharedMesh = cupRimMesh;
            rim.AddComponent<MeshRenderer>().material = cupMaterial;
        }

        private void CreateWorldDie()
        {
            var die = new GameObject("World Die");
            die.transform.SetParent(diceStageRoot, false);
            die.transform.localScale = Vector3.one * 0.42f;
            worldDieMesh = CreateRoundedCubeMesh(0.5f, 0.075f, 4);
            var dieFilter = die.AddComponent<MeshFilter>();
            dieFilter.sharedMesh = worldDieMesh;
            die.AddComponent<MeshRenderer>().material = dieMaterial;
            var dieCollider = die.AddComponent<MeshCollider>();
            dieCollider.sharedMesh = worldDieMesh;
            dieCollider.convex = true;
            worldDieCollider = dieCollider;
            if (worldDieCollider != null)
            {
                worldDiePhysicsMaterial = new PhysicsMaterial("World Die Physics")
                {
                    dynamicFriction = 0.16f,
                    staticFriction = 0.2f,
                    bounciness = 0.28f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
                worldDieCollider.material = worldDiePhysicsMaterial;
            }

            worldDieBody = die.AddComponent<Rigidbody>();
            worldDieBody.mass = 0.18f;
            worldDieBody.useGravity = false;
            worldDieBody.isKinematic = true;
            worldDieBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            worldDieBody.interpolation = RigidbodyInterpolation.Interpolate;
            worldDieBody.linearDamping = 0.45f;
            worldDieBody.angularDamping = 0.28f;
            worldDieBody.maxAngularVelocity = 24f;
            worldDieBody.solverIterations = 8;
            worldDieBody.solverVelocityIterations = 4;
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
                pip.GetComponent<Renderer>().material = pipMaterial;
                Destroy(pip.GetComponent<Collider>());
            }
        }

        private void CreateControls(Transform parent)
        {
            var controls = CreateUiObject("Controls", parent);
            var controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 18;
            controlsLayout.childAlignment = TextAnchor.MiddleCenter;
            controlsLayout.childControlHeight = true;
            controlsLayout.childControlWidth = true;
            controlsLayout.childForceExpandHeight = true;
            controlsLayout.childForceExpandWidth = false;
            AddLayout(controls, -1, 62);

            rollButton = CreateButton("Roll Button", controls.transform, "컵 굴리기", 26);
            rollButton.image.color = ButtonColor;
            rollButtonText = rollButton.GetComponentInChildren<Text>();
            rollButton.onClick.AddListener(RollDie);
            AddControlButtonDepth(rollButton.gameObject);
            AddLayout(rollButton.gameObject, 190, 56);

            resetButton = CreateButton("Reset Button", controls.transform, "새 게임", 26);
            resetButton.image.color = new Color32(65, 73, 82, 255);
            resetButton.onClick.AddListener(StartNewGame);
            AddControlButtonDepth(resetButton.gameObject);
            var resetButtonText = resetButton.GetComponentInChildren<Text>();
            if (resetButtonText != null)
            {
                resetButtonText.color = Color.white;
            }

            AddLayout(resetButton.gameObject, 190, 56);

            modeButton = CreateButton("Mode Select Button", controls.transform, "모드 선택", 24);
            modeButton.image.color = ScoreNeutralColor;
            modeButton.onClick.AddListener(ShowModeSelection);
            AddControlButtonDepth(modeButton.gameObject);
            var modeButtonText = modeButton.GetComponentInChildren<Text>();
            if (modeButtonText != null)
            {
                modeButtonText.color = Color.white;
            }

            AddLayout(modeButton.gameObject, 190, 56);
        }

        private void StartNewGame()
        {
            StopAllCoroutines();
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
        }

        private void ShowModeSelection()
        {
            StopAllCoroutines();
            gameStarted = false;
            isRolling = false;
            isAttackAnimating = false;
            isAiThinking = false;
            hasPendingDie = false;
            SetDiceOverlayVisible(false);
            ClearAttackAnimationLayer();
            if (modeSelectionOverlay != null)
            {
                modeSelectionOverlay.SetActive(true);
                modeSelectionOverlay.transform.SetAsLastSibling();
            }

            RefreshView();
        }

        private void SelectMatchMode(MatchMode mode)
        {
            currentMode = mode;
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
            SetDiceOverlayVisible(true);
            ResetDiceStage();
            RefreshView();
        }

        private void BeginCupShake(Vector2 screenPosition)
        {
            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted)
            {
                return;
            }

            isCupDragging = true;
            cupDragStartScreenPosition = screenPosition;
            cupDragStartLocalPosition = diceCupTransform != null
                ? diceCupTransform.localPosition
                : CupHomePosition;
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

            RecordShakeDelta(delta);
            shakeEnergy = Mathf.Min(2.35f, shakeEnergy + Mathf.Max(0.025f, delta.magnitude * 0.009f));
            ApplyCupShakePose(screenPosition, delta);
        }

        private void EndCupShake()
        {
            if (!isRolling || !isWaitingForCupShake || cupReleaseStarted)
            {
                return;
            }

            isCupDragging = false;
            isWaitingForCupShake = false;
            cupReleaseStarted = true;
            shakeEnergy = Mathf.Max(shakeEnergy, 0.18f);
            StartCoroutine(RollDieRoutine(BuildShakeMotionSeed()));
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
            var cupPosition = cupDragStartLocalPosition + new Vector3(
                dragOffset.x / screenWidth * 1.85f,
                Mathf.Clamp(delta.magnitude * 0.0015f, 0f, 0.065f),
                dragOffset.y / screenHeight * 1.35f);
            cupPosition.x = Mathf.Clamp(cupPosition.x, -0.82f, 1.02f);
            cupPosition.y = Mathf.Clamp(cupPosition.y, CupHomePosition.y, CupHomePosition.y + 0.08f);
            cupPosition.z = Mathf.Clamp(cupPosition.z, -0.58f, 0.68f);

            var tiltX = Mathf.Clamp(delta.y * 0.07f, -10f, 10f);
            var tiltY = Mathf.Clamp(delta.x * 0.04f, -11f, 11f);
            var tiltZ = Mathf.Clamp(-delta.x * 0.085f, -14f, 14f);
            var cupRotation = Quaternion.Euler(tiltX, tiltY, tiltZ);
            SetCupPose(cupPosition, cupRotation, false);
        }

        private IEnumerator RollDieRoutine(int finalDie)
        {
            worldDieHasTouchedSurface = false;
            var startCupPosition = diceCupTransform.localPosition;
            var startCupRotation = diceCupTransform.localRotation;
            yield return PourDieFromCupRoutine(startCupPosition, startCupRotation);
            yield return RollDieOnTableRoutine(finalDie);
            CommitRolledDie(GetWorldDieTopFaceValue());
            hasPendingDie = true;
            RefreshView();
            yield return new WaitForSeconds(0.7f);

            isRolling = false;
            SetDiceOverlayVisible(false);
            RefreshView();
        }

        private IEnumerator PourDieFromCupRoutine(Vector3 startCupPosition, Quaternion startCupRotation)
        {
            const float pourDuration = 0.62f;
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
            yield return new WaitForFixedUpdate();
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
            var lateral = new Vector3(-horizontalDirection.z, 0f, horizontalDirection.x) * Random.Range(-0.18f, 0.18f);
            var launchSpeed = Mathf.Lerp(1.25f, 1.9f, shake01) + shakeDirectionChanges * 0.012f;
            var cupOpeningDirection = diceCupTransform != null ? diceCupTransform.up : horizontalDirection;
            cupOpeningDirection.y = Mathf.Max(cupOpeningDirection.y, 0.12f);
            cupOpeningDirection.Normalize();
            var launchDirection = (horizontalDirection * 0.4f + cupOpeningDirection * 0.6f).normalized;
            var spinAxis = Vector3.Cross(Vector3.up, horizontalDirection).normalized;
            var compactSeed = spinSeedValue % 997;
            var spinVariance = new Vector3(
                Mathf.Sin(compactSeed * 1.73f) * 1.5f,
                Mathf.Cos(compactSeed * 2.19f) * 2f,
                Mathf.Sin(compactSeed * 2.61f) * 1.5f);

            worldDieBody.AddForce(launchDirection * launchSpeed + lateral + Vector3.up * Mathf.Lerp(0.58f, 0.9f, shake01), ForceMode.VelocityChange);
            worldDieBody.AddTorque(
                spinAxis * Mathf.Lerp(11f, 18f, shake01) +
                spinVariance +
                Random.insideUnitSphere * Mathf.Lerp(2.2f, 4.2f, shake01),
                ForceMode.VelocityChange);

            const float minRollDuration = 1.05f;
            const float stableDurationRequired = 0.34f;
            const float naturalSettleWindow = 4.8f;
            var elapsed = 0f;
            var stableDuration = 0f;
            var appliedCupExitAssist = false;
            while (elapsed < naturalSettleWindow || stableDuration < stableDurationRequired)
            {
                elapsed += Time.deltaTime;
                RecoverEscapedWorldDie();

                if (!appliedCupExitAssist && elapsed >= 0.34f && IsWorldDieInsideCup())
                {
                    appliedCupExitAssist = true;
                    worldDieBody.AddForce(launchDirection * 1.15f + Vector3.up * 0.28f, ForceMode.VelocityChange);
                    worldDieBody.AddTorque(Random.onUnitSphere * 3.6f, ForceMode.VelocityChange);
                }

                if (elapsed > 3.4f)
                {
                    var assist = Mathf.InverseLerp(3.4f, 5.8f, elapsed);
                    worldDieBody.linearDamping = Mathf.Lerp(0.18f, 1.8f, assist);
                    worldDieBody.angularDamping = Mathf.Lerp(0.08f, 1.55f, assist);
                }

                var isStable = elapsed >= minRollDuration
                    && worldDieHasTouchedSurface
                    && worldDieBody.linearVelocity.sqrMagnitude < 0.0036f
                    && worldDieBody.angularVelocity.sqrMagnitude < 0.0064f
                    && GetWorldDieTopFaceDot() > 0.88f;
                stableDuration = isStable ? stableDuration + Time.deltaTime : 0f;
                if (stableDuration >= stableDurationRequired)
                {
                    break;
                }

                if (elapsed >= 7.2f)
                {
                    break;
                }

                yield return null;
            }

            var assistedSettleElapsed = 0f;
            worldDieBody.linearDamping = 3.2f;
            worldDieBody.angularDamping = 2.8f;
            while (assistedSettleElapsed < 1.4f
                && (worldDieBody.linearVelocity.sqrMagnitude > 0.0025f
                    || worldDieBody.angularVelocity.sqrMagnitude > 0.0049f))
            {
                assistedSettleElapsed += Time.deltaTime;
                RecoverEscapedWorldDie();
                yield return null;
            }

            worldDieBody.Sleep();
            worldDieBody.useGravity = false;
            worldDieBody.isKinematic = true;
            worldDieBody.constraints = RigidbodyConstraints.None;
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
            currentDie = 0;
            hasPendingDie = false;
            pendingDieCanAttack = false;
            pendingDieCanPlaceOnOpponent = false;
            nextRollCanAttack = true;
            nextRollCanPlaceOnOpponent = false;

            AdvanceTurn();
            RefreshView();
            StartPlacementPulse(playerIndex, row, placedColumn);
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
            dieImage.sprite = diceFaceSprite;
            dieImage.color = GetCellFaceColor(value, attackProtected);
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

            var pipColor = attackProtected || value == 6 ? Color.white : (Color)new Color32(31, 35, 39, 255);
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
            SetDiceOverlayVisible(true);
            ResetDiceStage();
            RefreshView();

            yield return new WaitForSeconds(0.16f);
            yield return AnimateAiCupShakeRoutine();
            cupReleaseStarted = true;
            yield return RollDieRoutine(BuildShakeMotionSeed());
        }

        private IEnumerator AnimateAiCupShakeRoutine()
        {
            const float duration = 0.72f;
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
            var targetColumns = boards[targetPlayer].GetMatchingGroupColumns(row, column);
            if (targetColumns.Length == 0)
            {
                return int.MinValue;
            }

            var aiScore = boards[AiPlayerIndex].CalculateSectionScoreUnits(row);
            var humanBefore = boards[0].CalculateSectionScoreUnits(row);
            var humanAfter = CalculateSectionScoreUnitsWithoutColumns(boards[0], row, targetColumns);
            var beforeOutcome = EvaluateSectionForAi(aiScore, humanBefore);
            var afterOutcome = EvaluateSectionForAi(aiScore, humanAfter);
            var beforeMatch = EvaluateProjectedMatchForAi(row, aiScore, humanBefore);
            var afterMatch = EvaluateProjectedMatchForAi(row, aiScore, humanAfter);
            var removedScore = humanBefore - humanAfter;
            var rowPressure = boards[AiPlayerIndex].CountSectionFilled(row) * 22;
            return targetColumns.Length * 320 + removedScore * 6 + (afterOutcome - beforeOutcome) + (afterMatch - beforeMatch) + rowPressure + currentDie * 20 + Random.Range(0, 22);
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
            var counts = new int[7];
            for (var column = 0; column < BoardSize; column++)
            {
                var value = board.Cells[row, column];
                if (value > 0 && value < counts.Length)
                {
                    counts[value]++;
                }
            }

            if (extraValue > 0 && extraValue < counts.Length)
            {
                counts[extraValue]++;
            }

            return CalculateScoreUnitsFromCounts(counts);
        }

        private int CalculateSectionScoreUnitsWithoutColumns(PlayerBoard board, int row, int[] ignoredColumns)
        {
            var counts = new int[7];
            for (var column = 0; column < BoardSize; column++)
            {
                if (IsIgnoredColumn(ignoredColumns, column))
                {
                    continue;
                }

                var value = board.Cells[row, column];
                if (value > 0 && value < counts.Length)
                {
                    counts[value]++;
                }
            }

            return CalculateScoreUnitsFromCounts(counts);
        }

        private static bool IsIgnoredColumn(int[] ignoredColumns, int column)
        {
            for (var index = 0; index < ignoredColumns.Length; index++)
            {
                if (ignoredColumns[index] == column)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateScoreUnitsFromCounts(int[] counts)
        {
            var scoreUnits = 0;
            for (var value = 1; value < counts.Length; value++)
            {
                var count = counts[value];
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
                board.ProgressText.text = $"{board.FilledCount}/{board.Capacity}";
                RefreshBoardProgressBar(board, player);
                var isActiveBoard = player == activePlayer && !placementComplete;
                board.PanelImage.color = isActiveBoard ? new Color32(240, 250, 247, 255) : PanelColor;
                if (board.PanelOutline != null)
                {
                    board.PanelOutline.effectColor = isActiveBoard ? PlayerAccentColors[player] : Color.clear;
                    board.PanelOutline.effectDistance = isActiveBoard ? new Vector2(4f, -4f) : Vector2.zero;
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
                        button.image.color = GetCellColor(player, column, value, canPlace, canAttack);
                        RefreshCellPips(board, row, column, value, canPlace, canAttack, isAttackProtected);
                        RefreshCellOutline(board, row, column, canPlace, canAttack);
                    }
                }
            }

            RefreshScoreComparison();

            rollButton.interactable = gameStarted && !IsAiTurnActive() && !placementComplete && !hasPendingDie && !isRolling && !isAttackAnimating && !boards[activePlayer].IsFull;
            rollButton.image.color = rollButton.interactable ? ButtonColor : DisabledButtonColor;
            if (rollButtonText != null)
            {
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
                    rollButtonText.text = "배치 대기";
                }
                else
                {
                    rollButtonText.text = nextRollCanAttack ? "컵 굴리기" : "보너스 굴리기";
                }
            }
            resetButton.interactable = true;
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
            statusText.color = placementComplete ? TextColor : PlayerAccentColors[activePlayer];
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

            var baseColor = new Color32(247, 249, 246, 255);
            headerImage.color = placementComplete
                ? baseColor
                : gameStarted ? Color.Lerp(baseColor, PlayerAccentColors[activePlayer], 0.08f) : baseColor;
        }

        private void RefreshBoardProgressBar(PlayerBoard board, int player)
        {
            if (board.ProgressTrackImage != null)
            {
                board.ProgressTrackImage.color = board.IsFull
                    ? new Color32(226, 232, 226, 255)
                    : new Color32(220, 225, 221, 255);
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
                face.color = isPreview
                    ? GetPreviewCellFaceColor(displayValue, displayProtected)
                    : GetCellFaceColor(displayValue, displayProtected);
            }

            var pipColor = displayProtected || displayValue == 6 ? Color.white : (Color)new Color32(31, 35, 39, 255);
            if (isPreview)
            {
                pipColor.a = displayProtected || displayValue == 6 ? 0.9f : 0.72f;
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
                markerColor.a = isPreview ? 0.66f : 1f;
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
                drawnDieImage.color = PanelColor;
                drawnDieLabel.text = "공격";
                drawnDieLabel.color = MutedTextColor;
                drawnDieFaceImage.color = PanelColor;
                drawnDieText.text = string.Empty;
                RefreshDrawnDiePips(0, Color.clear);
                SetDrawnDieProtectionMarker(false, 0f);
                return;
            }

            if (hasPendingDie && currentDie > 0)
            {
                drawnDieImage.color = pendingDieCanAttack ? DieColors[Mathf.Clamp(currentDie - 1, 0, DieColors.Length - 1)] : Color.black;
                var textColor = !pendingDieCanAttack || currentDie == 6 ? Color.white : TextColor;
                drawnDieLabel.text = pendingDieCanAttack ? "주사위" : "공격 불가";
                drawnDieLabel.color = textColor;
                drawnDieFaceImage.color = pendingDieCanAttack ? DieColors[Mathf.Clamp(currentDie - 1, 0, DieColors.Length - 1)] : Color.black;
                drawnDieText.text = string.Empty;
                RefreshDrawnDiePips(currentDie, textColor);
                SetDrawnDieProtectionMarker(!pendingDieCanAttack, 1f);
                return;
            }

            if (isRolling)
            {
                drawnDieImage.color = pendingDieCanAttack ? new Color32(255, 246, 226, 255) : Color.black;
                drawnDieLabel.text = pendingDieCanAttack ? "주사위" : "공격 불가";
                drawnDieLabel.color = pendingDieCanAttack ? MutedTextColor : Color.white;
                drawnDieFaceImage.color = pendingDieCanAttack ? PanelColor : Color.black;
                drawnDieText.color = pendingDieCanAttack ? TextColor : Color.white;
                drawnDieText.text = "?";
                RefreshDrawnDiePips(0, Color.clear);
                SetDrawnDieProtectionMarker(!pendingDieCanAttack, 0.7f);
                return;
            }

            drawnDieImage.color = PanelColor;
            drawnDieLabel.text = "주사위";
            drawnDieLabel.color = MutedTextColor;
            drawnDieFaceImage.color = PanelColor;
            drawnDieText.color = MutedTextColor;
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
                matchScoreImage.color = ScorePanelColor;
                matchScoreText.color = TextColor;
                matchScoreLabel.color = MutedTextColor;
                return;
            }

            var winner = playerOneWins > playerTwoWins ? 0 : 1;
            var background = PlayerAccentColors[winner];
            background.a = 0.18f;
            matchScoreImage.color = background;
            matchScoreText.color = PlayerAccentColors[winner];
            matchScoreLabel.color = PlayerAccentColors[winner];
        }

        private void RefreshResultBanner()
        {
            if (resultBanner == null)
            {
                return;
            }

            resultBanner.SetActive(placementComplete);
            if (!placementComplete)
            {
                return;
            }

            var playerOneWins = CountSectionWins(0);
            var playerTwoWins = CountSectionWins(1);
            if (resultDetailText != null)
            {
                resultDetailText.text = $"구간 승수 {playerOneWins} : {playerTwoWins}";
            }

            if (playerOneWins == playerTwoWins)
            {
                if (resultTitleText != null)
                {
                    resultTitleText.text = "무승부";
                    resultTitleText.color = TieColor;
                }

                if (resultCardImage != null)
                {
                    resultCardImage.color = PanelColor;
                }

                return;
            }

            var winner = playerOneWins > playerTwoWins ? 0 : 1;
            if (resultTitleText != null)
            {
                resultTitleText.text = $"{GetPlayerDisplayName(winner)} 승리";
                resultTitleText.color = PlayerAccentColors[winner];
            }

            if (resultCardImage != null)
            {
                resultCardImage.color = Color.Lerp(PanelColor, PlayerAccentColors[winner], 0.12f);
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

            worldDieBody.isKinematic = true;
            worldDieBody.useGravity = false;
            worldDieBody.constraints = RigidbodyConstraints.None;
            worldDieBody.position = diceStageRoot.TransformPoint(GetDiePositionInCup(CupHomePosition, Quaternion.identity, DieInCupOffset));
            worldDieBody.rotation = diceStageRoot.rotation * Random.rotationUniform;
            worldDieBody.linearDamping = 0.07f;
            worldDieBody.angularDamping = 0.035f;
            if (worldDieCollider != null)
            {
                worldDieCollider.enabled = true;
            }

            worldDieBody.isKinematic = false;
            worldDieBody.linearVelocity = Vector3.zero;
            worldDieBody.angularVelocity = Vector3.zero;
            worldDieBody.useGravity = true;
            worldDieBody.WakeUp();
        }

        private void PrepareWorldDieForManualPose()
        {
            worldDieHasTouchedSurface = false;
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
            worldDieBody.linearDamping = 0.07f;
            worldDieBody.angularDamping = 0.035f;
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
            var radialDistance = new Vector2(cupLocalPosition.x, cupLocalPosition.z).sqrMagnitude;
            return cupLocalPosition.y > -0.62f
                && cupLocalPosition.y < 0.56f
                && radialDistance < 0.3f;
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
        }

        private void SetDiceOverlayVisible(bool visible)
        {
            if (diceOverlay != null)
            {
                diceOverlay.SetActive(visible);
            }

            if (diceCamera != null)
            {
                diceCamera.enabled = visible;
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
                    sectionResultTexts[section].color = isEmptyTie ? MutedTextColor : TieColor;
                    var tieColor = isEmptyTie ? ScoreNeutralColor : TieColor;
                    sectionScoreImages[0, section].color = tieColor;
                    sectionScoreImages[1, section].color = tieColor;
                    SetSectionRowColor(section, isEmptyTie ? Color.clear : TieColor, isEmptyTie ? 0f : 0.1f);
                    continue;
                }

                sectionResultTexts[section].text = winner == 0
                    ? "1P승"
                    : currentMode == MatchMode.Pve ? "AI승" : "2P승";
                sectionResultTexts[section].color = PlayerAccentColors[winner];
                sectionScoreImages[0, section].color = winner == 0 ? WinColor : LoseColor;
                sectionScoreImages[1, section].color = winner == 1 ? WinColor : LoseColor;
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
            color.a = isAttackProtected ? 0.76f : 0.58f;
            return color;
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
            colors.pressedColor = new Color32(224, 233, 229, 255);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color32(214, 216, 213, 255);
            button.colors = colors;

            var text = CreateText("Label", buttonObject.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = TextColor;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
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

        private static Material CreateStageMaterial(string name, Color color, bool transparent)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
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
            var triangles = new int[segments * 6];
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[index * 2] = new Vector3(x, -height * 0.5f, z);
                vertices[index * 2 + 1] = new Vector3(x, height * 0.5f, z);
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
            var triangles = new int[segments * 6];
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var outer = new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius);
                var inner = new Vector3(Mathf.Cos(angle) * innerRadius, 0f, Mathf.Sin(angle) * innerRadius);
                vertices[index * 2] = outer;
                vertices[index * 2 + 1] = inner;
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
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (diceRenderTexture != null)
            {
                diceRenderTexture.Release();
                Destroy(diceRenderTexture);
                diceRenderTexture = null;
            }

            DestroyStageMaterial(cupMaterial);
            DestroyStageMaterial(cupInnerMaterial);
            DestroyStageMaterial(dieMaterial);
            DestroyStageMaterial(pipMaterial);
            DestroyStageMaterial(tableMaterial);
            DestroyStageMaterial(woodGrainMaterial);
            DestroyStageMaterial(feltMaterial);
            DestroyStageMaterial(trayRimMaterial);
            DestroyStageMaterial(trayHighlightMaterial);
            DestroyRuntimeObject(worldDiePhysicsMaterial);
            DestroyRuntimeObject(worldDieMesh);
            DestroyRuntimeObject(cupOuterMesh);
            DestroyRuntimeObject(cupInnerMesh);
            DestroyRuntimeObject(cupRimMesh);
            DestroyRuntimeSprite(scoreBadgeSprite);
            DestroyRuntimeSprite(diceFaceSprite);
            DestroyRuntimeSprite(dicePipSprite);
            if (ownsDefaultFont)
            {
                DestroyRuntimeObject(defaultFont);
            }
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

        private static void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
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
                if (controller == null || controller.IsDiceCupCollider(collision.collider))
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

        private sealed class DiceCupDragSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
        {
            private DiceBoardGameController controller;

            public void Initialize(DiceBoardGameController owner)
            {
                controller = owner;
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
        }

    }
}
