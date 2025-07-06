using System.Text;

public enum AstType {
    None          = 0,
    Ident         = 1,
    Statement     = 2,
    Expression    = 3,
    Literal       = 4,
    Operator      = 5,
}

public enum StatementType {
    VarDecl = 0,
    Assign  = 1,
    Return  = 2,
    Typedef = 3,
}

public class AstNode {
    public AstType       Type;
    public string        String;
    public TokenType     OperatorType;
    public StatementType StmtType;
    public bool          IsBinary;
    public AstNode       Ident;
    public AstNode       Literal;
    public AstNode       Expression;
    public AstNode       Stmt;
    public AstNode       Operator;
    public AstNode       Left;
    public AstNode       Right;
    public TypeInfo      TypeInfo;

    public void Draw(StringBuilder sb, ref int indent) {
        const int spaces = 3;
        switch(Type) {
            case AstType.Ident : {
                sb.Append(' ', indent * spaces);
                sb.Append(String);
                sb.Append('\n');
            } break;
            case AstType.Literal : {
                sb.Append(' ', indent * spaces);
                sb.Append(TypeInfo.Name);
                sb.Append(" : ");
                sb.Append(String);
                sb.Append('\n');
            } break;
            case AstType.Operator : {
                sb.Append(' ', indent * spaces);
                if(IsBinary) {
                    sb.Append($"{OperatorType}:");
                    sb.Append('\n');
                    indent++;
                    Left.Draw(sb, ref indent);
                    Right.Draw(sb, ref indent);
                    indent--;
                } else {
                    sb.Append($"Unary{OperatorType}:");
                    sb.Append('\n');
                    indent++;
                    Right.Draw(sb, ref indent);
                    indent--;
                }
                sb.Append('\n');
            } break;
            case AstType.Statement : {
                sb.Append(' ', indent * spaces);
                sb.Append($"{StmtType}: ");
                sb.Append('\n');
                indent++;
                if(StmtType == StatementType.VarDecl) {
                    Ident.Draw(sb, ref indent);
                    if(Stmt != null) {
                        sb.Append('\n');
                        Stmt.Draw(sb, ref indent);
                    }
                } else if (StmtType == StatementType.Assign) {
                    Ident.Draw(sb, ref indent);
                    if(Literal != null) {
                        sb.Append('\n');
                        Literal.Draw(sb, ref indent);
                    } else if (Expression != null) {
                        sb.Append('\n');
                        Expression.Draw(sb, ref indent);
                    }
                } else if (StmtType == StatementType.Return) {
                    Expression.Draw(sb, ref indent);
                } else if (StmtType == StatementType.Typedef) {
                    sb.Append(TypeInfo.Name);
                    sb.Append(":\n");
                    indent++;
                    foreach(var field in TypeInfo.Fields) {
                        sb.Append(' ', spaces * indent);
                        sb.Append($"{field.Name} : {field.Type.Name}\n");
                    }
                    sb.Append(' ', spaces * indent);
                    sb.Append($"Align: {TypeInfo.Align}\n");
                    sb.Append(' ', spaces * indent);
                    sb.Append($"Size:  {TypeInfo.Size}");
                    indent--;
                }
                indent--;
                sb.Append('\n');
            } break;
        }
    }
}