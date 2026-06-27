using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TrapTerrainVisual : MonoBehaviour
{
    [SerializeField] private Color fireColor = new Color(1f, 0.25f, 0.05f, 0.45f);
    [SerializeField] private Color chillColor = new Color(0.4f, 0.85f, 1f, 0.45f);
    [SerializeField] private Color shockColor = new Color(1f, 0.95f, 0.25f, 0.45f);
    [SerializeField] private Color waterColor = new Color(0.2f, 0.45f, 1f, 0.35f);
    [SerializeField] private Color gasColor = new Color(0.55f, 1f, 0.35f, 0.35f);
    [SerializeField] private Color oozeColor = new Color(0.55f, 0.9f, 0.15f, 0.45f);
    [SerializeField] private Color pitColor = new Color(0.05f, 0.05f, 0.05f, 0.65f);

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Show(Vector2Int cell, TrapTerrainType terrainType)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = GridSystem.Instance.GridToWorld(cell);
        _spriteRenderer.color = GetColor(terrainType);
    }

    private Color GetColor(TrapTerrainType terrainType)
    {
        switch (terrainType)
        {
            case TrapTerrainType.Fire: return fireColor;
            case TrapTerrainType.Chill: return chillColor;
            case TrapTerrainType.Shock: return shockColor;
            case TrapTerrainType.Water: return waterColor;
            case TrapTerrainType.Gas: return gasColor;
            case TrapTerrainType.Ooze: return oozeColor;
            case TrapTerrainType.Pit: return pitColor;
            default: return Color.white;
        }
    }
}
