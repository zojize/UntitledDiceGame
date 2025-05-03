using UnityEngine;
using UnityEngine.Tilemaps;

public class TileHighlighter : MonoBehaviour
{
    public Tilemap tilemap;  // Reference to the Tilemap component
    private Vector3Int previousCellPos;  // Store the last highlighted tile position

    void Update()
    {
        // Get the mouse position in world coordinates
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);

        // Only update if the mouse is over a new tile
        if (cellPos != previousCellPos)
        {
            // Reset the color of the previously highlighted tile
            tilemap.SetColor(previousCellPos, Color.black);

            // Set the new tile's color to highlight it
            tilemap.SetColor(cellPos, Color.yellow);

            // Update the previous cell position
            previousCellPos = cellPos;
        }
    }
}
