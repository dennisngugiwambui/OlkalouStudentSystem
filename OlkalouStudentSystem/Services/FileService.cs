// ===============================
// Services/FileService.cs
// ===============================
using OlkalouStudentSystem.Models;
using System.IO;

#if ANDROID
using Android.OS;
#endif

namespace OlkalouStudentSystem.Services
{
    public class FileService
    {
        private readonly string _documentsPath;
        private readonly string _cachePath;
        private readonly string _tempPath;

        // Supported file types for different operations
        private readonly Dictionary<string, string[]> _supportedFileTypes = new()
        {
            ["assignments"] = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" },
            ["images"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" },
            ["documents"] = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" },
            ["all"] = new[] { "*" }
        };

        // Maximum file sizes (in bytes)
        private readonly Dictionary<string, long> _maxFileSizes = new()
        {
            ["assignments"] = 10 * 1024 * 1024, // 10 MB
            ["images"] = 5 * 1024 * 1024,       // 5 MB
            ["documents"] = 20 * 1024 * 1024,   // 20 MB
            ["default"] = 10 * 1024 * 1024      // 10 MB
        };

        public FileService()
        {
            // Initialize paths using Microsoft.Maui.Storage
            _documentsPath = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
            _cachePath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
            _tempPath = Path.Combine(_cachePath, "temp");

            // Create directories if they don't exist
            EnsureDirectoriesExist();
        }

        #region File Picking

        /// <summary>
        /// Pick a file for assignment submission
        /// </summary>
        /// <returns>FileData object containing file information</returns>
        public async Task<FileData> PickFileAsync()
        {
            return await PickFileAsync("assignments");
        }

        /// <summary>
        /// Pick a file with specific category restrictions
        /// </summary>
        /// <param name="category">File category (assignments, images, documents, all)</param>
        /// <returns>FileData object containing file information</returns>
        public async Task<FileData> PickFileAsync(string category = "all")
        {
            try
            {
                var fileTypes = GetFileTypesForCategory(category);
                var maxSize = GetMaxSizeForCategory(category);

                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, fileTypes },
                        { DevicePlatform.Android, fileTypes },
                        { DevicePlatform.WinUI, fileTypes },
                        { DevicePlatform.macOS, fileTypes },
                    });

                var options = new PickOptions
                {
                    FileTypes = customFileType,
                    PickerTitle = GetPickerTitle(category)
                };

                var result = await FilePicker.PickAsync(options);

                if (result != null)
                {
                    // Validate file
                    var validationResult = await ValidateFileAsync(result, category, maxSize);
                    if (!validationResult.IsValid)
                    {
                        throw new InvalidOperationException(validationResult.ErrorMessage);
                    }

                    // Read file data
                    using var stream = await result.OpenReadAsync();
                    var fileData = await CreateFileDataAsync(result, stream);

                    return fileData;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File picking error: {ex.Message}");
                throw new InvalidOperationException($"Failed to pick file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Pick multiple files
        /// </summary>
        /// <param name="category">File category</param>
        /// <param name="maxFiles">Maximum number of files to pick</param>
        /// <returns>List of FileData objects</returns>
        public async Task<List<FileData>> PickMultipleFilesAsync(string category = "all", int maxFiles = 5)
        {
            try
            {
                var fileTypes = GetFileTypesForCategory(category);
                var maxSize = GetMaxSizeForCategory(category);

                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, fileTypes },
                        { DevicePlatform.Android, fileTypes },
                        { DevicePlatform.WinUI, fileTypes },
                        { DevicePlatform.macOS, fileTypes },
                    });

                var options = new PickOptions
                {
                    FileTypes = customFileType,
                    PickerTitle = $"Select up to {maxFiles} files"
                };

                var results = await FilePicker.PickMultipleAsync(options);
                var fileDataList = new List<FileData>();

                if (results != null)
                {
                    var fileCount = 0;
                    foreach (var result in results)
                    {
                        if (fileCount >= maxFiles)
                            break;

                        // Validate each file
                        var validationResult = await ValidateFileAsync(result, category, maxSize);
                        if (validationResult.IsValid)
                        {
                            using var stream = await result.OpenReadAsync();
                            var fileData = await CreateFileDataAsync(result, stream);
                            fileDataList.Add(fileData);
                            fileCount++;
                        }
                    }
                }

                return fileDataList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Multiple file picking error: {ex.Message}");
                throw new InvalidOperationException($"Failed to pick files: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Pick an image file with camera option
        /// </summary>
        /// <returns>FileData object containing image data</returns>
        public async Task<FileData> PickImageAsync(bool allowCamera = true)
        {
            try
            {
                string action;
                if (allowCamera)
                {
                    action = await Application.Current.MainPage.DisplayActionSheet(
                        "Select Image Source", "Cancel", null, "Camera", "Photo Library");
                }
                else
                {
                    action = "Photo Library";
                }

                FileResult photo = null;

                if (action == "Camera")
                {
                    photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                    {
                        Title = "Take a photo"
                    });
                }
                else if (action == "Photo Library")
                {
                    photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                    {
                        Title = "Select a photo"
                    });
                }

                if (photo != null)
                {
                    // Validate image
                    var validationResult = await ValidateFileAsync(photo, "images", GetMaxSizeForCategory("images"));
                    if (!validationResult.IsValid)
                    {
                        throw new InvalidOperationException(validationResult.ErrorMessage);
                    }

                    using var stream = await photo.OpenReadAsync();
                    var fileData = await CreateFileDataAsync(photo, stream);

                    return fileData;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image picking error: {ex.Message}");
                throw new InvalidOperationException($"Failed to pick image: {ex.Message}", ex);
            }
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Save file to local storage
        /// </summary>
        /// <param name="fileData">File data to save</param>
        /// <param name="folder">Subfolder to save in (optional)</param>
        /// <returns>Full path to saved file</returns>
        public async Task<string> SaveFileAsync(FileData fileData, string folder = null)
        {
            try
            {
                if (fileData == null)
                    throw new ArgumentNullException(nameof(fileData));

                var targetDir = string.IsNullOrEmpty(folder)
                    ? _documentsPath
                    : Path.Combine(_documentsPath, folder);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(targetDir);

                // Generate unique filename to avoid conflicts
                var fileName = GenerateUniqueFileName(fileData.FileName, targetDir);
                var filePath = Path.Combine(targetDir, fileName);

                // Save file
                await File.WriteAllBytesAsync(filePath, fileData.Data);

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File save error: {ex.Message}");
                throw new InvalidOperationException($"Failed to save file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read file from local storage
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>FileData object containing file information</returns>
        public async Task<FileData> ReadFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path is required", nameof(filePath));

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var bytes = await File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);

                return new FileData
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Data = bytes,
                    Size = bytes.Length
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File read error: {ex.Message}");
                throw new InvalidOperationException($"Failed to read file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete file from local storage
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if file was deleted successfully</returns>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File delete error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Copy file to another location
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <param name="overwrite">Whether to overwrite existing file</param>
        /// <returns>True if file was copied successfully</returns>
        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                    return false;

                if (!File.Exists(sourcePath))
                    return false;

                if (File.Exists(destinationPath) && !overwrite)
                    return false;

                // Create destination directory if it doesn't exist
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                File.Copy(sourcePath, destinationPath, overwrite);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File copy error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region File Information

        /// <summary>
        /// Get file information
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>FileInfo object</returns>
        public FileInfo GetFileInfo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            return new FileInfo(filePath);
        }

        /// <summary>
        /// Get files in a directory
        /// </summary>
        /// <param name="directoryPath">Directory path</param>
        /// <param name="searchPattern">Search pattern (e.g., "*.pdf")</param>
        /// <returns>List of file paths</returns>
        public List<string> GetFilesInDirectory(string directoryPath, string searchPattern = "*.*")
        {
            try
            {
                if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                    return new List<string>();

                return Directory.GetFiles(directoryPath, searchPattern).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get files error: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get total size of files in a directory
        /// </summary>
        /// <param name="directoryPath">Directory path</param>
        /// <returns>Total size in bytes</returns>
        public long GetDirectorySize(string directoryPath)
        {
            try
            {
                if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                    return 0;

                return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get directory size error: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Cache file temporarily
        /// </summary>
        /// <param name="fileData">File data to cache</param>
        /// <param name="cacheKey">Unique cache key</param>
        /// <returns>Path to cached file</returns>
        public async Task<string> CacheFileAsync(FileData fileData, string cacheKey)
        {
            try
            {
                var cacheFilePath = Path.Combine(_cachePath, $"{cacheKey}_{fileData.FileName}");
                await File.WriteAllBytesAsync(cacheFilePath, fileData.Data);
                return cacheFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File cache error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get cached file
        /// </summary>
        /// <param name="cacheKey">Cache key</param>
        /// <returns>FileData if found, null otherwise</returns>
        public async Task<FileData> GetCachedFileAsync(string cacheKey)
        {
            try
            {
                var cacheFiles = Directory.GetFiles(_cachePath, $"{cacheKey}_*");
                if (cacheFiles.Length > 0)
                {
                    return await ReadFileAsync(cacheFiles[0]);
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get cached file error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <param name="olderThan">Clear files older than this timespan (optional)</param>
        public async Task ClearCacheAsync(TimeSpan? olderThan = null)
        {
            try
            {
                var cutoffTime = olderThan.HasValue ? DateTime.Now - olderThan.Value : DateTime.MinValue;

                var cacheFiles = Directory.GetFiles(_cachePath);
                foreach (var file in cacheFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffTime)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear cache error: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Format file size for display
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted size string</returns>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Check if file extension is supported for a category
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="category">File category</param>
        /// <returns>True if supported</returns>
        public bool IsFileTypeSupported(string fileName, string category)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var supportedTypes = GetFileTypesForCategory(category);

            return supportedTypes.Contains("*") || supportedTypes.Contains(extension);
        }

        #endregion

        #region Private Methods

        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(_documentsPath);
                Directory.CreateDirectory(_cachePath);
                Directory.CreateDirectory(_tempPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory creation error: {ex.Message}");
            }
        }

        private string[] GetFileTypesForCategory(string category)
        {
            return _supportedFileTypes.ContainsKey(category)
                ? _supportedFileTypes[category]
                : _supportedFileTypes["all"];
        }

        private long GetMaxSizeForCategory(string category)
        {
            return _maxFileSizes.ContainsKey(category)
                ? _maxFileSizes[category]
                : _maxFileSizes["default"];
        }

        private string GetPickerTitle(string category)
        {
            return category switch
            {
                "assignments" => "Select Assignment File",
                "images" => "Select Image",
                "documents" => "Select Document",
                _ => "Select File"
            };
        }

        private async Task<FileValidationResult> ValidateFileAsync(FileResult file, string category, long maxSize)
        {
            try
            {
                // Check file extension
                if (!IsFileTypeSupported(file.FileName, category))
                {
                    return new FileValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"File type '{Path.GetExtension(file.FileName)}' is not supported for {category}."
                    };
                }

                // Check file size
                using var stream = await file.OpenReadAsync();
                if (stream.Length > maxSize)
                {
                    return new FileValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"File size ({FormatFileSize(stream.Length)}) exceeds maximum allowed size ({FormatFileSize(maxSize)})."
                    };
                }

                return new FileValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File validation failed: {ex.Message}"
                };
            }
        }

        private async Task<FileData> CreateFileDataAsync(FileResult file, Stream stream)
        {
            var bytes = new byte[stream.Length];
            await stream.ReadAsync(bytes, 0, (int)stream.Length);

            return new FileData
            {
                FileName = file.FileName,
                ContentType = file.ContentType ?? GetContentType(file.FileName),
                Data = bytes,
                Size = stream.Length
            };
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }

        private string GenerateUniqueFileName(string originalFileName, string targetDirectory)
        {
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var counter = 0;
            var newFileName = originalFileName;

            while (File.Exists(Path.Combine(targetDirectory, newFileName)))
            {
                counter++;
                newFileName = $"{fileName}_{counter}{extension}";
            }

            return newFileName;
        }

        #endregion

        #region File Sharing

        /// <summary>
        /// Share file using platform sharing mechanism
        /// </summary>
        /// <param name="filePath">Path to file to share</param>
        /// <param name="title">Share dialog title</param>
        /// <returns>True if sharing was initiated successfully</returns>
        public async Task<bool> ShareFileAsync(string filePath, string title = "Share File")
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return false;

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = title,
                    File = new ShareFile(filePath)
                });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File sharing error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Share text content
        /// </summary>
        /// <param name="text">Text to share</param>
        /// <param name="title">Share dialog title</param>
        /// <returns>True if sharing was initiated successfully</returns>
        public async Task<bool> ShareTextAsync(string text, string title = "Share")
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return false;

                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = text,
                    Title = title
                });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Text sharing error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region File Download

        /// <summary>
        /// Download file from URL and save to local storage
        /// </summary>
        /// <param name="url">File URL</param>
        /// <param name="fileName">Local file name</param>
        /// <param name="folder">Subfolder to save in</param>
        /// <param name="progress">Progress callback</param>
        /// <returns>Path to downloaded file</returns>
        public async Task<string> DownloadFileAsync(string url, string fileName, string folder = null,
            IProgress<double> progress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    throw new ArgumentException("URL is required", nameof(url));

                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentException("File name is required", nameof(fileName));

                var targetDir = string.IsNullOrEmpty(folder)
                    ? _documentsPath
                    : Path.Combine(_documentsPath, folder);

                Directory.CreateDirectory(targetDir);

                var filePath = Path.Combine(targetDir, fileName);

                using var client = new HttpClient();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var progressPercentage = (double)downloadedBytes / totalBytes;
                        progress?.Report(progressPercentage);
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File download error: {ex.Message}");
                throw new InvalidOperationException($"Failed to download file: {ex.Message}", ex);
            }
        }

        #endregion

        #region File Compression

        /// <summary>
        /// Compress image file to reduce size
        /// </summary>
        /// <param name="imageData">Original image data</param>
        /// <param name="quality">Compression quality (0.0 to 1.0)</param>
        /// <param name="maxWidth">Maximum width in pixels</param>
        /// <param name="maxHeight">Maximum height in pixels</param>
        /// <returns>Compressed image data</returns>
        public async Task<FileData> CompressImageAsync(FileData imageData, double quality = 0.8,
            int maxWidth = 1024, int maxHeight = 1024)
        {
            try
            {
                // This is a simplified implementation
                // In a real app, you'd use platform-specific image processing libraries

                // For demo purposes, we'll just return the original data
                // In production, implement actual image compression using libraries like:
                // - SkiaSharp
                // - ImageSharp
                // - Platform-specific APIs

                await Task.Delay(500); // Simulate processing time

                return new FileData
                {
                    FileName = $"compressed_{imageData.FileName}",
                    ContentType = imageData.ContentType,
                    Data = imageData.Data, // In real implementation, this would be compressed data
                    Size = imageData.Size
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image compression error: {ex.Message}");
                throw new InvalidOperationException($"Failed to compress image: {ex.Message}", ex);
            }
        }

        #endregion

        #region Storage Management

        /// <summary>
        /// Get available storage space
        /// </summary>
        /// <returns>Available space in bytes</returns>
        public long GetAvailableStorageSpace()
        {
            try
            {
                // Platform-specific implementation
#if ANDROID
                var path = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "/storage/emulated/0";
                var stat = new Android.OS.StatFs(path);
                return stat.AvailableBlocksLong * stat.BlockSizeLong;
#elif IOS
                var documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                var drive = new System.IO.DriveInfo(Path.GetPathRoot(documents));
                return drive.AvailableFreeSpace;
#elif WINDOWS
                var drive = new System.IO.DriveInfo(Path.GetPathRoot(_documentsPath));
                return drive.AvailableFreeSpace;
#else
                // Fallback for other platforms
                return 1024L * 1024L * 1024L; // 1GB as fallback
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Storage space check error: {ex.Message}");
                return 1024L * 1024L * 1024L; // 1GB fallback on error
            }
        }

        /// <summary>
        /// Get used storage space by the app
        /// </summary>
        /// <returns>Used space in bytes</returns>
        public long GetUsedStorageSpace()
        {
            try
            {
                var documentsSize = GetDirectorySize(_documentsPath);
                var cacheSize = GetDirectorySize(_cachePath);
                return documentsSize + cacheSize;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Used storage check error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Clean up old files to free storage space
        /// </summary>
        /// <param name="olderThan">Delete files older than this timespan</param>
        /// <returns>Number of bytes freed</returns>
        public async Task<long> CleanupOldFilesAsync(TimeSpan olderThan)
        {
            try
            {
                var cutoffTime = DateTime.Now - olderThan;
                var bytesFreed = 0L;

                // Clean cache directory
                var cacheFiles = Directory.GetFiles(_cachePath);
                foreach (var file in cacheFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffTime)
                    {
                        bytesFreed += fileInfo.Length;
                        File.Delete(file);
                    }
                }

                // Clean temp directory
                var tempFiles = Directory.GetFiles(_tempPath);
                foreach (var file in tempFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffTime)
                    {
                        bytesFreed += fileInfo.Length;
                        File.Delete(file);
                    }
                }

                return bytesFreed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            // Clean up temporary files
            try
            {
                if (Directory.Exists(_tempPath))
                {
                    Directory.Delete(_tempPath, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dispose cleanup error: {ex.Message}");
            }
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// File validation result
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// File operation result
    /// </summary>
    public class FileOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public Exception Exception { get; set; }
    }

    #endregion
}