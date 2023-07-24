# SharpES
SharpES(Sharp Embedded Script) is designed to be a scripting language that lives inside a host application.

It was inspired by the book "Crafting Interpreters", so it can also be seen as a derivative of the lox scripting language implemented on c#.

## Getting Started

### Creating a VM
You need to create a `ScriptConfiguration` object and set some callback functions
``` C#
ScriptConfiguration configuration = new ScriptConfiguration()
{
    PrintErrorFn = PrintError,
    WriteFunction = Console.Write,
    LoadModuleFunction = LoadModule
};
```
First we need a function that will do something with the output that SharpES sends us from Print function in script. 
The internal delegate is defined as follows, so you need a method that is compatible with  `Action<string>`.
``` C#
public delegate void WriteFn(string message);
```
Then you need a method to print the error, if not there will be no error message output.

You can set the format and decide where to print the error, like this.
``` C#
private static void PrintError(ErrorType errorType, string moduleName, int line, string message)
{
    switch (errorType)
    {
        case ErrorType.CompileError:
            Console.WriteLine($"Compile error : [{moduleName} line {line}] {message}");
            break;
        case ErrorType.RuntimeError:
            Console.WriteLine($"Runtime error : [{moduleName} line {line}] {message}");
            break;
    }
}
```