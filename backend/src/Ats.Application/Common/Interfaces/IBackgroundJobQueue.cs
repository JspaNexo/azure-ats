namespace Ats.Application.Common.Interfaces;

public interface IBackgroundJobQueue
{
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
}
