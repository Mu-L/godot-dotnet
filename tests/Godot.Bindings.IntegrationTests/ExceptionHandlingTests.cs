using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Godot.Bindings.IntegrationTests;

public sealed class ExceptionHandlingTests : IntegrationTestBase
{
    public ExceptionHandlingTests(GodotBinaryFixture godot, ITestOutputHelper output) : base(godot, output) { }

    [Fact]
    public async Task ExceptionPolicyHandle()
    {
        await Verify("res://TestExceptionHandlingPolicy.tscn", "handle");
    }

    [Fact]
    public async Task ExceptionPolicyThrow()
    {
        // The test is expected to throw an exception that crashes the engine.
        // This will prevent the test from exiting cleanly, which will report a failure in the test runner.
        // To workaround this, we catch the exception and verify that it is the expected exception.
        try
        {
            await Verify("res://TestExceptionHandlingPolicy.tscn", "throw");
        }
        catch (InvalidOperationException exception) when (IsExpectedException(exception))
        {
            // This is the expected exception message when the Godot process crashes.
            // We catch it here to prevent it from causing a test failure,
            // since the crash is the expected behavior in this case.
        }

        static bool IsExpectedException(Exception exception)
        {
            // The exact exit code does not matter, since different platforms may have
            // different exit codes for a process that crashes due to an unhandled exception.
            return exception.Message.Contains("Godot process exited with code", StringComparison.Ordinal);
        }
    }
}
