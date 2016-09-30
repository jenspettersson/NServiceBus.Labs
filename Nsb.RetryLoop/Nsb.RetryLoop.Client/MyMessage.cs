using NServiceBus;

namespace Nsb.RetryLoop.Client
{
    public class MyMessage : IEvent
    {
        public string WhatToDo { get; set; }

        public MyMessage(string whatToDo)
        {
            WhatToDo = whatToDo;
        }
    }
}