using System.ComponentModel.DataAnnotations;
using X.PagedList;

namespace Web_Application_for_Analytics_Data.Models;

public class Filter
{
	/*
	 * We are telling the application:
	 * - How to show the information
	 * - What type of field is
	 * - If it can be null or not
	 */
	[Display(Name = "Start Date")]                   
	[DataType(DataType.Date)]                       
	public DateTime? StartDate { get; set; }        
	[Display(Name = "End Date")]
	[DataType(DataType.Date)]
	public DateTime? EndDate { get; set; }          
	[Display(Name = "Report Identifier")]
	public string ReportId { get; set; }           
	public IPagedList<Report> Reports { get; set; }
	
}