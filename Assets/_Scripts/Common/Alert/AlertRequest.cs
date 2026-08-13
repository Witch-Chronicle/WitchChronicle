/// <summary>
/// AlertManager에서 AlertUIController로 전달하는 런타임 Alert 정보입니다.
/// </summary>
public readonly struct AlertRequest
{
    public AlertType Type { get; }
    public string Message { get; }
    public float LifeTime { get; }

    public AlertRequest(
        AlertType type,
        string message,
        float lifeTime)
    {
        Type = type;
        Message = message;
        LifeTime = lifeTime;
    }
}