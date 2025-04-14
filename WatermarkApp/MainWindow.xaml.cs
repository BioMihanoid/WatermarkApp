using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WatermarkApp
{
    public partial class MainWindow : Window
    {
        private Bitmap mainImage;
        private Bitmap watermarkImage;

        public MainWindow()
        {
            InitializeComponent();
            PreviewImage.AllowDrop = true;
            PreviewImage.Drop += PreviewImage_Drop;
        }

        private void PreviewImage_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    mainImage = new Bitmap(files[0]);
                    UpdatePreview();
                }
            }
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                mainImage = new Bitmap(dialog.FileName);
                UpdatePreview();
            }
        }

        private void LoadWatermark_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                watermarkImage = new Bitmap(dialog.FileName);
                UpdatePreview();
            }
        }

        private void UpdatePreview_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void SaveResult_Click(object sender, RoutedEventArgs e)
        {
            if (mainImage == null) return;

            Bitmap result = GenerateWatermarkedImage();
            SaveFileDialog dialog = new SaveFileDialog { Filter = "JPEG Image|*.jpg" };
            if (dialog.ShowDialog() == true)
            {
                result.Save(dialog.FileName, ImageFormat.Jpeg);
                MessageBox.Show("Сохранено!");
            }
        }

        private void UpdatePreview()
        {
            if (mainImage == null) return;

            Bitmap bmp = GenerateWatermarkedImage();
            using (MemoryStream memory = new MemoryStream())
            {
                bmp.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                PreviewImage.Source = bitmapImage;
            }
        }

        private Bitmap GenerateWatermarkedImage()
        {
            Bitmap result = new Bitmap(mainImage);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int countX = (int)CountXSlider.Value;
                int countY = (int)CountYSlider.Value;
                float scale = (float)(SizeSlider.Value / 100.0);
                float angle = (float)RotationSlider.Value;

                for (int i = 0; i < countX; i++)
                {
                    for (int j = 0; j < countY; j++)
                    {
                        float x = result.Width * (i + 0.5f) / countX;
                        float y = result.Height * (j + 0.5f) / countY;

                        g.TranslateTransform(x, y);
                        g.RotateTransform(angle);

                        if (UseTextCheckBox.IsChecked == true || watermarkImage == null)
                        {
                            using (Font font = new Font("Arial", 20 * scale))
                            using (Brush brush = new SolidBrush(Color.FromArgb(100, Color.White)))
                            {
                                SizeF textSize = g.MeasureString(WatermarkTextBox.Text, font);
                                g.DrawString(WatermarkTextBox.Text, font, brush, -textSize.Width / 2, -textSize.Height / 2);
                            }
                        }
                        else
                        {
                            int wmWidth = (int)(watermarkImage.Width * scale);
                            int wmHeight = (int)(watermarkImage.Height * scale);
                            Rectangle rect = new Rectangle(-wmWidth / 2, -wmHeight / 2, wmWidth, wmHeight);
                            g.DrawImage(watermarkImage, rect);
                        }

                        g.ResetTransform();
                    }
                }
            }
            return result;
        }
    }
}