# ThrowsAnalyzer

A Roslyn-based C# analyzer that detects exception handling patterns in your code. ThrowsAnalyzer helps identify throw statements, unhandled exceptions, and try-catch blocks across all executable member types.

## Features

- **THROWS001**: Detects methods and members containing throw statements
- **THROWS002**: Identifies unhandled throw statements (throws outside try-catch blocks)
- **THROWS003**: Flags methods and members containing try-catch blocks

## Supported Member Types

ThrowsAnalyzer analyzes exception handling patterns in:

- Methods
- Constructors and Destructors
- Properties (including expression-bodied properties)
- Property Accessors (get, set, init, add, remove)
- Operators (binary, unary, conversion)
- Local Functions
- Lambda Expressions (simple and parenthesized)
- Anonymous Methods

## Installation

Add the analyzer to your project via NuGet:

```bash
dotnet add package ThrowsAnalyzer
```

## Usage

Once installed, the analyzer runs automatically during compilation. Diagnostics will appear in your IDE and build output.

## Building from Source

```bash
# Build the analyzer
dotnet build src/ThrowsAnalyzer/ThrowsAnalyzer.csproj

# Run tests
dotnet test tests/ThrowsAnalyzer.Tests/ThrowsAnalyzer.Tests.csproj
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
