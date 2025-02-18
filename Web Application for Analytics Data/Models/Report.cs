namespace Web_Application_for_Analytics_Data.Models;

public class Report
{
	public string ReportId { get; set; }
	public DateTime CreationDate { get; set; }
	public DateTime CompletionDate { get; set; }
	public DateTime ReportStartDate { get; set; }
	public DateTime ReportEndDate { get; set; }
	public string Accounts { get; set; }
	public string AccountMembers {get; set;}
}

