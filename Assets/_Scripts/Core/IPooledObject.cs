/// <summary>
/// An interface for objects that can be managed by the ObjectPooler.
/// Any script on a pooled object can implement this to reset its state when it's reused.
/// </summary>
public interface IPooledObject
{
    // This method is called by the ObjectPooler right after the object is activated.
    void OnObjectSpawn();
}