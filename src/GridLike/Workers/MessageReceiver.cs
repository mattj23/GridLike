using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GridLike.Workers
{
    public class MessageReceiver : IDisposable
    {
        private readonly WebSocket _socket;
        private volatile bool _isRunning = false;
        private CancellationTokenSource? _cancel;
        private readonly Subject<BaseMessage> _messages;
        private readonly Subject<byte[]> _binaryMessages;

        public MessageReceiver(WebSocket socket)
        {
            _socket = socket;
            _messages = new Subject<BaseMessage>();
            _binaryMessages = new Subject<byte[]>();
        }

        public IObservable<BaseMessage> Messages => _messages.AsObservable();

        public IObservable<byte[]> BinaryMessages => _binaryMessages.AsObservable();

        public async Task Receive()
        {
            var received = new List<byte>();
            var buffer = new byte[1024 * 1024];
            _isRunning = true;

            while (_isRunning)
            {
                WebSocketReceiveResult message;
                _cancel = new CancellationTokenSource();
                try
                {
                    message = await _socket.ReceiveAsync(buffer, _cancel.Token);
                }
                catch (WebSocketException)
                {
                    return;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Receive cancelled");
                    return;
                }
                _cancel.Dispose();

                for (int i = 0; i < message.Count; ++i)
                {
                    received.Add(buffer[i]);
                }

                if (!message.EndOfMessage) continue;

                var bytes = received.ToArray();
                received.Clear();

                if (message.MessageType == WebSocketMessageType.Binary)
                {
                    _binaryMessages.OnNext(bytes);
                }

                if (message.MessageType == WebSocketMessageType.Text)
                {
                    ProcessMessage(Encoding.UTF8.GetString(bytes));
                }
            }
        }

        private void ProcessMessage(string text)
        {
            var baseMessage = JsonConvert.DeserializeObject<BaseMessage>(text);
            BaseMessage? decoded = baseMessage.Code switch
            {
                MessageCode.Register => JsonConvert.DeserializeObject<RegisterMessage>(text),
                MessageCode.Status => JsonConvert.DeserializeObject<StatusMessage>(text),
                MessageCode.Progress => JsonConvert.DeserializeObject<ProgressMessage>(text),
                MessageCode.JobFailed => JsonConvert.DeserializeObject<JobFailedMessage>(text),
                _ => null
            };

            try
            {
                if (decoded is not null)
                    _messages.OnNext(decoded);
            }
            catch (ObjectDisposedException)
            {
                // This can happen when the worker has been kicked and so the messages have been unregistered
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public void Dispose()
        {
            _isRunning = false;
            _messages.Dispose();
            try
            {
                _cancel?.Cancel();
                _cancel?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}