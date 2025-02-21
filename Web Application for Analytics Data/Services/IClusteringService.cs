using Web_Application_for_Analytics_Data.Models;

namespace Web_Application_for_Analytics_Data.Services;

public interface IClusteringService
{
	/*
	 * Cualquier clase que implemente IClusteringService debe analizar una lista de reportes
	 * y devolver los resultados del análisis de agrupamiento (Clustering).
	 */
	ClusteringResults AnalyzeReportUsage(IEnumerable<Report> reports);
}

