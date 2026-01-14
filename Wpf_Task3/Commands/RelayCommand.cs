using System.Windows;
using System.Windows.Input;

namespace Wpf_Task3.Commands;

/// <summary>
/// A standard command implementation for synchronous actions
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Determines whether the command can execute in its current state
    public bool CanExecute(object? parameter)
        => _canExecute == null || _canExecute(parameter);

    // Executes the synchronous logic
    public void Execute(object? parameter)
        => _execute(parameter);

    // Forces the UI to re-check if the command can be executed
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

/// <summary>
/// An ICommand implementation designed for Task-based asynchronous operations
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Action<Exception>? _onException;

    public AsyncRelayCommand(
        Func<object?, Task> execute,
        Func<object?, bool>? canExecute = null,
        Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException;
    }

    public bool CanExecute(object? parameter)
        => _canExecute == null || _canExecute(parameter);

    // Safely executes the task and handles potential exceptions
    public async void Execute(object? parameter)
    {
        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            // Use custom handler if provided, otherwise show a system dialog
            if (_onException != null)
            {
                _onException(ex);
            }
            else
            {
                MessageBox.Show(
                    $"Command execution error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}