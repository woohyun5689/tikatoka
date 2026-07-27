using Tikatooka;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikatooka.Editor
{
    public static class ProjectSetup
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tikatooka/Create Main Scene")]
        public static void CreateMainScene()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Create2DCamera();

            var controllerObject = new GameObject("Dice Board Game");
            controllerObject.AddComponent<DiceBoardGameController>();

            EditorSceneManager.SaveScene(scene, MainScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            PlayerSettings.companyName = "Tikatooka";
            PlayerSettings.productName = "Tikatooka Dice Board";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;

            AssetDatabase.SaveAssets();
        }

        private static void Create2DCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(236, 238, 234, 255);

            cameraObject.AddComponent<AudioListener>();
        }
    }
}
