using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Game;
using Game.DesignPatterns.Observers;
using Game.Entities;
using Game.General;
using Game.Levels;
using Game.Saves;
using Game.Utils;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSpawner : SingletonBehaviour<PlayerSpawner>
{
    public static NullCheck<PlayerController> Player => _instance.TryGet(out var spawner) ? spawner._player : default;

    #region Spawner Events

    /// <summary>
    /// Called when the player respawns.
    /// Position, position difference and rotation
    /// </summary>
    public static readonly Subject<Vector3, Vector3, Quaternion> PlayerSpawned = new();
    /// <summary>
    /// For when the player reference in the spawner is set.
    /// </summary>
    public static readonly Subject<PlayerController> PlayerSet = new();
    /// <summary>
    /// For when it doesn't have a already made reference or finds a player in the scene.
    /// </summary>
    public static readonly Subject<PlayerController> PlayerCreated = new();
    /// <summary>
    /// For when the player it didn't had a player reference before hand and finds one in the scene.
    /// </summary>
    public static readonly Subject<PlayerController> PlayerFound = new();

    #endregion
    
    [Header("Reference")]
    [SerializeField] private PlayerController instantiatedRef;
    [SerializeField] private PlayerController prefab;

    [Header("Respawn")]
    [SerializeField] private Transform spawn;
    

    //private NullCheck<Transform> _camera;
    private NullCheck<PlayerController> _player;
    private NullCheck<Rigidbody> _body;
    private List<EffectInstance> _playerEffects = new();
    private ActionObserver<BaseLevelSO> _onLevelReady;
    private NullCheck<BaseLevelSO> _prevLevel;
    private MedalSaveData _prevMedals;
    
    protected override void OnAwake()
    {
        base.OnAwake();
        
        if (!_player.TryGet(out var player, SetPlayer)) return;
        
        PlayerSet.NotifyAll(player);
        
        if (player.TryGetComponent<Rigidbody>(out var body)) _body = body;
        _onLevelReady = new ActionObserver<BaseLevelSO>(OnLevelReadyHandler);
        GameEntry.LoadingState.AttachOnReady(_onLevelReady);
    }

    public bool SetUpgrade(BaseLevelSO levelSo, string medal)
    {
        Effect upgrade;
        
        switch (medal)
        {
            case "bronze":
                if (_prevMedals.bronzeUnlocked)
                {
                    return false;
                }
                upgrade = levelSo.GetMedal(MedalType.Bronze).upgrade;
                _prevMedals.bronzeUnlocked = true;
                break;
            case "silver":
                if (_prevMedals.silverUnlocked)
                {
                    return false;
                }
                upgrade = levelSo.GetMedal(MedalType.Silver).upgrade;
                _prevMedals.silverUnlocked = true;
                break;
            case "gold":
                if (_prevMedals.goldUnlocked)
                {
                    return false;
                }
                upgrade = levelSo.GetMedal(MedalType.Gold).upgrade;
                _prevMedals.goldUnlocked = true;
                break;
            default:
                return false;
        }

        if (upgrade == null || !_player.TryGet(out var player)) return false;
        upgrade.ApplyEffect(player, remove: new []{ OnLevelChanged.Trigger(player, new OnLevelChanged.IsLoadingPredicate()) });

        return true;

    }

    public bool SetUpgrade(BaseLevelSO levelSo, int medal)
    {
        Effect upgrade;
        
        switch (medal)
        {
            case 1:
                if (_prevMedals.bronzeUnlocked)
                {
                    return false;
                }
                upgrade = levelSo.GetMedal(MedalType.Bronze).upgrade;
                _prevMedals.bronzeUnlocked = true;
                break;
            case 2:
                if (_prevMedals.silverUnlocked)
                {
                    return false;
                }
                upgrade = levelSo.GetMedal(MedalType.Silver).upgrade;
                _prevMedals.silverUnlocked = true;
                break;
            case 3:
                if (_prevMedals.goldUnlocked)
                {
                    return false;
                }
                upgrade = levelSo.GetMedal(MedalType.Gold).upgrade;
                _prevMedals.goldUnlocked = true;
                break;
            default:
                return false;
        }

        if (upgrade == null || !_player.TryGet(out var player)) return false;
        upgrade.ApplyEffect(player, remove: new []{ OnLevelChanged.Trigger(player, new OnLevelChanged.IsLoadingPredicate()) });

        return true;

    }

    private void OnLevelReadyHandler(BaseLevelSO levelSo)
    {
        if (!_player.TryGet(out var player))
        {
            this.Log("[OnLevelReadyHandler] Doesn't have the player's reference", LogType.Error);
            return;
        }

        var levelId = levelSo.LevelID;
        // Remove previous upgrades if it is a different level
        var isTheSameLevel = _prevLevel.TryGet(out var prev) && prev.LevelID == levelId;
        var data = SaveSystem.LoadGame();
        var medals = data.GetMedalSaveData(levelId);
        var hasNewUpgrades = _prevMedals != medals;
        
        // if ((isTheSameLevel)) this.Log("Is the same level", LogType.Error);
        // if ((isTheSameLevel && hasNewUpgrades)) this.Log("Is the same level, but has new upgrades", LogType.Error);
        //
        if ((isTheSameLevel && hasNewUpgrades) || !isTheSameLevel)
        {
            if (_playerEffects.Count > 0)
            {
                for (var i = 0; i < _playerEffects.Count; i++)
                {
                    _playerEffects[i].Remove();
                }
            
                _playerEffects.Clear();
            }
            
            //var effectsAmount = data.TryGetUnlockedEffects(levelId, out var effects);
            var effectsAmount = levelSo.TryGetEffects(out var effects);
        
            for (var i = 0; i < effectsAmount; i++)
            {
                _playerEffects.Add(effects[i].ApplyEffect(player));
            }
            
            
        }

        if (!_prevLevel.TryGet(out prev) || prev.LevelID != levelId)
        {
            
        }

        _prevMedals = medals;
        _prevLevel = levelSo;
    }

    private PlayerController SetPlayer()
    {
        return instantiatedRef ? instantiatedRef : FindPlayer();
    }

    private PlayerController FindPlayer()
    {
        var existing = FindObjectOfType<PlayerController>();
        if (existing)
        {
            PlayerFound.NotifyAll(existing);
            return existing;
        }

        return CreatePlayer();
    }
    
    private PlayerController CreatePlayer()
    {
        if (!prefab)
        {
            this.Log("Doesn't have a prefab for the player", LogType.Error);
            return null;
        }
        
        var player = Instantiate(prefab, transform);
        PlayerCreated.NotifyAll(player);
        return player;
    }

    public async UniTask Respawn(Vector3 position, Quaternion rotation, CancellationToken ct)
    {
        if (!_player.TryGet(out var player))
        {
            this.Log("Trying to respawn the player when it doesn't exist.", LogType.Error);
            return;
        }
        
        
        
        var tr = player.transform;
        if (_body.TryGet(out var body))
            await RigidbodyRespawn(position, rotation, body, tr, ct);
        else
            await TransformRespawnAsync(position, rotation, tr, ct);
        
        // Wait for transform sync after physics update
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);
            
        var diff = tr.position - position;
        PlayerSpawned.NotifyAll(position, diff, rotation);
    }

    private async UniTask RigidbodyRespawn(Vector3 position, Quaternion rotation, Rigidbody body, Transform tr, CancellationToken token)
    {
        // save the previous kinematic state
        var prevKinematic = body.isKinematic;

        // Set velocity to 0
        if (body.isKinematic)
        {
            body.isKinematic = false;
            body.velocity = Vector3.zero;
        }
        
        // Set back as kinematic to reposition.
        body.isKinematic = true;
        
        // Move the player
#if false
        tr.position = position;
        tr.rotation = rotation;
#else
        tr.SetPositionAndRotation(position, rotation);
#endif
        await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate, token);
        
        // Set velocity to 0
        body.isKinematic = false;
        body.velocity = Vector3.zero;
                
        // Set the kinematic to starting state
        body.isKinematic = prevKinematic;
    }

    private async UniTask TransformRespawnAsync(Vector3 position, Quaternion rotation, Transform tr, CancellationToken token)
    {
        tr.SetPositionAndRotation(position, rotation);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
    }

    public async UniTask Respawn(Transform tr, CancellationToken ct) => await Respawn(tr.position, tr.rotation, ct);

    public async UniTask<bool> Respawn(NullCheck<Transform> trCheck)
    {
        if (!trCheck.TryGet(out var tr))
        {
            return false;
        }

        await Respawn(tr);
        return true;
    }

    public static async UniTask RespawnPlayerAsync(CancellationToken ct)
    {
        var playerSpawner = await GetAsync(ct);
        await UniTask.WaitUntil(() => _instance.Get().spawn != null, cancellationToken: ct);

        if (playerSpawner.TryGet(out var spawner) && spawner._player.TryGet(out var player))
        {
            await spawner.Respawn(spawner.spawn, ct);
        }
    }

    protected override bool CreateIfNull() => false;
    protected override bool DontDestroy() => false;

    public void SetSpawn(Transform tr)
    {
        spawn = tr;
    }

    protected override void OnDisposeNotInstance()
    {
        base.OnDisposeNotInstance();
        
        instantiatedRef = null;
        prefab = null;
        spawn = null;
        //_player = null;
        _body = null;
    }

    protected override void OnDisposeInstance()
    {
        Debug.LogError("Dispose player spawner");
        base.OnDisposeInstance();
        GameEntry.LoadingState.DetachOnReady(_onLevelReady);
        
        PlayerSpawned.DetachAll();
        PlayerSet.DetachAll();
        PlayerCreated.DetachAll();
        PlayerFound.DetachAll();
        
        _player.Reset();
    }
}
