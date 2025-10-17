using TodoApp.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TaskStatus = TodoApp.Models.TaskStatus;

namespace TodoApp.Api.Services
{
    // The service implements a singleton pattern using a static ConcurrentDictionary
    // to simulate a database for demonstration and testing purposes.
    public class TodoService
    {
        // Static dictionary to hold the tasks, shared across all service instances
        private static ConcurrentDictionary<long, TodoTask> _tasks = new ConcurrentDictionary<long, TodoTask>();
        private static long _nextId = 0;

        public TodoService()
        {
            // Seed data only if the dictionary is empty
            if (!_tasks.Any()) // Using .Any() to check if the dictionary is empty
            {
                // Note: Using Interlocked.Increment ensures thread-safe ID generation
                var task1 = new TodoTask { Id = Interlocked.Increment(ref _nextId), Name = "Design API", Priority = 1, Status = TaskStatus.Completed };
                var task2 = new TodoTask { Id = Interlocked.Increment(ref _nextId), Name = "Implement Services", Priority = 2, Status = TaskStatus.InProgress };
                var task3 = new TodoTask { Id = Interlocked.Increment(ref _nextId), Name = "Write Unit Tests", Priority = 3, Status = TaskStatus.NotStarted };

                _tasks.TryAdd(task1.Id, task1);
                _tasks.TryAdd(task2.Id, task2);
                _tasks.TryAdd(task3.Id, task3);
            }
        }

        public void ClearAll()
        {
            _tasks.Clear();
            _nextId = 0; // Reset ID counter
        }

        // --- Core CRUD Operations ---

        // Retrieves all tasks, ordered by priority
        public IEnumerable<TodoTask> GetAll() => _tasks.Values.OrderBy(t => t.Priority);

        // Retrieves a single task by ID
        public TodoTask? GetById(long id)
        {
            _tasks.TryGetValue(id, out var task);
            return task;
        }

        // Adds a new task
        public (TodoTask? Task, string? ErrorMessage) AddTask(TodoTask task)
        {
            // CRITICAL FIX: Validate that the incoming status is a valid enum value
            if (!Enum.IsDefined(typeof(TaskStatus), task.Status))
            {
                return (null, $"Invalid status value provided: {task.Status}. Status must be a defined member of TaskStatus.");
            }

            // CRITICAL FIX 1: Trim the incoming name before validating or saving
            var trimmedName = task.Name.Trim();

            if (IsDuplicateName(trimmedName))
            {
                return (null, $"A task named '{trimmedName}' already exists.");
            }

            // ID generation is thread-safe
            task.Id = Interlocked.Increment(ref _nextId);
            task.Name = trimmedName;
            // The status provided by the Model Binder (from the user's POST request) will now be used.

            if (_tasks.TryAdd(task.Id, task))
            {
                return (task, null);
            }
            return (null, "Failed to add task due to internal error.");
        }

        // Updates an existing task
        public (TodoTask? Task, string? ErrorMessage) UpdateTask(long id, TodoTask updatedTask)
        {
            if (!_tasks.TryGetValue(id, out var existingTask))
            {
                return (null, "Task not found.");
            }

            // CRITICAL FIX: Validate that the incoming status is a valid enum value
            if (!Enum.IsDefined(typeof(TaskStatus), updatedTask.Status))
            {
                return (null, $"Invalid status value provided: {updatedTask.Status}. Status must be a defined member of TaskStatus.");
            }

            // CRITICAL FIX 2: Trim the incoming name before validating or saving
            var trimmedName = updatedTask.Name.Trim();

            // Check for duplicate name against ALL OTHER tasks (excluding the current one being updated)
            if (IsDuplicateName(trimmedName, id))
            {
                return (null, $"A task named '{trimmedName}' already exists.");
            }

            // Apply updates
            existingTask.Name = trimmedName;
            existingTask.Priority = updatedTask.Priority;
            existingTask.Status = updatedTask.Status;

            // Note: In a real database, you'd save changes here. In ConcurrentDictionary,
            // we update properties directly since the reference is held.

            return (existingTask, null);
        }

        // Deletes a task
        public bool DeleteTask(long id)
        {
            if (_tasks.TryGetValue(id, out var task))
            {
                // Only allow deletion if the task is completed
                if (task.Status == TaskStatus.Completed)
                {
                    return _tasks.TryRemove(id, out _);
                }
            }
            // Returns false if task not found or status is not Completed
            return false;
        }

        // --- Validation Logic ---

        // Checks for duplicate name (case-insensitive and now whitespace-insensitive)
        // ExcludeId is optional, used during Update to ignore the current task's own name
        public bool IsDuplicateName(string newName, long? excludeId = null)
        {
            // CRITICAL FIX 3: Trim and convert to lower case for comparison
            var lowerTrimmedNewName = newName.Trim().ToLowerInvariant();

            return _tasks.Values
                .Where(t => t.Id != excludeId)
                .Any(t => t.Name.Trim().ToLowerInvariant() == lowerTrimmedNewName);
        }
    }
}
