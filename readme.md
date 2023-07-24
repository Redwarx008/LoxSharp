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

With this ready, you can create the VM:

``` C#
VM vM = new VM(configuration);
```

The VM implementation has no static variables, so you can have multiple VMs running independently of each other.

### Executing code

You execute source code like so:

``` C#
InterpretResult res = ScriptEngine.Run(vM, source);
```

Just call this static method and pass in a source code string.

### Import Module

SharpES has a simple module system. A file containing code defines a module.
A module can use the code defined in another module by importing it.

SharpES does not have a single global scope. Instead, each module has its own top-level scope independent of all other modules.

You need to provide a module loading method in `ScriptConfiguration` to return a string containing the source code, the script will call this method at runtime and then compile it.

``` C#
private static string LoadModule(string moduleName)
{
    return File.ReadAllText(moduleName + ".lox");
}
ScriptConfiguration configuration = new ScriptConfiguration()
{
    LoadModuleFunction = LoadModule
};
```

To load and execute other modules, you use an import statement:

``` javascript
import variableA, variableB from "ModuleA";
```

This imports the two top-level variables in module A into the current context.

This finds a module named “ModuleA” and executes its source code. Then, it looks up two top-level variables, variableA and variableB in that module and creates new variables in this context.

The copy behavior occurs if the variables are value types (like booleans and numbers), otherwise any modifications made in the current context will be reflected in their original module.

You can also give variables an alias when importing them.

``` javascript
import variableA as A, variableB as B from "ModuleA";
```

You can even import all top-level variables of the module at once.

``` javascript
import * as ModuleA from "ModuleA";
```

You can treat ModuleA like a normal variable.

``` C#
Print(MoudleA.variableA);
```

### Syntax

#### Comments

Line comments start with // and end at the end of the line:

``` C#
// This is a comment.
```

#### Reserved words

``` javascript
as  class break continue if else true false for while import null return static this var 
``` 

#### Blocks

SharpES uses curly braces to define blocks.
You can use a block anywhere a statement is allowed, like in control flow statements. Method and function bodies are also blocks.

The variables defined in the block are all local variables, which will be destroyed when they leave the block.

``` C#
{
    var a = 1;
}
Print(a); // error, a is not defined.
```

#### Values

Values are the built-in atomic object types that all other objects are composed of. They can be created through literals, expressions that evaluate to a value. All values are immutable.

##### Booleans

A boolean value represents truth or falsehood. `true` or `false`

##### Numbers

SharpES has a single numeric type: double-precision.

##### Strings

String literals are surrounded in double quotes: `"hello world"`.

##### NULL

Regardless of whether it is a module top-level variable or a local variable, if the initial value is not assigned at the time of declaration, the default value is `null`.

If the function does not return a value, it will also return a `null` by default.

### Variables

Define a variable just like in other languages:

``` C#
var a = 1 + 2;
var b = "hello world";
var c = true;
var d; // The default assignment is null.
```

Variables defined at the top level of a script are top-level and are visible to the module context. All other variables are local.

Local variables declared inside shadow outside variables:

``` C#
{
    var a = 1;
    {
        var a = 2;
        Print(a); // print 2.
    }
}
```

Declaring a variable with the same name in the same scope is an error:

``` C#
var a = 2;
var a = 3; // "a" is already declared.
```

### Classes

Classes define an objects behavior and state. Behavior is defined by methods which live in the class. Every object of the same class supports the same methods. State is defined in fields, whose values are stored in each instance.

#### Defining a class

``` C#
class A 
{
    Init(name)
    {
        this.name = name;
    }
}
```

#### Create a class instance

To create an instance of a class just call it like a function:

``` c#
var instance = A("Lilei");
```

The method named Init is the initializer of the class and can accept parameters.

#### This Keyword

You can use the this keyword in a class's non-static methods to access fields in the class, but you can only define new fields in initializers.

``` C#
class A 
{
    init()
    {
        this.name = "Lilei";
    }
    static static_method()
    {
        this.name = 123; // error, The this keyword cannot be used in static methods.
        return "hello world";
    }
}
```

#### Static method

Add the keyword static before the method of the class to define the static method, the static method cannot be called by the instance of the class, the caller must be the class itself.

``` C#
var instance = A();
instance.static_method(); // error, Undefined method static_method.
A.static_method(); // print 'hello world'.
```

Therefore, it is possible to have a normal method and a static method with the same name.

### Functions

Like many languages today, functions in SharpES are little bundles of code you can store in a variable, or pass as an argument to a function.

#### Define a function

``` javascript
fun Fib(n)
{
    if (n < 2)
    {
        return n;
    }
    return Fib(n - 1) + Fib(n -2);
}
```

The method of the class defined in the script is the same as the bottom-level implementation of the top-level function of the module, and they are both a code block.

But the functions and methods defined externally are different, we will talk about this later.

### Control Flow

Control flow is used to determine which chunks of code are executed and how many times. Branching statements and expressions decide whether or not to execute some code and looping ones execute something more than once.

#### loop

continue and break can be used in for and while loops to skip some code and end the loop early.

``` javascript
for(var i = 0; i < 3; i = i + 1)
{
    for(var j = 0; j <= 2; j = j + 1)
    {
        if(j == 0)
        continue;
    }
}

var i = 3;
while(i >= 0)
{
    var a = 0;
    while(a <= 2)
    {
        
        a = a + 1;
        print a - 1;
        if(a == 2) break;
        1;
    }
    i = i - 1;
}
```

#### Logical operators

* The boolean value false is false.
* The null value null is false.
* Everything else is true.

``` javascript
3 == 2; // false
3 != 2; // true
3 > 2;  // true
3 < 2;  // false
2 <= 2; // true
2 >= 2; // true
true and true; // true
true and false; // false
true or false; // true
```

### Built-in classes and functions

For convenience, XX has some built-in classes and functions, which are implicitly imported into the current context.

#### Array

A Array is a compound object that holds a collection of elements identified by integer index. You can create a Array by placing a sequence of comma-separated expressions inside square brackets:

``` javascript
var object;
var array = ["tree", 1, true, object];
Print(array[0]); //print 'tree'
Print(array[-1]); // error, index out of bounds.
Print(array[4]); // error, index out of bounds.
```

Of course you can define an array variable like a normal class:

``` javascript
var array = Array();
```

Arrays have methods:

``` javascript
var object;
var array = ["tree", 1, true, object];
Print(array.Count()); // print 4.

array.Add(5);
Print(array[4]); // print 5.
Print(array.Get(4)); // print 5.

array.RemoveAt(4)
array.Clear();
```

#### Map

A map is an associative collection. It holds a set of entries, each of which maps a key to a value. 

You can create a map by placing a series of comma-separated entries inside curly braces. Each entry is a key and a value separated by a colon:

``` javascript
var map = 
{   
    2:"dddd", 
    3:"xxxx"
};
map.Add(0, "jianglei");
map.Add(1, "laohu");
map[3] = "hello world"; // implicit add a pair.
map.ContainsKey(2); // return true.
map.Get(5); // return null.
map.Remove(2) // returns true if deleting a pair based on the specified key was successful.
map.Clear()
```

Similarly you can explicitly define a map:

``` javascript
var array = Map();
```

