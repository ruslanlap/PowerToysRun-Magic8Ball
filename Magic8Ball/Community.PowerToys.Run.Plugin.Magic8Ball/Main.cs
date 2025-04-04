using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using Community.PowerToys.Run.Plugin.Magic8Ball.Models;
using Community.PowerToys.Run.Plugin.Magic8Ball.Services;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Magic8Ball
{
    /// <summary>
    /// Main class of the Magic 8-Ball plugin that implements all required interfaces.
    /// </summary>
    public class Main : IPlugin, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IDisposable
    {
        /// <summary>
        /// ID of the plugin.
        /// </summary>
        public static string PluginID => "3BBD724C46964C4187B055A94E7E6852";

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Magic 8-Ball";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "Ask the Magic 8-Ball a yes-or-no question and receive a fortune-telling response";

        /// <summary>
        /// Additional options for the plugin.
        /// </summary>
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => [
            new()
            {
                Key = nameof(EnableAnimations),
                DisplayLabel = "Enable animations",
                DisplayDescription = "Show animation while waiting for a response",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = EnableAnimations,
            },
            new()
            {
                Key = nameof(EnableSoundEffects),
                DisplayLabel = "Enable sound effects",
                DisplayDescription = "Play sound effects during animations",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = EnableSoundEffects,
            },
            new()
            {
                Key = nameof(UseBiasedResponses),
                DisplayLabel = "Use biased responses",
                DisplayDescription = "Analyze your question to provide more relevant responses",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = UseBiasedResponses,
            }
        ];

        public bool EnableAnimations { get; private set; } = true;
        public bool EnableSoundEffects { get; private set; } = true;
        public bool UseBiasedResponses { get; private set; } = false;

        private PluginInitContext? Context { get; set; }
        private string? IconPath { get; set; }
        private EightBallApiService? ApiService { get; set; }
        private MediaPlayer? SoundPlayer { get; set; }

        private bool Disposed { get; set; }
        private const string DefaultImagePath = "Images/magic8ball.dark.png";
        private const string AnimationImagePath = "Images/magic8ball-animation.gif";
        private const string ShakeSound = "Sounds/shake.wav";

        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            Log.Info("Query: " + query.Search, GetType());

            if (string.IsNullOrWhiteSpace(query.Search))
            {
                return [
                    new Result
                    {
                        QueryTextDisplay = string.Empty,
                        IcoPath = IconPath,
                        Title = "Ask the Magic 8-Ball a question",
                        SubTitle = "Type a yes-or-no question and press Enter",
                        Score = 100,
                    }
                ];
            }

            return [
                new Result
                {
                    QueryTextDisplay = query.Search,
                    IcoPath = IconPath,
                    Title = query.Search,
                    SubTitle = "Press Enter to ask the Magic 8-Ball",
                    Score = 100,
                    Action = _ => ShowMagic8BallWindow(query.Search, UseBiasedResponses),
                    ContextData = query.Search,
                }
            ];
        }

        /// <summary>
        /// Return a filtered list for delayed execution, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <param name="delayedExecution">Indicates if this is a delayed execution.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query, bool delayedExecution)
        {
            if (delayedExecution && !string.IsNullOrWhiteSpace(query.Search))
            {
                return Query(query);
            }

            return [];
        }

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
        public void Init(PluginInitContext context)
        {
            Log.Info("Init", GetType());

            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());

            ApiService = new EightBallApiService(GetType());
            SoundPlayer = new MediaPlayer();
        }

        /// <summary>
        /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
        /// </summary>
        /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
        /// <returns>A list context menu entries.</returns>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            Log.Info("LoadContextMenus", GetType());

            if (selectedResult?.ContextData is string question)
            {
                return [
                    new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "Ask (Enter)",
                        FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                        Glyph = "\xE8AF", // Question
                        AcceleratorKey = Key.Enter,
                        Action = _ => ShowMagic8BallWindow(question, false),
                    },
                    new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "Use biased response (Ctrl+Enter)",
                        FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                        Glyph = "\xE8C5", // Filter
                        AcceleratorKey = Key.Enter,
                        AcceleratorModifiers = ModifierKeys.Control,
                        Action = _ => ShowMagic8BallWindow(question, true),
                    }
                ];
            }

            return [];
        }

        /// <summary>
        /// Creates setting panel.
        /// </summary>
        /// <returns>The control.</returns>
        /// <exception cref="NotImplementedException">method is not implemented.</exception>
        public Control CreateSettingPanel() => throw new NotImplementedException();

        /// <summary>
        /// Updates settings.
        /// </summary>
        /// <param name="settings">The plugin settings.</param>
        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            Log.Info("UpdateSettings", GetType());

            EnableAnimations = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(EnableAnimations))?.Value ?? true;
            EnableSoundEffects = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(EnableSoundEffects))?.Value ?? true;
            UseBiasedResponses = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(UseBiasedResponses))?.Value ?? false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Log.Info("Dispose", GetType());

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wrapper method for <see cref="Dispose()"/> that disposes additional objects and events from the plugin itself.
        /// </summary>
        /// <param name="disposing">Indicate that the plugin is disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
            {
                return;
            }

            if (Context?.API != null)
            {
                Context.API.ThemeChanged -= OnThemeChanged;
            }

            ApiService?.Dispose();
            SoundPlayer?.Close();

            Disposed = true;
        }

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite ? Context?.CurrentPluginMetadata.IcoPathLight : Context?.CurrentPluginMetadata.IcoPathDark;

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);

        private bool ShowMagic8BallWindow(string question, bool useBiasedResponse)
        {
            if (Context == null)
            {
                Log.Error("Context is null", GetType());
                return false;
            }

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Create and show the Magic 8-Ball window
                    var window = new Magic8BallResultWindow(this);
                    window.SetQuestion(question, useBiasedResponse);
                    window.Show();
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception("Error showing Magic 8-Ball window", ex, GetType());
                Context.API.ShowMsg(
                    "Magic 8-Ball Error",
                    "An error occurred: " + ex.Message,
                    IconPath);
                return false;
            }
        }

        // Keep this for legacy support or non-UI based responses
        private bool AskQuestion(string question, bool forceBiased = false)
        {
            if (ApiService == null || Context == null)
            {
                Log.Error("ApiService or Context is null", GetType());
                return false;
            }

            try
            {
                // Show animation and play sound if enabled
                if (EnableAnimations)
                {
                    Context.API.ShowMsg("Shaking the Magic 8-Ball...", question, IconPath);

                    if (EnableSoundEffects)
                    {
                        PlayShakeSound();
                    }
                }

                // Get response from API
                Task.Run(async () =>
                {
                    EightBallResponse? response = null;

                    // Small delay to show animation
                    if (EnableAnimations)
                    {
                        await Task.Delay(1500);
                    }

                    // Get response based on settings
                    if (forceBiased || UseBiasedResponses)
                    {
                        response = await ApiService.GetBiasedResponseAsync(question);
                    }
                    else
                    {
                        response = await ApiService.GetRandomResponseAsync();
                    }

                    // Display response
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (response != null)
                        {
                            // Format the message with emojis based on response type
                            string emoji = response.Type.ToLower() switch
                            {
                                "positive" => "✅",
                                "negative" => "❌",
                                "neutral" => "⚠️",
                                _ => "🔮"
                            };

                            Context.API.ShowMsg(
                                $"Magic 8-Ball says: {emoji}", 
                                response.Reading, 
                                IconPath);
                        }
                        else
                        {
                            Context.API.ShowMsg(
                                "Magic 8-Ball Error", 
                                "Could not get a response. Please try again later.", 
                                IconPath);
                        }
                    });
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception("Error asking question", ex, GetType());
                Context.API.ShowMsg(
                    "Magic 8-Ball Error",
                    "An error occurred: " + ex.Message,
                    IconPath);
                return false;
            }
        }

        private void PlayShakeSound()
        {
            try
            {
                if (SoundPlayer != null)
                {
                    var pluginDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var soundPath = Path.Combine(pluginDirectory!, ShakeSound);

                    if (File.Exists(soundPath))
                    {
                        SoundPlayer.Open(new Uri(soundPath));
                        SoundPlayer.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception("Error playing sound", ex, GetType());
            }
        }
    }
}