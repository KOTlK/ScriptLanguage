using System.Collections.Generic;

using static AstType;
using static TokenType;
using static StatementType;

public class Ast {
    public List<AstNode> Nodes = new();

    public void Add(AstNode child) {
        Nodes.Add(child);
    }
}

public static class AstParser {
    public static Ast Parse(Tokenizer tokenizer, ErrorStream err) {
        var root = new Ast();

        while (tokenizer.GetCurrent().Type != EndOfFile) {
            var currentToken = tokenizer.GetCurrent();
            switch (currentToken.Type) {
                case TokenType.Equals :
                    root.Add(ParseAssignment(tokenizer, err));
                    break;
                case TokenType.Return :
                    root.Add(ParseReturn(tokenizer, err));
                    break;
                default :
                    currentToken = tokenizer.EatToken();
                    break;
            }
        }

        return root;
    }

    // public static int ComputeExpression(AstNode expr) {
    //     switch (expr.Type) {
    //         case AstType.Number :
    //             return expr.IntValue;
    //         case AstType.Operator : {
    //             if(expr.IsBinary == false && expr.OperatorType == Minus) {
    //                 return -ComputeExpression(expr.Right);
    //             }

    //             var left  = ComputeExpression(expr.Left);
    //             var right = ComputeExpression(expr.Right);

    //             switch (expr.OperatorType) {
    //                 case Plus :
    //                     return left + right;
    //                 case Minus :
    //                     return left - right;
    //                 case Mul :
    //                     return left * right;
    //                 case Div :
    //                     return left / right;
    //                 case Mod :
    //                     return left % right;
    //                 default :
    //                     UnityEngine.Debug.LogError("WRONG");
    //                     break;
    //             }
    //         } break;
    //     }

    //     return 0;
    // }

    private static AstNode ParseAssignment(Tokenizer tokenizer, ErrorStream err) {
        var prev  = tokenizer.Previous();
        var next  = tokenizer.Peek();
        var ident = prev.Type == TokenType.Ident ? prev : tokenizer.Previous(2);

        // ident = expr; == assign
        var assign        = new AstNode();
        assign.Type       = Statement;
        assign.StmtType   = Assign;
        assign.Ident      = MakeIdent(ident.StringValue);
        tokenizer.EatToken();
        assign.Expression = ParseExpression(tokenizer, err, -9999);

        if(prev.Type == Colon) {
            // ident := expr; == vardecl + assign
            var vardecl      = new AstNode();
            vardecl.Type     = Statement;
            vardecl.StmtType = VarDecl;
            vardecl.Ident    = assign.Ident;

            vardecl.Stmt = assign;
            assign       = vardecl;
        }

        return assign;
    }

    private static AstNode ParseReturn(Tokenizer tokenizer, ErrorStream err) {
        var node      = new AstNode();
        node.Type     = Statement;
        node.StmtType = StatementType.Return;

        var token = tokenizer.EatToken();
        var next  = tokenizer.Peek();

        node.Expression = ParseExpression(tokenizer, err, -9999);

        return node;
    }

    public static AstNode ParseExpression(Tokenizer tokenizer, ErrorStream err, int prec) {
        var token = tokenizer.GetCurrent();
        AstNode left = null;

        switch(token.Type) {
            case TokenType.Ident : {
                left = MakeIdent(token.StringValue);
            } break;
            case TokenType.Literal : {
                left = MakeLiteral(token);
            } break;
            case TokenType.ORParen : {
                tokenizer.EatToken();
                left = ParseExpression(tokenizer, err, -9999);
                tokenizer.EatToken();
            } break;
            case TokenType.Minus : {
                if(IsBinary(tokenizer) == false) {
                    var node          = new AstNode();
                    node.Type         = Operator;
                    node.OperatorType = TokenType.Minus;
                    node.IsBinary     = false;
                    var next = tokenizer.EatToken();
                    if(next.Type == TokenType.Literal) {
                        node.Right = MakeLiteral(next);
                    } else {
                        node.Right = ParseExpression(tokenizer, err, prec);
                    }
                    left = node;
                }
            } break;
        }

        while(true) {
            token         = tokenizer.EatToken();
            var tokenPrec = GetPrecedence(token.Type);

            if(IsOperator(token) == false) {
                tokenizer.Current--;
                break;
            }

            if(tokenPrec < prec) {
                tokenizer.Current--;
                break;
            }

            var node          = new AstNode();
            node.Type         = Operator;
            node.OperatorType = token.Type;
            node.IsBinary     = true;
            node.Left         = left;
            var next = tokenizer.EatToken();
            node.Right        = ParseExpression(tokenizer, err, tokenPrec + 1);

            left = node;
        }

        return left;
    }

    private static AstNode MakeIdent(string ident) {
        var node    = new AstNode();
        node.Type   = AstType.Ident;
        node.String = ident;

        return node;
    }

    private static AstNode MakeLiteral(Token token) {
        var node    = new AstNode();
        node.Type   = AstType.Literal;
        node.String = token.StringValue;

        return node;
    }

    private static bool IsBinary(Tokenizer tokenizer) {
        var cur  = tokenizer.GetCurrent();

        switch(cur.Type) {
            case Plus    : return true;
            case Minus   : {
                if(tokenizer.Current == 0) return false;
                var prev = tokenizer.Previous();
                if(IsOperator(prev) ||
                   prev.Type == TokenType.Equals ||
                   prev.Type == ORParen) {
                    return false;
                }
                return true;
            }
            case Mul     : return true;
            case Div     : return true;
            case Exp     : return true;
            case Mod     : return true;
            case ORParen : return true;

            default : return false;
        }
    }

    private static bool IsOperator(Token token) => token.Type switch {
        Minus => true,
        Plus  => true,
        Mul   => true,
        Div   => true,
        Exp   => true,
        Mod   => true,
        _     => false,
    };

    public static int GetPrecedence(TokenType token) {
        switch (token) {
            case Minus : return 10;
            case Plus  : return 10;
            case Mul   : return 20;
            case Div   : return 20;
            case Mod   : return 20;
            case Exp   : return 30;
            default    : return 0;
        }
    }
}