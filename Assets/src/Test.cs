using System.IO;
using UnityEngine;

using static TokenType;

public class Test : MonoBehaviour {
    private void Start() {
        var path      = $"{Application.streamingAssetsPath}/test_file.sl";
        var text      = File.ReadAllText(path);
        var tokenizer = Lexer.Tokenize(text);

        foreach(var token in tokenizer.Tokens) {
            if(token.Type == Char) {
                Debug.Log($"{token.Type}, {token.CharValue}");
            } else {
                Debug.Log($"{token.Type}, {token.StringValue}");
            }
        }
    }
}