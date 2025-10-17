using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TodoApp.Models;
using TodoApp.Client.Services;
using TaskStatus = TodoApp.Models.TaskStatus;

namespace TodoApp.Client.Pages
{
    public class EditModel : PageModel
    {
        private readonly ApiConsumerService _apiService;

        public EditModel(ApiConsumerService apiService) => _apiService = apiService;

        [BindProperty]
        public TodoTask TaskToEdit { get; set; } = new();

        public SelectList StatusOptions { get; set; } = new(Enum.GetValues(typeof(TaskStatus)));

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        // GET: Loads the task based on the {id} route parameter
        public async Task<IActionResult> OnGetAsync(long id)
        {
            // FIX: Deconstruct the tuple into the task object and the error message.
            var (task, error) = await _apiService.GetTaskByIdAsync(id);

            // Now we check if the task object itself is null.
            if (task == null)
            {
                // If task is null (e.g., 404 Not Found from API), display the error message on the Index page
                TempData["Error"] = error ?? "Task not found.";
                return RedirectToPage("/Index");
            }

            TaskToEdit = task;
            // Select the current status when initializing the dropdown
            StatusOptions = new SelectList(Enum.GetValues(typeof(TaskStatus)), TaskToEdit.Status);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Server-Side Validation (Client App)
            if (!ModelState.IsValid)
            {
                StatusOptions = new SelectList(Enum.GetValues(typeof(TaskStatus)), TaskToEdit.Status);
                return Page();
            }

            var (success, error) = await _apiService.UpdateTaskAsync(TaskToEdit);

            if (!success)
            {
                // Handle business logic errors (like duplicate name)
                ModelState.AddModelError("TaskToEdit.Name", error ?? "An unknown error occurred during update.");
                StatusOptions = new SelectList(Enum.GetValues(typeof(TaskStatus)), TaskToEdit.Status);
                return Page();
            }

            return RedirectToPage("/Index");
        }
    }
}
