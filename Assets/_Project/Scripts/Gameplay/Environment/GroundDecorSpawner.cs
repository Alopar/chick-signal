using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Template.Gameplay.Environment
{
    /// <summary>
    /// Spawns random decorative sprites around this object on a 2D plane.
    /// Intended for quick iteration in editor and optional runtime regeneration.
    /// </summary>
    public class GroundDecorSpawner : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private List<Sprite> _sprites = new();

        [Header("Distribution")]
        [Min(0f)]
        [SerializeField] private float _radius = 8f;
        [SerializeField] private bool _useDensity = true;
        [Min(0f)]
        [Tooltip("Decor elements per 1 world-unit area.")]
        [SerializeField] private float _densityPerUnit = 0.12f;
        [Min(0)]
        [SerializeField] private int _count = 20;

        [Header("Transform Randomization")]
        [SerializeField] private Vector2 _scaleRange = new(0.8f, 1.25f);
        [SerializeField] private Vector2 _zRotationRange = new(-8f, 8f);
        [SerializeField] private bool _randomFlipX = true;
        [SerializeField] private bool _randomFlipY = false;

        [Header("Generation")]
        [SerializeField] private bool _fixedSeed = false;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _clearBeforeGenerate = true;
        [SerializeField] private string _generatedContainerName = "_GeneratedDecor";

        [ContextMenu("Generate Decor")]
        public void GenerateDecor()
        {
            if (_sprites == null || _sprites.Count == 0)
            {
                Debug.LogWarning($"{nameof(GroundDecorSpawner)}: No sprites assigned.", this);
                return;
            }

            Transform container = GetOrCreateGeneratedContainer();

            if (_clearBeforeGenerate)
            {
                ClearDecor();
            }

            int spawnCount = CalculateSpawnCount();
            if (spawnCount <= 0) return;

            Random.State cachedRandomState = Random.state;
            if (_fixedSeed)
            {
                Random.InitState(_seed);
            }

            for (int i = 0; i < spawnCount; i++)
            {
                Sprite sprite = _sprites[Random.Range(0, _sprites.Count)];
                if (sprite == null) continue;

                GameObject decor = new($"Decor_{i:000}");
                decor.transform.SetParent(container, false);

                Vector2 randomPoint = Random.insideUnitCircle * _radius;
                decor.transform.localPosition = new Vector3(randomPoint.x, randomPoint.y, 0f);

                float zRotation = Random.Range(_zRotationRange.x, _zRotationRange.y);
                decor.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

                float scale = Random.Range(_scaleRange.x, _scaleRange.y);
                decor.transform.localScale = new Vector3(scale, scale, 1f);

                SpriteRenderer spriteRenderer = decor.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.flipX = _randomFlipX && Random.value > 0.5f;
                spriteRenderer.flipY = _randomFlipY && Random.value > 0.5f;
            }

            if (_fixedSeed)
            {
                Random.state = cachedRandomState;
            }
        }

        [ContextMenu("Clear Decor")]
        public void ClearDecor()
        {
            Transform container = transform.Find(_generatedContainerName);
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private int CalculateSpawnCount()
        {
            if (!_useDensity)
            {
                return Mathf.Max(0, _count);
            }

            float area = Mathf.PI * _radius * _radius;
            return Mathf.Max(0, Mathf.RoundToInt(area * _densityPerUnit));
        }

        private Transform GetOrCreateGeneratedContainer()
        {
            Transform existing = transform.Find(_generatedContainerName);
            if (existing != null) return existing;

            GameObject container = new(_generatedContainerName);
            container.transform.SetParent(transform, false);
            return container.transform;
        }

        private void OnValidate()
        {
            if (_scaleRange.x > _scaleRange.y)
            {
                (_scaleRange.x, _scaleRange.y) = (_scaleRange.y, _scaleRange.x);
            }

            if (_zRotationRange.x > _zRotationRange.y)
            {
                (_zRotationRange.x, _zRotationRange.y) = (_zRotationRange.y, _zRotationRange.x);
            }
        }
    }
}
