using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly TmsDbContext _context;

    public ReportsController(TmsDbContext context)
    {
        _context = context;
    }


    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;

        var students = await _context.Students
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses()
    {
        var result = await _context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        return Ok(result);
    }

    // 1. Active students with GPA >= 3.0
    [HttpGet("active-students-count")]
    public async Task<IActionResult> GetActiveStudentsCount()
    {
        var count = await _context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(count);
    }

    // 2. Courses with the most enrollments
    [HttpGet("courses-by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollment()
    {
        var list = await _context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        return Ok(list);
    }

    // 3. Average GPA per course
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await _context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();

        return Ok(list);
    }

    // 4A. Students with zero enrollments (NOT EXISTS)
    [HttpGet("students-without-enrollments")]
    public async Task<IActionResult> GetStudentsWithoutEnrollments()
    {
        var list = await _context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(list);
    }

    // 4B. Students with zero enrollments (LeftJoin - EF Core 10)
    [HttpGet("students-without-enrollments-leftjoin")]
    public async Task<IActionResult> GetStudentsWithoutEnrollmentsLeftJoin()
    {
        var list = await _context.Students
            .LeftJoin(
                _context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();

        return Ok(list);
    }
}