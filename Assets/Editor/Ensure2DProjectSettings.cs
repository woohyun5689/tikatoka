using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikatooka.Editor
{
    [InitializeOnLoad]
    public static class Ensure2DProjectSettings
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string SessionKey = "Tikatooka.Ensure2DProjectSettings.Applied";

        static Ensure2DProjectSettings()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != MainScenePath || Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(236, 238, 234, 255);

            cameraObject.AddComponent<AudioListener>();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }
    }
}
