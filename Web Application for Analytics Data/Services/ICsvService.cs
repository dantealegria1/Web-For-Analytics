using Web_Application_for_Analytics_Data.Models;

namespace Web_Application_for_Analytics_Data.Services;

public interface ICsvService
{
	/*
	 * Cualquier clase que use esta interfaz debe tener un método llamado GetReports que devuelva una lista de Report.
	 * Pero no dice como se obtiene la lista, solo que debe de existir
	 */
	IEnumerable<Report> GetReports();  
}