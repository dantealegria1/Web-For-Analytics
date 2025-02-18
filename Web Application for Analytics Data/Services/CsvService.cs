using System.Globalization;
using CsvHelper;
using Web_Application_for_Analytics_Data.Models;

namespace Web_Application_for_Analytics_Data.Services;

public class CsvService : ICsvService
{
	private readonly string _csvPath;
	private readonly ILogger<CsvService> _logger;

	public CsvService(IConfiguration configuration, ILogger<CsvService> logger)
	{
		_csvPath = configuration.GetValue<string>("CsvFilePath");
		if (string.IsNullOrEmpty(_csvPath))
		{
			throw new ArgumentException("CSV file path not found in configuration");
		}
		_logger = logger;
	}

	public IEnumerable<Report> GetReports()
	{
		if (!File.Exists(_csvPath))
		{
			_logger.LogError($"CSV file not found at path: {_csvPath}");
			throw new FileNotFoundException($"CSV file not found at path: {_csvPath}");
		}

		try
		{
			using var reader = new StreamReader(_csvPath);
			using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
			return csv.GetRecords<Report>().ToList();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading CSV file");
			throw;
		}
	}
}