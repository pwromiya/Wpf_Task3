namespace Wpf_Task3.Models;

// Entity model representing one database record
public class Record
{
    // Primary key
    public int Id { get; set; }

    // Data fields
    public DateTime RecordDate { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string SurName { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}
