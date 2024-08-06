using OposFS;
using OposScheduler;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void MyScheduler_Constructor() 
        {
            int expectedMaxDegree = 3;
            int expectedNumOfCores = 5;

            MyScheduler scheduler = new MyScheduler(3, 5);
            int actualMaxDegree = scheduler.MaxDegreeOfParallelism;
            int actualNumOfCores = scheduler.NumberOfCores;

            Assert.Equal(expectedMaxDegree, actualMaxDegree);
            Assert.Equal(expectedNumOfCores, actualNumOfCores);

        }

       

        [Fact]
        public void MyTask_Constructor1()
        {
            int expectedDuration = 1000;
            int expectedDeadline = 5000;

            MyTask myTask = new MyTask(testFunction, 1000, 5000, new CancellationTokenSource());
            int actualDuration = myTask.Duration;
            int actualDeadline = myTask.Deadline;

            Assert.Equal(expectedDuration, actualDuration);
            Assert.Equal(expectedDeadline, actualDeadline);

        }
        [Fact]
        public void MyTask_Constructor2()
        {
            int expextedPriority = 5;

            MyTask myTask = new MyTask(testFunction, 5 ,1000, 5000, new CancellationTokenSource());
            int actualPriority = myTask.Priority;
           
            Assert.Equal(expextedPriority, actualPriority);
        

        }
        [Fact]
        public void MyTask_Constructor3()
        {
            string expectedResource = "resource";
            
            MyTask myTask = new MyTask(testFunction, 5, 1000, 5000, new CancellationTokenSource(), "resource");
            string actualResource = (string)myTask.Resource;
           
            Assert.Equal(expectedResource, actualResource);

        }


        [Fact]
        public void MyScheduler_PriorityScheduling()
        {
            int expected = 4;

            MyTask myTask1 = new MyTask(testFunction,2 ,1000, 1000, new CancellationTokenSource());
            MyTask myTask2 = new MyTask(testFunction, 4, 1000, 1000, new CancellationTokenSource());

            MyScheduler scheduler = new MyScheduler(1, 5);
            MyScheduler.PriorityScheduling = true;

            myTask1.Start(scheduler);
            myTask2.Start(scheduler);

            Task.Delay(100).Wait();
            int actual = (scheduler.GetTasks()[0] as MyTask).Priority;
            Assert.Equal(expected, actual);

        }

        [Fact]
        public void MyScheduler_PreemptiveScheduling()
        {
            bool expected = false;

            MyTask myTask1 = new MyTask(testFunction, 2, 1000, 1000, new CancellationTokenSource());
            MyTask myTask2 = new MyTask(testFunction, 4, 1000, 1000, new CancellationTokenSource());

            MyScheduler scheduler = new MyScheduler(1, 5);
            MyScheduler.PreemptiveScheduling = true;

            myTask1.Start(scheduler);
            Task.Delay(100).Wait();
            myTask2.Start(scheduler);

            Task.Delay(100).Wait();
            bool actual = myTask1.CancellationToken.IsCancellationRequested;
            Assert.Equal(expected, actual);

        }
        public void MyScheduler_PIP()
        {
            int expected = 5;
            bool expectedCancellation = false;

            MyTask myTask1 = new MyTask(testFunction, 2, 1000, 1000, new CancellationTokenSource(), "resource");
            MyTask myTask2 = new MyTask(testFunction, 4, 1000, 1000, new CancellationTokenSource(), "resource");

            MyScheduler scheduler = new MyScheduler(1, 5);
            MyScheduler.PreemptiveScheduling = true;

            myTask1.Start(scheduler);
            Task.Delay(100).Wait();
            myTask2.Start(scheduler);

            Task.Delay(100).Wait();
            int actual = myTask1.Priority;
            bool actualCancellation = myTask1.CancellationToken.IsCancellationRequested;

            Assert.Equal(expected, actual);
            Assert.Equal(expectedCancellation, actualCancellation);

        }


    [Fact]
    public void MyScheduler_Deadlock()
    {
            string file = "C:\\Users\\danil\\OneDrive\\Desktop\\deadlock.txt";
            bool expectedStatus1 = true;
            bool expectedStatus2 = true;
        

            MyTask myTask1 = new MyTask(delegate { TaskWithResources(file); }, 2, 1000, 1000, new CancellationTokenSource(), "resource");
            MyTask myTask2 = new MyTask(delegate { TaskWithResources2(file); }, 4, 1000, 1000, new CancellationTokenSource(), "resource");

            MyScheduler scheduler = new MyScheduler(2, 5);
            MyScheduler.PreemptiveScheduling = true;

            myTask1.Start(scheduler);
            myTask2.Start(scheduler);

            Task.Delay(10000).Wait();
            bool actualStatus1 = myTask1.IsCompleted;
            bool actualStatus2 = myTask2.IsCompleted;


            Assert.Equal(expectedStatus1, actualStatus1);
            Assert.Equal(expectedStatus2, actualStatus2);

    }
        [Fact]
        public void Sharpening()
        {
            string file1 = "C:\\Users\\danil\\OneDrive\\Desktop\\opos-test\\pexels-photo-922611.jpeg";
            string file2 = "C:\\Users\\danil\\OneDrive\\Desktop\\opos-test\\pexels-photo-922611 - Copy.jpeg";
            MyScheduler scheduler = new MyScheduler(2, 6);
            TaskFactory taskFactory = new TaskFactory(scheduler);

            Sharpen sharpen1 = new Sharpen();
            Sharpen sharpen2 = new Sharpen();
            Sharpen.Parallelism = 2;
            Sharpen.Test = true;
            sharpen1.SharpeningAsync(file1, taskFactory);       
            sharpen2.Sharpening(file2, taskFactory);

            Task.Delay(10000).Wait();
            Sharpen.Test = false;
            Assert.True(sharpen1.Time < sharpen2.Time);




        }


        private void testFunction()
        {
            for(int i = 0; i < 10; i++)
            {
               
            }
        }

        object locker = new object();
        object locker1 = new object();
        TimeSpan timeout = TimeSpan.FromMilliseconds(500);
        void TaskWithResources(string file)
        {
            FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            bool lockWasTaken = false;
            bool lockWasTaken1 = false;
            try
            {
             Monitor.TryEnter(locker, timeout, ref lockWasTaken);
              {
                    Thread.Sleep(1000);
                    try
                    {
                      Monitor.TryEnter(locker1, timeout, ref lockWasTaken1);
                       {
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine("opos1\n");
                            sw.Close();
                      }
                    }
                    finally { if (lockWasTaken1) { Monitor.Exit(locker1); } }
                }
            }
            finally { if (lockWasTaken) Monitor.Exit(locker); }


        }
        void TaskWithResources2(string file)
        {

            FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            bool lockWasTaken = false;
            bool lockWasTaken1 = false;
            try
            {
               Monitor.TryEnter(locker1, timeout, ref lockWasTaken1);
               {
                    Thread.Sleep(1000);
                    try
                    {
                        Monitor.TryEnter(locker, timeout, ref lockWasTaken);
                       {
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine("opos2\n");
                            sw.Close();
                        }
                    }
                    finally { if (lockWasTaken) { Monitor.Exit(locker1); } }
                }
            }
            finally { if (lockWasTaken1) Monitor.Exit(locker); }
        }
       
     
    }
}