using Web_Application_for_Analytics_Data.Services;

namespace Web_Application_for_Analytics_Data.Models;

public class Clustering
{
	public ClusteringResults Results { get; set; }
	public int TotalAccounts { get; set; }
	public DateTime AnalysisDate { get; set; }
}