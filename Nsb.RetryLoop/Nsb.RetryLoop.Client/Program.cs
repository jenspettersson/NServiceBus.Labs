using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using RabbitMQ.Client.Events;

namespace Nsb.RetryLoop.Client
{
    class ApplicationConstants
    {
        public static string EndpointName = "LAB.Endpoint";
        public static string ErrorQueue = "LAB.Endpoint.Error";
        public static string LegacyQueue = "LAB.LegacyQueue";
    }

    class Program
    {
        static void Main(string[] args)
        {
            var instance = new Runner().Start().Result;

            Console.WriteLine("RUNNING!");

            Console.WriteLine($"Publish a string to the queue '{ApplicationConstants.LegacyQueue}' - 'fail' to make it throw or any thing else to succeed");
            while (true)
            {
                Thread.Sleep(500);
                
                Console.Write("...or write a string here to publish a normal NSB event: ");
                var readLine = Console.ReadLine();

                instance.Publish(new MyMessage(readLine));
            }
        }

        public class Runner
        {
            public async Task<IEndpointInstance> Start()
            {
                var endpointConfiguration = new EndpointConfiguration("LAB.Endpoint");
                endpointConfiguration.SendFailedMessagesTo(ApplicationConstants.ErrorQueue);
                endpointConfiguration.UsePersistence<InMemoryPersistence>();
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.UseSerialization<JsonSerializer>();

                var recoverability = endpointConfiguration.Recoverability();

                recoverability.DisableLegacyRetriesSatellite();
                recoverability.Immediate(x => x.NumberOfRetries(1));
                recoverability.Delayed(x =>
                {
                    x.NumberOfRetries(1);
                    x.TimeIncrease(TimeSpan.FromSeconds(5));
                });

                var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                transport.ConnectionStringName("NServiceBus.RabbitMQ.ConnectionString");
                transport.CustomMessageIdStrategy(CustomIdStrategy);
                
                var endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
                return endpoint;
            }
            
            private static string CustomIdStrategy(BasicDeliverEventArgs arg)
            {
                var properties = arg.BasicProperties;

                if (!properties.IsMessageIdPresent() || string.IsNullOrWhiteSpace(properties.MessageId))
                {
                    return Guid.NewGuid().ToString();
                    //throw new InvalidOperationException("A non-empty 'message-id' property is required when running NServiceBus on top of RabbitMQ. If this is an interop message, then set the 'message-id' property before publishing the message");
                }

                return arg.BasicProperties.MessageId;
            }
        }
    }
}
