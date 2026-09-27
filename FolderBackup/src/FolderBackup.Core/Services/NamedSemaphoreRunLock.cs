using FolderBackup.Core.Constants;

namespace FolderBackup.Core.Services;

/// <summary>
/// Uses a named semaphore so the tray app and a scheduled run never copy at the same time.
/// A semaphore (unlike a mutex) isn't tied to a thread, so it works across await points.
/// </summary>
public sealed class NamedSemaphoreRunLock : IBackupRunLock
{
    public IDisposable? TryAcquire()
    {
        var semaphore = new Semaphore(initialCount: 1, maximumCount: 1, AppConstants.RunLockName);
        if (semaphore.WaitOne(TimeSpan.Zero))
        {
            return new Releaser(semaphore);
        }

        semaphore.Dispose();
        return null;
    }

    private sealed class Releaser : IDisposable
    {
        private Semaphore? _semaphore;

        public Releaser(Semaphore semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _semaphore, null);
            if (semaphore is null)
            {
                return;
            }

            semaphore.Release();
            semaphore.Dispose();
        }
    }
}
