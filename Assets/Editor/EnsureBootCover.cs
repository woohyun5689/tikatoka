using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikatooka.Editor
{
    public static class EnsureBootCover
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string BootCoverName = "Tikatooka Boot Cover";
        private const string BackgroundName = "Felt Background";
        private const string BackdropTexturePath = "Assets/Resources/Art/TikatookaMainBoardBackdropV2.png";
        private static readonly Color32 BootBackgroundColor = new Color32(13, 39, 34, 255);

        [MenuItem("Tikatooka/Refresh Boot Cover")]
        private static void RefreshActiveSceneBootCover()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != MainScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Tikatooka",
                    "Main 씬을 연 뒤 다시 실행해 주세요.",
                    "확인");
                return;
            }

            CreateOrUpdate(activeScene);
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        public static GameObject CreateOrUpdate(Scene scene)
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.gameObject.scene != scene || !camera.CompareTag("MainCamera"))
                {
                    continue;
                }

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = BootBackgroundColor;
            }

            GameObject cover = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == BootCoverName)
                {
                    cover = root;
                    break;
                }
            }

            if (cover == null)
            {
                cover = new GameObject(
                    BootCoverName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(CanvasGroup));
                SceneManager.MoveGameObjectToScene(cover, scene);
            }

            cover.layer = LayerMask.NameToLayer("UI");
            cover.SetActive(true);

            var coverCanvas = GetOrAddComponent<Canvas>(cover);
            coverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            coverCanvas.overrideSorting = true;
            coverCanvas.sortingOrder = 32760;

            var scaler = GetOrAddComponent<CanvasScaler>(cover);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(cover);

            var group = GetOrAddComponent<CanvasGroup>(cover);
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = true;

            var coverRect = cover.GetComponent<RectTransform>();
            coverRect.anchorMin = Vector2.zero;
            coverRect.anchorMax = Vector2.one;
            coverRect.offsetMin = Vector2.zero;
            coverRect.offsetMax = Vector2.zero;
            coverRect.localPosition = Vector3.zero;
            coverRect.localRotation = Quaternion.identity;
            coverRect.localScale = Vector3.one;

            var background = cover.transform.Find(BackgroundName)?.gameObject;
            if (background == null)
            {
                background = new GameObject(BackgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                background.transform.SetParent(cover.transform, false);
            }

            background.layer = cover.layer;
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundRect.localRotation = Quaternion.identity;
            backgroundRect.localScale = Vector3.one;

            var oldRawImage = background.GetComponent<RawImage>();
            if (oldRawImage != null)
            {
                Object.DestroyImmediate(oldRawImage, true);
            }

            var image = GetOrAddComponent<Image>(background);
            image.sprite = LoadBackdropSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            image.raycastTarget = true;

            return cover;
        }

        private static Sprite LoadBackdropSprite()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(BackdropTexturePath))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
