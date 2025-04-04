using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Community.PowerToys.Run.Plugin.Magic8Ball.Models;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Magic8Ball.Services
{
    /// <summary>
    /// Service to interact with the 8Ball API.
    /// </summary>
    public class EightBallApiService
    {
        private const string BaseUrl = "https://www.eightballapi.com";
        private readonly HttpClient _httpClient;
        private readonly Type _loggerType;

        /// <summary>
        /// Initializes a new instance of the <see cref="EightBallApiService"/> class.
        /// </summary>
        /// <param name="loggerType">Type used for logging.</param>
        public EightBallApiService(Type loggerType)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
            _loggerType = loggerType;
        }

        /// <summary>
        /// Gets a random response from the 8Ball API.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with the response.</returns>
        public async Task<EightBallResponse?> GetRandomResponseAsync()
        {
            try
            {
                Log.Info("Fetching random response from 8Ball API", _loggerType);
                var response = await _httpClient.GetFromJsonAsync<EightBallResponse>("/");

                // If we get a null response from the API, use the fallback
                if (response == null)
                {
                    Log.Info("Received null response from API, using fallback", _loggerType);
                    return GetFallbackResponse();
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("Error fetching random response from 8Ball API, using fallback", ex, _loggerType);
                return GetFallbackResponse();
            }
        }

        /// <summary>
        /// Gets a biased response from the 8Ball API based on the question.
        /// </summary>
        /// <param name="question">The question to analyze for sentiment.</param>
        /// <returns>A task representing the asynchronous operation with the response.</returns>
        public async Task<EightBallResponse?> GetBiasedResponseAsync(string question)
        {
            try
            {
                Log.Info($"Fetching biased response from 8Ball API for question: {question}", _loggerType);
                var response = await _httpClient.GetFromJsonAsync<EightBallResponse>($"/biased?question={Uri.EscapeDataString(question)}");

                // If we get a null response from the API, use the fallback
                if (response == null)
                {
                    Log.Info("Received null response from API, using fallback with question bias", _loggerType);
                    return GetFallbackResponseWithBias(question);
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Exception("Error fetching biased response from 8Ball API, using fallback", ex, _loggerType);
                return GetFallbackResponseWithBias(question);
            }
        }

        /// <summary>
        /// Gets a fallback response when the API is unavailable.
        /// </summary>
        /// <returns>A randomly selected fallback response.</returns>
        private EightBallResponse GetFallbackResponse()
        {
            // Classic Magic 8-Ball responses
            string[] readings = new[]
            {
                "It is certain", "Without a doubt", "Yes definitely", 
                "You may rely on it", "As I see it, yes", "Most likely",
                "Outlook good", "Signs point to yes", "Reply hazy try again",
                "Ask again later", "Better not tell you now", "Cannot predict now",
                "Concentrate and ask again", "Don't count on it", "My reply is no",
                "My sources say no", "Outlook not so good", "Very doubtful"
            };

            Random random = new Random();
            string reading = readings[random.Next(readings.Length)];

            // Determine type based on the reading
            string type;
            if (reading.Contains("certain") || reading.Contains("definitely") || 
                reading.Contains("yes") || reading.Contains("rely") || 
                reading.Contains("good") || reading.Contains("likely"))
            {
                type = "positive";
            }
            else if (reading.Contains("no") || reading.Contains("don't") ||
                     reading.Contains("doubtful") || reading.Contains("not"))
            {
                type = "negative";
            }
            else
            {
                type = "neutral";
            }

            return new EightBallResponse
            {
                Reading = reading,
                Type = type
            };
        }

        /// <summary>
        /// Gets a biased fallback response based on the question.
        /// </summary>
        /// <param name="question">The question to analyze.</param>
        /// <returns>A fallback response with simple question bias.</returns>
        private EightBallResponse GetFallbackResponseWithBias(string question)
        {
            question = question.ToLower();
            Random random = new Random();

            // Simple sentiment analysis based on common positive/negative words
            string[] positiveWords = { "good", "great", "best", "happy", "positive", "success", "win", "love", "right", "yes" };
            string[] negativeWords = { "bad", "worst", "fail", "sad", "negative", "wrong", "hate", "lose", "no", "don't" };

            int positiveScore = 0;
            int negativeScore = 0;

            // Count positive and negative words
            foreach (var word in positiveWords)
            {
                if (question.Contains(word))
                {
                    positiveScore++;
                }
            }

            foreach (var word in negativeWords)
            {
                if (question.Contains(word))
                {
                    negativeScore++;
                }
            }

            // Positive responses
            string[] positiveResponses = {
                "It is certain", "Without a doubt", "Yes definitely", 
                "You may rely on it", "As I see it, yes", "Most likely",
                "Outlook good", "Signs point to yes"
            };

            // Negative responses
            string[] negativeResponses = {
                "Don't count on it", "My reply is no",
                "My sources say no", "Outlook not so good", "Very doubtful"
            };

            // Neutral responses
            string[] neutralResponses = {
                "Reply hazy try again", "Ask again later", 
                "Better not tell you now", "Cannot predict now",
                "Concentrate and ask again"
            };

            string reading;
            string type;

            // Determine response type based on question sentiment
            if (positiveScore > negativeScore)
            {
                // Lean towards positive, but still allow for some randomness
                int rand = random.Next(10);
                if (rand < 7) // 70% chance of positive
                {
                    reading = positiveResponses[random.Next(positiveResponses.Length)];
                    type = "positive";
                }
                else if (rand < 9) // 20% chance of neutral
                {
                    reading = neutralResponses[random.Next(neutralResponses.Length)];
                    type = "neutral";
                }
                else // 10% chance of negative
                {
                    reading = negativeResponses[random.Next(negativeResponses.Length)];
                    type = "negative";
                }
            }
            else if (negativeScore > positiveScore)
            {
                // Lean towards negative, but still allow for some randomness
                int rand = random.Next(10);
                if (rand < 7) // 70% chance of negative
                {
                    reading = negativeResponses[random.Next(negativeResponses.Length)];
                    type = "negative";
                }
                else if (rand < 9) // 20% chance of neutral
                {
                    reading = neutralResponses[random.Next(neutralResponses.Length)];
                    type = "neutral";
                }
                else // 10% chance of positive
                {
                    reading = positiveResponses[random.Next(positiveResponses.Length)];
                    type = "positive";
                }
            }
            else
            {
                // No clear bias, use completely random response
                return GetFallbackResponse();
            }

            return new EightBallResponse
            {
                Reading = reading,
                Type = type
            };
        }

        /// <summary>
        /// Disposes the HttpClient.
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}