using Microsoft.Win32;
using OposMMOP;
using OposScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OposGui
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[]? _images;

        private bool _paused = false;
        private bool _paused1 = false;

        MyScheduler myScheduler = new(2, 2);

        public static List<Task> tasksForExecution = new List<Task>();
        public static List<Task> allTasks = new List<Task>();
        public static List<Task> sharpenTasksForExecution = new List<Task>();
        public static List<Task> allSharpenTasks = new List<Task>();

        string? resource;
        public static StackPanel? ProgressBarsStackPanel1;
        ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
        public MainWindow()
        {

            InitializeComponent();
            PlaybackButton.Content = "Pause";
            PlaybackButton1.Content = "Pause";

        }
        async Task Function(CancellationTokenSource cts, TaskFactory factory, int parallelism, int totaltime)
        {
            CancellationToken cancellationToken = cts.Token;

            var progressBars = new ProgressBar[parallelism + 1];
            int rowsPerThread = 100000 / parallelism;
            Application.Current.Dispatcher.Invoke(() =>
            {


                for (int i = 0; i < parallelism; i++)
                {

                    progressBars[i] = new ProgressBar()
                    {
                        Maximum = 1.0,
                        Height = 18,
                        Value = 0.5,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    ProgressBarsStackPanelTask.Children.Add(progressBars[i]);
                }


                progressBars[parallelism] = new ProgressBar()
                {
                    Maximum = 1.0,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                ProgressBarsStackPanelTask.Children.Add(progressBars[parallelism]);

            });
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await factory.StartNew(() =>
            {
                _ = Parallel.For(1, 100000, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, (i, state) =>
                {
                    ManualResetEvent.WaitOne();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Trace.WriteLine("Task is canceled.");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            progressBars[parallelism].Value = 0.0;
                        });

                        state.Break();

                    }

                    if (stopwatch.ElapsedMilliseconds >= totaltime)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressBars[parallelism].Value = 0.0;
                            });
                            state.Break();
                        }
                        catch (OperationCanceledException)
                        {

                        }

                    }

                    Console.WriteLine($"Function {i} on thread {Environment.CurrentManagedThreadId}");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        double di = (double)i;
                        double drpt = (double)rowsPerThread;
                        progressBars[i / rowsPerThread].Value = (di - (i / rowsPerThread) * rowsPerThread) / drpt;
                        progressBars[parallelism].Value += 1.0 / (double)100000;

                    });
                    if (i == 100000 - 1)
                    {
                        cts.Cancel();
                    }
                });

            }, cancellationToken);

        }
        object locker = new object();
        private TimeSpan timeout = TimeSpan.FromMilliseconds(500);
        async Task FunctionWithResource(CancellationTokenSource cts, TaskFactory factory, int parallelism, string res, int totalTime)
        {
            CancellationToken cancellationToken = cts.Token;
            var progressBars = new ProgressBar[parallelism + 1];
            int rowsPerThread = 100000 / parallelism;
            Application.Current.Dispatcher.Invoke(() =>
            {


                for (int i = 0; i < parallelism; i++)
                {

                    progressBars[i] = new ProgressBar()
                    {
                        Maximum = 1.0,
                        Height = 18,
                        Value = 0.5,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    ProgressBarsStackPanelTask.Children.Add(progressBars[i]);
                }


                progressBars[parallelism] = new ProgressBar()
                {
                    Maximum = 1.0,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                ProgressBarsStackPanelTask.Children.Add(progressBars[parallelism]);

            });

            bool lockWasTaken = false;
            try
            {
                Monitor.TryEnter(locker, timeout, ref lockWasTaken);
                {

                    FileStream fs = new FileStream(res, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    StreamWriter sw = new StreamWriter(fs);
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await factory.StartNew(() =>
                    {

                        _ = Parallel.For(1, 100000, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, (i, state) =>
                        {
                            ManualResetEvent.WaitOne();
                            if (cancellationToken.IsCancellationRequested)
                            {
                                System.Diagnostics.Trace.WriteLine("Task is canceled.");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressBars[parallelism].Value = 0.0;
                                });

                                state.Break();

                            }
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                            catch (OperationCanceledException)
                            {
                                System.Diagnostics.Trace.WriteLine("Task is canceled.");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressBars[parallelism].Value = 0.0;
                                });

                                state.Break();


                            }


                            if (stopWatch.ElapsedMilliseconds >= totalTime)
                            {
                                try
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        progressBars[parallelism].Value = 0.0;
                                    });
                                    state.Break();
                                }
                                catch (OperationCanceledException)
                                {
                                }

                            }

                            sw.WriteLine(i);


                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                double di = (double)i;
                                double drpt = (double)rowsPerThread;
                                progressBars[i / rowsPerThread].Value = (di - (i / rowsPerThread) * rowsPerThread) / drpt;
                                progressBars[parallelism].Value += 1.0 / (double)100000;

                            });
                            if (i == 100000 - 1)
                            {
                                cts.Cancel();
                            }
                        });

                    }, cancellationToken);
                    sw.Close();


                }
            }
            finally { if (lockWasTaken) Monitor.Exit(locker); }



        }
        private void SharpImageButton_Click(object sender, RoutedEventArgs e)
        {
            Sharpen.ParallelSharp = false;

            foreach (MyTask myTask in sharpenTasksForExecution)
            {
                Sharpen.ManualResetEvent.Set();
                myTask.Start(myScheduler);
                myTask.CancellationTokenSource.CancelAfter(myTask.Deadline);

            }

            Task.WhenAll(sharpenTasksForExecution);
            sharpenTasksForExecution.Clear();


        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog ofd = new();
            ofd.DefaultExt = ".txt";
            ofd.Multiselect = true;
            ofd.InitialDirectory = Environment.CurrentDirectory;
            ofd.ShowDialog();
            _images = ofd.FileNames;


        }


        private void PlaybackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_paused)
            {
                _paused = false;
                Sharpen.ManualResetEvent.Set();
                PlaybackButton.Content = "Pause";

            }
            else
            {
                _paused = true;
                Sharpen.ManualResetEvent.Reset();
                PlaybackButton.Content = "Start";

            }
        }

        private void StopPlaybackButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var task in allSharpenTasks)
            {
                if (task is MyTask myTask)
                {
                    try
                    {
                        myTask.CancellationTokenSource.Cancel();

                    }
                    catch (TaskCanceledException)
                    { }
                }
            }

        }


        private void SharpImageParallelButton_Click(object sender, RoutedEventArgs e)
        {
            Sharpen.ParallelSharp = true;

            foreach (MyTask myTask in sharpenTasksForExecution)
            {
                Sharpen.ManualResetEvent.Set();
                myTask.Start(myScheduler);
                myTask.CancellationTokenSource.CancelAfter(myTask.Deadline);

            }

            Task.WhenAll(sharpenTasksForExecution);
            sharpenTasksForExecution.Clear();

        }








        private void Process_Click(object sender, RoutedEventArgs e)
        {

            foreach (MyTask myTask in tasksForExecution)
            {
                ManualResetEvent.Set();
                myTask.Start(myScheduler);
                myTask.CancellationTokenSource.CancelAfter(myTask.Deadline);

            }

            Task.WhenAll(tasksForExecution);
            tasksForExecution.Clear();

        }



        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MyScheduler.PriorityScheduling = true;
            PriorityTxt.Visibility = Visibility.Visible;
            Priority.Visibility = Visibility.Visible;

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MyScheduler.PriorityScheduling = false;
            PriorityTxt.Visibility = Visibility.Hidden;
            Priority.Visibility = Visibility.Hidden;

        }



        private new void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }


        private void AddTask_Click(object sender, RoutedEventArgs e)
        {

            if (CheckBoxResources.IsChecked == true)
            {
                try
                {
                    int parallelism = Int32.Parse(MaxDegreeOfParallelismTxt.Text);

                    if (parallelism > myScheduler.NumberOfCores)
                    {
                        parallelism = myScheduler.NumberOfCores;
                    }

                    int totalTime = Int32.Parse(TotalExecutionTimeTxt.Text);
                    TaskFactory factory = new TaskFactory(myScheduler);
                    CancellationTokenSource cancellationToken = new CancellationTokenSource();
                    if (resource != null)
                    {
                        MyTask myTask = new MyTask(delegate { FunctionWithResource(cancellationToken, factory, parallelism, resource, totalTime); }, int.Parse(PriorityTxt.Text), int.Parse(TotalExecutionTimeTxt.Text), int.Parse(DeadlineTxt.Text), cancellationToken, resource);

                        tasksForExecution.Add(myTask);
                        allTasks.Add(myTask);
                    }
                    else
                    {
                        MessageBox.Show("Please, input all values.");
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Please, input all values.");
                }
            }
            else if ((CheckBox.IsChecked == true || CheckBoxPreemptive.IsChecked == true) && CheckBoxResources.IsChecked == false)
            {
                try
                {
                    int parallelism = Int32.Parse(MaxDegreeOfParallelismTxt.Text);

                    if (parallelism > myScheduler.NumberOfCores)
                    {
                        parallelism = myScheduler.NumberOfCores;
                    }

                    int totaltime = Int32.Parse(TotalExecutionTimeTxt.Text);
                    TaskFactory factory = new TaskFactory(myScheduler);
                    CancellationTokenSource cancellationToken = new CancellationTokenSource();

                    MyTask myTask = new MyTask(delegate { Function(cancellationToken, factory, parallelism, totaltime); }, int.Parse(PriorityTxt.Text), int.Parse(TotalExecutionTimeTxt.Text), int.Parse(DeadlineTxt.Text), cancellationToken);

                    tasksForExecution.Add(myTask);
                    allTasks.Add(myTask);
                }
                catch (Exception)
                {
                    MessageBox.Show("Please, input all values.");
                }
            }
            else
            {
                int parallelism = Int32.Parse(MaxDegreeOfParallelismTxt.Text);

                if (parallelism > myScheduler.NumberOfCores)
                {
                    parallelism = myScheduler.NumberOfCores;
                }
                int totaltime = Int32.Parse(TotalExecutionTimeTxt.Text);
                TaskFactory factory = new TaskFactory(myScheduler);
                CancellationTokenSource cancellationToken = new();
                try
                {
                    MyTask myTask = new MyTask(delegate { Function(cancellationToken, factory, parallelism, totaltime); }, Int32.Parse(TotalExecutionTimeTxt.Text), Int32.Parse(DeadlineTxt.Text), cancellationToken);
                    tasksForExecution.Add(myTask);
                    allTasks.Add(myTask);
                }
                catch (Exception)
                {
                    MessageBox.Show("Please, input all values.");
                }
            }
        }

        private void CreateScheduler_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numOfTasks = Int32.Parse(NumOfTasksTxt.Text);
                int numOfCores = Int32.Parse(NumberOfCoresTxt.Text);
                myScheduler = new MyScheduler(numOfTasks, numOfCores);

            }
            catch
            {
                MessageBox.Show("Please, insert all values.");
            }
        }

        private void CheckBoxPreemptive_Unchecked(object sender, RoutedEventArgs e)
        {
            MyScheduler.PreemptiveScheduling = false;
            PriorityTxt.Visibility = Visibility.Hidden;
            Priority.Visibility = Visibility.Hidden;

        }

        private void CheckBoxPreemptive_Checked(object sender, RoutedEventArgs e)
        {
            MyScheduler.PreemptiveScheduling = true;
            PriorityTxt.Visibility = Visibility.Visible;
            Priority.Visibility = Visibility.Visible;


        }


        private void AddSharpenTask_Click(object sender, RoutedEventArgs e)
        {

            Sharpen sharpen = new Sharpen();
            sharpen.ProgressBarsStackPanel = ProgressBarsStackPanel;

            if (CheckBoxResources.IsChecked == true)
            {
                try
                {
                    int parallelism = Int32.Parse(MaxDegreeOfParallelismTxt.Text);

                    if (parallelism > myScheduler.NumberOfCores)
                    {
                        parallelism = myScheduler.NumberOfCores;
                    }
                    sharpen.Parallelism = parallelism;
                    sharpen.TotalTime = Int32.Parse(TotalExecutionTimeTxt.Text);
                    sharpen.Deadline = Int32.Parse(DeadlineTxt.Text);
                    TaskFactory factory = new TaskFactory(myScheduler);
                    CancellationTokenSource cancellationToken = new CancellationTokenSource();
                    sharpen.CancellationTokenSource = cancellationToken;
                    sharpen.CancellationToken = cancellationToken.Token;
                    if (_images != null)
                    {
                        object? resources = resource ?? "default_resource";
                        MyTask myTask = new MyTask(delegate { sharpen.MultipleImagesSharpening(_images, factory); }, Int32.Parse(PriorityTxt.Text), Int32.Parse(TotalExecutionTimeTxt.Text), Int32.Parse(DeadlineTxt.Text), cancellationToken, resources);
                        sharpenTasksForExecution.Add(myTask);
                        allSharpenTasks.Add(myTask);
                    }
                    else
                    {
                        MessageBox.Show("Please, choose image first.");
                    }

                }
                catch (Exception)
                {
                    MessageBox.Show("Please, input all values.");
                }
            }
            else if ((CheckBox.IsChecked == true || CheckBoxPreemptive.IsChecked == true) && CheckBoxResources.IsChecked == false)
            {

                try
                {

                    int parallelism = Int32.Parse(MaxDegreeOfParallelismTxt.Text);

                    if (parallelism > myScheduler.NumberOfCores)
                    {
                        parallelism = myScheduler.NumberOfCores;
                    }
                    sharpen.Parallelism = parallelism;
                    sharpen.TotalTime = int.Parse(TotalExecutionTimeTxt.Text);
                    sharpen.Deadline = int.Parse(DeadlineTxt.Text);
                    TaskFactory factory = new TaskFactory(myScheduler);

                    CancellationTokenSource cancellationToken = new CancellationTokenSource();
                    sharpen.CancellationToken = cancellationToken.Token;
                    sharpen.CancellationTokenSource = cancellationToken;
                    if (_images != null)
                    {
                        MyTask myTask = new MyTask(delegate { sharpen.MultipleImagesSharpening(_images, factory); }, Int32.Parse(PriorityTxt.Text), Int32.Parse(TotalExecutionTimeTxt.Text), Int32.Parse(DeadlineTxt.Text), cancellationToken);
                        sharpenTasksForExecution.Add(myTask);
                        allSharpenTasks.Add(myTask);
                    }
                    else
                    {
                        MessageBox.Show("Please, choose image first.");
                    }


                }
                catch (Exception)
                {
                    MessageBox.Show("");
                }
            }
            else
            {

                int parallelism = Int32.Parse(MaxDegreeOfParallelismTxt.Text);

                if (parallelism > myScheduler.NumberOfCores)
                {
                    parallelism = myScheduler.NumberOfCores;
                }
                sharpen.Parallelism = parallelism;
                sharpen.TotalTime = Int32.Parse(TotalExecutionTimeTxt.Text);
                sharpen.Deadline = Int32.Parse(DeadlineTxt.Text);
                TaskFactory factory = new TaskFactory(myScheduler);
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                sharpen.CancellationTokenSource = cancellationToken;
                sharpen.CancellationToken = cancellationToken.Token;

                if (_images != null)
                {
                    MyTask myTask = new MyTask(delegate { sharpen.MultipleImagesSharpening(_images, factory); }, Int32.Parse(TotalExecutionTimeTxt.Text), Int32.Parse(DeadlineTxt.Text), cancellationToken);
                    sharpenTasksForExecution.Add(myTask);
                    allSharpenTasks.Add(myTask);
                }
                else
                {
                    MessageBox.Show("Please, choose image first.");
                }

            }

        }

        private void CheckBoxResources_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchResourcesButton.Visibility = Visibility.Hidden;
        }

        private void CheckBoxResources_Checked(object sender, RoutedEventArgs e)
        {
            SearchResourcesButton.Visibility = Visibility.Visible;
        }

        private void SearchResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.DefaultExt = ".txt";
            ofd.Multiselect = false;
            ofd.InitialDirectory = Environment.CurrentDirectory;
            ofd.ShowDialog();
            resource = ofd.FileName;

        }

        private void PlaybackButton1_Click(object sender, RoutedEventArgs e)
        {
            if (_paused1)
            {
                _paused1 = false;
                ManualResetEvent.Set();
                PlaybackButton1.Content = "Pause";

            }
            else
            {
                _paused1 = true;
                ManualResetEvent.Reset();
                PlaybackButton1.Content = "Start";

            }
        }

        private void StopPlaybackButton1_Click(object sender, RoutedEventArgs e)
        {

            foreach (var task in allTasks)
            {

                if (task is MyTask myTask)
                {
                    try
                    {
                        myTask.CancellationTokenSource.Cancel();
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }

        }
    }
}