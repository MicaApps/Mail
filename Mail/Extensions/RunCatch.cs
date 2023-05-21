using System;

namespace Mail.Extensions
{
    public static class RunCatch
    {
        public static T GetOrDefault<T>(Func<T> func, T defaultValue = default)
        {
            try
            {
                return func.Invoke();
            } catch 
            { 
                return defaultValue; 
            }
        }

        public static T GetOrThrow<T>(Func<T> func)
        {
            try
            {
                return func.Invoke();
            } catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void GetOrThrow(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
