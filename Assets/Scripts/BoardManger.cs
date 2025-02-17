using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManger : MonoBehaviour
{
    private Tilemap m_Tilemap;

    public int Width;
    public int Height;
    public Tile[] GroundTiles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Tilemap = GetComponentInChildren<Tilemap>();
        for(int y=0;y<Height;++y){
            for(int x=0;x<Width;++x){
                int tileNumber = Random.Range(0, GroundTiles.Length);
                m_Tilemap.SetTile(new Vector3Int(x, y, 0), GroundTiles[tileNumber]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
