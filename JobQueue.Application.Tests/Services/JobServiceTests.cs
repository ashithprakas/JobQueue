using JobQueue.Application.Services;
using JobQueue.Core.Enums;
using JobQueue.Core.Exceptions;
using JobQueue.Core.Interfaces;
using JobQueue.Core.Models;
using Moq;

namespace JobQueue.Application.Tests.Services;

public class JobServiceTests
{
    private readonly Mock<IJobStreamService> _jobStreamService = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly JobService _sut;

    public JobServiceTests()
    {
        _sut = new JobService(_jobRepository.Object, _eventPublisher.Object, _jobStreamService.Object);
    }

    [Fact]
    public async Task ProcessJob_HappyPath_MarksCompletedAndPublishesOnce()
    {
        //Arrange
        var testJobId = Guid.NewGuid();
        var testJob = new Job(){Id = testJobId,Payload = "test payload",Status = JobStatus.Pending,Attempts = 0,UpdatedAt = DateTime.UtcNow};
        var updatedStatuses = new List<JobStatus>();
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        _jobRepository.Setup(r => r.UpdateAsync(It.IsAny<Job>()))
            .Callback<Job>(j => updatedStatuses.Add(j.Status))
            .Returns(Task.CompletedTask);
        
        //Act
        await _sut.ProcessJob(testJobId);
        
        
        //Assert
        _jobRepository.Verify(service=>service.GetJobByIdAsync(testJobId), Times.Once);
        Assert.Equal(JobStatus.Completed, testJob.Status);
        Assert.Equal(1, testJob.Attempts);
        _jobRepository.Verify(r => r.UpdateAsync(It.IsAny<Job>()), Times.Exactly(2));
        Assert.Equal([JobStatus.Processing, JobStatus.Completed], updatedStatuses);
        _eventPublisher.Verify(service=>service.PublishJobStatusUpdate(testJobId, JobStatus.Processing), Times.Once);
        _eventPublisher.Verify(service=>service.PublishJobStatusUpdate(testJobId, JobStatus.Completed), Times.Once);
        
        
    }

    [Fact]
    public async Task ProcessJob_PersistFailureAlsoThrows_SwallowsAndLeavesJobUnpersisted()
    {
        //Arrange
        var testJobId = Guid.NewGuid();
        var testJob = new Job() { Id = testJobId, Payload = "test payload", Status = JobStatus.Pending, Attempts = 0, UpdatedAt = DateTime.UtcNow };
        _jobRepository.Setup(r => r.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        _jobRepository.SetupSequence(r => r.UpdateAsync(It.IsAny<Job>()))
            .ThrowsAsync(new Exception("Simulated DB write failure"))
            .ThrowsAsync(new Exception("SQL Server unreachable"));

        //Act
        await _sut.ProcessJob(testJobId);

        //Assert
        Assert.Equal(JobStatus.Pending, testJob.Status);
        Assert.Equal("Simulated DB write failure", testJob.ErrorMessage);
        _jobRepository.Verify(r => r.UpdateAsync(It.IsAny<Job>()), Times.Exactly(2));
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.Pending), Times.Once);
    }

    [Fact]
    public async Task ProcessJob_RetryableFailure_MarksPendingWithRetryAt()
    {
        //Arrange
        var testJobId = Guid.NewGuid();
        var testJob = new Job(){Id = testJobId,Payload = "test payload",Status = JobStatus.Pending,Attempts = 1,UpdatedAt = DateTime.UtcNow};
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        _jobRepository.SetupSequence(r => r.UpdateAsync(It.IsAny<Job>()))
            .ThrowsAsync(new Exception("Simulated DB write failure"))
            .Returns(Task.CompletedTask);

        //Act
        await _sut.ProcessJob(testJobId);

        //Assert
        Assert.Equal(JobStatus.Pending, testJob.Status);
        Assert.NotNull(testJob.RetryAt);
        Assert.Equal("Simulated DB write failure", testJob.ErrorMessage);
        Assert.Equal(2, testJob.Attempts);
        _jobRepository.Verify(r => r.UpdateAsync(It.IsAny<Job>()), Times.Exactly(2));
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.Pending), Times.Once);
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.Processing), Times.Never);
    }
    
    [Fact]
    public async Task ProcessJob_MaxAttemptsExceeded_MarksDeadLetter()
    {
        //Arrange
        var testJobId = Guid.NewGuid();
        var testJob = new Job(){Id = testJobId,Payload = "test payload",Status = JobStatus.Pending,Attempts = 3,UpdatedAt = DateTime.UtcNow};
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        _jobRepository.SetupSequence(r => r.UpdateAsync(It.IsAny<Job>()))
            .ThrowsAsync(new Exception("Simulated DB write failure"))
            .Returns(Task.CompletedTask);

        //Act
        await _sut.ProcessJob(testJobId);

        //Assert
        Assert.Equal(JobStatus.DeadLetter, testJob.Status);
        Assert.NotNull(testJob.RetryAt);
        Assert.Equal("Simulated DB write failure", testJob.ErrorMessage);
        Assert.Equal(4, testJob.Attempts);
        _jobRepository.Verify(r => r.UpdateAsync(It.IsAny<Job>()), Times.Exactly(2));
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.DeadLetter), Times.Once);
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.Processing), Times.Never);
    }
    [Fact]
    public async Task ProcessJob_TrailingPublishThrows_JobStillCompletesWithoutPropagating()
    {
        //Arrange
        var testJobId = Guid.NewGuid();
        var testJob = new Job(){Id = testJobId,Payload = "test payload",Status = JobStatus.Pending,Attempts = 0,UpdatedAt = DateTime.UtcNow};
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        _eventPublisher.SetupSequence(e => e.PublishJobStatusUpdate(testJobId, It.IsAny<JobStatus>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new Exception("Redis unreachable"));

        //Act
        await _sut.ProcessJob(testJobId);

        //Assert
        Assert.Equal(JobStatus.Completed, testJob.Status);
        _jobRepository.Verify(r => r.UpdateAsync(It.IsAny<Job>()), Times.Exactly(2));
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.Processing), Times.Once);
        _eventPublisher.Verify(e => e.PublishJobStatusUpdate(testJobId, JobStatus.Completed), Times.Once);
    }
    
    [Fact]
    public async Task CreateJob_Success()
    {
        //Arrange
        var testJobId = Guid.NewGuid();
        var result = new Job();
        
        //Act
        result = await _sut.CreateJob(testJobId, "test payload");
        
        //Assert
        Assert.Equal(JobStatus.Pending, result.Status);
        Assert.Equal(0, result.Attempts);
        Assert.Equal("test payload", result.Payload);
        Assert.Equal(testJobId, result.Id);
        
        _jobRepository.Verify(r => r.AddAsync(It.Is<Job>(j =>
            j.Id == testJobId && j.Payload == "test payload" && j.Status == JobStatus.Pending && j.Attempts == 0)), Times.Once);
        _jobStreamService.Verify(r=>r.AddJobToQueueAsync(It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public async Task GetJobStatus_Success()
    {
        var testJobId = Guid.NewGuid();
        var testJob = new Job(){Id = testJobId,Payload = "test payload",Status = JobStatus.Processing,Attempts = 1,UpdatedAt = DateTime.UtcNow,RetryAt = DateTime.UtcNow};
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        
        //Act
        var status = await _sut.GetJobStatus(testJobId);
        
        //Assert
        Assert.Equal(JobStatus.Processing,status);
    }
    
    [Fact]
    public async Task GetJobStatus_NullJob_ReturnsNotFound()
    {
        var testJobId = Guid.NewGuid();
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(null as Job);
        
        //Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetJobStatus(testJobId));
        
        //Assert
        Assert.Equal($"Job with id {testJobId} not found",exception.Message);
    }
    
    [Fact]
    public async Task GetJobById_Success()
    {
        var testJobId = Guid.NewGuid();
        var testJob = new Job(){Id = testJobId,Payload = "test payload",Status = JobStatus.Processing,Attempts = 1,UpdatedAt = DateTime.UtcNow,RetryAt = DateTime.UtcNow};
        _jobRepository.Setup(service => service.GetJobByIdAsync(testJobId)).ReturnsAsync(testJob);
        
        //Act
        var job = await _sut.GetJobById(testJobId);
        
        //Assert
        Assert.Equal(testJobId, job.Id);
    }
}