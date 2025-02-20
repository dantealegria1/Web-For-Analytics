using Web_Application_for_Analytics_Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Accord.Math.Distances;
using Microsoft.Extensions.Logging;

namespace Web_Application_for_Analytics_Data.Services;

public class ClusteringService : IClusteringService
{
	private readonly ILogger<ClusteringService> _logger;

	public ClusteringService(ILogger<ClusteringService> logger)
	{
		_logger = logger;
	}

	public ClusteringResults AnalyzeReportUsage(IEnumerable<Report> reports)
	{
		var features = ExtractFeatures(reports);
		var clusters = ApplyKMeansClustering(features);
		return InterpretResults(features, clusters);
	}
	
	private List<ReportFeatures> ExtractFeatures(IEnumerable<Report> reports)
    {
        var reportsByAccount = reports.GroupBy(r => r.AccountMembers);
        
        return reportsByAccount.Select(group => new ReportFeatures
        {
            AccountId = group.Key,
            AverageCompletionTime = CalculateAverageCompletionTime(group),
            ReportsCount = group.Count(),
            ReportFrequency = CalculateReportFrequency(group)
        }).ToList();
    }

    private double CalculateAverageCompletionTime(IGrouping<string, Report> reports)
    {
        return reports.Average(r => 
            (r.CompletionDate - r.CreationDate).TotalHours);
    }

    private double CalculateReportFrequency(IGrouping<string, Report> reports)
    {
        var timeSpan = reports.Max(r => r.CreationDate) - reports.Min(r => r.CreationDate);
        return reports.Count() / (timeSpan.TotalDays + 1); // Reports per day
    }

    private ClusteringOutput ApplyKMeansClustering(List<ReportFeatures> features)
    {
        // Prepare data for clustering
        var data = features.Select(f => new double[]
        {
            NormalizeValue(f.AverageCompletionTime),
            NormalizeValue(f.ReportsCount),
            NormalizeValue(f.ReportFrequency)
        }).ToArray();

        // Apply K-Means clustering
        var kmeans = new KMeans(k: 3)
        {
            MaxIterations = 1000,
            Tolerance = 0.0001
        };

        // Compute clusters
        var clusters = kmeans.Learn(data);
        var labels = clusters.Decide(data);

        return new ClusteringOutput
        {
            ClusterLabels = labels,
            Centroids = clusters.Centroids
        };
    }

    private double NormalizeValue(double value)
    {
        // Min-Max normalization
        const double epsilon = 1e-10;
        return (value + epsilon) / (100 + epsilon);
    }

    private ClusteringResults InterpretResults(
        List<ReportFeatures> features, 
        ClusteringOutput clusteringOutput)
    {
        var results = new ClusteringResults
        {
            Clusters = new List<ClusterInfo>()
        };

        for (int i = 0; i < 3; i++) // For each cluster
        {
            var clusterAccounts = features
                .Where((f, index) => clusteringOutput.ClusterLabels[index] == i)
                .ToList();

            var performance = DeterminePerformanceLevel(
                clusteringOutput.Centroids[i], 
                clusteringOutput.Centroids);

            results.Clusters.Add(new ClusterInfo
            {
                PerformanceLevel = performance,
                AccountCount = clusterAccounts.Count,
                AverageCompletionTime = clusterAccounts.Average(f => f.AverageCompletionTime),
                AverageReportsPerDay = clusterAccounts.Average(f => f.ReportFrequency),
                Accounts = clusterAccounts.Select(f => f.AccountId).ToList()
            });
        }

        results.Insights = GenerateInsights(results.Clusters);
        return results;
    }

    private PerformanceLevel DeterminePerformanceLevel(double[] centroid, double[][] allCentroids)
    {
        // Calculate overall performance level based on centroid values
        // Higher activity score + lower completion time = higher performance
        double completionTimeWeight = -1.0; // Negative weight as lower completion time is better
        double reportFrequencyWeight = 1.0;  // Positive weight as more reports is better
        
        var performanceScore = (centroid[0] * completionTimeWeight) + 
                              (centroid[1] * reportFrequencyWeight) + 
                              (centroid[2] * reportFrequencyWeight);
        
        var scores = allCentroids
            .Select(c => (c[0] * completionTimeWeight) + 
                         (c[1] * reportFrequencyWeight) + 
                         (c[2] * reportFrequencyWeight))
            .OrderDescending()
            .ToList();
        
        if (Math.Abs(performanceScore - scores[0]) < 0.001) return PerformanceLevel.High;
        if (Math.Abs(performanceScore - scores[1]) < 0.001) return PerformanceLevel.Medium;
        return PerformanceLevel.Low;
    }

    private List<string> GenerateInsights(List<ClusterInfo> clusters)
    {
        var insights = new List<string>();
        
        var highPerformanceCluster = clusters.FirstOrDefault(c => c.PerformanceLevel == PerformanceLevel.High);
        if (highPerformanceCluster != null)
        {
            insights.Add($"High-performance accounts ({highPerformanceCluster.AccountCount} accounts) " +
                        $"submit an average of {highPerformanceCluster.AverageReportsPerDay:F1} reports per day"+
                        $"These accounts might benefit from advanced features or early access to new capabilities"+
                        $"Examine what makes these accounts successful - their workflows and practices could be used as best practices");
        }
		
        var medianPerformanceCluster = clusters.FirstOrDefault(c => c.PerformanceLevel == PerformanceLevel.Medium);
        if (medianPerformanceCluster != null)
        {
	        insights.Add($"Medium-performance accounts ({medianPerformanceCluster.AccountCount} accounts)"+
	                     $"Target training or workflow improvements to help them improve efficiency"+
	                     $"Identify specific bottlenecks preventing them from reaching high performance");
        }
        var lowPerformanceCluster = clusters.FirstOrDefault(c => c.PerformanceLevel == PerformanceLevel.Low);
        if (lowPerformanceCluster != null)
        {
            insights.Add($"Low-performance accounts ({lowPerformanceCluster.AccountCount} accounts) might " +
                        "benefit from additional training or engagement initiatives"+
                        $"Investigate the root causes: Is it lack of training, technical issues, or process inefficiencies?");
        }

        return insights;
    }
}

public class ReportFeatures
{
	public string AccountId { get; set; }
	public double AverageCompletionTime { get; set; }
	public int ReportsCount { get; set; }
	public double ReportFrequency { get; set; }
}

public class ClusteringOutput
{
	public int[] ClusterLabels { get; set; }
	public double[][] Centroids { get; set; }
}

public class ClusteringResults
{
	public List<ClusterInfo> Clusters { get; set; }
	public List<string> Insights { get; set; }
}

public class ClusterInfo
{
	public PerformanceLevel PerformanceLevel { get; set; }
	public int AccountCount { get; set; }
	public double AverageCompletionTime { get; set; }
	public double AverageReportsPerDay { get; set; }
	public List<string> Accounts { get; set; }
}

public enum PerformanceLevel
{
	Low,
	Medium,
	High
}