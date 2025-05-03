using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;


public class BoardManger : MonoBehaviour
{
    [SerializeField] private Tilemap m_Tilemap = null;
    [SerializeField] private Tilemap highlight = null;
    [SerializeField] private Tile hoverTile = null;
    [SerializeField] private Tilemap boss = null;
    [SerializeField] private Tile bosstile = null;
    [SerializeField] private Tilemap bossHighlight = null;

    private Grid grid;
    public int Width;
    public int Height;
    public int numBlock;
    public int x, y;
    public int nwx, nwy;
    public Tile[] GroundTiles;
    private int[] pathX, pathY;
    private int num, cur = 0;
    [SerializeField] public UnityEngine.UI.Button yourButton;
    private int[] blockX, blockY;
    private HashSet<(int x, int y)> nxt_step;
    private int choose = 0;
    private int outer = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Vector3Int previousMousePos = new Vector3Int();

    private int[] dx = {1, 0, -1, 0};
    private int[] dy = {0, 1, 0, -1};
    private int[,] isBlock, vst;

    private Dictionary<(int x, int y), List<(int x, int y)>> graph = new Dictionary<(int x, int y), List<(int x, int y)>>();

    // void dfs(int x, int y, int dire){
    //     if(num == Width * Height) return;
    //     for(int i=0;i<4;i++){
    //         int nx = x + dx[i]; int ny = y + dy[i];
    //         if(nx < 0 || nx >= Width || ny < 0 || ny >= Height || vst[nx, ny] != 0) continue;
    //         pathX[++num] = nx; pathY[num] = ny; vst[nx, ny] = 1;
    //         Debug.Log(pathX[num] + " and " + pathY[num]);
    //         dfs(nx, ny);
    //     }
    //     int x = 0; int y = 0; int dire = 0;
    //     while( x < Width && x >= 0 && y < Height && y >= 0 && vst[x][y] == 0){
    //         num++; vst[x][y] = 1; pathX[num] = x; pathY[num] = y;
    //         int nx = x + dx[dire]; int ny = y + dy[dire];
    //         while(nx < 0 || nx >= Width || ny < 0 || ny >= Height || vst[nx, ny] != 0){
    //             dire = (dire + 1)%4;
    //             nx = x + dx[dire]; ny = y + dy[dire];
    //         }
    //     }
    // }

    // void traverseMap(){
    //     int total = Height * Width; 
    //     while(num < total){
    //         pathX[num] = 
    //     }
    // }

    HashSet<(int x, int y)> findVertex(int x, int y, int step){
        HashSet<(int x, int y)> cur = new HashSet<(int x, int y)>();
        HashSet<(int x, int y)> nxt = new HashSet<(int x, int y)>();
        (int x, int y) hi = (x, y);
        cur.Add(hi);
        for(int curstep = 1; curstep <= step; curstep++){
            foreach ((int hix, int hiy) in cur){
                // Debug.Log(hix);
                // Debug.Log(hiy);
                (int x, int y) curnode = (hix, hiy);
                List<(int x, int y)> neighbor = graph[curnode];
                // Debug.Log(neighbor.Count);
                for(int i = 0; i < neighbor.Count; i++){
                    // Debug.Log(neighbor[i]);
                    nxt.Add(neighbor[i]);
                }
            }
            cur = nxt; nxt = new HashSet<(int x, int y)>();
        }
        return cur;
    }

    void add_edge(int x, int y, int nx, int ny){
        if(nx < 0 || nx >= Width || ny < 0 || ny >= Height || isBlock[x, y] == 1 || isBlock[nx,ny] == 1) return;
        (int x, int y) u = (x, y);
        (int x, int y) v = (nx, ny);
        if(!graph.ContainsKey(u)){
            List<(int x, int y)> tmp = new List<(int x, int y)>();
            // Debug.Log(u);
            graph.Add(u, tmp);
        }
        graph[u].Add(v);
    }

    void getPath(){
        blockX = new int[10]; blockY = new int[10];
        for(int i=0;i<numBlock/2;i++){
            blockX[i] = UnityEngine.Random.Range(1, Width-1);
            blockY[i] = 0;
            isBlock[blockX[i], 0] = 1;
            // add_edge(blockX[i], 0, blockX[i], 1);
        }
        for(int i=numBlock/2;i<numBlock;i++){
            blockX[i] = UnityEngine.Random.Range(1, Width-1);
            blockY[i] = Height-1;
            isBlock[blockX[i], Height-1] = 1;
            // add_edge(blockX[i], Height-1, blockX[i], Height-2);
        }
        for(int i=0;i<numBlock/2;i++){
            blockX[i] = 1;
            blockY[i] = UnityEngine.Random.Range(1, Height-1);
            isBlock[1, blockY[i]] = 1;
            // add_edge(1,  blockY[i], 2, blockY[i]);
        }
        for(int i=numBlock/2;i<numBlock;i++){
            blockX[i] = Width-2;
            blockY[i] = UnityEngine.Random.Range(1, Height-1);
            isBlock[Width-2, blockY[i]] = 1;
            // add_edge(Width-2, blockY[i], Width-3, blockY[i]);
        }
        add_edge(0, 0, 0, 1); add_edge(0, 0, 1, 0);
        for(int y=1;y<Height-1;y++){
            add_edge(0, y, 0, y + 1); add_edge(0, y, 0, y - 1); add_edge(0, y, 1, y);
            add_edge(Width-1, y, 0, y + 1); add_edge(Width-1, y, 0, y - 1); add_edge(Width-1, y, Width-2, y);
        }
        for(int x=1;x<Width-1;x++){
            add_edge(x, 0, x + 1, 0); add_edge(x, 0, x - 1, 0); add_edge(x, 0, x, 1);
            add_edge(x, Height - 1, x + 1, Height - 1); add_edge(x, Height - 1, x - 1, Height - 1); add_edge(x, Height - 1, x, Height - 2);
        }
        for(int y=1;y<Height-1;y++){
            add_edge(1, y, 1, y + 1); add_edge(1, y, 1, y - 1); add_edge(1, y, 2, y);
            add_edge(Width-2, y, Width-2, y + 1); add_edge(Width-2, y, Width-2, y - 1); add_edge(Width-2, y, Width-3, y);
        }
        for(int x=1;x<Width-1;x++){
            add_edge(x, 1, x + 1, 1); add_edge(x, 1, x - 1, 1); add_edge(x, 1, x, 2);
            add_edge(x, Height - 2, x + 1, Height - 2); add_edge(x, Height - 2, x - 1, Height - 2); add_edge(x, Height - 2, x, Height - 3);
        }
    }

    void Start(){
        grid = gameObject.GetComponent<Grid>(); outer = 1;
		UnityEngine.UI.Button btn = yourButton.GetComponent<UnityEngine.UI.Button>();
		btn.onClick.AddListener(TaskOnClick);
        isBlock = new int[Width, Height]; vst = new int[Width, Height];
        for(int y=0;y<Height;++y){
            for(int x=0;x<Width;++x){
                isBlock[x, y] = 0; vst[x, y] = 0;
            }
        }
        vst[0, 0] = 1;
        getPath();
        for(int y=0;y<Height;++y){
            for(int x=0;x<Width;++x){
                if(isBlock[x, y] == 1) continue;
                if((x < Width/2-1 || x >= Width / 2 + 1) || (y < Width/2-1 || y >= Width / 2 + 1)){
                    int tileNumber = UnityEngine.Random.Range(0, GroundTiles.Length);
                    m_Tilemap.SetTile(new Vector3Int(x, y, 0), GroundTiles[tileNumber]);
                }
            }
        }
        boss.SetTile(new Vector3Int(1, 1, 0), bosstile);
        highlight.SetTile(new Vector3Int(0, 0, 0), hoverTile);
        nwx = 0; nwy = 0;
    }

    void TaskOnClick(){
        // var rnd = new System.Random();
        // int num = rnd.Next(3);
        UpdateHighlight(1);
        // GameObject dicePrefab = Resources.Load<GameObject>("Prefab/Dice");
        // GameObject dice = Instantiate(dicePrefab);
        // Dice diceInfo = dice.GetComponent<Dice>();
        // Debug.Log(diceInfo.currDamage);
        // UpdateHighlight(diceInfo.currDamage);
    }

    // Update is called once per frame
    void Update() {
        // Mouse over -> highlight tile
        Vector3Int mousePos = GetMousePosition();
        // Debug.Log(mousePos);

        // Left mouse click -> add path tile
        (int x, int y) tmp = (mousePos[0], mousePos[1]);
        if (choose == 1 && Input.GetMouseButton(0) && nxt_step.Contains(tmp)) {
            // Debug.Log("here");
            nwx = mousePos[0]; nwy = mousePos[1]; choose = 0;
            if(!((nwx == 0 || nwx == Width -1 || nwy == 0 || nwy == Height - 1))){
                outer = 0;
            }
            vst[nwx, nwy] = 1;
        }

        // Right mouse click -> remove path tile
        // if (Input.GetMouseButton(1)) {
        //     pathMap.SetTile(mousePos, null);
        // }
    }

    void UpdateHighlight(int step){
        highlight.SetTile(new Vector3Int(nwx, nwy, 0), null);
        if(nxt_step != null){
            if(outer == 0){
                bossHighlight.SetTile(new Vector3Int(1, 1, 0), null);
            }
            foreach ((int x, int y) in nxt_step){
                if((x == 3 || x == 2) && (y == 3 || y == 2)){
                    bossHighlight.SetTile(new Vector3Int(1, 1, 0), null);
                }
                else{
                    highlight.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }
        Debug.Log("original vertex: " + nwx + nwy + outer);
        nxt_step = findVertex(nwx, nwy, step);
        if(outer == 0){
            bossHighlight.SetTile(new Vector3Int(1, 1, 0), hoverTile);
        }
        foreach ((int x, int y) in nxt_step){
            Debug.Log("(" + x + "," + y + ")");
            if((x == 3 || x == 2) && (y == 3 || y == 2)){
                bossHighlight.SetTile(new Vector3Int(1, 1, 0), hoverTile);
            }
            else if(outer == 0 && (x == 0 || x == Width -1 || y == 0 || y == Height - 1)){
                continue;
            }
            else if(vst[x, y] != 1){
                highlight.SetTile(new Vector3Int(x, y, 0), hoverTile);
            }
        }
        choose = 1;
    }

    Vector3Int GetMousePosition () {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return grid.WorldToCell(mouseWorldPos);
    }

}
