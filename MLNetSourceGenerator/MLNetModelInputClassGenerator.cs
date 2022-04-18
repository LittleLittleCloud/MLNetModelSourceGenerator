using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.ML;
using MLNetSourceGenerator.Template;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MLNetSourceGenerator
{
    [Generator]
    public class MLNetModelInputClassGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
            var mlContext = new MLContext();
            // get all mlnet model paths
            var mlnetModels = context.AdditionalFiles.Select(f =>
            {
                var path = f.Path;
                var modelName = Path.GetFileNameWithoutExtension(path);
                context.AnalyzerConfigOptions.GetOptions(f).TryGetValue("build_metadata.additionalfiles.IsMLNetModel", out var isMLNetModel);
                context.AnalyzerConfigOptions.GetOptions(f).TryGetValue("build_metadata.additionalfiles.InputClassName", out var inputClassName);
                context.AnalyzerConfigOptions.GetOptions(f).TryGetValue("build_metadata.additionalfiles.OutputClassName", out var outputClassName);
                inputClassName = string.IsNullOrEmpty(inputClassName)? Utils.Sanitize(modelName + "Input") : inputClassName!;
                outputClassName = string.IsNullOrEmpty(outputClassName) ? Utils.Sanitize(modelName + "Output") : outputClassName!;
                bool.TryParse(isMLNetModel, out var isModel);
                return (path, modelName, inputClassName, outputClassName, isModel);
            }).Where(f => f.isModel);

            foreach(var modelMetaData in mlnetModels)
            {
                var model = mlContext.Model.Load(modelMetaData.path, out var inputSchema);
                var outputSchema = model.GetOutputSchema(inputSchema);
                var inputClass = Utils.GenerateClassCodeFromSchema(modelMetaData.inputClassName, inputSchema);
                var outputClass = Utils.GenerateClassCodeFromSchema(modelMetaData.outputClassName, outputSchema);

                var clazz = new ModelInputOutputClassTemplate()
                {
                    ModelInputClass = inputClass,
                    ModelOutputClass = outputClass,
                    NameSpace = "MLNetSourceGenerator.CodeGen",
                }.TransformText();

                context.AddSource($"{modelMetaData.modelName}.g.cs", SourceText.From(clazz, Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            
        }
    }
}