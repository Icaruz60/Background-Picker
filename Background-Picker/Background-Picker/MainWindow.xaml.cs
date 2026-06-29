using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CredentialManagement;
using Functions;
using Helpers;
using Microsoft.Win32;

namespace Background_Picker;

public partial class MainWindow : Window
{
    private readonly string SupportedExtension = ".png";
    private string _apiKey;
    private string _bgImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"MeetingBackground");
    public MainWindow()
    {
        InitializeComponent();

        CreateBackgroundDirectory();

        SetImagePath();

        DataIntegrity.SyncImageData();

        LoadApiKey();
    }

    private void SubmitBtn_OnClick(object sender, RoutedEventArgs e)
    {
        string apiKey = apiBox.Password;
        if (!string.IsNullOrEmpty(apiKey))
        {
            var cred = new Credential { Target = "Background-Picker/OpenAI", Password = apiKey };
            cred.Save();
            _apiKey = cred.Password;
            apiOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseApiOverlay_OnClick(object sender, RoutedEventArgs e)
    {
        apiOverlay.Visibility = Visibility.Collapsed;
    }

    private void SetApiKey_OnClick(object sender, RoutedEventArgs e)
    {
        apiOverlay.Visibility = Visibility.Visible;
        apiBox.Focus();
    }

    private void SetFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        dialog.InitialDirectory = _bgImagePath;
        if (dialog.ShowDialog() == true)
        {
            _bgImagePath = dialog.FolderName;
        }
        SetImagePath(_bgImagePath);
        CreateBackgroundDirectory();
    }

    private async void SendBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputBox.Text))
        {
            SendBtn.IsEnabled = false;
            try
            {
                await ImageSearchService.SendRequestAsync(InputBox.Text, _apiKey, Image1Btn, Image2Btn, Image3Btn);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SendBtn.IsEnabled = true;
            }
        }
    }

    private async void UploadImageBtn_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = "Image Files (*.png)|*.png";
        ofd.Title = "Add Images";
        ofd.Multiselect = true;
        bool? success = ofd.ShowDialog();

        if (success == true)
        {
            string[] files = ofd.FileNames;
            loadingOverlay.Visibility = Visibility.Visible;
            try
            {
                await PreprocessingPipeline.ProcessImagesAsync(files, _apiKey, loadingBar, percentageText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void UploadImageBtn_OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //To be convenient, only check first file for correct extension
            if (files.Length > 0 && Path.GetExtension(files[0]).ToLower() == SupportedExtension)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void UploadImageBtn_OnDrop(object sender, DragEventArgs e)
    {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var filterFiles = new List<string>();
        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            if (Path.GetExtension(files[i]).ToLower() == SupportedExtension)
            {
                filterFiles.Add(filePath);
                Debug.WriteLine("Taking in: " + filePath);
            }

        }
        string[] checkedFiles = filterFiles.ToArray();
        loadingOverlay.Visibility = Visibility.Visible;
        try
        {
            await PreprocessingPipeline.ProcessImagesAsync(checkedFiles, _apiKey, loadingBar, percentageText);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Something went wrong: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            loadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ImageBtn_OnClick(object sender, RoutedEventArgs e)
    {
        string from = (string)((Button)sender).Tag;
        string to = Path.Combine(_bgImagePath, "bg.png");
        ChangeBackground(from, to);
    }

    private void ChangeBackground(string from, string to)
    {
        if(!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to))
        {
            File.Copy(from, to, true);
        }
    }

    private void CreateBackgroundDirectory()
    {
        Directory.CreateDirectory(_bgImagePath);

        if (!File.Exists(Path.Combine(_bgImagePath, "bg.png")))
        {
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", "108.png"), Path.Combine(_bgImagePath, "bg.png"));
        }
    }

    private void SetImagePath(string path = null)
    {
        if (path != null)
        {
            _bgImagePath = path;
        }
        else
        {
            var pathCred = new Credential { Target = "Background-Picker/BGPath" };
            _bgImagePath = pathCred.Load() ? pathCred.Password : _bgImagePath;
        }

        var saveCred = new Credential { Target = "Background-Picker/BGPath", Password = _bgImagePath };
        saveCred.Save();
    }

    private void LoadApiKey()
    {
        var cred = new Credential { Target = "Background-Picker/OpenAI" };
        bool exists = cred.Load();
        if (!exists)
        {
            apiOverlay.Visibility = Visibility.Visible;
            apiBox.Focus();
        }
        else
        {
            _apiKey = cred.Password;
        }
    }
}