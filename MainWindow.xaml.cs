using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace RIKA_IMBANIKA_TEXTURER;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += ScaleContent;
        WindowsManager._mainWindow = this;

        S.Init();

        string noTexPath = $"{S.PF}NoTex.jpg";
        var noTex = new BitmapImage();
        noTex.BeginInit();
        noTex.UriSource = new Uri(noTexPath, UriKind.Absolute);
        noTex.CacheOption = BitmapCacheOption.OnLoad;
        noTex.EndInit();

        img.Source = noTex;
        texPreview.Source = noTex;

        Title = $"{S.AppName} - {S.GetHello()}";
    }

    private void ScaleContent(object sender, RoutedEventArgs e)
    {
        PresentationSource src = PresentationSource.FromVisual(this);
        Matrix matrix = src.CompositionTarget.TransformToDevice;
        double dpiFactorX = matrix.M11;
        double dpiFactorY = matrix.M22;

        double realScreenWidth = SystemParameters.WorkArea.Width / dpiFactorX;
        double realScreenHeight = SystemParameters.WorkArea.Height / dpiFactorY;

        double scale = Math.Min(
            realScreenWidth / 1280,
            realScreenHeight / 720
        ) * 0.9;

        MainGrid.LayoutTransform = new ScaleTransform(scale, scale);

        // Autosomething
        this.SizeToContent = SizeToContent.WidthAndHeight;

        this.Width = this.ActualWidth;
        this.Height = this.ActualHeight;

        this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
        this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
    }

    private void OpenModelClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "OBJ files (*.obj)|*.obj";

        if (openFileDialog.ShowDialog() == true)
        {
            Texturer._obj = Obj.Parse(openFileDialog.FileName);
            Texturer._obj.Triangulate();
        }
    }

    private void DoClick(object sender, RoutedEventArgs e)
    {
        int texSize = int.Parse(((ComboBoxItem)Resolution.SelectedItem).Content.ToString());
        float scaler = float.Parse(Scale.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        int startCount = (int)float.Parse(StartCount.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
        string order = ((ComboBoxItem)FillOrder.SelectedItem).Content.ToString();

        if (Texturer._obj != null && Texturer._tex != null)
        {
            Texturer.Do(texSize, scaler, startCount, order);
        }
        else
            MessageBox.Show("Please, select .obj first.");
    }

    private void OpenTextureClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

        if (openFileDialog.ShowDialog() == true)
        {
            Texturer._tex = new BitmapImage();
            Texturer._tex.BeginInit();
            Texturer._tex.UriSource = new Uri(openFileDialog.FileName);
            Texturer._tex.EndInit();

            img.Source = Texturer._tex;
            texPreview.Source = Texturer._tex;
        }
    }

    private void SmoothClick(object sender, RoutedEventArgs e)
    {
        Texturer.Smooth();
    }
}