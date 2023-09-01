#nullable enable
using Inedo.Extensibility;
using System.ComponentModel;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.YouTrack.VariableFunctions;

[ScriptAlias("YouTrackStatusFieldName")]
[Description("[Legacy] The name of the custom field used by YouTrack for an issue's status. The default is \"State\".")]
[Undisclosed]
public sealed class YouTrackStatusFieldNameVariableFunction : ScalarVariableFunction
{
    protected override object EvaluateScalar(IVariableFunctionContext context) => "State";
}
