using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GridLike.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GridLike.Workers
{
    /// <summary>
    /// WorkerState represents the server's knowledge of the worker's state.
    ///
    /// Only the Ready and Busy states are based on information self-reported by the worker, the other states are
    /// representations of the connection flow between worker and server.
    /// </summary>
    public enum WorkerState
    {
        WaitingForRegistration,
        Registered,
        Ready,
        Busy,
        Disconnected,
        FailedRegistration
    }
    
    /// <summary>
    /// The Worker class represents a self-contained handle to a remote worker, connected to the server through a
    /// Websocket that is stored internally on Worker creation. Workers are also assigned GUIDs, which are used to
    /// keep track of them by the server.  All communication with a remote worker occurs through this class.
    /// </summary>
    public class Worker : IDisposable
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
        };
            
        private readonly WebSocket _socket;
        private readonly TaskCompletionSource<Guid> _completion;
        private readonly MessageReceiver _receiver;
        private readonly Subject<Guid> _update;
        private readonly IDisposable _messageSubscription;
        private readonly IDisposable _binarySubscription;
        private readonly IWorkerAuthenticator _authenticator;

        public Worker(WebSocket socket, IWorkerAuthenticator authenticator)
        {
            _socket = socket;
            _authenticator = authenticator;
            _completion = new TaskCompletionSource<Guid>();
            _receiver = new MessageReceiver(_socket);
            _update = new Subject<Guid>();

            Id = Guid.NewGuid();
            ConnectedAt = DateTime.UtcNow;
            State = WorkerState.WaitingForRegistration;

            _messageSubscription = _receiver.Messages.Subscribe(ReceiveMessage);
            _binarySubscription = _receiver.BinaryMessages.Subscribe(ReceiveBinary);
        }
        
        /// <summary>
        /// A unique GUID assigned randomly to the worker on object creation immediately after connection to the
        /// server by the remote worker.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// A human friendly name that the worker provides to the server on registration.  Will be null before the
        /// initial registration is received from the worker.
        /// </summary>
        public string? Name { get; private set; }

        /// <summary>
        /// The current state of the worker as tracked by the server. Only the busy and ready states are based on self
        /// reported values.
        /// </summary>
        public WorkerState State { get; private set; }
        
        /// <summary>
        /// Gets an observable which notifies a client that something about the worker's state or information has
        /// changed.  The GUID comes from the worker, allowing clients to subscribe to multiple workers with the
        /// same handler.
        /// </summary>
        public IObservable<Guid> Update => _update.AsObservable();

        /// <summary>
        /// Gets an observable which pushes binary messages received from the client.
        /// </summary>
        public IObservable<Tuple<Guid, byte[]>> BinaryMessages =>
            _receiver.BinaryMessages.Select(b => Tuple.Create(Id, b));

        /// <summary>
        /// Gets a task which completes when the worker disconnects from the server.  Used by the WorkerController to
        /// keep the middleware pipeline open to maintain the websocket.
        /// </summary>
        public Task<Guid> Task => _completion.Task;

        /// <summary>
        /// Gets the time which the worker connected to the server.
        /// </summary>
        public DateTime ConnectedAt { get; }
        
        /// <summary>
        /// Gets the time at which the worker disconnected from the server.  Will be null before the worker
        /// disconnects.
        /// </summary>
        public DateTime? DisconnectedAt { get; private set; }

        public async Task SendBytes(byte[] bytes)
        {
            try
            {
                await _socket.SendAsync(bytes, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage,
                    CancellationToken.None);
            }
            catch (WebSocketException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void SetBusy()
        {
            State = WorkerState.Busy;
            _update.OnNext(Id);
        }

        public async void RequestStatus()
        {
            var text = JsonConvert.SerializeObject(new BaseMessage {Code = MessageCode.StatusRequest}, 
                _serializerSettings);
            Console.WriteLine($"Requesting Status for {Id}");
            
            try
            {
                await _socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, 
                    WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async void StartReceive()
        {
            Console.WriteLine("starting receiver");
            // This method will run until the socket is closed
            await _receiver.Receive();
            
            // Once the socket has closed we can clean up
            _completion.TrySetResult(Id);
            DisconnectedAt = DateTime.UtcNow;
            State = WorkerState.Disconnected;
            SendUpdate();
        }

        public void Dispose()
        {
            _receiver.Dispose();
            _socket.Dispose();
            _messageSubscription.Dispose();
            _binarySubscription.Dispose();
        }

        private async void ReceiveMessage(BaseMessage message)
        {
            if (State == WorkerState.WaitingForRegistration && message is RegisterMessage registerMessage)
            {
                var result = await _authenticator.Authenticate(registerMessage);
                State = result ? WorkerState.Registered : WorkerState.FailedRegistration;
                Name = registerMessage.Name;
                SendUpdate();
            }
            else if (message is StatusMessage statusMessage && State.IsSafe())
            {
                State = statusMessage.Status switch
                {
                    WorkerStatusCode.Busy => WorkerState.Busy,
                    WorkerStatusCode.Ready => WorkerState.Ready,
                    _ => State
                };
                SendUpdate();
            }
        }

        private void ReceiveBinary(byte[] bytes)
        {
            
        }

        private void SendUpdate()
        {
            _update.OnNext(Id);
        }
    }
}