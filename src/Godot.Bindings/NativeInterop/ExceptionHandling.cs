using System;
using System.Diagnostics;

namespace Godot.NativeInterop;

internal static class ExceptionHandling
{
    public enum UnhandledExceptionPolicy
    {
        /// <summary>
        /// Swallows all exceptions and prints them to the console. If a debugger is attached, it will break.
        /// </summary>
        Handle,

        /// <summary>
        /// Throw all uncaught exceptions. This is the default behavior for .NET applications,
        /// but it will crash the Godot engine.
        /// </summary>
        Throw,
    }

    private static UnhandledExceptionPolicy? _effectiveUnhandledExceptionPolicy;

    private static UnhandledExceptionPolicy EffectiveUnhandledExceptionPolicy =>
        _effectiveUnhandledExceptionPolicy ??= GetEffectiveUnhandledExceptionPolicy();

    private static UnhandledExceptionPolicy GetEffectiveUnhandledExceptionPolicy()
    {
        // When running in the editor, we always want to handle exceptions to prevent crashing the editor.
        if (Engine.Singleton.IsEditorHint())
        {
            return UnhandledExceptionPolicy.Handle;
        }

        return ProjectSettings.Singleton.GetSetting("dotnet/runtime/unhandled_exception_policy").As<UnhandledExceptionPolicy>();
    }

    internal static bool IsHandled(Exception exception)
    {
        if (EffectiveUnhandledExceptionPolicy == UnhandledExceptionPolicy.Handle)
        {
            // GD.Print may not be available when throwing an exception,
            // since that may be what causes the exception.
            Console.Error.WriteLine($"Unhandled exception. {exception}");

            // Allow the user to receive the exception in the UnhandledException event.
            System.Runtime.ExceptionServices.ExceptionHandling.RaiseAppDomainUnhandledExceptionEvent(exception);

            // Allow the user to break into the debugger if one is attached.
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            return true;
        }

        return false;
    }
}
