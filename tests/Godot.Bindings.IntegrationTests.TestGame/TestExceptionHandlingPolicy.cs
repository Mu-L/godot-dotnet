using System;
using Godot.Collections;
using Godot.NativeInterop;

namespace Godot.Bindings.IntegrationTests.TestGame;

[GodotClass]
public partial class TestExceptionHandlingPolicy : TestBase
{
    private bool _executionContinuedAfterThrow;

    [BindMethod(Name = "throwing_method")]
    public void ThrowingMethod()
    {
        throw new InvalidOperationException("EXCEPTION");
    }

    protected override void _Ready()
    {
        PackedStringArray cmdlineArgs = OS.Singleton.GetCmdlineUserArgs();

        AssertEqual(1, cmdlineArgs.Count);
        if (cmdlineArgs.Count != 1)
        {
            ExitWithStatus();
            return;
        }

        ExceptionHandling.UnhandledExceptionPolicy? expectedPolicy = cmdlineArgs[0] switch
        {
            "handle" => ExceptionHandling.UnhandledExceptionPolicy.Handle,
            "throw" => ExceptionHandling.UnhandledExceptionPolicy.Throw,
            _ => null,
        };

        AssertNotEqual(expectedPolicy, null);
        if (expectedPolicy is null)
        {
            ExitWithStatus();
            return;
        }

        ProjectSettings.Singleton.SetSetting("dotnet/runtime/unhandled_exception_policy", (int)expectedPolicy.Value);

        if (expectedPolicy == ExceptionHandling.UnhandledExceptionPolicy.Handle)
        {
            AssertTrue(ExceptionHandling.IsHandled(new InvalidOperationException("Integration test exception.")));

            _executionContinuedAfterThrow = false;
            Call(MethodName.ThrowingMethod);
            _executionContinuedAfterThrow = true;

            AssertTrue(_executionContinuedAfterThrow);
        }
        else
        {
            AssertFalse(ExceptionHandling.IsHandled(new InvalidOperationException("Integration test exception.")));

            _executionContinuedAfterThrow = false;
            Call(MethodName.ThrowingMethod);
            _executionContinuedAfterThrow = true;

            AssertFalse(_executionContinuedAfterThrow);
        }

        ExitWithStatus();
    }
}
