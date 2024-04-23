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
        private static string lastStoryTitle;

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

        [OneTimeTearDown]
        public void TearDown()
        {
            var getAllRequest = new RestRequest("/api/Story/All", Method.Get);

            var getAllResponse = this.client.Execute(getAllRequest);
            var allStoriesJson = JsonSerializer.Deserialize<List<StorySpoilerDto>>(getAllResponse.Content);

            foreach (var story in allStoriesJson)
            {
                storyId = story.Id;

                var deleteRequest = new RestRequest($"/api/Story/Delete/{storyId}", Method.Delete);
                this.client.Execute(deleteRequest);
            }
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
        
        // Positive tests
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
            Assert.That(responseJson.Id, Is.Not.Empty);
            Assert.That(responseJson.Message, Is.EqualTo(creationMessage));

            storyId = responseJson.Id;

        }

        [Test, Order(2)]
        public void Get_SearchAllStorySpoilers_ShouldReturnAllAvailableStorySpoilers()
        {
            // Arrange
            var request = new RestRequest($"/api/Story/All", Method.Get);

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<List<StorySpoilerDto>>(response.Content);
            var lastStory = responseJson.Last();

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseJson.Count, Is.AtLeast(1));
            Assert.That(lastStory.Id, Is.EqualTo(storyId));

            lastStoryTitle = lastStory.Title;

        }

        [Test, Order(3)]  
        public void Get_SearchStorySpoilByTitle_ShouldReturnStory_WhenGivenExistingTitle()
        {
            // Arrange
            var request = new RestRequest($"/api/Story/Search?keyword={lastStoryTitle}");
            var title = lastStoryTitle;

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<List<StorySpoilerDto>>(response.Content);
            var foundStoryByTitle = responseJson.FirstOrDefault();

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseJson.Count, Is.AtLeast(1));
            Assert.That(foundStoryByTitle, Is.Not.Null);
            Assert.That(foundStoryByTitle.Title, Is.EqualTo(title));
        }

        [Test, Order(4)]
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

        [Test, Order(5)]
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

        // Negative tests
        [Test]
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

        [Test]
        public void Post_CreateStorySpoilers_ShouldReturnCreatedSpoilersWithDifferentIds_WhenGivenSameTitle()
        {
            // Arrange        
            RestResponse postRequest = null;            
            int storyPosts = 2;

            var story = new StorySpoilerDto
            {
                Title = "TitleTest",
                Description = "Description",
            };

            for(int i = 0; i < storyPosts; i++)
            {
                var request = new RestRequest("/api/Story/Create", Method.Post);
                request.AddJsonBody(story);

                postRequest = this.client.Execute(request);                
            }

            // Act
            var matchingTitleRequest = new RestRequest($"/api/Story/Search?keyword={story.Title}", Method.Get);
            var responseGet = this.client.Execute(matchingTitleRequest);
            var matchingTitles = JsonSerializer.Deserialize<List<StorySpoilerDto>>(responseGet.Content);            

            // Assert
            Assert.That(postRequest.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(responseGet.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            for (int i = 1; i <= matchingTitles.Count(); i++)
            {
                if(i < matchingTitles.Count())
                {
                    Assert.That(matchingTitles[i].Title, Is.EqualTo(matchingTitles[i - 1].Title));
                    Assert.That(matchingTitles[i].Id, Is.Not.EqualTo(matchingTitles[i - 1].Id));
                }                
            }         

        }

        [Test]
        public void Get_SearchForStory_ShouldReturnBadRequest_WhenGivenInvalidStoryTitle()
        {
            // Arrange
            var request = new RestRequest("/api/Story/Search?keyword=invalidTitle");
            string responseMessage = "No spoilers...";

            // Act
            var response = this.client.Execute(request);
            var responseJson = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(responseJson.Message, Is.EqualTo(responseMessage));

        }

        [Test]
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

        [Test]
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