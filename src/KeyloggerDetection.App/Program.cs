using System.Diagnostics;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Infrastructure.Logging;
using KeyloggerDetection.Infrastructure.Pipeline;
using KeyloggerDetection.Monitoring.ProcessContext;
using KeyloggerDetection.Monitoring.FileBehaviour;
using KeyloggerDetection.Monitoring.NetworkBehaviour;
using KeyloggerDetection.Monitoring.Persistence;
using KeyloggerDetection.Scoring;

namespace KeyloggerDetection.App;

internal static class Program
{
    private static IAppLogger? _logger;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1. Configuration
        var config = new DetectionConfig();
        var baseDir = AppContext.BaseDirectory;
        var logDir = Path.Combine(baseDir, config.LogDirectory);

        // 2. Logging Infrastructure
        _logger = new TextAppLogger(logDir, config.LogLevel);
        _logger.LogInfo("========================================");
        _logger.LogInfo("KeylogSense tray application started.");
        
        var detectionLogger = new DetectionLogFileService(config);

        // 3. Global Exception Handling
        Application.ThreadException += GlobalThreadExceptionHandler;
        AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

        // 4. DI Bootstrapping
        IClock clock = new SystemClock();
        ITelemetryPipeline pipeline = new TelemetryPipeline(_logger);
        
        var allowlistManager = new AllowlistManager(config);
        var scoringEngine = new RiskScoringEngine(config, allowlistManager, clock);
        
        var fileWriteAnalyzer = new FileWriteAnalyzer(config, clock);
        var processCollector = new ProcessCollector(_logger, config, clock);
        var etwCollector = new EtwFileCollector(_logger, config, clock);
        var networkCollector = new NetworkCollector(_logger, config, clock);
        var persistenceCollector = new PersistenceCollector(_logger, config, clock);

        var collectors = new ICollector[] 
        { 
            processCollector, 
            etwCollector, 
            networkCollector, 
            persistenceCollector 
        };

        var aggregator = new FeatureAggregator(_logger, pipeline, scoringEngine, detectionLogger, fileWriteAnalyzer);

        // 5. Monitoring Coordinator
        var coordinator = new MonitoringCoordinator(_logger, collectors, aggregator, pipeline);

        // 6. Run Tray Applicaton
        try
        {
            using var trayContext = new TrayApplicationContext(_logger, coordinator);
            Application.Run(trayContext);
        }
        catch (Exception ex)
        {
            _logger.LogError("Fatal error in main application loop.", ex);
        }
        finally
        {
            _logger.LogInfo("KeylogSense tray application shutting down.");
            _logger.LogInfo("========================================");
            _logger.Dispose();
        }
    }

    private static void GlobalThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
    {
        _logger?.LogError("Unhandled UI thread exception occurred.", e.Exception);
    }

    private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.LogError("Unhandled non-UI thread exception occurred.", e.ExceptionObject as Exception);
    }
}
