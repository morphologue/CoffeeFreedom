namespace CoffeeFreedom.Common.Models
{
    public class WorkerResponse : WorkerModelBase
    {
        public WorkerResponse() { }

        public WorkerResponse(WorkStatus status)
        {
            Status = status;
        }

        public WorkStatus Status { get; set; }
        public Progress Progress { get; set; }
    }

    public enum WorkStatus
    {
        /// <summary>The request succeeded.</summary>
        Ok,

        BadLogin,
        CafeClosed,
        
        /// <summary>An new order was attempted while one was already in progress.</summary>
        Conflict,

        /// <summary>Generic system error</summary>
        Error
    }
}
