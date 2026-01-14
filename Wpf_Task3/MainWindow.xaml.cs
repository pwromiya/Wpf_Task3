using System.Windows;
using Wpf_Task3.ViewModels;

namespace Wpf_Task3;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Following MVVM: The View contains no business logic, only UI initialization
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Constructor with Dependency Injection
    /// </summary>
    /// <param name="vm">The ViewModel resolved by the DI container</param>
    public MainWindow(MainViewModel vm)
    {
        // Standard WPF component initialization
        InitializeComponent();

        // Assign the ViewModel as the DataContext for XAML Data Binding
        DataContext = vm;
    }
}