using SessionScape.Main.Protocol.Messages;
using System.Collections.Generic;
using UnityEngine;

public class NetworkEntity : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1.67f;
    [SerializeField] private float sprintSpeed = 3.34f;

    private Vector3 networkPosition;
    private Vector3 visualPosition;

    private readonly Queue<Vector3> visualPath = new();

    private bool initialized;
    private bool isSprinting;

    public bool IsRunning => isSprinting;

    void Update()
    {
        if (visualPath.Count == 0)
            return;

        float currentSpeed = isSprinting
            ? sprintSpeed
            : movementSpeed;

        Vector3 target = visualPath.Peek();

        visualPosition = Vector3.MoveTowards(
            visualPosition,
            target,
            currentSpeed * Time.deltaTime);

        transform.position = visualPosition;

        if (visualPosition == target)
        {
            visualPath.Dequeue();
        }
    }

    public void SetPath(Waypoint[] path)
    {
        visualPath.Clear();

        foreach (Waypoint waypoint in path)
        {
            visualPath.Enqueue(new Vector3(
                    waypoint.X + 0.5f,
                    waypoint.Y,
                    waypoint.Z + 0.5f));
        }
    }

    public void SetNetworkPosition(Vector3 newPosition)
    {
        networkPosition = newPosition;

        if (!initialized)
        {
            visualPosition = newPosition;
            transform.position = newPosition;
            initialized = true;
        }
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }
}