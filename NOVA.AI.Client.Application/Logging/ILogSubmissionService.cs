namespace NOVA.AI.Client.Application.Logging;

public interface ILogSubmissionService
{
    void Submit(LogMessageCommand command);
}
