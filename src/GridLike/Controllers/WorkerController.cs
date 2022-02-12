using System.Net.WebSockets;
using GridLike.Services;
using Microsoft.AspNetCore.Mvc;

namespace GridLike.Controllers
{
    [Route("api/worker")]
    public class WorkerController : ControllerBase
    {
        private readonly WorkerManager _workers;
        private readonly ILogger<WorkerController> _logger;

        public WorkerController(WorkerManager workers, ILogger<WorkerController> logger)
        {
            _workers = workers;
            _logger = logger;
        }

        [HttpGet]
        public async Task Index()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var r = HttpContext.WebSockets.WebSocketRequestedProtocols;
                _logger.LogDebug("Opening middleware on websocket from {0}", HttpContext.Connection.RemoteIpAddress);
                using WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var closed = _workers.NewWorker(socket);
                await closed;
                _logger.LogDebug("Closing middleware on websocket from {0}", HttpContext.Connection.RemoteIpAddress);
            }
        }
    }
}