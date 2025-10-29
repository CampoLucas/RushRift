using Game;
using UnityEngine;

public class DeathZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GlobalEvents.GameOver.NotifyAll(false);
    }
}

