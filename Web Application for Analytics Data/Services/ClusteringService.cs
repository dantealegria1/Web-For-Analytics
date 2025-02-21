// Importación de namespaces necesarios para acceder a modelos, colecciones, LINQ, el algoritmo de clustering y logging.
using Web_Application_for_Analytics_Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Accord.Math.Distances;
using Microsoft.Extensions.Logging;

namespace Web_Application_for_Analytics_Data.Services;

// Clase que implementa la interfaz IClusteringService para realizar clustering en los datos de reportes.
public class ClusteringService : IClusteringService
{
    // Instancia para registrar logs, inyectada a través del constructor.
    private readonly ILogger<ClusteringService> _logger;

    // Constructor que recibe una instancia de ILogger para la clase ClusteringService.
    public ClusteringService(ILogger<ClusteringService> logger)
    {
        _logger = logger;
    }

    // Método principal que analiza el uso de reportes.
    // Recibe una colección de Report y devuelve un objeto ClusteringResults con el análisis.
    public ClusteringResults AnalyzeReportUsage(IEnumerable<Report> reports)
    {
        // Extrae características relevantes de cada reporte.
        var features = ExtractFeatures(reports);
        // Aplica el algoritmo K-Means para agrupar las características extraídas.
        var clusters = ApplyKMeansClustering(features);
        // Interpreta los resultados del clustering para generar información útil.
        return InterpretResults(features, clusters);
    }
    
    // Método privado que extrae características de los reportes.
    // Agrupa los reportes por la propiedad AccountMembers y genera un objeto ReportFeatures para cada grupo.
    private List<ReportFeatures> ExtractFeatures(IEnumerable<Report> reports)
    {
        var reportsByAccount = reports.GroupBy(r => r.AccountMembers);
        
        return reportsByAccount.Select(group => new ReportFeatures
        {
            AccountId = group.Key, // Identificador de la cuenta
            AverageCompletionTime = CalculateAverageCompletionTime(group), // Tiempo promedio de finalización
            ReportsCount = group.Count(), // Número de reportes en el grupo
            ReportFrequency = CalculateReportFrequency(group) // Frecuencia de generación de reportes
        }).ToList();
    }

    // Calcula el tiempo promedio (en horas) que tarda un reporte en completarse.
    // Se obtiene la diferencia entre CompletionDate y CreationDate para cada reporte.
    private double CalculateAverageCompletionTime(IGrouping<string, Report> reports)
    {
        return reports.Average(r => 
            (r.CompletionDate - r.CreationDate).TotalHours);
    }

    // Calcula la frecuencia de reportes (reportes por día) en el grupo.
    // Se calcula el lapso total entre el primer y el último reporte, y se divide la cantidad de reportes por el número de días.
    private double CalculateReportFrequency(IGrouping<string, Report> reports)
    {
        var timeSpan = reports.Max(r => r.CreationDate) - reports.Min(r => r.CreationDate);
        return reports.Count() / (timeSpan.TotalDays + 1); // Se suma 1 para evitar división por cero
    }

    // Aplica el algoritmo de clustering K-Means a las características extraídas.
    // Retorna un objeto ClusteringOutput con las etiquetas de los clusters y los centroides.
    private ClusteringOutput ApplyKMeansClustering(List<ReportFeatures> features)
    {
        // Prepara los datos normalizados para el clustering.
        var data = features.Select(f => new double[]
        {
            NormalizeValue(f.AverageCompletionTime),
            NormalizeValue(f.ReportsCount),
            NormalizeValue(f.ReportFrequency)
        }).ToArray();

        // Configuración del algoritmo K-Means con 3 clusters.
        var kmeans = new KMeans(k: 3)
        {
            MaxIterations = 1000, // Máximo número de iteraciones permitidas
            Tolerance = 0.0001    // Tolerancia para la convergencia del algoritmo
        };

        // Ejecuta el aprendizaje del algoritmo sobre los datos preparados.
        var clusters = kmeans.Learn(data);
        // Asigna a cada dato su etiqueta correspondiente (cluster al que pertenece).
        var labels = clusters.Decide(data);

        // Retorna la salida del clustering con etiquetas y centroides.
        // Osea a cuales cluster pertenece cada cuenta y cual es su centroide
        return new ClusteringOutput
        {
            ClusterLabels = labels,
            Centroids = clusters.Centroids
        };
    }

    // Normaliza un valor utilizando la técnica Min-Max normalization.
    // Se añade un epsilon para evitar problemas de división por cero.
    private double NormalizeValue(double value)
    {
        const double epsilon = 1e-10;
        return (value + epsilon) / (100 + epsilon);
    }

    // Interpreta los resultados obtenidos del clustering para generar insights y estadísticas.
    // Recorre cada cluster y calcula estadísticas agregadas.
    private ClusteringResults InterpretResults(
        List<ReportFeatures> features, 
        ClusteringOutput clusteringOutput)
    {
        var results = new ClusteringResults
        {
            Clusters = new List<ClusterInfo>()
        };

        // Itera para cada uno de los 3 clusters.
        for (int i = 0; i < 3; i++)
        {
            // Selecciona las características de los reportes que pertenecen al cluster actual.
            var clusterAccounts = features
                .Where((f, index) => clusteringOutput.ClusterLabels[index] == i)
                .ToList();

            // Determina el nivel de rendimiento del cluster usando sus centroides.
            var performance = DeterminePerformanceLevel(
                clusteringOutput.Centroids[i], 
                clusteringOutput.Centroids);

            // Agrega la información del cluster a los resultados.
            results.Clusters.Add(new ClusterInfo
            {
                PerformanceLevel = performance,
                AccountCount = clusterAccounts.Count,
                AverageCompletionTime = clusterAccounts.Average(f => f.AverageCompletionTime),
                AverageReportsPerDay = clusterAccounts.Average(f => f.ReportFrequency),
                Accounts = clusterAccounts.Select(f => f.AccountId).ToList()
            });
        }

        // Genera insights basados en los clusters formados.
        results.Insights = GenerateInsights(results.Clusters);
        return results;
    }

    // Determina el nivel de rendimiento (High, Medium, Low) de un cluster basado en sus centroides.
    // Se utiliza un puntaje calculado a partir de los valores normalizados y pesos definidos.
    private PerformanceLevel DeterminePerformanceLevel(double[] centroid, double[][] allCentroids)
    {
        // Pesos para el cálculo del rendimiento: menor tiempo (mejor) y mayor frecuencia (mejor).
        double completionTimeWeight = -1.0; // Valor negativo porque menos tiempo es mejor
        double reportFrequencyWeight = 1.0;  // Valor positivo porque más reportes es mejor
        
        // Calcula el puntaje de rendimiento del centroid actual.
        var performanceScore = (centroid[0] * completionTimeWeight) + 
                              (centroid[1] * reportFrequencyWeight) + 
                              (centroid[2] * reportFrequencyWeight);
        
        // Calcula los puntajes de todos los centroides para compararlos.
        var scores = allCentroids
            .Select(c => (c[0] * completionTimeWeight) + 
                         (c[1] * reportFrequencyWeight) + 
                         (c[2] * reportFrequencyWeight))
            .OrderDescending()
            .ToList();
        
        // Compara el puntaje con los demás para asignar el nivel de rendimiento.
        //SI la diferencia de rendimiento es muy pequeña peude haber dos cluster iguales
        if (Math.Abs(performanceScore - scores[0]) < 0.001) return PerformanceLevel.High;
        if (Math.Abs(performanceScore - scores[1]) < 0.001) return PerformanceLevel.Medium;
        return PerformanceLevel.Low;
    }

    // Genera insights (información relevante) basados en el análisis de los clusters.
    // Examina cada cluster y crea recomendaciones o conclusiones.
    private List<string> GenerateInsights(List<ClusterInfo> clusters)
    {
        var insights = new List<string>();
        
        // Si existe un cluster de alto rendimiento, se agrega un insight relevante.
        var highPerformanceCluster = clusters.FirstOrDefault(c => c.PerformanceLevel == PerformanceLevel.High);
        if (highPerformanceCluster != null)
        {
            insights.Add($"High-performance accounts ({highPerformanceCluster.AccountCount} accounts) " +
                        $"submit an average of {highPerformanceCluster.AverageReportsPerDay:F1} reports per day" +
                        $"These accounts might benefit from advanced features or early access to new capabilities" +
                        $"Examine what makes these accounts successful - their workflows and practices could be used as best practices");
        }
		
        // Si existe un cluster de rendimiento medio, se agrega un insight adecuado.
        var medianPerformanceCluster = clusters.FirstOrDefault(c => c.PerformanceLevel == PerformanceLevel.Medium);
        if (medianPerformanceCluster != null)
        {
            insights.Add($"Medium-performance accounts ({medianPerformanceCluster.AccountCount} accounts)" +
                         $"Target training or workflow improvements to help them improve efficiency" +
                         $"Identify specific bottlenecks preventing them from reaching high performance");
        }
        
        // Si existe un cluster de bajo rendimiento, se agrega un insight para mejorar su desempeño.
        var lowPerformanceCluster = clusters.FirstOrDefault(c => c.PerformanceLevel == PerformanceLevel.Low);
        if (lowPerformanceCluster != null)
        {
            insights.Add($"Low-performance accounts ({lowPerformanceCluster.AccountCount} accounts) might " +
                        "benefit from additional training or engagement initiatives" +
                        $"Investigate the root causes: Is it lack of training, technical issues, or process inefficiencies?");
        }

        return insights;
    }
}

// Clase para representar las características extraídas de un Report
public class ReportFeatures
{
    public string AccountId { get; set; }
    public double AverageCompletionTime { get; set; }
    public int ReportsCount { get; set; }
    public double ReportFrequency { get; set; }
}

// Clase para encapsular la salida del clustering, incluyendo etiquetas de clusters y centroides.
public class ClusteringOutput
{
    public int[] ClusterLabels { get; set; }
    public double[][] Centroids { get; set; }
}

// Clase que contiene los resultados finales del clustering, con información de cada cluster e insights.
public class ClusteringResults
{
    public List<ClusterInfo> Clusters { get; set; }
    public List<string> Insights { get; set; }
}

// Clase que representa la información de cada cluster, incluyendo nivel de rendimiento y estadísticas.
public class ClusterInfo
{
    public PerformanceLevel PerformanceLevel { get; set; }
    public int AccountCount { get; set; }
    public double AverageCompletionTime { get; set; }
    public double AverageReportsPerDay { get; set; }
    public List<string> Accounts { get; set; }
}

// Enumeración para definir los niveles de rendimiento de un cluster.
public enum PerformanceLevel
{
    Low,
    Medium,
    High
}
