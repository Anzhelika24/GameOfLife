using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class GameOfLife2 : MonoBehaviour
{
    [Header("Game Modes")]
    public bool isPvPMode = true;
    public Button switchModeButton;

    [Header("Game Settings")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float cellSize = 0.8f;
    public float updateInterval = 0.5f;
    public int cellsPerPlayer = 10;

    [Header("Colors")]
    public Color player1Color = Color.blue;
    public Color player2Color = Color.red;
    public Color singlePlayerColor = Color.black;
    public Color deadColor = Color.white;

    private int[,] grid;
    private GameObject[,] cellObjects;
    private bool isSimulationRunning = false;
    private float timer = 0f;
    
    // PvP переменные
    private int currentPlayer = 1;
    private int player1Score = 0;
    private int player2Score = 0;
    private int placedCellsPlayer1 = 0;
    private int placedCellsPlayer2 = 0;
    private int generation = 0;

    [Header("UI Elements")]
    public Button startPauseButton;
    public Button clearButton;
    public Button randomButton;
    public Slider speedSlider;
    public TextMeshProUGUI generationText;
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI currentPlayerText;
    public Button switchPlayerButton;
    public Button finishButton;

    [Header("Results Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI scoreText;
    public Button closeResultButton;

    void Start()
    {
        CreateGrid();
        SetupUI();
        UpdateUI();
    }

    void Update()
    {
        if (isSimulationRunning)
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                ComputeNextGeneration();
                generation++;
                UpdateUI();
                timer = 0f;
            }
        }
        
        if (!isSimulationRunning && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            if (isPvPMode)
                PlaceCellWithMousePvP();
            else
                ToggleCellWithMouseSingle();
        }
        
        updateInterval = 1f - speedSlider.value;
    }

    void CreateGrid()
    {
        grid = new int[gridWidth, gridHeight];
        cellObjects = new GameObject[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
                cell.transform.localScale = Vector3.one * cellSize * 0.95f;
                
                Renderer renderer = cell.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = deadColor;
                
                cellObjects[x, y] = cell;
                grid[x, y] = 0;
            }
        }
    }

    void SetupUI()
    {
        startPauseButton.onClick.AddListener(ToggleSimulation);
        clearButton.onClick.AddListener(ClearGrid);
        randomButton.onClick.AddListener(RandomizeGrid);
        switchPlayerButton.onClick.AddListener(SwitchPlayer);
        finishButton.onClick.AddListener(FinishGame);
        closeResultButton.onClick.AddListener(CloseResults);
        switchModeButton.onClick.AddListener(SwitchGameMode);
        
        speedSlider.value = 0.5f;
        updateInterval = 1f - speedSlider.value;
    }

    void ToggleCellWithMouseSingle()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 worldPos = hit.point;
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.y / cellSize);
            
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                grid[x, y] = grid[x, y] == 0 ? 1 : 0;
                UpdateCellVisual(x, y);
            }
        }
    }

    void PlaceCellWithMousePvP()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
            
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 worldPos = hit.point;
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.y / cellSize);
                
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                if (grid[x, y] == 0)
                {
                    if ((currentPlayer == 1 && placedCellsPlayer1 < cellsPerPlayer) ||
                        (currentPlayer == 2 && placedCellsPlayer2 < cellsPerPlayer))
                    {
                        grid[x, y] = currentPlayer;
                        UpdateCellVisual(x, y);
                        
                        if (currentPlayer == 1)
                            placedCellsPlayer1++;
                        else
                            placedCellsPlayer2++;
                    }
                }
                else if (grid[x, y] == currentPlayer)
                {
                    grid[x, y] = 0;
                    UpdateCellVisual(x, y);
                    
                    if (currentPlayer == 1)
                        placedCellsPlayer1--;
                    else
                        placedCellsPlayer2--;
                }
                UpdateUI();
            }
        }
    }

    void UpdateCellVisual(int x, int y)
    {
        Renderer renderer = cellObjects[x, y].GetComponent<Renderer>();
        
        if (isPvPMode)
        {
            if (grid[x, y] == 0)
                renderer.material.color = deadColor;
            else if (grid[x, y] == 1)
                renderer.material.color = player1Color;
            else if (grid[x, y] == 2)
                renderer.material.color = player2Color;
        }
        else
        {
            renderer.material.color = grid[x, y] == 0 ? deadColor : singlePlayerColor;
        }
    }

    void ComputeNextGeneration()
    {
        if (isPvPMode)
        {
            ComputeNextGenerationPvP();
        }
        else
        {
            ComputeNextGenerationSingle();
        }
    }

    void ComputeNextGenerationSingle()
    {
        int[,] newGrid = new int[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int aliveNeighbors = CountAliveNeighbors(x, y);
                
                if (grid[x, y] == 1)
                {
                    newGrid[x, y] = (aliveNeighbors == 2 || aliveNeighbors == 3) ? 1 : 0;
                }
                else
                {
                    newGrid[x, y] = (aliveNeighbors == 3) ? 1 : 0;
                }
            }
        }
        
        grid = newGrid;
        UpdateAllCellsVisual();
    }

    void ComputeNextGenerationPvP()
    {
        int[,] newGrid = new int[gridWidth, gridHeight];
        int newPlayer1Score = player1Score;
        int newPlayer2Score = player2Score;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int aliveNeighbors = CountAliveNeighbors(x, y);
                int dominantPlayer = GetDominantNeighborPlayer(x, y);
                
                if (grid[x, y] != 0)
                {
                    newGrid[x, y] = (aliveNeighbors == 2 || aliveNeighbors == 3) ? grid[x, y] : 0;
                }
                else
                {
                    if (aliveNeighbors == 3)
                    {
                        newGrid[x, y] = dominantPlayer;
                        if (dominantPlayer == 1) newPlayer1Score++;
                        else if (dominantPlayer == 2) newPlayer2Score++;
                    }
                }
            }
        }
        
        grid = newGrid;
        player1Score = newPlayer1Score;
        player2Score = newPlayer2Score;
        UpdateAllCellsVisual();
        
        if (CountTotalAliveCells() == 0)
        {
            isSimulationRunning = false;
        }
    }

    int CountAliveNeighbors(int x, int y)
    {
        int count = 0;
        
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                
                int neighborX = (x + i + gridWidth) % gridWidth;
                int neighborY = (y + j + gridHeight) % gridHeight;
                
                if (grid[neighborX, neighborY] != 0)
                {
                    count++;
                }
            }
        }
        
        return count;
    }

    int GetDominantNeighborPlayer(int x, int y)
    {
        int player1Count = 0;
        int player2Count = 0;
        
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                
                int neighborX = (x + i + gridWidth) % gridWidth;
                int neighborY = (y + j + gridHeight) % gridHeight;
                
                if (grid[neighborX, neighborY] == 1) player1Count++;
                else if (grid[neighborX, neighborY] == 2) player2Count++;
            }
        }
        
        return player1Count >= player2Count ? 1 : 2;
    }

    int CountTotalAliveCells()
    {
        int count = 0;
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] != 0) count++;
        return count;
    }

    void UpdateAllCellsVisual()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                UpdateCellVisual(x, y);
    }

    void UpdateUI()
    {
        generationText.text = "Generation: " + generation;
        
        player1ScoreText.gameObject.SetActive(isPvPMode);
        player2ScoreText.gameObject.SetActive(isPvPMode);
        currentPlayerText.gameObject.SetActive(isPvPMode);
        switchPlayerButton.gameObject.SetActive(isPvPMode);
        finishButton.gameObject.SetActive(isPvPMode);
        
        if (isPvPMode)
        {
            player1ScoreText.text = $"Player 1: {player1Score} ({placedCellsPlayer1}/{cellsPerPlayer})";
            player2ScoreText.text = $"Player 2: {player2Score} ({placedCellsPlayer2}/{cellsPerPlayer})";
            currentPlayerText.text = "Current: Player " + currentPlayer;
            
            if (placedCellsPlayer1 >= cellsPerPlayer && currentPlayer == 1 && !isSimulationRunning)
                switchPlayerButton.GetComponent<Image>().color = Color.green;
            else
                switchPlayerButton.GetComponent<Image>().color = Color.white;
                
            if (placedCellsPlayer1 >= cellsPerPlayer && placedCellsPlayer2 >= cellsPerPlayer && !isSimulationRunning)
                startPauseButton.GetComponent<Image>().color = Color.green;
            else if (!isSimulationRunning)
                startPauseButton.GetComponent<Image>().color = Color.white;
        }
        else
        {
            startPauseButton.GetComponent<Image>().color = Color.white;
        }
        
        startPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = 
            isSimulationRunning ? "Pause" : "Start";
            
        switchModeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
            isPvPMode ? "Single" : "PvP";
    }

    public void ToggleSimulation()
    {
        if (!isSimulationRunning)
        {
            if (isPvPMode)
            {
                if (placedCellsPlayer1 >= cellsPerPlayer && placedCellsPlayer2 >= cellsPerPlayer)
                {
                    isSimulationRunning = true;
                }
            }
            else
            {
                isSimulationRunning = true;
            }
        }
        else
        {
            isSimulationRunning = false;
        }
        UpdateUI();
    }

    public void SwitchPlayer()
    {
        if (!isSimulationRunning)
        {
            currentPlayer = currentPlayer == 1 ? 2 : 1;
            UpdateUI();
        }
    }

    public void SwitchGameMode()
    {
        isPvPMode = !isPvPMode;
        ClearGrid();
        UpdateUI();
    }

    public void ClearGrid()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                grid[x, y] = 0;
                
        UpdateAllCellsVisual();
        generation = 0;
        
        if (isPvPMode)
        {
            player1Score = 0;
            player2Score = 0;
            placedCellsPlayer1 = 0;
            placedCellsPlayer2 = 0;
            currentPlayer = 1;
        }
        
        isSimulationRunning = false;
        UpdateUI();
    }

    public void RandomizeGrid()
    {
        ClearGrid();
        
        if (isPvPMode)
        {
            for (int i = 0; i < cellsPerPlayer; i++)
            {
                PlaceRandomCell(1);
                PlaceRandomCell(2);
            }
            placedCellsPlayer1 = cellsPerPlayer;
            placedCellsPlayer2 = cellsPerPlayer;
        }
        else
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = (Random.Range(0, 100) < 30) ? 1 : 0;
                }
            }
            UpdateAllCellsVisual();
        }
        UpdateUI();
    }

    void PlaceRandomCell(int player)
    {
        int x, y;
        do
        {
            x = Random.Range(0, gridWidth);
            y = Random.Range(0, gridHeight);
        } while (grid[x, y] != 0);
        
        grid[x, y] = player;
        UpdateCellVisual(x, y);
    }

    public void FinishGame()
    {
        isSimulationRunning = false;
        ShowResults();
    }

    public void ShowResults()
    {
        if (!isPvPMode) return;
        
        string winner = player1Score > player2Score ? "Player 1 Wins!" : 
                       player2Score > player1Score ? "Player 2 Wins!" : "It's a Tie!";
        
        resultText.text = winner;
        scoreText.text = $"Player 1: {player1Score}\nPlayer 2: {player2Score}";
        resultPanel.SetActive(true);
    }

    public void CloseResults()
    {
        resultPanel.SetActive(false);
    }

    private bool IsPointerOverUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}