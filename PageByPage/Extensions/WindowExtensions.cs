using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PageByPage.Extensions
{
    public static class WindowExtensions
    {
        public static void ShowSingle<TWindow>(this TWindow window) where TWindow : Window
        {
            var existingWindow = Application.Current.Windows.Cast<Window>()
                .FirstOrDefault(w => w is TWindow);

            if (existingWindow != null)
            {
                existingWindow.WindowState = WindowState.Normal;
                existingWindow.Focus();
            }
            else
            {
                window.Show();
            }
        }
    }
}
