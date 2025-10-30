namespace webapi.Utilities;

public class DelayedQueue<T>(int ms) : IDisposable
{
    private Queue<T> _queue = new();
    private Timer? _timer = null;
    private TaskCompletionSource<Queue<T>>? _completion = null;
    private CancellationTokenSource _cts = new ();

    public void Enqueue(T obj)
    {
        if (_cts.Token.IsCancellationRequested) return;
        lock (_queue)
        {
            _queue.Enqueue(obj);
            if (_timer == null)
            {
                var cb = new TimerCallback(_ =>
                {
                    lock (_queue)
                    {
                        _timer!.Dispose();
                        _timer = null;
                        if (_completion != null)
                        {
                            var completion = _completion;
                            _completion = null;
                            var queue = _queue;
                            _queue = new Queue<T>();
                            
                            completion.SetResult(queue);
                        }

                    }
                });

                _timer = new Timer(cb, null, ms, Timeout.Infinite);
            }
        }
    }

    public async IAsyncEnumerable<Queue<T>> GetStream()
    {
        while (true)
        {
            Task<Queue<T>> task;
            lock (_queue)
            {
                _completion ??= new TaskCompletionSource<Queue<T>>();
                task = _completion.Task;
            }

            var res = await task;
            if (_cts.Token.IsCancellationRequested) break;
            yield return res;
        }
        
        
    }
    

    public void Dispose()
    {
        lock (_queue)
        {
            _queue.Clear();
            _completion = null;
            _cts.Cancel();
            _timer?.Dispose();
        }
    }
}