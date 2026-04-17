using Foody.Tests.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    public class FoodyTests
    {
        private RestClient client;
        private static string foodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("user202", "123456");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:81")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:81");
            RestRequest request = new RestRequest("api/User/Authentication", Method.Post);
            request.AddJsonBody(new {username, password});
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateFoodWithRequiredFields_ShouldReturnSuccess_SomeChangesAdded()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "Salad",
                Description = "Chicken Salad",
                Url = ""
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            foodId = readyResponse.FoodId;
        }

        [Order(2)]
        [Test]
        public void EditFoodTitle_ShourReturnSuccess()
        {
            RestRequest request = new RestRequest($"/api/Food/Edit/{foodId}", Method.Patch);
            request.AddBody(new [] 
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Edited Chicken Salad"
                }

            });

            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Order(3)]  
        [Test]
        public void GetAllFoods_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest("api/Food/All", Method.Get);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            List<ApiResponseDTO> readyResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse, Is.Not.Empty);
            Assert.That(readyResponse.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Order(4)]
        [Test]
        public void DeleteExistingFood_ShournReturnSuccess()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{foodId}", Method.Delete);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateFoodWithoutRequiredFields_ShouldReturnBadRequest()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddBody(food);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditTitleOfNonEexistingFood_ShouldReturnNotFound()
        {
            string nonExistingFoodId = "12345";
            RestRequest request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);

            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken Salad"
                }
            });

            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExisitngFood_ShouldReturnNotFound()
        {
            string nonExistingFoodId = "12345";
            RestRequest request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}