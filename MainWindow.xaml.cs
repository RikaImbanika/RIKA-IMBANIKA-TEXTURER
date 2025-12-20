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
        TexPreview.Source = noTex;
    }

    private void ScaleContent(object sender, RoutedEventArgs e)
    {
        // Учет DPI и масштабирования Windows
        PresentationSource src = PresentationSource.FromVisual(this);
        Matrix matrix = src.CompositionTarget.TransformToDevice;
        double dpiFactorX = matrix.M11;
        double dpiFactorY = matrix.M22;

        // Реальный доступный размер с учетом DPI
        double realScreenWidth = SystemParameters.WorkArea.Width / dpiFactorX;
        double realScreenHeight = SystemParameters.WorkArea.Height / dpiFactorY;

        // Расчёт масштаба для оригинальных 1280x720
        double scale = Math.Min(
            realScreenWidth / 1280,
            realScreenHeight / 720
        ) * 0.9;

        // Применяем трансформацию ко всему содержимому
        MainGrid.LayoutTransform = new ScaleTransform(scale, scale);

        // Автоматический подбор размеров окна
        this.SizeToContent = SizeToContent.WidthAndHeight;

        // Фиксация размеров после масштабирования
        this.Width = this.ActualWidth;
        this.Height = this.ActualHeight;

        // Центрирование
        this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
        this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
    }
}