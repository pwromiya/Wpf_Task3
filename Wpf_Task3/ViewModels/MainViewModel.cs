using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Wpf_Task3.Commands;
using Wpf_Task3.Models;
using Wpf_Task3.Services;

namespace Wpf_Task3.ViewModels;

/// <summary>
/// Main ViewModel that coordinates data operations and UI logic
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IDataService _dataService;
    private readonly FileService _fileService;

    // Filter fields and UI state properties
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _surName = string.Empty;
    private string _city = string.Empty;
    private string _country = string.Empty;
    private int _importProgress;
    private string _progressText = "Ready";

    // Observable collection for data binding to the UI grid
    public ObservableCollection<Record> Records { get; } = new();

    #region Properties
    public DateTime? DateFrom { get => _dateFrom; set { _dateFrom = value; OnPropertyChanged(nameof(DateFrom)); } }
    public DateTime? DateTo { get => _dateTo; set { _dateTo = value; OnPropertyChanged(nameof(DateTo)); } }
    public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(nameof(FirstName)); } }
    public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(nameof(LastName)); } }
    public string SurName { get => _surName; set { _surName = value; OnPropertyChanged(nameof(SurName)); } }
    public string City { get => _city; set { _city = value; OnPropertyChanged(nameof(City)); } }
    public string Country { get => _country; set { _country = value; OnPropertyChanged(nameof(Country)); } }
    public int ImportProgress { get => _importProgress; set { _importProgress = value; OnPropertyChanged(nameof(ImportProgress)); } }
    public string ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(nameof(ProgressText)); } }
    #endregion

    // Commands for UI interactions
    public ICommand LoadCsvCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportXmlCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand ClearDbCommand { get; }
    public ICommand ShowAllCommand { get; }
    public ICommand ExitCommand { get; }

    public MainViewModel(IDataService dataService, FileService fileService)
    {
        _dataService = dataService;
        _fileService = fileService;

        // Command initializations
        LoadCsvCommand = new AsyncRelayCommand(async _ => await ImportCsvAsync());
        ExportExcelCommand = new AsyncRelayCommand(async _ => await ExportExcelAsync());
        ExportXmlCommand = new AsyncRelayCommand(async _ => await ExportXmlAsync());
        SelectCommand = new AsyncRelayCommand(async _ => await ApplyFiltersAsync());
        ClearDbCommand = new AsyncRelayCommand(async _ => await ClearDbAsync());
        ShowAllCommand = new AsyncRelayCommand(async _ => await LoadAllAsync());
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

        // Initial data load
        _ = LoadAllAsync();
    }

    // Fetches all records from the database
    private async Task LoadAllAsync()
    {
        ProgressText = "Loading data...";
        var data = await _dataService.GetAllAsync();
        UpdateUI(data);
        ProgressText = "Ready";
    }

    // Applies search filters and updates the UI collection
    private async Task ApplyFiltersAsync()
    {
        var data = await _dataService.FilterAsync(DateFrom, DateTo, FirstName, LastName, SurName, City, Country);
        UpdateUI(data);
    }

    // Handles CSV file selection and database import
    private async Task ImportCsvAsync()
    {
        var dialog = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var progress = new Progress<int>(v => ImportProgress = v);
            ProgressText = "Reading file...";
            var records = await _fileService.ImportCsvAsync(dialog.FileName, progress);

            ProgressText = "Saving to database...";
            await _dataService.AddRangeAsync(records, progress);

            await LoadAllAsync();
            ProgressText = $"Imported {records.Count} records successfully!";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ProgressText = "Ready";
        }
    }

    // Handles Excel export with visual status updates
    private async Task ExportExcelAsync()
    {
        var dialog = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog() != true) return;

        ProgressText = "Exporting to Excel...";
        try
        {
            await _fileService.ExportToExcelAsync(dialog.FileName, Records.ToList());
            MessageBox.Show("Excel Export Done", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            ProgressText = "Ready";
        }
    }

    // Handles XML export with visual status updates
    private async Task ExportXmlAsync()
    {
        var dialog = new SaveFileDialog { Filter = "XML (*.xml)|*.xml" };
        if (dialog.ShowDialog() != true) return;

        ProgressText = "Exporting to XML...";
        try
        {
            await _fileService.ExportToXmlAsync(dialog.FileName, Records.ToList());
            MessageBox.Show("XML Export Done", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            ProgressText = "Ready";
        }
    }

    // Clears the database after user confirmation
    private async Task ClearDbAsync()
    {
        if (MessageBox.Show("Delete all records from database?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await _dataService.ClearAsync();
            await LoadAllAsync();
        }
    }

    // Helper method to refresh the ObservableCollection
    private void UpdateUI(List<Record> data)
    {
        Records.Clear();
        foreach (var r in data) Records.Add(r);
    }

    // Implementation of INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}