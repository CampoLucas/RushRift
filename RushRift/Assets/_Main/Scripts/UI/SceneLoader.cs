using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("Utilities/Scene Loader")]
public class SceneLoader : MonoBehaviour
{
    #region Inspector Variables

    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load. Make sure it's added to the Build Settings.")]
    public string sceneNameToLoad;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the scene with the specified name.
    /// </summary>
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneNameToLoad))
        {
            SceneManager.LoadScene(sceneNameToLoad);
        }
        else
        {
            Debug.LogWarning("Scene name is empty. Please enter a valid scene name.");
        }
    }

    /// <summary>
    /// Reloads the current scene.
    /// </summary>
    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Loads the next scene in the Build Settings.
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("Next scene index is out of bounds.");
        }
    }

    /// <summary>
    /// Loads the previous scene in the Build Settings.
    /// </summary>
    public void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int previousIndex = currentIndex - 1;

        if (previousIndex >= 0)
        {
            SceneManager.LoadScene(previousIndex);
        }
        else
        {
            Debug.LogWarning("Previous scene index is out of bounds.");
        }
    }

    #endregion
}