using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PRISM;

namespace CPULoadTester
{
    // This program will run test calculations on one more more cores to simulate a multi-threaded application
    //
    // -------------------------------------------------------------------------------
    // Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    //
    // E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
    // Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/
    // -------------------------------------------------------------------------------
    //

    class Program
    {

        public const string PROGRAM_DATE = "June 1, 2017";

        private enum eProcessingMode
        {
            Serial = 0,
            ParallelFor = 1,
            TaskParallelLibrary = 2,
            TaskParallelLibrary4_5 = 3
        }

        private static int mThreadCount;
        private static int mRuntimeSeconds;
        private static bool mUseTieredRuntimes;
        private static bool mLinuxOS;

        private static eProcessingMode mProcessingMode;

        static int Main(string[] args)
        {
            var objParseCommandLine = new clsParseCommandLine();

            try
            {
                mProcessingMode = eProcessingMode.TaskParallelLibrary4_5;

                // Set this to 1 for now
                // If argument /Threads is present, it will be set to that
                // Otherwise, it will be set to value returned by GetCorecount()
                // Hold off calling GetCoreCount() until we have checked for argument /Linux or examined Path.DirectorySeparatorChar
                mThreadCount = 1;

                mRuntimeSeconds = 15;
                mUseTieredRuntimes = false;

                var success = false;

                if (objParseCommandLine.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(objParseCommandLine))
                        success = true;
                }
                else
                {
                    if (objParseCommandLine.NonSwitchParameterCount + objParseCommandLine.ParameterCount == 0 && !objParseCommandLine.NeedToShowHelp)
                    {
                        // No arguments were provided; that's OK
                        success = true;
                    }
                }

                if (!success ||
                    objParseCommandLine.NeedToShowHelp)
                {
                    ShowProgramHelp();
                    return -1;

                }

                StartProcessing();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            return 0;

        }

        /// <summary>
        /// Returns the number of cores
        /// </summary>
        /// <returns>The number of cores on this computer</returns>
        /// <remarks>Should not be affected by hyperthreading, so a computer with two 4-core chips will report 8 cores</remarks>
        private static int GetCoreCount()
        {
            if (mLinuxOS)
            {
                // Assume running on Linux
                return GetCoreCountLinux();
            }

            return GetCoreCountWindows();
        }

        private static int GetCoreCountLinux()
        {
            var linuxSystemInfo = new PRISM.clsLinuxSystemInfo(false);

            var coreCount = linuxSystemInfo.GetCoreCount();

            if (coreCount >= 1)
                return coreCount;

            var cpuInfoFile = new FileInfo(PRISM.clsLinuxSystemInfo.CPUINFO_FILE_PATH);
            if (cpuInfoFile.Exists)
                Console.WriteLine("Core count determined using " + cpuInfoFile.FullName + " is " + coreCount + "; will use 1 thread");
            else
                Console.WriteLine("cpuinfo file not found at: " + cpuInfoFile.FullName + "; will use 1 thread");

            return 1;

        }

        private static int GetCoreCountWindows()
        {
            var coreCount = PRISMWin.clsProcessStats.GetCoreCount();
            if (coreCount >= 1)
                return coreCount;

            Console.WriteLine("Core count reported as " + coreCount + "; will use 1 thread");
            return 1;
        }

        private static void StartProcessing()
        {

            var piApproximator = new clsMonteCarloPiApproximation
            {
                UseTieredRuntimes = mUseTieredRuntimes
            };

            var watch = new Stopwatch();
            watch.Start();

            switch (mProcessingMode)
            {
                case eProcessingMode.Serial:
                    Console.WriteLine("Estimating Pi using {0}", "Serial calculation (single core)");
                    piApproximator.SerialCalculation(mRuntimeSeconds);
                    break;

                case eProcessingMode.ParallelFor:
                    Console.WriteLine("Estimating Pi using {0}, {1} thread", "ParallelFor", mThreadCount);
                    piApproximator.ParallellFor(mRuntimeSeconds, mThreadCount);
                    break;

                case eProcessingMode.TaskParallelLibrary:
                    Console.WriteLine("Estimating Pi using {0}, {1} threads", "Task Parallel Library (Task Factory)", mThreadCount);
                    piApproximator.TaskParallelLibrary40(mRuntimeSeconds, mThreadCount);
                    break;

                case eProcessingMode.TaskParallelLibrary4_5:
                    Console.WriteLine("Estimating Pi using {0}, {1} threads", "Task Parallel Library (No Factory)", mThreadCount);
                    piApproximator.TaskParallelLibrary45(mRuntimeSeconds, mThreadCount);
                    break;

            }

            watch.Stop();

            Console.WriteLine("Done, {0:0.00} seconds elapsed", (watch.ElapsedMilliseconds / 1000.0));
            Console.WriteLine();

        }

        private static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine objParseCommandLine)
        {
            // Returns True if no problems; otherwise, returns false
            var lstValidParameters = new List<string> { "Mode", "Runtime", "Threads", "UseTiered", "Linux" };

            try
            {
                // Make sure no invalid parameters are present
                if (objParseCommandLine.InvalidParametersPresent(lstValidParameters))
                {
                    var badArguments = new List<string>();
                    foreach (var item in objParseCommandLine.InvalidParameters(lstValidParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ShowErrorMessage("Invalid commmand line parameters", badArguments);

                    return false;
                }

                // Could query objParseCommandLine to see if various parameters are present
                //if (objParseCommandLine.NonSwitchParameterCount > 0)
                //{
                //    mFileName = objParseCommandLine.RetrieveNonSwitchParameter(0);
                //}


                var modeValue = 0;
                if (!GetParamInt(objParseCommandLine, "Mode", ref modeValue))
                    return false;

                try
                {
                    if (modeValue > 0)
                        mProcessingMode = (eProcessingMode)(modeValue - 1);
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Invalid value for /Mode; should be /Mode:1 or /Mode:2 or /Mode:3 or /Mode:4" + Environment.NewLine + ex.Message);
                }

                if (!GetParamInt(objParseCommandLine, "Runtime", ref mRuntimeSeconds))
                    return false;

                if (objParseCommandLine.IsParameterPresent("Linux"))
                {
                    mLinuxOS = true;
                }
                else
                {
                    if (Path.DirectorySeparatorChar == '/')
                        mLinuxOS = true;
                }


                if (objParseCommandLine.IsParameterPresent("Threads"))
                {
                    if (!GetParamInt(objParseCommandLine, "Threads", ref mThreadCount))
                        return false;
                }
                else
                {
                    mThreadCount = GetCoreCount();
                }

                mUseTieredRuntimes = objParseCommandLine.IsParameterPresent("UseTiered");

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static bool GetParamInt(clsParseCommandLine objParseCommandLine, string paramName, ref int paramValue)
        {

            string paramValueText;
            if (!objParseCommandLine.RetrieveValueForParameter(paramName, out paramValueText))
            {
                // Leave paramValue unchanged
                return true;
            }

            if (string.IsNullOrWhiteSpace(paramValueText))
            {
                ShowErrorMessage("/" + paramName + " does not have a value; should define the number of threads to use");
                return false;
            }

            // Update paramValue
            if (int.TryParse(paramValueText, out paramValue))
            {
                return true;
            }

            ShowErrorMessage("Error converting " + paramValueText + " to an integer for parameter /" + paramName);
            return false;
        }


        private static void ShowErrorMessage(string strMessage)
        {
            const string strSeparator = "------------------------------------------------------------------------------";

            Console.WriteLine();
            Console.WriteLine(strSeparator);
            Console.WriteLine(strMessage);
            Console.WriteLine(strSeparator);
            Console.WriteLine();

            WriteToErrorStream(strMessage);
        }

        private static void ShowErrorMessage(string strTitle, IEnumerable<string> items)
        {
            const string strSeparator = "------------------------------------------------------------------------------";

            Console.WriteLine();
            Console.WriteLine(strSeparator);
            Console.WriteLine(strTitle);
            var strMessage = strTitle + ":";

            foreach (var item in items)
            {
                Console.WriteLine("   " + item);
                strMessage += " " + item;
            }
            Console.WriteLine(strSeparator);
            Console.WriteLine();

            WriteToErrorStream(strMessage);
        }


        private static void ShowProgramHelp()
        {
            var exeName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                Console.WriteLine();
                Console.WriteLine("This program estimates the value of Pi, using either a single thread or multiple threads");
                Console.WriteLine("This can be used to simulate varying levels of load on a computer");
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + exeName);
                Console.WriteLine(" [/Mode:{1,2,3,4}] [/RunTime:Seconds] [/Threads:ThreadCount] [/UseTiered] [/Linux]");
                Console.WriteLine();
                Console.WriteLine("/Mode:1 is serial calculation (single thread)");
                Console.WriteLine("/Mode:2 uses a Parallel.For loop");
                Console.WriteLine("/Mode:3 uses the Task Parallel Library (TPL) framework, initializing with factories");
                Console.WriteLine("/Mode:4 uses the Task Parallel Library (TPL) framework, but without factories");

                Console.WriteLine();
                Console.WriteLine("Specify the runtime, in seconds, using /RunTime");
                Console.WriteLine();
                Console.WriteLine("Specify the number of threads to use with /Threads");
                Console.WriteLine("If not specified, all cores will be used; " + GetCoreCount() + " on this computer");
                Console.WriteLine();
                Console.WriteLine("Use /UseTiered with modes 2 through 4 to indicate that different threads should run for tiered runtimes (each thread will run for 80% of the length of the previous thread)");
                Console.WriteLine();
                Console.WriteLine("Use /Linux to specify that we're running on a Linux OS and thus core count should be determined using /proc/cpuinfo");
                Console.WriteLine("Linux mode is auto-enabled if the path separator character is a forward slash");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com");
                Console.WriteLine("Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/");
                Console.WriteLine();

                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                System.Threading.Thread.Sleep(750);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error displaying the program syntax: " + ex.Message);
            }

        }

        private static void WriteToErrorStream(string strErrorMessage)
        {
            try
            {
                using (var swErrorStream = new System.IO.StreamWriter(Console.OpenStandardError()))
                {
                    swErrorStream.WriteLine(strErrorMessage);
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Ignore errors here
            }
        }

        static void ShowErrorMessage(string message, bool pauseAfterError)
        {
            Console.WriteLine();
            Console.WriteLine("===============================================");

            Console.WriteLine(message);

            if (pauseAfterError)
            {
                Console.WriteLine("===============================================");
                System.Threading.Thread.Sleep(1500);
            }
        }

    }
}
