using System;
using System.Threading.Tasks;

namespace Mail.Extensions
{
    public static class RunCatch
    {
        public static T GetOrDefault<T>(Func<T> func, T defaultValue = default)
        {
            try
            {
                return func();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static async Task<T> GetOrDefault<T>(Task<T> task, T defaultValue = default)
        {
            try
            {
                return await task;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static Task<T> GetOrDefault<T>(Func<Task<T>> func, T defaultValue = default)
        {
            return GetOrDefault(func(), defaultValue);
        }

        public static T GetOrThrow<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void GetOrThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
