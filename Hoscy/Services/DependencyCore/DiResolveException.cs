using System;

namespace Hoscy.Services.DependencyCore;

public class DiResolveException(string message) : Exception(message)
{
}