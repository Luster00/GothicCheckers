namespace GothicCheckers;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Инициализация стандартных параметров Windows Forms.
        ApplicationConfiguration.Initialize();

        // Создание и запуск главного игрового окна.
        Application.Run(new MainForm());
    }
}
