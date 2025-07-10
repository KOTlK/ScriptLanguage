using System.Collections.Generic;
using System.Text;

public struct Error {
    public string Message;
}

public class ErrorStream {
    public List<Error> Errors = new List<Error>();

    private static StringBuilder sb = new StringBuilder();

    public int Count => Errors.Count;

    public void Push(string message) {
        Errors.Add(new Error() {
            Message = message
        });
    }

    public override string ToString() {
        sb.Clear();

        foreach(var err in Errors) {
            sb.AppendLine($"Error: {err.Message}");
        }

        return sb.ToString();
    }

    public void UnexpectedSymbol(int line, int column, TokenType expected, TokenType got) {
        Errors.Add(new Error() {
            Message = $"unexpected symbol at {line}:{column}. Expected {expected}, got {got}"
        });
    }

    public void TypeAlreadyDefined(int line, int column, string name) {
        Errors.Add(new Error() {
            Message = $"{line}:{column}. Type '{name}' is already defined."
        });
    }

    public void FuncAlreadyDefined(int line, int column, string name) {
        Errors.Add(new Error() {
            Message = $"{line}:{column}. Function '{name}' is already defined."
        });
    }
}