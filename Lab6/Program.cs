using DataAccess;
using Presentation;
using Services;

namespace Lab6;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Lab6Tests.HospitalTests.Start();

            //var hospitalService = new HospitalService(new JsonHospitalDao());
            //Application.Run(new MainForm(hospitalService));
        }
        catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить приложение или загрузить данные.\n\n{ex.Message}", "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
