using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging.Filters;
using AForge.Vision.Motion;
using System.Data;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;

namespace CameraUP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isStartButtonClicked;

        FilterInfoCollection videoDevices;
        VideoCaptureDevice videoSource;
        public MainWindow()
        {
            InitializeComponent();

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo device in videoDevices)
            {
                comboBoxCameraDevices.Items.Add(device.Name);
            }

            comboBoxCameraDevices.SelectedIndex = 0;

            videoSource = new VideoCaptureDevice();

            isStartButtonClicked = false;

            void OnClosing(){
                if (videoSource.IsRunning)
                {
                    videoSource.Stop();
                }
            }
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            if (isStartButtonClicked)
            {
                buttonStart.Content = "Start Video";
                isStartButtonClicked = !isStartButtonClicked;

                imageVideo.Source = null;
                videoSource.Stop();

            }
            else
            {
                buttonStart.Content = "Stop Video";
                isStartButtonClicked = !isStartButtonClicked;

                videoSource = new VideoCaptureDevice(videoDevices[comboBoxCameraDevices.SelectedIndex].MonikerString);
                
                videoSource.NewFrame += VideoSource_NewFrame;
                videoSource.Start();
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap image = (Bitmap)eventArgs.Frame.Clone();
            CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            using (Image<Bgr, Byte> imageFrame = (Image<Bgr, Byte>)eventArgs.Frame.Clone())
            {
                if (imageFrame != null)
                {
                    var grayframe = imageFrame.Convert<Gray, Byte>();
                    var faces = cascadeClassifier.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty);
                    foreach (var face in faces)
                    {
                        imageFrame.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);

                    }
                }
                image = imageFrame.Bitmap;
            }
            this.Dispatcher.Invoke(() =>
            {
                imageVideo.Source = bitmapToImageSource(image);
            });
        }

        BitmapImage bitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void buttonSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (videoSource.IsRunning)
            {
                try
                {
                    saveToPng(imageVideo, "snapshot.png");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Video Source error", MessageBoxButton.OK, MessageBoxImage.Error);
                };
            }
        }

        void saveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            saveUsingEncoder(visual, fileName, encoder);
        }

        void saveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }
    }
}
