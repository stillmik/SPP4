using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib;

namespace MainPart
{
    public class Pipeline
    {
        public Task Generate(IEnumerable<string> files, string pathToGenerated)// файлы с которых генерим точнее их имена СССС .сs, куда генерим
        {
            var execOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = files.Count() };//дает настройки для обработки представленной by dataflow blocks, которая обрабатывает сообщения пользователя ерез делегат пользователя
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };//Provides options used to configure a link between dataflow blocks.  //Gets or sets whether the linked target will have completion and faulting notification propagated to it automatically.

            var downloadStringBlock = new TransformBlock<string, string>//Provides a dataflow block that invokes a provided Func<T,TResult> delegate for every data element received.
            (
                async path =>
                {
                    Console.WriteLine("DCP");
                    using var reader = new StreamReader(path);
                    return await reader.ReadToEndAsync();
                },
                execOptions
            );
            var generateTestsBlock = new TransformManyBlock<string, KeyValuePair<string, string>>//Provides a dataflow block that invokes a provided Func<T,TResult> delegate for every data element received.
            (
                async sourceCode =>
                {
                    var fileInfo = await Task.Run(() => CodeAnalyzer.GetFileInfo(sourceCode));
                    Console.WriteLine("zzz");
                    return await Task.Run(() => TestsGenerator.GenerateTests(fileInfo));// fileName of new generated file no.cs | text
                },
                execOptions
            );
            var writeFileBlock = new ActionBlock<KeyValuePair<string, string>>
            (
                async fileNameCodePair =>
                {
                    Console.WriteLine("ohoho");
                    using var writer = new StreamWriter(pathToGenerated + '\\' + fileNameCodePair.Key + ".cs");
                    Console.WriteLine("hhh" + fileNameCodePair.Key);
                    await writer.WriteAsync(fileNameCodePair.Value);
                },
                execOptions
            );

            downloadStringBlock.LinkTo(generateTestsBlock, linkOptions);
            generateTestsBlock.LinkTo(writeFileBlock, linkOptions);

            foreach (var file in files)
            {
                downloadStringBlock.Post(file);
                Console.WriteLine(file);
            }

            downloadStringBlock.Complete();
            return writeFileBlock.Completion;
        }
    }
}
