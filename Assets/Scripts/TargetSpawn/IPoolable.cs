public interface IPoolable
{
    // プールから取得されたときに呼ばれる
    void OnSpawn();
    // プールに返却されたときに呼ばれる
    void OnDespawn(DespawnReason reason);
}