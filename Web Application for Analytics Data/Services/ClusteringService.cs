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
        var reportsByAccount = reports.GroupBy(r => r.Accounts);
        
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
            MaxIterations = 100,
            Tolerance = 0.001
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

            var usage = DetermineUsageLevel(
                clusteringOutput.Centroids[i], 
                clusteringOutput.Centroids);

            results.Clusters.Add(new ClusterInfo
            {
                UsageLevel = usage,
                AccountCount = clusterAccounts.Count,
                AverageCompletionTime = clusterAccounts.Average(f => f.AverageCompletionTime),
                AverageReportsPerDay = clusterAccounts.Average(f => f.ReportFrequency),
                Accounts = clusterAccounts.Select(f => f.AccountId).ToList()
            });
        }

        results.Insights = GenerateInsights(results.Clusters);
        return results;
    }

    private UsageLevel DetermineUsageLevel(double[] centroid, double[][] allCentroids)
    {
        // Calculate overall activity level based on centroid values
        var activityScore = centroid.Sum();
        var scores = allCentroids.Select(c => c.Sum()).OrderDescending().ToList();
        
        if (activityScore == scores[0]) return UsageLevel.High;
        if (activityScore == scores[1]) return UsageLevel.Medium;
        return UsageLevel.Low;
    }

    private List<string> GenerateInsights(List<ClusterInfo> clusters)
    {
        var insights = new List<string>();
        
        var highUsageCluster = clusters.FirstOrDefault(c => c.UsageLevel == UsageLevel.High);
        if (highUsageCluster != null)
        {
            insights.Add($"High-usage accounts ({highUsageCluster.AccountCount} accounts) " +
                        $"submit an average of {highUsageCluster.AverageReportsPerDay:F1} reports per day");
        }

        var lowUsageCluster = clusters.FirstOrDefault(c => c.UsageLevel == UsageLevel.Low);
        if (lowUsageCluster != null)
        {
            insights.Add($"Low-usage accounts ({lowUsageCluster.AccountCount} accounts) might " +
                        "benefit from additional training or engagement initiatives");
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
	public UsageLevel UsageLevel { get; set; }
	public int AccountCount { get; set; }
	public double AverageCompletionTime { get; set; }
	public double AverageReportsPerDay { get; set; }
	public List<string> Accounts { get; set; }
}

public enum UsageLevel
{
	Low,
	Medium,
	High
}