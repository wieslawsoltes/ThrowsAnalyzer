using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = @"
using System.IO;
class Test {
    void M() {
        using (var stream = new FileStream(""test.txt"", FileMode.Open)) {
        }
    }
}";

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

var declarator = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
Console.WriteLine($"Declarator: {declarator}");
Console.WriteLine($"Parent: {declarator.Parent} (Type: {declarator.Parent.GetType().Name})");
Console.WriteLine($"Parent.Parent: {declarator.Parent.Parent} (Type: {declarator.Parent.Parent.GetType().Name})");
Console.WriteLine($"Parent.Parent.Parent: {declarator.Parent.Parent.Parent} (Type: {declarator.Parent.Parent.Parent.GetType().Name})");
