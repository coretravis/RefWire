namespace ListServDB.Persistence.Retry;

public static class AsyncRetryHelper
{
    /// <summary>
    /// Executes an asynchronous function with retry logic.
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, int delayMilliseconds = 500)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception)
            {
                attempt++;
                if (attempt >= maxRetries)
                {
                    throw;
                }

                await Task.Delay(delayMilliseconds);
            }
        }
    }
}
