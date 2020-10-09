using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerService.BFSW
{
    public class EventQueueOverflowException : Exception
    {
        public EventQueueOverflowException()
            : base() { }

        public EventQueueOverflowException(string message)
            : base(message) { }
    }
}
