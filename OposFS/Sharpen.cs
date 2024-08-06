using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace OposFS
{
    [SupportedOSPlatform("windows")]
    public class Sharpen
    {
        private readonly int _factor = 1;
        private readonly int _offset = 1;
        private readonly int[,] _matrix = new int[3, 3];
        public static bool Test = false;
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public static int Parallelism { get; set; }
        public int Time { get; set; }
        public Sharpen()
        {

            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

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

        private static Bitmap ConvertToBitmap(string fileName)
        {
            Stream bmpStream = File.Open(fileName, FileMode.Open);
            Image image = Image.FromStream(bmpStream);
            Bitmap bitmap = new(image);
            image.Dispose();

            return bitmap;
        }

        public async Task Sharpening(string fileName, TaskFactory factory)
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


                int rows = bmp.Height;
                await factory.StartNew(() =>
                {

                    unsafe
                    {

                        byte* dest = (byte*)ptrDest;
                        byte* src = (byte*)ptrSrc;

                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();
                        for (int i = 0; i < height; i++)
                        {

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
                        Time = int.Parse(ts.Milliseconds.ToString());
                        Console.WriteLine("RunTime  " + elapsedTime);

                    }
                }, CancellationToken);
       

        }

        public async Task SharpeningAsync(string fileName, TaskFactory factory)
        {


            Bitmap bmp = ConvertToBitmap(fileName);
            Bitmap bmpClone = (Bitmap)bmp.Clone();

            Rectangle rect = new(0, 0, bmp.Width, bmp.Height);
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


            int rows = bmp.Height;

            await factory.StartNew(() =>
            {
                int rowsPerThread = height / Parallelism;
                unsafe
                {

                    byte* dest = (byte*)ptrDest;
                    byte* src = (byte*)ptrSrc;

                    Stopwatch stopWatch = new();
                    stopWatch.Start();

                    Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = Parallelism }, (i, state) =>
                    {

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

                    });

                    Bitmap res = new(bmp.Width, bmp.Height, bmpDest.Stride, PixelFormat.Format24bppRgb, ptrDest);
                    if (Test)
                    {
                        res.Save("C:\\Users\\danil\\OneDrive\\Desktop\\output\\" + Path.GetFileName(fileName), ImageFormat.Jpeg);
                    }
                    else
                    {
                        res.Save("Y:\\output\\" + Path.GetFileName(fileName), ImageFormat.Jpeg);
                    }

                    bmp.UnlockBits(bmpSrc);
                    bmpClone.UnlockBits(bmpDest);

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    Time = int.Parse(ts.Milliseconds.ToString());
                    Console.WriteLine("RunTime parallel " + elapsedTime);
                }
            }, CancellationToken);


        }

    }
}
