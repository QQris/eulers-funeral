namespace EulersFuneral.Core.Data
{
    /// <summary>
    /// Mantiene el índice del nivel seleccionado al transicionar entre las escenas MainMenu y Gameplay.
    /// </summary>
    public static class GameSessionContext
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string GameplaySceneName = "Gameplay";

        public static int SelectedLevelIndex { get; set; } = 0;
    }
}
