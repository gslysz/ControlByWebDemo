using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ControlByWebDemo.Utils
{
       public class DebugUtils
    {
        #region Methods

        public static void Write(string outputString, bool addMethodName = true, bool addCallingMethodName = false, bool addDateTimeTagToOutputFilename = false)
        {
            Write(0, outputString, addMethodName, addCallingMethodName, addDateTimeTagToOutputFilename, false);
        }

        public static void Write(int counterNum, string outputString, bool addMethodName = true, bool addCallingMethodName = false, bool addDateTimeTagToOutputFilename = false, bool addElapsedInfo = true)
        {
            IncreaseCounter(counterNum);

            var currentTime = DateTime.Now;
            TimeSpan elapsed = currentTime - LastTimes[counterNum];



            AddElapsedTime(counterNum, elapsed);

            LastTimes[counterNum] = currentTime;

            if (string.IsNullOrEmpty(_outputFilePath))
                _outputFilePath = CreateOutputFile(addDateTimeTagToOutputFilename);

            string methodInfo = "";

            if (addMethodName)
            {
                StackTrace stackTrace = new StackTrace();

                bool callByOverload = counterNum == 0;

                int stackTraceFrame = callByOverload ? 2 : 1;
                
                var method = stackTrace.GetFrame(stackTraceFrame++).GetMethod();

                var className = method.ReflectedType?.Name;

                var methodName2 = "";

                if (addCallingMethodName)
                    methodName2 = $"CallingMethod={stackTrace.GetFrame(stackTraceFrame).GetMethod().Name}";

                methodInfo = $"{className}.{method.Name}() - {methodName2}";
            }

            string elapsedInfo = "";
            if (addElapsedInfo)
            {
                elapsedInfo = $"ElapsedMs=\t{elapsed.TotalMilliseconds:f1}";
            }

            string prefix = $"{DateTime.Now:HH:mm:ss.fff} - GWS -";

            string output = $"{prefix}\t{elapsedInfo}\t{methodInfo}\t{outputString}";

            _outputQueue.Enqueue(output);

            WriteQueue();
        }

        /// <summary>
        /// Returns the average elapsed measurement (in milliseconds)
        /// </summary>
        /// <param name="counter"></param>
        /// <returns></returns>
        public static double GetAverageElapsedSinceLast(int counter)
        {
            if (ElapsedTimes[counter]?.Count > 0)
            {
                double averageTimespan = ElapsedTimes[counter].Select(p => p.TotalMilliseconds).Average();
                return averageTimespan;
            }

            return 0.0;
        }

        public static string CreateOutputFile(bool addDateTimeTagToFileName = false)
        {
            string baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Cadwell", "Temp2");
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }

            string fileTag = "";
            if (addDateTimeTagToFileName)
                fileTag = $"_{DateTime.Now:HHmmss-fff}";
            string fileName = $"RA_DebugOutput{fileTag}.txt";

            string outputFilePath = Path.Combine(baseFolder, fileName);
            return outputFilePath;
        }

        public static void IncreaseCounter(int i)
        {
            Counters[i]++;
        }

        public static TimeSpan GetElapsed(int counter)
        {
            return DateTime.Now - LastTimes[counter];
        }

        public static void OutputDataToTextFile<T>(string outputFileName, List<IReadOnlyList<T>> dataList, List<string> headers, bool outputAsShort = false, double voltsPerBit = 1e-6)
        {
            char delimiter = '\t';
            using (StreamWriter writer = new StreamWriter(outputFileName))
            {
                int numColumns = dataList.Count;

                Type dataType = dataList.First().GetType();

                int numRows = dataList.First().Count;

                StringBuilder sb = new StringBuilder();

                //write the header line
                for (int colIndex = 0; colIndex < numColumns; colIndex++)
                {
                    sb.Append(headers[colIndex]);
                    sb.Append(delimiter);
                }

                writer.WriteLine(sb.ToString().TrimEnd(delimiter));
                sb.Clear();

                //write the data
                sb = new StringBuilder();
                for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < numColumns; colIndex++)
                    {
                        if (dataType == typeof(float))
                        {
                            var value = Convert.ToSingle(dataList[colIndex][rowIndex]);
                            if (outputAsShort)
                            {
                                short valueAsShort = (short)Math.Round(value / voltsPerBit);
                                sb.Append(valueAsShort);
                            }
                            else
                            {
                                sb.Append(value);
                            }
                        }
                        else
                        {
                            sb.Append(dataList[colIndex][rowIndex]);
                        }

                        sb.Append(delimiter);
                    }

                    writer.WriteLine(sb.ToString().TrimEnd(delimiter));
                    sb.Clear();
                }
            }
        }

        #endregion

        #region Non-Public Methods

        private static void AddElapsedTime(int counterNum, TimeSpan timeSpan)
        {
            if (ElapsedTimes[counterNum] == null)
                ElapsedTimes[counterNum] = new LimitedSizeStack<TimeSpan>(1000);

            bool initialTimeSpan = timeSpan > TimeSpan.FromDays(100);
            if (initialTimeSpan)
                return;

            ElapsedTimes[counterNum].Push(timeSpan);
        }

        private static void WriteQueue()
        {
            try
            {
                using (var sw = new StreamWriter(new FileStream(_outputFilePath, FileMode.Append, FileAccess.Write, FileShare.Read)))
                {
                    sw.AutoFlush = true;

                    while (_outputQueue.TryPeek(out _))
                    {
                        var success = _outputQueue.TryDequeue(out string result);
                        if (success)
                            sw.WriteLine(result);
                    }

                    sw.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.Write($"DebugUtils error. {ex.GetBaseException()}");
            }
        }

        #endregion

        #region Fields

        public static int[] Counters = new int[10];
        public static LimitedSizeStack<TimeSpan>[] ElapsedTimes = new LimitedSizeStack<TimeSpan>[10];
        public static DateTime[] LastTimes = new DateTime[10];
        private static readonly ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();
        private static string _outputFilePath;

        #endregion
    }

    public class LimitedSizeStack<T> : LinkedList<T>
    {
        #region Constructors And Destructors

        public LimitedSizeStack(int maxSize)
        {
            _maxSize = maxSize;
        }

        #endregion

        #region Methods

        public void Push(T item)
        {
            AddFirst(item);

            if (Count > _maxSize)
                RemoveLast();
        }

        public T Pop()
        {
            var item = First.Value;
            RemoveFirst();
            return item;
        }

        #endregion

        #region Fields

        private readonly int _maxSize;

        #endregion
    }
}
