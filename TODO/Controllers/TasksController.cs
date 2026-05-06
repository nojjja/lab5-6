using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TODO.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TaskWebSocketHandler _taskWebSocketHandler;

    public TasksController(AppDbContext context, TaskWebSocketHandler taskWebSocketHandler)
    {
        _context = context;
        _taskWebSocketHandler = taskWebSocketHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoTask>>> GetTasks()
        => await _context.Tasks.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoTask>> GetTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        return task == null ? NotFound() : task;
    }

    [HttpPost]
    public async Task<ActionResult<TodoTask>> Create(TodoTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        await _taskWebSocketHandler.BroadcastTaskChangedAsync("created", task);

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TodoTask updatedTask)
    {
        if (id != updatedTask.Id) return BadRequest();

        var existingTask = await _context.Tasks.FindAsync(id);
        if (existingTask == null) return NotFound();

        existingTask.Title = updatedTask.Title;
        existingTask.Description = updatedTask.Description;
        existingTask.IsCompleted = updatedTask.IsCompleted;

        await _context.SaveChangesAsync();
        await _taskWebSocketHandler.BroadcastTaskChangedAsync("updated", existingTask);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        await _taskWebSocketHandler.BroadcastTaskChangedAsync("deleted", task);

        return NoContent();
    }
}
