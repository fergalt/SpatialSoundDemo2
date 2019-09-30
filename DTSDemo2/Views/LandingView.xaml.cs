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

        public LandingView()
        {
            InitializeComponent();
            this.DataContext = this;
            x = 0;
            y = 0;
            z = 0;
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

        private AudioDeviceOutputNode _deviceOutput;
        private AudioGraph _graph;
        private AudioFileInputNode _currentAudioFileInputNode = null;
        private StorageFile soundFile;
        private AudioNodeEmitter emitter = new AudioNodeEmitter(AudioNodeEmitterShape.CreateOmnidirectional(), AudioNodeEmitterDecayModel.CreateCustom(0.2, 1), AudioNodeEmitterSettings.None);

        private async Task BuildAndStartAudioGraph()
        {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.GameEffects);
            settings.EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 32);
            settings.EncodingProperties.Subtype = MediaEncodingSubtypes.Float;
            settings.DesiredRenderDeviceAudioProcessing = Windows.Media.AudioProcessing.Raw;

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status == AudioGraphCreationStatus.Success)
            {
                _graph = result.Graph;
                CreateAudioDeviceOutputNodeResult deviceResult = await _graph.CreateDeviceOutputNodeAsync();

                if (deviceResult.Status == AudioDeviceNodeCreationStatus.Success)
                {
                    _deviceOutput = deviceResult.DeviceOutputNode;

                    emitter.Position = new Vector3((float)X, (float)Y, (float)Z);

                    CreateAudioFileInputNodeResult inCreateResult = await _graph.CreateFileInputNodeAsync(soundFile, emitter);

                    _currentAudioFileInputNode = inCreateResult.FileInputNode;

                    _currentAudioFileInputNode.AddOutgoingConnection(_deviceOutput);

                    _graph.Start();
                }
            }
        }


        private void stop_Click(object sender, RoutedEventArgs e)
        {
            _graph.Stop();
        }

        private async void start_Click(object sender, RoutedEventArgs e)
        {


            await BuildAndStartAudioGraph();
        }

        private async void load_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".wav");
            ((IInitializeWithWindow)(object)openPicker).Initialize(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
            soundFile = await openPicker.PickSingleFileAsync();
        }
    }
}
