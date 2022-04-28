# if UNITY_5_3_OR_NEWER
using Slaggy.Messages;
using Slaggy.Unity.Singletons;

namespace Slaggy.Unity.Messages
{
    public class MessagesBusEventDelivery : SingletonStandalone<MessagesBusEventDelivery>
    {
        private void Start()
        {
            if (!MessageBus.Default.immediateDelivery) return;

            // Debug.LogWarningFormat( "Warning: {0} is only needed when the " +
            //                         "static message bus's delivery is not immediate! " +
            //                         "Disabling component.", this);

            enabled = false;
            Destroy(gameObject);
        }

        private void Update() => MessageBus.Default.DeliverAll();
    }
}
#endif