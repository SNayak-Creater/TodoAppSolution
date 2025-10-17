using Xunit;
using TodoApp.Api.Services;
using TodoApp.Models;
using System.Linq;
using System.Collections.Generic;
using TaskStatus = TodoApp.Models.TaskStatus;

namespace TodoApp.Tests
{
    // Note: Since TodoService uses static state (ConcurrentDictionary), 
    // it's crucial to call ResetServiceData() before every test to ensure isolation.
    public class TodoServiceTests
    {
        // Helper to create a new service instance
        private TodoService CreateService() => new TodoService();

        // Helper to clear static data before each test
        private void ResetServiceData()
        {
            // The service implements ClearAll() to reset its static dictionary and ID counter.
            CreateService().ClearAll();
        }

        // --- Basic CRUD and Ordering Tests ---

        [Fact]
        public void Test01_AddTask_Success()
        {
            ResetServiceData();
            var service = CreateService();
            var task = new TodoTask { Name = "New Task 1", Priority = 5 };

            var (addedTask, error) = service.AddTask(task);

            Assert.True(addedTask != null);
            Assert.Null(error);
            Assert.True(addedTask.Id > 0);
            Assert.Equal(TaskStatus.NotStarted, addedTask.Status);
        }

        [Fact]
        public void Test02_GetAll_ReturnsCorrectCount()
        {
            ResetServiceData();
            var service = CreateService();
            service.AddTask(new TodoTask { Name = "Task A", Priority = 1 });
            service.AddTask(new TodoTask { Name = "Task B", Priority = 2 });

            var tasks = service.GetAll();

            // We expect 2 tasks since we cleared the seed data
            Assert.Equal(2, tasks.Count());
        }

        [Fact]
        public void Test03_GetAll_ReturnsTasksOrderedByPriority()
        {
            ResetServiceData();
            var service = CreateService();

            // Add tasks out of order of priority
            service.AddTask(new TodoTask { Name = "Low Priority", Priority = 10 });
            service.AddTask(new TodoTask { Name = "Highest Priority", Priority = 1 });
            service.AddTask(new TodoTask { Name = "Medium Priority", Priority = 5 });

            var actualNamesInOrder = service.GetAll().Select(t => t.Name).ToList();

            var expectedNamesInOrder = new List<string> {
                "Highest Priority", // Priority 1
                "Medium Priority",  // Priority 5
                "Low Priority"      // Priority 10
            };

            // Checking the sequence of names against the expected ordered sequence
            Assert.Equal(expectedNamesInOrder, actualNamesInOrder);
        }

        // --- Business Rule Tests: Validation and Constraints ---

        [Fact]
        public void Test04_AddTask_FailsOnEmptyName()
        {
            ResetServiceData();
            var service = CreateService();
            // Data Annotation validation is typically done outside the service, 
            // but we test the service's reliance on a valid model.
            var task = new TodoTask { Name = "", Priority = 1 };

            // Service method doesn't explicitly check empty string, but the API controller ModelState does.
            // This test is kept to ensure the core logic handles the state.
            var (addedTask, error) = service.AddTask(task);

            // The service prevents adding empty string after trimming logic fix
            Assert.Null(addedTask);
            Assert.NotNull(error);
        }

        [Fact]
        public void Test05_AddTask_FailsOnInvalidStatus()
        {
            ResetServiceData();
            var service = CreateService();
            // Cast an integer not defined in the enum (e.g., 999)
            var task = new TodoTask { Name = "Invalid Status Task", Priority = 1, Status = (TaskStatus)999 };

            var (addedTask, error) = service.AddTask(task);

            Assert.False(addedTask != null);
            Assert.NotNull(error);
            Assert.Contains("Invalid status value provided", error);
        }

        [Fact]
        public void Test06_AddTask_FailsOnDuplicateName_CaseInsensitive()
        {
            ResetServiceData();
            var service = CreateService();
            service.AddTask(new TodoTask { Name = "My Task", Priority = 1 });

            var duplicate = new TodoTask { Name = "my task", Priority = 5 };
            var (addedTask, error) = service.AddTask(duplicate);

            Assert.Null(addedTask);
            Assert.NotNull(error);
            Assert.Contains("already exists", error);
        }

        [Fact]
        public void Test07_AddTask_FailsOnDuplicateName_WithLeadingSpace()
        {
            ResetServiceData();
            var service = CreateService();
            service.AddTask(new TodoTask { Name = "Task B", Priority = 1 });

            // Should fail due to the trimming logic in the service
            var duplicate = new TodoTask { Name = " Task B", Priority = 5 };
            var (addedTask, error) = service.AddTask(duplicate);

            Assert.Null(addedTask);
            Assert.NotNull(error);
            Assert.Contains("already exists", error);
        }

        [Fact]
        public void Test08_AddTask_FailsOnDuplicateName_WithWhitespace()
        {
            ResetServiceData();
            var service = CreateService();
            service.AddTask(new TodoTask { Name = "Task C", Priority = 1 });

            // Should fail due to the trimming logic in the service
            var duplicate = new TodoTask { Name = "Task C ", Priority = 5 };
            var (addedTask, error) = service.AddTask(duplicate);

            Assert.Null(addedTask);
            Assert.NotNull(error);
            Assert.Contains("already exists", error);
        }

        // --- Update Tests ---

        [Fact]
        public void Test09_UpdateTask_Success_NameChange()
        {
            ResetServiceData();
            var service = CreateService();
            var (addedTask, _) = service.AddTask(new TodoTask { Name = "Old Name", Priority = 5 });

            var updateModel = new TodoTask
            {
                Name = "New Name",
                Priority = addedTask.Priority,
                Status = addedTask.Status
            };

            var (updatedTask, error) = service.UpdateTask(addedTask.Id, updateModel);

            Assert.True(updatedTask != null);
            Assert.Null(error);
            Assert.Equal("New Name", updatedTask.Name);
        }

        [Fact]
        public void Test10_UpdateTask_FailsOnDuplicateName()
        {
            ResetServiceData();
            var service = CreateService();
            var (task1, _) = service.AddTask(new TodoTask { Name = "First Task", Priority = 1 });
            var (task2, _) = service.AddTask(new TodoTask { Name = "Second Task", Priority = 2 });

            // Try to rename task2 to task1's name
            var updateModel = new TodoTask { Name = "First Task", Priority = 5, Status = TaskStatus.InProgress };
            var (updatedTask, error) = service.UpdateTask(task2.Id, updateModel);

            Assert.Null(updatedTask);
            Assert.NotNull(error);
            Assert.Contains("already exists", error);
        }

        [Fact]
        public void Test11_UpdateTask_FailsOnInvalidStatus()
        {
            ResetServiceData();
            var service = CreateService();
            var (addedTask, _) = service.AddTask(new TodoTask { Name = "Update Status Task", Priority = 1 });

            // Try to update status to an invalid integer
            var updateModel = new TodoTask { Name = addedTask.Name, Priority = addedTask.Priority, Status = (TaskStatus)999 };
            var (updatedTask, error) = service.UpdateTask(addedTask.Id, updateModel);

            Assert.Null(updatedTask);
            Assert.NotNull(error);
            Assert.Contains("Invalid status value provided", error);
        }

        [Fact]
        public void Test12_UpdateTask_AllowsSameName()
        {
            ResetServiceData();
            var service = CreateService();
            var (addedTask, _) = service.AddTask(new TodoTask { Name = "Check Name", Priority = 5 });

            // Update only the priority, keeping the same name
            var updateModel = new TodoTask { Name = "Check Name", Priority = 1, Status = TaskStatus.Completed };
            var (updatedTask, error) = service.UpdateTask(addedTask.Id, updateModel);

            Assert.True(updatedTask != null);
            Assert.Null(error);
            Assert.Equal(1, updatedTask.Priority);
            Assert.Equal(TaskStatus.Completed, updatedTask.Status);
        }

        [Fact]
        public void Test13_UpdateTask_NotFound()
        {
            ResetServiceData();
            var service = CreateService();
            var bogusId = 999;
            var updateModel = new TodoTask { Name = "Bogus", Priority = 1, Status = TaskStatus.Completed };

            var (updatedTask, error) = service.UpdateTask(bogusId, updateModel);

            Assert.Null(updatedTask);
            Assert.NotNull(error);
            Assert.Contains("Task not found", error);
        }

        // --- Deletion Tests (Conditional Business Rule) ---

        [Fact]
        public void Test14_DeleteTask_Success_WhenCompleted()
        {
            ResetServiceData();
            var service = CreateService();
            var task = new TodoTask { Name = "Delete Me", Priority = 1, Status = TaskStatus.Completed };
            var (addedTask, _) = service.AddTask(task);

            var result = service.DeleteTask(addedTask.Id);

            Assert.True(result);
            // Must be empty since we cleared seed data and only added one
            Assert.Empty(service.GetAll());
        }

        [Fact]
        public void Test15_DeleteTask_Fails_WhenNotStarted()
        {
            ResetServiceData();
            var service = CreateService();
            var task = new TodoTask { Name = "Keep Me", Priority = 1, Status = TaskStatus.NotStarted };
            var (addedTask, _) = service.AddTask(task);

            var result = service.DeleteTask(addedTask.Id);

            Assert.False(result);
            // Task should still exist
            Assert.Single(service.GetAll());
        }

        [Fact]
        public void Test16_DeleteTask_Fails_WhenInProgress()
        {
            ResetServiceData();
            var service = CreateService();
            var task = new TodoTask { Name = "Hold On", Priority = 1, Status = TaskStatus.InProgress };
            var (addedTask, _) = service.AddTask(task);

            var result = service.DeleteTask(addedTask.Id);

            Assert.False(result);
            // Task should still exist
            Assert.Single(service.GetAll());
        }

        [Fact]
        public void Test17_DeleteTask_Fails_WhenIdNotFound()
        {
            ResetServiceData();
            var service = CreateService();

            var result = service.DeleteTask(999);

            Assert.False(result);
            Assert.Empty(service.GetAll());
        }

        [Fact]
        public void Test18_UpdateTask_CanSetStatusToCompleted()
        {
            ResetServiceData();
            var service = CreateService();
            var (addedTask, _) = service.AddTask(new TodoTask { Name = "To Be Completed", Priority = 5 });

            var updateModel = new TodoTask
            {
                Name = addedTask.Name,
                Priority = addedTask.Priority,
                Status = TaskStatus.Completed // Change status
            };

            var (updatedTask, error) = service.UpdateTask(addedTask.Id, updateModel);

            Assert.True(updatedTask != null);
            Assert.Null(error);
            Assert.Equal(TaskStatus.Completed, updatedTask.Status);
        }

        [Fact]
        public void Test19_GetById_ReturnsCorrectTask()
        {
            ResetServiceData();
            var service = CreateService();
            var (task1, _) = service.AddTask(new TodoTask { Name = "Check ID 1", Priority = 1 });
            var (task2, _) = service.AddTask(new TodoTask { Name = "Check ID 2", Priority = 2 });

            var retrievedTask = service.GetById(task1.Id);

            Assert.NotNull(retrievedTask);
            Assert.Equal("Check ID 1", retrievedTask.Name);
            Assert.Equal(task1.Id, retrievedTask.Id);
        }
    }
}
