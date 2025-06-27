// ===============================
// Services/SupabaseService.cs - Enhanced Supabase Connection Service
// ===============================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue;
using Supabase.Realtime;
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
        public bool IsConnected
        {
            get
            {
                try
                {
                    return _client?.Realtime?.Socket?.IsConnected ?? false;
                }
                catch
                {
                    return false;
                }
            }
        }

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

            // Parse configuration values manually
            var autoConnectRealtime = bool.TryParse(supabaseConfig["AutoConnectRealtime"], out var acr) ? acr : true;
            var autoRefreshToken = bool.TryParse(supabaseConfig["AutoRefreshToken"], out var art) ? art : true;

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = autoConnectRealtime,
                AutoRefreshToken = autoRefreshToken,
                SessionHandler = new SupabaseSessionHandler()
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
            if (_client?.Realtime != null && !IsConnected)
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
                // Fix the lambda to remove the cache entry correctly
                _ = Task.Delay(expiration.Value).ContinueWith(_ =>
                {
                    _cache.TryRemove(key, out object? _);
                }, TaskScheduler.Default);
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
                SessionHandler = new SupabaseSessionHandler()
            };
        }

        /// <summary>
        /// Dispose the current client safely
        /// </summary>
        private async Task DisposeClientAsync()
        {
            if (_client?.Realtime != null && IsConnected)
            {
                try
                {
                    // Use Disconnect() instead of DisconnectAsync() as it doesn't exist in current version
                    _client.Realtime.Disconnect();

                    // Give it a moment to disconnect gracefully
                    await Task.Delay(100);
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
            var startTime = DateTime.UtcNow;

            try
            {
                if (!IsInitialized)
                {
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        Status = "Not Initialized",
                        Message = "SupabaseService has not been initialized",
                        ResponseTime = startTime,
                        Duration = DateTime.UtcNow - startTime
                    };
                }

                var isConnected = await TestConnectionAsync();
                var endTime = DateTime.UtcNow;

                return new HealthCheckResult
                {
                    IsHealthy = isConnected,
                    Status = isConnected ? "Healthy" : "Unhealthy",
                    Message = isConnected ? "Connection successful" : "Connection failed",
                    ResponseTime = startTime,
                    Duration = endTime - startTime
                };
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Error",
                    Message = ex.Message,
                    ResponseTime = startTime,
                    Duration = endTime - startTime
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
        private const string SessionKey = "supabase_auth_session";

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
                var sessionJson = Preferences.Get(SessionKey, string.Empty);
                if (string.IsNullOrEmpty(sessionJson))
                    return null;

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                return System.Text.Json.JsonSerializer.Deserialize<Session>(sessionJson, options);
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
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var sessionJson = System.Text.Json.JsonSerializer.Serialize(session, options);
                Preferences.Set(SessionKey, sessionJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Custom session persistor for Supabase (alternative implementation)
    /// </summary>
    public class SupabaseSessionPersistor : IGotrueSessionPersistence<Session>
    {
        private const string SessionKey = "supabase_user_session_v2";

        public void DestroySession()
        {
            try
            {
                if (Preferences.ContainsKey(SessionKey))
                {
                    Preferences.Remove(SessionKey);
                }

                // Also clear any legacy session keys
                var legacyKeys = new[] { "supabase_user_session", "supabase_session", "supabase_auth_session" };
                foreach (var key in legacyKeys)
                {
                    if (Preferences.ContainsKey(key))
                    {
                        Preferences.Remove(key);
                    }
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

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                return System.Text.Json.JsonSerializer.Deserialize<Session>(sessionData, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading session: {ex.Message}");

                // If loading fails, try to clear corrupted session data
                try
                {
                    DestroySession();
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return null;
            }
        }

        public void SaveSession(Session session)
        {
            if (session == null)
            {
                DestroySession();
                return;
            }

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var sessionData = System.Text.Json.JsonSerializer.Serialize(session, options);
                Preferences.Set(SessionKey, sessionData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving session: {ex.Message}");
            }
        }
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Extension methods for SupabaseService
    /// </summary>
    public static class SupabaseServiceExtensions
    {
        /// <summary>
        /// Initialize SupabaseService with retry logic
        /// </summary>
        public static async Task<bool> TryInitializeAsync(this SupabaseService service, int maxRetries = 3, TimeSpan? delay = null)
        {
            var retryDelay = delay ?? TimeSpan.FromSeconds(2);

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await service.InitializeAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Initialization attempt {i + 1} failed: {ex.Message}");

                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(retryDelay);
                        retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5); // Exponential backoff
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get configuration value with fallback
        /// </summary>
        public static T GetValueOrDefault<T>(this IConfiguration configuration, string key, T defaultValue)
        {
            try
            {
                var value = configuration[key];
                if (string.IsNullOrWhiteSpace(value))
                    return defaultValue;

                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(value);
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(value);
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    #endregion
}