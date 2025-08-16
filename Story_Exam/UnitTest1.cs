using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
//using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using Story_Exam.Models;

namespace Story_Exam
{
    [TestFixture]   
    public class StoryTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";
      
        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("deya", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnStoryId()
        {
            var story = new
            {
                Title = "new Story",
                Description = "Back in the time",
                Url = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request); 

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty, "Story Id should not be null or empty.");
        }

        [Test, Order(2)]    

        public void EditStoryTitle_ShouldReturnOk()
        {
            var editRequest = new StoryDTO
            {
                Title = "EditedMystery",
                Description = "Обновено описание със свежа интрига!",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test, Order(3)]

        public void GetAllStorySpoilers_ShouldReturnListOfStories()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = this.client.Execute(request);

            var responsItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteStorySpoiler_ShouldReturnOkAndConfirmationMessage()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "Response content should not be empty.");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var message = json.GetProperty("msg").GetString();

            Assert.That(message, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var storyRequest = new StoryDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]

        public void EditNonExistingStorySpoiler_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "999";  
            var editRequest = new StoryDTO
            {
                Title = "Edited Title",
                Description = "Trying to edit a non-existing spoiler.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Edit/{nonExistingStoryId}", Method.Put);
            request.AddJsonBody(editRequest);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Data.Msg, Does.Contain("No spoilers"));
        }
        [Test, Order(7)]
        public void DeleteNonExistingStorySpoiler_ShouldReturnBadRequest()
        {
            string nonExistingStoryId = "non-existent-id-999";
            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Data.Msg, Does.Contain("Unable to delete this story spoiler!"));
        }


        [OneTimeTearDown]
        public void Cleanup() 
        {
            client?.Dispose();
        }  
    }
}