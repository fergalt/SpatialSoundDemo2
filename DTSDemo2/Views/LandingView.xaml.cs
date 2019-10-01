using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Media.MediaProperties;
using System.Numerics;
using Windows.Storage;
using Windows.Storage.Pickers;
using DTSDemo2.Interfaces;

namespace DTSDemo2.Views
{
    /// <summary>
    /// Interaction logic for LandingView.xaml
    /// </summary>
    public partial class LandingView : UserControl, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public double x;
        public double y;
        public double z;

        public bool startButtonEnabled;
        public bool stopButtonEnabled;

        public LandingView()
        {
            InitializeComponent();
            this.DataContext = this;
            x = 0;
            y = 0;
            z = 0;
            StartButtonEnabled = false;
            StopButtonEnabled = false;
        }

        /// <summary>
        /// X position in metres
        /// </summary>
        public double X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
                OnPropertyChanged("X");
            }
        }

        /// <summary>
        /// Y position in metres
        /// </summary>
        public double Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
                OnPropertyChanged("Y");
            }
        }

        /// <summary>
        /// Z position in metres
        /// </summary>
        public double Z
        {
            get
            {
                return z;
            }
            set
            {
                z = value;
                OnPropertyChanged("Z");
            }
        }

        /// <summary>
        /// Start Button Enabled
        /// </summary>
        public bool StartButtonEnabled
        {
            get
            {
                return startButtonEnabled;
            }
            set
            {
                startButtonEnabled = value;
                OnPropertyChanged("StartButtonEnabled");
            }
        }

        /// <summary>
        /// Stop Button Enabled
        /// </summary>
        public bool StopButtonEnabled
        {
            get
            {
                return stopButtonEnabled;
            }
            set
            {
                stopButtonEnabled = value;
                OnPropertyChanged("StopButtonEnabled");
            }
        }


        /// <summary>
        /// Adds changed property to the event handler
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
                emitter.Position = new Vector3((float)X, (float)Z, (float)Y);
            }
        }

        /// <summary>
        /// Handles dragging of listener in view, updates XY metric values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseDragElementBehavior_Dragging(object sender, MouseEventArgs e)
        {
            Point PxPosition = Emitter.TranslatePoint(new Point(Emitter.Width / 2 - Listener.Width / 2, Emitter.Height / 2 - Listener.Height / 2), Listener);
            Point MetricPosition = new Point(PxPosition.X / Canvas.Width, PxPosition.Y / Canvas.Height);
            X = MetricPosition.X*3;
            Y = MetricPosition.Y*3;
        }

        private AudioDeviceOutputNode deviceOutput;
        private AudioGraph graph;
        private AudioFileInputNode currentAudioFileInputNode = null;
        private StorageFile soundFile;
        private AudioNodeEmitter emitter = new AudioNodeEmitter(AudioNodeEmitterShape.CreateOmnidirectional(), AudioNodeEmitterDecayModel.CreateCustom(0.1, 1), AudioNodeEmitterSettings.None);


        private async Task BuildAudioGraph()
        {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.GameEffects);
            settings.EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 32);
            settings.EncodingProperties.Subtype = MediaEncodingSubtypes.Float;
            settings.DesiredRenderDeviceAudioProcessing = Windows.Media.AudioProcessing.Raw;

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                MessageBox.Show(String.Format("AudioGraph creation error: {0}", result.Status.ToString()), "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            graph = result.Graph;
            CreateAudioDeviceOutputNodeResult deviceResult = await graph.CreateDeviceOutputNodeAsync();

            if (deviceResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                MessageBox.Show(String.Format("Audio device error: {0}", deviceResult.Status.ToString()), "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            deviceOutput = deviceResult.DeviceOutputNode;
            graph.UnrecoverableErrorOccurred += Graph_UnrecoverableErrorOccurred;
        }

        private async void Graph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            sender.Dispose();
            StartButtonEnabled = false;
            StopButtonEnabled = false;
            await BuildAudioGraph();
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            graph.Stop();
            StartButtonEnabled = true;
            StopButtonEnabled = false;
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            graph.Start();
            StartButtonEnabled = false;
            StopButtonEnabled = true;
        }


        private async void load_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".wav");
            ((IInitializeWithWindow)(object)openPicker).Initialize(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
            soundFile = await openPicker.PickSingleFileAsync();

            if (soundFile != null)
            {
                StartButtonEnabled = false;
                StopButtonEnabled = false;

                if (graph != null)
                {
                    graph.Stop();
                }
                else
                {
                    await BuildAudioGraph();
                }
                emitter.Position = new Vector3((float)X, (float)Y, (float)Z);

                CreateAudioFileInputNodeResult inCreateResult = await graph.CreateFileInputNodeAsync(soundFile, emitter);
                if (inCreateResult.Status != AudioFileNodeCreationStatus.Success)
                {
                    MessageBox.Show(String.Format("Audio file load error: {0}. {1}", inCreateResult.Status.ToString(), inCreateResult.ExtendedError.Message.ToString()), "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (currentAudioFileInputNode != null)
                {
                    currentAudioFileInputNode.Dispose();
                }
                currentAudioFileInputNode = inCreateResult.FileInputNode;
                currentAudioFileInputNode.AddOutgoingConnection(deviceOutput);
                StartButtonEnabled = true;
            }

        }
    }
}
