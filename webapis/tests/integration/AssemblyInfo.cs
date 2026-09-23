using Xunit.Sdk;
using Xunit.v3;
using yggdrasil.Integration.Tests.Infrastructure;

[assembly: AssemblyFixture(typeof(PostgresFixture))]
[assembly: Parallelization(Mode = ParallelMode.None)]
