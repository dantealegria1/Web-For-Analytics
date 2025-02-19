using Microsoft.AspNetCore.Mvc;
using Web_Application_for_Analytics_Data.Models;
using Web_Application_for_Analytics_Data.Services;
using X.PagedList.Extensions;

namespace Web_Application_for_Analytics_Data.Controllers;

public class ReportController(ICsvService csvService) : Controller
{
	private const int PageSize = 10;

	public IActionResult Index(Filter filter, int page = 1)
	{
		var reports = csvService.GetReports();

		// Apply filters
		if (filter.StartDate.HasValue)
			reports = reports.Where(r => r.CreationDate >= filter.StartDate);
        
		if (filter.EndDate.HasValue)
		{
			if (filter.EndDate < filter.StartDate)
			{
				ModelState.AddModelError("EndDate", "End Date cannot be earlier than Start Date");
				return View(filter);
			}
			reports = reports.Where(r => r.CreationDate <= filter.EndDate);
		}

		if (!string.IsNullOrEmpty(filter.ReportId))
			reports = reports.Where(r => r.ReportId != null && 
			                             r.ReportId.IndexOf(filter.ReportId, StringComparison.OrdinalIgnoreCase) >= 0);
		// Apply pagination
		filter.Reports = reports.ToPagedList(page, PageSize);

		return View(filter);
	}
}