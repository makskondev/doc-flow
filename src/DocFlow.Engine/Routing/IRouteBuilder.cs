namespace DocFlow.Engine;

public interface IRouteBuilder
{
    IRouteBuilder Step<TStep>() where TStep : IStep;
    /// <summary>Припарковать процесс до сигнала. При истечении таймаута процесс уходит в NeedsAttention.</summary>
    IRouteBuilder WaitFor(string signal, TimeSpan timeout);
    /// <summary>Вставить общий фрагмент маршрута.</summary>
    IRouteBuilder Include(Action<IRouteBuilder> fragment);
}
