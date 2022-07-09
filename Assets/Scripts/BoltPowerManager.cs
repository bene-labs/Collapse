using UnityEngine;
using TMPro;

// Tracks and displays number of available Bolt Power usages. Used by CollapseGameBoard
public class BoltPowerManager : MonoBehaviour
{   
    // As there should only be one BoltPowerManager, this instance is exposed as static member variable
    public static BoltPowerManager Instance => s_instance;
    private static BoltPowerManager s_instance;

    [Header("Configuration")]
    [SerializeField]
    [Tooltip("Power uses cannot go above this value")]
    private int m_maxUses = 6;
    public int MaxUses
    {
        set
        {
            if (m_currentUses == m_maxUses)
            {
                countText.color = Color.black;
            }
            m_maxUses = value;
        }
        get => m_maxUses;
    }
    [SerializeField]
    [Tooltip("Bolt Power cannot be used while this is zero.")]
    private int m_currentUses = 3;
    public int CurrentUses
    {
        set
        {
            if (value > m_maxUses)
                return;

            if (value == m_maxUses)
            {
                countText.color = Color.yellow;
            }
            else if (value > m_currentUses)
            {
                countText.color = Color.green;
            } 
            else if (value == 0)
            {
                countText.color = Color.red;
            } 
            else
            {
                countText.color = Color.black;
            }
            countText.text = "x" + value;
            m_currentUses = value;
        }
        get => m_currentUses;
    }
    [Tooltip("Whenever you get this many points, a new Bolt Power Usage is added")]
    public int scoreRequirement = 5000;
    [Header("Linked UI Elements")]
    public TextMeshProUGUI countText;

    // Awake is called before Start
    private void Awake()
    {
        s_instance = this;
    }
    
    // Start is called before the first frame update
    private void Start()
    { 
        CurrentUses = m_currentUses; // triggers setter function and updates Text
    }
}
