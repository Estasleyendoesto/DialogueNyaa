using System;
using System.Collections.Generic;
using System.Globalization;

namespace EEsto.DialogueNyaa
{
    /// <summary>
    /// Valida la estructura de un guion ANTES de pasarlo a DialogueParser.
    /// DialogueParser asume que el texto ya es válido y no hace ninguna
    /// comprobación defensiva: si algo aquí falla, este es el único punto
    /// donde debería detectarse, con línea y mensaje claro.
    /// </summary>
    public static class DialogueValidator
    {
        public readonly struct Issue
        {
            public readonly int Line;
            public readonly string Message;

            public Issue(int line, string message)
            {
                Line = line;
                Message = message;
            }

            public override string ToString() =>
                $"Línea {Line}: {Message}";
        }

        public sealed class Result
        {
            public readonly List<Issue> Errors = new();
            public readonly List<Issue> Warnings = new();
            public readonly HashSet<string> Nodes = new();

            public bool IsValid => Errors.Count == 0;
        }

        private sealed class ChoiceFrame
        {
            public int OptionCount;
            public int OptionStartLine;
            public int OptionBodyLines;
        }

        private sealed class IfFrame
        {
            public int OpenLine;
            public bool HasElse;
        }

        public static Result Validate(string rawText)
        {
            var result = new Result();

            if (string.IsNullOrEmpty(rawText))
            {
                result.Errors.Add(new Issue(0, "El guion está vacío."));
                return result;
            }

            string[] lines = NormalizeLineEndings(rawText).Split('\n');

            var jumpRefs = new List<(int Line, string Label)>();
            var ifs = new Stack<IfFrame>();
            var choices = new Stack<ChoiceFrame>();

            string currentNode = null;
            int currentNodeLine = 0;
            bool hasAnyContent = false;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string line = lines[i].Trim();

                if (line.Length == 0 || line.StartsWith("//"))
                    continue;

                if (line.StartsWith("node "))
                {
                    CloseNode(result, choices, ifs, currentNode, currentNodeLine, hasAnyContent);

                    string nodeName = ParseNodeName(line);

                    if (string.IsNullOrEmpty(nodeName))
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "Declaración de nodo sin nombre ('node :')."));
                    }
                    else if (!result.Nodes.Add(nodeName))
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            $"Nodo '{nodeName}' declarado más de una vez. " +
                            "La segunda declaración sobreescribe la primera en silencio."));
                    }

                    currentNode = nodeName;
                    currentNodeLine = lineNumber;
                    hasAnyContent = false;

                    continue;
                }

                if (currentNode == null)
                {
                    result.Errors.Add(new Issue(
                        lineNumber,
                        "Contenido antes de declarar ningún 'node'. " +
                        "El guion debe empezar con 'node <nombre>:'."));

                    continue;
                }

                hasAnyContent = true;

                if (line.StartsWith("-"))
                {
                    if (choices.Count == 0)
                    {
                        choices.Push(new ChoiceFrame());
                    }
                    else if (choices.Peek().OptionBodyLines == 0)
                    {
                        result.Warnings.Add(new Issue(
                            choices.Peek().OptionStartLine,
                            "Opción de choice sin ningún contenido en su cuerpo (ni diálogo ni comando). " +
                            "El parser la cerrará en silencio con un salto al final del choice, " +
                            "sin disparar ningún ':: Return'."));
                    }

                    ChoiceFrame frame = choices.Peek();
                    frame.OptionCount++;
                    frame.OptionStartLine = lineNumber;
                    frame.OptionBodyLines = 0;

                    continue;
                }

                if (line == ":: continue")
                {
                    if (choices.Count == 0)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: continue' sin ningún choice abierto " +
                            "(el parser lanzará una excepción al intentar cerrarlo)."));
                    }
                    else
                    {
                        ChoiceFrame frame = choices.Pop();

                        if (frame.OptionBodyLines == 0)
                        {
                            result.Warnings.Add(new Issue(
                                frame.OptionStartLine,
                                "Opción de choice sin ningún contenido en su cuerpo antes de ':: continue'."));
                        }
                    }

                    continue;
                }

                if (line.StartsWith("::"))
                {
                    if (choices.Count > 0)
                        choices.Peek().OptionBodyLines++;

                    ValidateSystemLine(line, lineNumber, ifs, jumpRefs, result);
                    continue;
                }

                // Línea de diálogo normal.
                if (choices.Count > 0)
                    choices.Peek().OptionBodyLines++;
            }

            CloseNode(result, choices, ifs, currentNode, currentNodeLine, hasAnyContent);

            if (result.Nodes.Count == 0)
            {
                result.Errors.Add(new Issue(
                    0,
                    "El guion no declara ningún nodo ('node <nombre>:')."));
            }

            foreach ((int line, string label) in jumpRefs)
            {
                if (!result.Nodes.Contains(label))
                {
                    result.Errors.Add(new Issue(
                        line,
                        $"':: jump {label}' apunta a un nodo que no existe. " +
                        "El parser lanzará KeyNotFoundException al reproducir el diálogo."));
                }
            }

            return result;
        }

        /// <summary>
        /// Comprueba además que un nodo de inicio concreto exista, útil para
        /// validar el campo "startNode" que se pasa a DialogueSystem.Play
        /// antes de arrancar la reproducción (evita el KeyNotFoundException
        /// de DialogueRunner.Play si alguien escribe mal el nodo en el inspector).
        /// </summary>
        public static bool HasStartNode(Result result, string startNode)
        {
            return result.Nodes.Contains(startNode);
        }

        private static void CloseNode(
            Result result,
            Stack<ChoiceFrame> choices,
            Stack<IfFrame> ifs,
            string currentNode,
            int currentNodeLine,
            bool hasAnyContent)
        {
            if (currentNode != null && !hasAnyContent)
            {
                result.Warnings.Add(new Issue(
                    currentNodeLine,
                    $"Nodo '{currentNode}' está vacío."));
            }

            while (choices.Count > 0)
            {
                ChoiceFrame frame = choices.Pop();

                if (frame.OptionBodyLines == 0)
                {
                    result.Warnings.Add(new Issue(
                        frame.OptionStartLine,
                        "Opción de choice sin ningún contenido en su cuerpo antes de cerrar el nodo."));
                }
            }

            while (ifs.Count > 0)
            {
                IfFrame frame = ifs.Pop();

                result.Errors.Add(new Issue(
                    frame.OpenLine,
                    "':: if' sin su ':: end' correspondiente antes de cerrar el nodo. " +
                    "El salto de la condición falsa nunca se resuelve: si la condición es falsa, " +
                    "el diálogo terminará en silencio."));
            }
        }

        private static void ValidateSystemLine(
            string line,
            int lineNumber,
            Stack<IfFrame> ifs,
            List<(int Line, string Label)> jumpRefs,
            Result result)
        {
            string content = line.Substring(2).Trim();

            string[] parts = content.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                result.Errors.Add(new Issue(lineNumber, "'::' sin ningún comando después."));
                return;
            }

            string command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "jump":
                    if (parts.Length < 2)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: jump' sin nombre de nodo destino."));
                    }
                    else
                    {
                        jumpRefs.Add((lineNumber, parts[1]));

                        result.Warnings.Add(new Issue(
                            lineNumber,
                            $"':: jump {parts[1]}' no dispara ningún ':: Return'. " +
                            "Confirma que este no es el sitio donde querías escribir 'Return' " +
                            "(o que el nodo destino termina llamando a Return en algún punto)."));
                    }
                    break;

                case "if":
                    if (parts.Length < 2)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: if' sin ninguna condición."));
                    }

                    ifs.Push(new IfFrame { OpenLine = lineNumber });
                    break;

                case "elseif":
                    if (ifs.Count == 0)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: elseif' sin un ':: if' abierto (el parser lanzará una excepción)."));
                    }
                    else if (ifs.Peek().HasElse)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: elseif' después de un ':: else' en el mismo bloque."));
                    }
                    else if (parts.Length < 2)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: elseif' sin ninguna condición."));
                    }
                    break;

                case "else":
                    if (ifs.Count == 0)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: else' sin un ':: if' abierto (el parser lanzará una excepción)."));
                    }
                    else if (ifs.Peek().HasElse)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "Más de un ':: else' en el mismo bloque 'if'."));
                    }
                    else
                    {
                        ifs.Peek().HasElse = true;
                    }
                    break;

                case "end":
                    if (ifs.Count == 0)
                    {
                        result.Errors.Add(new Issue(
                            lineNumber,
                            "':: end' sin un ':: if' abierto (el parser lanzará una excepción)."));
                    }
                    else
                    {
                        ifs.Pop();
                    }
                    break;

                case "endstory":
                    break;

                case "wait":
                case "auto":
                case "autoonce":
                case "speed":
                    ValidateNumericArg(command, parts, lineNumber, result);
                    break;

                default:
                    // Comando custom (ej. "Return"): el parser no lo valida en
                    // tiempo de compilación, solo comprueba en runtime si hay
                    // un handler registrado en DialogueContext.Commands.
                    // No es posible verificarlo aquí de forma estática.
                    break;
            }
        }

        private static void ValidateNumericArg(
            string command,
            string[] parts,
            int lineNumber,
            Result result)
        {
            if (parts.Length < 2)
                return; // Todos tienen valor por defecto si se omite.

            if (!float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                result.Warnings.Add(new Issue(
                    lineNumber,
                    $"':: {command} {parts[1]}' no es un número válido, se usará el valor por defecto."));
            }
        }

        private static string ParseNodeName(string line)
        {
            return line
                .Substring(5)
                .Replace(":", string.Empty)
                .Trim();
        }

        private static string NormalizeLineEndings(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
        }
    }
}
