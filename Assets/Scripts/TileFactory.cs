using UnityEngine;

public interface ITileFactory
{
    (int Value, DieFaceType Type, Texture texture) GetTileOptions();

    private static GameObject _tilePrefab = null;
    public GameObject CreateTile(Vector3 position, Quaternion rotation)
    {
        var (value, diceFaceType, texture) = GetTileOptions();

        if (_tilePrefab == null)
        {
            _tilePrefab = Resources.Load<GameObject>("Prefabs/BlueprintTile");
        }

        GameObject tile = Object.Instantiate(_tilePrefab, position, rotation);
        var bpTile = tile.GetComponent<BlueprintTile>();
        bpTile.Type = diceFaceType;
        bpTile.Value = value;
        bpTile.SetTexture(texture);

        return tile;
    }
}
