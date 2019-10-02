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

        private AudioDeviceOutputNode deviceOutput;
        private AudioGraph graph;
        private AudioFileInputNode currentAudioFileInputNode = null;
        private StorageFile soundFile;
        private AudioNodeEmitter emitter;

        /// <summary>
        /// Constructor
        /// </summary>
        public LandingView()
        {
            InitializeComponent();
            this.DataContext = this;
            StartButtonEnabled = false;
            StopButtonEnabled = false;
            UpdateXYPositions();
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
        /// Start Button Enabled flag
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
        /// Stop Button Enabled flag
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
                if (emitter != null)
                {
                    emitter.Position = new Vector3((float)X, (float)Z, (float)Y);
                }
            }
        }

        /// <summary>
        /// Handles dragging of listener in view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseDragElementBehavior_Dragging(object sender, MouseEventArgs e)
        {
            UpdateXYPositions();
        }

        /// <summary>
        /// Updates the XY metric values based on listener position in view
        /// </summary>
        private void UpdateXYPositions()
        {
            Point PxPosition = Emitter.TranslatePoint(new Point(Emitter.Width / 2 - Listener.Width / 2, Emitter.Height / 2 - Listener.Height / 2), Listener);
            Point MetricPosition = new Point(PxPosition.X / Canvas.Width, PxPosition.Y / Canvas.Height);
            X = MetricPosition.X * 3;
            Y = MetricPosition.Y * 3;
        }


        /// <summary>
        /// Builds audio graph and assignes output device
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Handles AudioGraph errors (e.g. device change) and attempts to restart playback from same position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void Graph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            TimeSpan CurrentPlayPosition = currentAudioFileInputNode != null ? currentAudioFileInputNode.Position : TimeSpan.Zero;
            sender.Dispose();
            StartButtonEnabled = false;
            StopButtonEnabled = false;
            await LoadAudioFile(CurrentPlayPosition);
        }

        /// <summary>
        /// Handles Stop button clicks. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// Handles Start button clicks. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        /// <summary>
        /// Handles Load button clicks. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".wav");

            // Link file picker to current window thread.
            ((IInitializeWithWindow)(object)openPicker).Initialize(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
            soundFile = await openPicker.PickSingleFileAsync();
            await LoadAudioFile(TimeSpan.Zero);
        }

        /// <summary>
        /// Stops playback and resets position to zero.
        /// </summary>
        private void Stop()
        {
            graph.Stop();
            currentAudioFileInputNode.Seek(TimeSpan.Zero);
            StartButtonEnabled = true;
            StopButtonEnabled = false;
        }


        /// <summary>
        /// Attempts to start playback. Reloads file and audiograph on failure.
        /// </summary>
        private async void Start()
        {
            try
            {
                graph.Start();
            }
            catch (Exception)
            {
                await LoadAudioFile(TimeSpan.Zero);
                try
                {
                    graph.Start();
                }
                catch
                {
                    return;
                }

            }
            StartButtonEnabled = false;
            StopButtonEnabled = true;
        }

        /// <summary>
        /// Loads audio file.
        /// </summary>
        /// <param name="PlayPosition"></param>
        /// <returns></returns>
        private async Task LoadAudioFile(TimeSpan PlayPosition)
        {
            if (soundFile != null)
            {
                StartButtonEnabled = false;
                StopButtonEnabled = false;

                await BuildAudioGraph();
                emitter = new AudioNodeEmitter(AudioNodeEmitterShape.CreateOmnidirectional(), AudioNodeEmitterDecayModel.CreateCustom(0.1, 1), AudioNodeEmitterSettings.None);
                emitter.Position = new Vector3((float)X, (float)Y, (float)Z);

                if (graph != null)
                {
                    CreateAudioFileInputNodeResult inCreateResult = await graph.CreateFileInputNodeAsync(soundFile, emitter);
                    if (inCreateResult.Status != AudioFileNodeCreationStatus.Success)
                    {
                        MessageBox.Show(String.Format("Audio file load error: {0}. {1}", inCreateResult.Status.ToString(), inCreateResult.ExtendedError.Message.ToString()), "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (currentAudioFileInputNode != null)
                    {
                        try
                        {
                            currentAudioFileInputNode.Dispose();
                        }
                        catch (ObjectDisposedException)
                        {

                        }
                    }
                    currentAudioFileInputNode = inCreateResult.FileInputNode;
                    currentAudioFileInputNode.AddOutgoingConnection(deviceOutput);

                    if (PlayPosition != TimeSpan.Zero)
                    {
                        currentAudioFileInputNode.Seek(PlayPosition);
                        graph.Start();
                        StopButtonEnabled = true;
                    }
                    else
                    {
                        StartButtonEnabled = true;
                    }
                }
            }

        }
    }
}
