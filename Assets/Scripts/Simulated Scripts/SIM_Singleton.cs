using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// UNITY ONLY!!!
    /// Used for singleton instance access
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SIM_Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        public static T instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
                _instance = this as T;
            else if (_instance != this)
                Destroy(gameObject);
        }
    }
}
