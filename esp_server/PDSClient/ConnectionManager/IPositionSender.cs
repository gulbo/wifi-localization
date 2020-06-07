namespace PDSClient.ConnectionManager
{
    public interface IPositionSender
    {
        void WaitAll();
        void WaitAny();
    }
}
