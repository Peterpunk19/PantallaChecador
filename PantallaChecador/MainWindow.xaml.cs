using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Emgu.CV.Structure;
using Emgu.CV;
using MySqlX.XDevAPI;
using NAudio.Wave;
using PantallaChecador.Modelos;
using PantallaChecador.Servicios;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Drawing.Imaging;
using System.Timers;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;
using Emgu.CV.Features2D;
using System.Threading;
using System.Drawing.Printing;
using Newtonsoft.Json;
using DPFP.Capture;
using DPFP.Processing;
using DPUruNet;
using DPFeatureExtraction = DPFP.Processing.FeatureExtraction;
using UruFeatureExtraction = DPUruNet.FeatureExtraction;

namespace PantallaChecador
{
    delegate void Function();

    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, DPFP.Capture.EventHandler
    {
        private DPFP.Verification.Verification Verificator;
        private DPFP.Capture.Capture Capturer;

        private VideoCaptureDevice videoSource;
        private System.Timers.Timer captureTimer;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
        }

        protected virtual void Init(string serialNumber)
        {

            // 7ddb7c55-34e1-2f4a-9f06-0f2a3a98eb8d 
            // d2d5f261-deb4-8b44-b263-4cea4d6fc67f

            try
            {

                Capturer = new DPFP.Capture.Capture();              // Create a capture operation.

                if (Capturer != null)
                {
                    Capturer.EventHandler = this;
                    Capturer.StartCapture();
                    lblReport.Content = "Lector listo para escanear.";
                }
                else
                {
                    SetStatusLabel("Can't initiate capture operation!");
                }
            }
            catch
            {
                MessageBox.Show("Can't initiate capture operation!", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            Verificator = new DPFP.Verification.Verification();


        }
        protected void Process(DPFP.Sample Sample)
        {
            lblReport.Content = "Huella leida.";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // Process the sample and create a feature set for the enrollment purpose.
            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);

            // Check quality of the sample and start verification if it's good
            // TODO: move to a separate task
            if (features != null)
            {
                // Compare the feature set with our template
                DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();

                DPFP.Template template = new DPFP.Template();
                Stream stream;

                int pageNumber = 1;
                int pageSize = 500; // Número de empleados por página
                bool found = false;

                while (!found)
                {
                    List<Employee> empleados = DatoEmpleado.MuestraEmpleados(txtAssistanceCode.Text, pageNumber, pageSize);
                    
                    if (empleados.Count == 0)
                    {
                        break; // Salir del bucle si no hay más empleados
                    }

                    foreach (var empleado in empleados)
                    {
                        if (empleado.fingerprint_thumb != null)
                        {
                            stream = new MemoryStream(empleado.fingerprint_thumb);
                            template = new DPFP.Template(stream);

                            Verificator.Verify(features, template, ref result);
                            if (result.Verified)
                            {
                                this.Dispatcher.Invoke(new Function(delegate ()
                                {
                                    Desplegar(empleado, empleado.id_employee_fingerprint_thumb);
                                }));
                                found = true;
                                break;
                            }
                        }
                        
                        if (empleado.fingerprint_index != null)
                        {
                            stream = new MemoryStream(empleado.fingerprint_index);
                            template = new DPFP.Template(stream);

                            Verificator.Verify(features, template, ref result);
                            if (result.Verified)
                            {
                                this.Dispatcher.Invoke(new Function(delegate ()
                                {
                                    Desplegar(empleado, empleado.id_employee_fingerprint_index);
                                }));
                                found = true;
                                break;
                            }
                        }
                    }
                    pageNumber++; // Ir a la siguiente página
                }

                if (!found)
                {
                    this.Dispatcher.Invoke(new Function(delegate ()
                    {
                        ShowAlert("No se encontró la huella", "error");
                    }));
                    TakePhoto();
                    Alarm();
                }
                else
                {
                    Success();
                }
                
            }

            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;

            // Format the elapsed time and display it
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            System.Diagnostics.Debug.WriteLine("RunTime " + elapsedTime);
        }

        protected void Alarm()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory; // Get the application's base directory            
            string filePath = Path.Combine(basePath, "Resources", "alarm.wav");

            SoundPlayer player = new SoundPlayer(filePath);
            player.Load();
            player.PlaySync();
        }

        protected void Success()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory; // Get the application's base directory            
            string filePath = Path.Combine(basePath, "Resources", "success.wav");

            SoundPlayer player = new SoundPlayer(filePath);
            player.Load();
            player.PlaySync();
        }

        protected void TakePhoto()
        {
            try
            {
                lblWebcam.Content = "";

                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    lblWebcam.Content = "No se encontró ninguna cámara conectada.";

                    return;
                }
                else
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                    videoSource.Start();

                    captureTimer = new System.Timers.Timer(1500); // 5000 ms = 5 seconds
                    captureTimer.Elapsed += CaptureTimerElapsed;
                    captureTimer.Start();
                }


            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(new Function(delegate ()
                {
                    lblWebcam.Content = $"Error al iniciar la captura de video: {ex.Message}";
                }));
                
            }
        }

        private void CaptureTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the video source and timer
            captureTimer.Stop();
            videoSource.SignalToStop();
            videoSource.WaitForStop();
        }

        private async void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Stop the event handler from being triggered again
            videoSource.NewFrame -= new NewFrameEventHandler(video_NewFrame);

            // Process the frame
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            BitmapImage bitmapImage = BitmapToBitmapImage(bitmap);
            string base64Image = BitmapImageToBase64(bitmapImage);

            // Upload the image
            await UploadImageAsync(base64Image);

        }

        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        public static string BitmapImageToBase64(BitmapImage bitmapImage)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }

        public static async Task UploadImageAsync(string base64Image)
        {
            string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            try
            {

                using (var client = new HttpClient())
                {
                    var jsonContent = new
                    {
                        imageBase64 = base64Image,
                        filename = $"error_{date}.jpg",
                        checkin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonContent);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(Config.Env.GetApiUrl("prod") + "registry-error", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Image uploaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to upload image. Status code: {response.StatusCode}");
                        var responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response: {responseBody}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request error: {ex.Message}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        protected void Start()
        {
            if (null != Capturer)
            {
                try
                {
                    Capturer.StartCapture();
                    lblReport.Content = "Lector listo para escanear.";
                }
                catch
                {
                    lblReport.Content = "Can't initiate capture!";
                }
            }
        }

        protected void Stop()
        {
            if (null != Capturer)
            {
                try
                {
                    Capturer.StopCapture();
                }
                catch
                {
                    lblReport.Content = "Can't terminate capture!";
                }
            }
        }

        private void txtAssistanceCode_LostFocus(object sender, RoutedEventArgs e)
        {
            // Set focus back to txtAssistanceCode
            txtAssistanceCode.Focus();
        }

        private void txtAssistanceCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numeric input
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void txtAssistanceCode_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextNumeric(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+"); // Regex that matches non-numeric text
            return !regex.IsMatch(text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentMode = FingerprintMode.DPFP; // o FingerprintMode.DPUruNet según el lector conectado
            InitFingerprint("");
            txtAssistanceCode.Focus();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Stop();
        }

        protected DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
        {
            DPFP.Processing.FeatureExtraction Extractor = new DPFP.Processing.FeatureExtraction();  // Create a feature extractor
            DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
            DPFP.FeatureSet features = new DPFP.FeatureSet();
            Extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);            // TODO: return features as a result?
            if (feedback == DPFP.Capture.CaptureFeedback.Good)
                return features;
            else
                return null;
        }


        public void Desplegar(Employee employee, int id_fingerprint_thumb)
        {
            lblMessage.Visibility = Visibility.Hidden;
            lblEmployeeName.Visibility = Visibility.Visible;
            lblHora.Content = DateTime.Now.ToString("hh:mm");

            lblFirstName.Content = $"{employee.name}";
            lblLastName.Content = $"{employee.paternal_last_name} {employee.maternal_last_name}";
            lblArea.Content = $"Área: {(string)employee.area_display_name}";

            string imageUrl = Config.Env.GetApiUrl("prod") + "employees/picture/" + employee.picture;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageUrl);
            bitmap.EndInit();

            imgPerfil.Source = bitmap;

            Registry registry = new Registry();
            registry.fk_employee = employee.id_employee;
            registry.fk_employee_fingerprint = id_fingerprint_thumb;
            registry.checkin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            int checkin = DatoEmpleado.SaveEmployeeFingerprint(registry);
            lblActive.Content = employee.status_display_name;

            DatoEmpleado.UpdateEmployee(employee.id_employee);
            ShowAlert("Huella encontrada", "success");
        }

        public void Reset()
        {
            lblFirstName.Content = "";
            lblLastName.Content = "";
            lblArea.Content = "";
            lblHora.Content = "";
            lblActive.Content = "";
            txtAssistanceCode.Text = "";
            lblStatus.Content = "";
            lblMessage.Content = "Coloca tu dedo en el lector";
            lblMessage.Visibility = Visibility.Visible;
            lblEmployeeName.Visibility = Visibility.Hidden;
            lblAssistanceCode.Visibility = Visibility.Visible;
            txtAssistanceCode.Visibility = Visibility.Visible;
            ShowAlert("", "null");
            alertContainer.Visibility = Visibility.Hidden;

            imgPerfil.Source = ImgSource("huella-dactilar2.jpg");

        }

        private BitmapImage ImgSource(string img)
        {
            string imageUrl = Config.Env.GetApiUrl("prod") + "img/" + img;
            Debug.WriteLine(imageUrl);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(imageUrl);
            bitmapImage.EndInit();

            return bitmapImage;
        }


        #region EventHandler Members:

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            // Create a TaskCompletionSource to wait for the first Dispatcher.Invoke to complete
            var tcs = new TaskCompletionSource<bool>();

            this.Dispatcher.Invoke(new Function(delegate ()
            {
                lblAssistanceCode.Visibility = Visibility.Hidden;
                txtAssistanceCode.Visibility = Visibility.Hidden;

                lblReport.Content = "Huella leida.";
                lblStatus.Content = "Espere por favor.";

                Process(Sample);

                // Set the TaskCompletionSource to completed
                tcs.SetResult(true);
            }));

            // Wait for the Dispatcher.Invoke to complete
            tcs.Task.Wait();

            // Proceed with the rest of the code
            Thread.Sleep(2000);

            this.Dispatcher.Invoke(new Function(delegate ()
            {
                Reset();
            }));
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {

            this.Dispatcher.Invoke(new Function(delegate ()
            {
                lblReport.Content = "El dedo fue retirado del lector de huellas.";
                System.Diagnostics.Debug.WriteLine(ReaderSerialNumber);

            }));
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {

            this.Dispatcher.Invoke(new Function(delegate ()
            {
                lblMessage.Content = "Autenticando huella.";
                lblReport.Content = "El lector de huellas fue tocado.";
            }));
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {

            this.Dispatcher.Invoke(new Function(delegate ()
            {
                lblReport.Content = "El lector de huellas está conectado.";
            }));
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {

            this.Dispatcher.Invoke(new Function(delegate ()
            {
                lblReport.Content = "El lector de huellas está desconectado.";
            }));
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {
            /*
            if (CaptureFeedback == DPFP.Capture.CaptureFeedback.Good)
                MakeReport("The quality of the fingerprint sample is good.");
            else
                MakeReport("The quality of the fingerprint sample is poor.");*/
        }
        #endregion

        private void SetStatusLabel(string message)
        {
            lblReport.Content = message;
        }

        private void ShowAlert(string message, string alertType)
        {
            switch (alertType)
            {
                case "success":
                    alertContainer.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF00C851"));
                    break;
                case "error":
                    alertContainer.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFF4444"));
                    break;
                case "warning":
                    alertContainer.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFBB33"));
                    break;
                default:
                    alertContainer.Background = System.Windows.Media.Brushes.Yellow;
                    break;
            }

            alertContainer.Visibility = Visibility.Visible;
            alertTextBlock.Text = message;
        }


        private enum FingerprintMode
        {
            DPFP,
            DPUruNet
        }

        private FingerprintMode currentMode = FingerprintMode.DPUruNet;

        // --- Para DPUruNet ---
        private Reader currentReader;
        private Dictionary<int, Fmd> fmds = new Dictionary<int, Fmd>();

        private void InitFingerprint(string serialNumber)
        {
            InitDPUruNet();
        }

        private async void InitDPUruNet()
        {
            try
            {
                var readers = ReaderCollection.GetReaders();
                if (readers == null || readers.Count == 0)
                {
                    lblReport.Content = "No se encontraron lectores DPUruNet.";
                    return;
                }

                currentReader = readers[0];

                var result = currentReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    lblReport.Content = $"Error al abrir lector: {result}";
                    return;
                }

                currentReader.On_Captured += new Reader.CaptureCallback(OnCapturedDPUruNet);
                StartCaptureDPUruNet();
                lblReport.Content = "DPUruNet listo para escanear.";
            }
            catch (Exception ex)
            {
                lblReport.Content = $"Error: {ex.Message}";
            }
        }

        private void StartCaptureDPUruNet()
        {
            if (currentReader != null)
            {
                currentReader.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, currentReader.Capabilities.Resolutions[0]);
            }
        }

        private void OnCapturedDPUruNet(CaptureResult captureResult)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (captureResult.Data == null || captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    lblReport.Content = $"Error en captura: {captureResult.ResultCode}";
                    return;
                }

                try
                {
                    var result = DPUruNet.FeatureExtraction.CreateFmdFromFid(
                        captureResult.Data, Constants.Formats.Fmd.ANSI);

                    if (result.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        lblReport.Content = $"Error creando FMD: {result.ResultCode}";
                        return;
                    }

                    Fmd capturedFmd = result.Data;
                    BuscarEmpleadoPorFmd(capturedFmd);
                }
                catch (Exception ex)
                {
                    lblReport.Content = $"Error al procesar huella: {ex.Message}";
                }
            });
        }
        private void BuscarEmpleadoPorFmd(Fmd capturedFmd)
        {
            int pageNumber = 1;
            int pageSize = 500;
            bool found = false;

            while (!found)
            {
                List<Employee> empleados = DatoEmpleado.MuestraEmpleados(txtAssistanceCode.Text, pageNumber, pageSize);
                if (empleados.Count == 0)
                    break;

                foreach (var empleado in empleados)
                {
                    foreach (var fingerData in new[] { empleado.fingerprint_thumb, empleado.fingerprint_index })
                    {
                        if (fingerData != null)
                        {
                            Fmd templateFmd = Fmd.DeserializeFmd(fingerData);

                            var result = Comparison.Compare(capturedFmd, 0, templateFmd, 0);
                            if (result.Score < 30000)
                            {
                                Desplegar(empleado, empleado.id_employee);
                                Success();
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found) break;
                }

                pageNumber++;
            }

            if (!found)
            {
                ShowAlert("No se encontró la huella", "error");
                TakePhoto();
                Alarm();
            }
        }

    }
}
