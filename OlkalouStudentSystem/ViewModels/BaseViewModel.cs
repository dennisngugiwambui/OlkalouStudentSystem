// ===============================
// ViewModels/BaseViewModel.cs - Base ViewModel Implementation
// ===============================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OlkalouStudentSystem.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _title = string.Empty;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual async Task NavigateToAsync(string route)
        {
            try
            {
                if (!string.IsNullOrEmpty(route))
                {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                    await Shell.Current.GoToAsync(route);
#else
                    // Fallback for other platforms
                    await Task.CompletedTask;
#endif
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
#if ANDROID || IOS || MACCATALYST || WINDOWS
                await Application.Current.MainPage.DisplayAlert("Navigation Error", "Unable to navigate to the requested page", "OK");
#endif
            }
        }

        protected async Task ShowErrorAsync(string title, string message)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
#else
                System.Diagnostics.Debug.WriteLine($"Error: {title} - {message}");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {ex.Message}");
            }
        }

        protected async Task ShowSuccessAsync(string title, string message)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
#else
                System.Diagnostics.Debug.WriteLine($"Success: {title} - {message}");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing success alert: {ex.Message}");
            }
        }

        protected async Task<bool> ShowConfirmAsync(string title, string message)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
#else
                System.Diagnostics.Debug.WriteLine($"Confirm: {title} - {message}");
                return true; // Default to true for non-UI environments
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing confirm dialog: {ex.Message}");
                return false;
            }
        }

        protected async Task<string> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = "", int maxLength = -1, Keyboard keyboard = null)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard ?? Keyboard.Default);
#else
                System.Diagnostics.Debug.WriteLine($"Prompt: {title} - {message}");
                return string.Empty; // Default empty for non-UI environments
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing prompt: {ex.Message}");
                return string.Empty;
            }
        }

        protected async Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                return await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons);
#else
                System.Diagnostics.Debug.WriteLine($"Action Sheet: {title}");
                return cancel; // Default to cancel for non-UI environments
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing action sheet: {ex.Message}");
                return cancel ?? string.Empty;
            }
        }

        protected async Task<string> GetSecureStorageAsync(string key)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                return await SecureStorage.GetAsync(key);
#else
                // Fallback for non-mobile platforms
                return await Task.FromResult<string>(null);
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting secure storage value for key '{key}': {ex.Message}");
                return null;
            }
        }

        protected async Task SetSecureStorageAsync(string key, string value)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                await SecureStorage.SetAsync(key, value);
#else
                // Fallback for non-mobile platforms
                await Task.CompletedTask;
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting secure storage value for key '{key}': {ex.Message}");
            }
        }

        protected async Task RemoveSecureStorageAsync(string key)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                SecureStorage.Remove(key);
#endif
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing secure storage value for key '{key}': {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual Task InitializeAsync() => Task.CompletedTask;

        public virtual Task LoadDataAsync() => Task.CompletedTask;

        public virtual Task RefreshAsync() => Task.CompletedTask;

        protected virtual void OnDisappearing() { }

        protected virtual void OnAppearing() { }
    }

    #region Platform-specific Keyboard class for non-MAUI environments
#if !ANDROID && !IOS && !MACCATALYST && !WINDOWS
    public static class Keyboard
    {
        public static object Default => new object();
        public static object Email => new object();
        public static object Numeric => new object();
        public static object Telephone => new object();
        public static object Text => new object();
        public static object Chat => new object();
        public static object Url => new object();
    }
#endif
    #endregion
}