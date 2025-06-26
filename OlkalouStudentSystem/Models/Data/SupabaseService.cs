// ===============================
// Services/SupabaseService.cs - Enhanced Supabase Connection Service
// ===============================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue;
using System.Collections.Concurrent;

namespace OlkalouStudentSystem.Services
{
    /// <summary>
    /// Thread-safe singleton service for managing Supabase client connections
    /// </summary>
    public sealed class SupabaseService : IDisposable
    {
        #region Fields and Properties

        private static readonly Lazy<SupabaseService> _lazyInstance = new(() => new SupabaseService());
        private static readonly object _lock = new();

        private Supabase.Client? _client;
        private readonly ILogger<SupabaseService>? _logger;
        private readonly ConcurrentDictionary<string, object> _cache = new();
        private bool _disposed = false;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

        /// <summary>
        /// Gets the singleton instance of SupabaseService
        /// </summary>
        public static SupabaseService Instance => _lazyInstance.Value;

        /// <summary>
        /// Gets the Supabase client instance
        /// </summary>
        public Supabase.Client Client
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SupabaseService));

                if (_client == null)
                {
                    throw new InvalidOperationException(
                        "SupabaseService has not been initialized. Call InitializeAsync() first.");
                }

                return _client;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the service is initialized
        /// </summary>
        public bool IsInitialized => _client != null && !_disposed;

        /// <summary>
        /// Gets a value indicating whether the client is connected
        /// </summary>
        public bool IsConnected => _client?.Realtime?.IsConnected ?? false;

        #endregion

        #region Constructor

        private SupabaseService()
        {
            // Try to get logger from DI if available - simplified without ServiceLocator
            _logger = null; // Will be set externally if needed
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initialize Supabase with default configuration
        /// </summary>
        public async Task InitializeAsync()
        {
            const string defaultUrl = "https://pleyzgsfvarlrjcpachc.supabase.co";
            const string defaultKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InBsZXl6Z3NmdmFybHJqY3BhY2hjIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTA4Nzg2NjQsImV4cCI6MjA2NjQ1NDY2NH0.2t7IkK2c-dJRq0joDCr4R4mcns_bdLzytxhLivikVxQ";

            await InitializeAsync(defaultUrl, defaultKey);
        }

        /// <summary>
        /// Initialize Supabase with configuration from appsettings
        /// </summary>
        public async Task InitializeAsync(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var supabaseConfig = configuration.GetSection("Supabase");
            var url = supabaseConfig["Url"] ?? throw new InvalidOperationException("Supabase URL not found in configuration");
            var key = supabaseConfig["Key"] ?? throw new InvalidOperationException("Supabase Key not found in configuration");

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = supabaseConfig.GetValue<bool>("AutoConnectRealtime", true),
                AutoRefreshToken = supabaseConfig.GetValue<bool>("AutoRefreshToken", true),
                SessionHandler = new SupabaseSessionHandler(),
                SessionPersistor = new SupabaseSessionPersistor()
            };

            await InitializeAsync(url, key, options);
        }

        /// <summary>
        /// Initialize Supabase with custom configuration
        /// </summary>
        public async Task InitializeAsync(string url, string key, SupabaseOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_disposed)
                throw new ObjectDisposedException(nameof(SupabaseService));

            await _initializationSemaphore.WaitAsync();

            try
            {
                if (_client != null)
                {
                    _logger?.LogWarning("SupabaseService is already initialized. Disposing existing client.");
                    await DisposeClientAsync();
                }

                options ??= CreateDefaultOptions();

                _logger?.LogInformation("Initializing Supabase client with URL: {Url}", url);

                _client = new Supabase.Client(url, key, options);

                // Test the connection
                await TestConnectionAsync();

                _logger?.LogInformation("Supabase client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Supabase client");
                await DisposeClientAsync();
                throw new InvalidOperationException("Failed to initialize Supabase connection. Please check your configuration.", ex);
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        #endregion

        #region Connection Methods

        /// <summary>
        /// Test the Supabase connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            if (_client == null)
                return false;

            try
            {
                // Simple test query to verify connection
                await _client.From<TestEntity>().Limit(1).Get();
                _logger?.LogDebug("Connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Connection test failed");
                return false;
            }
        }

        /// <summary>
        /// Reconnect to Supabase if connection is lost
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            if (_client?.Realtime != null && !_client.Realtime.IsConnected)
            {
                try
                {
                    await _client.Realtime.ConnectAsync();
                    _logger?.LogInformation("Successfully reconnected to Supabase Realtime");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to reconnect to Supabase Realtime");
                    return false;
                }
            }

            return IsConnected;
        }

        #endregion

        #region Cache Methods

        /// <summary>
        /// Get cached data
        /// </summary>
        public T? GetCached<T>(string key) where T : class
        {
            return _cache.TryGetValue(key, out var value) ? value as T : null;
        }

        /// <summary>
        /// Set cached data with expiration
        /// </summary>
        public void SetCache<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            ArgumentNullException.ThrowIfNull(value);

            _cache.AddOrUpdate(key, value, (k, oldValue) => value);

            if (expiration.HasValue)
            {
                _ = Task.Delay(expiration.Value).ContinueWith(_ => _cache.TryRemove(key, out _));
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create default Supabase options
        /// </summary>
        private static SupabaseOptions CreateDefaultOptions()
        {
            return new SupabaseOptions
            {
                AutoConnectRealtime = true,
                AutoRefreshToken = true,
                SessionHandler = new SupabaseSessionHandler(),
                SessionPersistor = new SupabaseSessionPersistor()
            };
        }

        /// <summary>
        /// Dispose the current client safely
        /// </summary>
        private async Task DisposeClientAsync()
        {
            if (_client?.Realtime != null && _client.Realtime.IsConnected)
            {
                try
                {
                    await _client.Realtime.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error while disconnecting Realtime client");
                }
            }

            _client = null;
            ClearCache();
        }

        #endregion

        #region Health Check

        /// <summary>
        /// Perform health check on the Supabase connection
        /// </summary>
        public async Task<HealthCheckResult> HealthCheckAsync()
        {
            try
            {
                if (!IsInitialized)
                {
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Status = "Not Initialized",
                        Message = "SupabaseService has not been initialized"
                    };
                }

                var isConnected = await TestConnectionAsync();

                return new HealthCheckResult
                {
                    IsHealthy = isConnected,
                    Status = isConnected ? "Healthy" : "Unhealthy",
                    Message = isConnected ? "Connection successful" : "Connection failed",
                    ResponseTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Error",
                    Message = ex.Message,
                    ResponseTime = DateTime.UtcNow
                };
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose the service and clean up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    DisposeClientAsync().GetAwaiter().GetResult();
                    _initializationSemaphore?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during disposal");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~SupabaseService()
        {
            Dispose(false);
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Test entity for connection testing
    /// </summary>
    [Supabase.Postgrest.Attributes.Table("users")]
    public class TestEntity : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// Health check result for Supabase connection
    /// </summary>
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ResponseTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    /// <summary>
    /// Custom session handler for Supabase
    /// </summary>
    public class SupabaseSessionHandler : IGotrueSessionPersistence<Session>
    {
        public void DestroySession()
        {
            // Implementation for destroying session
            Preferences.Remove("supabase_session");
        }

        public Session? LoadSession()
        {
            // Implementation for loading session
            var sessionJson = Preferences.Get("supabase_session", string.Empty);
            if (string.IsNullOrEmpty(sessionJson))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Session>(sessionJson);
            }
            catch
            {
                return null;
            }
        }

        public void SaveSession(Session session)
        {
            // Implementation for saving session
            try
            {
                var sessionJson = System.Text.Json.JsonSerializer.Serialize(session);
                Preferences.Set("supabase_session", sessionJson);
            }
            catch
            {
                // Handle serialization error
            }
        }
    }

    /// <summary>
    /// Custom session persistor for Supabase
    /// </summary>
    public class SupabaseSessionPersistor : IGotrueSessionPersistence<Session>
    {
        private const string SessionKey = "supabase_user_session";

        public void DestroySession()
        {
            try
            {
                if (Preferences.ContainsKey(SessionKey))
                {
                    Preferences.Remove(SessionKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error destroying session: {ex.Message}");
            }
        }

        public Session? LoadSession()
        {
            try
            {
                var sessionData = Preferences.Get(SessionKey, string.Empty);
                if (string.IsNullOrWhiteSpace(sessionData))
                    return null;

                return System.Text.Json.JsonSerializer.Deserialize<Session>(sessionData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading session: {ex.Message}");
                return null;
            }
        }

        public void SaveSession(Session session)
        {
            try
            {
                var sessionData = System.Text.Json.JsonSerializer.Serialize(session);
                Preferences.Set(SessionKey, sessionData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session: {ex.Message}");
            }
        }
    }

    #endregion
}