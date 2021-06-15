using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.YouTrack.VariableFunctions
{
    [Tag("youtrack")]
    [ScriptAlias("YouTrackStatusFieldName")]
    [Description("The name of the custom field used by YouTrack for an issue's status. The default is \"State\".")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class YouTrackStatusFieldNameVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => "State";
    }
}
