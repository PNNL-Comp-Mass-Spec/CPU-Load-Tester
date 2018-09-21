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
    // E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    // Website: https://omics.pnl.gov or https://panomics.pnnl.gov/
    // -------------------------------------------------------------------------------
    //

    class Program
    {

        public const string PROGRAM_DATE = "September 21, 2018";

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
        private static bool mPreviewMode;

        private static eProcessingMode mProcessingMode;

        static int Main()
        {
            var commandLineParser = new clsParseCommandLine();

            try
            {
                mProcessingMode = eProcessingMode.TaskParallelLibrary4_5;

                // Set this to 1 for now
                // If argument /Threads is present, it will be set to that
                // Otherwise, it will be set to value returned by GetCoreCount()
                mThreadCount = 1;
                mRuntimeSeconds = 15;
                mUseTieredRuntimes = false;

                var success = false;

                if (commandLineParser.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(commandLineParser))
                        success = true;
                }
                else
                {
                    if (commandLineParser.NonSwitchParameterCount + commandLineParser.ParameterCount == 0 && !commandLineParser.NeedToShowHelp)
                    {
                        // No arguments were provided; that's OK
                        success = true;
                    }
                }

                if (!success || commandLineParser.NeedToShowHelp)
                {
                    ShowProgramHelp();
                    return -1;
                }

                StartProcessing();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main", ex);
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
            var coreCount = SystemInfo.GetCoreCount();
            return coreCount;
        }

        private static void StartProcessing()
        {

            var piApproximator = new clsMonteCarloPiApproximation
            {
                UseTieredRuntimes = mUseTieredRuntimes,
                PreviewMode = mPreviewMode
            };

            var watch = new Stopwatch();
            watch.Start();

            var pluralS = mThreadCount > 1 ? "s" : string.Empty;

            if (mPreviewMode)
            {
                Console.WriteLine("Preview of thread{0} that would be used to compute Pi", pluralS);
                Console.WriteLine();
            }

            switch (mProcessingMode)
            {
                case eProcessingMode.Serial:
                    Console.WriteLine("Estimating Pi using {0}", "Serial calculation (single core)");
                    piApproximator.SerialCalculation(mRuntimeSeconds);
                    break;

                case eProcessingMode.ParallelFor:
                    Console.WriteLine("Estimating Pi using {0}, {1} thread{2}", "ParallelFor", mThreadCount, pluralS);
                    piApproximator.ParallellFor(mRuntimeSeconds, mThreadCount);
                    break;

                case eProcessingMode.TaskParallelLibrary:
                    Console.WriteLine("Estimating Pi using {0}, {1} thread{2}", "Task Parallel Library (Task Factory)", mThreadCount, pluralS);
                    piApproximator.TaskParallelLibrary40(mRuntimeSeconds, mThreadCount);
                    break;

                case eProcessingMode.TaskParallelLibrary4_5:
                    Console.WriteLine("Estimating Pi using {0}, {1} thread{2}", "Task Parallel Library (No Factory)", mThreadCount, pluralS);
                    piApproximator.TaskParallelLibrary45(mRuntimeSeconds, mThreadCount);
                    break;

            }

            watch.Stop();

            if (!mPreviewMode)
                Console.WriteLine("Done, {0:0.00} seconds elapsed", watch.ElapsedMilliseconds / 1000.0);

            Console.WriteLine();

        }

        private static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false
            var lstValidParameters = new List<string> { "Mode", "Runtime", "Threads", "UseTiered", "Preview" };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(lstValidParameters))
                {
                    var badArguments = new List<string>();
                    foreach (var item in commandLineParser.InvalidParameters(lstValidParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ShowErrorMessage("Invalid command line parameters", badArguments);

                    return false;
                }

                // Could query commandLineParser to see if various parameters are present
                //if (commandLineParser.NonSwitchParameterCount > 0)
                //{
                //    mFileName = commandLineParser.RetrieveNonSwitchParameter(0);
                //}


                var modeValue = 0;
                if (!GetParamInt(commandLineParser, "Mode", ref modeValue))
                    return false;

                try
                {
                    if (modeValue > 0)
                        mProcessingMode = (eProcessingMode)(modeValue - 1);
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Invalid value for /Mode; should be /Mode:1 or /Mode:2 or /Mode:3 or /Mode:4", ex);
                }

                if (!GetParamInt(commandLineParser, "Runtime", ref mRuntimeSeconds))
                    return false;

                if (commandLineParser.IsParameterPresent("Threads"))
                {
                    if (!GetParamInt(commandLineParser, "Threads", ref mThreadCount))
                        return false;
                }
                else
                {
                    mThreadCount = GetCoreCount();
                }

                mUseTieredRuntimes = commandLineParser.IsParameterPresent("UseTiered");

                mPreviewMode = commandLineParser.IsParameterPresent("Preview");

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static bool GetParamInt(clsParseCommandLine commandLineParser, string paramName, ref int paramValue)
        {
            if (!commandLineParser.RetrieveValueForParameter(paramName, out var paramValueText))
            {
                // Leave paramValue unchanged
                return true;
            }

            if (string.IsNullOrWhiteSpace(paramValueText))
            {
                ShowErrorMessage("/" + paramName + " does not have a value");
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

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowErrorMessage(string title, IEnumerable<string> items)
        {
            ConsoleMsgUtils.ShowErrors(title, items);
        }

        private static void ShowProgramHelp()
        {
            var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "This program estimates the value of Pi, using either a single thread or multiple threads. " +
                                      "This can be used to simulate varying levels of load on a computer"));
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + exeName);
                Console.WriteLine(" [/Mode:{1,2,3,4}] [/RunTime:Seconds] [/Threads:ThreadCount] [/UseTiered] [/Preview]");
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
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Use /UseTiered with modes 2 through 4 to indicate that different threads should run for tiered runtimes " +
                                      "(each thread will run for a shorter time than the previous thread)"));
                Console.WriteLine();
                Console.WriteLine("Use /Preview to preview the threads that would be started");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://omics.pnl.gov or https://panomics.pnnl.gov/");
                Console.WriteLine();

                // Delay for 1 second in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                ConsoleMsgUtils.SleepSeconds(1);

            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax", ex);
            }

        }

    }
}
