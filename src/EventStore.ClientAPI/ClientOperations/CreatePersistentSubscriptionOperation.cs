using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class CreatePersistentSubscriptionOperation : OperationBase<PersistentSubscriptionCreateResult, ClientMessage.CreatePersistentSubscriptionCompleted>
    {
        private readonly string _stream;
        private readonly string _groupName;
        private readonly bool _resolveLinkTos;
        private readonly bool _startFromBeginning;
        private readonly int _messageTimeoutMilliseconds;
        private bool _latencyTracking;

        public CreatePersistentSubscriptionOperation(ILogger log,
                                       TaskCompletionSource<PersistentSubscriptionCreateResult> source,
                                       string stream,
                                       string groupName,
                                       PersistentSubscriptionSettings settings,
                                       UserCredentials userCredentials)
            : base(log, source, TcpCommand.CreatePersistentSubscription, TcpCommand.CreatePersistentSubscriptionCompleted, userCredentials)
        {
            Ensure.NotNull(settings, "settings");
            _resolveLinkTos = settings.ResolveLinkTos;
            _stream = stream;
            _groupName = groupName;
            _startFromBeginning = settings.StartFromBeginning;
            _latencyTracking = settings.LatencyStatistics;
            _messageTimeoutMilliseconds = (int) settings.MessageTimeout.TotalMilliseconds;
        }

        protected override object CreateRequestDto()
        {
            return new ClientMessage.CreatePersistentSubscription(_groupName, _stream, _resolveLinkTos, _startFromBeginning, _messageTimeoutMilliseconds, _latencyTracking);
        }

        protected override InspectionResult InspectResponse(ClientMessage.CreatePersistentSubscriptionCompleted response)
        {
            switch (response.Result)
            {
                case ClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.Success:
                    Succeed();
                    return new InspectionResult(InspectionDecision.EndOperation, "Success");
                case ClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.Fail:
                    Fail(new InvalidOperationException(String.Format("Subscription group {0} on stream {1} failed '{2}'", _groupName, _stream, response.Reason)));
                    return new InspectionResult(InspectionDecision.EndOperation, "Fail");
                case ClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.AccessDenied:
                    Fail(new AccessDeniedException(string.Format("Write access denied for stream '{0}'.", _stream)));
                    return new InspectionResult(InspectionDecision.EndOperation, "AccessDenied");
                case ClientMessage.CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult.AlreadyExists:
                    Fail(new InvalidOperationException(String.Format("Subscription group {0} on stream {1} alreay exists", _groupName, _stream)));
                    return new InspectionResult(InspectionDecision.EndOperation, "AlreadyExists");
                default:
                    throw new Exception(string.Format("Unexpected OperationResult: {0}.", response.Result));
            }
        }

        protected override PersistentSubscriptionCreateResult TransformResponse(ClientMessage.CreatePersistentSubscriptionCompleted response)
        {

            return new PersistentSubscriptionCreateResult(PersistentSubscriptionCreateStatus.Success);
        }

        public override string ToString()
        {
            return string.Format("Stream: {0}, Group Name: {1}", _stream, _groupName);
        }
    }
}