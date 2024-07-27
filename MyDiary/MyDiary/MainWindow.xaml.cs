using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyDiary.db;
using MyDiary.models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyDiary
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<Record> TreeViewItems { get; set; }
        private string _currentDatabasePath;
        private DBHelper _dbHelper;
        private SettingsWindow _settingsWindow;
        //text flags
        private bool isBold = false;
        private bool isItalic = false;
        private bool isUnder = false;
        public MainWindow()
        {
            this.InitializeComponent();
        }
        private void LoadTreeViewItems()
        {
            if (_dbHelper == null) return;
            TreeViewItems = new ObservableCollection<Record>(_dbHelper.GetRecords());
            treeDates.ItemsSource = TreeViewItems;
        }

        private async void BtnOpenD_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add(".db");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _currentDatabasePath = file.Path;
                _dbHelper = new DBHelper(_currentDatabasePath);
                this.Title = "My Diary - " + file.Name;
                LoadTreeViewItems();
                btnDeleteD.IsEnabled = true;
                btnSaveC.IsEnabled = true;
            }
        }

        private async void BtnSaveC_Click(object sender, RoutedEventArgs e)
        {
            if (_dbHelper == null) return;
            DateTime dateTime = DateTime.Now;
            var date = dateTime.ToString("dd/MM/yyyy");
            var rtfStream = new InMemoryRandomAccessStream();
            txtContent.Document.SaveToStream(TextGetOptions.FormatRtf, rtfStream);
            using (var dataReader = new DataReader(rtfStream.GetInputStreamAt(0)))
            {
                await dataReader.LoadAsync((uint)rtfStream.Size);
                string rtfContent = dataReader.ReadString((uint)rtfStream.Size);
                var record = new Record { Date = date, Content = rtfContent };
                _dbHelper.UpsertRecord(record);
                LoadTreeViewItems();
            }
        }

        private async void BtnNewD_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeChoices.Add("Database", [".db"]);
            picker.DefaultFileExtension = ".db";
            picker.SuggestedFileName = "NewDatabase";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                _currentDatabasePath = file.Path;
                _dbHelper = new DBHelper(_currentDatabasePath);
                _dbHelper.CreateDB();
                LoadTreeViewItems();
                this.Title = "My Diary - " + file.Name;
                btnDeleteD.IsEnabled = true;
                btnSaveC.IsEnabled = true;
            }
        }

        private async void BtnDeleteD_Click(object sender, RoutedEventArgs e)
        {
            if (_dbHelper == null)
            {
                await ShowErrorDialogAsync("NO DB HELPER");
                return;
            }
            if (_currentDatabasePath == null)
            {
                await ShowErrorDialogAsync("NO PATH");
                return;
            }
            try
            {
                if (!File.Exists(_currentDatabasePath))
                {
                    await ShowErrorDialogAsync("THE FILE DOES NOT EXIST");
                    return;
                }
                _dbHelper?.DispatchDB();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(_currentDatabasePath);
                btnDeleteD.IsEnabled = false;
                btnSaveC.IsEnabled = false;
                _currentDatabasePath = null;
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync(ex.Message);
            }
        }
        private async Task ShowErrorDialogAsync(string message)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var dialog = new MessageDialog(message, "Error");
            InitializeWithWindow.Initialize(dialog, hwnd);
            await dialog.ShowAsync();
        }
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Activate();
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            T parentT = parent as T;
            if (parentT != null) return parentT;
            else return FindParent<T>(parent);
        }
        private async void ContentPresenter_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentPresenter clickedPresenter = sender as ContentPresenter;
            if (clickedPresenter != null)
            {
                var parentGrid = FindParent<Grid>(clickedPresenter);
                if (parentGrid != null && parentGrid.DataContext is Record clickedRecord)
                {
                    var rtfStream = new InMemoryRandomAccessStream();
                    using (var dataWriter = new DataWriter(rtfStream))
                    {
                        dataWriter.WriteString(clickedRecord.Content);
                        await dataWriter.StoreAsync();
                        rtfStream.Seek(0);
                        txtContent.Document.LoadFromStream(TextSetOptions.FormatRtf, rtfStream);
                    }
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton.DataContext is Record clickedRecord)
            {
                _dbHelper.DeleteRecord(clickedRecord.Date);
                TreeViewItems.Remove(clickedRecord);
                txtContent.Document.SetText(TextSetOptions.FormatRtf, "");
            }
        }

        private async void ApplyCharacterFormatting()
        {
            var selection = txtContent.Document.Selection;
            if (selection != null)
            {
                selection.CharacterFormat.Bold = isBold ? FormatEffect.On : FormatEffect.Off;
                selection.CharacterFormat.Italic = isItalic ? FormatEffect.On : FormatEffect.Off;
                selection.CharacterFormat.Underline = isUnder ? UnderlineType.Single : UnderlineType.None;
                var cbbitem = cbbFont.SelectedItem as ComboBoxItem;
                selection.CharacterFormat.Name = cbbitem.Content.ToString();
                var cbbitemsize = cbbSize.SelectedItem as ComboBoxItem;
                try
                {
                    int size = Int32.Parse(cbbitemsize.Content.ToString());
                    selection.CharacterFormat.Size = size;
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("1. " + ex.Message);
                }
            }
        }

        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            isItalic = !isItalic;
            ApplyCharacterFormatting();
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            isBold = !isBold;
            ApplyCharacterFormatting();
        }

        private void BtnUnder_Click(object sender, RoutedEventArgs e)
        {
            isUnder = !isUnder;
            ApplyCharacterFormatting();
        }

        private void CbbFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fonts = sender as ComboBox;
            var selectedItem = fonts.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                var selection = txtContent?.Document?.Selection;
                if (selection != null)
                {
                    selection.CharacterFormat.Name = selectedItem.Content.ToString();
                }
                txtContent?.Focus(FocusState.Pointer);
            }
        }

        private async void CbbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sizes = sender as ComboBox;
            var selectedItem = sizes.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                var selection = txtContent?.Document?.Selection;
                if (selection != null)
                {
                    try
                    {
                        selection.CharacterFormat.Size = Int32.Parse(selectedItem.Content.ToString());
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorDialogAsync("2. " + ex.Message);
                    }
                }
                txtContent?.Focus(FocusState.Pointer);
            }
        }
    }
}
