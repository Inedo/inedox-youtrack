using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.YouTrack.VariableFunctions
{
    [Tag("youtrack")]
    [ScriptAlias("YouTrackTypeFieldName")]
    [Description("The name of the custom field used by YouTrack for an issue's type. The default is \"Type\".")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class YouTrackTypeFieldNameVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => "Type";
    }
}
