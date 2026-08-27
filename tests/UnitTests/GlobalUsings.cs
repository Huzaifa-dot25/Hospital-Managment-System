// ─────────────────────────────────────────────────────────────────────────────
// GLOBAL USINGS — applied to every file in this project automatically.
//
// WHY THIS APPROACH?
//   In .NET 6+, "global using" declarations apply to all files in the assembly
//   without needing to repeat the using in every file.
//
//   This is especially useful for test projects because xUnit types ([Fact],
//   [Theory], Assert) and FluentAssertions (.Should()) are used in EVERY test file.
//   Without global usings, every test class needs 5+ identical using lines at the top.
//
// WHAT EACH NAMESPACE PROVIDES:
//   Xunit          → [Fact], [Theory], [InlineData], Assert
//   FluentAssertions → .Should().Be(), .Should().Throw(), .Should().HaveCount()
//   Moq            → Mock<T>, It.IsAny<T>(), .Setup(), .Returns(), .Verify()
//   System.Threading → CancellationToken (used in mock setups for SaveChangesAsync)
// ─────────────────────────────────────────────────────────────────────────────
global using Xunit;
global using FluentAssertions;
global using Moq;
global using System.Threading;
global using Microsoft.Extensions.Logging.Abstractions;
