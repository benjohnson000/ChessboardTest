using UnityEngine;

/// <summary>
/// UNITY ONLY!!! (MonoBehaviour)
/// Meant to simulate a physical board space
/// Must be able to: check for the presence of a game piece on the square
/// </summary>
public class SIM_BoardSpace : MonoBehaviour
{
    [SerializeField] private SpriteRenderer led;

    [SerializeField] private bool detecting;

    public static SIM_BoardSpace Create(Vector2 worldPosiiton)
    {
        SIM_BoardSpace bspace = Instantiate(Resources.Load<SIM_BoardSpace>("BoardSpace"), worldPosiiton, Quaternion.identity);
        bspace.SetLEDColor(Color.clear);
        return bspace;
    }

    private void Update()
    {
        detecting = CheckForGamePiece();
    }

    /// <summary>
    /// Returns true of a game piece is present on this boardspace, otherwise false
    /// </summary>
    /// <returns></returns>
    public bool CheckForGamePiece()
    {
        return Physics2D.OverlapPoint(transform.position);
    }

    public void SetLEDColor(Color color) => led.color = color;
}