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

using Windows.Devices.Enumeration;

using Windows.Graphics.Imaging;

using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

namespace MediaCaptureNote
{

    public partial class MainWindow : Window
    {
        private MediaCapture _mediaCapture;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comboBox_Devices.Items.Clear();

            var cameraDevices = await GetCameraDevicesAsync();

            foreach (var device in cameraDevices)
            {
                comboBox_Devices.Items.Add(device.Name);
            }


            //await InitializeCameraAsync();
        }

        private async void ComboBox_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await InitializeCameraAsync();
        }

        public async Task InitializeCameraAsync()
        {
            var cameraDevices = await GetCameraDevicesAsync();

            foreach (var device in cameraDevices)
            {
                Console.WriteLine($"Device Name: {device.Name}, ID: {device.Id}");
            }

            var selectedDevice = cameraDevices[comboBox_Devices.SelectedIndex];

            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = selectedDevice.Id, 
                StreamingCaptureMode = StreamingCaptureMode.Video
            });

            _isInitialized = true;
        }

        private async Task CaptureFrameAsync()
        {
            var imgFormat = ImageEncodingProperties.CreateJpeg();
            var stream = new InMemoryRandomAccessStream();
            await _mediaCapture.CapturePhotoToStreamAsync(imgFormat, stream);

            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            cameraPreview.Source = ConvertToBitmapSource(softwareBitmap);
        }

        public static BitmapSource ConvertToBitmapSource(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap == null) return null;

            int width = softwareBitmap.PixelWidth;
            int height = softwareBitmap.PixelHeight;
            int stride = width * 4; // BGRA32

            byte[] buffer = new byte[height * stride];
            softwareBitmap.CopyToBuffer(buffer.AsBuffer());

            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, stride, 0);

            return bitmap;
        }

        public async Task StartPreviewAsync()
        {
            if (!_isInitialized) return;

            while (true)
            {
                var imgFormat = ImageEncodingProperties.CreateJpeg();
                var stream = new InMemoryRandomAccessStream();
                await _mediaCapture.CapturePhotoToStreamAsync(imgFormat, stream);

                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                cameraPreview.Source = ConvertToBitmapSource(softwareBitmap);

                await Task.Delay(100); 
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            await CaptureFrameAsync();
        }

        public async Task<List<DeviceInformation>> GetCameraDevicesAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return devices.ToList();
        }

    }

}
