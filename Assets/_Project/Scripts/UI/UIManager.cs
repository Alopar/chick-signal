using System.Collections.Generic;
using LudumDare.Template.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LudumDare.Template.UI
{
    /// <summary>
    /// Holds the persistent UI Canvas and a stack of currently-open screens. Call <see cref="Push"/>/
    /// <see cref="Pop"/> to navigate; the bottom screen stays interactable if you want overlays.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private Canvas _rootCanvas;

        private readonly List<UIScreen> _registered = new();
        private readonly Stack<UIScreen> _stack = new();

        public Canvas RootCanvas => _rootCanvas;

        protected override void OnAwakeSingleton()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PurgeDead();
            if (mode == LoadSceneMode.Single)
            {
                _stack.Clear();
            }
        }

        public void Register(UIScreen screen)
        {
            if (screen == null) return;
            if (!_registered.Contains(screen)) _registered.Add(screen);
        }

        public void Unregister(UIScreen screen)
        {
            _registered.Remove(screen);
        }

        public T Find<T>() where T : UIScreen
        {
            for (int i = 0; i < _registered.Count; i++)
            {
                if (_registered[i] is T typed) return typed;
            }
            return null;
        }

        public void Push(UIScreen screen, bool hideCurrent = true)
        {
            if (screen == null) return;
            PurgeDead();
            if (hideCurrent && _stack.Count > 0)
            {
                var top = _stack.Peek();
                if (top != null) top.Hide();
            }
            _stack.Push(screen);
            screen.Show();
        }

        public void Pop()
        {
            PurgeDead();
            if (_stack.Count == 0) return;
            var top = _stack.Pop();
            if (top != null) top.Hide();
            if (_stack.Count > 0)
            {
                var next = _stack.Peek();
                if (next != null) next.Show();
            }
        }

        public void Replace(UIScreen screen)
        {
            while (_stack.Count > 0)
            {
                var s = _stack.Pop();
                if (s != null) s.Hide();
            }
            if (screen != null)
            {
                _stack.Push(screen);
                screen.Show();
            }
        }

        public void CloseAll()
        {
            while (_stack.Count > 0)
            {
                var s = _stack.Pop();
                if (s != null) s.Hide();
            }
        }

        private void PurgeDead()
        {
            _registered.RemoveAll(s => s == null);

            if (_stack.Count == 0) return;
            var tmp = new List<UIScreen>(_stack.Count);
            while (_stack.Count > 0) tmp.Add(_stack.Pop());
            for (int i = tmp.Count - 1; i >= 0; i--)
            {
                if (tmp[i] != null) _stack.Push(tmp[i]);
            }
        }
    }
}
