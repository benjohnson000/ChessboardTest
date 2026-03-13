using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UNITY ONLY!!!
/// Meant to simulate chess piece on board. can be dragged and dropped
/// </summary>
public class SIM_ChessPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private List<UTIL_ChessPieceIconSerializer> icons;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D coll;

    [System.Serializable]
    public class UTIL_ChessPieceIconSerializer
    {
        public string name;
        public Sprite icon;
    }

    private void SetIconFromPieceName(string name)
    {
        foreach (UTIL_ChessPieceIconSerializer p in icons)
        {
            if (p.name == name)
            {
                sr.sprite = p.icon;
                break;
            }
        }
    }

    public static SIM_ChessPiece Create(Vector2 pos, string name)
    {
        SIM_ChessPiece piece = Instantiate(Resources.Load<SIM_ChessPiece>("ChessPiece"), pos, Quaternion.identity);
        piece.SetIconFromPieceName(name);
        return piece;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        coll.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = benjohnson.Utilities.Input.MouseWorldPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        coll.enabled = true;
    }
}
