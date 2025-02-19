using Microsoft.AspNetCore.Mvc;
using Web_Application_for_Analytics_Data.Models;
using Web_Application_for_Analytics_Data.Services;
using X.PagedList.Extensions;

namespace Web_Application_for_Analytics_Data.Controllers;

public class ClusteringAnalysisController : Controller
{
	private readonly ICsvService _csvService;
	private readonly IClusteringService _clusteringService;

	public ClusteringAnalysisController(ICsvService csvService, IClusteringService clusteringService)
	{
		_csvService = csvService;
		_clusteringService = clusteringService;
	}

	public IActionResult Index()
	{
		try
		{
			var reports = _csvService.GetReports();
			var analysisResults = _clusteringService.AnalyzeReportUsage(reports);
            
			var viewModel = new Clustering()
			{
				Results = analysisResults,
				TotalAccounts = analysisResults.Clusters.Sum(c => c.AccountCount),
				AnalysisDate = DateTime.Now
			};

			return View(viewModel);
		}
		catch (Exception ex)
		{
			// Log the error
			return View("Error");
		}
	}
}
