using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer.Core.Configuration.Options;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Configuration.Options
{
    public class AnalyzerOptionsReaderTests
    {
        private static SyntaxTree CreateSyntaxTree()
        {
            return CSharpSyntaxTree.ParseText("class C { }");
        }

        private static AnalyzerOptions CreateAnalyzerOptions(Dictionary<string, string> config)
        {
            var provider = new TestAnalyzerConfigOptionsProvider(config);
            return new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, provider);
        }

        [Fact]
        public void GetBoolOption_WithTrueValue_ReturnsTrue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "true"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetBoolOption(options, tree, "test_option", defaultValue: false);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetBoolOption_WithFalseValue_ReturnsFalse()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "false"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetBoolOption(options, tree, "test_option", defaultValue: true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetBoolOption_WithMissingKey_ReturnsDefault()
        {
            // Arrange
            var config = new Dictionary<string, string>();
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetBoolOption(options, tree, "missing_option", defaultValue: true);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetBoolOption_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "TRUE"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetBoolOption(options, tree, "test_option", defaultValue: false);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetBoolOption_WithWhitespace_ReturnsTrue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "  true  "
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetBoolOption(options, tree, "test_option", defaultValue: false);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetStringOption_WithValue_ReturnsValue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "test_value"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetStringOption(options, tree, "test_option", defaultValue: "default");

            // Assert
            result.Should().Be("test_value");
        }

        [Fact]
        public void GetStringOption_WithMissingKey_ReturnsDefault()
        {
            // Arrange
            var config = new Dictionary<string, string>();
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetStringOption(options, tree, "missing_option", defaultValue: "default");

            // Assert
            result.Should().Be("default");
        }

        [Fact]
        public void GetStringOption_WithWhitespace_ReturnsTrimmed()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "  test_value  "
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetStringOption(options, tree, "test_option", defaultValue: "default");

            // Assert
            result.Should().Be("test_value");
        }

        [Fact]
        public void GetIntOption_WithValidInt_ReturnsValue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "42"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetIntOption(options, tree, "test_option", defaultValue: 0);

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public void GetIntOption_WithMissingKey_ReturnsDefault()
        {
            // Arrange
            var config = new Dictionary<string, string>();
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetIntOption(options, tree, "missing_option", defaultValue: 100);

            // Assert
            result.Should().Be(100);
        }

        [Fact]
        public void GetIntOption_WithInvalidInt_ReturnsDefault()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["test_option"] = "not_a_number"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetIntOption(options, tree, "test_option", defaultValue: 100);

            // Assert
            result.Should().Be(100);
        }

        [Fact]
        public void IsAnalyzerEnabled_WithEnabledAnalyzer_ReturnsTrue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["my_analyzer_enable_rule1"] = "true"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.IsAnalyzerEnabled(options, tree, "my_analyzer", "rule1");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnalyzerEnabled_WithDisabledAnalyzer_ReturnsFalse()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["my_analyzer_enable_rule1"] = "false"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.IsAnalyzerEnabled(options, tree, "my_analyzer", "rule1");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAnalyzerEnabled_WithMissingConfig_ReturnsTrueByDefault()
        {
            // Arrange
            var config = new Dictionary<string, string>();
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.IsAnalyzerEnabled(options, tree, "my_analyzer", "rule1");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsFeatureEnabled_WithEnabledFeature_ReturnsTrue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["my_analyzer_feature1"] = "true"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.IsFeatureEnabled(options, tree, "my_analyzer", "feature1", defaultValue: false);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsFeatureEnabled_WithDisabledFeature_ReturnsFalse()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["my_analyzer_feature1"] = "false"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.IsFeatureEnabled(options, tree, "my_analyzer", "feature1", defaultValue: true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsFeatureEnabled_WithMissingConfig_ReturnsDefault()
        {
            // Arrange
            var config = new Dictionary<string, string>();
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.IsFeatureEnabled(options, tree, "my_analyzer", "feature1", defaultValue: false);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetOptionsWithPrefix_WithMatchingKeys_ReturnsAll()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["my_analyzer_option1"] = "value1",
                ["my_analyzer_option2"] = "value2",
                ["other_option"] = "value3"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetOptionsWithPrefix(options, tree, "my_analyzer");

            // Assert
            result.Should().HaveCount(2);
            result["my_analyzer_option1"].Should().Be("value1");
            result["my_analyzer_option2"].Should().Be("value2");
            result.Should().NotContainKey("other_option");
        }

        [Fact]
        public void GetOptionsWithPrefix_WithNoMatchingKeys_ReturnsEmpty()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["other_option"] = "value"
            };
            var options = CreateAnalyzerOptions(config);
            var tree = CreateSyntaxTree();

            // Act
            var result = AnalyzerOptionsReader.GetOptionsWithPrefix(options, tree, "my_analyzer");

            // Assert
            result.Should().BeEmpty();
        }

        // Test helper classes
        private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly Dictionary<string, string> _options;

            public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options)
            {
                _options = options;
            }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            {
                return new TestAnalyzerConfigOptions(this);
            }

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            {
                return new TestAnalyzerConfigOptions(this);
            }

            public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(this);

            public Dictionary<string, string> Options => _options;
        }

        private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly TestAnalyzerConfigOptionsProvider _provider;

            public TestAnalyzerConfigOptions(TestAnalyzerConfigOptionsProvider provider)
            {
                _provider = provider;
            }

            public override bool TryGetValue(string key, out string value)
            {
                return _provider.Options.TryGetValue(key, out value);
            }

            public override IEnumerable<string> Keys => _provider.Options.Keys;
        }
    }
}
