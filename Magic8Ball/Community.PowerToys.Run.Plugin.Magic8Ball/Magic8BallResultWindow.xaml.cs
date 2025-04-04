using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using Community.PowerToys.Run.Plugin.Magic8Ball.Models;
using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.Magic8Ball
{
    /// <summary>
    /// Interaction logic for Magic8BallResultWindow.xaml
    /// </summary>
    public partial class Magic8BallResultWindow : Window
    {
        #region Fields
        private string _question = string.Empty;
        private bool _useBiasedResponse;
        private Main _pluginInstance;
        private MediaPlayer _soundPlayer;
        private bool _isAnimating = false;
        private bool _enableAnimations;
        private bool _enableSoundEffects;

        // Animation storyboards - marked as nullable
        private Storyboard? _loadingAnimation;
        private Storyboard? _triangleEntranceAnimation;
        private Storyboard? _ballShakeAnimation;

        // Animation timings - reduced by 20%
        private const int ShakeDurationMs = 1200;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the Magic8BallResultWindow
        /// </summary>
        /// <param name="pluginInstance">The main plugin instance</param>
        public Magic8BallResultWindow(Main pluginInstance)
        {
            InitializeComponent();

            // Initialize fields
            _pluginInstance = pluginInstance ?? throw new ArgumentNullException(nameof(pluginInstance));
            _soundPlayer = new MediaPlayer();
            _enableAnimations = pluginInstance.EnableAnimations;
            _enableSoundEffects = pluginInstance.EnableSoundEffects;

            // Get animations from resources - using safe null handling
            _loadingAnimation = FindResource("LoadingAnimation") as Storyboard;
            _triangleEntranceAnimation = FindResource("TriangleEntranceAnimation") as Storyboard;
            _ballShakeAnimation = FindResource("BallShakeAnimation") as Storyboard;

            // Load static 8-ball image
            LoadStaticImage();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the question and begins the Magic 8-Ball animation
        /// </summary>
        /// <param name="question">The user's question</param>
        /// <param name="useBiasedResponse">Whether to use the biased response API</param>
        public void SetQuestion(string question, bool useBiasedResponse)
        {
            _question = question;
            _useBiasedResponse = useBiasedResponse;
            QuestionTextBlock.Text = question;

            // Hide answer triangle initially
            AnswerTriangle.Visibility = Visibility.Collapsed;

            // Start animation sequence
            StartAnimationSequence();
        }
        #endregion

        #region Private Methods

        #region Resource Loading
        /// <summary>
        /// Loads the static 8-ball image
        /// </summary>
        private void LoadStaticImage()
        {
            try
            {
                string? imagePath = GetResourcePath("Images", "magic8ball.dark.png");
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    StaticBallImage.Source = new BitmapImage(new Uri(imagePath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading static image: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the full path to a resource file
        /// </summary>
        /// <param name="folder">The resource folder</param>
        /// <param name="fileName">The resource file name</param>
        /// <returns>The full path to the resource</returns>
        private string? GetResourcePath(string folder, string fileName)
        {
            try
            {
                var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                if (assemblyDirectory != null)
                {
                    return Path.Combine(assemblyDirectory, folder, fileName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting resource path: {ex.Message}");
            }

            return null;
        }
        #endregion

        #region Animation and UI
        /// <summary>
        /// Starts the animation sequence for the Magic 8-Ball
        /// </summary>
        private void StartAnimationSequence()
        {
            _isAnimating = true;

            // Disable Ask Again button during animation
            AskAgainButton.IsEnabled = false;

            // Hide answer triangle initially
            AnswerTriangle.Visibility = Visibility.Collapsed;

            if (_enableAnimations)
            {
                // Play ball shaking animation
                _ballShakeAnimation?.Begin();

                // Show and start loading spinner animation
                LoadingSpinner.Visibility = Visibility.Visible;
                _loadingAnimation?.Begin();

                // Play shake sound if enabled
                if (_enableSoundEffects)
                {
                    PlayShakeSound();
                }

                // Simulate shaking with async delay
                SimulateShakingAnimation();
            }
            else
            {
                // Skip directly to getting the response if animations are disabled
                GetResponseFromApiAsync();
            }
        }

        /// <summary>
        /// Simulates the shaking animation with a timed delay
        /// </summary>
        private async void SimulateShakingAnimation()
        {
            try
            {
                // Use await Task.Delay for better async behavior
                await Task.Delay(ShakeDurationMs);

                // Only proceed if we're still in animating state
                if (_isAnimating)
                {
                    // Hide spinner animation
                    LoadingSpinner.Visibility = Visibility.Collapsed;
                    _loadingAnimation?.Stop();

                    // Reset ball position after shake
                    BallShakeTransform.X = 0;
                    BallShakeTransform.Y = 0;
                    BallRotateTransform.Angle = 0;

                    // Get the response
                    GetResponseFromApiAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in shake animation: {ex.Message}");
                // Fall back to getting response directly
                GetResponseFromApiAsync();
            }
        }

        /// <summary>
        /// Plays the shake sound effect
        /// </summary>
        private void PlayShakeSound()
        {
            try
            {
                string? soundPath = GetResourcePath("Sounds", "shake.wav");

                // Fall back to alternate sound if primary doesn't exist
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    soundPath = GetResourcePath("Sounds", "magic.wav");
                }

                if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
                {
                    _soundPlayer.Open(new Uri(soundPath));
                    _soundPlayer.MediaFailed += (s, e) => 
                        System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorException.Message}");
                    _soundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                // Log error but continue without sound
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Displays the answer in the UI
        /// </summary>
        /// <param name="answer">The 8-ball reading</param>
        /// <param name="responseType">The response type (positive, negative, neutral)</param>
        private void ShowAnswer(string answer, string responseType)
        {
            // Reset animation state
            _isAnimating = false;
            AskAgainButton.IsEnabled = true;

            // Hide animation elements
            LoadingSpinner.Visibility = Visibility.Collapsed;
            if (_loadingAnimation != null) _loadingAnimation.Stop();
            AnimationContainer.Visibility = Visibility.Collapsed;

            // Show static image
            StaticBallImage.Visibility = Visibility.Visible;

            // Set answer text - no more custom splitting needed since TextWrapping is now Wrap
            AnswerText.Text = answer;

            // Update response text
            ResponseTextBlock.Text = answer;

            // Set response type with appropriate emoji
            string emoji = responseType.ToLower() switch
            {
                "positive" => "✅",
                "negative" => "❌",
                "neutral" => "⚠️",
                _ => "❓"
            };

            ResponseTypeTextBlock.Text = $"{emoji} {responseType}";

            // Show and animate triangle appearance
            AnswerTriangle.Visibility = Visibility.Visible;
            _triangleEntranceAnimation?.Begin();
        }
        #endregion

        #region API Interaction
        /// <summary>
        /// Gets a response from the 8-Ball API asynchronously
        /// </summary>
        private async void GetResponseFromApiAsync()
        {
            try
            {
                // Create a new API service instance
                var apiService = new Services.EightBallApiService(_pluginInstance.GetType());

                try
                {
                    // Get response based on settings
                    EightBallResponse? response;

                    if (_useBiasedResponse)
                    {
                        response = await apiService.GetBiasedResponseAsync(_question);
                    }
                    else
                    {
                        response = await apiService.GetRandomResponseAsync();
                    }

                    // Display the response
                    if (response != null)
                    {
                        ShowAnswer(response.Reading, response.Type);
                    }
                    else
                    {
                        // Handle API error
                        ShowAnswer("ERROR", "Unknown");
                        ResponseTextBlock.Text = "Could not get a response. Please try again later.";
                    }
                }
                finally
                {
                    // Ensure we dispose of the API service
                    apiService.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                ShowAnswer("ERROR", "Unknown");
                ResponseTextBlock.Text = $"An error occurred: {ex.Message}";
            }
        }
        #endregion

        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the Ask Again button click
        /// </summary>
        private void AskAgainButton_Click(object sender, RoutedEventArgs e)
        {
            TryAskAgain();
        }

        /// <summary>
        /// Handles click on the 8-Ball display area
        /// </summary>
        private void BallDisplayArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TryAskAgain();
        }

        /// <summary>
        /// Attempts to ask again if not currently animating
        /// </summary>
        private void TryAskAgain()
        {
            // Only allow asking again if not currently animating
            if (!_isAnimating)
            {
                // Reset display texts
                AnswerText.Text = "";
                ResponseTextBlock.Text = "";
                ResponseTypeTextBlock.Text = "";

                // Hide triangle
                AnswerTriangle.Visibility = Visibility.Collapsed;

                // Start new animation and request
                StartAnimationSequence();
            }
        }

        /// <summary>
        /// Handles the Close button click
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles Media Element animation completion (legacy support)
        /// </summary>
        private void BallAnimation_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Legacy handler for MediaElement - kept for backward compatibility
            AnimationContainer.Visibility = Visibility.Collapsed;
            StaticBallImage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles keyboard shortcuts for the window
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter - Ask Again (only if not currently animating)
            if (e.Key == Key.Enter && !_isAnimating && AskAgainButton.IsEnabled)
            {
                TryAskAgain();
                e.Handled = true;
            }
            // Escape - Close
            else if (e.Key == Key.Escape)
            {
                CloseButton_Click(sender, e);
                e.Handled = true;
            }
        }
        #endregion
    }
}