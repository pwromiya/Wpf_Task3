using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Windows;
using Wpf_Task3.Models;

namespace Wpf_Task3
{
    public partial class MainWindow : Window
    {
        // Load all records from DB to DataGrid
        private void LoadRecordsToGrid()
        {
            using (var db = new AppDbContext())
            {
                RecordsGrid.ItemsSource = db.Records.ToList();
            }
        }

        // Application start
        public MainWindow()
        {
            InitializeComponent();

            // Database initialization (create if not exists)
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }

            // Load data after initialization
            LoadRecordsToGrid();
        }

        // Clear all records from database
        private void BtnClearDb_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to delete all entries?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var db = new AppDbContext();

                // Fast full table очистка
                db.Database.ExecuteSqlRaw("TRUNCATE TABLE Records");

                LoadRecordsToGrid(); // Refresh grid
                MessageBox.Show("The database has been successfully cleared.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while clearing database:\n" + ex.Message);
            }
        }

        // Apply all filters using LINQ
        private List<Record> GetFilteredRecords()
        {
            using var db = new AppDbContext();
            var query = db.Records.AsQueryable();

            // Date range filter
            if (DateFrom.SelectedDate != null)
                query = query.Where(r => r.RecordDate >= DateFrom.SelectedDate.Value);

            if (DateTo.SelectedDate != null)
                query = query.Where(r => r.RecordDate <= DateTo.SelectedDate.Value);

            // Text filters
            if (!string.IsNullOrWhiteSpace(TxtFirstName.Text))
                query = query.Where(r => r.FirstName.Contains(TxtFirstName.Text));

            if (!string.IsNullOrWhiteSpace(TxtLastName.Text))
                query = query.Where(r => r.LastName.Contains(TxtLastName.Text));

            if (!string.IsNullOrWhiteSpace(TxtSurName.Text))
                query = query.Where(r => r.SurName.Contains(TxtSurName.Text));

            if (!string.IsNullOrWhiteSpace(TxtCity.Text))
                query = query.Where(r => r.City.Contains(TxtCity.Text));

            if (!string.IsNullOrWhiteSpace(TxtCountry.Text))
                query = query.Where(r => r.Country.Contains(TxtCountry.Text));

            return query.ToList(); // Execute query
        }

        // Select records by filter
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = GetFilteredRecords();
                RecordsGrid.ItemsSource = data;

                if (data.Count == 0)
                    MessageBox.Show("No data was found matching the specified conditions.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sampling error: " + ex.Message);
            }
        }

        // Show all records
        private void BtnShowAll_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            RecordsGrid.ItemsSource = db.Records.ToList();
        }

        // Import CSV file into database
        private async void BtnLoadCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "CSV files (*.csv)|*.csv";

            if (dialog.ShowDialog() != true)
                return;

            string path = dialog.FileName;

            if (!File.Exists(path))
            {
                MessageBox.Show("File not found.");
                return;
            }

            int batchSize = 5000; // Batch insert size
            int total = 0; // Total imported rows

            try
            {
                using (var db = new AppDbContext())
                using (var reader = new StreamReader(path))
                {
                    var batch = new List<Record>();

                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var parts = line.Split(';');
                        if (parts.Length != 6)
                            continue; // Skip invalid rows

                        var rec = new Record
                        {
                            RecordDate = DateTime.Parse(parts[0]),
                            FirstName = parts[1],
                            LastName = parts[2],
                            SurName = parts[3],
                            City = parts[4],
                            Country = parts[5]
                        };

                        batch.Add(rec);
                        total++;

                        // Save batch to DB
                        if (batch.Count >= batchSize)
                        {
                            db.Records.AddRange(batch);
                            await db.SaveChangesAsync();
                            batch.Clear();
                        }
                    }

                    // Save remaining records
                    if (batch.Count > 0)
                    {
                        db.Records.AddRange(batch);
                        await db.SaveChangesAsync();
                    }
                }

                MessageBox.Show($"Import is complete. Records uploaded: {total}");

                LoadRecordsToGrid(); // Refresh grid
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        // Export current grid to Excel
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var data = RecordsGrid.ItemsSource as List<Record>;

            if (data == null || data.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Excel files (*.xlsx)|*.xlsx";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var sheet = workbook.Worksheets.Add("Records");

                // Header row
                sheet.Cell(1, 1).Value = "ID";
                sheet.Cell(1, 2).Value = "Date";
                sheet.Cell(1, 3).Value = "Firstname";
                sheet.Cell(1, 4).Value = "Lastname";
                sheet.Cell(1, 5).Value = "Surname";
                sheet.Cell(1, 6).Value = "City";
                sheet.Cell(1, 7).Value = "Country";

                // Data rows
                int row = 2;
                foreach (var r in data)
                {
                    sheet.Cell(row, 1).Value = r.Id;
                    sheet.Cell(row, 2).Value = r.RecordDate;
                    sheet.Cell(row, 3).Value = r.FirstName;
                    sheet.Cell(row, 4).Value = r.LastName;
                    sheet.Cell(row, 5).Value = r.SurName;
                    sheet.Cell(row, 6).Value = r.City;
                    sheet.Cell(row, 7).Value = r.Country;
                    row++;
                }

                sheet.Columns().AdjustToContents(); // Auto width
                workbook.SaveAs(dialog.FileName);

                MessageBox.Show("Excel export completed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export error:\n" + ex.Message);
            }
        }

        // Export current grid to XML
        private void BtnExportXml_Click(object sender, RoutedEventArgs e)
        {
            var data = RecordsGrid.ItemsSource as List<Record>;

            if (data == null || data.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var root = new System.Xml.Linq.XElement("TestProgram");

                foreach (var r in data)
                {
                    root.Add(new System.Xml.Linq.XElement("Record",
                        new System.Xml.Linq.XAttribute("id", r.Id),
                        new System.Xml.Linq.XElement("Date", r.RecordDate),
                        new System.Xml.Linq.XElement("FirstName", r.FirstName),
                        new System.Xml.Linq.XElement("LastName", r.LastName),
                        new System.Xml.Linq.XElement("SurName", r.SurName),
                        new System.Xml.Linq.XElement("City", r.City),
                        new System.Xml.Linq.XElement("Country", r.Country)
                    ));
                }

                root.Save(dialog.FileName);
                MessageBox.Show("XML export completed successfully.");
            }
            catch (Exception)
            {
                MessageBox.Show("XML export error.");
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Do you really want to exit?",
                "Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown(); // Close the application
            }
        }

    }
}
