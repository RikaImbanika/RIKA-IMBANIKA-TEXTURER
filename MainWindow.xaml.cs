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

namespace RIKA_TEXTURER;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += ScaleContent;

        string noTexPath = $"{Disk._programFiles}NoTex.jpg";
        var noTex = new BitmapImage();
        noTex.BeginInit();
        noTex.UriSource = new Uri(noTexPath, UriKind.Absolute);
        noTex.CacheOption = BitmapCacheOption.OnLoad;
        noTex.EndInit();

        img.Source = noTex;
        texPreview.Source = noTex;
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

        // Автоматический подбор размеров окна
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
        Texturer.Do(int.Parse(((ComboBoxItem)Resolution.SelectedItem).Content.ToString()));
    }
    private void OpenTextureClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

        if (openFileDialog.ShowDialog() == true)
        {
            Texturer._img = new BitmapImage();
            Texturer._img.BeginInit();
            Texturer._img.UriSource = new Uri(openFileDialog.FileName);
            Texturer._img.EndInit();

            img.Source = Texturer._img;
            texPreview.Source = Texturer._img;
        }
    }
}