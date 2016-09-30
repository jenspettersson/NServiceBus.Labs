using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace Nsb.RetryLoop.Client
{
    public class LegacySatellite : Feature
    {
        public LegacySatellite()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddSatelliteReceiver(
                name: ApplicationConstants.LegacyQueue,
                transportAddress: ApplicationConstants.LegacyQueue,
                requiredTransportTransactionMode: TransportTransactionMode.TransactionScope,
                runtimeSettings: PushRuntimeSettings.Default, 
                recoverabilityPolicy: (config, errorContext) => MoveToError(config),
                onMessage: OnMessage);
        }

        private static MoveToError MoveToError(RecoverabilityConfig config)
        {
            return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
        }

        private Task OnMessage(IBuilder builder, MessageContext messageContext)
        {
            var receivedString = Encoding.UTF8.GetString(messageContext.Body);

            var targetMessage = new MyMessage(receivedString);
            var s = JsonConvert.SerializeObject(targetMessage);
            var bytes = Encoding.UTF8.GetBytes(s);

            messageContext.Headers.Add(Headers.EnclosedMessageTypes, targetMessage.GetType().FullName);
            
            //Uncomment this to prevent a retried message from ServicePulse to get stuck in a loop
            //messageContext.Headers.Add(Headers.CorrelationId, messageContext.MessageId);

            var outgoingMessage = new OutgoingMessage(messageContext.MessageId, messageContext.Headers, bytes);
            var outgoingOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(ApplicationConstants.EndpointName));

            var dispatcher = builder.Build<IDispatchMessages>();

            return dispatcher.Dispatch(
                new TransportOperations(outgoingOperation),
                messageContext.TransportTransaction,
                messageContext.Context);
        }
    }


}