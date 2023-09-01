#nullable enable
using Inedo.Extensibility;
using System.ComponentModel;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.YouTrack.VariableFunctions;

[ScriptAlias("YouTrackTypeFieldName")]
[Description("[Legacy] The name of the custom field used by YouTrack for an issue's type. The default is \"Type\".")]
[Undisclosed]
public sealed class YouTrackTypeFieldNameVariableFunction : ScalarVariableFunction
{
    protected override object EvaluateScalar(IVariableFunctionContext context) => "Type";
}
