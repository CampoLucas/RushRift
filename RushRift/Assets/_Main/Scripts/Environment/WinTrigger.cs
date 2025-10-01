using UnityEngine;
using UnityEngine.SceneManagement;
using Game;
using Game.DesignPatterns.Observers;

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
    [Header("Trigger Settings")]
    [SerializeField] private string triggerTag = "Player";
    
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;
        if (LevelManager.TryGetLevelWon(out var levelWonSubject))
        {
            levelWonSubject.NotifyAll();
        }
    }
    
}