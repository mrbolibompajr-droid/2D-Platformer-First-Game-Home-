using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [Header("Key Mappings")]
    public KeyCode goToStartScreenKey = KeyCode.Escape; // Press Escape to go to Start Screen
    public string startScreenSceneName = "StartScreen"; // Name of your start/menu scene

    public KeyCode goToGameSceneKey = KeyCode.Return; // Press Enter to go to Game Scene
    public string gameSceneName = "GameScene"; // Name of your main game scene

    void Update()
    {
        // Go to Start Screen
        if (Input.GetKeyDown(goToStartScreenKey))
        {
            SceneManager.LoadScene(startScreenSceneName);
        }

        // Go to Game Scene
        if (Input.GetKeyDown(goToGameSceneKey))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
