## Introduction

This is a simple test cli framework to simplify building cli apps. It's aot compatible.

By default, apps that use this framework has these commands:

Show Commands:
```bash
.\AttributesTestApp.exe
OR
.\AttributesTestApp.exe -h
OR
.\AttributesTestApp.exe --help
```
Output:
```
\AttributesTestApp.exe --help
Available commands:
  datetime - Get the current date and time
    -u|--utc

  greet - Greets a user by name
    -n|--name   [REQUIRED]
    -c|--count
    -u|--uppercase

  long
```

To run a test command:
```bash
.\AttributesTestApp.exe greet -n John -c 3 -u
```
Output:
```
HELLO, JOHN!
HELLO, JOHN!
HELLO, JOHN!
```
By pressing Ctrl+C we can cancel the command
```bash
.\AttributesTestApp.exe long
```
Output:
```
The operation was cancelled
```

We may have reqired options:
```
.\AttributesTestApp.exe greet
```
Output:
```
Error: Required option missing: name
```

## Start using

You just define commands:

```cs
[Command(Name = "datetime", Description = "Get the current date and time")]
public class GetDateTimeCommand(IDateTimeProvider dateTimeProvider): ICommand
{
   [Option(ShortName = "u", LongName = "utc", Description = "Get the current date and time in the utc format")]
    public bool IsUtc { get; set; }

    public async ValueTask Execute(CancellationToken cancellationToken)
    {
        var dateTime = await dateTimeProvider.DateTimeUtc;

        var formattedDateTimeString = IsUtc ? dateTime.ToString("U") : dateTime.ToLocalTime().ToString("G");

        Console.WriteLine($"Current date and time is: {formattedDateTimeString}");
    }
}

```

Commands may have options. Use them when you run this command by its name:

```bash
.\AttributesTestApp.exe datetime -u
```
Output:
```
Current date and time is: Friday, 18 September 2026 21:06:31
```