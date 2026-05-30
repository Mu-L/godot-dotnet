using Godot;

namespace NS;

[GodotClass]
public partial class MyNodeWithImplicitParameterlessConstructor : Node { }

[GodotClass]
public partial class {|GODOT0103:MyNodeWithoutParameterlessConstructor|} : Node
{
    public MyNodeWithoutParameterlessConstructor(int a, float b) { }
}

[GodotClass]
public partial class MyNodeWithParameterlessConstructor : Node
{
    private MyNodeWithParameterlessConstructor() { }

    public MyNodeWithParameterlessConstructor(int a, float b) { }
}
