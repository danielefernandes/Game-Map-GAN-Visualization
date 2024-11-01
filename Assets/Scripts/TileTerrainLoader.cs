using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class TileTerrainLoader
{
    public List<GameObject> tilePrefab;
    public GameObject playerPrefab;
    private Vector3 playerStartPosition;
    private Vector2 tileSize = new Vector2(1, 1); // Tamanho do tile
    private GameObject currentPlayer;
    private List<GameObject> generatedObjects = new List<GameObject>();
    private int[,] map;

    public void LoadTerrain(Texture2D drawing, List<GameObject> tilePrefab,
            GameObject playerPrefab)
    {
        this.tilePrefab = tilePrefab;
        this.playerPrefab = playerPrefab;

        ClearTerrain();
        

        if(!drawing){
            map = LoadMatrixFromFile("Assets/Maps/mapa0.txt");
        }
        LoadTerrain(map);
        
    }
    public void LoadTerrain(int[,] map)
    {
        
        ClearTerrain();
        this.map = map;
        GenerateTerrain(map);

        if(currentPlayer == null){
            playerStartPosition = new Vector3(15 * tileSize.x, 1, 15 * tileSize.y);
            currentPlayer = MonoBehaviour.Instantiate(playerPrefab, playerStartPosition, Quaternion.identity);
            generatedObjects.Add(currentPlayer);
        }
        
    }

    void GenerateTerrain(int[,] map)
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                
                int indexTile = map[i, j];
                Vector3 position;

                if(indexTile==0){
                    position = new Vector3(i * tileSize.x, 0, j * tileSize.y);
                }
                else{
                    position = new Vector3(i * tileSize.x, 1, j * tileSize.y);
                }
                
                GameObject tile = MonoBehaviour.Instantiate(tilePrefab[indexTile], position, Quaternion.identity);
                generatedObjects.Add(tile);

                if(indexTile==2){
                    playerStartPosition = position;
                    currentPlayer = tile;

                }
                

                
            }
        }
    }

    private void ClearTerrain()
    {
        foreach (GameObject obj in generatedObjects)
        {
            MonoBehaviour.Destroy(obj);
        }
        generatedObjects.Clear();

        if (currentPlayer != null)
        {
            MonoBehaviour.Destroy(currentPlayer);
            currentPlayer = null;
        }
    }


    private int[,] LoadMatrixFromFile(string path)
    {
        string[] lines = System.IO.File.ReadAllLines(path);
        int rows = lines.Length;
        int cols = lines[0].Split(' ').Length-1;

        int[,] matrix = new int[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            string[] values = lines[i].Split(' ');
            for (int j = 0; j < cols; j++)
            {
                if(values[j] != "")
                    matrix[i, j] = int.Parse(values[j]);
            }
        }
        return matrix;
    }

    
}