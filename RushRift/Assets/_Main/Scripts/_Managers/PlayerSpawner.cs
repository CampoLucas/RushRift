using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Entities;
using MyTools.Global;
using UnityEngine;

public class PlayerSpawner : SingletonBehaviour<PlayerSpawner>
{
    public static NullCheck<PlayerController> Player => _instance.TryGet(out var spawner) ? spawner._player : default;
    
    [Header("Reference")]
    [SerializeField] private PlayerController instantiatedRef;
    [SerializeField] private PlayerController prefab;

    [Header("Debug")]
    [SerializeField] private Transform respawnPos;

    private NullCheck<PlayerController> _player;
    protected override void OnAwake()
    {
        base.OnAwake();

        if (instantiatedRef)
        {
            _player = instantiatedRef;
        }
        else if (!_player.Set(FindObjectOfType<PlayerController>()))
        {
            _player = Instantiate(prefab);
        }
    }

    private void Update()
    {
        if (respawnPos && Input.GetKeyDown(KeyCode.R))
        {
            Respawn(respawnPos);
        }
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        if (_player.TryGet(out var player))
        {
            player.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            this.Log("Trying to respawn the player when it doesn't exist.", LogType.Error);
        }
    }

    public void Respawn(Transform tr) => Respawn(tr.position, tr.rotation);

    public bool Respawn(NullCheck<Transform> trCheck)
    {
        if (!trCheck.TryGet(out var tr))
        {
            return false;
        }

        Respawn(tr);
        return true;
    }

    protected override bool CreateIfNull() => false;
    protected override bool DontDestroy() => false;
}
