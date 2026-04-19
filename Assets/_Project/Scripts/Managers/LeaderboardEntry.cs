namespace LudumDare.Template.Managers
{
    public readonly struct LeaderboardEntry
    {
        public readonly string PlayerName;
        public readonly int Score;
        public readonly string RecordedAt;

        public LeaderboardEntry(string playerName, int score, string recordedAt)
        {
            PlayerName = playerName;
            Score = score;
            RecordedAt = recordedAt ?? string.Empty;
        }
    }
}
