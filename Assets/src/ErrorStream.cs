using System.Collections.Generic;
using System.Text;

public struct Error {
    public string Message;
}

public class ErrorStream {
    public List<Error> Errors = new();

    private static StringBuilder sb = new();

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
}