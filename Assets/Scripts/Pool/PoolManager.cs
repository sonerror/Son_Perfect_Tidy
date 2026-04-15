using System.Collections.Generic;
using UnityEngine;

// Định nghĩa lại các loại object của bạn
public enum PoolType
{
    Wood,
    Brick,
    Stone,
    Bullet,
    VFX
}

public class PoolManager : Singleton<PoolManager>
{
    [System.Serializable]
    public struct PoolConfig
    {
        public PoolType type;       // Thêm lại PoolType để làm thẻ tên tra cứu
        public PoolMember prefab;
        public int initialSize;
    }

    [SerializeField] private PoolConfig[] configs;

    // Các Dictionary cốt lõi
    private readonly Dictionary<PoolMember, Queue<PoolMember>> _pools = new();
    private readonly Dictionary<PoolMember, Transform> _parents = new();

    // TỪ ĐIỂN TRA CỨU: Giúp dịch từ PoolType -> Prefab tương ứng
    private readonly Dictionary<PoolType, PoolMember> _typeToPrefab = new();

    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    private void Init()
    {
        foreach (var cfg in configs)
        {
            if (cfg.prefab == null)
            {
                Debug.LogError($"[PoolManager] Prefab của loại {cfg.type} bị trống — đã bỏ qua.");
                continue;
            }

            // Ghi nhớ cặp Type -> Prefab vào từ điển tra cứu
            _typeToPrefab[cfg.type] = cfg.prefab;

            _pools[cfg.prefab] = new Queue<PoolMember>(cfg.initialSize);
            _parents[cfg.prefab] = CreateParent(cfg.prefab);

            for (int i = 0; i < cfg.initialSize; i++)
            {
                _pools[cfg.prefab].Enqueue(CreateInstance(cfg.prefab));
            }
        }
    }

    // ==========================================
    // CÁC HÀM SPAWN THEO LOẠI (POOLTYPE)
    // ==========================================

    /// <summary>
    /// Spawn 3D Object bằng PoolType (trả về PoolMember mặc định)
    /// </summary>
    public PoolMember Spawn(PoolType type, Vector3 pos, Quaternion rot)
    {
        if (_typeToPrefab.TryGetValue(type, out var prefab))
        {
            return Spawn(prefab, pos, rot);
        }

        Debug.LogError($"[PoolManager] Không tìm thấy Prefab nào được cấu hình cho loại: {type}");
        return null;
    }

    /// <summary>
    /// Spawn 3D Object bằng PoolType (ép kiểu Generic tự động)
    /// </summary>
    public T Spawn<T>(PoolType type, Vector3 pos, Quaternion rot) where T : PoolMember
    {
        return Spawn(type, pos, rot) as T;
    }

    /// <summary>
    /// Spawn 2D Object bằng PoolType
    /// </summary>
    public T Spawn<T>(PoolType type, Vector2 pos) where T : PoolMember
    {
        return Spawn(type, pos, Quaternion.identity) as T;
    }

    // ==========================================
    // CÁC HÀM SPAWN GỐC (DÙNG PREFAB)
    // ==========================================

    public PoolMember Spawn(PoolMember prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        if (!_pools.TryGetValue(prefab, out var queue))
        {
            _pools[prefab] = new Queue<PoolMember>();
            _parents[prefab] = CreateParent(prefab);
            queue = _pools[prefab];
        }

        PoolMember member = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);

        member.Tf.SetPositionAndRotation(pos, rot);
        member.gameObject.SetActive(true);
        member.OnSpawn();

        return member;
    }

    public T Spawn<T>(T prefab, Vector3 pos, Quaternion rot) where T : PoolMember
    {
        return Spawn((PoolMember)prefab, pos, rot) as T;
    }

    // ==========================================
    // CÁC HÀM DESPAWN
    // ==========================================

    public void Despawn(PoolMember member)
    {
        if (member == null) return;

        member.OnDespawn();
        member.gameObject.SetActive(false);

        if (member.OriginalPrefab != null && _pools.TryGetValue(member.OriginalPrefab, out var queue))
        {
            queue.Enqueue(member);
        }
        else
        {
            Debug.LogWarning($"[PoolManager] Không tìm thấy Pool của '{member.name}' — đã tiến hành Destroy.");
            Destroy(member.gameObject);
        }
    }

    public void DespawnDelayed(PoolMember member, float delay)
        => StartCoroutine(DespawnRoutine(member, delay));

    private PoolMember CreateInstance(PoolMember prefab)
    {
        var instance = Instantiate(prefab, _parents[prefab]);
        instance.OriginalPrefab = prefab;
        instance.gameObject.SetActive(false);
        return instance;
    }

    private Transform CreateParent(PoolMember prefab)
    {
        var go = new GameObject($"Pool_{prefab.name}");
        go.transform.SetParent(transform);
        return go.transform;
    }

    private System.Collections.IEnumerator DespawnRoutine(PoolMember member, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (member != null && member.gameObject.activeSelf)
            Despawn(member);
    }
}