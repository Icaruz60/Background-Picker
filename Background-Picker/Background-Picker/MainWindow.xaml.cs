using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CredentialManagement;
using Functions;
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

        setImagePath();

        var cred = new Credential { Target = "Background-Picker/OpenAI" };
        bool exists = cred.Load();
        if(!exists)
        {
            apiPopup.IsOpen = true;
            apiBox.Focus();
        }
        else
        {
            _apiKey = cred.Password;
        }
    }

    private void SubmitBtn_OnClick(object sender, RoutedEventArgs e)
    {
        string apiKey = apiBox.Password;
        if (!string.IsNullOrEmpty(apiKey))
        {
            var cred = new Credential { Target = "Background-Picker/OpenAI", Password = apiKey };
            cred.Save();
            _apiKey = cred.Password;
            apiPopup.IsOpen = false;
        }
    }

    private void setApiKey_OnClick(object sender, RoutedEventArgs e)
    {
        apiPopup.IsOpen = true;
        apiBox.Focus();
    }

    private void setFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        dialog.InitialDirectory = _bgImagePath;
        if (dialog.ShowDialog() == true)
        {
            _bgImagePath = dialog.FolderName;
        }
        setImagePath(_bgImagePath);
        CreateBackgroundDirectory();
    }

    private async void SendBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputBox.Text))
        {
            SendBtn.IsEnabled = false;
            await RequestSender.SendRequestAsync(InputBox.Text, _apiKey, Image1Btn, Image2Btn, Image3Btn);
            SendBtn.IsEnabled = true;
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
            loadingPopup.IsOpen = true;
            await PreprocessingPipeline.ProcessImagesAsync(files, _apiKey, loadingBar, percentageText);
            loadingPopup.IsOpen = false;
        }
    }

    private void UploadImageBtn_OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //To be convenient, only check first file for correct extension
            if (files.Length > 0 && System.IO.Path.GetExtension(files[0]).ToLower() == SupportedExtension)
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
            if (System.IO.Path.GetExtension(files[i]).ToLower() == SupportedExtension)
            {
                filterFiles.Add(filePath);
                Debug.WriteLine("Taking in: " + filePath);
            }

        }
        string[] checkedFiles = filterFiles.ToArray();
        loadingPopup.IsOpen = true;
        await PreprocessingPipeline.ProcessImagesAsync(files, _apiKey, loadingBar, percentageText);
        loadingPopup.IsOpen = false;
    }
    private void InputBox_TextChanged(object sender, RoutedEventArgs e)
    {

    }

    private void Image1Btn_OnClick(object sender, RoutedEventArgs e)
    {
        string from = (string)((Button)sender).Tag;
        string to = Path.Combine(_bgImagePath, "bg.png");
        ChangeBackground(from, to);
    }

    private void Image2Btn_OnClick(object sender, RoutedEventArgs e)
    {
        string from = (string)((Button)sender).Tag;
        string to = Path.Combine(_bgImagePath, "bg.png");
        ChangeBackground(from, to);
    }

    private void Image3Btn_OnClick(object sender, RoutedEventArgs e)
    {
        string from = (string)((Button)sender).Tag;
        string to = Path.Combine(_bgImagePath, "bg.png");
        ChangeBackground(from, to);
    }

    internal void ChangeBackground(string from, string to)
    {
        System.IO.File.Copy(from, to, true);
    }

    internal void CreateBackgroundDirectory()
    {
        Directory.CreateDirectory(_bgImagePath);

        if (!File.Exists(Path.Combine(_bgImagePath, "bg.png")))
        {
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data" , "Images", "108.png"), Path.Combine(_bgImagePath, "bg.png"));
        }
    }

    internal void setImagePath(string path = null)
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
}