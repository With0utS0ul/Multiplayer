using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

// Структура для данных ввода (от клиента к серверу)
public struct MoveData : IReplicateData
{
    public float Horizontal;
    public float Vertical;
    private uint _tick;

    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

// Структура для данных состояния (от сервера к клиенту для сверки)
public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    private uint _tick;

    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

public class PlayerMovementPredicted : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    private CharacterController _cc;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public override void OnStartNetwork()
    {
        // Подписываемся на игровой тик, а не на обновление кадров
        base.TimeManager.OnTick += OnTick;
    }

    public override void OnStopNetwork()
    {
        base.TimeManager.OnTick -= OnTick;
    }

    // Главный метод, который вызывается каждый сетевой "тик"
    private void OnTick()
    {
        // Владелец собирает ввод и "предсказывает" своё движение
        if (base.IsOwner)
        {
            MoveData md = new MoveData
            {
                Horizontal = Input.GetAxisRaw("Horizontal"),
                Vertical = Input.GetAxisRaw("Vertical")
            };
            Replicate(md);
        }
        else
        {
            // Не-владельцы просто вызывают Replicate с пустыми данными
            Replicate(default);
        }

        // Сервер готовит данные для сверки с клиентами
        if (base.IsServerInitialized)
        {
            // ВАЖНО: Вызываем CreateReconcile, чтобы подготовить и отправить данные
            CreateReconcile();
        }
    }

    // --- ЭТОТ МЕТОД БЫЛ ОТСУТСТВУЕТ, НО ОН ОЧЕНЬ ВАЖЕН! ---
    // Он создаёт "слепок" текущего состояния (позиции) для отправки на клиент.
    public override void CreateReconcile()
    {
        // Создаём структуру с текущей позицией
        ReconcileData rd = new ReconcileData
        {
            Position = transform.position
        };
        // Отправляем эти данные в метод с атрибутом [Reconcile]
        ReconcileState(rd);
    }

    // Метод, который выполняет предсказанное движение на основе ввода игрока.
    [Replicate]
    private void Replicate(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        // Вычисляем направление движения
        Vector3 move = new Vector3(md.Horizontal, 0f, md.Vertical).normalized;
        // Применяем движение
        _cc.Move(move * _speed * (float)base.TimeManager.TickDelta);
    }

    // Метод, который "сверяет" и "исправляет" позицию, если она разошлась с серверной.
    [Reconcile]
    private void ReconcileState(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        // Просто устанавливаем позицию, которую прислал сервер.
        // Благодаря предсказанию, игрок почти никогда не увидит "дерганья", 
        // так как его собственные предсказания были точны.
        transform.position = rd.Position;
    }
}