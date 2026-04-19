using UnityEngine;

namespace LudumDare.Template.Managers
{
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "LudumDare/Leaderboard Config", order = 0)]
    public class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("Base Google Apps Script web app URL (no query string), must end with /exec")]
        [SerializeField] private string _webAppUrl =
            "https://script.google.com/macros/s/AKfycbzypEA1-hD1x96bLtb_fPBxWonpI7vB88OuVHLQOpwfRlSxj_xRhAptYnYiuYffc7PDxg/exec";

        public string WebAppUrl => _webAppUrl?.Trim() ?? string.Empty;
    }
}
