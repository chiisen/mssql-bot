using Microsoft.SqlServer.TransactSql.ScriptDom;

public class CustomTSqlFragmentVisitor : TSqlFragmentVisitor
{
    public List<string> InputParams { get; } = new List<string>();
    public List<string> OutputParams { get; } = new List<string>();
    public Dictionary<string, string> Assignments { get; } = new Dictionary<string, string>();
    public List<ScalarExpression> ReturnValues { get; } = new List<ScalarExpression>();

    public override void Visit(ProcedureParameter node)
    {
        if (node.Modifier == ParameterModifier.Output)
        {
            OutputParams.Add(node.VariableName.Value);
        }
        else
        {
            InputParams.Add(node.VariableName.Value);
        }
        base.Visit(node);
    }

    public override void Visit(SetVariableStatement node)
    {
        if (node == null)
        {
            return;
        }
        if (node.Variable != null && OutputParams.Contains(node.Variable.Name))
        {
            if (node.Expression != null) // 添加 null 检查
            {
                Assignments[node.Variable.Name] = node.Expression?.ToString() ?? string.Empty;
            }
        }
        base.Visit(node);
    }

    public override void Visit(ReturnStatement node)
    {
        if (node == null)
        {
            return;
        }
        if (node.Expression != null)
        {
            ReturnValues.Add(node.Expression);
        }
        base.Visit(node);
    }
}
