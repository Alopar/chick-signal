namespace LudumDare.Template.Gameplay.Signal
{
    /// <summary>
    /// Если в сцене есть <see cref="SignalIntroCutsceneController"/>, автозагрузка SIGNAL создаётся неактивной,
    /// пока вводная сцена не завершится.
    /// </summary>
    public static class SignalGameplayBootstrapDefer
    {
        public static bool DeferGameplayStartRequested { get; private set; }

        public static void RequestDeferGameplayStart()
        {
            DeferGameplayStartRequested = true;
        }

        public static void ClearDeferRequest()
        {
            DeferGameplayStartRequested = false;
        }
    }
}
