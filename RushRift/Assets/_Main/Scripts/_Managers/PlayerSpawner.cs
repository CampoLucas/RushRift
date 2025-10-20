using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game;
using Game.Entities;
using MyTools.Global;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSpawner : SingletonBehaviour<PlayerSpawner>
{
    public static NullCheck<PlayerController> Player => _instance.TryGet(out var spawner) ? spawner._player : default;
    
    [Header("Reference")]
    [SerializeField] private PlayerController instantiatedRef;
    [SerializeField] private PlayerController prefab;

    [Header("Respawn")]
    [SerializeField] private Transform spawn;
    [SerializeField] private float minDistance = 0.1f;

    private NullCheck<Transform> _camera;
    private NullCheck<PlayerController> _player;
    private NullCheck<Rigidbody> _body;
    protected override void OnAwake()
    {
        base.OnAwake();

        _camera = Camera.main.transform;
        
        if (instantiatedRef)
        {
            _player = instantiatedRef;
        }
        else if (!_player.Set(FindObjectOfType<PlayerController>()))
        {
            _player = Instantiate(prefab);
        }

        if (_player.TryGet(out var player) && player.TryGetComponent<Rigidbody>(out var body))
        {
            Debug.Log("[SuperTest] has rigidbody");
            _body = body;
        }
    }

    private void Update()
    {
        if (spawn && Input.GetKeyDown(KeyCode.R))
        {
            Respawn(spawn);
        }
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        Debug.Log("[SuperTest] Respawn player");
        
        if (_player.TryGet(out var player))
        {
            var tr = player.transform;
            
            if (_body.TryGet(out var body))
            {
                // save the previous kinematic state
                var prevKinematic = body.isKinematic;

                // Set velocity to 0
                body.isKinematic = false;
                body.velocity = Vector3.zero;
                
                // Set as kinematic
                body.isKinematic = true;
                
                // Move the player
                tr.position = position;
                tr.rotation = rotation;
                
                // Set velocity to 0
                body.isKinematic = false;
                body.velocity = Vector3.zero;
                
                // Set the kinematic to starting state
                body.isKinematic = prevKinematic;
            }
            else
            {
                tr.position = position;
                tr.rotation = rotation;
            }

            if (_camera.TryGet(out var camTr)) // Needs to have an offset, because the camera is not at the player's feet.
            {
                camTr.position = position;
                camTr.rotation = rotation;
            }
            //player.transform.SetPositionAndRotation(position, rotation);
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

    public static async UniTask RespawnPlayerAsync()
    {
        var playerSpawner = await GetAsync();
        await UniTask.WaitUntil(() => _instance.Get().spawn != null);

        if (playerSpawner.TryGet(out var spawner) && spawner._player.TryGet(out var player))
        {
            spawner.Respawn(spawner.spawn);

            await UniTask.WaitUntil(() =>
                Vector3.Distance(player.transform.position, spawner.spawn.position) <= spawner.minDistance);
        }
    }

    protected override bool CreateIfNull() => false;
    protected override bool DontDestroy() => false;

    public void SetSpawn(Transform tr)
    {
        spawn = tr;
    }
}
