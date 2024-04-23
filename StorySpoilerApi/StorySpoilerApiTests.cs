using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoilerApi.Models;
using System.Net;
using System.Text.Json;

namespace StorySpoilerApi
{
    public class StorySpoilerApiTests
    {
        private RestClient client;
        private string url = "https://d5wfqm7y6yb3q.cloudfront.net";
        private string username = "storytester";
        private string password = "abc123";

        private static string storyId;

        [OneTimeSetUp]
        public void Setup()
        {
            var accessToken = GetToken(this.username, this.password);

            var options = new RestClientOptions(url)
            {
                Authenticator = new JwtAuthenticator(accessToken)
            };

            this.client = new RestClient(options);

        }

        private string GetToken(string username, string password)
        {
            var authClient = new RestClient(url);
            var authRequest = new RestRequest("/api/User/Authentication", Method.Post);

            var credentials = new
            {
                userName = username,
                password = password
            };

            authRequest.AddJsonBody(credentials);

            var authResponse = authClient.Execute(authRequest);

            if (authResponse.StatusCode == HttpStatusCode.OK)
            {
                var authResponseJson = JsonSerializer.Deserialize<AuthenticationResponseDto>(authResponse.Content);
                var accessToken = authResponseJson.AccessToken;

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new InvalidOperationException("Access token is empty.");
                }

                return accessToken;
            }
            else
            {
                throw new InvalidOperationException($"Request status is {authResponse.StatusCode}.");
            }
        }
        
        [Test, Order(1)]
        public void Post_CreateNewStory_ShouldCreateNewStory_WhenGivenValidInput()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Create", Method.Post);

            var story = new StorySpoilerDto
            {
                Title = "Some random title",
                Description = "Some description"
            };

            request.AddJsonBody(story);

            string creationMessage = "Successfully created!";

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(responseJson.StoryId, Is.Not.Empty);
            Assert.That(responseJson.Message, Is.EqualTo(creationMessage));

            storyId = responseJson.StoryId;

        }      

        [Test, Order(2)]
        public void Put_EditCreatedStory_ShouldEditStory_WhenGivenValidInputAndStoryId()
        {
            // Arrange
            var request = new RestRequest($"api/Story/Edit/{storyId}", Method.Put);

            var story = new StorySpoilerDto
            {
                Title = "Edited title",
                Description = "Edited description"
            };

            request.AddJsonBody(story);

            string editMessage = "Successfully edited";

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseJson.Message, Is.EqualTo(editMessage));

        }

        [Test, Order(3)]
        public void Delete_DeleteStory_ShouldDeleteStory_WhenGivenValidStoryId()
        {
            // Arrange
            var request = new RestRequest($"api/Story/Delete/{storyId}", Method.Delete);

            string deleteMessage = "Deleted successfully!";

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseJson.Message, Is.EqualTo(deleteMessage));

        }

        [Test, Order(4)]
        public void Post_CreateStory_ShouldReturn_BadRequest_WhenGivenInvalidInput()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Create", Method.Post);

            var story = new StorySpoilerDto { };

            request.AddJsonBody(story);

            // Act
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Test, Order(5)]
        public void Put_EditStory_ShouldReturn_NotFound_WhenGivenInvalidStoryId()
        {
            // Arrange
            var request = new RestRequest($"api/Story/Edit/InvalidId", Method.Put);

            var story = new StorySpoilerDto
            {
                Title = "Edited title",
                Description = "Edited description"
            };

            request.AddJsonBody(story);

            // Act
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        }

        [Test, Order(6)]
        public void Delete_DeleteStory_ShouldReturn_BadRequest_WhenGivenInvalidStoryId()
        {
            // Arrange
            var request = new RestRequest($"api/Story/Delete/InvalidId", Method.Delete);

            string deletionMessage = "Unable to delete this story spoiler!";

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responseJson.Message, Is.EqualTo(deletionMessage));
        }
    }
}