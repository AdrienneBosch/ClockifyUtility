using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace ClockifyUtility.Helpers;

public enum AppTheme
{
        System,
        Light,
        Dark
}

public sealed class ThemeChangedEventArgs : EventArgs
{
        public ThemeChangedEventArgs(AppTheme requestedTheme, AppTheme effectiveTheme)
        {
                RequestedTheme = requestedTheme;
                EffectiveTheme = effectiveTheme;
        }

        public AppTheme RequestedTheme { get; }

        public AppTheme EffectiveTheme { get; }
}

public static class ThemeManager
{
        private const string SettingsFileName = "theme-preferences.json";
        private static readonly string SettingsDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockifyUtility");
        private static readonly string SettingsPath = System.IO.Path.Combine(SettingsDirectory, SettingsFileName);

        private static ResourceDictionary? _currentThemeDictionary;
        private static AppTheme _requestedTheme = AppTheme.System;

        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public static AppTheme RequestedTheme => _requestedTheme;

        public static AppTheme EffectiveTheme { get; private set; } = AppTheme.System;

        public static void Initialize(Application application)
        {
                var savedTheme = LoadPreference();
                ApplyTheme(application, savedTheme, persist: false);
        }

        public static void ApplyTheme(Application application, AppTheme theme, bool persist = true)
        {
                _requestedTheme = theme;
                var effectiveTheme = theme == AppTheme.System ? GetSystemTheme() : theme;

                var dictionaries = application.Resources.MergedDictionaries;
                if (_currentThemeDictionary != null)
                {
                        dictionaries.Remove(_currentThemeDictionary);
                }

                _currentThemeDictionary = CreateThemeDictionary(effectiveTheme);
                dictionaries.Insert(0, _currentThemeDictionary);
                EffectiveTheme = effectiveTheme;

                if (persist)
                {
                        SavePreference(theme);
                }

                ThemeChanged?.Invoke(application, new ThemeChangedEventArgs(theme, effectiveTheme));
        }

        private static ResourceDictionary CreateThemeDictionary(AppTheme theme)
        {
                var uri = theme switch
                {
                        AppTheme.Light => new Uri("/ClockifyUtility;component/Themes/Theme.Light.xaml", UriKind.Relative),
                        _ => new Uri("/ClockifyUtility;component/Themes/Theme.Dark.xaml", UriKind.Relative)
                };

                return new ResourceDictionary { Source = uri };
        }

        private static AppTheme LoadPreference()
        {
                try
                {
                        if (System.IO.File.Exists(SettingsPath))
                        {
                                var json = System.IO.File.ReadAllText(SettingsPath);
                                var preference = JsonSerializer.Deserialize<ThemePreference>(json);
                                if (preference != null && Enum.TryParse(preference.RequestedTheme, out AppTheme parsed))
                                {
                                        return parsed;
                                }
                        }
                }
                catch
                {
                        // Ignore preference load failures and fall back to system theme
                }

                return AppTheme.System;
        }

        private static void SavePreference(AppTheme theme)
        {
                try
                {
                        if (!System.IO.Directory.Exists(SettingsDirectory))
                        {
                                _ = System.IO.Directory.CreateDirectory(SettingsDirectory);
                        }

                        var json = JsonSerializer.Serialize(new ThemePreference(theme.ToString()));
                        System.IO.File.WriteAllText(SettingsPath, json);
                }
                catch
                {
                        // Ignore persistence failures; app can operate without stored preferences
                }
        }

        private static AppTheme GetSystemTheme()
        {
                try
                {
                        object? value = Registry.GetValue(
                                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                                "AppsUseLightTheme",
                                null);
                        if (value is int intValue)
                        {
                                return intValue > 0 ? AppTheme.Light : AppTheme.Dark;
                        }
                }
                catch
                {
                        // Ignore registry access errors and fall back to light theme
                }

                return AppTheme.Light;
        }

        private sealed record ThemePreference(string RequestedTheme);
}
