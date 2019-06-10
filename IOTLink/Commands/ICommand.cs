namespace IOTLink
{
    public interface ICommand
    {
        string GetCommandLine();
        int ExecuteCommand(string[] args);
    }
}
