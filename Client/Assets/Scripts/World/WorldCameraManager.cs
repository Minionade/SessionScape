using UnityEngine;
using UnityEngine.InputSystem;

namespace SessionScape.Client.Assets.Scripts.World
{
    public class WorldCameraManager : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private float yMinAngle = 3f;
        [SerializeField] private float yMaxAngle = 90f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float scrollStepAmount = 3f;

        Transform target;
        float distance;
        float x, y;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            Networking.ServerConnection.onPlayerConnected += ServerConnection_onPlayerConnected;
        }

        private void Update()
        {
            distance -= Mouse.current.scroll.y.value * scrollStepAmount * 100f * Time.deltaTime;
            distance = Mathf.Clamp(distance, minZoom, maxZoom);
        }

        private void LateUpdate()
        {
            if (!target)
                return;

            if (Mouse.current.middleButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.value;
                x += delta.x * sensitivity * 100f * Time.deltaTime;
                y -= delta.y * sensitivity * 100f * Time.deltaTime;
                y = Mathf.Clamp(y, yMinAngle, yMaxAngle);
            }

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 newDistance = new(0, 0, -distance);
            Vector3 position = rotation * newDistance + target.position;

            transform.SetPositionAndRotation(position, rotation);
        }

        private void ServerConnection_onPlayerConnected(Transform transform)
        {
            target = transform;
        }
    }
}