using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// Handles games-states like GameOvers and level changes
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance => s_instance;
    private static GameStateManager s_instance;

    [Header("Level Management")]
    [Tooltip("The number of unique tiles in the map will increase every x levels, according to this value")]
    public int difficultyIncreaseThreshold = 5;
    public float newLevelStartDelay = 2.0f;

    private int m_level = 1;
    public int Level
    {
        get => m_level;
        set
        {
            if (m_level == value) return; 
            m_level = value;
            levelText.text = "Level " + m_level;
            eventText.text = "";
            m_isPostLevelBonusAwarded = false;
        }
    }
    private bool m_isNexLevelQueued;
    public bool IsNextLevelQueued
    {
        get => m_isNexLevelQueued;
        set
        {
            if (value == false)
            {
                m_isNexLevelQueued = false;
            } else
            {
                Debug.LogWarning("Only The GameStateManager is allowed queue the next level!");
            }
        }
    }
    private bool m_isPostLevelBonusAwarded;
    public bool IsPostLevelBonusAwarded => m_isPostLevelBonusAwarded;
    private bool m_isGameOver;

    [Header("Linked UI Elements")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI eventText;
    public GameObject restartButton;
    public GameObject quitButton;

    private CollapseGameBoard m_gameBoard;
    
    // Awake is called before Start
    private void Awake()
    {
        s_instance = this;
    }

    // Start is called before the first frame update
    private void Start()
    {
        quitButton.SetActive(false);
        restartButton.SetActive(false);
        m_gameBoard = CollapseGameBoard.Instance;
    }

    // Update is called once per frame
    private void Update()
    {
        // Restarts game when pressing r key
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        
        // Once the game is over, no new level can be loaded. Restarting and Quitting is already handled via buttons.
        if (m_isGameOver)
        {
            return;
        }
        
        // Once the board is cleared, the next level must be scheduled if it wasn't already
        if (!m_isNexLevelQueued && m_gameBoard.IsEmpty())
        {
            // Handles Post Level Score Bonus
            if (!m_isPostLevelBonusAwarded)
            {
                if (ScoreManager.Instance.IsPostGameBonusRequirementMet())
                {
                    var scoreManger = ScoreManager.Instance;
                    var bonus = (int) (scoreManger.IncomingPoints * scoreManger.levelEndBonusMultiplier);
                    eventText.text = "Level complete with " + bonus + " Bonus!";
                    ScoreManager.Instance.IncomingPoints += bonus;
                } 
                else
                {
                    eventText.text = "Level complete!";
                }
                m_isPostLevelBonusAwarded = true;
            }
            // Queues next level once scoring is completed
            else if (ScoreManager.Instance.IsIdle())
            {
                m_isNexLevelQueued = true;
                m_gameBoard.QueueNextLevel(newLevelStartDelay);
            }
        }

        HandleGameOver();
    }

    // Checks if game is lost and prepares GameOver-Screen if this is the case 
    private void HandleGameOver()
    {
        if (m_gameBoard.IsEmpty() || BoltPowerManager.Instance.CurrentUses != 0 || !m_gameBoard.IsIdle()) return;
        if (CollapseGameBoard.Instance.IsMatchingTileLeft()) return;
        m_isGameOver = true;
        quitButton.SetActive(true);
        restartButton.SetActive(true);
        eventText.text = "GAME OVER";
        eventText.color = Color.red;
    }
    
    public void RestartGame()
    {
        // Loads Scene again, as Game is currently one Scene only
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        // Will only be run in built game
        Application.Quit();
    }
}
