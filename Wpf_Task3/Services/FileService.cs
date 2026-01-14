using ClosedXML.Excel;
using System.IO;
using System.Xml.Linq;
using Wpf_Task3.Models;

namespace Wpf_Task3.Services;

/// <summary>
/// Service for handling file import and export operations (CSV, Excel, XML)
/// </summary>
public class FileService
{
    // Centralized header definitions to avoid string duplication
    private readonly string[] _excelHeaders = { "ID", "Date", "First Name", "Last Name", "Patronymic", "City", "Country" };

    /// <summary>
    /// Imports records from a semi-colon separated CSV file
    /// </summary>
    public async Task<List<Record>> ImportCsvAsync(string path, IProgress<int>? progress = null)
    {
        var result = new List<Record>();
        var fileInfo = new FileInfo(path);
        long totalBytes = fileInfo.Length;

        using var reader = new StreamReader(path);
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            var parts = line.Split(';');
            // Basic validation: ensure correct number of columns and valid date format
            if (parts.Length == 6 && DateTime.TryParse(parts[0], out var date))
            {
                result.Add(new Record
                {
                    RecordDate = date,
                    FirstName = parts[1],
                    LastName = parts[2],
                    SurName = parts[3],
                    City = parts[4],
                    Country = parts[5]
                });
            }
            // Report progress based on stream position vs total file size
            progress?.Report((int)((double)reader.BaseStream.Position / totalBytes * 100));
        }
        return result;
    }

    /// <summary>
    /// Exports the current collection to an Excel (.xlsx) file using ClosedXML
    /// </summary>
    public async Task ExportToExcelAsync(string path, List<Record> records)
    {
        await Task.Run(() => {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Records");

                // Populate header row with styling
                for (int i = 0; i < _excelHeaders.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = _excelHeaders[i];
                }
                ws.Range(1, 1, 1, _excelHeaders.Length).Style.Font.Bold = true;

                // Populate data rows
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    ws.Cell(i + 2, 1).Value = r.Id;
                    ws.Cell(i + 2, 2).Value = r.RecordDate;
                    ws.Cell(i + 2, 3).Value = r.FirstName;
                    ws.Cell(i + 2, 4).Value = r.LastName;
                    ws.Cell(i + 2, 5).Value = r.SurName;
                    ws.Cell(i + 2, 6).Value = r.City;
                    ws.Cell(i + 2, 7).Value = r.Country;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(path);
            }
            catch (IOException ex)
            {
                // Handle cases where the file is locked by another process (e.g., open in Excel)
                throw new Exception($"File access denied. Please close the file if it's open in another program.\n{ex.Message}");
            }
        });
    }

    /// <summary>
    /// Exports the current collection to an XML file asynchronously
    /// </summary>
    public async Task ExportToXmlAsync(string path, List<Record> records)
    {
        await Task.Run(async () => {
            var root = new XElement("TestProgram",
                records.Select(r => new XElement("Record",
                    new XAttribute("id", r.Id),
                    new XElement("Date", r.RecordDate.ToString("yyyy-MM-dd")),
                    new XElement("FirstName", r.FirstName),
                    new XElement("LastName", r.LastName),
                    new XElement("SurName", r.SurName),
                    new XElement("City", r.City),
                    new XElement("Country", r.Country)
                ))
            );

            // Use FileStream with async sharing options for safe writing
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await root.SaveAsync(stream, System.Xml.Linq.SaveOptions.None, default);
        });
    }
}