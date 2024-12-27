namespace OrderManagement.Services
{
    public class SystemStatusService
    {
        private bool _isAdminProcessing = false;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async Task SetAdminProcessing(bool status)
        {
            await _semaphore.WaitAsync();
            try
            {
                _isAdminProcessing = status;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> IsAdminProcessing()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _isAdminProcessing;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
