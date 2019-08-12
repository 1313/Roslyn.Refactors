using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.IO;

using System.Threading.Tasks;
using _1313.Omnisharp.Extensions.Helpers;

namespace _1313.Omnisharp.Extensions
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceAsFoldersProvider)), Shared]
    public class NamespaceAsFoldersProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("NAMESPACES01");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (!diagnostic.Id.Equals("NAMESPACES01"))
                {
                    continue;
                }

                var token = root.FindNode(diagnostic.Location.SourceSpan);
                if (token.IsMissing)
                {
                    continue;
                }

                if (token != null)
                {

                    var rootFolder = Path.GetDirectoryName(document.Project.FilePath).Split(Path.DirectorySeparatorChar).LastOrDefault();
                    if (!string.IsNullOrEmpty(rootFolder))
                    {

                        var folderName = Path.GetDirectoryName(token.SyntaxTree.FilePath)
                                             .Split($"{Path.DirectorySeparatorChar}{rootFolder}")
                                             .LastOrDefault()?
                                             .Replace(Path.DirectorySeparatorChar, '.')?
                                             .Trim('.');

                        var rootNamespace = $"{document.Project.AssemblyName}";


                        var newName = $"{rootNamespace}.{folderName}".Trim('.').SafeIdentifier();
                        context.RegisterCodeFix(CodeAction.Create("Make namespace match folder",
                             cancellationToken => Task.FromResult(RenameSymbol(document, root, token, newName)),
                             equivalenceKey: nameof(NamespaceAsFoldersProvider)), diagnostic);
                    }
                }
            }
        }
        public static Solution RenameSymbol(Document document, SyntaxNode root, SyntaxNode declarationToken, string newName)
        {
            var newNode = SyntaxFactory.IdentifierName(newName);
            root = root.ReplaceNode(declarationToken, newNode);
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, root);
        }


    }
}