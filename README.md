<p align="center">
<img width="300" src="logo.svg" alt="feval logo">
</p>
<h1 align="center">A Light C# Expression Evaluator</h1>
<p align="center"><a href="https://www.nuget.org/packages/Feval.Core"><img alt="Static Badge" src="https://img.shields.io/badge/nuget-v1.0.6-blue">
</a></p>

## About

Feval is a lightweight C# expression evaluator written by pure C# based on reflection. It is designed to be embedded in
existing project for simple expression evaluation or debugging(inspect or modify values).

## Installation

```shell
PM> NuGet\Install-Package Feval.Core -Version 1.0.6.34490
```

## Use at a Glance

### Sample Code

```c#
public class A 
{
    public int instanceValue = 2;
    
    public string Func(string value)
    {
        return $"Hello {value}"
    }
}
```

### Setup

Create an evaluation context and reference all assemblies in current domain.

```c#
var ctx = Context.Create();
ctx.WithReferences(AppDomain.CurrentDomain.GetAssemblies());
```

### Property/Field Access

Assuming the evaluation context has been created.

```c#
> ctx.Evaluate("a = new A()")
> ctx.Evalute("a.instanceValue")
1
> ctx.Evalute("a.instanceValue = 2")
> ctx.Evalute("a.instanceValue")
2
```

### Method Call

```c#
> ctx.Evaluate("a.Func(\"World\"))"
> Hello World
```
