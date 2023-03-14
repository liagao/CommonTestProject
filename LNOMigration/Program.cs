namespace LNOMigration
{
    class MachineItem
    {
        public MachineItem(string line)
        {
            var parts = line.Split(",");
            this.MachineLine = line;
            this.IsLNO = line.Contains("LogicalNameMachine");
            this.MFPartitionKey = parts[2] + parts[6]??String.Empty;
            this.MachineName = parts[0];
        }

        public string MFPartitionKey { get; set; }
        public string MachineLine { get; set; }
        public string MachineName { get; set; }
        public bool IsLNO { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var machineList = MigrateMachineList(@"D:\apgold\autopilotservice\Bn2B\XAP-Prod-BN2B\machines.csv", 10)
                .Union(MigrateMachineList(@"D:\apgold\autopilotservice\Ch1b\XAP-Prod-Ch1b\machines.csv", 10))
                .Union(MigrateMachineList(@"D:\apgold\autopilotservice\DUB02\XAP-Prod-DUB02\machines.csv", 10))
                .Union(MigrateMachineList(@"D:\apgold\autopilotservice\HKG01\XAP-Prod-HKG01\machines.csv", 10))
                ;
            File.WriteAllLines(@"D:\MigrateMachineList.txt", machineList);
        }

        private static List<string> MigrateMachineList(string filePath, int batchSize)
        {
            var migratedMachineNameList = new List<string>();
            var lineList = new List<MachineItem>();
            var machineDic = new Dictionary<string, List<MachineItem>>();
            foreach (var line in File.ReadAllLines(filePath).Where(o=>!o.StartsWith("#") && !string.IsNullOrWhiteSpace(o)))
            {
                var machineItem = new MachineItem(line);
                lineList.Add(machineItem);
                if (machineDic.ContainsKey(machineItem.MFPartitionKey))
                {
                    machineDic[machineItem.MFPartitionKey].Add(machineItem);
                }
                else
                {
                    machineDic.Add(machineItem.MFPartitionKey, new List<MachineItem>() { machineItem });
                }
            }

            foreach (var item in machineDic)
            {
                int count = item.Value.Count * batchSize/100;
                if (count ==0)
                {
                    count += 1;
                }

                int i = 0;
                foreach (var machine in item.Value)
                {
                    if(!machine.IsLNO)
                    {
                        machine.MachineLine = machine.MachineLine.Replace("LogicalMachine", "LogicalNameMachine");
                        migratedMachineNameList.Add(machine.MachineName);
                        i++;

                        if(i==count)
                        {
                            break;
                        }
                    }
                }
            }

            File.WriteAllLines(filePath, new string[] { "#Version: 1.0", "#Fields: MachineName,Image,MachineFunction,ScaleUnit,RequestedSku,MachineType,PartitionGroups" });
            File.AppendAllLines(filePath, lineList.Select(o=>o.MachineLine));

            return migratedMachineNameList;
        }
    }
}