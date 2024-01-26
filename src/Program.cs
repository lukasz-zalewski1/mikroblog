namespace WykopDiscussions
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var manager = new Manager();

            // Arguments
            (int, int) discussionsRange;
            string logType = "";
            bool isMaxLog = false;
            // Only for manual adjustment
            string arg4 = "";
            int arg5 = 0;
            string arg6 = "";

            // Reads arguments
            //discussionsRange.Item1 = Convert.ToInt32(args[0]);
            //discussionsRange.Item2 = Convert.ToInt32(args[1]);
            //string processType = args[2];
            //if (args.Length > 3)
            //    logType = args[3];

            //if (args.Length > 5)
            //{
            //    arg4 = args[4];
            //    arg5 = Convert.ToInt32(args[5]);
            //}

            //if (args.Length > 6)
            //{
            //    arg6 = args[6];
            //}

            discussionsRange.Item1 = Convert.ToInt32(Console.ReadLine());
            discussionsRange.Item2 = Convert.ToInt32(Console.ReadLine());
            //string processType = "-extended";
            string processType = "";

            // Apply maximum amount of logs if specified
            if (logType == "-maxlog")
                isMaxLog = true;

            // Run process depending on processType
            if (processType == "-extended")
            {
                await manager.Run(Manager.ProcessType.CreateVideoScript, discussionsRange, isMaxLog);
            }
            else if (processType == "-redo")
            {
                await manager.RunManualAdjustment(arg4, arg5, arg6);
            }
            else if (processType == "-vs")
            {
                manager.RunCreateVideoScript(discussionsRange.Item1, arg4, arg5);
            }
            else if (processType == "-speech")
            {
                await manager.RunManualSpeech();
            }
            else
            {
                await manager.Run(Manager.ProcessType.DataPreparation, discussionsRange, isMaxLog);
            }
        }
    }
}