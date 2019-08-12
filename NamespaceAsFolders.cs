using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using System.Collections.Immutable;
using System;

using System.IO;
using System.Reflection;
using System.Threading;
using _1313.Omnisharp.Extensions.Helpers;

namespace _1313.Omnisharp.Extensions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceAsFoldersAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Namespace should match folder structure";
        internal const string MessageFormat = "Make namespace match folder hierarchy: '{0}'";
        const string Description = "In order to improve project navigation, namespaces should match folder structure";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "NAMESPACES01",
            Title,
            MessageFormat,
            "NAMESPACES01",
            DiagnosticSeverity.Info,
            true,
            description: Description
            );



        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(c =>
            {
                var rootFolders = c.Compilation.SyntaxTrees.Where(x => !x.FilePath.Contains("AssemblyAttributes.cs"))
                                                           .Select(s => new
                                                           {
                                                               Path = Path.GetDirectoryName(s.FilePath),
                                                               NumParts = s.FilePath.Split(Path.AltDirectorySeparatorChar).Length
                                                           })
                                                           .Distinct()
                                                           .OrderBy(a => a.NumParts)
                                                           .Take(2);
                string root = null;

                if (rootFolders.Count() == 2 && rootFolders.First().NumParts == rootFolders.Last().NumParts)
                {
                    root = string.Join(Path.AltDirectorySeparatorChar, rootFolders.First().Path.Split(Path.AltDirectorySeparatorChar).SkipLast(1));
                }
                else
                {
                    root = rootFolders.FirstOrDefault()?.Path;
                }
                c.RegisterSyntaxNodeAction((syntaxTreeContext) =>
                {
                    var semModel = syntaxTreeContext.SemanticModel;
                    var filePath = syntaxTreeContext.Node.SyntaxTree.FilePath;

                    if (filePath == null)
                        return;

                    var parentDirectory = Path.GetDirectoryName(filePath)
                                              .Replace(root, "")
                                              .Replace(Path.DirectorySeparatorChar, '.')
                                              .Trim('.');


                    var assemblyName = syntaxTreeContext.Compilation.AssemblyName.SafeIdentifier();
                    var requiredNamespace = $"{assemblyName}.{parentDirectory}".Trim('.').SafeIdentifier();

                    var symbolInfo = semModel.GetDeclaredSymbol(syntaxTreeContext.Node) as INamespaceSymbol;

                    var name = symbolInfo.ToDisplayString();


                    if (requiredNamespace != name)
                    {

                        syntaxTreeContext.ReportDiagnostic(Diagnostic.Create(
                           Rule, (syntaxTreeContext.Node as NamespaceDeclarationSyntax).Name.GetLocation(), requiredNamespace));
                    }



                }, SyntaxKind.NamespaceDeclaration);
            });

        }
    }
}