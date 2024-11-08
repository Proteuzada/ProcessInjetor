using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace ScriptInjector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Script Injector com Lua ===");
            Console.WriteLine("1 - Carregar e injetar script");
            Console.WriteLine("2 - Sair");

            bool executando = true;
            string processName = "RobloxPlayerBeta"; // Nome fixo do processo
            string scriptContent = "loadstring(game:HttpGet(\"LINKGITHUB SCRIPT"))()";

            while (executando)
            {
                Console.Write("Selecione uma opção: ");
                string opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        await InjectAndExecuteScript(processName, scriptContent);
                        break;

                    case "2":
                        executando = false;
                        Console.WriteLine("Encerrando o injetor...");
                        break;

                    default:
                        Console.WriteLine("Opção inválida. Tente novamente.");
                        break;
                }
            }
        }

        // Função para injetar e executar o script Lua
        static async Task InjectAndExecuteScript(string processName, string luaScript)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    Console.WriteLine("Processo não encontrado.");
                    return;
                }

                foreach (var process in processes)
                {
                    Console.WriteLine($"Injetando e executando script no processo: {process.ProcessName} (ID: {process.Id})");

                    // Adiciona mock para funções Roblox e executa o código
                    string mockedLuaScript = await AddRobloxMocks(luaScript);

                    // Executa o script Lua usando MoonSharp
                    ExecuteLuaScript(mockedLuaScript, process.ProcessName);

                    LogInjectionAttempt(process.ProcessName, mockedLuaScript);
                }

                Console.WriteLine("Script Lua simulado como injetado e executado com sucesso.");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                Console.WriteLine($"Erro ao injetar e executar o script: {ex.Message}");
            }
        }

        // Função para adicionar mocks de objetos e funções Roblox
        static async Task<string> AddRobloxMocks(string luaScript)
        {
            // Baixa o script da URL
            string downloadedScript = await DownloadScript("LINKGITHUB SCRIPT");
            if (string.IsNullOrWhiteSpace(downloadedScript))
            {
                throw new Exception("O script baixado está vazio ou inválido.");
            }

            // Log do conteúdo do script baixado
            LogInjectionAttempt("Conteúdo do Script Baixado", downloadedScript);

            // Normaliza e escapa aspas e caracteres especiais no script baixado
            string normalizedScript = NormalizeLuaScript(downloadedScript);
            string escapedScript = EscapeLuaString(normalizedScript);

            // Define mocks para as principais funções e objetos Roblox
            string robloxMocks = $@"
                game = {{
                    HttpGet = function(self, url)
                        print('Mocked HttpGet called with URL:', url)
                        return {escapedScript}
                    end
                }}

                function loadstring(scriptContent)
                    return function() 
                        print('Executando o script injetado...')
                        local func, err = load(scriptContent)
                        if not func then
                            print('Erro ao carregar script:', err)
                            return
                        end
                        func()
                    end
                end

                -- Chamada para carregar e executar o script da URL
                loadstring(game:HttpGet('LINKGITHUB SCRIPT'))()
            ";

            return robloxMocks + "\n" + luaScript;
        }

        // Função para normalizar o script Lua, removendo quebras de linha
        static string NormalizeLuaScript(string script)
        {
            // Remove quebras de linha e espaços desnecessários
            return script.Replace("\r", "").Replace("\n", " ").Trim();
        }

        // Função para escapar strings no formato Lua
        static string EscapeLuaString(string str)
        {
            // Escapa aspas e barras invertidas
            return "\"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        // Função para baixar o script de uma URL
        public static async Task<string> DownloadScript(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                LogError($"Erro ao baixar o script: {ex.Message}");
                return string.Empty;
            }
        }

        // Função para executar o script Lua usando MoonSharp
        static void ExecuteLuaScript(string luaScript, string processName)
        {
            try
            {
                Script script = new Script();
                script.DoString(luaScript);
            }
            catch (SyntaxErrorException ex)
            {
                LogError($"Erro de sintaxe no script Lua no processo {processName}: {ex.Message}");
                throw; // Re-lança a exceção para tratamento posterior
            }
            catch (Exception ex)
            {
                LogError($"Erro ao executar o script Lua no processo {processName}: {ex.Message}");
                throw; // Re-lança a exceção para tratamento posterior
            }
        }

        // Função para logar a tentativa de injeção
        static void LogInjectionAttempt(string processName, string scriptContent)
        {
            string logPath = @"D:\csv\injection_log.txt";
            string logEntry = $"[{DateTime.Now}] Processo: {processName}\nScript:\n{scriptContent}\n";

            File.AppendAllText(logPath, logEntry);
            Console.WriteLine("Tentativa de injeção registrada no log.");
        }

        // Função para registrar erros em um arquivo de log
        static void LogError(string errorMessage)
        {
            string errorLogPath = @"D:\csv\error_log.txt";
            string errorEntry = $"[{DateTime.Now}] Erro: {errorMessage}\n";

            File.AppendAllText(errorLogPath, errorEntry);
            Console.WriteLine("Erro registrado no log de erros.");
        }
    }
}
