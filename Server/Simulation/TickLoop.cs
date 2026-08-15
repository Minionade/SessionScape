using SessionScape.Main.Protocol;
using SessionScape.Main.Protocol.Messages;
using SessionScape.Server.Simulation.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SessionScape.Server.Simulation
{
    public class TickLoop
    {
        private const int TICK_INTERVAL_MS = 600;

        public static System.Action<long> OnTick;

        private readonly ConcurrentQueue<PendingAction> _queue = new();
        private readonly MessageHandlerRegistry _registry;
        private long _currentTick = 0;

        public TickLoop(MessageHandlerRegistry registry)
        {
            _registry = registry;
        }

        public void Enqueue(PendingAction action) => _queue.Enqueue(action);

        public void Start()
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TICK_INTERVAL_MS));

            _ = RunAsync(timer);
        }
        
        private async Task RunAsync(PeriodicTimer timer)
        {
            while (await timer.WaitForNextTickAsync())
            {
                ProcessTick();
            }
        }

        private void ProcessTick()
        {
            _currentTick++;

            OnTick?.Invoke(_currentTick);

            var actionsThisTick = new List<PendingAction>();

            while(_queue.TryDequeue(out var action))
            {
                actionsThisTick.Add(action);

                try
                {
                    if (!_registry.TryHandle(action, _currentTick))
                    {
                        Console.WriteLine($"[Tick {_currentTick}] No handler for {action.Envelope.Type}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Tick {_currentTick}] Handler threw: {ex}");
                }
            }
        }

    }
}