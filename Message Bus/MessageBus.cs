using System;
using System.Collections.Generic;

namespace Slaggy.Messages
{
    public interface IMessage { }

    public class MessageBus
    {
        public class Config
        {
            public static Config DefaultConfig => new Config(true, false);

            public readonly bool immediateDelivery = false;
            public readonly bool verbose = false;

            public Config(bool immediateDelivery, bool verbose)
            {
                this.immediateDelivery = immediateDelivery;
                this.verbose = verbose;
            }
        }

        private static MessageBus _default;
        public static MessageBus Default => _default ?? (_default = new MessageBus(Config.DefaultConfig));

        private readonly Queue<IMessage> _messages = new Queue<IMessage>();

        public delegate void MessageEventHandler<in T>(T delegateEvent) where T : IMessage;
        private delegate void MessageEventDelegate(IMessage delegateMessage);

        private Dictionary<Type, MessageEventDelegate> _delegates = new Dictionary<Type, MessageEventDelegate>();
        private Dictionary<Delegate, MessageEventDelegate> _handlersToDelegates = new Dictionary<Delegate, MessageEventDelegate>();

        private delegate void DeliveryTypeDelegate(IMessage e);
        private DeliveryTypeDelegate _deliveryTypeDelegate;

        public readonly bool immediateDelivery;
        public readonly bool verbose;

        public MessageBus(Config config) : this(config.immediateDelivery, config.verbose) { }

        public MessageBus(bool immediateDelivery = false, bool verbose = false)
        {
            this.immediateDelivery = immediateDelivery;
            this.verbose = verbose;

            if (immediateDelivery)
            {
                if (verbose) Logger.Log("Starting Event Bus with Immediate Delivery of Events.");
                _deliveryTypeDelegate += Deliver;
            }
            else
            {
                if (verbose) Logger.Log("Starting Event Bus with Queued Delivery of Events.");
                _deliveryTypeDelegate += Enqueue;
            }
        }

        ~MessageBus()
        {
            if (immediateDelivery)
                // ReSharper disable once DelegateSubtraction
                _deliveryTypeDelegate -= Deliver;
            // ReSharper disable once DelegateSubtraction
            else _deliveryTypeDelegate -= Enqueue;
        }

        #region Listeners
        /// <summary>
        /// Adds/subscribes a listener (method) <paramref name="handler"/> to listen for (to be triggered by) message <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of message to listen for.</typeparam>
        /// <param name="handler">The handling method.</param>
        public void AddListener<T>(MessageEventHandler<T> handler)
            where T : IMessage
        {
            //This is needed, because two message event handlers (possibly the same method) have an equal reference/value,
            //therefore the dictionary does not store them as separate objects, therefore it is impossible to
            //assign the same event handler over and over again. Possibly could be done with the actual MS EventHandlers.
            if (_handlersToDelegates.ContainsKey(handler))
            {
                Logger.LogWarning("You cannot add the same method/event handler as a listener twice. Please use another way.");
                return;
            }

            if (verbose) Logger.Log("Added listener for: " + typeof(T).Name);

            //Create a new non-generic delegate which calls our handler.
            //This is the delegate we actually invoke.
            MessageEventDelegate internalDelegate = addedDelegate => handler((T)addedDelegate);
            //Define relationship by assigning delegate to handler. Basically keep track of subscriptions
            _handlersToDelegates[handler] = internalDelegate;

            //AddAbility the non-generic delegate to the 'invocation' list, if there are other 
            //delegates present for the event type, add it, otherwise assign it.
            MessageEventDelegate temporaryDelegate;
            if (_delegates.TryGetValue(typeof(T), out temporaryDelegate))
                _delegates[typeof(T)] = temporaryDelegate += internalDelegate;
            else
                _delegates[typeof(T)] = internalDelegate;
        }

        /// <summary>
        /// Removes/unsubscribes a listener (method) <paramref name="handler"/> from message <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of message to stop listening for/unsubscribe.</typeparam>
        /// <param name="handler">The handling method.</param>
        public bool RemoveListener<T>(MessageEventHandler<T> handler)
            where T : IMessage
        {
            //Get the subscribed handler from the 'invocation' list
            MessageEventDelegate internalDelegate;
            if (!_handlersToDelegates.TryGetValue(handler, out internalDelegate)) return false;

            //Get the delegate we're about to edit
            MessageEventDelegate temporaryDelegate;
            if (_delegates.TryGetValue(typeof(T), out temporaryDelegate))
            {
                //Unsubscribe the handler
                temporaryDelegate -= internalDelegate;
                //If there are no more delegates, kill the IEvent Type
                if (temporaryDelegate == null) _delegates.Remove(typeof(T));
                else _delegates[typeof(T)] = temporaryDelegate;
            }

            //Remove handler reference
            return _handlersToDelegates.Remove(handler);
        }

        /// <summary>
        /// Checks if given handler/method is already listening/subscribed.
        /// </summary>
        /// <typeparam name="T">The type of message to listen for.</typeparam>
        /// <param name="handler">The handling method.</param>
        /// <returns>True if it is a listener/is subscribed, false otherwise.</returns>
        public bool IsListener<T>(MessageEventHandler<T> handler)
            where T : IMessage
        {
            return _handlersToDelegates.ContainsKey(handler);
        }

        /// <summary>
        /// Checks if given handler/method is already listening/subscribed. Warning: Generic!
        /// </summary>
        /// <param name="handler">The handling method.</param>
        /// <returns>True if it is a listener/is subscribed, false otherwise.</returns>
        public bool IsListener(Delegate handler)
        {
            //Generic, should not happen often
            if (verbose) Logger.LogWarning("Generic IMessage Handler! Should not happen often (or at all).");
            return _handlersToDelegates.ContainsKey(handler);
        }

        /// <summary>
        /// Checks if given message type <typeparamref name="T"/> has any listeners.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>True if it has any listeners, false otherwise.</returns>
        public bool HasListeners<T>()
            where T : IMessage
        {
            return _delegates.ContainsKey(typeof(T));
        }

        #endregion

        #region IMessage Queueing and Delivery
        /// <summary>
        /// Sends a message to be delivered to subscribed listeners.
        /// </summary>
        /// <param name="sentMessage">The sent message.</param>
        public void Send(IMessage sentMessage) { _deliveryTypeDelegate(sentMessage); }

        /// <summary>
        /// Sends a message  to be delivered to subscribed listeners.
        /// </summary>
        public void Send<T>(params object[] args) where T : IMessage
        {
            _deliveryTypeDelegate((T)Activator.CreateInstance(typeof(T), args));
        }

        private void Enqueue(IMessage enqueuedMessage)
        {
            _messages.Enqueue(enqueuedMessage);
            if (verbose) Logger.Log("Enqueued Event. Event Count: " + _messages.Count);
        }

        //Could also have been generic but typeof(T) is slightly faster than GetType(). ¯\_(ツ)_/¯
        //private void Deliver<T>(T deliveredEvent)
        //    where T : IEvent
        //{
        //    Debug.Log("Yo");
        //    EventDelegate eventDelegate;
        //    if (_delegates.TryGetValue(typeof(T), out eventDelegate))
        //    {
        //        Debug.Log("I am here");
        //        eventDelegate.Invoke(deliveredEvent);
        //    }
        //}

        private void Deliver(IMessage deliveredMessage)
        {
            if (verbose) Logger.Log("Delivered: " + deliveredMessage.GetType().Name);
            if (_delegates.TryGetValue(deliveredMessage.GetType(), out var eventDelegate))
                eventDelegate?.Invoke(deliveredMessage);
        }


        /// <summary>
        /// Delivers the next message in the queue.
        /// </summary>
        public void DeliverNext()
        {
            Deliver(_messages.Dequeue());
            if (verbose) Logger.Log("Delivered. Left: " + _messages.Count);
        }

        /// <summary>
        /// Delivers all events in the queue.
        /// </summary>
        public void DeliverAll()
        {
            while (_messages.Count != 0) DeliverNext();
        }
        #endregion
    }
}