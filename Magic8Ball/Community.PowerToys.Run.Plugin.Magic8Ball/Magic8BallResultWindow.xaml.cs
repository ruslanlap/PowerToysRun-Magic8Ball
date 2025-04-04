using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Community.PowerToys.Run.Plugin.Magic8Ball.Models;

namespace Community.PowerToys.Run.Plugin.Magic8Ball
{
    public partial class Magic8BallResultWindow : Window
    {
        private string _question = string.Empty;
        private bool _useBiasedResponse;
        private Main _pluginInstance;
        private MediaPlayer _soundPlayer;
        private bool _isAnimating = false;
        private bool _enableAnimations;
        private bool _enableSoundEffects;

        public Magic8BallResultWindow(Main pluginInstance)
        {
            InitializeComponent();
            _pluginInstance = pluginInstance;
            _soundPlayer = new MediaPlayer();

            // Get settings from the plugin instance
            _enableAnimations = pluginInstance.EnableAnimations;
            _enableSoundEffects = pluginInstance.EnableSoundEffects;

            // Initialize with static image
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            if (assemblyDirectory != null)
            {
                var imagePath = Path.Combine(assemblyDirectory, "Images", "magic8ball.dark.png");

                if (File.Exists(imagePath))
                {
                    StaticBallImage.Source = new BitmapImage(new Uri(imagePath));
                }
            }
        }

        public void SetQuestion(string question, bool useBiasedResponse)
        {
            _question = question;
            _useBiasedResponse = useBiasedResponse;
            QuestionTextBlock.Text = question;

            // Hide answer triangle initially
            AnswerTriangle.Visibility = Visibility.Collapsed;

            // Immediately start animation
            StartAnimation();
        }

        private void StartAnimation()
        {
            _isAnimating = true;

            // Hide static image and show animation only if animations are enabled
            StaticBallImage.Visibility = Visibility.Collapsed;
            AnimationContainer.Visibility = _enableAnimations ? Visibility.Visible : Visibility.Collapsed;

            // Play shake sound if available and if sound effects are enabled
            if (_enableSoundEffects)
            {
                PlayShakeSound();
            }

            // Only load the GIF if animations are enabled
            if (_enableAnimations)
            {
                var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                if (assemblyDirectory != null)
                {
                    var imagePath = Path.Combine(assemblyDirectory, "Images", "magic8ball.gif");
                    if (File.Exists(imagePath))
                    {
                        // For a MediaElement, use Uri directly
                        BallAnimation.Source = new Uri(imagePath);
                        BallAnimation.Play(); // Make sure to play the media
                    }
                }

                // Simulate animation with ball movement (since GIF loading is complex in WPF)
                // We'll use a StoryBoard to create the shaking effect
                var shakeAnimation = new Storyboard();

                // Create a TranslateTransform for the ball
                var translateTransform = new TranslateTransform();
                BallAnimation.RenderTransform = translateTransform;

                // Add horizontal animation
                var horizontalAnimation = new DoubleAnimation
                {
                    From = -10,
                    To = 10,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(10) // Shake 10 times
                };

                Storyboard.SetTarget(horizontalAnimation, BallAnimation);
                Storyboard.SetTargetProperty(horizontalAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                shakeAnimation.Children.Add(horizontalAnimation);

                // Add vertical animation
                var verticalAnimation = new DoubleAnimation
                {
                    From = -5,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(75),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(7) // Shake 7 times
                };

                Storyboard.SetTarget(verticalAnimation, BallAnimation);
                Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                shakeAnimation.Children.Add(verticalAnimation);

                // Set completion action
                shakeAnimation.Completed += (s, e) => 
                {
                    AnimationContainer.Visibility = Visibility.Collapsed;
                    StaticBallImage.Visibility = Visibility.Visible;
                    AnswerTriangle.Visibility = Visibility.Visible;
                    _isAnimating = false;

                    // Get a response from the API
                    GetResponseFromApi();
                };

                // Start animation
                shakeAnimation.Begin();
            }
            else
            {
                // If animations are disabled, skip directly to getting the response
                AnswerTriangle.Visibility = Visibility.Visible;
                _isAnimating = false; // Make sure this is reset even when animations are disabled
                GetResponseFromApi();
            }
        }

        private void PlayShakeSound()
        {
            try
            {
                var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                if (assemblyDirectory != null)
                {
                    // Check for shake.wav first, then fall back to magic.wav if needed
                    var soundPath = Path.Combine(assemblyDirectory, "Sounds", "shake.wav");

                    if (!File.Exists(soundPath))
                    {
                        // Try the alternative sound file if the primary one doesn't exist
                        soundPath = Path.Combine(assemblyDirectory, "Sounds", "magic.wav");
                    }

                    if (File.Exists(soundPath))
                    {
                        _soundPlayer.Open(new Uri(soundPath));
                        _soundPlayer.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        private async void GetResponseFromApi()
        {
            try
            {
                // Get new EightBallApiService instance
                var apiService = new Services.EightBallApiService(_pluginInstance.GetType());

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
                    // Truncate to fit in triangle if needed
                    string displayText = response.Reading;
                    if (displayText.Length > 10)
                    {
                        displayText = SplitToFitTriangle(displayText);
                    }

                    AnswerText.Text = displayText;
                    ResponseTextBlock.Text = response.Reading;

                    // Set response type with emoji
                    string emoji = response.Type.ToLower() switch
                    {
                        "positive" => "✅ Positive",
                        "negative" => "❌ Negative",
                        "neutral" => "⚠️ Neutral",
                        _ => "🔮 Unknown"
                    };

                    ResponseTypeTextBlock.Text = emoji;

                    // Clean up
                    apiService.Dispose();
                }
                else
                {
                    // Handle error
                    AnswerText.Text = "ERROR";
                    ResponseTextBlock.Text = "Could not get a response. Please try again later.";
                    ResponseTypeTextBlock.Text = "❌ Error";
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                AnswerText.Text = "ERROR";
                ResponseTextBlock.Text = $"An error occurred: {ex.Message}";
                ResponseTypeTextBlock.Text = "❌ Error";
            }
        }

        private string SplitToFitTriangle(string text)
        {
            // For longer phrases like "Concentrate and ask again", we need better splitting
            if (text.Length <= 10) return text;

            // Find spaces to split into multiple lines
            string[] words = text.Split(' ');

            if (words.Length <= 1)
            {
                // If only 1 word, just return with possible truncation
                return text.Length > 15 ? text.Substring(0, 12) + "..." : text;
            }

            // Try to balance the lines
            var result = new System.Text.StringBuilder();
            int currentLineLength = 0;
            int maxLineLength = 10; // Shorter lines for triangle
            int lineCount = 0;
            
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                // If adding this word would exceed max line length or we already have 3 lines
                if ((currentLineLength + word.Length > maxLineLength) || 
                    (currentLineLength > 0 && lineCount >= 3))
                {
                    // Start a new line
                    result.Append("\n");
                    currentLineLength = 0;
                    lineCount++;
                    
                    // Limit to 4 lines maximum to fit in triangle
                    if (lineCount >= 4)
                    {
                        result.Append("...");
                        break;
                    }
                }
                else if (i > 0 && currentLineLength > 0)
                {
                    // Add space between words (not at the start of a line)
                    result.Append(" ");
                    currentLineLength++;
                }

                // For very long words, truncate them
                if (word.Length > maxLineLength && currentLineLength == 0)
                {
                    word = word.Substring(0, Math.Min(word.Length, maxLineLength - 3)) + "...";
                }

                result.Append(word);
                currentLineLength += word.Length;
            }

            return result.ToString();
        }

        private void BallAnimation_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Animation ended, show the static image
            AnimationContainer.Visibility = Visibility.Collapsed;
            StaticBallImage.Visibility = Visibility.Visible;
        }

        private void AskAgainButton_Click(object sender, RoutedEventArgs e)
        {
            // Always allow asking again, regardless of animation state
            // Just ensure we're not in the middle of an active animation
            if (!_isAnimating)
            {
                // Hide answer and show animation again
                AnswerTriangle.Visibility = Visibility.Collapsed;

                // Reset display
                AnswerText.Text = "";
                ResponseTextBlock.Text = "";
                ResponseTypeTextBlock.Text = "";

                // Start animation (or just get response if animations disabled)
                StartAnimation();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}