namespace Godot.Bindings.IntegrationTests.TestGame;

[GodotClass]
public partial class TestSignals : Control
{
    [Signal(Name = "custom_signal")]
    public delegate void CustomSignalEventHandler(string name, int value);

    [BindMethod(Name = "emit_custom_signal")]
    public void EmitCustomSignal(string name, int value)
    {
        EmitSignal(SignalName.CustomSignal, name, value);
    }

    [BindMethod(Name = "simple_func")]
    public void SimpleFunc()
    {
        EmitCustomSignal("simple_func", 3);
    }

    [BindMethod(Name = "callable_bind")]
    public void CallableBind()
    {
        var callable = new Callable(this, MethodName.EmitCustomSignal).Bind(["bound", 11]);
        callable.Call();
    }
}
