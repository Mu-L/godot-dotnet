namespace Godot.SourceGeneration.Tests;

public class IdentifierUtilsTests
{
    // NOTE: These test cases are mirrored on the C++ side in
    // `modules/dotnet/tests/test_identifier_utils.h`.
    // Any change here must be reflected there, and vice versa.

    [Theory]
    [InlineData("MyProject", "MyProject")]
    [InlineData("My.Test.Project", "My.Test.Project")]
    [InlineData("My...Project", "My.Project")]
    [InlineData("My Test Project", "My_Test_Project")]
    [InlineData("my-test-project", "my_test_project")]
    [InlineData("3D_Game", "_3D_Game")]
    [InlineData("My.Test.3D_Game", "My.Test._3D_Game")]
    [InlineData("私のテストプロジェクト", "私のテストプロジェクト")]
    [InlineData("１私のテストプロジェクト", "_１私のテストプロジェクト")]
    [InlineData("My👍Project", "My_Project")]
    [InlineData("C++Project", "C_Project")]
    [InlineData("C++_Project", "C_Project")]
    [InlineData("C#Project", "C_Project")]
    [InlineData(".NETProject", "NETProject")]
    [InlineData(".3DGame", "_3DGame")]
    [InlineData("---", "UnnamedProject")]
    [InlineData("", "UnnamedProject")]
    [InlineData("namespace", "namespace")]
    public void SanitizeName(string value, string expected)
    {
        string actual = IdentifierUtils.SanitizeName(value);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("MyProject", "@MyProject")]
    [InlineData("My.Test.Project", "@My.@Test.@Project")]
    [InlineData("My...Project", "@My.@Project")]
    [InlineData("My Test Project", "@My_Test_Project")]
    [InlineData("my-test-project", "@my_test_project")]
    [InlineData("3D_Game", "@_3D_Game")]
    [InlineData("My.Test.3D_Game", "@My.@Test.@_3D_Game")]
    [InlineData("私のテストプロジェクト", "@私のテストプロジェクト")]
    [InlineData("１私のテストプロジェクト", "@_１私のテストプロジェクト")]
    [InlineData("My👍Project", "@My_Project")]
    [InlineData("C++Project", "@C_Project")]
    [InlineData("C++_Project", "@C_Project")]
    [InlineData("C#Project", "@C_Project")]
    [InlineData(".NETProject", "@NETProject")]
    [InlineData(".3DGame", "@_3DGame")]
    [InlineData("---", "@UnnamedProject")]
    [InlineData("", "@UnnamedProject")]
    [InlineData("namespace", "@namespace")]
    public void SanitizeNameEscapingSegments(string value, string expected)
    {
        string actual = IdentifierUtils.SanitizeName(value, escapeSegments: true);
        Assert.Equal(expected, actual);
    }
}
