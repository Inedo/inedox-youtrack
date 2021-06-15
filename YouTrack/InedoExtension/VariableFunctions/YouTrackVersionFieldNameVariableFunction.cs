using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.YouTrack.VariableFunctions
{
    [Tag("youtrack")]
    [ScriptAlias("YouTrackVersionFieldName")]
    [Description("The name of the custom field used by YouTrack for an issue's targeted version number. The default is \"Fix version\".")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class YouTrackVersionFieldNameVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => "Fix version";
    }
}
