using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LudumDare.Template.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// GET requests to Google Apps Script: submit score (name, score) and fetch JSON table data.
    /// </summary>
    public class LeaderboardClient : Singleton<LeaderboardClient>
    {
        private const int RedirectLimit = 32;

        [SerializeField] private LeaderboardConfig _config;

        public void SubmitScore(string playerName, int score)
        {
            if (_config == null || string.IsNullOrEmpty(_config.WebAppUrl))
            {
                Debug.LogWarning("[LeaderboardClient] LeaderboardConfig is not assigned.");
                return;
            }

            if (string.IsNullOrWhiteSpace(playerName)) return;

            StartCoroutine(SubmitRoutine(playerName.Trim(), score));
        }

        public void FetchLeaderboard(Action<IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            if (_config == null || string.IsNullOrEmpty(_config.WebAppUrl))
            {
                Debug.LogWarning("[LeaderboardClient] LeaderboardConfig is not assigned.");
                onComplete?.Invoke(Array.Empty<LeaderboardEntry>());
                return;
            }

            StartCoroutine(FetchRoutine(onComplete));
        }

        private static UnityWebRequest CreateGet(string url)
        {
            var req = UnityWebRequest.Get(url);
            req.redirectLimit = RedirectLimit;
            return req;
        }

        private IEnumerator SubmitRoutine(string playerName, int score)
        {
            var baseUrl = _config.WebAppUrl;
            var url =
                $"{baseUrl}?name={UnityWebRequest.EscapeURL(playerName)}&score={score}";

            using var req = CreateGet(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[LeaderboardClient] Failed to submit score: {req.error} (code {req.responseCode})");
                yield break;
            }

            var body = req.downloadHandler?.text ?? string.Empty;
            if (!body.Contains("Success", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[LeaderboardClient] Unexpected response: {body}");
            }
        }

        private IEnumerator FetchRoutine(Action<IReadOnlyList<LeaderboardEntry>> onComplete)
        {
            using var req = CreateGet(_config.WebAppUrl);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[LeaderboardClient] Failed to load leaderboard: {req.error} (code {req.responseCode})");
                onComplete?.Invoke(Array.Empty<LeaderboardEntry>());
                yield break;
            }

            var text = req.downloadHandler?.text ?? "[]";
            var list = ParseLeaderboardJson(text);
            onComplete?.Invoke(list);
        }

        /// <summary>
        /// Parses <c>JSON.stringify</c> output from Google Apps Script: array of string rows <c>[name, score, date]</c>.
        /// No external JSON libraries (WebGL / dependency constraints).
        /// </summary>
        private static List<LeaderboardEntry> ParseLeaderboardJson(string json)
        {
            var result = new List<LeaderboardEntry>();
            try
            {
                var s = json.Trim();
                var i = 0;
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != '[') return result;
                i++;

                while (true)
                {
                    SkipWs(s, ref i);
                    if (i >= s.Length) break;
                    if (s[i] == ']') break;
                    if (s[i] != '[')
                    {
                        i++;
                        continue;
                    }

                    i++;
                    var cells = new List<string>();
                    while (true)
                    {
                        SkipWs(s, ref i);
                        if (i < s.Length && s[i] == ']')
                        {
                            i++;
                            break;
                        }

                        cells.Add(ReadJsonValue(s, ref i));
                        SkipWs(s, ref i);
                        if (i < s.Length && s[i] == ',')
                        {
                            i++;
                            continue;
                        }

                        if (i < s.Length && s[i] == ']')
                        {
                            i++;
                            break;
                        }

                        break;
                    }

                    if (cells.Count >= 2)
                    {
                        var name = cells[0];
                        var score = ParseIntLoose(cells[1]);
                        var when = cells.Count > 2 ? cells[2] : string.Empty;
                        result.Add(new LeaderboardEntry(name, score, when));
                    }

                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == ',')
                    {
                        i++;
                        continue;
                    }

                    if (i < s.Length && s[i] == ']') break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardClient] JSON parse error: {e.Message}");
            }

            return result;
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static string ReadJsonValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) return string.Empty;
            if (s[i] == '"')
            {
                i++;
                var start = i;
                while (i < s.Length && s[i] != '"') i++;
                var text = start <= i ? s.Substring(start, i - start) : string.Empty;
                if (i < s.Length && s[i] == '"') i++;
                return text;
            }

            var n0 = i;
            while (i < s.Length && s[i] != ',' && s[i] != ']' && !char.IsWhiteSpace(s[i]))
            {
                i++;
            }

            return s.Substring(n0, i - n0).Trim();
        }

        private static int ParseIntLoose(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return 0;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                return n;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                return Mathf.RoundToInt(f);
            }

            return 0;
        }
    }
}
