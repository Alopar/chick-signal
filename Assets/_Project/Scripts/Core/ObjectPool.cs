using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Template.Core
{
    /// <summary>
    /// Simple prefab-based pool. Create via <c>new ObjectPool(prefab, parent, prewarm)</c>.
    /// Call <see cref="Get"/> and <see cref="Release"/>; inactive items live under <c>parent</c>.
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _pool = new();

        public ObjectPool(GameObject prefab, Transform parent = null, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                var instance = Object.Instantiate(_prefab, _parent);
                instance.SetActive(false);
                _pool.Push(instance);
            }
        }

        public GameObject Get(Vector3 position = default, Quaternion rotation = default)
        {
            var go = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, _parent);
            go.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (_parent != null) instance.transform.SetParent(_parent, false);
            _pool.Push(instance);
        }
    }
}
