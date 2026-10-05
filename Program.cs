using CovolSplitter.WinForms.Services;
using System.Threading;

namespace CovolSplitter.WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new ThreadExceptionEventHandler((sender, args) =>
            {
                ExceptionLogger.LogException(args.Exception);
                MessageBox.Show("Ocurrió un error inesperado: " + args.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    ExceptionLogger.LogException(ex);
                }
            });

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}