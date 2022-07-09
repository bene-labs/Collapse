using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Tile to be used and managed by CollapseGameBoard
public class CollapseTile : MonoBehaviour
{
    [Header("Coloring")]
    [Tooltip("Tiles can be colored to any color in this list when creating a new board")]
    public List<Color> availableColors;
    private Color m_currentColor;
    public Color CurrentColor
    {
        get => m_currentColor;
        set
        {
            if (m_currentColor == value) return;

            m_currentColor = value;
            baseSpriteRenderer.color = m_currentColor;
        }
        
    }
    public Color hoverBorderColor;
    private Color m_idleBorderColor;
    
    [Header("Sprites")]
    [FormerlySerializedAs("spriteRenderer")] 
    public SpriteRenderer baseSpriteRenderer;
    [Header("Must be a bigger copy of the base sprite and located behind it to act as a border")]
    public SpriteRenderer borderSprite;

    [HideInInspector]
    public int coordinateX;
    [HideInInspector]
    public int coordinateY;

    // Start is called before the first frame update
    private void Start()
    {
        m_idleBorderColor = borderSprite.color;
    }

    // Called when the mouse hits this GameObject's collider
    private void OnMouseEnter()
    {
        borderSprite.color = hoverBorderColor;
    }

    // Called when this object is clicked by the mouse
    private void OnMouseOver()
    {
        if (Camera.main == null) return;
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new(mousePos.x, mousePos.y);
        var hit = Physics2D.Raycast(mousePos2D, Vector2.up);

        if (Input.GetMouseButtonUp(0) && hit.transform == this.transform)
        {
            // Tell parent GameBoard to handle this tile being clicked.
            GetComponentInParent<CollapseGameBoard>().PopAdjacentTiles(this);
        }
    }

    // Called when mouse leaves this GameObject's collider.
    private void OnMouseExit()
    {
        borderSprite.color = m_idleBorderColor;
    }
}
