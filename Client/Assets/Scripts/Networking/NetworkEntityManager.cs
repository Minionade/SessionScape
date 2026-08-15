using SessionScape.Main.Protocol.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class NetworkEntityManager
{
    public event Action<Transform> OnPlayerConnected;

    public string PlayerId { get; set; }

    private readonly Dictionary<string, NetworkEntity> _networkEntities = new();
    private NetworkEntity _playerEntity;

    public NetworkEntity PlayerEntity => _playerEntity;

    public readonly ConcurrentQueue<Action> ActionQueue = new();

    public void ProcessQueuedActions()
    {
        while (ActionQueue.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

    public void ApplyEntityUpdate(EntitySnapshot snapshot)
    {
        Vector3 position = new Vector3(snapshot.X + 0.5f, snapshot.Y, snapshot.Z + 0.5f);

        if (snapshot.EntityId == PlayerId)
        {
            if (_playerEntity == null)
            {
                _playerEntity = SpawnNetworkEntity(snapshot.EntityType, snapshot.Name);
                OnPlayerConnected?.Invoke(_playerEntity.transform);
            }

            _playerEntity.SetNetworkPosition(position);
            return;
        }

        if (!_networkEntities.TryGetValue(snapshot.EntityId, out var networkEntity))
        {
            networkEntity = SpawnNetworkEntity(snapshot.EntityType, snapshot.Name);
            _networkEntities.Add(snapshot.EntityId, networkEntity);
        }

        networkEntity.SetNetworkPosition(position);
    }

    public void ApplyEntityMovementUpdate(string entityId, Waypoint[] path)
    {
        if (entityId == PlayerId)
        {
            _playerEntity?.SetPath(path);
        }
        else if (_networkEntities.TryGetValue(entityId, out var networkEntity))
        {
            networkEntity.SetPath(path);
        }
    }

    public void ApplySprintUpdate(string entityId, bool isSprinting)
    {
        if (entityId == PlayerId)
        {
            _playerEntity?.SetSprinting(isSprinting);
        }
        else if (_networkEntities.TryGetValue(entityId, out var networkEntity))
        {
            networkEntity.SetSprinting(isSprinting);
        }
    }

    public void RemoveEntity(string entityId)
    {
        if (!_networkEntities.TryGetValue(entityId, out var networkEntity))
        {
            GameObject.Destroy(networkEntity.gameObject);
            _networkEntities.Remove(entityId);
        }
    }

    public NetworkEntity SpawnNetworkEntity(string entityType, string name)
    {
        var gameObject = new GameObject($"{entityType}_{name}");
        var visualObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visualObject.transform.parent = gameObject.transform;
        Vector3 position = visualObject.transform.position;
        position.y += 1;
        visualObject.transform.position = position;

        return gameObject.AddComponent<NetworkEntity>();
    }
}