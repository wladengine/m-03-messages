namespace PhotoAndCopyShop.ScanWorker.Handlers;

public abstract class HandlerBase<T> : IHandler<T>
{
    private IHandler<T> _next;

    public IHandler<T> SetNext(IHandler<T> handler)
    {
        _next = handler;
        return handler;
    }

    public virtual async Task HandleAsync(T request)
    {
        if (_next != null)
        {
            await _next.HandleAsync(request);
        }
    }
}