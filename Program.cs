using Newtonsoft.Json;
using OpenAI_API;
using ProblemSolver;
using ProblemSolver.GeneradorCodigo;
using System.Diagnostics;

public class Mensaje
{
    public string CodigoRealizado { get; set; }
    public string CodigoCorregido { get; set; }
    public string Comentarios { get; set; }
    
}

public class BuildResult
{
    public bool Success { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }



    public BuildResult((bool success, string output, string error) resBuild)
    {
        Success = resBuild.success;
        Output = resBuild.output;
        Error = resBuild.error;

    }
}


class Program
{

    

    static async Task Main(string[] args)
    {
        var api = new OpenAIAPI("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");

        //string problema = "El objetivo de la función es sumar dos números enteros.";
        string problema = "El objetivo de la función a crear es resolver una funcion de segundo grado\n";
        //problema = "Crea una aplicacion de consola para bajar el documento https://www.nachocabanes.com/csharp/curso2015/csharp_ejercicios.php," + 
        //           "al terminar mandar el documento a la direccion pedrovega@gmail.com";

        problema = "Crea una aplicacion de c# que se le pase el path de un proyecto de visual studio y genere un listado de todos los ficheros que componen el proyecto\n" +
                   "No utilizar XmlDocument. Objetivos :" +
                   "1. " +
                   "2. " +
                   "3." +
                   "4. \n" ;


        IGeneradorCodigo generadorCodigo = new GeneradorCodigo(api);

        // Crear instancias de ConsoleProjectCreator, ConsoleProjectBuilder y NUnitTestProjectCreator
        IOutputHandler outputHandler = new ConsoleOutputHandler();
        IProjectCreator projectCreator = new ConsoleProjectCreator(outputHandler);
        IProjectBuilder projectBuilder = new ConsoleProjectBuilder(outputHandler);
        ITestProjectCreator testProjectCreator = new NUnitTestProjectCreator(outputHandler);

        var cicloProceso = new CicloProceso(generadorCodigo, projectCreator, projectBuilder, testProjectCreator);
        await cicloProceso.EjecutarCicloAsync(problema, outputHandler);
    }
}

class CicloProceso
{
    private readonly IGeneradorCodigo _generadorCodigo;
    private readonly IProjectCreator _projectCreator;
    private readonly IProjectBuilder _projectBuilder;
    private readonly ITestProjectCreator _testProjectCreator;

    public CicloProceso(IGeneradorCodigo generadorCodigo, IProjectCreator projectCreator, IProjectBuilder projectBuilder, ITestProjectCreator testProjectCreator)
    {
        _generadorCodigo = generadorCodigo;
        _projectCreator = projectCreator;
        _projectBuilder = projectBuilder;
        _testProjectCreator = testProjectCreator;
    }
    public async Task EjecutarCicloAsync(string problema, IOutputHandler outputHandler)
    {
        string msgerror = "\nParece que este codigo tiene errores, corrigelos y devuelve el resultado en el json : \n";
        // Crear proyectos de consola y prueba
        string appName = "FuncionApp";
        string testProjectName = "FuncionApp.Tests";
        string parentDirectory = @"C:\Users\pvega\pruebasNet";

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(problema + "\n");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        _projectCreator.Create(appName, parentDirectory);
        string mainProjectPath = Path.Combine(parentDirectory, appName, $"{appName}.csproj");
        _testProjectCreator.CreateTestProject(testProjectName, parentDirectory, mainProjectPath);

        BuildResult buildResult = new BuildResult((true,"",""));
        Estado estadoActual = Estado.GenerarFuncion;
        string codigoFuncion = null;
        string codigoTests = null;
        bool compilacionExitosa = false;
        Mensaje msg = new Mensaje();

        
        while (estadoActual != Estado.Finalizar)
        {
            switch (estadoActual)
            {
                case Estado.GenerarFuncion:
                    string mensajeParaChatbot;
                    if (buildResult.Success == false)
                    {
                        mensajeParaChatbot = problema + codigoFuncion + msgerror + buildResult.Output;
                    }
                    else
                    {
                        mensajeParaChatbot = problema;
                    }
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Generando Codigo. Esperando IA...");
                    var chatbotResponse = await _generadorCodigo.GenerarCodigoFuncionAsync(mensajeParaChatbot);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(chatbotResponse);
                    try
                    {
                        msg = JsonConvert.DeserializeObject<Mensaje>(chatbotResponse);
                        if (msg.CodigoCorregido != null && msg.CodigoCorregido.Count() > 0)
                        {
                            codigoFuncion = msg.CodigoCorregido;
                        }
                        else
                        {
                            codigoFuncion = msg.CodigoRealizado;
                        }
                        estadoActual = Estado.CompilarFuncion;
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message );
                        buildResult.Success = false;
                        buildResult.Output= e.Message;
                        estadoActual = Estado.GenerarFuncion;
                    }
                    
                    
                    break;

                case Estado.CompilarFuncion:
                    string mainProjectFile = Path.Combine(parentDirectory, appName, "Program.cs");
                    File.WriteAllText(mainProjectFile, codigoFuncion);
                    var resBuild = _projectBuilder.Build(Path.Combine(parentDirectory, appName));
                    buildResult = new BuildResult(resBuild);
                    estadoActual = buildResult.Success ? Estado.GenerarTests : Estado.GenerarFuncion;
                    if (buildResult.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Codigo generado correcto.");
                    }
                    break;

               

                case Estado.GenerarTests:
                    if (buildResult.Success == false)
                    {
                        mensajeParaChatbot = codigoTests + msgerror + buildResult.Output;
                    }
                    else
                    {
                        mensajeParaChatbot = problema;
                    }
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Generando Tests. Esperando IA...");
                    chatbotResponse = await _generadorCodigo.GenerarCodigoTestsAsync(mensajeParaChatbot, codigoFuncion);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(chatbotResponse);

                    try
                    {
                        msg = JsonConvert.DeserializeObject<Mensaje>(chatbotResponse);
                        codigoTests = msg.CodigoRealizado;
                        estadoActual = Estado.CompilarTests;
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message);
                        buildResult.Success = false;
                        buildResult.Output = e.Message;
                        estadoActual = Estado.GenerarTests;
                    }
                    
                    break;

                case Estado.CompilarTests:
                    string testProjectFile = Path.Combine(parentDirectory, testProjectName, "UnitTest1.cs");
                    File.WriteAllText(testProjectFile, codigoTests);

                    resBuild = _projectBuilder.Build(Path.Combine(parentDirectory, testProjectName));
                    buildResult = new BuildResult(resBuild);
                    estadoActual = buildResult.Success ? Estado.EjecutarTests : Estado.GenerarTests;
                    break;

                case Estado.EjecutarTests:
                  
                    
                    // Ejecutar pruebas y verificar el resultado
                    ProblemSolver.ITestRunner testRunner = new NUnitTestRunner(outputHandler);
                    bool testsExitosos = testRunner.RunTests(Path.Combine(parentDirectory, testProjectName));
                    estadoActual = testsExitosos ? Estado.Finalizar : Estado.ReportarFallo;
                    break;

               
            }
        }

        Console.WriteLine("¡La función y los tests se ejecutaron correctamente!");
    }

    enum Estado
    {
        GenerarFuncion,
        CompilarFuncion,
        GenerarTests,
        CompilarTests,
        EjecutarTests,
        Finalizar,
        ReportarFallo
    }
}
