using CompilerLabs.Core.Lexer;
using CompilerLabs.Core.Parser.Ast;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CompilerLabs.Core.Parser
{
    public class IncrementalWorkspace
    {
        private readonly ConcurrentDictionary<string, BlockState> _blocks = new ConcurrentDictionary<string, BlockState>();

        private class BlockState
        {
            public string TextHash { get; set; } = string.Empty;
            public List<Statement> AstNodes { get; set; } = new List<Statement>();
            public List<string> Errors { get; set; } = new List<string>();
        }

        public async Task UpdateBlockAsync(string blockId, string newCode)
        {
            var newHash = ComputeHash(newCode);

            if (_blocks.TryGetValue(blockId, out var existingState))
            {
                if (existingState.TextHash == newHash)
                {
                    return;
                }
            }

            await Task.Run(() =>
            {
                var lexer = new Lexer.Lexer(newCode);
                var tokens = lexer.Tokenize();

                var parser = new Parser(tokens);
                var ast = AstConstantFolder.FoldConstants(parser.Parse());

                _blocks[blockId] = new BlockState
                {
                    TextHash = newHash,
                    AstNodes = ast,
                    Errors = parser.Errors.ToList()
                };
            });
        }

        public async Task UpdateMultipleBlocksAsync(Dictionary<string, string> files)
        {
            var tasks = files.Select(kvp => UpdateBlockAsync(kvp.Key, kvp.Value));
            await Task.WhenAll(tasks);
        }

        public List<Statement> GetFullAst()
        {
            var fullAst = new List<Statement>();
            foreach (var state in _blocks.Values)
            {
                fullAst.AddRange(state.AstNodes);
            }
            return fullAst;
        }

        public List<string> GetAllErrors()
        {
            var allErrors = new List<string>();
            foreach (var kvp in _blocks)
            {
                if (kvp.Value.Errors.Any())
                {
                    allErrors.Add($"--- Ошибки в блоке {kvp.Key} ---");
                    allErrors.AddRange(kvp.Value.Errors);
                }
            }
            return allErrors;
        }

        private string ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return System.Convert.ToBase64String(hash);
            }
        }
    }
}