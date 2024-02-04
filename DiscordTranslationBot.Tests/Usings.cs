global using FluentAssertions;
global using Microsoft.Extensions.Logging;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using NSubstitute;
global using NSubstitute.ClearExtensions;
global using NSubstitute.ExceptionExtensions;
global using NSubstitute.Extensions;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
