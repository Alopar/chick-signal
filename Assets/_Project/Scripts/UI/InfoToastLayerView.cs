using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Template.UI
{
    public sealed class InfoToastLayerView : MonoBehaviour
    {
        [SerializeField] private InfoToastItemView _toastPrefab;
        [SerializeField] private RectTransform _spawnRoot;
        [SerializeField] private float _spacingY = 58f;
        [SerializeField] private float _showInterval = 0.2f;
        [SerializeField] private int _maxQueueSize = 12;

        private readonly Queue<string> _queue = new();
        private Coroutine _dequeueRoutine;
        private int _spawnedCount;

        public void Show(string message)
        {
            if (_toastPrefab == null || string.IsNullOrWhiteSpace(message))
                return;

            if (_queue.Count >= _maxQueueSize)
                _queue.Dequeue();

            _queue.Enqueue(message.Trim());
            if (_dequeueRoutine == null)
                _dequeueRoutine = StartCoroutine(DequeueRoutine());
        }

        private IEnumerator DequeueRoutine()
        {
            var wait = new WaitForSecondsRealtime(Mathf.Max(0.01f, _showInterval));

            while (_queue.Count > 0)
            {
                var item = Instantiate(_toastPrefab, _spawnRoot != null ? _spawnRoot : transform);
                if (item.TryGetComponent(out RectTransform itemRect))
                {
                    float yOffset = -Mathf.Min(2, _spawnedCount) * _spacingY;
                    itemRect.anchoredPosition = new Vector2(0f, yOffset);
                }

                item.SetupAndPlay(_queue.Dequeue());
                _spawnedCount = (_spawnedCount + 1) % 3;
                yield return wait;
            }

            _dequeueRoutine = null;
        }
    }
}
