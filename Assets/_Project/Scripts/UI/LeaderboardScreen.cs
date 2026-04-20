using System.Collections.Generic;
using System.Linq;
using LudumDare.Template.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.Template.UI
{
    public class LeaderboardScreen : UIScreen
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField] private TextMeshProUGUI _rowPrefab;

        private readonly List<GameObject> _spawnedRows = new();
        private bool _backListenerAdded;
        private const int MaxNameLength = 18;

        public void SetRuntimeRefs(Button back, TMP_Text status, RectTransform rowsRoot, TextMeshProUGUI rowPrefab)
        {
            _backButton = back;
            _statusLabel = status;
            _rowsRoot = rowsRoot;
            _rowPrefab = rowPrefab;
            TryAddBackListener();
        }

        protected override void Awake()
        {
            base.Awake();
            TryAddBackListener();
        }

        private void TryAddBackListener()
        {
            if (_backListenerAdded || _backButton == null) return;
            _backListenerAdded = true;
            _backButton.onClick.AddListener(OnBack);
        }

        protected override void OnShow()
        {
            Refresh();
        }

        private void OnBack()
        {
            if (UIManager.HasInstance) UIManager.Instance.Pop();
        }

        private void Refresh()
        {
            ClearRows();
            if (_statusLabel != null) _statusLabel.text = "Loading...";

            if (!LeaderboardClient.HasInstance)
            {
                if (_statusLabel != null) _statusLabel.text = "Service unavailable.";
                return;
            }

            LeaderboardClient.Instance.FetchLeaderboard(OnFetched);
        }

        private void OnFetched(IReadOnlyList<LeaderboardEntry> entries)
        {
            ClearRows();

            if (entries == null || entries.Count == 0)
            {
                if (_statusLabel != null) _statusLabel.text = "No entries or network error.";
                return;
            }

            if (_statusLabel != null) _statusLabel.text = string.Empty;

            var sorted = entries.OrderByDescending(e => e.Score).ToList();
            for (var i = 0; i < sorted.Count; i++)
            {
                var e = sorted[i];
                var line = BuildRowText(i + 1, e.PlayerName, e.Score);
                AddRow(line);
            }
        }

        private static string BuildRowText(int rank, string playerName, int score)
        {
            var safeName = string.IsNullOrWhiteSpace(playerName) ? "Unknown" : playerName.Trim();
            if (safeName.Length > MaxNameLength)
            {
                safeName = safeName.Substring(0, MaxNameLength - 1) + ".";
            }

            return $"{rank}. {safeName} : {score}";
        }

        private void AddRow(string text)
        {
            if (_rowsRoot == null || _rowPrefab == null) return;

            var row = Instantiate(_rowPrefab, _rowsRoot);
            row.gameObject.SetActive(true);
            row.text = text;
            _spawnedRows.Add(row.gameObject);
        }

        private void ClearRows()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null) Destroy(_spawnedRows[i]);
            }

            _spawnedRows.Clear();
        }
    }
}
