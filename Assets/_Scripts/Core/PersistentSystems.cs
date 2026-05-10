using UnityEngine;

/// <summary>
/// A simple singleton script whose only purpose is to make the GameObject it's attached to
/// (and all of its children) persist across all scene loads.
/// </summary>
public class PersistentSystems : MonoBehaviour
{
    // A static reference to the single instance of this class.
    private static PersistentSystems instance;

    [Header("Required Manager Prefabs (optional)")]
    [SerializeField] private AudioManager _audioManagerPrefab;
    [SerializeField] private UIManager _uiManagerPrefab;

    private AudioManager _audioManagerInstance;
    private UIManager _uiManagerInstance;

    private void Awake()
    {
        // Standard singleton pattern to ensure only one instance ever exists.
        if (instance == null)
        {
            instance = this;
            // This is the command that prevents the object from being destroyed on a new scene load.
            DontDestroyOnLoad(gameObject);
            if (!TryGetComponent<TimeScaleManager>(out _))
            {
                gameObject.AddComponent<TimeScaleManager>();
            }

            _audioManagerInstance = EnsureManagerInstance(_audioManagerPrefab);
            _uiManagerInstance = EnsureManagerInstance(_uiManagerPrefab);
        }
        else
        {
            // If another instance already exists, destroy this new one.
            Destroy(gameObject);
        }
    }

    private T EnsureManagerInstance<T>(T prefab) where T : MonoBehaviour
    {
        T existing = FindFirstObjectByType<T>();
        if (existing == null && prefab != null)
        {
            existing = Instantiate(prefab);
        }

        if (existing == null)
        {
            Debug.LogWarning($"PersistentSystems could not locate a {typeof(T).Name}. Assign a prefab or place one in the scene.");
            return null;
        }

        DontDestroyOnLoad(existing.gameObject);
        return existing;
    }
}

