using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [Header("Key Mappings")]
    public bool teleportSceneOne = false;
    public string startScreenSceneName = "StartScreen"; // Name of your start/menu scene

    public bool teleportSceneTwo = false;
    public string gameSceneName = "GameScene"; // Name of your main game scene

    void Update()
    {
        // Go to Start Screen
        if (teleportSceneOne)
        {
            SceneManager.LoadScene(startScreenSceneName);
        }

        // Go to Game Scene
        if (teleportSceneTwo)
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
