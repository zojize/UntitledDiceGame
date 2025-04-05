using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private GameObject _diePrefab;
    private GameObject[] _grid;
    private static readonly List<BlueprintTile> _selectedTiles = new();
    private static readonly Dictionary<BlueprintTile, Vector2Int> _tilePositions = new();
    private static UnityEngine.UI.Button _foldButton;
    private static Vector2 _tileSize;

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
        _diePrefab = Resources.Load<GameObject>("Prefabs/Die");

        GameObject button = GameObject.Find("FoldButton");
        _foldButton = button.GetComponent<UnityEngine.UI.Button>();

        _foldButton.onClick.AddListener(() =>
        {
            // var die = CreateDieFromSelection();
            // if (die == null)
            // {
            //     Debug.Log("Failed to create die");
            //     return;
            // }
            // else
            //     Debug.Log($"Die: {die}");
            var folds = TryFoldIntoDie();
            if (folds != null)
                StartCoroutine(FoldDie(folds));
        });

        CreateGrid();
    }

    public GameObject CreateDieFromSelection()
    {
        if (_selectedTiles.Count != 6)
        {
            Debug.Log("Not enough tiles selected to create a die");
            return null;
        }

        var die = Instantiate(_diePrefab);
        var dieComponent = die.GetComponent<Die>();
        var rigidbody = die.GetComponent<Rigidbody>();
        rigidbody.isKinematic = true;

        List<Side> allSides = new()
        {
            Side.Top,
            Side.Bottom,
            Side.Left,
            Side.Right,
            Side.Front,
            Side.Back
        };
        Dictionary<BlueprintTile, List<Side>> tileToSides = _selectedTiles.Select(t =>
        {
            return (t, allSides.ToList());
        }).ToDictionary(t => t.t, t => t.Item2);

        Dictionary<Side, List<Side>> validNeighbors = new()
        {
            { Side.Top, new List<Side> { Side.Left, Side.Right, Side.Back, Side.Front } },
            { Side.Bottom, new List<Side> { Side.Left, Side.Right, Side.Back, Side.Front } },
            { Side.Left, new List<Side> { Side.Top, Side.Bottom, Side.Back, Side.Front } },
            { Side.Right, new List<Side> { Side.Top, Side.Bottom, Side.Back, Side.Front } },
            { Side.Front, new List<Side> { Side.Top, Side.Bottom, Side.Left, Side.Right } },
            { Side.Back, new List<Side> { Side.Top, Side.Bottom, Side.Left, Side.Right } }
        };

        tileToSides[_selectedTiles[0]] = new List<Side>
        {
            Side.Top,
        };

        List<BlueprintTile> workList = new()
        {
            _selectedTiles[0]
        };

        List<Side> usedSides = new()
        {
            Side.Top
        };

        while (workList.Count > 0)
        {
            var tile = workList[0];
            workList.RemoveAt(0);
            var position = _tilePositions[tile];

            var neighbors = new List<Vector2Int>
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right,
            };

            var neighborTiles = neighbors
                .Select(n => position + n)
                .Select(n => _grid.ElementAtOrDefault(n.y * GridSize.x + n.x))
                .Where(t => t != null)
                .Select(t => t.GetComponent<BlueprintTile>())
                .Where(t => _selectedTiles.Contains(t))
                .ToList();

            foreach (var neighbor in neighborTiles)
            {
                var prevSidesCount = tileToSides[neighbor].Count;

                foreach (var side in tileToSides[tile])
                {
                    tileToSides[neighbor] = tileToSides[neighbor]
                        .Intersect(validNeighbors[side])
                        .ToList();
                }

                if (tileToSides[neighbor].Count == 1)
                {
                    var side = tileToSides[neighbor][0];
                    if (usedSides.Contains(side))
                    {
                        Debug.Log($"Side {side} already used for tile {tile.name}");
                        Destroy(die);
                        return null;
                    }

                    usedSides.Add(side);
                }
                else
                {
                    tileToSides[neighbor] = tileToSides[neighbor]
                        .Where(s => !usedSides.Contains(s))
                        .ToList();
                }

                Debug.Log($"Tile {tile.name}");
                Debug.Log($"Tile to sides: {string.Join(", \n", tileToSides.Select(t => $"{t.Key.name}: {string.Join(", ", t.Value)}"))}");
                if (tileToSides[neighbor].Count == 0)
                {
                    Debug.Log($"No valid sides for neighbor {neighbor.name}");

                    Destroy(die);
                    return null;
                }

                if (prevSidesCount == tileToSides[neighbor].Count)
                {
                    continue;
                }

                Debug.Log($"Adding neighbor {neighbor.name} to work list");
                workList.Add(neighbor);
            }
        }

        if (tileToSides.Values.Any(s => s.Count != 1))
        {
            Debug.Log("No valid sides for some tiles");
            Debug.Log($"Tile to sides: {string.Join(", \n", tileToSides.Select(t => $"{t.Key.name}: {string.Join(", ", t.Value)}"))}");
            Destroy(die);
            return null;
        }

        foreach (var tile in tileToSides)
        {
            var side = tile.Value[0];
            if (!dieComponent.TrySetFace(side, tile.Key))
            {
                Debug.Log($"Failed to set face {side} for tile {tile.Key.name}");
                Destroy(die);
                return null;
            }
        }

        return die;
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
        _tileSize = tileSize;

        var totalGridWidth = tileSize.x * GridSize.x;
        var totalGridHeight = tileSize.y * GridSize.y;
        var centerPoint = (bottomLeft + topRight) * 0.5f;
        bottomLeft = new Vector3(
            centerPoint.x - totalGridWidth * 0.5f,
            centerPoint.y - totalGridHeight * 0.5f,
            bottomLeft.z
        );

        var tileOffset = new Vector2(tileSize.x / 2, tileSize.y / 2);

        for (int x = 0; x < GridSize.y; x++)
        {
            for (int y = 0; y < GridSize.x; y++)
            {
                var tile = Instantiate(
                    _tilePrefab,
                    new Vector3(bottomLeft.x + x * tileSize.x + tileOffset.x, bottomLeft.y + y * tileSize.y + tileOffset.y, bottomLeft.z),
                    Quaternion.Euler(90, 180, 0)
                );

                tile.transform.parent = transform;
                tile.transform.localScale = new Vector3(tileSize.x / bounds.size.x, 1, tileSize.y / bounds.size.y);
                tile.name = $"Tile ({x}, {y})";

                var bpTile = tile.GetComponent<BlueprintTile>();
                bpTile.OnSelectionChangeEvent += OnTileSelectionChange;
                var randValue = Random.Range(1, 7);
                bpTile.Value = randValue;
                var randType = DieFaceType.Damage; // TODO: randomize this
                bpTile.Type = randType;
                bpTile.SetTexture(Resources.Load<Texture>($"Textures/dice_face_{randValue}"));
                _grid[y * GridSize.x + x] = tile;
                _tilePositions[bpTile] = new Vector2Int(x, y);
            }
        }
    }

    public void RecreateGrid()
    {
        ClearGrid();
        CreateGrid();
    }

    public void OnTileSelectionChange(BlueprintTile tile)
    {
        if (tile.IsSelected)
        {
            // if selected dice plus this one does not form a cube then deselect this one
            Debug.Log($"Selected tile: {_tilePositions[tile]}");
            _selectedTiles.Add(tile);
            // TryFoldIntoDie();


            // if (_currentDie == null)
            // {
            //     _currentDie = new DieLogic();
            //     _currentDie.SetFace(Side.Top, tile);
            //     SelectedTiles.Add(tile);
            //     return;
            // }

            // if (_currentDie.TrySetFace(tile))
            // {
            //     SelectedTiles.Add(tile);
            // }
            // else
            // {
            //     tile.PreventSelection();
            // }
        }
        else
        {
            _selectedTiles.Remove(tile);
        }
    }


    public Dictionary<BlueprintTile, (BlueprintTile, Vector2Int)> TryFoldIntoDie()
    {
        Dictionary<BlueprintTile, (BlueprintTile, Vector2Int)> folds = new();


        while (folds.Count < _selectedTiles.Count - 1)
        {
            var initialCount = folds.Count;

            foreach (var tile in _selectedTiles)
            {
                if (folds.ContainsKey(tile))
                {
                    continue;
                }

                var position = _tilePositions[tile];
                var neighbors = new List<Vector2Int>
                {
                    Vector2Int.up,
                    Vector2Int.down,
                    Vector2Int.left,
                    Vector2Int.right,
                };

                var neighborTiles = neighbors
                    .Select(n => position + n)
                    .Select(n => _grid.ElementAtOrDefault(n.y * GridSize.x + n.x))
                    .Where(t => t != null)
                    .Select(t => t.GetComponent<BlueprintTile>())
                    // .Where(t =>
                    // {
                    //     Debug.Log($"Checking {t.name}: {t.IsSelected} && {!folds.ContainsKey(t)}");
                    //     return t.IsSelected && !folds.ContainsKey(t);
                    // })
                    .Where(t => t.IsSelected && !folds.ContainsKey(t))
                    .ToList();

                // Debug.Log($"{position} N neighbors: {neighborTiles.Count}");

                if (neighborTiles.Count == 1)
                {
                    var neighborTile = neighborTiles[0];
                    folds[tile] = (
                        neighborTile,
                        _tilePositions[neighborTile] - position
                    // * new Vector2Int(-1, 1)
                    );
                }
            }

            if (folds.Count == initialCount)
            {
                Debug.Log("No more folds found");
                return null;
            }
        }

        Debug.Log($"Folds ({folds.Count}):");
        foreach (var fold in folds)
        {
            Debug.Log($"{fold.Key.name} -> {fold.Value}");
        }

        // var sum = folds.Values.Aggregate(Vector2Int.zero, (a, b) => a + b);
        // Debug.Log($"Sum: {sum}");

        return folds;
    }
    IEnumerator FoldDie(Dictionary<BlueprintTile, (BlueprintTile, Vector2Int)> folds)
    {
        Debug.Log("Folding die");
        float duration = 1.0f; // in seconds
        float elapsed = 0.0f;

        Dictionary<BlueprintTile, List<BlueprintTile>> parentToChildren = new();

        foreach (var (tile, (neighbor, _)) in folds)
        {
            if (!parentToChildren.ContainsKey(neighbor))
            {
                parentToChildren[neighbor] = new List<BlueprintTile>();
            }

            parentToChildren[neighbor].Add(tile);
        }

        yield return null;

        var initialState = new Dictionary<BlueprintTile, (Vector3 position, Quaternion rotation)>();
        foreach (var tile in _selectedTiles)
        {
            initialState[tile] = (tile.transform.position, tile.transform.rotation);
        }



        while (true)
        {
            foreach (var (tile, (neighbor, dir)) in folds)
            {
                var angle = Mathf.Lerp(0f, 90f, Time.deltaTime / duration);
                var position = dir switch
                {
                    Vector2Int v when v == Vector2Int.up => tile.Up.position,
                    Vector2Int v when v == Vector2Int.down => tile.Down.position,
                    Vector2Int v when v == Vector2Int.left => tile.Left.position,
                    Vector2Int v when v == Vector2Int.right => tile.Right.position,
                    _ => Vector3.zero
                };

                var forward = Vector3.Cross(tile.Right.position - tile.transform.position, tile.Up.position - tile.transform.position);
                var axis = Vector3.Cross(position - tile.transform.position, forward).normalized;
                tile.transform.RotateAround(position, axis, angle);
                PropagateTransformation(parentToChildren, tile, position, axis, angle);
            }


            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                break;
            }

            yield return null;
        }
    }

    private void PropagateTransformation(Dictionary<BlueprintTile, List<BlueprintTile>> parentToChildren, BlueprintTile tile, Vector3 position, Vector3 axis, float angle)
    {
        if (parentToChildren.ContainsKey(tile))
        {
            foreach (var child in parentToChildren[tile])
            {
                child.transform.RotateAround(position, axis, angle);
                PropagateTransformation(parentToChildren, child, position, axis, angle);
            }
        }
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
