using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 🏁 Triggers a win condition and loads a new scene when the player enters this zone.
/// </summary>
[AddComponentMenu("Game/Triggers/Win Trigger")]
[RequireComponent(typeof(Collider))]
public class WinTrigger : MonoBehaviour
{
    #region Serialized Fields

    [Header("Trigger Settings")]
    [Tooltip("Tag required to activate the trigger (default: Player).")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Scene Settings")]
    [Tooltip("Scene to load when the player wins (use name from Build Settings).")]
    [SerializeField] private string sceneToLoad = "";

#if UNITY_EDITOR
    [Tooltip("Scene asset to load (automatically sets scene name).")]
    [SerializeField] private SceneAsset sceneAsset;
#endif

    #endregion

    #region Unity Events

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"🏁 WinTrigger: Loading scene '{sceneToLoad}'");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("WinTrigger: No scene assigned to load!");
        }
    }

    #endregion

    #region Editor Sync

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneToLoad = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
#endif

    #endregion
}