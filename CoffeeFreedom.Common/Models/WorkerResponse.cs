using System;

namespace CoffeeFreedom.Common.Models
{
    public class WorkerResponse : WorkerModelBase
    {
        public WorkStatus Status { get; set; }
        public int? QueueLength { get; set; }
        public int? QueuePosition { get; set; }
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
