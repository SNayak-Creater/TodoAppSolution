using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoApp.Models;
using TodoApp.Client.Services;

namespace TodoApp.Client.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApiConsumerService _apiService;

        //public IndexModel(ApiConsumerService apiService) => _apiService = apiService;
        public IndexModel(ApiConsumerService apiService)
        {
            _apiService = apiService;
        }

        public List<TodoTask> Tasks { get; set; } = new();

        [BindProperty]
        public TodoTask NewTask { get; set; } = new();

        public async Task OnGetAsync()
        {
            Tasks = (await _apiService.GetTasksAsync()) ?? new List<TodoTask>();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                Tasks = (await _apiService.GetTasksAsync()) ?? new List<TodoTask>();
                return Page();
            }

            var (success, error) = await _apiService.AddTaskAsync(NewTask);

            if (!success)
            {
                ModelState.AddModelError("NewTask.Name", error ?? "An unknown API error occurred.");
                Tasks = (await _apiService.GetTasksAsync()) ?? new List<TodoTask>();
                return Page();
                //load only added data
            }

            return RedirectToPage();
        }

        // Delete handler argument changed to long id
        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            await _apiService.DeleteTaskAsync(id);
            return RedirectToPage();
        }
    }
}
