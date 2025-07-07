using System.IO;
using UnityEngine;
using System.Text;
using TMPro;

using static TokenType;
using static AstParser;

public class Test : MonoBehaviour {
    public TMP_Text Text;
    private void Start() {
        var path      = $"{Application.streamingAssetsPath}/test_file.sl";
        var text      = File.ReadAllText(path);
        var tokenizer = Lexer.Tokenize(text);
        var err       = new ErrorStream();
        var sb        = new StringBuilder();

        var ast = AstParser.Parse(tokenizer, err);

        if(err.Count > 0) {
            Debug.LogError(err.ToString());
        }

        foreach(var node in ast.Typedefs) {
            var indent = 0;
            node.Draw(sb, ref indent);
            sb.Append('\n', 1);
        }

        sb.Append('\n', 3);

        foreach(var node in ast.Functions) {
            var indent = 0;
            node.Draw(sb, ref indent);
            sb.Append('\n', 1);
        }
        
        sb.Append('\n', 3);

        foreach(var node in ast.Nodes) {
            var indent = 0;
            node.Draw(sb, ref indent);
            sb.Append('\n', 3);
        }

        Text.text = sb.ToString();
    }
}