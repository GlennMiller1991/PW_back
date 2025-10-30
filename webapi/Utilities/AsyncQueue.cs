namespace webapi.Utilities;

public class AsyncQueue
{
    private volatile bool _shouldStop;
    private volatile bool _isAwakeRequested;
    private volatile bool _isWaitingForLock;
    private readonly  Lock _addWork = new ();
    
    private readonly Queue<(Action action, Task completion)> _taskQueue = new();
    private readonly AutoResetEvent _signal = new(false);
    public Thread Thread { get; }

    public AsyncQueue()
    {
        Thread = new Thread(ProcessingItself);
        Thread.Start();
    }


    private int WorkQty
    {
        get
        {
            lock (_taskQueue) return _taskQueue.Count;
        }
    }

    private bool IsThereWork
    {
        get
        {
            _isWaitingForLock = true;
            var b = Convert.ToBoolean(WorkQty);
            _isWaitingForLock = false;
            return b;
        }
    }

    public Task AddWork(Action action)
    {
        var completion = new Task(() => { });
        lock (_addWork)
        {
            lock (_taskQueue)
            {
                _taskQueue.Enqueue((action, completion));
            }

            Awake();
        }

        return completion;
    }


    private void Awake()
    {
        if (Thread.ThreadState != ThreadState.WaitSleepJoin || _isWaitingForLock || _isAwakeRequested) return;
        _isAwakeRequested = true;
        _signal.Set();

    }

    public void KillSafe()
    {
        _shouldStop = true;
        Awake();
    }

    private void ProcessingItself()
    { 
        while (true)
        {
            if (IsThereWork)
            {
                Action action;
                Task completion;
                lock (_taskQueue)
                {
                    (action, completion) = _taskQueue.Dequeue();
                }

                action();
                completion.Start();
            }
            else
            {
                if (_shouldStop)
                    break;

                _signal.WaitOne();
                _isAwakeRequested = false;
            }
        }
    }
   
}