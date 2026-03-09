using Markdig;
using Query.Core.Domain;
using Query.Core.Schema;

namespace Query.Core.Compilers;

public class HtmlCompiler : ICompiler
{
    private readonly MarkdownCompiler _markdownCompiler = new();
    private readonly MarkdownPipeline _pipeline;

    public string Format => "html";

    public HtmlCompiler()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public OutputBundle Compile(QuerySpec spec, PermissionContext permissions)
    {
        var markdownBundle = _markdownCompiler.Compile(spec, permissions);
        var htmlBody = Markdown.ToHtml(markdownBundle.RawOutput, _pipeline);

        var html = "<!DOCTYPE html>\n"
            + "<html lang=\"en\">\n"
            + "<head>\n"
            + "    <meta charset=\"UTF-8\">\n"
            + "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n"
            + $"    <title>{FormatTitle(spec)}</title>\n"
            + "    <style>\n"
            + "        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 900px; margin: 2rem auto; padding: 0 1rem; line-height: 1.6; color: #333; }\n"
            + "        table { border-collapse: collapse; width: 100%; margin: 1rem 0; }\n"
            + "        th, td { border: 1px solid #ddd; padding: 8px 12px; text-align: left; }\n"
            + "        th { background-color: #f5f5f5; font-weight: 600; }\n"
            + "        code { background-color: #f0f0f0; padding: 2px 6px; border-radius: 3px; font-size: 0.9em; }\n"
            + "        blockquote { border-left: 4px solid #e0e0e0; margin: 1rem 0; padding: 0.5rem 1rem; background: #fafafa; }\n"
            + "    </style>\n"
            + "</head>\n"
            + "<body>\n"
            + htmlBody + "\n"
            + "</body>\n"
            + "</html>";

        return new OutputBundle(
            RawOutput: html,
            Explanation: markdownBundle.Explanation,
            Spec: spec,
            Compiler: "html",
            Dialect: "html"
        );
    }

    private static string FormatTitle(QuerySpec spec) =>
        string.IsNullOrEmpty(spec.Intent)
            ? "Query Report"
            : char.ToUpperInvariant(spec.Intent[0]) + spec.Intent[1..] + " Report";
}
