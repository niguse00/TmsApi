using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;





namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(
    TmsDbContext context,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

    public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        // Create the enrollment
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Student {StudentId} enrolled in Course {CourseId} with EnrollmentId {EnrollmentId}",
            request.StudentId,
            courseId,
            enrollment.Id);

        // Re-read the created enrollment
        return await GetByIdAsync(courseId, enrollment.Id, ct)
               ?? throw new InvalidOperationException("Failed to retrieve the created enrollment.");
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<EnrollmentResponseDto?> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(
    int studentId,
    string courseCode,
    CancellationToken ct)
{
    return context.Enrollments
        .AnyAsync(e =>
            e.StudentId == studentId &&
            e.Course.Code == courseCode,
            ct);
}
    

public async Task AddAsync(
    Enrollment enrollment,
    CancellationToken ct)
{
    context.Enrollments.Add(enrollment);
    await context.SaveChangesAsync(ct);
}

    public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct)
    {
        return context.Enrollments
        .Include(e => e.Course)
        .Where(e => e.StudentId == studentId)
        .ToListAsync(ct);
    }
}


public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt);

public class TmsDatabaseException(string message) : Exception(message);