using TodoApp.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http;

namespace TodoApp.Client.Services
{
    public class ApiConsumerService
    {
        private readonly HttpClient _httpClient;

        public ApiConsumerService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<List<TodoTask>?> GetTasksAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<TodoTask>>("api/todotasks");
        }

        // REQUIRED METHOD FOR EDIT PAGE LOAD - NOW RETURNS ERROR MESSAGE ON FAILURE
        public async Task<(TodoTask? Task, string? ErrorMessage)> GetTaskByIdAsync(long id)
        {
            var response = await _httpClient.GetAsync($"api/todotasks/{id}");

            if (response.IsSuccessStatusCode)
            {
                var task = await response.Content.ReadFromJsonAsync<TodoTask>();
                return (task, null); // Success: returns the task and null error
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (null, "Task with the specified ID was not found (404).");
            }

            // Generic failure
            return (null, $"API Error: Could not retrieve task. Status Code: {response.StatusCode}.");
        }

        // REQUIRED METHOD FOR EDIT PAGE POST
        public async Task<(bool Success, string? ErrorMessage)> UpdateTaskAsync(TodoTask task)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/todotasks/{task.Id}", task);

            if (response.IsSuccessStatusCode) return (true, null);

            // Handle 409 Conflict (Business Rule: Duplicate Name)
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>();
                //  (Directly looking for "Name" key at the root)
                var errorObject = content.GetProperty("Name").EnumerateArray().FirstOrDefault();
                var error = errorObject.GetString();
                return (false, error);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return (false, "Validation failed on the server.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, "Task not found.");
            }

            return (false, $"API Error: {response.StatusCode}");
        }

        // Method for adding a task
        public async Task<(bool Success, string? ErrorMessage)> AddTaskAsync(TodoTask task)
        {
            var response = await _httpClient.PostAsJsonAsync("api/todotasks", task);

            if (response.IsSuccessStatusCode) return (true, null);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>();
                //var error = content.GetProperty("errors").GetProperty("Name").EnumerateArray().FirstOrDefault().GetString();
                // NEW (Directly looking for "Name" key at the root)
                var errorObject = content.GetProperty("Name").EnumerateArray().FirstOrDefault();
                var error = errorObject.GetString();
                return (false, error);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return (false, "Validation failed on the server.");
            }

            return (false, $"API Error: {response.StatusCode}");
        }

        // Existing method for deleting a task
        public async Task<bool> DeleteTaskAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"api/todotasks/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
