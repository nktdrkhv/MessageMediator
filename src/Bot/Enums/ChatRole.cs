namespace MessageMediator.Bot.Enums;

[Flags]
public enum ChatRole
{
    Newby = 0,
    Source = 2,
    Worker = 4,
    Supervisor = 8,
    Admin = 16
}