using UnityEngine;

public abstract class PoolMember : MonoBehaviour
{
    public Transform Tf { get; private set; }

    // Thêm dòng này: Để lưu lại gốc gác của Object
    public PoolMember OriginalPrefab { get; set; }

    protected virtual void Awake()
    {
        Tf = transform;
    }

    public virtual void OnSpawn() { }
    public virtual void OnDespawn() { }
}