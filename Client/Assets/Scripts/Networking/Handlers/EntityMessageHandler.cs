using SessionScape.Client.Assets.Scripts.Networking;
using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using System;
using System.Collections.Concurrent;

public class EntityUpdateHandler : ClientMessageHandler<EntityUpdate>
{
    private readonly NetworkEntityManager _entities;
    public override MessageType Type => MessageType.EntityUpdate;

    public EntityUpdateHandler(NetworkEntityManager entities)
    {
        _entities = entities;
    }

    protected override void HandleTyped(EntityUpdate data)
    {
        _entities.ActionQueue.Enqueue(() => _entities.ApplyEntityUpdate(data.Entity));
    }
}

public class EntitySnapshotListHandler : ClientMessageHandler<EntitySnapshotList>
{
    private readonly NetworkEntityManager _entities;

    public override MessageType Type => MessageType.EntitySnapshotList;

    public EntitySnapshotListHandler(NetworkEntityManager entities)
    {
        _entities = entities;
    }

    protected override void HandleTyped(EntitySnapshotList data)
    {
        foreach (var snapshot in data.Entities)
        {
            var snapshotCopy = snapshot;
            _entities.ActionQueue.Enqueue(() => _entities.ApplyEntityUpdate(snapshotCopy));
        }
    }
}

public class EntityMovementUpdateHandler : ClientMessageHandler<EntityMovementUpdate>
{
    private readonly NetworkEntityManager _entities;

    public override MessageType Type => MessageType.EntityMovementUpdate;
    
    public EntityMovementUpdateHandler(NetworkEntityManager entities)
    {
        _entities = entities;
    }

    protected override void HandleTyped(EntityMovementUpdate data)
    {
        _entities.ActionQueue.Enqueue(() => _entities.ApplyEntityMovementUpdate(data.EntityId, data.Path));
    }
}

public class EntityRemovedHandler : ClientMessageHandler<EntityRemoved>
{
    private readonly NetworkEntityManager _entities;

    public override MessageType Type => MessageType.EntityRemoved;

    public EntityRemovedHandler(NetworkEntityManager entities)
    {
        _entities = entities;
    }

    protected override void HandleTyped(EntityRemoved data)
    {
        _entities.ActionQueue.Enqueue(() => _entities.RemoveEntity(data.EntityId));
    }
}

public class RunUpdateHandler : ClientMessageHandler<RunUpdate>
{
    private readonly NetworkEntityManager _entities;

    public override MessageType Type => MessageType.RunUpdate;

    public RunUpdateHandler(NetworkEntityManager entities)
    {
        _entities = entities;
    }

    protected override void HandleTyped(RunUpdate data)
    {
        _entities.ActionQueue.Enqueue(() => _entities.ApplySprintUpdate(data.EntityId, data.IsSprinting));
    }
}

public class RunEnergyUpdateHandler : ClientMessageHandler<RunEnergyUpdate>
{
    private readonly ConcurrentQueue<double> _energyUpdateQueue;

    public override MessageType Type => MessageType.RunEnergyUpdate;

    public RunEnergyUpdateHandler(ConcurrentQueue<double> energyUpdateQueue)
    {
        _energyUpdateQueue = energyUpdateQueue;
    }

    protected override void HandleTyped(RunEnergyUpdate data)
    {
        _energyUpdateQueue.Enqueue(data.RunEnergy);
    }
}