using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class TerrainLoader
{
    public List<GameObject> tilePrefab;
    public GameObject playerPrefab;
    private Vector3 playerStartPosition;
    private Vector2 tileSize = new Vector2(1, 1);
    private GameObject currentPlayer;
    private List<GameObject> generatedObjects = new List<GameObject>();
    private int[,] map;
    private Texture2D heightmap;
    private int[,] gameElements;

    private int terrainWidth = 32;
    private int terrainLength = 32;
    private float maxHeight = 10.0f;
    private Gradient heightGradient;

    public void LoadTerrain(Texture2D drawing, int[,] elements, List<GameObject> tilePrefab = null,
            GameObject playerPrefab  = null, Gradient heightGradient = null)
    {
        this.tilePrefab = tilePrefab!=null ? tilePrefab : this.tilePrefab;
        this.playerPrefab = playerPrefab!=null ? playerPrefab : this.playerPrefab;
        this.heightGradient = heightGradient!=null ? heightGradient : this.heightGradient;
        this.gameElements = elements!=null ? elements : this.gameElements;

        ClearTerrain();
        
        this.heightmap = drawing;
        GenerateTerrainFromHeightmap();

        if(currentPlayer == null){
            playerStartPosition = new Vector3(15, 26, 15);
            currentPlayer = MonoBehaviour.Instantiate(playerPrefab, playerStartPosition, Quaternion.identity);
            generatedObjects.Add(currentPlayer);
        }
        
    }


    void GenerateTerrainFromHeightmap()
    {
        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = heightmap.width,
            size = new Vector3(terrainWidth, maxHeight, terrainLength)
        };

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrainObject.layer = 3; //ground

        float[,] heights = new float[heightmap.width, heightmap.height];
        for (int x = 0; x < heightmap.width; x++)
        {
            for (int y = 0; y < heightmap.height; y++)
            {
                float heightValue = heightmap.GetPixel(x, y).grayscale;
                heights[x, y] = heightValue;
                
                int indexTile = gameElements[x, y];
                if(indexTile > 1){
                    Vector3 position = new Vector3(x * tileSize.x, 1-heightValue*2f, y * tileSize.y);
                    GameObject tile = MonoBehaviour.Instantiate(tilePrefab[indexTile], position, Quaternion.identity);
                    generatedObjects.Add(tile);
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);
        generatedObjects.Add(terrainObject);

        ApplyHeightColors(terrain);
    }

    void ApplyHeightColors(Terrain terrain)
    {
        int width = terrain.terrainData.heightmapResolution;
        int height = terrain.terrainData.heightmapResolution;
        Texture2D colorMap = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float heightValue = terrain.terrainData.GetHeight(x, y) / maxHeight;
                Color color = heightGradient.Evaluate(heightValue);
                colorMap.SetPixel(x, y, color);
            }
        }

        colorMap.Apply();

        terrain.materialTemplate = new Material(Shader.Find("Standard"));
        terrain.materialTemplate.mainTexture = colorMap;
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
    
}