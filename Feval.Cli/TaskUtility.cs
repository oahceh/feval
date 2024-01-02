namespace Feval.Cli
{
    internal static class TaskUtility
    {
        public static Task WaitUntil(Func<bool> func)
        {
            return Task.Run(() =>
            {
                while (!func())
                {
                }
            });
        }

        public static Task WaitWhile(Func<bool> func)
        {
            return Task.Run(() =>
            {
                while (func())
                {
                }
            });
        }
    }
}