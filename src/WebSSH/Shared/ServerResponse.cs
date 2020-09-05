namespace WebSSH.Shared
{
    public class ServerResponse<T>
    {
        public StausResult StausResult { get; set; }
        public string ExtraMessage { get; set; }
        public T Response { get; set; }
    }

    public enum StausResult
    {
        Successful,
        Failed,
        Exception
    }

    public class ServerOutput
    {
        public string Output { get; set; }
        public int Lines { get; set; }
    }
}
