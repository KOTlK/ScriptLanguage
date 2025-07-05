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

        // Text.text = tokenizer.ToString();

        // Text.text = sb.ToString();

        var ast = AstParser.Parse(tokenizer, err);

        foreach(var node in ast.Nodes) {
            var indent = 0;
            node.Draw(sb, ref indent);
            sb.Append('\n', 3);
        }

        Text.text = sb.ToString();


        if(err.Count > 0) {
            Debug.LogError(err.ToString());
        }
    }
}