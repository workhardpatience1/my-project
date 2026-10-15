using UnityEditor;
using UnityEditor.SceneManagement;

namespace ObstacleDodge.EditorTools
{
    /// <summary>
    /// Opens SampleScene and presses Play. Used by OpenInUnity.command:
    ///   Unity -projectPath ObstacleDodge.Unity -executeMethod ObstacleDodge.EditorTools.OpenAndPlay.Run
    /// Also in the menu: Obstacle Dodge → Play.
    /// </summary>
    public static class OpenAndPlay
    {
        [MenuItem("Obstacle Dodge/Play")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
        }
    }
}
