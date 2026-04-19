using UnityEngine;

namespace LudumDare.Template.Core
{
    /// <summary>
    /// Persistent MonoBehaviour singleton. Attach to a prefab inside the Bootstrap scene; do not auto-create
    /// instances at runtime to keep scene composition explicit.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;

        public static T Instance => _instance;
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;

            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnAwakeSingleton();
        }

        protected virtual void OnAwakeSingleton() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
