using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

// Creates and manages all tiles that form the GameBoard
public class CollapseGameBoard : MonoBehaviour
{
    // As there should only be one game-board, this instance is exposed as static member variable
    public static CollapseGameBoard Instance { get; private set; }
    
    [Header("Setup")]
    [Tooltip("Prefab of a tile object that the map should be filled with")]
    [FormerlySerializedAs("tile")]
    public GameObject tilePrefab;
    private List<List<CollapseTile>> m_tileMap = new List<List<CollapseTile>>();
    private float m_tileWidth;
    private float m_tileHeight;

    [Header("Configuration")]
    [Tooltip("Number of tiles per Column")]
    public int height = 5;
    [Tooltip("Number of tiles per Row")]
    public int width = 10;
    [Tooltip("Number of unique colors to be used by the Tiles. (As long as Tile prefab has enough available)")]
    public int uniqueTileCount = 2;

    // Awake is called before Start
    private void Awake()
    {
        Instance = this; // exposes Instance
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (uniqueTileCount > tilePrefab.GetComponent<CollapseTile>().availableColors.Count)
        {
            uniqueTileCount = tilePrefab.GetComponent<CollapseTile>().availableColors.Count;
        }

        Refill();

        InvokeRepeating(nameof(MoveTilesDown), 0.5f, 0.2f);
        InvokeRepeating(nameof(MoveTilesToCenter), 0.5f, 0.33f);
    }

    // adds new tiles to all row and columns according to current configuration. Should only be called on an empty board.
    private void Fill()
    {
        var tempTile = Instantiate(tilePrefab);
        var boardRenderer = gameObject.GetComponent<SpriteRenderer>();
        var tileRenderer = tempTile.transform.GetChild(0).GetComponent<SpriteRenderer>();
        
        tempTile.transform.localScale = new Vector3((boardRenderer.bounds.size.x / width) / tileRenderer.bounds.size.x, (boardRenderer.bounds.size.y / height) / tileRenderer.bounds.size.y);
        m_tileWidth = tileRenderer.bounds.size.x;
        m_tileHeight = tileRenderer.bounds.size.y;
        
        // tiles are created from top to bottom, as tile movement will also only be from top to bottom
        for (var y = height - 1; y >= 0; y--)
        {
            // tiles are added to an Child 'Row' GameObject for an easier view in the hierarchy
            var row = new GameObject("row" + (height - y));
            row.transform.parent = this.gameObject.transform;

            m_tileMap.Add(new List<CollapseTile>());
            for (var x = 0; x < width; x++)
            {
                var newTile = Instantiate(tempTile,
                    this.transform.position + new Vector3((x * tileRenderer.bounds.size.x) - boardRenderer.bounds.size.x / 2 + tileRenderer.bounds.size.x / 2,
                    (y * tileRenderer.bounds.size.y) - boardRenderer.bounds.size.y / 2 + tileRenderer.bounds.size.y / 2, -0.1f),
                    Quaternion.identity, row.transform);
                var newTileScript = newTile.GetComponent<CollapseTile>();
                newTileScript.coordinateY = height - y - 1;
                newTileScript.coordinateX = x;
                m_tileMap[height - y - 1].Add(newTileScript);
            }
        }
        Destroy(tempTile);
    }

    // removes all tiles from the board
    private void Clear()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        m_tileMap.Clear();
    }

    // empties board and fills it with new tiles that are assigned random colors.
    public void Refill()
    {
        Clear();
        Fill();
        AssignTileColors();
    }

    // returns true if the board does not contain any tiles
    public bool IsEmpty()
    {
        return (m_tileMap.FindAll(row => row.Exists(tile => tile != null)).Count == 0);
    }

    // assigns random colors to all tiles, based on the available colors of the tiles and the amount of unique tile colors enabled.
    private void AssignTileColors()
    {
        foreach (var row in m_tileMap)
        {
            foreach (var tile in row)
            {
                var newColor = tile.availableColors[Random.Range(0, uniqueTileCount)];
                tile.CurrentColor = newColor;
            }
        }
    }

    // finds all connected tiles of the same color by searching for them recursively
    private void FindMatchingTileRec(int x, int y, List<CollapseTile> matchingTiles)
    {
        CollapseTile up = y > 0 ? m_tileMap[y - 1][x] : null;
        CollapseTile down = y < m_tileMap.Count - 1 ? m_tileMap[y + 1][x] : null;
        CollapseTile left = x > 0 ? m_tileMap[y][x - 1] : null;
        CollapseTile right = x < m_tileMap[y].Count - 1 ? m_tileMap[y][x + 1] : null;

        if (up != null && !matchingTiles.Contains(up) && up.CurrentColor == matchingTiles[0].CurrentColor)
        {
            matchingTiles.Add(up);
            FindMatchingTileRec(x, y - 1, matchingTiles);
        }
        if (down != null && !matchingTiles.Contains(down) && down.CurrentColor == matchingTiles[0].CurrentColor)
        {
            matchingTiles.Add(down);
            FindMatchingTileRec(x, y + 1, matchingTiles);
        }
        if (left != null && !matchingTiles.Contains(left) && left.CurrentColor == matchingTiles[0].CurrentColor)
        {
            matchingTiles.Add(left);
            FindMatchingTileRec(x - 1, y, matchingTiles);
        }
        if (right != null && !matchingTiles.Contains(right) && right.CurrentColor == matchingTiles[0].CurrentColor)
        {
            matchingTiles.Add(right);
            FindMatchingTileRec(x + 1, y, matchingTiles);
        }
    }

    // Find and remove all tiles connected to the originTile that share the same color. If successful, the originTile will also be removed.
    public void PopAdjacentTiles(CollapseTile originTile)
    {
        var matchingTiles = new List<CollapseTile> { originTile };

        // Find matching tiles
        FindMatchingTileRec(originTile.coordinateX, originTile.coordinateY, matchingTiles);

        // Remove base tile with bolt power up, if no matching tiles where found
        if (matchingTiles.Count == 1)
        {
            // Abort if no bolt power up is available
            if (BoltPowerManager.Instance.CurrentUses <= 0)
            {
                return;
            }
            BoltPowerManager.Instance.CurrentUses--;
        }

        // Add points for successfully removing tiles
        int gainedPoints = 0;
        for (var i = 0; i < matchingTiles.Count; i++)
        {
            gainedPoints += i; // gained points scale exponentially with the number of tiles removed at once
        }
        ScoreManager.Instance.Score += gainedPoints;

        // remove tiles from board
        for (var i = matchingTiles.Count - 1; i >= 0; i--)
        {
            m_tileMap[matchingTiles[i].coordinateY][matchingTiles[i].coordinateX] = null;
            Destroy(matchingTiles[i].gameObject);
        }
    }

    // Moves tiles downwards unless they are directly above another tile or already touching the floor
    private void MoveTilesDown()
    {
        if (IsEmpty())
            return;

        for (var y = m_tileMap.Count - 1; y > 0; y--)
        {
            // Goes Bottom to Top to find empty spaces with tiles above them
            for (var x = m_tileMap[y].Count - 1; x >= 0; x--)
            {
                if (m_tileMap[y][x] != null || m_tileMap[y - 1][x] == null) continue;
                
                // move floating tiles to the empty space below them
                m_tileMap[y - 1][x].coordinateY++;
                m_tileMap[y - 1][x].gameObject.transform.Translate(new Vector3(0, -m_tileHeight, 0));
                m_tileMap[y][x] = m_tileMap[y - 1][x];
                m_tileMap[y - 1][x] = null;
            }
        }
    }

    // prevents tiles from being cut off from center tiles by doing the following on each function call:
    // 1. moving tiles on the left side of the board to the right, unless they are touching another tile on their right side
    // 2. doing the exact same thing to tiles on the right side of the board, by moving them to the left
    private void MoveTilesToCenter()
    {
        if (IsEmpty() || IsVerticalMovement()) // Horizontal Movement should only occurs while no tiles are falling
        {
            return;
        }

        // move left tiles to center
        for (var x = (int)(width / 2); x > 0; x--)
        {
            for (var y = height - 1; y >= 0 && m_tileMap[y][x] == null; y--)
            {
                if (m_tileMap[y][x - 1] == null)
                    continue;
                m_tileMap[y][x - 1].coordinateX++;
                m_tileMap[y][x - 1].gameObject.transform.Translate(new Vector3(m_tileWidth, 0, 0));
                m_tileMap[y][x] = m_tileMap[y][x - 1];
                m_tileMap[y][x - 1] = null;
            }
        }

        // move right tiles to center
        for (var x = (int)(width / 2) + 1; x < width - 1; x++)
        {
            for (var y = height - 1; y >= 0 && m_tileMap[y][x] == null; y--)
            {
                if (m_tileMap[y][x + 1] == null)
                    continue;
                m_tileMap[y][x + 1].coordinateX--;
                m_tileMap[y][x + 1].gameObject.transform.Translate(new Vector3(-m_tileWidth, 0, 0));
                m_tileMap[y][x] = m_tileMap[y][x + 1];
                m_tileMap[y][x + 1] = null;
            }
        }
    }

    // returns true if no tiles are floating in the air in column x
    private bool IsColumGrounded(int x)
    {
        for (var y = 0; y < height - 1; y++)
        {
            // checks if tile has an empty field below them
            if (m_tileMap[y][x] != null && m_tileMap[y + 1][x] == null)
                return false;
        }
        return true;
    }

    // returns true if a tile is floating in the air
    private bool IsVerticalMovement()
    {
        for (var x = 0; x < width; x++)
        {
            if (!IsColumGrounded(x))
            {
                return true;
            }
        }
        return false;
    }

    // returns true if a floor tile is not connected to tiles in the center column
    private bool IsHorizontalTileMovement()
    {
       
        for (var x = 1; x < m_tileMap[0].Count; x++)
        {
            if (m_tileMap[0][x] == null) continue;

            CollapseTile left = x > 0 ? m_tileMap[0][x - 1] : null;
            CollapseTile right = x < m_tileMap[0].Count - 1 ? m_tileMap[0][x + 1] : null;

            if (left == null && m_tileMap[0].ToArray()[..(x - 1)].Any(tile => tile != null))
            {
                return true;
            }
            if (right == null && m_tileMap[0].ToArray()[(x + 1)..].Any(tile => tile != null))
            {
                return true;
            }
        }
        return false;
    }

    // Returns true, if no tile movement is required (no tiles float or are disconnected from center)
    public bool IsIdle()
    {
        return !IsVerticalMovement() && !IsHorizontalTileMovement();
    }
    
    // Returns true if there are no connected tiles of the same color
    public bool IsMatchingTileLeft()
    {
        for (var y = 0; y < m_tileMap.Count; y++)
        {
            for (var x = 0; x < m_tileMap[y].Count; x++)
            {
                CollapseTile tile = m_tileMap[y][x];
                if (tile == null) continue;

                CollapseTile up = y > 0 ? m_tileMap[y - 1][x] : null;
                CollapseTile down = y < m_tileMap.Count - 1 ? m_tileMap[y + 1][x] : null;
                CollapseTile left = x > 0 ? m_tileMap[y][x - 1] : null;
                CollapseTile right = x < m_tileMap[y].Count - 1 ? m_tileMap[y][x + 1] : null;

                if ((up != null && up.CurrentColor == tile.CurrentColor) || (down != null && down.CurrentColor == tile.CurrentColor)
                    || (left != null && left.CurrentColor == tile.CurrentColor) || (right != null && right.CurrentColor == tile.CurrentColor))
                    return true;
            }
        }
        return false;
    }
    
    public void QueueNextLevel(float delay)
    {
        Invoke(nameof(StartNextLevel), delay);
    }

    // Increases difficulty, increases level counter and refills board
    // Should only be invoked via QueueNextLevel
    private void StartNextLevel()
    {
        if (uniqueTileCount < tilePrefab.GetComponent<CollapseTile>().availableColors.Count
            && (GameStateManager.Instance.Level + 1) % GameStateManager.Instance.difficultyIncreaseThreshold == 0)
        {
            uniqueTileCount++;
            if (height > width)
            {
                width--;
                height -= height / width;
            }
            else if (width > height)
            {
                height--;
                width -= width / height;
            }
        }
        else
        {
            if (height > width)
            {
                width++;
                height += height / width;
            }
            else if (width > height)
            {
                height++;
                width += width / height;
            }
        }
        Refill();
        GameStateManager.Instance.IsNextLevelQueued = false;
        GameStateManager.Instance.Level++;
    }
}
