using System;
using AudioProcessing;
using Chromatic_Tuner.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Render;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Media;

namespace Chromatic_Tuner.Views
{

    public sealed partial class MainPage : Page
    {
        // Declaring the media audio necessary variables

        private AudioGraph graph;
        private AudioDeviceInputNode deviceInputNode;
        private AudioFrameOutputNode frameOutputNode;

        // Declare the custom classes
        private FFT2 freqAnalyzer = new FFT2();
        private AudioMemoryBuffer audioProcessing = new AudioMemoryBuffer();

        // Use an IList to store the points
        private List<float> pointsf = new List<float>();

        // Timer that would update the Oscilloscope
        private DispatcherTimer timer = new DispatcherTimer();
        
        // Sizes of the oscilloscope
        private int OSC_START_Y = 200;
        private int OSC_HEIGHT = 200;
        private int OSC_LENGTH = 1500;

        // Audio properties
        private int desiredSamples = 4096;
        private float uppercut = 10000;
        private float lowercut = 50;
        private float quantumDuration;
        private float uppercutPoint;
        private float lowercutPoint;
        private uint sizePowerOf2Log;
        private uint sizePowerOf2Int;
        private int size;
        private bool ready = true;

        // For testing
        private float sampleRate = 48000;
        private float frequency;

        // Main function
        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
        }
        
        // Strongly-typed view models enable x:bind
        public MainPageViewModel ViewModel => (DataContext as MainPageViewModel);

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await CreateAudioGraph();
            InitializeTimer();

            // Make sure that there are at least two points in the storage array
            pointsf.Add(0);
            pointsf.Add(0);

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (graph != null)
            {
                graph.Dispose();
            }
        }

        // Turn the AudioGraph on or off depending on the state of the button
        private void RecordStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (recordStopButton.Content.Equals("Play"))
            {
                graph.Start();
                recordStopButton.Content = "Stop";
                timer.Start();
            }
            else if (recordStopButton.Content.Equals("Stop"))
            {
                // Good idea to stop the graph to avoid data loss
                graph.Stop();
                recordStopButton.Content = "Play";
                timer.Stop();
            }
        }

        // Timer initialization
        private void InitializeTimer()
        {
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += updateGraph;
        }

        // Create the AudioGraph
        private async Task CreateAudioGraph()
        {
            // Create a new AudioGraph settings object to store the options, here you can play with latence/output device etc
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.SoundEffects);
            settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            settings.DesiredSamplesPerQuantum = desiredSamples;
            settings.DesiredRenderDeviceAudioProcessing = Windows.Media.AudioProcessing.Default;
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                ShowErrorMessage(string.Format("AudioGraph Creation Error because {0}", result.Status.ToString()));
                return;
            }
            graph = result.Graph;


            // Create a device input node using the default audio input device
            CreateAudioDeviceInputNodeResult deviceInputNodeResult = await graph.CreateDeviceInputNodeAsync(MediaCategory.Other);

            if (deviceInputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device input node
                ShowErrorMessage(string.Format("Audio Device Input unavailable because {0}", deviceInputNodeResult.Status.ToString()));
                return;
            }
            deviceInputNode = deviceInputNodeResult.DeviceInputNode;

            
            // Create the output node to send to data processing and add the event handler for when a quantum is processed
            frameOutputNode = graph.CreateFrameOutputNode();
            graph.QuantumProcessed += AudioGraph_QuantumProcessed;


            // Link the nodes together
            deviceInputNode.AddOutgoingConnection(frameOutputNode);


            // Because we are using lowest latency setting, we need to handle device disconnection errors
            graph.UnrecoverableErrorOccurred += Graph_UnrecoverableErrorOccurred;
        }


        private async void Graph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            // Recreate the graph and all nodes when this happens
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                sender.Dispose();
            });
        }


        // Get the buffer whenever it is processed
        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            if (Math.IEEERemainder(graph.CompletedQuantumCount,10) == 0)
            {
                IList<float> points = new List<float>();

                // Get the required properties and fill points array
                sampleRate = graph.EncodingProperties.SampleRate;
                AudioFrame frame = frameOutputNode.GetFrame();

                // Get the buffer into the point collection
                try
                {
                    points = audioProcessing.ProcessFrameOutput(frame);
                }
                catch (Exception e)
                {
                    ShowErrorMessage(e.Message);
                }

                size = points.Count();

                if (size != 0)
                {
                    sizePowerOf2Log = GetNextPowerOf2((uint)size);
                    quantumDuration = (float)Math.Pow(2, sizePowerOf2Log) / sampleRate;
                    uppercutPoint = (float)Math.Round(uppercut * quantumDuration);
                    lowercutPoint = (float)Math.Round(lowercut * quantumDuration);

                    size = (int)Math.Pow(2, sizePowerOf2Log);

                    while (points.Count() < size)
                    {
                        points.Add(0);
                    }
                    
                    // Perform FFT on quantum
                    points = freqAnalyzer.Run(points);

                    //Remove upper and lower limits
                    for (int i = 0; i < (uppercutPoint - lowercutPoint); i++)
                    {
                        points[i] = points[i + (int)lowercutPoint];
                    }
                    while (points.Count() > uppercutPoint - lowercutPoint)
                    {
                        points.RemoveAt(points.Count() - 1);
                    }

                    // Normalize the points
                    float mh = points.Max();
                    float mg = points.Min();

                    if ((mh - mg) / size > 0.001)
                    {
                        for (int i = 0; i < points.Count; i++)
                        {
                            points[i] = points[i] * size / (mh * 2);
                        }
                    }

                    // Store results
                    if(ready)
                    {
                        pointsf.Clear();
                        pointsf.AddRange(points);
                    }
                }
            }
        }


        private void testFFT_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();

            // Generate the points array
            IList<float> points = new List<float>();

            // Get input data
            frequency = float.Parse(frequencyBox.Text);
            size = int.Parse(samplesBox.Text);

            // Calculate variables
            sizePowerOf2Log = GetNextPowerOf2((uint)size);
            sizePowerOf2Int = (uint)Math.Pow(2, sizePowerOf2Log);
            quantumDuration = (float)Math.Pow(2, sizePowerOf2Log) / sampleRate;
            uppercutPoint = (float)Math.Round(uppercut * quantumDuration);
            lowercutPoint = (float)Math.Round(lowercut * quantumDuration);

            // Generate control
            for (int i = 0; i < size; i++)
            {
                //// Square wave
                //if(Math.Sin(2 * Math.PI * i * frequency / sampleRate) < 0)
                //{
                //    points.Add(-1);
                //}
                //else
                //{
                //    points.Add(1);
                //}

                // Sine wave
                points.Add((float)Math.Sin(2 * Math.PI * frequency * i / sampleRate));
            }

            // Pad with zeroes
            for (int i = points.Count(); i < sizePowerOf2Int; i++)
            {
                points.Add(0);
            }

            // Perform FFT on quantum
            points = freqAnalyzer.Run(points);

            //Remove upper and lower limits
            for (int i = 0; i < (uppercutPoint - lowercutPoint); i++)
            {
                points[i] = points[i + (int)lowercutPoint];
            }
            while (points.Count() > uppercutPoint - lowercutPoint)
            {
                points.RemoveAt(points.Count() - 1);
            }

            // Normalize the points
            float mh = points.Max();
            float mg = points.Min();

            if ((mh - mg) / size > 0.001)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = points[i] * size / (mh * 2);
                }
            }

            // Store results
            if (ready)
            {
                pointsf.Clear();
                pointsf.AddRange(points);
            }
        }


        // Function to write a collection of points to the line visualiser
        private async void updateGraph(object sender, object e)
        {
            //Random r = new Random();
            //pointsf.Clear();

            //for (int i = 0; i < 1000; i++)
            //{
            //    pointsf.Add(r.Next(100) / 50);
            //}

            ready = false;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {

                int oscSize = Oscilloscope.Points.Count();
                int colSize = pointsf.Count();

                for (int i = 0; i < colSize; i++)
                {
                    Point p;

                    if (i == 0)
                    {
                        p.X = 0;
                    }
                    else
                    {
                        // Arrange x's with regards to the logarithm
                        p.X = (Math.Log(i + 1) / Math.Log(colSize)) * OSC_LENGTH;
                    }

                    p.Y = OSC_START_Y - (OSC_HEIGHT * 2 * pointsf[i] / Math.Pow(2, sizePowerOf2Log));

                    if (i < oscSize)
                    {
                        Oscilloscope.Points[i] = p;
                    }
                    else
                    {
                        Oscilloscope.Points.Add(p);
                    }
                }
            });
            ready = true;
        }


        private void ShowErrorMessage(string v)
        {
            //StartText.Text = v;
        }

        private uint GetNextPowerOf2(uint p)
        {
            uint power = 0;

            while (Math.Pow(2, power) < p)
            {
                power++;
            };

            return power;
        }

    }
}
