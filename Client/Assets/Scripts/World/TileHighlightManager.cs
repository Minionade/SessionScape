using SessionScape.Main.World;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SessionScape.Client.Assets.Scripts.World
{
    public class TileHighlightManager : MonoBehaviour
    {
        [SerializeField] private Color mouseHighlightColor;
        [SerializeField] private Color pathHighlightColor;

        [SerializeField] private MapLoader _mapLoader;

        private List<TileHighlight> pathHighlights = new();
        private TileHighlight mouseHighlight;

        private void Update()
        {
            if (mouseHighlight == null)
            {
                mouseHighlight = DrawHighlight(Vector3.zero, mouseHighlightColor);
            }

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.value);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                mouseHighlight.SetTile(hit.point);
                mouseHighlight.Enable();
            }
            else
            {
                mouseHighlight.Disable();
            }
        }

        private TileHighlight DrawHighlight(Vector3 position, Color color = default, Transform destroyOnEntry = null)
        {
            if (!_mapLoader.TryGetTile(position, out var tileCoords, out TileData tile))
                return null;

            GameObject highlightObject = new GameObject("Tile Highlight");
            highlightObject.transform.parent = transform;
            TileHighlight newHighlight = highlightObject.AddComponent<TileHighlight>();
            newHighlight.Initialize(_mapLoader);
            newHighlight.SetColor(color);
            newHighlight.SetDestroyTarget(destroyOnEntry);
            newHighlight.Enable();
            return newHighlight;
        }
    }
}