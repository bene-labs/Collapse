using UnityEngine;
using TMPro;

// Displays and calculates Score
public class ScoreManager : MonoBehaviour
{   
    public static ScoreManager Instance => s_instance;
    private static ScoreManager s_instance;
    
    private int m_score;
    public int Score {
        get => m_score;

        set
        {
            // score cannot be decreased from external sources
            if (value < m_score) return; 
            m_isNewIncomingPoints = true;
            m_incomingPoints += value - m_score;
            scoreText.text = "Score: " + m_score + " + " + m_incomingPoints;
            m_updateTimer = 0f;
        }
    }
    private int m_incomingPoints;
    public int IncomingPoints
    {
        get => m_incomingPoints;
        set
        {
            if (value <= m_incomingPoints) return;
            m_isNewIncomingPoints = true;
            m_incomingPoints = value;
            scoreText.text = "Score: " + m_score + " + " + m_incomingPoints;
            m_updateTimer = 0f;
        }
    }
    
    [Header("Scoring Timer")]
    [Tooltip("Delay in seconds before incoming points are counted down")]
    public float scoreUpdateDelay = 1f;
    [Tooltip("While counting down incoming points, one point is added to the score every x seconds")]
    public float scoreUpdateSpeed = 0.1f;
    private float m_updateTimer;
    private bool m_isNewIncomingPoints; // Used to apply the scoreUpdateDelay
    
    [Header("Score Bonus")]
    [Tooltip("Minimum amount of incoming points that must be left at the end of the level to receive a bonus")]
    public int postLevelBonusRequirement = 100;
    [Tooltip("If Bonus Requirement is met, the incoming points will be multiplied by this value")]
    public float levelEndBonusMultiplier = 1.0f;
    
    [Header("Linked UI Elements")]
    public TextMeshProUGUI scoreText;

    // Awake is called before Start
    private void Awake()
    {
        s_instance = this; 
    }
    
    // Update is called once per frame
    private void Update()
    {
        if (m_incomingPoints <= 0)
        {
            m_updateTimer = 0;
            return;
        }

        m_updateTimer += Time.deltaTime;

        if (m_isNewIncomingPoints)
        {
            if (m_updateTimer < scoreUpdateDelay) return;
            m_updateTimer = 0;
            m_isNewIncomingPoints = false;
        }
        else
        {
            if (m_updateTimer < scoreUpdateSpeed) return;
            m_updateTimer = 0;
            m_score++;
            m_incomingPoints--;
            scoreText.text = "Score: " + m_score + (m_incomingPoints > 0 ? (" + " + m_incomingPoints) : "");
            if (m_score % BoltPowerManager.Instance.scoreRequirement == 0)
            {
                BoltPowerManager.Instance.CurrentUses++;
            }
        }
    }

    // Is true if no incoming points are left to count down
    public bool IsIdle()
    {
        return m_incomingPoints == 0;
    }

    // Is true if a post game bonus can be rewarded
    public bool IsPostGameBonusRequirementMet()
    {
        return m_incomingPoints >= postLevelBonusRequirement;
    }
}
