﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPULoadTester
{
    class clsMonteCarloPiApproximation
    {
        private const int ENDTIME_CHECK_MODULUS = 100000;

        #region "Properties"
        public bool UseTieredRuntimes { get; set; }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMonteCarloPiApproximation()
        {
            UseTieredRuntimes = false;
        }

        #region "Public methods"

        public void ParallellFor(int maxRuntimeSeconds)
        {
            var numberOfCores = Environment.ProcessorCount;
            ParallellFor(maxRuntimeSeconds, numberOfCores);
        }

        /// <summary>
        /// Estimate Pi using a parallel for loop
        /// </summary>
        /// <param name="maxRuntimeSeconds"></param>
        /// <param name="numberOfCores"></param>
        public void ParallellFor(int maxRuntimeSeconds, int numberOfCores)
        {
            long inCircle = 0;
            long totalIterations = 0;
            var threadNumber = 0;

            Parallel.For(0, numberOfCores, new ParallelOptions { MaxDegreeOfParallelism = numberOfCores }, i =>
            {
                Interlocked.Add(ref threadNumber, 1);

                var worker = new PiEstimateWorker(threadNumber);
                worker.DoWork(maxRuntimeSeconds);

                Interlocked.Add(ref inCircle, worker.HitsInCircle);
                Interlocked.Add(ref totalIterations, worker.TotalIterations);

                if (UseTieredRuntimes)
                {
                    maxRuntimeSeconds = DecrementRuntime(maxRuntimeSeconds);
                }

                
            });

            ComputeAndReportPi("ParallelFor", inCircle, totalIterations, maxRuntimeSeconds);
        
        }

        public void TaskParallelLibrary40(int maxRuntimeSeconds)
        {
            var numberOfCores = Environment.ProcessorCount;
            TaskParallelLibrary40(maxRuntimeSeconds, numberOfCores);
        }

        /// <summary>
        /// Estimate Pi using TPL tasks
        /// </summary>
        /// <param name="maxRuntimeSeconds"></param>
        /// <param name="numberOfCores"></param>
        /// <remarks>This code only works with .NET 4.0 or newer</remarks>
        public void TaskParallelLibrary40(int maxRuntimeSeconds, int numberOfCores)
        {
            var inCircleDetails = new long[numberOfCores];
            var iterationDetails = new long[numberOfCores];

            var tasks = new Task[numberOfCores];

            var runTimesByIndex = GetRunTimesByThread(numberOfCores, maxRuntimeSeconds);

            foreach (var item in runTimesByIndex)
            {           
                // Must cache the values to avoid error "Access to modified closure"
                var procIndex = item.Key;
                var threadRuntime = item.Value;

                tasks[procIndex] = Task.Factory.StartNew(() =>
                {
                    var worker = new PiEstimateWorker(procIndex + 1);
                    worker.DoWork(threadRuntime);

                    inCircleDetails[procIndex] = worker.HitsInCircle;
                    iterationDetails[procIndex] = worker.TotalIterations;

                });

            }

            Task.WaitAll(tasks);

            var inCircle = inCircleDetails.Sum();
            var totalIterations = iterationDetails.Sum();

            ComputeAndReportPi("TPL 4.0", inCircle, totalIterations, maxRuntimeSeconds);

        }

        public void TaskParallelLibrary45(int maxRuntimeSeconds)
        {
            var numberOfCores = Environment.ProcessorCount;
            TaskParallelLibrary45(maxRuntimeSeconds, numberOfCores);
        }

        /// <summary>
        /// Estimate Pi using TPL tasks
        /// </summary>
        /// <param name="maxRuntimeSeconds"></param>
        /// <param name="numberOfCores"></param>
        /// <remarks>This code only works with .NET 4.5 or newer</remarks>
        public void TaskParallelLibrary45(int maxRuntimeSeconds, int numberOfCores)
        {
            var workers = new List<PiEstimateWorker>();
            var workerTasks = new List<Task>();

            var runTimesByIndex = GetRunTimesByThread(numberOfCores, maxRuntimeSeconds);

            foreach (var item in runTimesByIndex)
            {
                // Must cache the values to avoid error "Access to modified closure"
                var procIndex = item.Key;
                var threadRuntime = item.Value;

                var worker = new PiEstimateWorker(procIndex + 1);
                workers.Add(worker);

                var workerTask = new Task(() => worker.DoWork(threadRuntime));
                workerTasks.Add(workerTask);
                workerTask.Start();
            }

            Task.WaitAll(workerTasks.ToArray());

            var inCircle = workers.Sum(worker => (long)worker.HitsInCircle);
            var totalIterations = workers.Sum(worker => (long)worker.TotalIterations);

            ComputeAndReportPi("TPL 4.5", inCircle, totalIterations, maxRuntimeSeconds);
        }
       
        /// <summary>
        /// Estimate Pi using a single thread
        /// </summary>
        /// <param name="maxRuntimeSeconds"></param>
        public void SerialCalculation(int maxRuntimeSeconds)
        {
            var worker = new PiEstimateWorker(1);
            worker.DoWork(maxRuntimeSeconds);

            ComputeAndReportPi("SerialCalculation", worker.HitsInCircle, worker.TotalIterations, maxRuntimeSeconds);

        }

        #endregion

        #region "Private Methods"

        private void ComputeAndReportPi(string taskDescription, long inCircle, long totalIterations, int maxRuntimeSeconds)
        {

            var piApproximation = 4 * (inCircle / (double)totalIterations);

            Console.WriteLine();
            Console.WriteLine("{0} approximated Pi = {1} using {2} iterations over {3} seconds",
                taskDescription,
                piApproximation.ToString("F8"), 
                totalIterations.ToString("#,##0"), 
                maxRuntimeSeconds);

        }

        private int DecrementRuntime(int maxRuntimeSeconds)
        {
            var newRuntimeSeconds = maxRuntimeSeconds * 0.8;

            var runtimeFloor = (int)Math.Floor(newRuntimeSeconds);
            if (runtimeFloor < 1)
                return 1;

            return runtimeFloor;
        }

        private Dictionary<int, int> GetRunTimesByThread(int numberOfCores, int maxRuntimeSeconds)
        {
            var runTimesByIndex = new Dictionary<int, int>();

            for (var i = 0; i < numberOfCores; i++)
            {
                runTimesByIndex.Add(i, maxRuntimeSeconds);
                if (UseTieredRuntimes)
                {
                    maxRuntimeSeconds = DecrementRuntime(maxRuntimeSeconds);
                }
            }

            return runTimesByIndex;
        }

        #endregion

        /// <summary>
        /// This class estimates Pi
        /// </summary>
        private class PiEstimateWorker
        {

            private long mHitsInCircle;
            private long mTotalIterations;
            private readonly int mThreadNumber;

            public long HitsInCircle
            {
                get { return mHitsInCircle; }
            }

            public double PiEstimate
            {
                get
                {
                    if (TotalIterations == 0)
                        return 0;

                    return 4 * (HitsInCircle / (double)TotalIterations);
                }
            }

            public long TotalIterations
            {
                get { return mTotalIterations; }
            }

            /// <summary>
            /// Construtor
            /// </summary>
            public PiEstimateWorker(int threadNumber)
            {
                mHitsInCircle = 0;
                mTotalIterations = 0;
                mThreadNumber = threadNumber;
            }

            public void DoWork(int maxRuntimeSeconds)
            {

                var dtEndTime = GetEndTime(maxRuntimeSeconds);

                mHitsInCircle = 0;
                mTotalIterations = 0;

                var rnd = new Random();
                var dtStartTime = DateTime.UtcNow;

                Console.WriteLine("  Thread {0} starting at {1} with runtime {2} seconds", mThreadNumber, GetFormattedTime(), maxRuntimeSeconds);

                var doWork = true;

                while (doWork)
                {
                    var x = rnd.NextDouble();
                    var y = rnd.NextDouble();
                    if (Math.Sqrt(x * x + y * y) <= 1.0)
                        mHitsInCircle++;

                    mTotalIterations++;
                    if (mTotalIterations % ENDTIME_CHECK_MODULUS == 0 && DateTime.UtcNow > dtEndTime)
                    {
                        doWork = false;
                    }
                }

                Console.WriteLine("  Thread {0} complete at {1}, {2} seconds elapsed", 
                    mThreadNumber, 
                    GetFormattedTime(), 
                    dtEndTime.Subtract(dtStartTime).TotalSeconds.ToString("0.0"));
            }

            private DateTime GetEndTime(int maxRuntimeSeconds)
            {
                if (maxRuntimeSeconds < 1)
                    return DateTime.UtcNow.AddSeconds(1);

                return DateTime.UtcNow.AddSeconds(maxRuntimeSeconds);
            }

            private string GetFormattedTime()
            {
                return DateTime.Now.ToString("hh:mm:ss");
            }
        }
    }
}
