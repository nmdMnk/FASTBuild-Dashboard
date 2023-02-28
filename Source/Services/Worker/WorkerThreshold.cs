using System.Collections.ObjectModel;

namespace FastBuild.Dashboard.Services.Worker
{
    internal class WorkerThreshold
    {
        public static ReadOnlyCollection<int> ThresholdValues = new ReadOnlyCollection<int>(new int[]{ 10, 20, 30, 40, 50 });

        public static bool IsValueValid(int value) => ThresholdValues.Contains(value);
    }
}
