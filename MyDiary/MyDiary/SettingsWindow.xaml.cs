using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyDiary
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            this.InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {

            AppInstance.Restart("Restart");
            /*var color = colorPicker.Color.ToString();
            await ShowErrorDialogAsync(color);*/
        }
        private async Task ShowErrorDialogAsync(string message)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var dialog = new MessageDialog(message, "Error");
            InitializeWithWindow.Initialize(dialog, hwnd);
            await dialog.ShowAsync();
        }

        private void CbbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void CbbSize_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            var combobox = sender as ComboBox;
            if (combobox == null) { return; }
            var text = combobox.Text;
            if (!Regex.IsMatch(text, @"^[1-9]\d*$"))
            {
                lblSize.Content = "Font Size: not num- " + text;
                //ShowErrorDialogAsync("Is not num: " + text);
            }
            else lblSize.Content = "Font Size:";
        }
    }
}
