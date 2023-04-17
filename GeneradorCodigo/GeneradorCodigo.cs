using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProblemSolver.GeneradorCodigo
{
    class GeneradorCodigo : IGeneradorCodigo
    {
        private string PromptSystemCodigo;
        private string PromptSystemTests;

        private readonly OpenAIAPI _api; public GeneradorCodigo(OpenAIAPI api)
        {
            _api = api;
            PromptSystemCodigo = File.ReadAllText("C:\\Users\\pvega\\source\\repos\\ProblemSolver\\GeneradorCodigo\\promptsystemcodigo.txt");
            PromptSystemTests = File.ReadAllText("C:\\Users\\pvega\\source\\repos\\ProblemSolver\\GeneradorCodigo\\promptsystemtests.txt");
        }

        public async Task<string> GenerarCodigoFuncionAsync(string problema)
        {
            var conversation = _api.Chat.CreateConversation();
            conversation.AppendSystemMessage(PromptSystemCodigo);
            conversation.AppendUserInput(problema);
            string codigo = await conversation.GetResponseFromChatbotAsync();
            return codigo;
        }

        public async Task<string> GenerarCodigoTestsAsync(string problema, string codigoFuncion)
        {
            var conversation = _api.Chat.CreateConversation();
            conversation.AppendSystemMessage(PromptSystemTests);
            conversation.AppendUserInput(codigoFuncion);
            string codigo = await conversation.GetResponseFromChatbotAsync();
            return codigo;
        }

    }
}
