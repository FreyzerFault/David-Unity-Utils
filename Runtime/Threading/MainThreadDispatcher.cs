using System;
using System.Collections.Concurrent;
using DavidUtils.Utils;

namespace DavidUtils.Threading
{
	// Guarda acciones para ejecutarlas en el hilo principal en cola FIFO
	public class MainThreadDispatcher : SingletonPersistent<MainThreadDispatcher>
	{
		private const int MaxLowPriorityActionsPerFrame = 10;
		private static readonly ConcurrentQueue<Action> Actions = new();
		private static readonly ConcurrentQueue<Action> LowPriorityActions = new();

		private void Update()
		{
			while (Actions.TryDequeue(out Action action)) action.Invoke();

			for (var i = 0; i < MaxLowPriorityActionsPerFrame; i++)
				if (LowPriorityActions.TryDequeue(out Action lowPriorityAction))
					lowPriorityAction.Invoke();
				else
					break;
		}

		public static void Dispatch(Action action) => Actions.Enqueue(action);

		public static void DispatchLowPriority(Action action) => LowPriorityActions.Enqueue(action);
	}
}
