using UnityEngine;
using Game.Entities;

[DisallowMultipleComponent]
public class UpgradeManager : MonoBehaviour
{
    // [Header("Player Binding")]
    // [SerializeField] private EntityController playerController;
    // [SerializeField] private bool autoFindPlayerByTag = true;
    //
    //
    // private static UpgradeManager _instance;
    

    // private void Awake()
    // {
    //     _instance = this;
    //     if (!playerController && autoFindPlayerByTag)
    //     {
    //         var go = GameObject.FindGameObjectWithTag("Player");
    //         if (go) playerController = go.GetComponentInParent<EntityController>();
    //     }
    // }

//     private void Start()
//     {
//         var data = SaveAndLoad.Load();
//         var levelID = Game.LevelManager.GetLevelID();
//
//         if (!playerController)
//         {
// #if UNITY_EDITOR
//             Debug.LogError("ERROR: The UpgradeManager doesn't have the reference of the player. Returning.");
// #endif
//             
//             return;
//         }
//         
//         var effectsAmount = data.TryGetUnlockedEffects(levelID, out var effects);
//
//         for (var i = 0; i < effectsAmount; i++)
//         {
//             effects[i].ApplyEffect(playerController);
//         }
//     }
}