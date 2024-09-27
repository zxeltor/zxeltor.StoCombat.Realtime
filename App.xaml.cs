using System.Configuration;
using System.Data;
using System.Windows;
using zxeltor.Types.Lib.Helpers;

namespace zxeltor.StoCombat.Realtime
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Overrides of Application

        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            var runningInstancesCount = ProcessHelper.RunningProcessInstanceCount(AssemblyInfoHelper.GetApplicationNameFromAssemblyOrDefault());

            if (runningInstancesCount > 1)
            {
                MessageBox.Show("An instance of this application is already running.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        #endregion
    }

}
