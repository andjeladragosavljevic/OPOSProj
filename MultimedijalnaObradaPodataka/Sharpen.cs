using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace OposMMOP
{
    public class Sharpen
    {
        /// <summary>
        /// Convolution matrix.
        /// </summary>
        private readonly int[,] _matrix = new int[3, 3];
        /// <summary>
        /// The factor by which the value of the matrix is ​​divided.
        /// </summary>
        private readonly int _factor = 1;
        /// <summary>
        ///  The value to be added to the value of the matrix.
        /// </summary>
        private readonly int _offset = 1;
        public StackPanel? ProgressBarsStackPanel { get; set; }
        /// <summary>
        /// Indicates whether it is parallel image sharpening.
        /// </summary>
        public static bool ParallelSharp { get; set; }

        private readonly string? _fileName;

        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public int Deadline { get; set; }
        public int TotalTime { get; set; }
        public int Parallelism { get; set; }
        public Sharpen()
        {
            _matrix[0, 0] = 0;
            _matrix[0, 1] = -2;
            _matrix[0, 2] = 0;
            _matrix[1, 0] = -2;
            _matrix[1, 1] = 9;
            _matrix[1, 2] = -2;
            _matrix[2, 0] = 0;
            _matrix[2, 1] = -2;
            _matrix[2, 2] = 0;
        }

        /// <summary>
        /// Utility method for converting file path to a bitmap.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = File.Open(fileName, FileMode.Open))
            {
                System.Drawing.Image image = System.Drawing.Image.FromStream(bmpStream);
                bitmap = new Bitmap(image);

            }
            return bitmap;
        }
        /// <summary>
        /// Task for sharpening multiple images in parallel.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public async Task MultipleImagesSharpening(string[] images, TaskFactory factory)
        {

            List<Task> tasks = new();

            if (ParallelSharp)
            {
                foreach (string image in images)
                {
                    tasks.Add(SharpeningAsync(image, factory));
                }
            }
            else
            {
                foreach (string image in images)
                {
                    tasks.Add(Sharpening(image, factory));
                }
            }
            await Task.WhenAll(tasks);
        }

        public async Task Sharpening(string fileName, TaskFactory factory)
        {
            int Parallelism = 1;

            Bitmap bmp = ConvertToBitmap(fileName);
            Bitmap bmpClone = (Bitmap)bmp.Clone();

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpSrc =
                bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmpDest =
                bmpClone.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);


            IntPtr ptrSrc = bmpSrc.Scan0;
            IntPtr ptrDest = bmpDest.Scan0;

            int stride = bmpSrc.Stride;
            int stride3thRow = stride * 2;

            int height = bmp.Height - 2;
            int width = bmp.Width - 2;

            int offset = stride - bmp.Width * 3;
            var progressBars = new ProgressBar[Parallelism + 1];
            Application.Current.Dispatcher.Invoke(() =>
            {


                for (int i = 0; i < Parallelism; i++)
                {

                    progressBars[i] = new ProgressBar()
                    {
                        Maximum = 1.0,
                        Height = 18,
                        Value = 0.5,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    if (ProgressBarsStackPanel != null)
                    {
                        ProgressBarsStackPanel.Children.Add(progressBars[i]);
                    }
                }


                progressBars[Parallelism] = new ProgressBar()
                {
                    Maximum = 1.0,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                if (ProgressBarsStackPanel != null)
                {
                    ProgressBarsStackPanel.Children.Add(progressBars[Parallelism]);
                }

            });

            int rows = bmp.Height;
            await factory.StartNew(() =>
            {
            int rowsPerThread = height / Parallelism;
            unsafe
            {

                byte* dest = (byte*)ptrDest;
                byte* src = (byte*)ptrSrc;

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                for (int i = 0; i < height; i++)
                {
         
                    ManualResetEvent.WaitOne();


                    if (CancellationToken.IsCancellationRequested)
                    {

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            progressBars[Parallelism].Value = 0.0;
                        });
                        break;
                    }

                    if (stopWatch.ElapsedMilliseconds >= TotalTime)
                    {
                        try
                        {
                            CancellationToken.ThrowIfCancellationRequested();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressBars[Parallelism].Value = 0.0;
                            });
                            break;
                        }
                        catch (TaskCanceledException)
                        { }

                    }

                    byte* pos = src + stride * i;
                    byte* posDest = dest + stride * i;

                    for (int j = 0; j < width; j++)
                    {

                        int R = (pos[0] * _matrix[0, 0] + pos[3] * _matrix[0, 1] + pos[6] * _matrix[0, 2]
                                + pos[0 + stride] * _matrix[1, 0] + pos[3 + stride] * _matrix[1, 1] + pos[6 + stride] * _matrix[1, 2]
                                + pos[0 + stride3thRow] * _matrix[2, 0] + pos[3 + stride3thRow] * _matrix[2, 1] + pos[6 + stride3thRow] * _matrix[2, 2]) / _factor + _offset;

                        if (R < 0) R = 0;
                        if (R > 255) R = 255;
                        posDest[0] = (byte)R;


                        int G = (pos[1] * _matrix[0, 0] + pos[4] * _matrix[0, 1] + pos[7] * _matrix[0, 2]
                          + pos[1 + stride] * _matrix[1, 0] + pos[4 + stride] * _matrix[1, 1] + pos[7 + stride] * _matrix[1, 2]
                          + pos[1 + stride3thRow] * _matrix[2, 0] + pos[4 + stride3thRow] * _matrix[2, 1] + pos[7 + stride3thRow] * _matrix[2, 2]) / _factor + _offset;

                        if (G < 0) G = 0;
                        if (G > 255) G = 255;
                        posDest[1] = (byte)G;


                        int B = (pos[2] * _matrix[0, 0] + pos[5] * _matrix[0, 1] + pos[8] * _matrix[0, 2]
                           + pos[2 + stride] * _matrix[1, 0] + pos[5 + stride] * _matrix[1, 1] + pos[8 + stride] * _matrix[1, 2]
                           + pos[2 + stride3thRow] * _matrix[2, 0] + pos[5 + stride3thRow] * _matrix[2, 1] + pos[8 + stride3thRow] * _matrix[2, 2]) / _factor + _offset;

                        if (B < 0) B = 0;
                        if (B > 255) B = 255;
                        posDest[2] = (byte)B;

                        pos += 3;
                        posDest += 3;


                    }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            double di = (double)i;
                        double drpt = (double)rowsPerThread;
                        progressBars[i / rowsPerThread].Value = (di - (i / rowsPerThread) * rowsPerThread) / drpt;
                        progressBars[Parallelism].Value += 1.0 / (double)rows;
                        if (i % 100 == 0)
                            Thread.Sleep(500);

                    });

                    }

                    Bitmap res = new Bitmap(bmp.Width, bmp.Height, bmpDest.Stride, PixelFormat.Format24bppRgb, ptrDest);
                    res.Save("C:\\Users\\danil\\OneDrive\\Desktop\\output\\" + Path.GetFileName(fileName), ImageFormat.Jpeg);

                    bmp.UnlockBits(bmpSrc);
                    bmpClone.UnlockBits(bmpDest);

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);

                    Console.WriteLine("RunTime  " + elapsedTime);

                }
            }, CancellationToken);

        }
    

        /// <summary>
        /// Parallel processing of image.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public async Task SharpeningAsync(string fileName, TaskFactory factory)
        {
           
            Bitmap bmp = ConvertToBitmap(fileName);
            Bitmap bmpClone = (Bitmap)bmp.Clone();

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpSrc =
                bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmpDest =
                bmpClone.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);


            IntPtr ptrSrc = bmpSrc.Scan0;
            IntPtr ptrDest = bmpDest.Scan0;

            int stride = bmpSrc.Stride;
            int stride3thRow = stride * 2;

            int height = bmp.Height - 2;
            int width = bmp.Width - 2;

            int offset = stride - bmp.Width * 3;
            var progressBars = new ProgressBar[Parallelism + 1];
            Application.Current.Dispatcher.Invoke(() =>
            {


                for (int i = 0; i < Parallelism; i++)
                {

                    progressBars[i] = new ProgressBar()
                    {
                        Maximum = 1.0,
                        Height = 18,
                        Value = 0.5,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    if (ProgressBarsStackPanel != null)
                    {
                        ProgressBarsStackPanel.Children.Add(progressBars[i]);
                    }
                }


                progressBars[Parallelism] = new ProgressBar()
                {
                    Maximum = 1.0,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                if (ProgressBarsStackPanel != null)
                {
                    ProgressBarsStackPanel.Children.Add(progressBars[Parallelism]);
                }

            });

            int rows = bmp.Height;
            await factory.StartNew(() =>
            {
                int rowsPerThread = height / Parallelism;
                unsafe
                {

                    byte* dest = (byte*)ptrDest;
                    byte* src = (byte*)ptrSrc;

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = Parallelism }, (i, state) =>
                    {


                        ManualResetEvent.WaitOne();


                        if (CancellationToken.IsCancellationRequested)
                        {

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressBars[Parallelism].Value = 0.0;
                            });
                            state.Break();
                        }

                        if (stopWatch.ElapsedMilliseconds >= TotalTime)
                        {
                            try
                            {
                                CancellationToken.ThrowIfCancellationRequested();
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressBars[Parallelism].Value = 0.0;
                                });
                                state.Break();
                            }
                            catch (TaskCanceledException) 
                            { }

                        }

                        byte* pos = src + stride * i;
                        byte* posDest = dest + stride * i;

                        for (int j = 0; j < width; j++)
                        {

                            int R = (pos[0] * _matrix[0, 0] + pos[3] * _matrix[0, 1] + pos[6] * _matrix[0, 2]
                                    + pos[0 + stride] * _matrix[1, 0] + pos[3 + stride] * _matrix[1, 1] + pos[6 + stride] * _matrix[1, 2]
                                    + pos[0 + stride3thRow] * _matrix[2, 0] + pos[3 + stride3thRow] * _matrix[2, 1] + pos[6 + stride3thRow] * _matrix[2, 2]) / _factor + _offset;

                            if (R < 0) R = 0;
                            if (R > 255) R = 255;
                            posDest[0] = (byte)R;


                            int G = (pos[1] * _matrix[0, 0] + pos[4] * _matrix[0, 1] + pos[7] * _matrix[0, 2]
                              + pos[1 + stride] * _matrix[1, 0] + pos[4 + stride] * _matrix[1, 1] + pos[7 + stride] * _matrix[1, 2]
                              + pos[1 + stride3thRow] * _matrix[2, 0] + pos[4 + stride3thRow] * _matrix[2, 1] + pos[7 + stride3thRow] * _matrix[2, 2]) / _factor + _offset;

                            if (G < 0) G = 0;
                            if (G > 255) G = 255;
                            posDest[1] = (byte)G;


                            int B = (pos[2] * _matrix[0, 0] + pos[5] * _matrix[0, 1] + pos[8] * _matrix[0, 2]
                               + pos[2 + stride] * _matrix[1, 0] + pos[5 + stride] * _matrix[1, 1] + pos[8 + stride] * _matrix[1, 2]
                               + pos[2 + stride3thRow] * _matrix[2, 0] + pos[5 + stride3thRow] * _matrix[2, 1] + pos[8 + stride3thRow] * _matrix[2, 2]) / _factor + _offset;

                            if (B < 0) B = 0;
                            if (B > 255) B = 255;
                            posDest[2] = (byte)B;

                            pos += 3;
                            posDest += 3;


                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            double di = (double)i;
                            double drpt = (double)rowsPerThread;
                            progressBars[i / rowsPerThread].Value = (di - (i / rowsPerThread) * rowsPerThread) / drpt;
                            progressBars[Parallelism].Value += 1.0 / (double)rows;
                            if (i % 100 == 0)
                                Thread.Sleep(500);

                        });

                    });

                    Bitmap res = new Bitmap(bmp.Width, bmp.Height, bmpDest.Stride, PixelFormat.Format24bppRgb, ptrDest);
                    res.Save("C:\\Users\\danil\\OneDrive\\Desktop\\output\\" + Path.GetFileName(fileName), ImageFormat.Jpeg);
                  
                    bmp.UnlockBits(bmpSrc);
                    bmpClone.UnlockBits(bmpDest);

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);

                    Console.WriteLine("RunTime parallel " + elapsedTime);
                }
            }, CancellationToken);

        }

    }
}
