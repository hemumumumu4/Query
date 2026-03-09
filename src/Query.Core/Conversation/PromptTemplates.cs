namespace Query.Core.Conversation;

public record QuerySpecSummary(
    string Intent,
    List<string> Entities,
    List<string> Measures,
    List<string> Dimensions,
    List<string> Filters);

public static class PromptTemplates
{
    public static string IntentExtraction(string schemaJson) =>
        $$"""
        You are a data-analyst assistant. Given the database schema below, extract the user's intent.

        ## Schema
        {{schemaJson}}

        ## Instructions
        Respond with ONLY a JSON object with the following structure:
        {
          "intent": "<aggregation|lookup|comparison|trend>",
          "entities": [{ "table": "<table_name>", "alias": "<alias>" }],
          "measures": [{ "expression": "<SQL expression>", "alias": "<name>" }],
          "dimensions": [{ "expression": "<SQL expression>", "alias": "<name>" }],
          "filters": [{ "expression": "<column>", "operator": "<op>", "value": "<val>" }],
          "clarification_needed": <true|false>,
          "clarification_question": "<question if clarification_needed is true, else empty>"
        }
        """;

    public static string SpecConfirmation(QuerySpecSummary summary) =>
        $"""
        Please confirm the following query specification:
        - Intent: {summary.Intent}
        - Tables: {string.Join(", ", summary.Entities)}
        - Measures: {string.Join(", ", summary.Measures)}
        - Dimensions: {string.Join(", ", summary.Dimensions)}
        - Filters: {string.Join(", ", summary.Filters)}

        Is this correct? Reply with "yes" to confirm or describe what should change.
        """;
}
