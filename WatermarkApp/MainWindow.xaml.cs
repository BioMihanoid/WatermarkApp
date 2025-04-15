using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatermarkApp
{
    public partial class MainWindow : Window
    {
        private BitmapImage originalImage;
        private BitmapImage watermarkImage;

        public MainWindow()
        {
            InitializeComponent();
            DropImageArea.AllowDrop = true;
            DropImageArea.Drop += DropImageArea_Drop;
        }

        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg" };
            if (dialog.ShowDialog() == true)
            {
                originalImage = new BitmapImage(new Uri(dialog.FileName));
                PreviewImage.Source = originalImage;
            }
        }

        private void BtnLoadWatermark_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg" };
            if (dialog.ShowDialog() == true)
            {
                watermarkImage = new BitmapImage(new Uri(dialog.FileName));
            }
        }

        private void BtnUpdatePreview_Click(object sender, RoutedEventArgs e)
        {
            if (originalImage == null) return;
            var preview = ApplyWatermark(originalImage);
            PreviewImage.Source = preview;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewImage.Source is RenderTargetBitmap bmp)
            {
                var dialog = new SaveFileDialog { Filter = "PNG Image|*.png" };
                if (dialog.ShowDialog() == true)
                {
                    using var fileStream = new FileStream(dialog.FileName, FileMode.Create);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Save(fileStream);
                }
            }
        }

        private void BtnBatchProcess_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() != true)
                return;

            string folderPath = dialog.SelectedPath;
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg") || f.EndsWith(".bmp"))
                .ToList();

            foreach (var file in files)
            {
                var img = new BitmapImage(new Uri(file));
                var processed = ApplyWatermark(img);

                string outputPath = Path.Combine(folderPath, "processed");
                Directory.CreateDirectory(outputPath);
                string fileName = Path.GetFileNameWithoutExtension(file) + "_watermarked.png";
                string savePath = Path.Combine(outputPath, fileName);

                using var fileStream = new FileStream(savePath, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(processed));
                encoder.Save(fileStream);
            }

            MessageBox.Show("Пакетная обработка завершена.");
        }

        private RenderTargetBitmap ApplyWatermark(BitmapImage source)
        {
            int xCount = (int)SliderX.Value;
            int yCount = (int)SliderY.Value;
            double scale = SliderScale.Value / 100.0;
            double angle = SliderAngle.Value;

            var visual = new DrawingVisual();
            using var dc = visual.RenderOpen();

            dc.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));

            double wmWidth, wmHeight;
            ImageBrush wmBrush = null;

            if (UseTextCheckBox.IsChecked == true || watermarkImage == null)
            {
                var ft = new FormattedText(
                    WatermarkTextBox.Text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    24,
                    Brushes.White,
                    1.25);
                wmWidth = ft.Width * scale;
                wmHeight = ft.Height * scale;

                var vb = new DrawingVisual();
                using (var ctx = vb.RenderOpen())
                {
                    ctx.PushTransform(new ScaleTransform(scale, scale));
                    ctx.DrawText(ft, new Point(0, 0));
                }
                var bmp = new RenderTargetBitmap((int)wmWidth, (int)wmHeight, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(vb);
                wmBrush = new ImageBrush(bmp);
            }
            else
            {
                wmWidth = watermarkImage.PixelWidth * scale;
                wmHeight = watermarkImage.PixelHeight * scale;
                wmBrush = new ImageBrush(watermarkImage);
            }

            wmBrush.Opacity = 0.5;
            wmBrush.TileMode = TileMode.None;
            wmBrush.Stretch = Stretch.Fill;

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    double posX = x * source.PixelWidth / xCount;
                    double posY = y * source.PixelHeight / yCount;
                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new RotateTransform(angle, 
                        posX + wmWidth / 2, posY + wmHeight / 2));
                    dc.PushTransform(transformGroup);
                    dc.DrawRectangle(wmBrush, null, new Rect(posX, posY, wmWidth, wmHeight));
                    dc.Pop();
                }
            }

            dc.Close();

            var rtb = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, 
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            return rtb;
        }

        private void DropImageArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    originalImage = new BitmapImage(new Uri(files[0]));
                    PreviewImage.Source = originalImage;
                }
            }
        }
    }
}
