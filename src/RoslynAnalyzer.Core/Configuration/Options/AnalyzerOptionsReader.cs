using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace RoslynAnalyzer.Core.Configuration.Options
{
    /// <summary>
    /// Reads analyzer configuration options from .editorconfig files.
    /// </summary>
    /// <remarks>
    /// This class provides utilities for reading analyzer configuration from .editorconfig:
    /// - Checking if specific analyzers are enabled/disabled
    /// - Reading custom analyzer options
    /// - Getting boolean, string, and integer configuration values
    ///
    /// Options are typically prefixed with an analyzer-specific identifier (e.g., "my_analyzer_enable_rule1").
    /// This class supports configurable prefixes to allow any analyzer to use it.
    ///
    /// Example .editorconfig:
    /// <code>
    /// [*.cs]
    /// my_analyzer_enable_rule1 = true
    /// my_analyzer_max_depth = 10
    /// my_analyzer_output_format = json
    /// </code>
    /// </remarks>
    public static class AnalyzerOptionsReader
    {
        /// <summary>
        /// Reads a boolean option from analyzer configuration.
        /// </summary>
        /// <param name="options">The analyzer options containing configuration.</param>
        /// <param name="tree">The syntax tree to get configuration for.</param>
        /// <param name="key">The configuration key to read.</param>
        /// <param name="defaultValue">The default value if the key is not found.</param>
        /// <returns>The boolean value from configuration, or the default value if not found.</returns>
        /// <remarks>
        /// Accepts "true", "false" (case-insensitive). Other values return the default.
        ///
        /// Example:
        /// <code>
        /// var enabled = AnalyzerOptionsReader.GetBoolOption(
        ///     options, tree, "my_analyzer_enable_feature", defaultValue: true);
        /// </code>
        /// </remarks>
        public static bool GetBoolOption(
            AnalyzerOptions options,
            SyntaxTree tree,
            string key,
            bool defaultValue = false)
        {
            var provider = options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            if (provider.TryGetValue(key, out var value))
            {
                var trimmed = value.Trim().ToLowerInvariant();
                if (trimmed == "true")
                {
                    return true;
                }
                else if (trimmed == "false")
                {
                    return false;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads a string option from analyzer configuration.
        /// </summary>
        /// <param name="options">The analyzer options containing configuration.</param>
        /// <param name="tree">The syntax tree to get configuration for.</param>
        /// <param name="key">The configuration key to read.</param>
        /// <param name="defaultValue">The default value if the key is not found.</param>
        /// <returns>The string value from configuration, or the default value if not found.</returns>
        /// <remarks>
        /// Returns the trimmed value from .editorconfig, or the default if the key doesn't exist.
        ///
        /// Example:
        /// <code>
        /// var format = AnalyzerOptionsReader.GetStringOption(
        ///     options, tree, "my_analyzer_output_format", defaultValue: "text");
        /// </code>
        /// </remarks>
        public static string GetStringOption(
            AnalyzerOptions options,
            SyntaxTree tree,
            string key,
            string defaultValue = "")
        {
            var provider = options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            if (provider.TryGetValue(key, out var value))
            {
                return value.Trim();
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads an integer option from analyzer configuration.
        /// </summary>
        /// <param name="options">The analyzer options containing configuration.</param>
        /// <param name="tree">The syntax tree to get configuration for.</param>
        /// <param name="key">The configuration key to read.</param>
        /// <param name="defaultValue">The default value if the key is not found or cannot be parsed.</param>
        /// <returns>The integer value from configuration, or the default value if not found or invalid.</returns>
        /// <remarks>
        /// Parses the value as an integer. If parsing fails, returns the default value.
        ///
        /// Example:
        /// <code>
        /// var maxDepth = AnalyzerOptionsReader.GetIntOption(
        ///     options, tree, "my_analyzer_max_depth", defaultValue: 5);
        /// </code>
        /// </remarks>
        public static int GetIntOption(
            AnalyzerOptions options,
            SyntaxTree tree,
            string key,
            int defaultValue = 0)
        {
            var provider = options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            if (provider.TryGetValue(key, out var value))
            {
                if (int.TryParse(value.Trim(), out var result))
                {
                    return result;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if a specific analyzer is enabled based on configuration.
        /// </summary>
        /// <param name="options">The analyzer options containing configuration.</param>
        /// <param name="tree">The syntax tree to get configuration for.</param>
        /// <param name="prefix">The analyzer-specific option prefix (e.g., "my_analyzer").</param>
        /// <param name="analyzerName">The name of the analyzer to check (e.g., "null_check").</param>
        /// <returns>True if the analyzer is enabled (or not configured); false if explicitly disabled.</returns>
        /// <remarks>
        /// Reads the configuration key "{prefix}_enable_{analyzerName}".
        /// By default, all analyzers are enabled unless explicitly disabled in .editorconfig.
        ///
        /// Example .editorconfig:
        /// <code>
        /// my_analyzer_enable_null_check = false
        /// </code>
        ///
        /// Example usage:
        /// <code>
        /// if (AnalyzerOptionsReader.IsAnalyzerEnabled(options, tree, "my_analyzer", "null_check"))
        /// {
        ///     // Run the null check analyzer
        /// }
        /// </code>
        /// </remarks>
        public static bool IsAnalyzerEnabled(
            AnalyzerOptions options,
            SyntaxTree tree,
            string prefix,
            string analyzerName)
        {
            var key = $"{prefix}_enable_{analyzerName}";
            return GetBoolOption(options, tree, key, defaultValue: true);
        }

        /// <summary>
        /// Checks if a specific feature is enabled based on configuration.
        /// </summary>
        /// <param name="options">The analyzer options containing configuration.</param>
        /// <param name="tree">The syntax tree to get configuration for.</param>
        /// <param name="prefix">The analyzer-specific option prefix.</param>
        /// <param name="featureName">The name of the feature to check.</param>
        /// <param name="defaultValue">The default value if not configured.</param>
        /// <returns>True if the feature is enabled; otherwise, false.</returns>
        /// <remarks>
        /// Similar to IsAnalyzerEnabled but allows specifying a custom default value.
        /// Reads the configuration key "{prefix}_{featureName}".
        ///
        /// Example:
        /// <code>
        /// if (AnalyzerOptionsReader.IsFeatureEnabled(options, tree, "my_analyzer", "analyze_lambdas", true))
        /// {
        ///     // Analyze lambda expressions
        /// }
        /// </code>
        /// </remarks>
        public static bool IsFeatureEnabled(
            AnalyzerOptions options,
            SyntaxTree tree,
            string prefix,
            string featureName,
            bool defaultValue = true)
        {
            var key = $"{prefix}_{featureName}";
            return GetBoolOption(options, tree, key, defaultValue);
        }

        /// <summary>
        /// Gets multiple configuration values that match a pattern.
        /// </summary>
        /// <param name="options">The analyzer options containing configuration.</param>
        /// <param name="tree">The syntax tree to get configuration for.</param>
        /// <param name="prefix">The prefix to filter keys by.</param>
        /// <returns>A dictionary of all keys matching the prefix and their values.</returns>
        /// <remarks>
        /// This method is useful for discovering all configuration options with a specific prefix.
        ///
        /// Example .editorconfig:
        /// <code>
        /// my_analyzer_enable_rule1 = true
        /// my_analyzer_enable_rule2 = false
        /// my_analyzer_max_depth = 10
        /// </code>
        ///
        /// Example usage:
        /// <code>
        /// var allOptions = AnalyzerOptionsReader.GetOptionsWithPrefix(options, tree, "my_analyzer");
        /// // Returns: { "my_analyzer_enable_rule1": "true", "my_analyzer_enable_rule2": "false", ... }
        /// </code>
        /// </remarks>
        public static Dictionary<string, string> GetOptionsWithPrefix(
            AnalyzerOptions options,
            SyntaxTree tree,
            string prefix)
        {
            var result = new Dictionary<string, string>();
            var provider = options.AnalyzerConfigOptionsProvider.GetOptions(tree);

            foreach (var key in provider.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    if (provider.TryGetValue(key, out var value))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }
    }
}
