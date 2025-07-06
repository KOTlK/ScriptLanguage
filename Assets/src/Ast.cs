using System.Collections.Generic;

using static AstType;
using static TokenType;
using static StatementType;

public class Ast {
    public List<AstNode> Typedefs = new();
    public List<AstNode> Nodes    = new();

    public void Add(AstNode child) {
        Nodes.Add(child);
    }
}

public static class AstParser {
    public static Ast Parse(Tokenizer tokenizer, ErrorStream err) {
        TypeSystem.Init();
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
                case TokenType.Struct :
                    root.Typedefs.Add(ParseTypedef(tokenizer, err));
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
            case TokenType.StringLiteral : {
                left = MakeLiteral(token, TypeSystem.String);
            } break;
            case TokenType.CharLiteral : {
                left = MakeLiteral(token, TypeSystem.Char);
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
                    if (next.Type == TokenType.Literal) {
                        node.Right = MakeLiteral(next);
                    } else if (next.Type == TokenType.StringLiteral) {
                        node.Right = MakeLiteral(next, TypeSystem.String);
                    } else if (next.Type == TokenType.CharLiteral) {
                        node.Right = MakeLiteral(next, TypeSystem.Char);
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

    private static AstNode ParseTypedef(Tokenizer tokenizer, ErrorStream err) {
        var name      = tokenizer.EatToken();
        var next      = tokenizer.EatToken();
        var node      = new AstNode();
        var type      = new TypeInfo();
        node.Type     = Statement;
        node.StmtType = Typedef;
        node.TypeInfo = type;
        type.Name     = name.StringValue;
        type.Fields   = new();

        if (next.Type != OParen) {
            err.UnexpectedSymbol(next.Line, next.Column, OParen, next.Type);
            return null;
        }

        uint align = 1;
        uint size  = 0;

        while (next.Type != EndOfFile) {
            next          = tokenizer.EatToken();
            if (next.Type == CParen) break;

            var colon     = tokenizer.EatToken();
            var fieldType = tokenizer.EatToken();
            var semicolon = tokenizer.EatToken();

            if (next.Type != TokenType.Ident) {
                err.UnexpectedSymbol(next.Line, next.Column, TokenType.Ident, next.Type);
                return null;
            }

            if (colon.Type != Colon) {
                err.UnexpectedSymbol(colon.Line, colon.Column, Colon, colon.Type);
                return null;
            }

            if (fieldType.Type != TokenType.Ident) {
                err.UnexpectedSymbol(fieldType.Line, fieldType.Column, TokenType.Ident, fieldType.Type);
                return null;
            }

            if (semicolon.Type != Semicolon) {
                err.UnexpectedSymbol(semicolon.Line, semicolon.Column, Semicolon, semicolon.Type);
                return null;
            }

            var field = new FieldInfo();

            field.Name = next.StringValue;
            field.Type = TypeSystem.GetType(fieldType.StringValue);

            if (field.Type.Align > align) {
                align = field.Type.Align;
            }

            size += field.Type.Size;

            type.Fields.Add(field);

        }

        if(next.Type == EndOfFile) {
            err.UnexpectedSymbol(next.Line, next.Column, CParen, EndOfFile);
        }

        type.Align = align;
        type.Size  = size;

        var add = TypeSystem.RegisterType(type);

        if (!add) {
            err.TypeAlreadyDefined(name.Line, name.Column, name.StringValue);
            return null;
        }

        return node;
    }

    private static AstNode MakeIdent(string ident) {
        var node    = new AstNode();
        node.Type   = AstType.Ident;
        node.String = ident;

        return node;
    }

    private static AstNode MakeLiteral(Token token, TypeInfo type = null) {
        var node    = new AstNode();
        node.Type   = AstType.Literal;
        node.String = token.StringValue;

        if(type == null) {
            node.TypeInfo = GuessLiteralType(token.StringValue);
        } else {
            node.TypeInfo = type;
        }

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

    private static int GetPrecedence(TokenType token) {
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

    private static TypeInfo GuessLiteralType(string value) {
        if (value[value.Length - 1] == 'f' ||
           value[value.Length - 1] == 'F') {
            return TypeSystem.Float;
        }

        if (value[value.Length - 1] == 'd' ||
           value[value.Length - 1] == 'D') {
            return TypeSystem.Double;
        }

        if (float.TryParse(value, out var f)) {
            return TypeSystem.Float;
        }

        if (double.TryParse(value, out var d)) {
            return TypeSystem.Double;
        }

        if (byte.TryParse(value, out var u8)) {
            return TypeSystem.u8;
        }

        if (sbyte.TryParse(value, out var s8)) {
            return TypeSystem.s8;
        }

        if (short.TryParse(value, out var s16)) {
            return TypeSystem.s16;
        }

        if (ushort.TryParse(value, out var u16)) {
            return TypeSystem.u16;
        }

        if (int.TryParse(value, out var s32)) {
            return TypeSystem.s32;
        }

        if (uint.TryParse(value, out var u32)) {
            return TypeSystem.u32;
        }

        if (long.TryParse(value, out var s64)) {
            return TypeSystem.s64;
        }

        if (ulong.TryParse(value, out var u64)) {
            return TypeSystem.u64;
        }

        return null;
    }
}