﻿namespace IOTLinkService.Service.Commands
{
    public interface ICommand
    {
        string GetCommandLine();
        int ExecuteCommand(string[] args);
    }
}
