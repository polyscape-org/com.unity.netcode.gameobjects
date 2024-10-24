namespace Unity.Multiplayer.Tools.NetStats
{
    internal interface IMetricDispatcher
    {
        void RegisterObserver(IMetricObserver observer);
    }

    internal interface IMetricObserver
    {
        void Observe(IMetricCollection collection);
    }
}
