using Microsoft.AspNetCore.Mvc;
using TodoApp.Models;
using TodoApp.Api.Services;

[Route("api/[controller]")]
[ApiController]
public class TodoTasksController : ControllerBase
{
    private readonly TodoService _service;

    public TodoTasksController(TodoService service) => _service = service;

    // 1. List existing tasks
    [HttpGet]
    public ActionResult<IEnumerable<TodoTask>> GetTasks() => Ok(_service.GetAll());

    // Action Method to handle client GET request (e.g., GET /api/todotasks/1)
    [HttpGet("{id}")]
    public IActionResult GetTask(long id)
    {
        var task = _service.GetById(id);

        if (task == null)
        {
            return NotFound($"Task with ID {id} not found.");
        }

        return Ok(task);
    }

    // 2. Add tasks
    [HttpPost]
    public IActionResult PostTask([FromBody] TodoTask task)
    {
        // Server-side validation (Data Annotations)
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (newTask, error) = _service.AddTask(task);

        // Server-side business validation (Duplicate Name)
        if (newTask == null)
        {
            ModelState.AddModelError("Name", error!);
            return Conflict(ModelState); // HTTP 409
        }
        return CreatedAtAction(nameof(GetTasks), newTask);
    }

    // 2. Edit tasks
    [HttpPut("{id}")]
    public IActionResult PutTask(long id, [FromBody] TodoTask task)
    {
        // Server-side validation (Data Annotations)
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Ensure the ID in the route matches the ID in the body
        if (id != task.Id)
        {
            return BadRequest("ID in URL must match ID in body.");
        }

        var (updatedTask, error) = _service.UpdateTask(id, task);

        if (error != null && error.Contains("not found")) return NotFound(error);

        // Server-side business validation (Duplicate Name)
        if (error != null)
        {
            ModelState.AddModelError("Name", error);
            return Conflict(ModelState); // HTTP 409
        }

        return NoContent();
    }

    // 3. Deletion of completed tasks only
    [HttpDelete("{id}")]
    public IActionResult DeleteTask(long id)
    {
        if (_service.DeleteTask(id)) return NoContent();

        // Return 404/400 if not found OR not completed
        return NotFound("Task not found or is not completed and cannot be deleted.");
    }
}
