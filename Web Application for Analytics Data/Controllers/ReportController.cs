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

		if (filter.EndDate.HasValue && filter.StartDate.HasValue && filter.EndDate < filter.StartDate)
		{
			TempData["ErrorMessage"] = "End Date cannot be earlier than Start Date!";
			return (View(filter));
		}

		// Apply filters
		if (filter.StartDate.HasValue)
			reports = reports.Where(r => r.CreationDate >= filter.StartDate);

		if (filter.EndDate.HasValue)
			reports = reports.Where(r => r.CreationDate <= filter.EndDate);
		
		if(filter.Account != null)
			reports = reports.Where(r => r.Accounts.Contains(filter.Account));

		if (filter.AccountId != null)
			reports = reports.Where(r => r.AccountMembers == filter.AccountId);

		if (!string.IsNullOrEmpty(filter.ReportId))
			reports = reports.Where(r => r.ReportId != null && 
			                             r.ReportId.IndexOf(filter.ReportId, StringComparison.OrdinalIgnoreCase) >= 0);
    
		// Apply pagination
		filter.Reports = reports.ToPagedList(page, PageSize);

		return View(filter);
	}

}