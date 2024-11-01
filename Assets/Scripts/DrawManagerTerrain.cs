using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;

public class DrawManagerTerrain : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject modalPanel; 
    public RawImage drawField;
    public int brushSize = 5;
    public Color drawColor = Color.black;
    
    public List<GameObject> tilePrefab;
    public Button generateButton;
    public Button cancelButton;
    public GameObject loadingPanel;
    public Texture2D heightmap;
    public Gradient heightGradient;

    private TerrainLoader terrainLoader; 
    private Texture2D textureToSend;
    private RectTransform rawImageRect;
    private GameObject player;
    private Camera playerCamera;
    private PlayerController playerController;
    private CameraController cameraController;
    private bool isModalOpen = false;
    private bool isDrawing;
    
    void Start()
    {
        rawImageRect = drawField.GetComponent<RectTransform>();
        textureToSend = new Texture2D((int)rawImageRect.rect.width, (int)rawImageRect.rect.height, TextureFormat.RGBA32, false);
        drawField.texture = textureToSend;

        generateButton.onClick.AddListener(SubmitDrawing);
        cancelButton.onClick.AddListener(CancelDrawing);
        loadingPanel.SetActive(false);

        int[,] elements = GenerateElements();

        terrainLoader = new TerrainLoader();
        terrainLoader.LoadTerrain(heightmap, elements, tilePrefab, playerPrefab, heightGradient);

        CancelDrawing();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleModal();

        }

        if (!isModalOpen) return;

        if (Input.GetMouseButtonDown(0))
            isDrawing = true;

        if (Input.GetMouseButtonUp(0))
            isDrawing = false;

        if (isDrawing)
        {
            Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, Input.mousePosition, null, out localPoint);

                int x = Mathf.FloorToInt(localPoint.x + rawImageRect.rect.width / 2);
                int y = Mathf.FloorToInt(localPoint.y + rawImageRect.rect.height / 2);
                
                DrawAt(x, y);
        }
    }
    int[,] GenerateElements()
    {
        int rows = heightmap.width;
        int cols = heightmap.height;

        int[,] matrix = new int[rows, cols];
        System.Random random = new System.Random();
        
        int numValues = 10;
        
        for (int i = 0; i < numValues; i++)
        {
            int randomRow = random.Next(0, rows);
            int randomCol = random.Next(0, cols);
            int randomValue = random.Next(3, 7);
            
           
            matrix[randomRow, randomCol] = randomValue;
        }
        return matrix;
    }

    void ToggleModal()
    {
        isModalOpen = !isModalOpen;
        modalPanel.SetActive(isModalOpen);
        
        if (isModalOpen)
        {
            EnableDrawingMode();
        }
        else
        {
            
            CancelDrawing();
        }
    }

    void EnableDrawingMode()
    {
        ClearCanvas();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdatePlayer(false);
    }

    void DisableDrawingMode()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UpdatePlayer(true);
        isDrawing = false;
    }
    async void GenerateTerrain()
    {
        loadingPanel.SetActive(true);
        TextureWrapper responseWrapper = await RequestTerrainAsync(textureToSend);
        float[,] terrainMatrix = responseWrapper.heightmap;
        int[,] elementsMatrix = responseWrapper.matrix;
        //float[,] terrainMatrix = await RequestTerrainAsync(textureToSend);

        if (terrainMatrix != null && elementsMatrix != null)
        {
            Texture2D heightmapTexture = ConvertMatrixToTexture(terrainMatrix);
            Debug.Log("Terreno gerado com sucesso!");
            terrainLoader.LoadTerrain(heightmapTexture, elementsMatrix, tilePrefab, playerPrefab, heightGradient);
        }
        else
        {
            Debug.LogError("Falha ao gerar terreno.");
        }
        loadingPanel.SetActive(false);
        
        UpdatePlayer(true);
    }

    void UpdatePlayer(bool active){
        player = GameObject.FindWithTag("Player");
        PlayerController playerController = player.GetComponent<PlayerController>();
        playerController.enabled = active;


        playerCamera = player.GetComponentInChildren<Camera>();
        playerCamera.enabled = true;
        CameraController cameraController = playerCamera.GetComponent<CameraController>();
        cameraController.enabled = active;
    }

    void CancelDrawing()
    {
        DisableDrawingMode();
        isModalOpen = false;
        modalPanel.SetActive(isModalOpen);
        UpdatePlayer(true);
    }

    void SubmitDrawing()
    {
        DisableDrawingMode();
        GenerateTerrain();
        isModalOpen = false;
        modalPanel.SetActive(isModalOpen);
        UpdatePlayer(true);
    }

    private void DrawAt(int x, int y)
    {
        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int pixelX = x + i;
                int pixelY = y + j;

                if (pixelX >= 0 && pixelX < textureToSend.width && pixelY >= 0 && pixelY < textureToSend.height)
                {
                    textureToSend.SetPixel(pixelX, pixelY, drawColor);
                }
            }
        }
        textureToSend.Apply(); 
    }

    void ClearCanvas()
    {
        Color clearColor = Color.white;
        for (int x = 0; x < textureToSend.width; x++)
        {
            for (int y = 0; y < textureToSend.height; y++)
            {
                textureToSend.SetPixel(x, y, clearColor);
            }
        }
        textureToSend.Apply();
    }


    //public async Task<float[,]> RequestTerrainAsync(Texture2D textureToSend)
    public async Task<TextureWrapper> RequestTerrainAsync(Texture2D textureToSend)
    {
        string apiUrl = "http://127.0.0.1:8000/generate-terrain/";
        
        string matrixJson = ConvertTextureToJson(textureToSend);
        //Debug.Log(matrixJson);
        string response = await SendPostRequestAsync(apiUrl, matrixJson);

        if (response == null)
        {
            Debug.LogError("Erro na requisição ao servidor.");
            return null;
        }

        //Debug.Log($"Resposta do servidor: {response}");

        TextureWrapper responseWrapper = JsonConvert.DeserializeObject<TextureWrapper>(response);
        //Debug.Log(responseWrapper.matrix);
        
        //return responseWrapper.heightmap;
        return responseWrapper;
    }

    async Task<string> SendPostRequestAsync(string url, string data)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Erro: {request.error}");
                return null;
            }
            
            return request.downloadHandler.text;
        }
    }

    string ConvertTextureToJson(Texture2D texture)
    {
        int width = textureToSend.width;
        int height = textureToSend.height;
        int[,] matrix = new int[height, width];
        

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = textureToSend.GetPixel(x, y);
                matrix[x, y] = (pixelColor == Color.white) ? 0 : 1;
            }
        }
        MatrixWrapper wrapper = new MatrixWrapper { matrix = matrix };
        return JsonConvert.SerializeObject(wrapper);
    }

    int[,] ConvertStringToMatrix(string matrixString)
    {
        string[] rows = matrixString.Split(';');
        int rowCount = rows.Length;
        int colCount = rows[0].Split(',').Length;

        int[,] matrix = new int[rowCount, colCount];

        for (int i = 0; i < rowCount; i++)
        {
            string[] cols = rows[i].Split(',');
            for (int j = 0; j < colCount; j++)
            {
                matrix[i, j] = int.Parse(cols[j]);
            }
        }

        return matrix;
    }

    [Serializable]
    public class MatrixWrapper
    {
        public int[,] matrix;
    }
    [Serializable]
    public class TextureWrapper
    {
        public float[,] heightmap;
        public int[,] matrix;
    }

    Texture2D ConvertMatrixToTexture(float[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        
        // Encontrar os valores mínimo e máximo para normalização
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        foreach (float value in matrix)
        {
            if (value < minValue) minValue = value;
            if (value > maxValue) maxValue = value;
        }

        // Converter cada valor da matriz em um pixel na textura
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Normaliza o valor entre 0 e 1
                float normalizedValue = (matrix[x, y] - minValue) / (maxValue - minValue);
                Color color = new Color(normalizedValue, normalizedValue, normalizedValue);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(); // Aplica as mudanças na textura
        return texture;
    }
}
