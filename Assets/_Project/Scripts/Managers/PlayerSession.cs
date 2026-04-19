using LudumDare.Template.Core;
using UnityEngine;

namespace LudumDare.Template.Managers
{
    /// <summary>
    /// Nickname for this session after choosing it in the menu; cleared when returning to the main menu.
    /// </summary>
    public class PlayerSession : Singleton<PlayerSession>
    {
        public string CurrentNickname { get; private set; } = string.Empty;

        public bool HasNickname => !string.IsNullOrEmpty(CurrentNickname);

        public void SetNickname(string nickname)
        {
            CurrentNickname = nickname?.Trim() ?? string.Empty;
        }

        public void ClearSession()
        {
            CurrentNickname = string.Empty;
        }
    }
}
