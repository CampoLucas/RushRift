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
    public static readonly ISubject<int> OnWinGivePoints = new Subject<int>();
    public static readonly ISubject OnWinSaveTimes = new Subject();
    
    [Header("Points")]
    [SerializeField] private int points;

    [Header("Trigger Settings")]
    [Tooltip("Tag required to activate the trigger (default: Player).")]
    [SerializeField] private string triggerTag = "Player";
    
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;
        OnWinSaveTimes.NotifyAll();
        OnWinGivePoints.NotifyAll(points);

        if (LevelManager.TryGetLevelWon(out var levelWonSubject))
        {
            levelWonSubject.NotifyAll();
        }
    }

    private void OnDestroy()
    {
        OnWinGivePoints.DetachAll();
        OnWinSaveTimes.DetachAll();
    }
}