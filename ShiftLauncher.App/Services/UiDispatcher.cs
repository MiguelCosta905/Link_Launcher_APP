using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ShiftLauncher.App.Services;

public sealed class UiDispatcher
{
    public Task InvokeAsync(Action action)
    {
        return Dispatcher.UIThread.InvokeAsync(action).GetTask();
    }
}
