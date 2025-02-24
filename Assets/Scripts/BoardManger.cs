using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections;
using System;


public class BoardManger : MonoBehaviour
{
    [SerializeField] private Tilemap m_Tilemap = null;
    [SerializeField] private Tilemap highlight = null;
    [SerializeField] private Tile hoverTile = null;
    [SerializeField] private Tilemap boss = null;
    [SerializeField] private Tile bosstile = null;

    public int Width;
    public int Height;
    public int x, y;
    public Tile[] GroundTiles;
    private int[] pathX, pathY;
    private int num, cur = 0;
    [SerializeField] public Button yourButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private int[] dx = {1, 0, -1, 0};
    private int[] dy = {0, 1, 0, -1};
    private int[,] vst;

    void dfs(int x, int y){
        if(num == Width * Height) return;
        for(int i=0;i<4;i++){
            int nx = x + dx[i]; int ny = y + dy[i];
            if(nx < 0 || nx >= Width || ny < 0 || ny >= Height || vst[nx, ny] != 0) continue;
            pathX[++num] = nx; pathY[num] = ny; vst[nx, ny] = 1;
            Debug.Log(pathX[num] + " and " + pathY[num]);
            dfs(nx, ny);
        }
    }

    void Start(){
		Button btn = yourButton.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
        for(int y=0;y<Height;++y){
            for(int x=0;x<Width;++x){
                if((x < Width/2-1 || x >= Width / 2 + 1) || (y < Width/2-1 || y >= Width / 2 + 1)){
                    int tileNumber = UnityEngine.Random.Range(0, GroundTiles.Length);
                    m_Tilemap.SetTile(new Vector3Int(x, y, 0), GroundTiles[tileNumber]);
                }
            }
        }
        boss.SetTile(new Vector3Int(2, 2, 0), bosstile);
        highlight.SetTile(new Vector3Int(0, 0, 0), hoverTile);
        pathX = new int[100]; pathY = new int[100]; num = 2;
        pathX[1] = 0; pathY[1] = 0; vst = new int[100, 100]; vst[0, 0] = 1;
        dfs(0, 0);
        Debug.Log(num);
        // for(int i=0;i<Width;i++){
        //     num++;
        //     pathX[num] = i;
        //     pathY[num] = 0;
        // }
        // for(int i=1;i<Height;i++){
        //     num++;
        //     pathX[num] = Width-1;
        //     pathY[num] = i;
        // }
        // for(int i=Width-2;i>=1;i--){
        //     num++;
        //     pathX[num] = i;
        //     pathY[num] = Height-1;
        // }
        // for(int i=Height-2;i>=1;i--){
        //     num++;
        //     pathX[num] = 0;
        //     pathY[num] = i;
        // }

        // for(int i=1;i<Width-1;i++){
        //     num++;
        //     pathX[num] = i;
        //     pathY[num] = 1;
        // }
        // for(int i=2;i<Height-1;i++){
        //     num++;
        //     pathX[num] = Width-2;
        //     pathY[num] = i;
        // }
    }

    void TaskOnClick(){
        var rnd = new System.Random();
        int num = rnd.Next(3);
        UpdateHighlight(num + 1);
    }
    // Update is called once per frame
    void Update(){

    }

    void UpdateHighlight(int step){
        if(cur + step < num){
            highlight.SetTile(new Vector3Int(pathX[cur], pathY[cur], 0), null);
            cur = cur + step;
            highlight.SetTile(new Vector3Int(pathX[cur], pathY[cur], 0), hoverTile);
        }
    }

}
