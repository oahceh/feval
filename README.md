<p align="center">
<img width="240" src="logo.svg" alt="feval logo">
</p>
<h1 align="center">A Light C# Expression Evaluator</h1>
<p align="center"><a href="https://www.nuget.org/packages/Feval.Core"><img alt="NuGet" src="https://img.shields.io/badge/nuget-v1.3.4-blue">
</a></p>

## About

Feval is a lightweight C# expression evaluator written in pure C# based on reflection. It is designed to be embedded in existing projects for simple expression evaluation or runtime debugging (inspect or modify values).

## Installation

```shell
PM> NuGet\Install-Package Feval.Core
```

## Features

- **Literals**: `int`, `long`, `float`, `string`, `bool`, `null`
- **Binary operators**: `+`, `-`, `*`, `/`, `|`
- **Unary operators**: `-` (negate), `` ` `` (dump)
- **Member access**: properties, fields (instance & static, including base class)
- **Method call**: instance / static methods, extension methods, generic methods
- **Constructor**: `new Type(args)`
- **Index access**: `obj[key]`
- **Assignment**: variable / member assignment
- **Variable declaration**: `var x = value`
- **`out` parameter**: `out varName`
- **`typeof`**: `typeof(Type)`
- **`using`**: `using Namespace`
- **String interpolation**: `$"text {expr}"`
- **Type keywords**: `int`, `float`, `long`, `double`, `string`
- **Delegate invocation**: invoke delegates stored in variables or fields
- **Built-in functions**: `help()`, `dump(obj)`, `usings()`, `vars()`, `assemblies()`, `version()`, `copyright()`
- **Extensibility**: custom dumpers, reflectors, and built-in functions

## Use at a Glance

### Sample Code

```c#
public class Player
{
    public int hp = 100;
    public string name;

    public Player(string name)
    {
        this.name = name;
    }

    public string Greet(string other)
    {
        return $"Hello {other}, I'm {name}";
    }
}
```

### Setup

Create an evaluation context and reference all assemblies in the current domain.

```c#
var ctx = Context.Create();
ctx.WithReferences(AppDomain.CurrentDomain.GetAssemblies());
```

### Property / Field Access

```c#
> ctx.Evaluate("using MyGame")
> ctx.Evaluate("var p = new Player(\"Alice\")")
> ctx.Evaluate("p.hp")
100
> ctx.Evaluate("p.hp = 200")
> ctx.Evaluate("p.hp")
200
```

### Method Call

```c#
> ctx.Evaluate("p.Greet(\"Bob\")")
Hello Bob, I'm Alice
```

### Variable Declaration & Arithmetic

```c#
> ctx.Evaluate("var x = 3 + 4 * 2")
> ctx.Evaluate("x")
11
```

### String Interpolation

```c#
> ctx.Evaluate("$\"Player {p.name} has {p.hp} HP\"")
Player Alice has 200 HP
```

### Delegate Invocation

```c#
// Invoke a delegate stored in a variable
> ctx.Evaluate("var func = holder.AddFunc")
> ctx.Evaluate("func(3, 4)")
7

// Invoke a delegate stored in a field
> ctx.Evaluate("holder.AddFunc(10, 20)")
30
```

### Using & typeof

```c#
> ctx.Evaluate("using System.Collections.Generic")
> ctx.Evaluate("var list = new List<int>()")
> ctx.Evaluate("typeof(List<int>)")
System.Collections.Generic.List`1[System.Int32]
```

### Built-in Functions

```c#
> ctx.Evaluate("help()")
> ctx.Evaluate("vars()")
> ctx.Evaluate("dump(someObject)")
```

## Extensibility

### Custom Dumper

```c#
ctx.RegisterDumper(obj => JsonConvert.SerializeObject(obj, Formatting.Indented));
```

### Custom Reflector

```c#
ctx.RegisterReflector(myReflector);
```

### Custom Built-in Function

```c#
ctx.RegisterBuiltInFunction("myFunc", typeof(MyClass).GetMethod("MyStaticMethod"));
```

## Project Structure

| Project | Description |
|---|---|
| **Feval.Core** | Core evaluator library (.NET Framework 4.7.1) |
| **Feval.Cli** | Command-line REPL tool (.NET 6/7/8/9) supporting standalone and remote modes |
| **Feval.UnitTests** | xUnit-based unit tests |

## License

Copyright (C) 2021 Feval.Core
