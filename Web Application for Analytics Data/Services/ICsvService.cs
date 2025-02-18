using Web_Application_for_Analytics_Data.Models;

namespace Web_Application_for_Analytics_Data.Services;

public interface ICsvService
{
	IEnumerable<Report> GetReports();  
}