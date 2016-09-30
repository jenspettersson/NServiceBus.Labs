using System;
using System.Threading.Tasks;
using NServiceBus;

namespace Nsb.RetryLoop.Client
{
    public class MyMessageHandler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (message.WhatToDo.ToLower() == "fail")
            {
                throw new Exception("I was told to fail! Don't blame me!");
            }

            Console.WriteLine($"Received a non failing message: {message.WhatToDo}");

            return Task.FromResult(0);
        }
    }
}