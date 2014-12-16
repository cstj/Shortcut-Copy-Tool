using System.Windows;
using GalaSoft.MvvmLight.Threading;

namespace Shortcut_Copy_Tool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }
    }
}
