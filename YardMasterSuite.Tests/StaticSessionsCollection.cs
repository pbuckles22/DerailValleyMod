using Xunit;

namespace YardMasterSuite.Tests;

/// <summary>Static session helpers must not run in parallel.</summary>
[CollectionDefinition("StaticSessions", DisableParallelization = true)]
public class StaticSessionsCollection
{
}
