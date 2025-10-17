using TodoApp.Models;
using System.Collections.Concurrent;
using TaskStatus = TodoApp.Models.TaskStatus;

namespace TodoApp.Api.Services
{
    public class TodoService
    {
        private static long _nextId = 1;
        private static readonly ConcurrentDictionary<long, TodoTask> _tasks = new();

        public TodoService()
        {
            // Seed data
            if (!_tasks.Any())
            {
                var task1 = new TodoTask { Id = _nextId++, Name = "Design API", Priority = 1, Status = TaskStatus.Completed };
                var task2 = new TodoTask { Id = _nextId++, Name = "Implement Services", Priority = 2, Status = TaskStatus.InProgress };
                var task3 = new TodoTask { Id = _nextId++, Name = "Write Unit Tests", Priority = 3, Status = TaskStatus.NotStarted };
                _tasks[task1.Id] = task1;
                _tasks[task2.Id] = task2;
                _tasks[task3.Id] = task3;
            }
        }

        public IEnumerable<TodoTask> GetAll() => _tasks.Values.OrderBy(t => t.Priority);

        // *** NEW/REQUIRED METHOD ***
        public TodoTask? GetById(long id) => _tasks.GetValueOrDefault(id);

        // Business Rule: Check for duplicate names (Case-insensitive)
        public bool IsDuplicateName(string name, long? excludeId = null)
        {
            return _tasks.Values.Any(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && t.Id != excludeId
            );
        }

        // Add (Includes Business Validation)
        public (TodoTask? Task, string? Error) AddTask(TodoTask task)
        {
            if (IsDuplicateName(task.Name))
                return (null, $"A task named '{task.Name}' already exists.");

            task.Id = _nextId++;
            task.Status = TaskStatus.NotStarted;
            _tasks[task.Id] = task;
            return (task, null);
        }

        // Edit (Includes Business Validation)
        public (TodoTask? Task, string? Error) UpdateTask(long id, TodoTask taskUpdate)
        {
            if (!_tasks.TryGetValue(id, out var existingTask))
                return (null, "Task not found.");

            if (IsDuplicateName(taskUpdate.Name, id))
                return (null, $"A task named '{taskUpdate.Name}' already exists.");

            existingTask.Name = taskUpdate.Name;
            existingTask.Priority = taskUpdate.Priority;
            existingTask.Status = taskUpdate.Status;

            return (existingTask, null);
        }

        // Deletion (Includes Business Rule)
        public bool DeleteTask(long id)
        {
            if (_tasks.TryGetValue(id, out var task))
            {
                // BUSINESS RULE: Deletion of completed tasks ONLY
                if (task.Status == TaskStatus.Completed)
                {
                    return _tasks.TryRemove(id, out _);
                }
            }
            return false;
        }
    }
}
