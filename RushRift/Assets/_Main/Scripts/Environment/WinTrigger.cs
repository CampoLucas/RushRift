using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 🏁 Triggers a win condition when the player reaches this zone.
/// </summary>
[AddComponentMenu("Game/Triggers/Win Trigger")]
[RequireComponent(typeof(Collider))]
public class WinTrigger : MonoBehaviour
{
    #region Serialized Fields

    [Header("Win Settings")]
    [Tooltip("Optional: require a specific tag to trigger win (default is 'Player')")]
    [SerializeField] private string triggerTag = "Player";

    #endregion

    #region Unity Methods

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;

        SceneManager.LoadScene("Level_1_Rework");
    }

    #endregion
}