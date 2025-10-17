using System.ComponentModel.DataAnnotations;

namespace TodoApp.Models
{
    public enum TaskStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public class TodoTask
    {
        // ID is now long (Int64)
        public long Id { get; set; }

        [Required(ErrorMessage = "Task name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required.")]
        [Range(1, 10, ErrorMessage = "Priority must be between 1 (highest) and 10.")]
        public int Priority { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
    }
}
