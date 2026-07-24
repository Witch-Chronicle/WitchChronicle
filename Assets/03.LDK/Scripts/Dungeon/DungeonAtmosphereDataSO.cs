using UnityEngine;

/// <summary>
/// 던전의 전체적인 대기 환경(안개, 조명 등) 데이터를 정의하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "DungeonAtmosphereData", menuName = "Dungeon/Atmosphere Data")]
public class DungeonAtmosphereDataSO : ScriptableObject
{
    [Header("Fog Settings")]
    [SerializeField] private bool _useFog = true;
    [SerializeField] private Color _fogColor = new Color(0.05f, 0.05f, 0.08f);
    [SerializeField] private FogMode _fogMode = FogMode.ExponentialSquared;
    [SerializeField] private float _fogDensity = 0.04f;
    [SerializeField] private float _fogStartDistance = 0f;
    [SerializeField] private float _fogEndDistance = 30f;

    [Header("Ambient Light")]
    [SerializeField] private Color _ambientLight = new Color(0.15f, 0.15f, 0.2f);

    public bool UseFog => _useFog;
    public Color FogColor => _fogColor;
    public FogMode FogMode => _fogMode;
    public float FogDensity => _fogDensity;
    public float FogStartDistance => _fogStartDistance;
    public float FogEndDistance => _fogEndDistance;
    public Color AmbientLight => _ambientLight;
}