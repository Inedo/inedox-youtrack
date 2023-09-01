#nullable enable
using Inedo.Extensibility;
using System.ComponentModel;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.YouTrack.VariableFunctions;

[ScriptAlias("YouTrackVersionFieldName")]
[Description("[Legacy] The name of the custom field used by YouTrack for an issue's targeted version number. The default is \"Fix version\".")]
[Undisclosed]
public sealed class YouTrackVersionFieldNameVariableFunction : ScalarVariableFunction
{
    protected override object EvaluateScalar(IVariableFunctionContext context) => "Fix version";
}
