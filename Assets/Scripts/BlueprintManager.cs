using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BlueprintManager : MonoBehaviour
{
    public static BlueprintManager Instance { get; private set; }
    public Camera Camera;
    public Vector2Int GridSize = new(5, 5);
    // the percentage of the screen that the grid should take up in the x and y directions
    public Vector2 DesiredGridSizeOnScreen = new(0.7f, 0.7f);
    public float ZCoord = -20;


    private GameObject _tilePrefab;
    private GameObject[] _grid;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }

        _tilePrefab = Resources.Load<GameObject>("Prefabs/BlueprintTile");

        CreateGrid();
    }

    public void ClearGrid()
    {
        foreach (var tile in _grid)
        {
            DestroyImmediate(tile);
        }
    }

    private readonly List<GameObject> _debugSpheres = new();

    void DrawDebugSphere(Vector3 position)
    {
        var debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.position = position;
        debugSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        debugSphere.layer = 3;
        _debugSpheres.Add(debugSphere);
    }

    void ClearDebugSpheres()
    {
        foreach (var sphere in _debugSpheres)
        {
            DestroyImmediate(sphere);
        }
        _debugSpheres.Clear();
    }

    public void CreateGrid()
    {
        if (_tilePrefab == null)
        {
            _tilePrefab = Resources.Load<GameObject>("Prefabs/BlueprintTile");
        }

        var bounds = _tilePrefab.GetComponent<Renderer>().bounds;
        
        float distanceToGrid = Mathf.Abs(ZCoord - Camera.transform.position.z);
        
        var frustumCorners = new Vector3[4];
        Camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), distanceToGrid, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        
        for (int i = 0; i < 4; i++)
        {
            frustumCorners[i] = Camera.transform.TransformPoint(frustumCorners[i]);
            frustumCorners[i].z = ZCoord;
        }

        Vector3 bottomLeft = Vector3.Lerp(
            Vector3.Lerp(frustumCorners[0], frustumCorners[3], 0.5f - DesiredGridSizeOnScreen.x / 2),
            Vector3.Lerp(frustumCorners[1], frustumCorners[2], 0.5f - DesiredGridSizeOnScreen.x / 2),
            0.5f - DesiredGridSizeOnScreen.y / 2
        );

        Vector3 topRight = Vector3.Lerp(
            Vector3.Lerp(frustumCorners[0], frustumCorners[3], 0.5f + DesiredGridSizeOnScreen.x / 2),
            Vector3.Lerp(frustumCorners[1], frustumCorners[2], 0.5f + DesiredGridSizeOnScreen.x / 2),
            0.5f + DesiredGridSizeOnScreen.y / 2
        );

        _grid = new GameObject[GridSize.x * GridSize.y];
        var rawTileSize = new Vector2((topRight.x - bottomLeft.x) / GridSize.x, (topRight.y - bottomLeft.y) / GridSize.y);
        float squareTileSize = Mathf.Min(rawTileSize.x, rawTileSize.y);
        var tileSize = new Vector2(squareTileSize, squareTileSize);
        
        var totalGridWidth = tileSize.x * GridSize.x;
        var totalGridHeight = tileSize.y * GridSize.y;
        var centerPoint = (bottomLeft + topRight) * 0.5f;
        bottomLeft = new Vector3(
            centerPoint.x - totalGridWidth * 0.5f,
            centerPoint.y - totalGridHeight * 0.5f,
            bottomLeft.z
        );

        var tileOffset = new Vector2(tileSize.x / 2, tileSize.y / 2);

        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                var tile = Instantiate(
                    _tilePrefab,
                    new Vector3(bottomLeft.x + i * tileSize.x + tileOffset.x, bottomLeft.y + j * tileSize.y + tileOffset.y, bottomLeft.z),
                    Quaternion.Euler(90, 180, 0)
                );
                tile.transform.parent = transform;
                tile.transform.localScale = new Vector3(tileSize.x / bounds.size.x, 1, tileSize.y / bounds.size.y);
                tile.name = $"Tile ({i}, {j})";
                var bpTile = tile.GetComponent<BlueprintTile>();
                bpTile.SetTexture(Resources.Load<Texture>($"Textures/dice_face_{Random.Range(1, 7)}"));
                _grid[i * GridSize.y + j] = tile;
            }
        }
    }

    public void RecreateGrid()
    {
        ClearGrid();
        CreateGrid();
    }
}

[CustomEditor(typeof(BlueprintManager))]
public class BlueprintManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var blueprint = (BlueprintManager)target;

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        // if (EditorGUI.EndChangeCheck() || GUILayout.Button("Recreate Grid"))
        if (GUILayout.Button("Recreate Grid"))
        {
            Debug.Log("Recreating grid");
            blueprint.RecreateGrid();
        }
    }
}
